using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Default implementation of assessment calculation
    /// Uses formulas defined in Learning Assessment document
    /// </summary>
    public class StandardAssessmentCalculator : IAssessmentCalculator
    {
        private const float DEATH_PENALTY_PER_DEATH = 10f;
        private const float PLANNING_TOLERANCE_PERCENT = 10f;
        
        public float CalculateEfficiencyScore(PerformanceMetrics metrics, OptimalMetrics optimal)
        {
            // Calculate resource usage efficiency
            float staminaRatio = metrics.totalStaminaUsed / optimal.expectedStamina;
            float foodRatio = (float)metrics.totalFoodItemsConsumed / Mathf.Max(optimal.expectedFoodItems, 1);
            float waterRatio = (float)metrics.totalWaterItemsConsumed / Mathf.Max(optimal.expectedWaterItems, 1);
            
            // Average ratio
            float avgRatio = (staminaRatio + foodRatio + waterRatio) / 3f;
            
            // Score formula: 100 - ((actual - optimal) / optimal × 100)
            float efficiency = 100f - ((avgRatio - 1f) * 100f);
            
            // Clamp to 0-100
            return Mathf.Clamp(efficiency, 0f, 100f);
        }
        
        public float CalculateSafetyScore(PerformanceMetrics metrics)
        {
            float avoidanceRate = metrics.totalRiskyEvents > 0
                ? 1f - ((float)metrics.encounterredRisks / metrics.totalRiskyEvents)
                : 1f;
            
            // Base score from avoidance
            float baseScore = avoidanceRate * 100f;
            
            // Penalty for health loss (each incident reduces score)
            float healthPenalty = metrics.healthLossIncidents * 5f; // -5 points per incident

            float deathPenalty = metrics.deathCount * DEATH_PENALTY_PER_DEATH;
            
            float finalScore = baseScore - healthPenalty - deathPenalty;
            
            return Mathf.Clamp(finalScore, 0f, 100f);
        }
        
        public float CalculatePlanningScore(PerformanceMetrics metrics, OptimalMetrics optimal)
        {
            float distanceScore = CalculateToleranceScore(metrics.totalDistance, optimal.optimalDistance);
            float timeScore = CalculateToleranceScore(metrics.totalTime, optimal.optimalTime);

            float planningScore = (distanceScore * 0.6f) + (timeScore * 0.4f);

            return Mathf.Clamp(planningScore, 0f, 100f);
        }
        
        private float CalculateToleranceScore(float actual, float optimal)
        {
            if (actual < optimal)
                return 100f; 
    
            if (optimal <= 0f)
                return 100f;

            float deviationPercent = Mathf.Abs((actual - optimal) / optimal) * 100f;
            if (deviationPercent <= PLANNING_TOLERANCE_PERCENT)
                return 100f;

            float excessPercent = deviationPercent - PLANNING_TOLERANCE_PERCENT;
            float normalizedExcess = excessPercent / Mathf.Max(100f - PLANNING_TOLERANCE_PERCENT, 0.0001f);

            return Mathf.Clamp(100f - (normalizedExcess * 100f), 0f, 100f);
        }
    }
}
