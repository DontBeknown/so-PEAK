// ThirstStat.cs
using System;
using UnityEngine;

[Serializable]
public class ThirstStat : Stat
{
    [SerializeField] private float gainPerSecond = 0.35f;
    [SerializeField] private float hurtThreshold = 60f;
    [SerializeField] private float damagePerSecond = 2f;
    private float sprintDrainMultiplier = 2.5f;
    private float _temperatureMultiplier = 1f; // set each frame by PlayerStats

    private bool isSprinting;

    public void Init(float gain, float threshold, float dps, float sprintMultiplier = 2.5f)
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

    /// <summary>Applied by PlayerStats each frame. 1.0 = normal. >1 = faster drain (e.g. heat sweating).</summary>
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
    public float DehydrateDPS => damagePerSecond;
}
