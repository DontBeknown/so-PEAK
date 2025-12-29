using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Abstract base class for all stat trackers.
/// Provides common time-series functionality using Template Method pattern.
/// OCP: Extensible without modification.
/// </summary>
public abstract class BaseStatTracker<T> : IStatTracker<T>
{
    protected readonly int maxDataPoints;
    protected readonly Queue<TimeSeriesDataPoint> timeSeriesQueue;
    
    public abstract string MetricName { get; }
    public abstract T CurrentValue { get; set; }
    
    public List<TimeSeriesDataPoint> TimeSeriesData => timeSeriesQueue.ToList();
    
    protected BaseStatTracker(int maxDataPoints = 100)
    {
        this.maxDataPoints = maxDataPoints;
        timeSeriesQueue = new Queue<TimeSeriesDataPoint>(maxDataPoints);
    }
    
    public abstract void RecordValue(T value);
    
    public virtual void UpdateTimeSeries(float timestamp)
    {
        // Maintain max data points by removing oldest
        if (timeSeriesQueue.Count >= maxDataPoints)
        {
            timeSeriesQueue.Dequeue();
        }
        
        timeSeriesQueue.Enqueue(CreateDataPoint(timestamp));
    }
    
    /// <summary>
    /// Template method for creating data points.
    /// Subclasses override to provide specific implementations.
    /// </summary>
    protected abstract TimeSeriesDataPoint CreateDataPoint(float timestamp);
    
    public virtual void Reset()
    {
        timeSeriesQueue.Clear();
    }
}
