namespace Game.Player.Inventory.Effects
{
    /// <summary>
    /// Handles consumable item effect application
    /// Decoupled from inventory storage
    /// </summary>
    public interface IConsumableEffectSystem
    {
        bool CanApplyEffect(ConsumableEffect effect);
        void ApplyEffect(ConsumableEffect effect, PlayerStats stats);
        void RegisterEffectStrategy(StatType statType, IEffectStrategy strategy);
    }
    
    /// <summary>
    /// Strategy interface for different effect types
    /// Allows extension without modification (Open/Closed Principle)
    /// </summary>
    public interface IEffectStrategy
    {
        void Apply(ConsumableEffect effect, PlayerStats stats);
    }
}
