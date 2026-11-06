using UnityEngine;

namespace LabyrinthOfDrones
{
    /// <summary>
    /// Simple static inventory for Scrap and Circuitry.
    /// Lives for the duration of a run. Max 999 of each resource.
    /// </summary>
    public static class Inventory
    {
        public static int Scrap { get; private set; } = 0;
        public static int Circuitry { get; private set; } = 0;

        public static event System.Action OnInventoryChanged;

        public static void AddScrap(int amount)
        {
            if (amount <= 0) return;
            Scrap = Mathf.Min(Scrap + amount, 999);
            OnInventoryChanged?.Invoke();
        }

        public static void AddCircuitry(int amount)
        {
            if (amount <= 0) return;
            Circuitry = Mathf.Min(Circuitry + amount, 999);
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Attempts to spend resources. Returns true only if the player had enough
        /// and the amounts were successfully deducted.
        /// </summary>
        public static bool TrySpend(int scrapCost, int circuitryCost)
        {
            if (Scrap < scrapCost || Circuitry < circuitryCost)
                return false;

            Scrap -= scrapCost;
            Circuitry -= circuitryCost;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public static void Reset()
        {
            Scrap = 0;
            Circuitry = 0;
            OnInventoryChanged?.Invoke();
        }

        // Optional debug helper
        public static void DebugSet(int scrap, int circuitry)
        {
            Scrap = Mathf.Clamp(scrap, 0, 999);
            Circuitry = Mathf.Clamp(circuitry, 0, 999);
            OnInventoryChanged?.Invoke();
        }
    }
}
