using System;

/// <summary>
/// Detailed breakdown of planning performance
/// </summary>
[Serializable]
public class PlanningBreakdown
{
    public float pathDeviation;          // Percentage deviation
    public float timeEfficiency;         // Time vs optimal
    public float routeOptimality;        // Overall route quality
    public string feedback;
}
