/// <summary>
/// Interface for calculating modified stat values.
/// Follows Interface Segregation Principle - only calculation methods.
/// Follows Dependency Inversion Principle - high-level modules depend on this abstraction.
/// </summary>
public interface IStatModifierCalculator
{
    /// <summary>
    /// Gets the modified value for a specific stat modifier type.
    /// </summary>
    /// <param name="type">The type of stat modifier</param>
    /// <param name="baseValue">The base value before modifications</param>
    /// <returns>The modified value after applying all relevant modifiers</returns>
    float GetModifiedValue(StatModifierType type, float baseValue);
    
    /// <summary>
    /// Checks if there are any modifiers of the specified type.
    /// </summary>
    /// <param name="type">The type of stat modifier to check</param>
    /// <returns>True if modifiers exist for this type</returns>
    bool HasModifier(StatModifierType type);
}
