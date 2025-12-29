using UnityEngine;

/// <summary>
/// Tracks total distance walked by the player.
/// SRP: Only responsible for distance tracking.
/// </summary>
public class DistanceTracker : BaseStatTracker<float>
{
    private Vector3 lastPosition;
    private float totalDistance;
    private bool isInitialized;
    
    public override string MetricName => "Distance Walked";
    public override float CurrentValue 
    { 
        get => totalDistance;
        set => totalDistance = value;
    }
    
    public DistanceTracker(int maxDataPoints = 100) : base(maxDataPoints)
    {
        totalDistance = 0f;
        lastPosition = Vector3.zero;
        isInitialized = false;
    }
    
    /// <summary>
    /// Updates position and calculates distance traveled.
    /// Call this every frame with current player position.
    /// </summary>
    public void UpdatePosition(Vector3 currentPosition)
    {
        if (!isInitialized)
        {
            lastPosition = currentPosition;
            isInitialized = true;
            return;
        }
        
        float distance = Vector3.Distance(lastPosition, currentPosition);
        
        // Only record if movement is meaningful (avoid floating point noise)
        if (distance > 0.01f)
        {
            RecordValue(distance);
        }
        
        lastPosition = currentPosition;
    }
    
    public override void RecordValue(float distance)
    {
        totalDistance += distance;
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        return new TimeSeriesDataPoint(timestamp, totalDistance);
    }
    
    public override void Reset()
    {
        base.Reset();
        totalDistance = 0f;
        lastPosition = Vector3.zero;
        isInitialized = false;
    }
}
