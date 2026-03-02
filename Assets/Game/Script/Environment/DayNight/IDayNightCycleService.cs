using UnityEngine;

namespace Game.Environment.DayNight
{
    /// <summary>
    /// Service interface for day/night cycle management.
    /// Provides access to time progression and current lighting state.
    /// </summary>
    public interface IDayNightCycleService
    {
        /// <summary>
        /// Current time of day in hours (0-24)
        /// </summary>
        float CurrentTime { get; }
        
        /// <summary>
        /// Current time period (Morning, Day, Evening, Night)
        /// </summary>
        TimeOfDay CurrentTimeOfDay { get; }
        
        /// <summary>
        /// Normalized day progress (0-1 where 0 = midnight, 0.5 = noon)
        /// </summary>
        float DayProgress { get; }
        
        /// <summary>
        /// Is the cycle currently paused?
        /// </summary>
        bool IsPaused { get; }
        
        /// <summary>
        /// Set specific time of day in hours (0-24)
        /// </summary>
        /// <param name="hours">Target time in 24-hour format</param>
        void SetTime(float hours);
        
        /// <summary>
        /// Jump to a specific time period
        /// </summary>
        /// <param name="timeOfDay">Target time period</param>
        void SetTimeOfDay(TimeOfDay timeOfDay);
        
        /// <summary>
        /// Pause or resume the day/night cycle
        /// </summary>
        /// <param name="paused">True to pause, false to resume</param>
        void SetPaused(bool paused);
        
        /// <summary>
        /// Get the light intensity multiplier for the current time
        /// </summary>
        /// <returns>Light intensity (typically 0.3 to 1.2)</returns>
        float GetLightIntensity();
        
        /// <summary>
        /// Get the ambient light color for the current time
        /// </summary>
        /// <returns>Current ambient color</returns>
        Color GetAmbientColor();
        
        /// <summary>
        /// Current in-game day number (starts at 1)
        /// </summary>
        int CurrentDay { get; }
        
        /// <summary>
        /// Skip time forward to the start of the next morning (increments the day counter)
        /// </summary>
        void SkipToNextMorning();
    }
}
