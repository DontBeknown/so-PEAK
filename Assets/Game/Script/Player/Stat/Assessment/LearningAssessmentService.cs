using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Main service that coordinates the entire assessment system
    /// Called when expedition ends to generate performance assessment
    /// </summary>
    public class LearningAssessmentService : MonoBehaviour
    {
        private const float DEATH_PENALTY_PER_DEATH = 10f;

        [Header("Dependencies")]
        [SerializeField] private PlayerStatsTrackerService statsTracker;
        [SerializeField] private AssessmentTracker assessmentTracker;

        [Header("Calculator")]
        private IAssessmentCalculator calculator;
        private OptimalMetricsCalculator optimalCalculator;

        // Returns the assigned tracker or searches the scene as a fallback (handles late-spawned players).
        private AssessmentTracker AssessmentTrackerRef =>
            assessmentTracker != null ? assessmentTracker : FindFirstObjectByType<AssessmentTracker>();
        
        // Cached optimal metrics (can be set externally)
        private OptimalMetrics cachedOptimalMetrics;
        private bool hasExternalOptimalMetrics;
        
        // Events
        public event System.Action<AssessmentScore> OnAssessmentComplete;
        
        private void Awake()
        {
            calculator = new StandardAssessmentCalculator();
            optimalCalculator = new OptimalMetricsCalculator();
            hasExternalOptimalMetrics = false;
        }
        
        /// <summary>
        /// Set optimal metrics from external planning module
        /// Call this before GenerateAssessment() if you have pre-calculated optimal values
        /// </summary>
        public void SetOptimalMetrics(OptimalMetrics optimalMetrics)
        {
            cachedOptimalMetrics = optimalMetrics;
            hasExternalOptimalMetrics = true;
            Debug.Log("[LearningAssessment] External optimal metrics set");
        }
        
        /// <summary>
        /// Set optimal metrics from individual values
        /// </summary>
        public void SetOptimalMetrics(
            float expectedStamina,
            int expectedFoodItems,
            int expectedWaterItems,
            float optimalDistance,
            float optimalTime)
        {
            cachedOptimalMetrics = optimalCalculator.CreateFromValues(
                expectedStamina,
                expectedFoodItems,
                expectedWaterItems,
                optimalDistance,
                optimalTime
            );
            hasExternalOptimalMetrics = true;
            Debug.Log("[LearningAssessment] External optimal metrics set from values");
        }
        
        /// <summary>
        /// Returns cumulative assessment metrics as a save payload.
        /// Called by SaveLoadService during save.
        /// </summary>
        public AssessmentSaveData GetSaveData()
        {
            var tracker = AssessmentTrackerRef;
            return tracker != null ? tracker.GetSaveData() : new AssessmentSaveData();
        }

        public int GetDeathCount()
        {
            return AssessmentTrackerRef != null ? AssessmentTrackerRef.DeathCount : 0;
        }

        /// <summary>
        /// Restores cumulative baseline into the tracker from a previous session's save data.
        /// Called by SaveLoadService after world load.
        /// </summary>
        public void RestoreFromSaveData(AssessmentSaveData data)
        {
            AssessmentTrackerRef?.LoadBaseline(data);
        }

        /// <summary>
        /// Clear cached optimal metrics (forces recalculation on next assessment)
        /// </summary>
        public void ClearOptimalMetrics()
        {
            hasExternalOptimalMetrics = false;
            cachedOptimalMetrics = null;
            Debug.Log("[LearningAssessment] Optimal metrics cleared");
        }
        
        /// <summary>
        /// Generate assessment report when expedition ends
        /// </summary>
        public AssessmentScore GenerateAssessment()
        {
            var tracker = AssessmentTrackerRef;
            if (tracker == null)
            {
                Debug.LogError("[LearningAssessment] AssessmentTracker reference is missing!");
                return null;
            }

            // Get performance metrics
            PerformanceMetrics metrics = tracker.GetCurrentMetrics();
            
            // Get or calculate optimal metrics
            OptimalMetrics optimal = GetOptimalMetrics(metrics);
            
            // Calculate scores
            float efficiencyScore = calculator.CalculateEfficiencyScore(metrics, optimal);
            float safetyScore = calculator.CalculateSafetyScore(metrics);
            float planningScore = calculator.CalculatePlanningScore(metrics, optimal);
            
            // Calculate weighted total
            float totalScore = 
                (efficiencyScore * 0.4f) + 
                (safetyScore * 0.3f) + 
                (planningScore * 0.3f);
            
            // Create assessment result
            AssessmentScore assessment = new AssessmentScore
            {
                efficiencyScore = efficiencyScore,
                safetyScore = safetyScore,
                planningScore = planningScore,
                totalScore = totalScore,
                rank = DetermineRank(totalScore),
                efficiencyDetails = CreateEfficiencyBreakdown(metrics, optimal, efficiencyScore),
                safetyDetails = CreateSafetyBreakdown(metrics, safetyScore),
                planningDetails = CreatePlanningBreakdown(metrics, optimal, planningScore)
            };
            
            Debug.Log($"[LearningAssessment] Assessment complete! Score: {totalScore:F1}, Rank: {assessment.rank}");
            
            OnAssessmentComplete?.Invoke(assessment);
            
            return assessment;
        }
        
        /// <summary>
        /// Gets optimal metrics (from cache or calculates automatically)
        /// </summary>
        private OptimalMetrics GetOptimalMetrics(PerformanceMetrics metrics)
        {
            if (hasExternalOptimalMetrics)
            {
                Debug.Log("[LearningAssessment] Using externally provided optimal metrics");
                return cachedOptimalMetrics;
            }

            var saveLoadService = SaveLoadService.Instance;
            if (saveLoadService != null)
            {
                var cachedPath = saveLoadService.GetCachedPathForCurrentLevel();
                if (cachedPath != null && cachedPath.Count >= 2)
                {
                    Debug.Log($"[LearningAssessment] Using cached HJB path from save for level {saveLoadService.GetCurrentLevel()}");
                    return optimalCalculator.Calculate(cachedPath);
                }

                Debug.LogWarning($"[LearningAssessment] No cached HJB path found for level {saveLoadService.GetCurrentLevel()}");
            }
            
            // Fall back to the tracked player path if the save has no cached HJB path yet.
            if (metrics.pathTaken != null && metrics.pathTaken.Count >= 2)
            {
                Debug.Log("[LearningAssessment] Calculating optimal metrics from player path");
                return optimalCalculator.Calculate(metrics.pathTaken);
            }
            
            // Fallback: use actual metrics as "optimal" (results in 100% efficiency)
            Debug.LogWarning("[LearningAssessment] No path data available, using actual metrics as baseline");
            return optimalCalculator.CreateFromValues(
                metrics.totalStaminaUsed,
                metrics.totalFoodItemsConsumed,
                metrics.totalWaterItemsConsumed,
                metrics.totalDistance,
                metrics.totalTime
            );
        }
        
        /// <summary>
        /// Determines performance rank based on total score
        /// </summary>
        private PerformanceRank DetermineRank(float totalScore)
        {
            if (totalScore >= 90f) return PerformanceRank.AlpineMaster;
            if (totalScore >= 70f) return PerformanceRank.SkilledPlanner;
            if (totalScore >= 50f) return PerformanceRank.Survivor;
            return PerformanceRank.LostWanderer;
        }
        
        /// <summary>
        /// Creates detailed efficiency breakdown
        /// </summary>
        private EfficiencyBreakdown CreateEfficiencyBreakdown(
            PerformanceMetrics metrics, 
            OptimalMetrics optimal,
            float efficiencyScore)
        {
            float staminaRatio = optimal.expectedStamina > 0 
                ? metrics.totalStaminaUsed / optimal.expectedStamina 
                : 1f;
            
            float foodRatio = optimal.expectedFoodItems > 0
                ? (float)metrics.totalFoodItemsConsumed / optimal.expectedFoodItems
                : 1f;
            
            float waterRatio = optimal.expectedWaterItems > 0
                ? (float)metrics.totalWaterItemsConsumed / optimal.expectedWaterItems
                : 1f;
            
            float avgRatio = (staminaRatio + foodRatio + waterRatio) / 3f;
            
            string feedback = avgRatio <= 1.1f ? "ยอดเยี่ยม! ใช้ทรัพยากรอย่างมีประสิทธิภาพ" :
                             avgRatio <= 1.3f ? "ดี แต่ยังสามารถปรับปรุงได้" :
                             avgRatio <= 1.6f ? "ใช้ทรัพยากรมากเกินไป ควรวางแผนให้ดีขึ้น" :
                             "ใช้ทรัพยากรสิ้นเปลืองมาก จำเป็นต้องปรับปรุง";
            
            return new EfficiencyBreakdown
            {
                staminaEfficiency = Mathf.Clamp(100f - ((staminaRatio - 1f) * 100f), 0f, 100f),
                foodEfficiency = Mathf.Clamp(100f - ((foodRatio - 1f) * 100f), 0f, 100f),
                waterEfficiency = Mathf.Clamp(100f - ((waterRatio - 1f) * 100f), 0f, 100f),
                resourceUsageRatio = avgRatio,
                feedback = feedback
            };
        }
        
        /// <summary>
        /// Creates detailed safety breakdown
        /// </summary>
        private SafetyBreakdown CreateSafetyBreakdown(PerformanceMetrics metrics, float safetyScore)
        {
            float avoidanceRate = metrics.totalRiskyEvents > 0 ?
                (1f - (float)metrics.encounterredRisks / metrics.totalRiskyEvents) * 100f : 100f;
            
            int risksAvoided = metrics.totalRiskyEvents - metrics.encounterredRisks;
            
            string feedback = avoidanceRate >= 90f ? "ปลอดภัยมาก หลีกเลี่ยงอันตรายได้ดีเยี่ยม" :
                             avoidanceRate >= 70f ? "ปลอดภัย แต่ยังมีความเสี่ยงบางส่วน" :
                             avoidanceRate >= 50f ? "เสี่ยงค่อนข้างมาก ควรระมัดระวังมากขึ้น" :
                             "อันตราย! พบเหตุการณ์เสี่ยงมากเกินไป";

            if (metrics.deathCount > 0)
            {
                feedback += $" (เสียชีวิต {metrics.deathCount} ครั้ง)";
            }
            
            return new SafetyBreakdown
            {
                risksAvoided = risksAvoided,
                risksEncountered = metrics.encounterredRisks,
                avoidanceRate = avoidanceRate,
                healthLossScore = safetyScore,
                deathCount = metrics.deathCount,
                deathPenaltyScore = metrics.deathCount * DEATH_PENALTY_PER_DEATH,
                feedback = feedback
            };
        }
        
        /// <summary>
        /// Creates detailed planning breakdown
        /// </summary>
        private PlanningBreakdown CreatePlanningBreakdown(
            PerformanceMetrics metrics, 
            OptimalMetrics optimal,
            float planningScore)
        {
            float distanceDeviation = optimal.optimalDistance > 0
                ? Mathf.Abs((metrics.totalDistance - optimal.optimalDistance) / optimal.optimalDistance) * 100f
                : 0f;
            
            float timeDeviation = optimal.optimalTime > 0
                ? Mathf.Abs((metrics.totalTime - optimal.optimalTime) / optimal.optimalTime) * 100f
                : 0f;
            
            string feedback = distanceDeviation <= 10f ? "วางแผนเยี่ยม! เลือกเส้นทางที่เหมาะสมมาก" :
                             distanceDeviation <= 25f ? "วางแผนดี แต่ยังมีเส้นทางที่ดีกว่า" :
                             distanceDeviation <= 40f ? "วางแผนพอใช้ ควรเลือกเส้นทางที่เหมาะสมกว่า" :
                             "วางแผนไม่ดี เลือกเส้นทางที่ไม่เหมาะสม";
            
            return new PlanningBreakdown
            {
                pathDeviation = distanceDeviation,
                timeEfficiency = Mathf.Clamp(100f - timeDeviation, 0f, 100f),
                routeOptimality = planningScore,
                feedback = feedback
            };
        }
    }
}
