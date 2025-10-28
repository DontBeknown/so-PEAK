using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName;
    public string description;
    public Sprite icon;

    [Header("Requirements")]
    public List<CraftingRequirement> requirements;

    [Header("Results")]
    public InventoryItem resultItem;
    public int resultQuantity = 1;

    [Header("Crafting Settings")]
    public float craftingTime = 2f;
    public bool requiresCampfire = false;
    public bool requiresWorkbench = false;

    public bool CanCraft(InventoryManager inventory)
    {
        foreach (var requirement in requirements)
        {
            if (!inventory.HasItem(requirement.item, requirement.quantity))
            {
                return false;
            }
        }
        return true;
    }

    public bool ConsumeMaterials(InventoryManager inventory)
    {
        if (!CanCraft(inventory)) return false;

        foreach (var requirement in requirements)
        {
            if (!inventory.RemoveItem(requirement.item, requirement.quantity))
            {
                return false; // This shouldn't happen if CanCraft returned true
            }
        }

        return true;
    }
}

[System.Serializable]
public class CraftingRequirement
{
    public InventoryItem item;
    public int quantity = 1;
}