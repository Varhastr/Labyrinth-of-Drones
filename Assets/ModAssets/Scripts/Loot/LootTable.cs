using UnityEngine;

namespace LabyrinthOfDrones
{
    [System.Serializable]
    public class LootEntry
    {
        public GameObject pickupPrefab;
        [Range(0f, 1f)] public float chance = 0.5f;
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    [CreateAssetMenu(fileName = "NewLootTable", menuName = "Labyrinth of Drones/Loot Table")]
    public class LootTable : ScriptableObject
    {
        public LootEntry[] entries;
    }
}
