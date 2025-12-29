/// <summary>
/// Tracks total stamina consumed by the player.
/// SRP: Only responsible for stamina usage tracking.
/// </summary>
public class StaminaTracker : BaseStatTracker<float>
{
    private float totalStaminaUsed;
    
    public override string MetricName => "Stamina Used";
    public override float CurrentValue 
    { 
        get => totalStaminaUsed;
        set => totalStaminaUsed = value;
    }
    
    public StaminaTracker(int maxDataPoints = 100) : base(maxDataPoints)
    {
        totalStaminaUsed = 0f;
    }
    
    public override void RecordValue(float staminaAmount)
    {
        if (staminaAmount > 0f)
        {
            totalStaminaUsed += staminaAmount;
        }
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        return new TimeSeriesDataPoint(timestamp, totalStaminaUsed);
    }
    
    public override void Reset()
    {
        base.Reset();
        totalStaminaUsed = 0f;
    }
}
