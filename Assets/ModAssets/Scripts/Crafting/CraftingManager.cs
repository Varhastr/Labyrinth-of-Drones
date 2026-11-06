using UnityEngine;
using System.Collections.Generic;

namespace LabyrinthOfDrones
{
    public class CraftingManager : MonoBehaviour
    {
        public static CraftingManager Instance { get; private set; }

        [Header("Recipes")]
        public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

        [Header("Placement")]
        public StructurePlacer structurePlacer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool CanAfford(CraftingRecipe recipe)
        {
            if (recipe == null) return false;
            return Inventory.Scrap >= recipe.scrapCost && Inventory.Circuitry >= recipe.circuitryCost;
        }

        public bool TryCraft(CraftingRecipe recipe)
        {
            if (recipe == null) return false;
            if (!CanAfford(recipe)) return false;

            if (!Inventory.TrySpend(recipe.scrapCost, recipe.circuitryCost))
                return false;

            switch (recipe.resultType)
            {
                case CraftResultType.WeaponPickup:
                    SpawnWeaponPickup(recipe);
                    break;
                case CraftResultType.Structure:
                case CraftResultType.Utility:
                    if (structurePlacer != null)
                        structurePlacer.BeginPlacement(recipe);
                    else
                        Debug.LogWarning("[CraftingManager] StructurePlacer is missing");
                    break;
                case CraftResultType.Consumable:
                    // v1: just log. Later this can apply a heal or give a temporary item.
                    Debug.Log($"Crafted consumable: {recipe.displayName}");
                    break;
            }

            return true;
        }

        void SpawnWeaponPickup(CraftingRecipe recipe)
        {
            if (recipe.resultPrefab == null) return;

            var player = FindObjectOfType<Unity.FPS.Gameplay.PlayerCharacterController>();
            if (player == null)
            {
                Debug.LogWarning("[CraftingManager] Could not find player to spawn weapon near");
                return;
            }

            Vector3 spawnPos = player.transform.position + player.transform.forward * 1.5f + Vector3.up * 0.5f;
            Instantiate(recipe.resultPrefab, spawnPos, Quaternion.identity);
        }
    }
}
