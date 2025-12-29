using System.Collections.Generic;

/// <summary>
/// Tracks consumable item usage.
/// SRP: Only responsible for consumable tracking.
/// </summary>
public class ConsumableTracker : BaseStatTracker<Dictionary<string, int>>
{
    private Dictionary<string, int> consumablesUsed;
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
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        // For consumables, track the total count over time
        return new TimeSeriesDataPoint(timestamp, totalConsumablesUsed);
    }
    
    public override void Reset()
    {
        base.Reset();
        consumablesUsed.Clear();
        totalConsumablesUsed = 0;
    }
}
