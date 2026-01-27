using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Main service that coordinates all stat tracking.
/// Manages tracker lifecycle and provides public API for data access.
/// SRP: Manages tracker coordination and event subscriptions.
/// </summary>
public class PlayerStatsTrackerService : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private InventoryManager inventoryManager;
    
    [Header("Settings")]
    [SerializeField] private int maxDataPoints = 100;
    [SerializeField] private float snapshotInterval = 5f; // seconds between time-series snapshots
    [SerializeField] private float minPathDistance = 1f; // minimum distance to record path positions
    
    // Trackers using IStatTracker<T> interface
    private DistanceTracker distanceTracker;
    private StaminaTracker staminaTracker;
    private FatigueTracker fatigueTracker;
    private HealthLossTracker healthLossTracker;
    private ConsumableTracker consumableTracker;
    private PathTracker pathTracker;
    private RiskTracker riskTracker;
    
    // State
    private bool isTracking;
    private float sessionStartTime;
    private float timeSinceLastSnapshot;
    private float accumulatedSessionDuration; // Stores duration when tracking is paused
    
    // Public properties
    public bool IsTracking => isTracking;
    public float SessionDuration => accumulatedSessionDuration + (isTracking ? Time.time - sessionStartTime : 0f);
    
    private void Awake()
    {
        InitializeTrackers();
        FindDependencies();
    }
    
    private void Start()
    {
        SubscribeToEvents();
        StartTracking();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeTrackers()
    {
        distanceTracker = new DistanceTracker(maxDataPoints);
        staminaTracker = new StaminaTracker(maxDataPoints);
        fatigueTracker = new FatigueTracker(maxDataPoints);
        healthLossTracker = new HealthLossTracker(maxDataPoints);
        consumableTracker = new ConsumableTracker(maxDataPoints);
        pathTracker = new PathTracker(minPathDistance, maxDataPoints);
        riskTracker = new RiskTracker(maxDataPoints);
    }
    
    private void FindDependencies()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();
        
        if (playerTransform == null && playerStats != null)
            playerTransform = playerStats.transform;
        
        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>();
    }
    
    private void SubscribeToEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnStaminaDrained += HandleStaminaDrained;
            playerStats.OnHealthDamaged += HandleHealthDamaged;
            playerStats.OnFatigueChanged += HandleFatigueChanged;
        }
        
        InventoryManager.OnItemConsumed += HandleItemConsumed;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (playerStats != null)
        {
            playerStats.OnStaminaDrained -= HandleStaminaDrained;
            playerStats.OnHealthDamaged -= HandleHealthDamaged;
            playerStats.OnFatigueChanged -= HandleFatigueChanged;
        }
        
        InventoryManager.OnItemConsumed -= HandleItemConsumed;
    }
    
    /// <summary>
    /// Starts tracking player statistics.
    /// </summary>
    public void StartTracking()
    {
        if (isTracking) return;
        
        isTracking = true;
        sessionStartTime = Time.time;
        timeSinceLastSnapshot = 0f;
        
        //Debug.Log("[StatTracker] Started tracking player statistics");
    }
    
    /// <summary>
    /// Stops tracking player statistics.
    /// </summary>
    public void StopTracking()
    {
        if (!isTracking) return;
        
        // Accumulate the session duration before stopping
        accumulatedSessionDuration += Time.time - sessionStartTime;
        isTracking = false;
        
        Debug.Log($"[StatTracker] Stopped tracking. Session duration: {FormatTime(SessionDuration)}");
    }
    
    /// <summary>
    /// Resets all tracking data and starts a new session.
    /// </summary>
    public void ResetTracking()
    {
        distanceTracker.Reset();
        staminaTracker.Reset();
        fatigueTracker.Reset();
        healthLossTracker.Reset();
        consumableTracker.Reset();
        pathTracker.Reset();
        riskTracker.Reset();
        
        accumulatedSessionDuration = 0f;
        sessionStartTime = Time.time;
        timeSinceLastSnapshot = 0f;
        
        Debug.Log("[StatTracker] Reset all tracking data");
    }
    
    private void Update()
    {
        if (!isTracking) return;
        
        float dt = Time.deltaTime;
        timeSinceLastSnapshot += dt;
        
        // Update distance and path tracking
        if (playerTransform != null)
        {
            distanceTracker.UpdatePosition(playerTransform.position);
            pathTracker.UpdatePosition(playerTransform.position);
        }
        
        // Periodic time-series snapshots
        if (timeSinceLastSnapshot >= snapshotInterval)
        {
            UpdateAllTimeSeriesData();
            timeSinceLastSnapshot = 0f;
        }

    }
    
    private void UpdateAllTimeSeriesData()
    {
        float timestamp = Time.time - sessionStartTime;
        
        distanceTracker.UpdateTimeSeries(timestamp);
        staminaTracker.UpdateTimeSeries(timestamp);
        fatigueTracker.UpdateTimeSeries(timestamp);
        healthLossTracker.UpdateTimeSeries(timestamp);
        consumableTracker.UpdateTimeSeries(timestamp);
        pathTracker.UpdateTimeSeries(timestamp);
        riskTracker.UpdateTimeSeries(timestamp);
    }
    
    // Event Handlers
    private void HandleStaminaDrained(float amount)
    {
        staminaTracker.RecordValue(amount);
    }
    
    private void HandleHealthDamaged(float amount)
    {
        healthLossTracker.RecordValue(amount);
    }
    
    private void HandleFatigueChanged(float newValue)
    {
        fatigueTracker.RecordValue(newValue);
    }
    
    private void HandleItemConsumed(InventoryItem item, int quantity)
    {
        if (item != null)
        {
            for (int i = 0; i < quantity; i++)
            {
                consumableTracker.RecordConsumable(item); // Pass InventoryItem object
            }
        }
    }
    
    // Public API for data access
    
    /// <summary>
    /// Gets the total distance walked in meters.
    /// </summary>
    public float GetDistanceWalked() => distanceTracker.CurrentValue;
    
    /// <summary>
    /// Gets the total stamina used.
    /// </summary>
    public float GetStaminaUsed() => staminaTracker.CurrentValue;
    
    /// <summary>
    /// Gets the maximum fatigue accumulated.
    /// </summary>
    public float GetFatigueAccumulated() => fatigueTracker.CurrentValue;
    
    /// <summary>
    /// Gets the total health lost.
    /// </summary>
    public float GetHealthLost() => healthLossTracker.CurrentValue;
    
    /// <summary>
    /// Gets the dictionary of consumables used.
    /// </summary>
    public Dictionary<string, int> GetConsumablesUsed() => consumableTracker.CurrentValue;
    
    /// <summary>
    /// Gets the total count of consumables used.
    /// </summary>
    public int GetTotalConsumablesUsed() => consumableTracker.TotalCount;
    
    /// <summary>
    /// Gets count of food items consumed (for assessment).
    /// </summary>
    public int GetFoodItemsConsumed() => consumableTracker.GetFoodItemsConsumed();
    
    /// <summary>
    /// Gets count of water items consumed (for assessment).
    /// </summary>
    public int GetWaterItemsConsumed() => consumableTracker.GetWaterItemsConsumed();
    
    /// <summary>
    /// Gets count of health loss incidents (for assessment).
    /// </summary>
    public int GetHealthLossIncidents() => healthLossTracker.GetIncidentCount();
    
    /// <summary>
    /// Gets the PathTracker instance.
    /// </summary>
    public PathTracker GetPathTracker() => pathTracker;
    
    /// <summary>
    /// Gets the RiskTracker instance.
    /// </summary>
    public RiskTracker GetRiskTracker() => riskTracker;
    
    /// <summary>
    /// Registers a risk event.
    /// </summary>
    public void RegisterRiskEvent(RiskEvent riskEvent)
    {
        riskTracker.RecordValue(riskEvent);
    }
    
    /// <summary>
    /// Gets time-series data for a specific metric type.
    /// </summary>
    public List<TimeSeriesDataPoint> GetTimeSeriesData(StatMetricType metricType)
    {
        return metricType switch
        {
            StatMetricType.Distance => distanceTracker.TimeSeriesData,
            StatMetricType.Stamina => staminaTracker.TimeSeriesData,
            StatMetricType.Fatigue => fatigueTracker.TimeSeriesData,
            StatMetricType.Health => healthLossTracker.TimeSeriesData,
            StatMetricType.Consumables => consumableTracker.TimeSeriesData,
            StatMetricType.PathTracking => pathTracker.TimeSeriesData,
            StatMetricType.RiskTracking => riskTracker.TimeSeriesData,
            _ => new List<TimeSeriesDataPoint>()
        };
    }
    
    /// <summary>
    /// Formats time in seconds to a readable string.
    /// </summary>
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes}m {secs}s";
    }
}
