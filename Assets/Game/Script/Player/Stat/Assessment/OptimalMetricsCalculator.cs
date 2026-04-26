using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Calculates optimal expected values based on terrain and conditions.
    /// Uses segment-by-segment traversal simulation with Tobler slope effects and
    /// cumulative fatigue, matching the movement model in WalkingState.
    /// Can also accept pre-calculated values from external planning modules.
    /// </summary>
    public class OptimalMetricsCalculator
    {
        // Resource consumption rates (per km) — food/water unchanged this iteration
        private const float BASE_FOOD_ITEMS_PER_KM = 2f;
        private const float BASE_WATER_ITEMS_PER_KM = 3f;

        // Fallback constants used when config is null or path is too sparse
        private const float FALLBACK_STAMINA_PER_METER = 0.5f;
        private const float FALLBACK_MOVE_SPEED = 3f;

        // Defaults matching PlayerConfig inspector values
        private const float DEFAULT_WALK_SPEED              = 3f;
        private const float DEFAULT_MIN_SLOPE_MULT          = 0.7f;
        private const float DEFAULT_MAX_SLOPE_MULT          = 1f;
        private const float DEFAULT_FATIGUE_SPEED_THRESHOLD = 70f;
        private const float DEFAULT_MAX_FATIGUE             = 100f;
        private const float DEFAULT_FATIGUE_RATE_TIME       = 0.12f;
        private const float DEFAULT_FATIGUE_RATE_ELEV       = 0.0005f;
        private const float DEFAULT_STAMINA_DRAIN           = 0.5f;

        // Pre-computed Tobler flat-ground reference: exp(-3.5 * 0.05) ≈ 0.839
        private static readonly float FlatGroundTobler = Mathf.Exp(-3.5f * 0.05f);

        /// <summary>
        /// Calculates optimal metrics for a given path using segment-level simulation.
        /// Pass a PlayerConfig to use live tuning values; omit to use defaults.
        /// Falls back to distance-only estimation when the path has fewer than 2 points.
        /// </summary>
        public OptimalMetrics Calculate(List<Vector3> optimalPath, PlayerConfig config = null)
        {
            if (optimalPath == null || optimalPath.Count < 2)
                return FallbackMetrics(optimalPath);

            float walkSpeed              = config != null ? config.baseWalkSpeed              : DEFAULT_WALK_SPEED;
            float minSlopeMult           = config != null ? config.minSlopeSpeedMultiplier     : DEFAULT_MIN_SLOPE_MULT;
            float maxSlopeMult           = config != null ? config.maxSlopeSpeedMultiplier     : DEFAULT_MAX_SLOPE_MULT;
            float fatigueSpeedThreshold  = config != null ? config.fatigueSpeedPenaltyThreshold: DEFAULT_FATIGUE_SPEED_THRESHOLD;
            float maxFatigue             = config != null ? config.maxFatigue                  : DEFAULT_MAX_FATIGUE;
            float fatigueRateTime        = config != null ? config.fatigueRateTime             : DEFAULT_FATIGUE_RATE_TIME;
            float fatigueRateElev        = config != null ? config.fatigueRateElev             : DEFAULT_FATIGUE_RATE_ELEV;
            float baseStaminaDrain       = config != null ? config.baseMovementStaminaDrain    : DEFAULT_STAMINA_DRAIN;

            float currentFatigue     = 0f;
            float totalTime          = 0f;
            float totalStaminaDrain  = 0f;
            float totalDistance      = 0f;

            for (int i = 0; i < optimalPath.Count - 1; i++)
            {
                Vector3 from = optimalPath[i];
                Vector3 to   = optimalPath[i + 1];

                float segDist = Vector3.Distance(from, to);
                if (segDist < 0.01f) continue;
                totalDistance += segDist;

                // Derive slope gradient from sampled ground normals to better match runtime movement.
                float slopeGradient = ComputeSlopeGradient(from, to, config);

                // Tobler's hiking function — same formula as WalkingState.CalculateSlopeEffects
                float toblerRaw  = Mathf.Exp(-3.5f * Mathf.Abs(slopeGradient + 0.05f));
                float toblerMult = Mathf.Lerp(minSlopeMult, maxSlopeMult, Mathf.Clamp01(toblerRaw / FlatGroundTobler));

                // Fatigue speed penalty at current accumulated fatigue
                float fatiguePenalty = SimulateFatigueSpeedPenalty(currentFatigue, fatigueSpeedThreshold, maxFatigue);

                // Combine reductions additively — mirrors WalkingState logic
                float totalReduction   = Mathf.Clamp01((1f - toblerMult) + (1f - fatiguePenalty));
                float combinedMult     = Mathf.Max(1f - totalReduction, 0.1f);

                float effectiveSpeed   = walkSpeed * combinedMult;
                float segTime          = segDist / effectiveSpeed;
                totalTime             += segTime;

                // Stamina drain: constant rate scaled by fatigue drain multiplier (1x–2x)
                float fatigueDrainMult  = 1f + (currentFatigue / maxFatigue);
                totalStaminaDrain      += baseStaminaDrain * fatigueDrainMult * segTime;

                // Accumulate fatigue — mirrors FatigueStat.Tick formula
                float fatigueGain = (fatigueRateTime + fatigueRateElev * Mathf.Abs(slopeGradient)) * segTime;
                currentFatigue    = Mathf.Min(currentFatigue + fatigueGain, maxFatigue);
            }

            float totalDistKm = totalDistance / 1000f;
            return new OptimalMetrics
            {
                expectedStamina    = totalStaminaDrain,
                expectedFoodItems  = Mathf.CeilToInt(totalDistKm * BASE_FOOD_ITEMS_PER_KM),
                expectedWaterItems = Mathf.CeilToInt(totalDistKm * BASE_WATER_ITEMS_PER_KM),
                optimalDistance    = totalDistance,
                optimalTime        = totalTime
            };
        }

        /// <summary>
        /// Creates optimal metrics from externally provided values.
        /// Use this when a planning module has already calculated optimal values.
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
                expectedStamina    = expectedStamina,
                expectedFoodItems  = expectedFoodItems,
                expectedWaterItems = expectedWaterItems,
                optimalDistance    = optimalDistance,
                optimalTime        = optimalTime
            };
        }

        // Mirrors FatigueStat.GetSpeedPenalty without requiring an instance
        private static float SimulateFatigueSpeedPenalty(float current, float threshold, float max)
        {
            if (current < threshold) return 1f;
            float excessNorm     = (current - threshold) / max;
            float additionalTime = excessNorm * 5f;
            return Mathf.Max(0.2f, 1f / (1f + additionalTime));
        }

        private OptimalMetrics FallbackMetrics(List<Vector3> path)
        {
            float dist    = CalculatePathDistance(path);
            float distKm  = dist / 1000f;
            return new OptimalMetrics
            {
                expectedStamina    = dist * FALLBACK_STAMINA_PER_METER,
                expectedFoodItems  = Mathf.CeilToInt(distKm * BASE_FOOD_ITEMS_PER_KM),
                expectedWaterItems = Mathf.CeilToInt(distKm * BASE_WATER_ITEMS_PER_KM),
                optimalDistance    = dist,
                optimalTime        = dist / FALLBACK_MOVE_SPEED
            };
        }

        private float CalculatePathDistance(List<Vector3> path)
        {
            if (path == null) return 0f;
            float total = 0f;
            for (int i = 0; i < path.Count - 1; i++)
                total += Vector3.Distance(path[i], path[i + 1]);
            return total;
        }

        private static float ComputeSlopeGradient(Vector3 from, Vector3 to, PlayerConfig config)
        {
            float horizontalDist = Mathf.Sqrt((to.x - from.x) * (to.x - from.x) + (to.z - from.z) * (to.z - from.z));
            if (horizontalDist <= 0.01f)
                return 0f;

            // If no config/layer info is available, keep the original waypoint-delta behavior.
            if (config == null)
                return (to.y - from.y) / horizontalDist;

            Vector3 direction = new Vector3(to.x - from.x, 0f, to.z - from.z).normalized;
            Vector3 normalSum = Vector3.zero;
            int hitCount = 0;

            const int samples = 4;
            for (int i = 0; i < samples; i++)
            {
                float t = samples == 1 ? 0f : (float)i / (samples - 1);
                Vector3 samplePos = Vector3.Lerp(from, to, t);
                Vector3 rayOrigin = samplePos + Vector3.up * 1.5f;

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 4f, config.groundLayer))
                {
                    normalSum += hit.normal;
                    hitCount++;
                }
            }

            if (hitCount == 0)
                return (to.y - from.y) / horizontalDist;

            Vector3 avgNormal = (normalSum / hitCount).normalized;
            float slopeAngle = Vector3.Angle(Vector3.up, avgNormal);
            float slopeGradient = Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

            bool isMovingUphill = Vector3.Dot(direction, avgNormal) < 0f;
            if (!isMovingUphill)
                slopeGradient = -slopeGradient;

            return slopeGradient;
        }
    }
}

