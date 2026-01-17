using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Interface for assessment calculation strategies
    /// Allows different calculation methods (e.g., for different difficulty modes)
    /// </summary>
    public interface IAssessmentCalculator
    {
        float CalculateEfficiencyScore(PerformanceMetrics metrics, OptimalMetrics optimal);
        float CalculateSafetyScore(PerformanceMetrics metrics);
        float CalculatePlanningScore(PerformanceMetrics metrics, OptimalMetrics optimal);
    }
}
