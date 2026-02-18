using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Stat.Assessment
{
    /// <summary>
    /// Tracks assessment-specific metrics during expedition
    /// Delegates to PlayerStatsTrackerService for path and risk tracking
    /// </summary>
    public class AssessmentTracker : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerStatsTrackerService statsTracker;
        
        // Assessment data
        private PerformanceMetrics currentMetrics;
        
        /// <summary>
        /// Gets current performance metrics collected during expedition
        /// </summary>
        /// <returns>Complete performance metrics</returns>
        public PerformanceMetrics GetCurrentMetrics()
        {
            if (statsTracker == null)
            {
                Debug.LogError("[AssessmentTracker] PlayerStatsTrackerService reference is missing!");
                return currentMetrics ?? new PerformanceMetrics();
            }
            
            currentMetrics = new PerformanceMetrics();
            
            // Collect metrics from PlayerStatsTrackerService
            currentMetrics.totalStaminaUsed = statsTracker.GetStaminaUsed();
            currentMetrics.totalDistance = statsTracker.GetDistanceWalked();
            currentMetrics.totalTime = statsTracker.SessionDuration;
            currentMetrics.totalHealthLost = statsTracker.GetHealthLost();
            
            // Collect from trackers
            var pathTracker = statsTracker.GetPathTracker();
            var riskTracker = statsTracker.GetRiskTracker();
            
            if (pathTracker != null)
            {
                currentMetrics.pathTaken = pathTracker.PathPositions;
            }
            
            if (riskTracker != null)
            {
                var riskStats = riskTracker.GetRiskStats();
                currentMetrics.totalRiskyEvents = riskStats.total;
                currentMetrics.encounterredRisks = riskStats.encountered;
            }
            
            // Get food and water consumption from consumables
            currentMetrics.totalFoodItemsConsumed = statsTracker.GetFoodItemsConsumed();
            currentMetrics.totalWaterItemsConsumed = statsTracker.GetWaterItemsConsumed();
            
            // Get health loss incidents count
            currentMetrics.healthLossIncidents = statsTracker.GetHealthLossIncidents();
            
            return currentMetrics;
        }
        
        /// <summary>
        /// Gets the recorded player path
        /// </summary>
        public List<Vector3> GetPlayerPath()
        {
            var pathTracker = statsTracker?.GetPathTracker();
            return pathTracker?.PathPositions ?? new List<Vector3>();
        }
        
        /// <summary>
        /// Gets all risk events that were encountered
        /// </summary>
        public List<RiskEvent> GetRiskEvents()
        {
            var riskTracker = statsTracker?.GetRiskTracker();
            return riskTracker?.EncounteredRisks ?? new List<RiskEvent>();
        }
        
        /// <summary>
        /// Gets all risk events (both encountered and avoided)
        /// </summary>
        public List<RiskEvent> GetAllRiskEvents()
        {
            var riskTracker = statsTracker?.GetRiskTracker();
            return riskTracker?.AllRiskEvents ?? new List<RiskEvent>();
        }
        
        /// <summary>
        /// Gets risk statistics
        /// </summary>
        public (int total, int encountered, int avoided) GetRiskStats()
        {
            var riskTracker = statsTracker?.GetRiskTracker();
            if (riskTracker != null)
            {
                var stats = riskTracker.GetRiskStats();
                return (stats.total, stats.encountered, stats.avoided);
            }
            return (0, 0, 0);
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Debug visualization of player path and risk events
        /// </summary>
        private void OnDrawGizmos()
        {
            /*if (statsTracker == null)
                return;
            
            var pathTracker = statsTracker.GetPathTracker();
            var riskTracker = statsTracker.GetRiskTracker();
            
            if (pathTracker == null || riskTracker == null)
                return;
            
            var path = pathTracker.PathPositions;
            if (path.Count >= 2)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Gizmos.DrawLine(path[i], path[i + 1]);
                }
            }
            
            // Draw risk event locations
            var encounterredRisks = riskTracker.EncounteredRisks;
            foreach (var risk in encounterredRisks)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(risk.location, 1f);
            }
            
            // Draw avoided risks
            var avoidedRisks = riskTracker.AvoidedRisks;
            foreach (var risk in avoidedRisks)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(risk.location, 0.7f);
            }*/
        }
#endif
    }
}
