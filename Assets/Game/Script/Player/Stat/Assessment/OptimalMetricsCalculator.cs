using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Calculates optimal expected values based on terrain and conditions
    /// Uses terrain analysis and pathfinding data
    /// Can also accept pre-calculated values from external planning modules
    /// </summary>
    public class OptimalMetricsCalculator
    {
        // Base consumption rates (per meter)
        private const float BASE_STAMINA_PER_METER = 0.5f;
        private const float BASE_FOOD_ITEMS_PER_KM = 2f;    // ~2 food items per kilometer
        private const float BASE_WATER_ITEMS_PER_KM = 3f;   // ~3 water items per kilometer
        
        // Movement speed (meters per second)
        private const float BASE_MOVE_SPEED = 2.5f;
        
        /// <summary>
        /// Calculates optimal metrics for a given path (automatic calculation)
        /// </summary>
        public OptimalMetrics Calculate(List<Vector3> optimalPath)
        {
            float distance = CalculatePathDistance(optimalPath);
            float distanceKm = distance / 1000f; // Convert to kilometers
            
            return new OptimalMetrics
            {
                expectedStamina = distance * BASE_STAMINA_PER_METER,
                expectedFoodItems = Mathf.CeilToInt(distanceKm * BASE_FOOD_ITEMS_PER_KM),
                expectedWaterItems = Mathf.CeilToInt(distanceKm * BASE_WATER_ITEMS_PER_KM),
                optimalDistance = distance,
                optimalTime = distance / BASE_MOVE_SPEED
            };
        }
        
        /// <summary>
        /// Creates optimal metrics from externally provided values
        /// Use this when planning module has already calculated optimal values
        /// </summary>
        public OptimalMetrics CreateFromValues(
            float expectedStamina,
            int expectedFoodItems,
            int expectedWaterItems,
            float optimalDistance,
            float optimalTime)
        {
            return new OptimalMetrics
            {
                expectedStamina = expectedStamina,
                expectedFoodItems = expectedFoodItems,
                expectedWaterItems = expectedWaterItems,
                optimalDistance = optimalDistance,
                optimalTime = optimalTime
            };
        }
        
        private float CalculatePathDistance(List<Vector3> path)
        {
            float totalDistance = 0f;
            for (int i = 0; i < path.Count - 1; i++)
            {
                totalDistance += Vector3.Distance(path[i], path[i + 1]);
            }
            return totalDistance;
        }
    }
}
