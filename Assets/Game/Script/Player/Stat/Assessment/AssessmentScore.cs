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
}
