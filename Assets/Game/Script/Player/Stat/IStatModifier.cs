/// <summary>
/// Interface for stat modifiers that can be applied to player stats.
/// Follows Interface Segregation Principle - only stat modification methods.
/// </summary>
public interface IStatModifier
{
    /// <summary>
    /// The type of stat this modifier affects.
    /// </summary>
    StatModifierType ModifierType { get; }
    
    /// <summary>
    /// The value of the modification.
    /// </summary>
    float Value { get; }
    
    /// <summary>
    /// Whether this modifier is multiplicative (percentage) or additive (flat value).
    /// </summary>
    bool IsMultiplicative { get; }
    
    /// <summary>
    /// Applies this modifier to a base value.
    /// </summary>
    /// <param name="baseValue">The base value to modify</param>
    /// <returns>The modified value</returns>
    float Apply(float baseValue);
}
