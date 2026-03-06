using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public string description;
    public Sprite icon;
    public int maxStackSize = 1;

    [Header("Item Type")]
    public ItemType itemType;
    public ItemCategory category;

    [Header("Grid Size (cells)")]
    public Vector2Int gridSize = Vector2Int.one;

    [Header("Consumable Properties")]
    public bool isConsumable = false;
    public ConsumableEffect[] consumableEffects;

    [Header("Crafting Properties")]
    public bool isCraftingMaterial = false;
    public bool isCraftedItem = false;

    private void OnValidate()
    {
        gridSize = Vector2Int.Max(gridSize, Vector2Int.one);
    }
}

[System.Serializable]
public class ConsumableEffect
{
    public StatType statType;
    public float value;
    public bool isPercentage = false;
}

public enum ItemType
{
    Resource,
    Food,
    Tool,
    Equipment,
    Crafted
}

public enum ItemCategory
{
    Stick,
    Rock,
    Leaf,
    Berry,
    Fish,
    CraftingTool,
    Shelter
}