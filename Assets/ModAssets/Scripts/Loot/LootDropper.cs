using UnityEngine;

namespace LabyrinthOfDrones
{
    /// <summary>
    /// Attach this component to every enemy prefab.
    /// Call DropLoot() from the existing enemy death logic (do not create a new death system).
    /// </summary>
    public class LootDropper : MonoBehaviour
    {
        [Header("Loot")]
        public LootTable lootTable;
        public float dropForce = 3f;
        public float dropUpwardForce = 2f;

        public void DropLoot()
        {
            if (lootTable == null || lootTable.entries == null) return;

            foreach (var entry in lootTable.entries)
            {
                if (entry.pickupPrefab == null) continue;
                if (Random.value > entry.chance) continue;

                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                for (int i = 0; i < amount; i++)
                {
                    Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
                    GameObject pickup = Instantiate(entry.pickupPrefab, spawnPos, Quaternion.identity);

                    Rigidbody rb = pickup.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 force = Random.insideUnitSphere * dropForce;
                        force.y = dropUpwardForce;
                        rb.AddForce(force, ForceMode.Impulse);
                    }
                }
            }
        }
    }
}
