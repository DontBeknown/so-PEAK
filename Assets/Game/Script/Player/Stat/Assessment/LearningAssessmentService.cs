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
        private bool lastAssessmentUsedFallbackPath;
        
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
                planningDetails = CreatePlanningBreakdown(metrics, optimal, planningScore),
                optimalMetrics = optimal,
                rawMetrics = metrics,
                planningUsedFallbackPath = lastAssessmentUsedFallbackPath
            };

            if (assessment.optimalMetrics != null && assessment.optimalMetrics.optimalTime > 0.01f)
            {
                float timeDeltaPercent = ((metrics.totalTime - assessment.optimalMetrics.optimalTime) / assessment.optimalMetrics.optimalTime) * 100f;
                Debug.Log($"[LearningAssessment] Time comparison - actual:{metrics.totalTime:F1}s optimal:{assessment.optimalMetrics.optimalTime:F1}s delta:{timeDeltaPercent:+0.0;-0.0;0.0}%");
            }
            
            Debug.Log($"[LearningAssessment] Assessment complete! Score: {totalScore:F1}, Rank: {assessment.rank}");
            
            OnAssessmentComplete?.Invoke(assessment);
            
            return assessment;
        }
        
        /// <summary>
        /// Gets optimal metrics (from cache or calculates automatically)
        /// </summary>
        private OptimalMetrics GetOptimalMetrics(PerformanceMetrics metrics)
        {
            lastAssessmentUsedFallbackPath = false;
            PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
            PlayerConfig playerConfig = playerStats != null ? playerStats.Config : null;

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
                    return optimalCalculator.Calculate(cachedPath, playerConfig);
                }

                Debug.LogWarning($"[LearningAssessment] No cached HJB path found for level {saveLoadService.GetCurrentLevel()}");
            }

            // Fall back to the tracked player path — planning score will be unreliable.
            if (metrics.pathTaken != null && metrics.pathTaken.Count >= 2)
            {
                lastAssessmentUsedFallbackPath = true;
                Debug.LogWarning("[LearningAssessment] Falling back to player path for optimal metrics — planning score unreliable");
                return optimalCalculator.Calculate(metrics.pathTaken, playerConfig);
            }

            lastAssessmentUsedFallbackPath = true;
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
            
            string feedback = avgRatio <= 1.1f ? "Excellent! Resources used very efficiently." :
                             avgRatio <= 1.3f ? "Good, but there is room for improvement." :
                             avgRatio <= 1.6f ? "Too many resources consumed. Plan your route more carefully." :
                             "Resource usage was very wasteful. Significant improvement needed.";
            
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
            
            string feedback = safetyScore >= 90f ? "Very safe — hazards avoided excellently." :
                             safetyScore >= 70f ? "Safe, but some risks were encountered." :
                             safetyScore >= 50f ? "Risky — be more cautious on the next climb." :
                             "Dangerous! Too many hazard events encountered.";

            if (metrics.deathCount > 0)
            {
                feedback += $" ({metrics.deathCount} death{(metrics.deathCount > 1 ? "s" : "")} recorded.)";
            }
            
            float healthLossPenalty = Mathf.Min(metrics.totalHealthLost * 0.5f, 50f);

            return new SafetyBreakdown
            {
                risksAvoided = risksAvoided,
                risksEncountered = metrics.encounterredRisks,
                avoidanceRate = avoidanceRate,
                healthLossScore = healthLossPenalty,
                deathCount = metrics.deathCount,
                deathPenaltyScore = metrics.deathCount * DEATH_PENALTY_PER_DEATH,
                healthLossIncidents = metrics.healthLossIncidents,
                totalHealthLost = metrics.totalHealthLost,
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
            
            float blendedDeviation = (distanceDeviation + timeDeviation) * 0.5f;
            string feedback = blendedDeviation <= 10f ? "Excellent planning! Route and time were near-optimal." :
                             blendedDeviation <= 25f ? "Good planning, but a better route or pace exists." :
                             blendedDeviation <= 40f ? "Adequate planning. Try to follow the suggested path more closely." :
                             "Poor planning. Route choice and timing need significant improvement.";
            
            return new PlanningBreakdown
            {
                pathDeviation = distanceDeviation,
                timeEfficiency = Mathf.Clamp(100f - timeDeviation, 0f, 100f),
                routeOptimality = planningScore,
                feedback = feedback
            };
        }

        /// <summary>
        /// Returns an actionable improvement tip for efficiency based on the score.
        /// </summary>
        public static string GetEfficiencyTip(float score)
        {
            if (score >= 90f) return "Keep it up — your resource management is excellent.";
            if (score >= 70f) return "Next time: pace yourself on steep sections to reduce stamina drain.";
            if (score >= 50f) return "Next time: bring fewer supplies and stick to flatter terrain to save stamina.";
            return "Next time: plan your load carefully — you over-consumed food, water, and stamina significantly.";
        }

        /// <summary>
        /// Returns an actionable improvement tip for safety based on the score.
        /// </summary>
        public static string GetSafetyTip(float score)
        {
            if (score >= 90f) return "Great awareness — keep monitoring the risk map as you climb.";
            if (score >= 70f) return "Next time: check the risk map before each segment to avoid the highlighted hazard zones.";
            if (score >= 50f) return "Next time: slow down near red zones on the map and take detours around known hazards.";
            return "Next time: prioritize safety over speed — review the risk map frequently and avoid all red-zone areas.";
        }

        /// <summary>
        /// Returns an actionable improvement tip for planning based on the score.
        /// </summary>
        public static string GetPlanningTip(float score)
        {
            if (score >= 90f) return "Excellent route choice — you followed near-optimal path and timing.";
            if (score >= 70f) return "Next time: follow the suggested HJB path more closely to shorten your route.";
            if (score >= 50f) return "Next time: check the path overlay before you start and aim to finish closer to the estimated time.";
            return "Next time: use the suggested path — your route and timing deviated significantly from optimal.";
        }
    }
}
