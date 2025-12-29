using System.Collections.Generic;

/// <summary>
/// Generic interface for tracking a single statistic.
/// SRP: Each tracker handles ONE type of metric.
/// OCP: New trackers can be added without modifying existing code.
/// </summary>
public interface IStatTracker<T>
{
    /// <summary>
    /// Name of the metric being tracked.
    /// </summary>
    string MetricName { get; }
    
    /// <summary>
    /// Current cumulative value of the tracked metric.
    /// </summary>
    T CurrentValue { get; }
    
    /// <summary>
    /// Time-series data points for graphing.
    /// </summary>
    List<TimeSeriesDataPoint> TimeSeriesData { get; }
    
    /// <summary>
    /// Record a new value for this metric.
    /// </summary>
    void RecordValue(T value);
    
    /// <summary>
    /// Update the time-series data with a new snapshot.
    /// </summary>
    void UpdateTimeSeries(float timestamp);
    
    /// <summary>
    /// Reset all tracking data.
    /// </summary>
    void Reset();
}
