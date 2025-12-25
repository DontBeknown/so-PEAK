using UnityEngine;
using System;

/// <summary>
/// Serializable implementation of IStatModifier.
/// Represents a single stat modification that can be applied to player stats.
/// Follows Single Responsibility Principle - only handles stat modification logic.
/// </summary>
[Serializable]
public class StatModifier : IStatModifier
{
    [SerializeField] private StatModifierType modifierType;
    [SerializeField] private float value;
    [SerializeField] private bool isMultiplicative;

    public StatModifierType ModifierType => modifierType;
    public float Value => value;
    public bool IsMultiplicative => isMultiplicative;

    /// <summary>
    /// Constructor for creating stat modifiers.
    /// </summary>
    /// <param name="type">The type of stat to modify</param>
    /// <param name="value">The modification value</param>
    /// <param name="isMultiplicative">True for percentage modifiers, false for flat values</param>
    public StatModifier(StatModifierType type, float value, bool isMultiplicative = false)
    {
        this.modifierType = type;
        this.value = value;
        this.isMultiplicative = isMultiplicative;
    }

    /// <summary>
    /// Applies this modifier to a base value.
    /// Additive: baseValue + value
    /// Multiplicative: baseValue * (1 + value)
    /// </summary>
    public float Apply(float baseValue)
    {
        if (isMultiplicative)
        {
            // Multiplicative modifier: value of 0.1 = +10%, -0.2 = -20%
            return baseValue * (1f + value);
        }
        else
        {
            // Additive modifier: flat value addition
            return baseValue + value;
        }
    }

    public override string ToString()
    {
        string sign = value >= 0 ? "+" : "";
        string format = isMultiplicative ? $"{sign}{value * 100:F1}%" : $"{sign}{value:F2}";
        return $"{modifierType}: {format}";
    }
}
