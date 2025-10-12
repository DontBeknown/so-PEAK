// HungerStat.cs
using System;
using UnityEngine;

[Serializable]
public class HungerStat : Stat
{
    [SerializeField] private float gainPerSecond = 0.2f;
    [SerializeField] private float hurtThreshold = 70f;
    [SerializeField] private float damagePerSecond = 1f;

    public void Init(float gain, float threshold, float dps)
    {
        gainPerSecond = gain;
        hurtThreshold = threshold;
        damagePerSecond = dps;
    }

    public override void Tick(float deltaTime)
    {
        Subtract(gainPerSecond * deltaTime);
    }

    public bool ShouldHurt => current <= hurtThreshold;
    public float StarveDPS => damagePerSecond;
}
