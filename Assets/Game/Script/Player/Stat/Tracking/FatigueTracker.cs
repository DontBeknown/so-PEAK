/// <summary>
/// Tracks fatigue accumulation over time.
/// SRP: Only responsible for fatigue tracking.
/// </summary>
public class FatigueTracker : BaseStatTracker<float>
{
    private float totalFatigueAccumulated;
    
    public override string MetricName => "Fatigue Accumulated";
    public override float CurrentValue 
    { 
        get => totalFatigueAccumulated;
        set => totalFatigueAccumulated = value;
    }
    
    public FatigueTracker(int maxDataPoints = 100) : base(maxDataPoints)
    {
        totalFatigueAccumulated = 0f;
    }
    
    /// <summary>
    /// Records the current fatigue value.
    /// Note: This tracks the current fatigue level, not increments.
    /// </summary>
    public override void RecordValue(float fatigueValue)
    {
        // Track maximum fatigue reached
        if (fatigueValue > totalFatigueAccumulated)
        {
            totalFatigueAccumulated = fatigueValue;
        }
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        return new TimeSeriesDataPoint(timestamp, totalFatigueAccumulated);
    }
    
    public override void Reset()
    {
        base.Reset();
        totalFatigueAccumulated = 0f;
    }
}
