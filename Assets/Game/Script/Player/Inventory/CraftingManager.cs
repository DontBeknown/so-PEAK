using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Game.Core.DI;
using Game.Core.Events;

public class CraftingManager : MonoBehaviour
{
    [Header("Crafting Settings")]
    [SerializeField] private List<CraftingRecipe> availableRecipes;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Crafting Stations")]
    [SerializeField] private bool nearCampfire = false;
    [SerializeField] private bool nearWorkbench = false;

    private bool isCrafting = false;
    private IEventBus eventBus;

    private void Awake()
    {
        // Get EventBus from ServiceContainer
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
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
        eventBus?.Publish(new CraftingStartedEvent(recipe));

        // Consume materials first
        if (!recipe.ConsumeMaterials(inventoryManager))
        {
            eventBus?.Publish(new CraftingFailedEvent(recipe, "Failed to consume materials"));
            isCrafting = false;
            yield break;
        }

        // Wait for crafting time
        yield return new WaitForSeconds(recipe.craftingTime);

        // Add result to inventory
        if (inventoryManager.AddItem(recipe.resultItem, recipe.resultQuantity))
        {
            eventBus?.Publish(new CraftingCompletedEvent(recipe));
        }
        else
        {
            eventBus?.Publish(new CraftingFailedEvent(recipe, "Inventory full"));
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