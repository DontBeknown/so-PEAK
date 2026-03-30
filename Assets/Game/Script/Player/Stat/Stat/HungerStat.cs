// HungerStat.cs
using System;
using UnityEngine;

[Serializable]
public class HungerStat : Stat
{
    [SerializeField] private float gainPerSecond = 0.2f;
    [SerializeField] private float hurtThreshold = 70f;
    [SerializeField] private float damagePerSecond = 1f;
    private float sprintDrainMultiplier = 2f;
    private float _temperatureMultiplier = 1f; // set each frame by PlayerStats

    private bool isSprinting;

    public void Init(float gain, float threshold, float dps, float sprintMultiplier = 2f)
    {
        gainPerSecond = gain;
        hurtThreshold = threshold;
        damagePerSecond = dps;
        sprintDrainMultiplier = sprintMultiplier;
        SetCurrent(max); // Always start full, ignore serialized leftover value
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    /// <summary>Applied by PlayerStats each frame. 1.0 = normal. >1 = faster drain (e.g. cold shivering).</summary>
    public void SetTemperatureMultiplier(float multiplier)
    {
        _temperatureMultiplier = Mathf.Max(1f, multiplier);
    }

    public override void Tick(float deltaTime)
    {
        float multiplier = (isSprinting ? sprintDrainMultiplier : 1f) * _temperatureMultiplier;
        Subtract(gainPerSecond * multiplier * deltaTime);
    }

    public bool ShouldHurt => current <= hurtThreshold;
    public float StarveDPS => damagePerSecond;
}
