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

    [Header("Grid Size (cells)")]
    public Vector2Int gridSize = Vector2Int.one;

    [Header("World Representation")]
    public GameObject worldPrefab;

    [Header("Consumable Properties")]
    public bool isConsumable = false;
    public ConsumableEffect[] consumableEffects;

    private void OnValidate()
    {
        gridSize = Vector2Int.Max(gridSize, Vector2Int.one);
    }
}

[System.Serializable]
public enum ConsumableEffectKind
{
    InstantStat = 0,
    ThirstDrainReductionBuff = 1
}

[System.Serializable]
public class ConsumableEffect
{
    [Tooltip("How this consumable effect is applied.")]
    public ConsumableEffectKind effectKind = ConsumableEffectKind.InstantStat;

    [Tooltip("Target stat for instant stat effects.")]
    public StatType statType;
    [Tooltip("Instant stat gain value, or percent thirst drain reduction (0..100) for thirst buff effects.")]
    public float value;
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