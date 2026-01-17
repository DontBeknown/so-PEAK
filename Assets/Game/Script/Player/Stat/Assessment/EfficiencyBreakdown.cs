using System;

/// <summary>
/// Detailed breakdown of efficiency performance
/// </summary>
[Serializable]
public class EfficiencyBreakdown
{
    public float staminaEfficiency;      // 0-100
    public float foodEfficiency;         // 0-100
    public float waterEfficiency;        // 0-100
    public float resourceUsageRatio;     // actual/optimal ratio
    public string feedback;              // Text feedback
}
