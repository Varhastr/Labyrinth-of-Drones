using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace LabyrinthOfDrones
{
    /// <summary>
    /// Minimal crafting menu.
    /// - Opens/closes with the configured key (default C).
    /// - Shows all recipes from CraftingManager.
    /// - Craft button only enabled when the player can afford the recipe.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.C;

        [Header("UI Roots")]
        public GameObject panelRoot;          // The whole crafting panel

        [Header("Recipe List")]
        public Transform recipeListParent;   // Content of a ScrollView or simple VerticalLayoutGroup
        public GameObject recipeEntryPrefab; // Prefab that has RecipeEntryUI component

        bool isOpen = false;

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            isOpen = !isOpen;
            if (panelRoot != null)
                panelRoot.SetActive(isOpen);

            if (isOpen)
                RebuildList();
        }

        public void Close()
        {
            isOpen = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        void RebuildList()
        {
            if (recipeListParent == null || recipeEntryPrefab == null) return;
            if (CraftingManager.Instance == null) return;

            // Clear old entries
            foreach (Transform child in recipeListParent)
                Destroy(child.gameObject);

            foreach (var recipe in CraftingManager.Instance.allRecipes)
            {
                if (recipe == null) continue;

                GameObject entryGO = Instantiate(recipeEntryPrefab, recipeListParent);
                var entry = entryGO.GetComponent<RecipeEntryUI>();
                if (entry != null)
                {
                    entry.Setup(recipe, this);
                }
            }
        }

        public void OnCraftPressed(CraftingRecipe recipe)
        {
            if (CraftingManager.Instance == null) return;

            bool success = CraftingManager.Instance.TryCraft(recipe);
            if (success)
            {
                // Close menu when entering placement mode or after successful craft
                Close();
            }
        }
    }

    /// <summary>
    /// Attach this to the recipe entry prefab.
    /// </summary>
    public class RecipeEntryUI : MonoBehaviour
    {
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI descriptionText;
        public Image iconImage;
        public Button craftButton;

        CraftingRecipe recipe;
        CraftingUI owner;

        public void Setup(CraftingRecipe r, CraftingUI ui)
        {
            recipe = r;
            owner = ui;

            if (nameText != null) nameText.text = r.displayName;
            if (descriptionText != null) descriptionText.text = r.description;
            if (costText != null)
                costText.text = $"Scrap: {r.scrapCost}  |  Circuitry: {r.circuitryCost}";
            if (iconImage != null && r.icon != null)
                iconImage.sprite = r.icon;

            if (craftButton != null)
            {
                craftButton.onClick.RemoveAllListeners();
                craftButton.onClick.AddListener(() => owner.OnCraftPressed(recipe));
                craftButton.interactable = CraftingManager.Instance != null &&
                                           CraftingManager.Instance.CanAfford(recipe);
            }
        }

        void Update()
        {
            // Keep button state up to date while menu is open
            if (craftButton != null && recipe != null && CraftingManager.Instance != null)
            {
                craftButton.interactable = CraftingManager.Instance.CanAfford(recipe);
            }
        }
    }
}
