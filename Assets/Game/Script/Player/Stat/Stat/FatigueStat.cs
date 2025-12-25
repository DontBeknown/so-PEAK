using System;
using UnityEngine;

/// <summary>
/// FatigueStat represents long-term exhaustion that accumulates during physical activity.
/// Unlike stamina (short-term energy), fatigue persists and only recovers through rest.
/// Formula: fatigue = fatigue_rate_time * time_step + fatigue_rate_elev * abs(slope) + rate * speed
/// </summary>
[Serializable]
public class FatigueStat : Stat
{
    [SerializeField] private float rateTime = 0.12f;
    [SerializeField] private float rateElev = 0.0005f;
    
    private float currentSlopeGradient;
    private float currentMovementSpeed;
    private bool isActuallyMoving;

    public void Init(float maxFat, float timeRate, float elevRate)
    {
        SetMax(maxFat);
        rateTime = timeRate;
        rateElev = elevRate;
        SetCurrent(0f);
    }

    public override void Tick(float deltaTime)
    {
        // Only accumulate fatigue when actually moving
        if (isActuallyMoving)
        {
            float fatigueGain = rateTime * deltaTime + 
                                rateElev * currentSlopeGradient * deltaTime;
            Add(fatigueGain);
        }
        // No passive recovery - only through FullRest()
        
        // Reset per-frame movement tracking
        isActuallyMoving = false;
        currentSlopeGradient = 0f;
        currentMovementSpeed = 0f;
    }

    /// <summary>
    /// Update movement parameters for fatigue calculation this frame
    /// </summary>
    public void UpdateMovement(float slopeGradient, float movementSpeed, bool isMoving)
    {
        currentSlopeGradient = Mathf.Abs(slopeGradient);
        currentMovementSpeed = movementSpeed;
        isActuallyMoving = isMoving;
    }

    /// <summary>
    /// Fully clear all fatigue (e.g., sleeping, sitting by campfire)
    /// </summary>
    public void FullRest()
    {
        SetCurrent(0f);
    }

    /// <summary>
    /// Get fatigue's impact on stamina regeneration (0-1, where 1 = full regen, 0 = no regen)
    /// </summary>
    public float GetStaminaRegenPenalty(float threshold)
    {
        if (Current < threshold) return 1f;
        
        // Linear penalty above threshold
        float excessFatigue = Current - threshold;
        float maxExcess = Max - threshold;
        return Mathf.Max(0.25f, 1f - (excessFatigue / maxExcess) * 0.75f); // Min 25% regen
    }

    /// <summary>
    /// Get fatigue's impact on stamina drain (multiplier where 1 = normal drain, 2 = double drain)
    /// Higher fatigue = exhaust faster during activities
    /// </summary>
    public float GetStaminaDrainMultiplier()
    {
        // 1x at 0% fatigue, 2x at 100% fatigue
        return 1f + (Current / Max);
    }

    /// <summary>
    /// Get fatigue's impact on movement speed using travel time formula
    /// Formula: travel_time += (f_local - fatigue_limit) * 5.0
    /// Returns speed multiplier (0-1, where 1 = full speed)
    /// </summary>
    public float GetSpeedPenalty(float threshold)
    {
        if (Current < threshold) return 1f;
        
        // Calculate excess fatigue beyond threshold (normalized 0-1)
        float excessFatigue = (Current - threshold) / Max;
        
        // Calculate additional travel time based on formula
        float additionalTime = excessFatigue * 5.0f;
        
        // Speed = 1 / (1 + additional_time)
        // This creates exponential slowdown as fatigue increases
        float speedMultiplier = 1f / (1f + additionalTime);
        
        return Mathf.Max(0.2f, speedMultiplier); // Min 20% speed
    }
}
