using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Aggregates multiple stat modifiers and calculates combined effects.
/// Implements IStatModifierCalculator for dependency inversion.
/// Follows Single Responsibility Principle - only handles modifier aggregation and calculation.
/// </summary>
public class StatModifierCollection : IStatModifierCalculator
{
    private readonly List<IStatModifier> modifiers = new List<IStatModifier>();

    /// <summary>
    /// Adds a modifier to the collection.
    /// </summary>
    public void AddModifier(IStatModifier modifier)
    {
        if (modifier != null)
        {
            modifiers.Add(modifier);
        }
    }

    /// <summary>
    /// Removes a modifier from the collection.
    /// </summary>
    public void RemoveModifier(IStatModifier modifier)
    {
        modifiers.Remove(modifier);
    }

    /// <summary>
    /// Clears all modifiers.
    /// </summary>
    public void Clear()
    {
        modifiers.Clear();
    }

    /// <summary>
    /// Gets all modifiers of a specific type.
    /// </summary>
    private IEnumerable<IStatModifier> GetModifiersOfType(StatModifierType type)
    {
        return modifiers.Where(m => m.ModifierType == type);
    }

    /// <summary>
    /// Checks if there are any modifiers of the specified type.
    /// </summary>
    public bool HasModifier(StatModifierType type)
    {
        return modifiers.Any(m => m.ModifierType == type);
    }

    /// <summary>
    /// Calculates the modified value by applying all relevant modifiers.
    /// Order of operations: Apply all additive modifiers first, then all multiplicative modifiers.
    /// </summary>
    public float GetModifiedValue(StatModifierType type, float baseValue)
    {
        var relevantModifiers = GetModifiersOfType(type).ToList();
        
        if (relevantModifiers.Count == 0)
        {
            return baseValue;
        }

        float result = baseValue;

        // Apply additive modifiers first
        foreach (var modifier in relevantModifiers.Where(m => !m.IsMultiplicative))
        {
            result = modifier.Apply(result);
        }

        // Then apply multiplicative modifiers
        foreach (var modifier in relevantModifiers.Where(m => m.IsMultiplicative))
        {
            result = modifier.Apply(result);
        }

        return result;
    }

    /// <summary>
    /// Gets the total count of modifiers in the collection.
    /// </summary>
    public int Count => modifiers.Count;

    /// <summary>
    /// Gets all modifiers (read-only).
    /// </summary>
    public IReadOnlyList<IStatModifier> GetAllModifiers() => modifiers.AsReadOnly();
}
