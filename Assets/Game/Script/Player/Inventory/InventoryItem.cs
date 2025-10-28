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

    [Header("Consumable Properties")]
    public bool isConsumable = false;
    public ConsumableEffect[] consumableEffects;

    [Header("Crafting Properties")]
    public bool isCraftingMaterial = false;
    public bool isCraftedItem = false;
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