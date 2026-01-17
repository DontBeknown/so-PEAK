using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Example integration for expedition management
    /// Shows how to use the Learning Assessment System
    /// </summary>
    public class ExpeditionIntegrationExample : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerStatsTrackerService statsTracker;
        [SerializeField] private LearningAssessmentService assessmentService;
        
        [Header("Optional: Route Planning Module")]
        [SerializeField] private bool useExternalOptimalMetrics = false;
        
        /// <summary>
        /// Call this when expedition/level starts
        /// </summary>
        public void StartExpedition()
        {
            // Reset and start tracking
            statsTracker.ResetTracking();
            statsTracker.StartTracking();
            
            // Optional: Set optimal metrics from your planning module
            if (useExternalOptimalMetrics)
            {
                // Example: Get from your route planning system
                assessmentService.SetOptimalMetrics(
                    expectedStamina: 1000f,
                    expectedFoodItems: 5,
                    expectedWaterItems: 8,
                    optimalDistance: 500f,
                    optimalTime: 300f
                );
            }
            else
            {
                // Clear any cached optimal metrics (auto-calculate)
                assessmentService.ClearOptimalMetrics();
            }
            
            Debug.Log("[Expedition] Started expedition with assessment tracking");
        }
        
        /// <summary>
        /// Call this when expedition ends (summit reached, returned to base, etc.)
        /// </summary>
        public void EndExpedition()
        {
            // Stop tracking
            statsTracker.StopTracking();
            
            // Generate assessment
            AssessmentScore score = assessmentService.GenerateAssessment();
            
            if (score != null)
            {
                // Log results
                Debug.Log($"[Expedition] Assessment Complete!");
                Debug.Log($"  Total Score: {score.totalScore:F1}/100");
                Debug.Log($"  Rank: {score.rank}");
                Debug.Log($"  Efficiency: {score.efficiencyScore:F1}");
                Debug.Log($"  Safety: {score.safetyScore:F1}");
                Debug.Log($"  Planning: {score.planningScore:F1}");
                
                // Display UI (your UI system handles this via OnAssessmentComplete event)
                // Save to profile, update leaderboards, etc.
                SaveAssessmentToProfile(score);
            }
        }
        
        /// <summary>
        /// Example: Register a risk event from your hazard detection system
        /// </summary>
        public void OnHazardDetected(Vector3 hazardLocation, RiskType riskType, bool playerEncountered)
        {
            RiskEvent riskEvent = new RiskEvent
            {
                riskType = riskType,
                location = hazardLocation,
                timestamp = Time.time,
                wasEncountered = playerEncountered,
                severity = 0.7f
            };
            
            statsTracker.RegisterRiskEvent(riskEvent);
        }
        
        /// <summary>
        /// Example: Save assessment to player profile
        /// </summary>
        private void SaveAssessmentToProfile(AssessmentScore score)
        {
            // Your save system implementation
            // PlayerProfile.AddAssessment(score);
            // PlayerProfile.UpdateBestScore(score.totalScore);
            // LeaderboardManager.SubmitScore(score);
            
            Debug.Log($"[Expedition] Assessment saved to profile");
        }
        
        /// <summary>
        /// Subscribe to assessment completion event
        /// </summary>
        private void OnEnable()
        {
            if (assessmentService != null)
            {
                assessmentService.OnAssessmentComplete += HandleAssessmentComplete;
            }
        }
        
        private void OnDisable()
        {
            if (assessmentService != null)
            {
                assessmentService.OnAssessmentComplete -= HandleAssessmentComplete;
            }
        }
        
        /// <summary>
        /// Handle assessment completion (trigger UI, etc.)
        /// </summary>
        private void HandleAssessmentComplete(AssessmentScore score)
        {
            // Show assessment UI
            // AssessmentUI.Show(score);
            
            // Play feedback sounds/effects based on rank
            // AudioManager.PlayRankSound(score.rank);
            
            // Unlock achievements
            // CheckAchievements(score);
            
            Debug.Log($"[Expedition] Assessment UI triggered for rank: {score.rank}");
        }
    }
}
