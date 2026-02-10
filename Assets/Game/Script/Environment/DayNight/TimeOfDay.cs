namespace Game.Environment.DayNight
{
    /// <summary>
    /// Represents the four distinct time periods in a day cycle.
    /// Used to control lighting, skybox, and gameplay systems.
    /// </summary>
    public enum TimeOfDay
    {
        /// <summary>
        /// Morning period (06:00 - 11:59)
        /// Warm orange/yellow light, dawn skybox
        /// </summary>
        Morning,
        
        /// <summary>
        /// Day period (12:00 - 17:59)
        /// Bright white light, clear blue skybox
        /// </summary>
        Day,
        
        /// <summary>
        /// Evening period (18:00 - 20:59)
        /// Orange/red light, sunset skybox
        /// </summary>
        Evening,
        
        /// <summary>
        /// Night period (21:00 - 05:59)
        /// Cool blue moonlight, dark starry skybox
        /// </summary>
        Night
    }
}
