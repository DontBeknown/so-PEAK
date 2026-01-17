using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks player path positions during expedition.
/// Records positions at meaningful intervals for route analysis.
/// SRP: Only responsible for path recording.
/// </summary>
public class PathTracker : BaseStatTracker<Vector3>
{
    private readonly List<Vector3> pathPositions;
    private readonly float minRecordDistance;
    private Vector3 lastRecordedPosition;
    private bool isInitialized;
    
    public override string MetricName => "Player Path";
    
    public override Vector3 CurrentValue 
    { 
        get => pathPositions.Count > 0 ? pathPositions[pathPositions.Count - 1] : Vector3.zero;
        set { } // Not used for path tracking
    }
    
    /// <summary>
    /// Gets all recorded path positions
    /// </summary>
    public List<Vector3> PathPositions => new List<Vector3>(pathPositions);
    
    /// <summary>
    /// Gets total number of recorded positions
    /// </summary>
    public int PositionCount => pathPositions.Count;
    
    public PathTracker(float minRecordDistance = 1f, int maxDataPoints = 1000) : base(maxDataPoints)
    {
        this.minRecordDistance = minRecordDistance;
        pathPositions = new List<Vector3>();
        lastRecordedPosition = Vector3.zero;
        isInitialized = false;
    }
    
    /// <summary>
    /// Updates current position and records if moved significantly
    /// </summary>
    public void UpdatePosition(Vector3 currentPosition)
    {
        if (!isInitialized)
        {
            RecordValue(currentPosition);
            lastRecordedPosition = currentPosition;
            isInitialized = true;
            return;
        }
        
        float distance = Vector3.Distance(lastRecordedPosition, currentPosition);
        
        // Only record if moved significantly
        if (distance >= minRecordDistance)
        {
            RecordValue(currentPosition);
            lastRecordedPosition = currentPosition;
        }
    }
    
    public override void RecordValue(Vector3 position)
    {
        pathPositions.Add(position);
    }
    
    /// <summary>
    /// Gets path segment between start and end index
    /// </summary>
    public List<Vector3> GetPathSegment(int startIndex, int endIndex)
    {
        if (startIndex < 0 || endIndex >= pathPositions.Count || startIndex > endIndex)
            return new List<Vector3>();
        
        return pathPositions.GetRange(startIndex, endIndex - startIndex + 1);
    }
    
    /// <summary>
    /// Calculates total path distance
    /// </summary>
    public float CalculateTotalDistance()
    {
        if (pathPositions.Count < 2)
            return 0f;
        
        float totalDistance = 0f;
        for (int i = 0; i < pathPositions.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(pathPositions[i], pathPositions[i + 1]);
        }
        
        return totalDistance;
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        return new TimeSeriesDataPoint(timestamp, pathPositions.Count);
    }
    
    public override void Reset()
    {
        base.Reset();
        pathPositions.Clear();
        lastRecordedPosition = Vector3.zero;
        isInitialized = false;
    }
}
