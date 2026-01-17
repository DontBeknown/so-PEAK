using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks consumable item usage.
/// SRP: Only responsible for consumable tracking.
/// </summary>
public class ConsumableTracker : BaseStatTracker<Dictionary<string, int>>
{
    private Dictionary<string, int> consumablesUsed;
    private Dictionary<InventoryItem, int> consumableItemsUsed; // Track actual items
    private int totalConsumablesUsed;
    
    public override string MetricName => "Consumables Used";
    public override Dictionary<string, int> CurrentValue 
    { 
        get => new Dictionary<string, int>(consumablesUsed);
        set => consumablesUsed = new Dictionary<string, int>(value);
    }
    
    /// <summary>
    /// Total count of all consumables used.
    /// </summary>
    public int TotalCount => totalConsumablesUsed;
    
    public ConsumableTracker(int maxDataPoints = 100) : base(maxDataPoints)
    {
        consumablesUsed = new Dictionary<string, int>();
        consumableItemsUsed = new Dictionary<InventoryItem, int>();
        totalConsumablesUsed = 0;
    }
    
    /// <summary>
    /// Records the usage of a consumable item.
    /// </summary>
    /// <param name="itemName">Name of the item consumed</param>
    public void RecordConsumable(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        
        if (consumablesUsed.ContainsKey(itemName))
        {
            consumablesUsed[itemName]++;
        }
        else
        {
            consumablesUsed[itemName] = 1;
        }
        
        totalConsumablesUsed++;
    }
    
    /// <summary>
    /// Records the usage of a consumable item (preferred method).
    /// </summary>
    /// <param name="item">The InventoryItem consumed</param>
    public void RecordConsumable(InventoryItem item)
    {
        if (item == null) return;
        
        // Track by name for backward compatibility
        RecordConsumable(item.itemName);
        
        // Track by item reference for assessment
        if (consumableItemsUsed.ContainsKey(item))
        {
            consumableItemsUsed[item]++;
        }
        else
        {
            consumableItemsUsed[item] = 1;
        }
    }
    
    public override void RecordValue(Dictionary<string, int> value)
    {
        // Not typically used directly, but required by interface
        consumablesUsed = new Dictionary<string, int>(value);
        
        // Recalculate total
        totalConsumablesUsed = 0;
        foreach (var count in consumablesUsed.Values)
        {
            totalConsumablesUsed += count;
        }
    }
    
    /// <summary>
    /// Gets count of food items consumed (items that restore Hunger)
    /// </summary>
    public int GetFoodItemsConsumed()
    {
        int count = 0;
        foreach (var kvp in consumableItemsUsed)
        {
            if (IsFood(kvp.Key))
                count += kvp.Value;
        }
        return count;
    }
    
    /// <summary>
    /// Gets count of water items consumed (items that restore Thirst)
    /// </summary>
    public int GetWaterItemsConsumed()
    {
        int count = 0;
        foreach (var kvp in consumableItemsUsed)
        {
            if (IsWater(kvp.Key))
                count += kvp.Value;
        }
        return count;
    }
    
    private bool IsFood(InventoryItem item)
    {
        if (item == null || !item.isConsumable) return false;
        
        // Check if item has consumable effects that restore Hunger
        foreach (var effect in item.consumableEffects)
        {
            if (effect.statType == StatType.Hunger && effect.value > 0)
                return true;
        }
        return false;
    }
    
    private bool IsWater(InventoryItem item)
    {
        if (item == null || !item.isConsumable) return false;
        
        // Check if item has consumable effects that restore Thirst
        foreach (var effect in item.consumableEffects)
        {
            if (effect.statType == StatType.Thirst && effect.value > 0)
                return true;
        }
        return false;
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        // For consumables, track the total count over time
        return new TimeSeriesDataPoint(timestamp, totalConsumablesUsed);
    }
    
    public override void Reset()
    {
        base.Reset();
        consumablesUsed.Clear();
        consumableItemsUsed.Clear();
        totalConsumablesUsed = 0;
    }
}
