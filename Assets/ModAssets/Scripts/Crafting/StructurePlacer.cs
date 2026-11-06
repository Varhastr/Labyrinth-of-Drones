using UnityEngine;

namespace LabyrinthOfDrones
{
    /// <summary>
    /// Handles ghost preview and placement of structures.
    /// Left click = place, Right click or Escape = cancel (and refund resources).
    /// </summary>
    public class StructurePlacer : MonoBehaviour
    {
        public LayerMask placementLayerMask = ~0;
        public float maxPlaceDistance = 8f;
        public Material ghostValidMaterial;
        public Material ghostInvalidMaterial;

        CraftingRecipe currentRecipe;
        GameObject ghostInstance;
        bool isPlacing = false;

        public void BeginPlacement(CraftingRecipe recipe)
        {
            if (recipe == null || recipe.resultPrefab == null) return;

            CancelPlacement();

            currentRecipe = recipe;
            isPlacing = true;

            ghostInstance = Instantiate(recipe.resultPrefab);

            // Disable colliders and rigidbodies while ghosting
            foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var rb in ghostInstance.GetComponentsInChildren<Rigidbody>())
                rb.isKinematic = true;

            SetGhostMaterial(ghostValidMaterial);
        }

        public void CancelPlacement()
        {
            isPlacing = false;
            currentRecipe = null;
            if (ghostInstance != null)
            {
                Destroy(ghostInstance);
                ghostInstance = null;
            }
        }

        void Update()
        {
            if (!isPlacing || ghostInstance == null) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementLayerMask))
            {
                ghostInstance.transform.position = hit.point;

                bool canPlace = IsValidPlacement(hit);
                SetGhostMaterial(canPlace ? ghostValidMaterial : ghostInvalidMaterial);

                if (Input.GetMouseButtonDown(0) && canPlace)
                {
                    PlaceStructure(hit.point);
                }
            }

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                // Refund on cancel
                if (currentRecipe != null)
                {
                    Inventory.AddScrap(currentRecipe.scrapCost);
                    Inventory.AddCircuitry(currentRecipe.circuitryCost);
                }
                CancelPlacement();
            }
        }

        bool IsValidPlacement(RaycastHit hit)
        {
            if (hit.collider == null) return false;
            // TODO: add slope checks, overlap checks, etc. later
            return true;
        }

        void PlaceStructure(Vector3 position)
        {
            if (currentRecipe == null || currentRecipe.resultPrefab == null) return;

            Instantiate(currentRecipe.resultPrefab, position, ghostInstance.transform.rotation);
            CancelPlacement();
        }

        void SetGhostMaterial(Material mat)
        {
            if (mat == null || ghostInstance == null) return;
            foreach (var r in ghostInstance.GetComponentsInChildren<Renderer>())
            {
                r.material = mat;
            }
        }
    }
}
