using System;
using UnityEngine;

[Serializable]
public class StaminaStat : Stat
{
    [SerializeField] private float regenPerSecond = 15f;
    [SerializeField] private float drainCooldown = 1f;
    [SerializeField] private float climbDrainPerSecond = 10f;

    private float cooldownTimer;
    private bool draining;
    private bool isClimbing;
    private bool isWalking;
    
    // Terrain-based drain tracking
    private float currentSlopeDrain;
    
    // Fatigue system: fatigue = fatigue_rate_time * time_step + fatigue_rate_elev * abs(slope) + rate * speed
    private float currentFatigue;
    private float fatigueRateTime = 0.12f;
    private float fatigueRateElev = 0.0005f;
    private float fatigueRateSpeed = 0.1f;
    private float maxFatigue = 100f;
    private float currentSlopeGradient; // abs(slope) for fatigue calculation
    private float currentMovementSpeed; // speed for fatigue calculation
    private bool isActuallyMoving; // Track if player is physically moving
    
    public float Fatigue => currentFatigue;
    public float FatiguePercent => maxFatigue > 0 ? (currentFatigue / maxFatigue) * 100f : 0f;

    public void Init(float regen, float cooldown, float climbDrain)
    {
        regenPerSecond = regen;
        drainCooldown = cooldown;
        climbDrainPerSecond = climbDrain;
    }
    
    public void InitFatigue(float maxFat, float rateTime, float rateElev, float rateSpeed)
    {
        maxFatigue = maxFat;
        fatigueRateTime = rateTime;
        fatigueRateElev = rateElev;
        fatigueRateSpeed = rateSpeed;
        currentFatigue = 0f;
    }
    
    /// <summary>
    /// Fully clear all fatigue (e.g., sleeping, sitting by campfire)
    /// </summary>
    public void FullRest()
    {
        currentFatigue = 0f;
    }

    public override void Tick(float deltaTime)
    {
        // Update fatigue using formula: fatigue = fatigue_rate_time * time_step + fatigue_rate_elev * abs(slope) + rate * speed
        // Only accumulate when actually moving
        if (isActuallyMoving)
        {
            float fatigueGain = fatigueRateTime * deltaTime + 
                                fatigueRateElev * currentSlopeGradient * deltaTime + 
                                fatigueRateSpeed * currentMovementSpeed * deltaTime;
            currentFatigue = Mathf.Min(maxFatigue, currentFatigue + fatigueGain);
        }
        // No fatigue recovery when standing still - only through FullRest()
        
        if (isClimbing)
        {
            Drain(climbDrainPerSecond * deltaTime);
            return;
        }

        // Apply terrain-based drain if moving
        if (currentSlopeDrain > 0f && isWalking)
        {
            Drain(currentSlopeDrain * deltaTime);
            currentSlopeDrain = 0f; // Reset after applying
            currentSlopeGradient = 0f; // Reset slope
            currentMovementSpeed = 0f; // Reset speed
            isActuallyMoving = false; // Reset movement flag after processing
        }
        else
        {
            // If no terrain drain this frame, player is not moving
            isActuallyMoving = false;
        }

        if (draining)
        {
            cooldownTimer = drainCooldown;
            draining = false;
        }
        else
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= deltaTime;
            else if (isWalking) // Only regen when in walking state
                Add(regenPerSecond * deltaTime);
        }
    }

    public void Drain(float amount)
    {
        draining = true;
        
        // Apply fatigue penalty to stamina drain
        // Higher fatigue = more stamina drain (exhausted players tire faster)
        float fatigueDrainMultiplier = 1f + (currentFatigue / maxFatigue); // 1x at 0% fatigue, 2x at 100% fatigue
        float adjustedAmount = amount * fatigueDrainMultiplier;
        
        SetCurrent(Current - adjustedAmount);
    }

    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing;
    }

    public void SetWalking(bool walking)
    {
        isWalking = walking;
    }

    /// <summary>
    /// Apply terrain-based stamina drain and set fatigue calculation parameters
    /// </summary>
    /// <param name="drainPerSecond">Calculated drain rate based on slope</param>
    /// <param name="slopeGradient">Absolute slope gradient for fatigue calculation</param>
    /// <param name="movementSpeed">Current movement speed for fatigue calculation</param>
    public void ApplyTerrainDrain(float drainPerSecond, float slopeGradient, float movementSpeed)
    {
        currentSlopeDrain = drainPerSecond;
        currentSlopeGradient = Mathf.Abs(slopeGradient);
        currentMovementSpeed = movementSpeed;
        isActuallyMoving = drainPerSecond > 0f; // Player is actually moving if there's terrain drain
    }

    public bool CanUse(float amount) => current >= amount;
    
    /// <summary>
    /// Get fatigue impact on stamina regeneration (0-1, where 0 = no regen, 1 = full regen)
    /// </summary>
    public float GetFatigueRegenPenalty(float threshold)
    {
        if (currentFatigue < threshold) return 1f;
        // Linear penalty above threshold
        float excessFatigue = currentFatigue - threshold;
        float maxExcess = maxFatigue - threshold;
        return Mathf.Max(0.25f, 1f - (excessFatigue / maxExcess) * 0.75f); // Min 25% regen
    }
    
    /// <summary>
    /// Get fatigue impact on movement speed using travel time formula
    /// Formula: travel_time += (f_local - fatigue_limit) * 5.0
    /// Returns speed multiplier (0-1, where 1 = full speed)
    /// </summary>
    public float GetFatigueSpeedPenalty(float threshold)
    {
        if (currentFatigue < threshold) return 1f;
        
        // Calculate excess fatigue beyond threshold (normalized 0-1)
        float excessFatigue = (currentFatigue - threshold) / maxFatigue;
        
        // Calculate additional travel time based on formula
        float additionalTime = excessFatigue * 5.0f;
        
        // Speed = 1 / (1 + additional_time)
        // This creates exponential slowdown as fatigue increases
        float speedMultiplier = 1f / (1f + additionalTime);
        
        return Mathf.Max(0.2f, speedMultiplier); // Min 20% speed
    }
}
