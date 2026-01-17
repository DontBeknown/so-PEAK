using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tracks risk events during expedition (both encountered and avoided).
/// SRP: Only responsible for risk event recording.
/// </summary>
public class RiskTracker : BaseStatTracker<RiskEvent>
{
    private readonly List<RiskEvent> allRiskEvents;
    private int totalPossibleRisks;
    private int risksEncountered;
    
    public override string MetricName => "Risk Events";
    
    public override RiskEvent CurrentValue 
    { 
        get => allRiskEvents.Count > 0 ? allRiskEvents[allRiskEvents.Count - 1] : null;
        set { } // Not used for risk tracking
    }
    
    /// <summary>
    /// Gets all recorded risk events (both encountered and avoided)
    /// </summary>
    public List<RiskEvent> AllRiskEvents => new List<RiskEvent>(allRiskEvents);
    
    /// <summary>
    /// Gets only risk events that were encountered
    /// </summary>
    public List<RiskEvent> EncounteredRisks => allRiskEvents.Where(r => r.wasEncountered).ToList();
    
    /// <summary>
    /// Gets only risk events that were avoided
    /// </summary>
    public List<RiskEvent> AvoidedRisks => allRiskEvents.Where(r => !r.wasEncountered).ToList();
    
    /// <summary>
    /// Total number of possible risk situations
    /// </summary>
    public int TotalPossibleRisks => totalPossibleRisks;
    
    /// <summary>
    /// Number of risks that were encountered (not avoided)
    /// </summary>
    public int RisksEncountered => risksEncountered;
    
    /// <summary>
    /// Number of risks that were successfully avoided
    /// </summary>
    public int RisksAvoided => totalPossibleRisks - risksEncountered;
    
    /// <summary>
    /// Risk avoidance rate (0-1, higher is better)
    /// </summary>
    public float AvoidanceRate => totalPossibleRisks > 0 ? 
        (float)RisksAvoided / totalPossibleRisks : 1f;
    
    public RiskTracker(int maxDataPoints = 500) : base(maxDataPoints)
    {
        allRiskEvents = new List<RiskEvent>();
        totalPossibleRisks = 0;
        risksEncountered = 0;
    }
    
    public override void RecordValue(RiskEvent riskEvent)
    {
        if (riskEvent == null)
            return;
        
        allRiskEvents.Add(riskEvent);
        totalPossibleRisks++;
        
        if (riskEvent.wasEncountered)
        {
            risksEncountered++;
        }
    }
    
    /// <summary>
    /// Gets risk events of a specific type
    /// </summary>
    public List<RiskEvent> GetRisksByType(RiskType riskType)
    {
        return allRiskEvents.Where(r => r.riskType == riskType).ToList();
    }
    
    /// <summary>
    /// Gets risk events within a time range
    /// </summary>
    public List<RiskEvent> GetRisksInTimeRange(float startTime, float endTime)
    {
        return allRiskEvents.Where(r => r.timestamp >= startTime && r.timestamp <= endTime).ToList();
    }
    
    /// <summary>
    /// Gets risk statistics summary
    /// </summary>
    public (int total, int encountered, int avoided, float avoidanceRate) GetRiskStats()
    {
        return (totalPossibleRisks, risksEncountered, RisksAvoided, AvoidanceRate);
    }
    
    /// <summary>
    /// Gets count of risks by severity threshold
    /// </summary>
    public int GetHighSeverityRisksCount(float minSeverity = 0.7f)
    {
        return allRiskEvents.Count(r => r.severity >= minSeverity);
    }
    
    protected override TimeSeriesDataPoint CreateDataPoint(float timestamp)
    {
        return new TimeSeriesDataPoint(timestamp, risksEncountered);
    }
    
    public override void Reset()
    {
        base.Reset();
        allRiskEvents.Clear();
        totalPossibleRisks = 0;
        risksEncountered = 0;
    }
}
