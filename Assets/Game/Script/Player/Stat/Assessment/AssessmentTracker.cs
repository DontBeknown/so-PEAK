using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;

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

        // Cumulative totals saved from previous sessions
        private AssessmentSaveData savedBaseline;

        // Returns the assigned tracker or resolves it from the ServiceContainer as a fallback.
        private PlayerStatsTrackerService StatsTracker =>
            statsTracker != null ? statsTracker : ServiceContainer.Instance.TryGet<PlayerStatsTrackerService>();
        
        /// <summary>
        /// Gets current performance metrics collected during expedition
        /// </summary>
        /// <returns>Complete performance metrics</returns>
        public PerformanceMetrics GetCurrentMetrics()
        {
            var tracker = StatsTracker;
            if (tracker == null)
            {
                Debug.LogError("[AssessmentTracker] PlayerStatsTrackerService reference is missing!");
                return currentMetrics ?? new PerformanceMetrics();
            }

            currentMetrics = new PerformanceMetrics();

            // Collect metrics from PlayerStatsTrackerService
            currentMetrics.totalStaminaUsed = tracker.GetStaminaUsed();
            currentMetrics.totalDistance = tracker.GetDistanceWalked();
            currentMetrics.totalTime = tracker.SessionDuration;
            currentMetrics.totalHealthLost = tracker.GetHealthLost();

            // Collect from trackers
            var pathTracker = tracker.GetPathTracker();
            var riskTracker = tracker.GetRiskTracker();

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
            currentMetrics.totalFoodItemsConsumed = tracker.GetFoodItemsConsumed();
            currentMetrics.totalWaterItemsConsumed = tracker.GetWaterItemsConsumed();

            // Get health loss incidents count
            currentMetrics.healthLossIncidents = tracker.GetHealthLossIncidents();

            // Add persisted baseline so cumulative tracking survives session exits
            if (savedBaseline != null)
            {
                currentMetrics.totalStaminaUsed        += savedBaseline.totalStaminaUsed;
                currentMetrics.totalFoodItemsConsumed  += savedBaseline.totalFoodItemsConsumed;
                currentMetrics.totalWaterItemsConsumed += savedBaseline.totalWaterItemsConsumed;
                currentMetrics.totalDistance           += savedBaseline.totalDistance;
                currentMetrics.totalTime               += savedBaseline.totalTime;
                currentMetrics.totalRiskyEvents        += savedBaseline.totalRiskyEvents;
                currentMetrics.encounterredRisks       += savedBaseline.encounterredRisks;
                currentMetrics.healthLossIncidents     += savedBaseline.healthLossIncidents;
                currentMetrics.totalHealthLost         += savedBaseline.totalHealthLost;
                currentMetrics.actualPathCost          += savedBaseline.actualPathCost;
                currentMetrics.optimalPathCost         += savedBaseline.optimalPathCost;
                currentMetrics.weatherSeverity          = Mathf.Max(currentMetrics.weatherSeverity, savedBaseline.weatherSeverity);
            }

            return currentMetrics;
        }
        
        /// <summary>
        /// Restores cumulative baseline from a previous session's save data.
        /// </summary>
        public void LoadBaseline(AssessmentSaveData baseline)
        {
            savedBaseline = baseline;
            // Restore all individual trackers (distance, stamina, health, fatigue, time, consumables)
            StatsTracker?.LoadBaseline(baseline);
        }

        /// <summary>
        /// Returns the current cumulative metrics as a save payload.
        /// </summary>
        public AssessmentSaveData GetSaveData()
        {
            var m = GetCurrentMetrics();
            return new AssessmentSaveData
            {
                totalStaminaUsed        = m.totalStaminaUsed,
                totalFoodItemsConsumed  = m.totalFoodItemsConsumed,
                totalWaterItemsConsumed = m.totalWaterItemsConsumed,
                totalDistance           = m.totalDistance,
                totalTime               = m.totalTime,
                totalRiskyEvents        = m.totalRiskyEvents,
                encounterredRisks       = m.encounterredRisks,
                healthLossIncidents     = m.healthLossIncidents,
                totalHealthLost         = m.totalHealthLost,
                totalFatigueAccumulated = StatsTracker != null ? StatsTracker.GetFatigueAccumulated() : 0f,
                actualPathCost          = m.actualPathCost,
                optimalPathCost         = m.optimalPathCost,
                weatherSeverity         = m.weatherSeverity,
                consumablesUsedList     = StatsTracker?.GetConsumablesForSave() ?? new System.Collections.Generic.List<ConsumableUseSaveData>(),
                riskEvents              = StatsTracker?.GetRiskEventsForSave() ?? new System.Collections.Generic.List<RiskEventSaveData>()
            };
        }


        /// <summary>
        /// Gets the recorded player path
        /// </summary>
        public List<Vector3> GetPlayerPath()
        {
            var pathTracker = StatsTracker?.GetPathTracker();
            return pathTracker?.PathPositions ?? new List<Vector3>();
        }
        
        /// <summary>
        /// Gets all risk events that were encountered
        /// </summary>
        public List<RiskEvent> GetRiskEvents()
        {
            var riskTracker = StatsTracker?.GetRiskTracker();
            return riskTracker?.EncounteredRisks ?? new List<RiskEvent>();
        }
        
        /// <summary>
        /// Gets all risk events (both encountered and avoided)
        /// </summary>
        public List<RiskEvent> GetAllRiskEvents()
        {
            var riskTracker = StatsTracker?.GetRiskTracker();
            return riskTracker?.AllRiskEvents ?? new List<RiskEvent>();
        }
        
        /// <summary>
        /// Gets risk statistics
        /// </summary>
        public (int total, int encountered, int avoided) GetRiskStats()
        {
            var riskTracker = StatsTracker?.GetRiskTracker();
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
