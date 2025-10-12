// HealthStat.cs
using System;

[Serializable]
public class HealthStat : Stat
{
    public event Action OnDeath;

    public void Damage(float amount)
    {
        if (current <= 0f) return;
        Subtract(amount);
        if (current <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        Add(amount);
    }

    public override void Subtract(float amount)
    {
        base.Subtract(amount);
        if (current <= 0f) OnDeath?.Invoke();
    }
}
