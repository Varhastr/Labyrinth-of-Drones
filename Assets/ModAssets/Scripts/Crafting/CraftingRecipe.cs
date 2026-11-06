using UnityEngine;

namespace LabyrinthOfDrones
{
    public enum CraftResultType
    {
        WeaponPickup,   // Instantiates a weapon pickup near the player
        Structure,      // Enters placement mode
        Consumable,     // Instant effect (v1 just logs)
        Utility         // Treated the same as Structure for now
    }

    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Labyrinth of Drones/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        public string id;                       // unique key, e.g. "scrap_barrier"
        public string displayName = "New Item";
        [TextArea] public string description;
        public Sprite icon;

        public int scrapCost = 10;
        public int circuitryCost = 0;

        public CraftResultType resultType = CraftResultType.Structure;
        public GameObject resultPrefab;         // Required for WeaponPickup and Structure

        // Optional future fields
        public int maxActiveInstances = 0;      // 0 = unlimited
    }
}
