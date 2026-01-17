using System;

/// <summary>
/// Detailed breakdown of safety performance
/// </summary>
[Serializable]
public class SafetyBreakdown
{
    public int risksAvoided;
    public int risksEncountered;
    public float avoidanceRate;          // Percentage
    public float healthLossScore;        // Penalty for health loss
    public string feedback;
}
