using UnityEngine;
using System;

/// <summary>
/// Calculates blur intensity based on player's hunger and thirst stats.
/// Follows Single Responsibility Principle - only concerned with intensity calculation.
/// </summary>
[Serializable]
public class SurvivalStatBlurCalculator : IBlurIntensityCalculator
{
    [Header("Thresholds")]
    [SerializeField] private float hungerCriticalThreshold = 30f;
    [SerializeField] private float thirstCriticalThreshold = 30f;
    [SerializeField] private float hungerSevereThreshold = 10f;
    [SerializeField] private float thirstSevereThreshold = 10f;
    
    [Header("Intensity Settings")]
    [SerializeField] private float minBlurIntensity = 0f;
    [SerializeField] private float maxBlurIntensity = 0.8f;
    [SerializeField] private bool useWorstStat = true;
    
    private PlayerStats playerStats;
    private float currentTargetIntensity;
    
    public event Action<float> OnIntensityChanged;
    
    /// <summary>
    /// Constructor with dependency injection.
    /// Follows Dependency Inversion Principle.
    /// </summary>
    public SurvivalStatBlurCalculator(PlayerStats playerStats)
    {
        this.playerStats = playerStats ?? throw new ArgumentNullException(nameof(playerStats));
    }
    
    public void Initialize()
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats is null. Cannot initialize SurvivalStatBlurCalculator.");
            return;
        }
        
        // Subscribe to stat change events
        SubscribeToStatEvents();
        
        // Calculate initial intensity
        UpdateIntensity();
    }
    
    public void Cleanup()
    {
        UnsubscribeFromStatEvents();
    }
    
    public float CalculateIntensity()
    {
        return currentTargetIntensity;
    }
    
    private void SubscribeToStatEvents()
    {
        // Subscribe to hunger and thirst stat events through PlayerStats
        // Since PlayerStats doesn't expose direct stat events, we'll need to check in UpdateIntensity
        // Alternative: Could add events to PlayerStats for hunger/thirst changes
    }
    
    private void UnsubscribeFromStatEvents()
    {
        // Unsubscribe from events
    }
    
    /// <summary>
    /// Updates the target intensity based on current hunger and thirst values.
    /// Call this when stats change or from Update loop.
    /// </summary>
    public void UpdateIntensity()
    {
        float hungerIntensity = CalculateStatIntensity(
            playerStats.Hunger,
            hungerCriticalThreshold,
            hungerSevereThreshold
        );
        
        float thirstIntensity = CalculateStatIntensity(
            playerStats.Thirst,
            thirstCriticalThreshold,
            thirstSevereThreshold
        );
        
        float newIntensity = CombineIntensities(hungerIntensity, thirstIntensity);
        
        if (!Mathf.Approximately(currentTargetIntensity, newIntensity))
        {
            currentTargetIntensity = newIntensity;
            OnIntensityChanged?.Invoke(currentTargetIntensity);
        }
    }
    
    private float CalculateStatIntensity(float statValue, float criticalThreshold, float severeThreshold)
    {
        // No blur if stat is above critical threshold
        if (statValue > criticalThreshold)
        {
            return minBlurIntensity;
        }
        
        // Maximum blur if at or below severe threshold
        if (statValue <= severeThreshold)
        {
            return maxBlurIntensity;
        }
        
        // Linear interpolation between severe and critical thresholds
        float normalizedValue = (statValue - severeThreshold) / (criticalThreshold - severeThreshold);
        return Mathf.Lerp(maxBlurIntensity, minBlurIntensity, normalizedValue);
    }
    
    private float CombineIntensities(float hungerIntensity, float thirstIntensity)
    {
        return useWorstStat 
            ? Mathf.Max(hungerIntensity, thirstIntensity)
            : (hungerIntensity + thirstIntensity) / 2f;
    }
    
    #region Configuration Setters (Optional - for runtime configuration)
    
    public void SetHungerThresholds(float critical, float severe)
    {
        hungerCriticalThreshold = critical;
        hungerSevereThreshold = severe;
        UpdateIntensity();
    }
    
    public void SetThirstThresholds(float critical, float severe)
    {
        thirstCriticalThreshold = critical;
        thirstSevereThreshold = severe;
        UpdateIntensity();
    }
    
    public void SetIntensityRange(float min, float max)
    {
        minBlurIntensity = Mathf.Clamp01(min);
        maxBlurIntensity = Mathf.Clamp01(max);
        UpdateIntensity();
    }
    
    public void SetUseWorstStat(bool useWorst)
    {
        useWorstStat = useWorst;
        UpdateIntensity();
    }
    
    #endregion
}
