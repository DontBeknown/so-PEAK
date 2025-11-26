using System;

namespace Game.Menu
{
    /// <summary>
    /// Data class representing a world
    /// This is a placeholder for the actual save/load system
    /// </summary>
    [Serializable]
    public class WorldData
    {
        public string WorldName;
        public int Seed;
        public string LastPlayed;
        public int PlayTimeMinutes;

        public WorldData(string worldName, int seed, string lastPlayed, int playTimeMinutes)
        {
            WorldName = worldName;
            Seed = seed;
            LastPlayed = lastPlayed;
            PlayTimeMinutes = playTimeMinutes;
        }

        // Additional properties that can be added later:
        // - Thumbnail/Screenshot
        // - Game mode (Survival, Creative, etc.)
        // - Difficulty
        // - Version
        // - World size
        // - File path
        // etc.
    }
}
