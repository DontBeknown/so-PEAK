namespace Game.Inventory.Effects
{
    /// <summary>
    /// Interface for consumable item effects
    /// Follows Strategy Pattern - effects are interchangeable
    /// Follows Open/Closed Principle - new effects without modification
    /// </summary>
    public interface IConsumableEffect
    {
        /// <summary>
        /// Applies the effect to the target
        /// </summary>
        void Apply(object target);
        
        /// <summary>
        /// Gets a description of what this effect does
        /// </summary>
        string GetDescription();
        
        /// <summary>
        /// Checks if this effect can be applied to the target
        /// </summary>
        bool CanApply(object target);
    }
}
