using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Default implementation of assessment calculation
    /// Uses formulas defined in Learning Assessment document
    /// </summary>
    public class StandardAssessmentCalculator : IAssessmentCalculator
    {
        // Weight constants (from design doc)
        private const float EFFICIENCY_WEIGHT = 0.4f;
        private const float SAFETY_WEIGHT = 0.3f;
        private const float PLANNING_WEIGHT = 0.3f;
        
        // Path cost weights
        private const float DISTANCE_WEIGHT = 0.5f;
        private const float TIME_WEIGHT = 0.3f;
        private const float STAMINA_WEIGHT = 0.2f;
        
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
            if (metrics.totalRiskyEvents == 0)
                return 100f;
            
            // Avoidance rate
            float avoidanceRate = 1f - ((float)metrics.encounterredRisks / metrics.totalRiskyEvents);
            
            // Base score from avoidance
            float baseScore = avoidanceRate * 100f;
            
            // Penalty for health loss (each incident reduces score)
            float healthPenalty = metrics.healthLossIncidents * 5f; // -5 points per incident
            
            float finalScore = baseScore - healthPenalty;
            
            return Mathf.Clamp(finalScore, 0f, 100f);
        }
        
        public float CalculatePlanningScore(PerformanceMetrics metrics, OptimalMetrics optimal)
        {
            // Calculate actual path cost
            float actualCost = CalculatePathCost(
                metrics.totalDistance,
                metrics.totalTime,
                metrics.totalStaminaUsed
            );
            
            // Calculate optimal path cost
            float optimalCost = CalculatePathCost(
                optimal.optimalDistance,
                optimal.optimalTime,
                optimal.expectedStamina
            );
            
            // Deviation percentage
            float deviation = Mathf.Abs((actualCost - optimalCost) / optimalCost);
            
            // Score: 100 - (deviation × 100)
            float planningScore = 100f - (deviation * 100f);
            
            return Mathf.Clamp(planningScore, 0f, 100f);
        }
        
        private float CalculatePathCost(float distance, float time, float stamina)
        {
            return (distance * DISTANCE_WEIGHT) + 
                   (time * TIME_WEIGHT) + 
                   (stamina * STAMINA_WEIGHT);
        }
    }
}
