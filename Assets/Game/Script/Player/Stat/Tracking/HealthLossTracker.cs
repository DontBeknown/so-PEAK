/// <summary>
/// Tracks total health lost by the player.
/// SRP: Only responsible for health loss tracking.
/// </summary>
public class HealthLossTracker : BaseStatTracker<float>
{
    private float totalHealthLost;
    
    public override string MetricName => "Health Lost";
    public override float CurrentValue 
    { 
        get => totalHealthLost;
        set => totalHealthLost = value;
    }
    
    public HealthLossTracker(int maxDataPoints = 100) : base(maxDataPoints)
    {
        totalHealthLost = 0f;
    }
    
    public override void RecordValue(float damageAmount)
    {
        if (damageAmount > 0f)
        {
            totalHealthLost += damageAmount;
        }
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        return new TimeSeriesDataPoint(timestamp, totalHealthLost);
    }
    
    public override void Reset()
    {
        base.Reset();
        totalHealthLost = 0f;
    }
}
