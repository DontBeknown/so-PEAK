using Game.Environment.DayNight;

namespace Game.Core.Events
{
    /// <summary>
    /// Published when the time of day changes between periods (Morning/Day/Evening/Night).
    /// Allows systems to react to time transitions (e.g., temperature changes, AI behavior).
    /// </summary>
    public class TimeOfDayChangedEvent
    {
        /// <summary>
        /// The previous time period before the transition
        /// </summary>
        public TimeOfDay previousTimeOfDay;
        
        /// <summary>
        /// The new time period after the transition
        /// </summary>
        public TimeOfDay newTimeOfDay;
        
        /// <summary>
        /// Current time in hours (0-24) when the transition occurred
        /// </summary>
        public float currentTime;
    }

    /// <summary>
    /// Published when a full 24-hour day cycle completes.
    /// Can be used for daily events, quest timers, or resource respawns.
    /// </summary>
    public class DayCompletedEvent
    {
        /// <summary>
        /// The number of days that have passed since game start
        /// </summary>
        public int dayNumber;
    }

    /// <summary>
    /// Published every in-game hour (optional event for hourly updates).
    /// Useful for systems that need more granular time tracking than TimeOfDayChangedEvent.
    /// </summary>
    public class HourChangedEvent
    {
        /// <summary>
        /// The current hour (0-23)
        /// </summary>
        public int hour;
    }
}
