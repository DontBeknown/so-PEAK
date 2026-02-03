using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Player.Inventory;

public class CraftingSlotUI : MonoBehaviour
{
    [Header("UI References - Slot Display Only")]
    [SerializeField] private Image recipeIcon;
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private GameObject selectedBorder; // Visual indicator when selected

    [Header("Visual Feedback")]
    [SerializeField] private Color canCraftColor = Color.white;
    [SerializeField] private Color cannotCraftColor = Color.gray;

    private CraftingRecipe recipe;
    private CraftingUI craftingUI;
    private IInventoryService inventoryService;
    private bool canCraft = false;

    public CraftingRecipe Recipe => recipe;
    public bool CanCraft => canCraft;

    public void Initialize(CraftingUI ui, CraftingRecipe craftingRecipe, IInventoryService inventory)
    {
        craftingUI = ui;
        recipe = craftingRecipe;
        inventoryService = inventory;

        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (recipe == null) return;

        // Update icon
        if (recipeIcon != null && recipe.icon != null)
        {
            recipeIcon.sprite = recipe.icon;
            recipeIcon.enabled = true;
        }
        else if (recipeIcon != null)
        {
            recipeIcon.enabled = false;
        }

        // Update name
        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.recipeName;
        }

        // Check if can craft
        canCraft = recipe.CanCraft(inventoryService);

        // Update visual feedback
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        Color targetColor = canCraft ? canCraftColor : cannotCraftColor;

        if (recipeIcon != null)
        {
            recipeIcon.color = targetColor;
        }

        if (recipeNameText != null)
        {
            recipeNameText.color = targetColor;
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(selected);
        }
    }

    public void OnSlotClicked()
    {
        if (craftingUI != null)
        {
            craftingUI.SelectRecipe(this);
        }
    }
}
