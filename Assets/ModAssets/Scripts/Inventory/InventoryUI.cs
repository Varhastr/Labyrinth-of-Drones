using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LabyrinthOfDrones
{
    /// <summary>
    /// Simple HUD that shows current Scrap and Circuitry counts.
    /// Assign the TextMeshProUGUI (or Text) references in the inspector.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI scrapText;
        public TextMeshProUGUI circuitryText;

        // Fallback if using classic UI Text
        public Text scrapTextLegacy;
        public Text circuitryTextLegacy;

        void OnEnable()
        {
            Inventory.OnInventoryChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Inventory.OnInventoryChanged -= Refresh;
        }

        void Refresh()
        {
            if (scrapText != null)
                scrapText.text = Inventory.Scrap.ToString();
            else if (scrapTextLegacy != null)
                scrapTextLegacy.text = Inventory.Scrap.ToString();

            if (circuitryText != null)
                circuitryText.text = Inventory.Circuitry.ToString();
            else if (circuitryTextLegacy != null)
                circuitryTextLegacy.text = Inventory.Circuitry.ToString();
        }
    }
}
