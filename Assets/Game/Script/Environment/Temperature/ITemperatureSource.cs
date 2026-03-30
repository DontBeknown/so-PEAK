namespace Game.Environment.Temperature
{
    /// <summary>
    /// Any scene object that can warm or cool the player when nearby.
    /// Implement this on campfires, hot springs, ice zones, cursed altars, etc.
    /// The bonus is accumulated each frame by TemperatureStat.GatherHeatSources().
    /// </summary>
    public interface ITemperatureSource
    {
        /// <summary>
        /// Temperature bonus in °C applied to the player while they are within range.
        /// Positive values warm the player; negative values cool the player.
        /// </summary>
        float TemperatureBonus { get; }

        /// <summary>Whether this source is currently active (e.g. campfire is lit).</summary>
        bool IsActive { get; }
    }
}
