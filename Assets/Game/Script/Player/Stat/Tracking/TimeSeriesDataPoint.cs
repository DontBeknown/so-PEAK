using System;

/// <summary>
/// Represents a single data point in a time series for graphing.
/// </summary>
[Serializable]
public class TimeSeriesDataPoint
{
    /// <summary>
    /// Time since session start (in seconds).
    /// </summary>
    public float Timestamp;
    
    /// <summary>
    /// Metric value at this timestamp.
    /// </summary>
    public float Value;
    
    public TimeSeriesDataPoint(float timestamp, float value)
    {
        Timestamp = timestamp;
        Value = value;
    }
}
