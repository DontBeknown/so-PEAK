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

    public void Init(float regen, float cooldown, float climbDrain)
    {
        regenPerSecond = regen;
        drainCooldown = cooldown;
        climbDrainPerSecond = climbDrain;
    }

    public override void Tick(float deltaTime)
    {
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
        SetCurrent(Current - amount);
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
    /// Apply terrain-based stamina drain
    /// </summary>
    /// <param name="drainPerSecond">Calculated drain rate based on slope</param>
    public void ApplyTerrainDrain(float drainPerSecond)
    {
        currentSlopeDrain = drainPerSecond;
    }

    public bool CanUse(float amount) => current >= amount;
}
