// ThirstStat.cs
using System;
using UnityEngine;

[Serializable]
public class ThirstStat : Stat
{
    [SerializeField] private float gainPerSecond = 0.35f;
    [SerializeField] private float hurtThreshold = 60f;
    [SerializeField] private float damagePerSecond = 2f;

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
    public float DehydrateDPS => damagePerSecond;
}
