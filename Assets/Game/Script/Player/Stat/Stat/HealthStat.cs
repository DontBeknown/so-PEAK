// HealthStat.cs
using System;

[Serializable]
public class HealthStat : Stat
{
    public event Action OnDeath;
    public event Action<float> OnDamaged;
    private bool hasDied;

    public void Damage(float amount)
    {
        if (hasDied || current <= 0f) return;
        OnDamaged?.Invoke(amount);
        Subtract(amount);
    }

    public void Heal(float amount)
    {
        Add(amount);
        if (current > 0f)
        {
            hasDied = false;
        }
    }

    public new void ResetToFull()
    {
        base.ResetToFull();
        hasDied = false;
    }

    public override void Subtract(float amount)
    {
        base.Subtract(amount);
        if (!hasDied && current <= 0f)
        {
            hasDied = true;
            OnDeath?.Invoke();
        }
    }
}
