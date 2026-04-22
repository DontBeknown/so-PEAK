using System;

/// <summary>
/// Contains calculated scores for each assessment category
/// </summary>
[Serializable]
public class AssessmentScore
{
    // Individual Scores (0-100)
    public float efficiencyScore;
    public float safetyScore;
    public float planningScore;
    
    // Weighted Final Score (0-100)
    public float totalScore;
    
    // Performance Rank
    public PerformanceRank rank;
    
    // Detailed Breakdown
    public EfficiencyBreakdown efficiencyDetails;
    public SafetyBreakdown safetyDetails;
    public PlanningBreakdown planningDetails;

    // Optimal baselines used during scoring (for display purposes)
    public OptimalMetrics optimalMetrics;
    public PerformanceMetrics rawMetrics;

    // True when planning was scored against the player's own path (no HJB path available)
    public bool planningUsedFallbackPath;
}
