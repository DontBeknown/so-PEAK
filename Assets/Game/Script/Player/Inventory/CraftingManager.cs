using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CraftingManager : MonoBehaviour
{
    [Header("Crafting Settings")]
    [SerializeField] private List<CraftingRecipe> availableRecipes;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Crafting Stations")]
    [SerializeField] private bool nearCampfire = false;
    [SerializeField] private bool nearWorkbench = false;

    // Events
    public static event Action<CraftingRecipe> OnCraftingStarted;
    public static event Action<CraftingRecipe> OnCraftingCompleted;
    public static event Action<CraftingRecipe> OnCraftingFailed;

    private bool isCrafting = false;

    private void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = GetComponent<InventoryManager>();
    }

    public List<CraftingRecipe> GetAvailableRecipes()
    {
        List<CraftingRecipe> craftableRecipes = new List<CraftingRecipe>();

        foreach (var recipe in availableRecipes)
        {
            if (CanCraftRecipe(recipe))
            {
                craftableRecipes.Add(recipe);
            }
        }

        return craftableRecipes;
    }

    // Get all recipes regardless of whether they can be crafted
    public List<CraftingRecipe> GetAllRecipes()
    {
        return new List<CraftingRecipe>(availableRecipes);
    }

    public bool CanCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null) return false;

        // Check materials
        if (!recipe.CanCraft(inventoryManager)) return false;

        // Check crafting station requirements
        if (recipe.requiresCampfire && !nearCampfire) return false;
        if (recipe.requiresWorkbench && !nearWorkbench) return false;

        return true;
    }

    public void StartCrafting(CraftingRecipe recipe)
    {
        if (isCrafting || !CanCraftRecipe(recipe)) return;

        StartCoroutine(CraftItem(recipe));
    }

    private IEnumerator CraftItem(CraftingRecipe recipe)
    {
        isCrafting = true;
        OnCraftingStarted?.Invoke(recipe);

        // Consume materials first
        if (!recipe.ConsumeMaterials(inventoryManager))
        {
            OnCraftingFailed?.Invoke(recipe);
            isCrafting = false;
            yield break;
        }

        // Wait for crafting time
        yield return new WaitForSeconds(recipe.craftingTime);

        // Add result to inventory
        if (inventoryManager.AddItem(recipe.resultItem, recipe.resultQuantity))
        {
            OnCraftingCompleted?.Invoke(recipe);
        }
        else
        {
            OnCraftingFailed?.Invoke(recipe);
            // TODO: Return materials to player or drop them on ground
        }

        isCrafting = false;
    }

    public void SetNearCampfire(bool near)
    {
        nearCampfire = near;
    }

    public void SetNearWorkbench(bool near)
    {
        nearWorkbench = near;
    }

    public bool IsCrafting()
    {
        return isCrafting;
    }
}