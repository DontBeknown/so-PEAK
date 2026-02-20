// Stat.cs
using UnityEngine;
using System;

[System.Serializable]
public class Stat : IStat
{
    [SerializeField] protected float max = 100f;
    [SerializeField] protected float current = 100f;

    public float Current => current;
    public float Max => max;
    public float Percent => max > 0f ? current / max : 0f;

    public event Action<float, float> OnChanged;

    public virtual void Add(float amount)
    {
        SetCurrent(current + amount);
    }

    public virtual void Subtract(float amount)
    {
        SetCurrent(current - amount);
    }

    public virtual void SetMax(float newMax)
    {
        max = Mathf.Max(0f, newMax);
        current = Mathf.Min(current, max);
        RaiseChanged();
    }

    protected virtual void SetCurrent(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, max);
        if (!Mathf.Approximately(current, clamped))
        {
            current = clamped;
            RaiseChanged();
        }
    }

    protected void RaiseChanged() => OnChanged?.Invoke(current, max);

    public virtual void Tick(float deltaTime) { }

    /// <summary>
    /// Resets current to max. Called at game start to clear any stale serialized values.
    /// </summary>
    public void ResetToFull()
    {
        current = max;
        RaiseChanged();
    }
}
