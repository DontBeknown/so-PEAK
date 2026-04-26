namespace Game.Environment.Temperature
{
    /// <summary>
    /// A held item that grants cold resistance while equipped and active.
    /// Resistance (0..1) is fed into TemperatureStat each frame by HeldItemBehaviorManager.
    /// </summary>
    public interface IHeldItemColdResistanceSource
    {
        /// <summary>Cold resistance in 0..1 range. Lerps effective temperature toward 37°C when below comfort.</summary>
        float ColdResistanceBonus { get; }

        /// <summary>Whether the item is currently active (equipped and has remaining durability).</summary>
        bool IsActive { get; }
    }
}
