using UnityEngine;

namespace Game.Environment.DayNight
{
    /// <summary>
    /// Configuration for the day/night cycle system.
    /// Defines timing, lighting, skybox, and fog settings for each time period.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "Game/Environment/Day Night Config")]
    public class DayNightConfig : ScriptableObject
    {
        [Header("Cycle Settings")]
        [Tooltip("Real-time seconds for a full day cycle (default: 1200 = 20 minutes)")]
        public float dayDurationInSeconds = 1200f;
        
        [Tooltip("Starting time in hours (0-24)")]
        [Range(0f, 24f)]
        public float startTime = 8f;
        
        [Header("Time Ranges")]
        [Tooltip("Hour when morning starts (default: 6:00)")]
        [Range(0f, 24f)]
        public float morningStartHour = 6f;
        
        [Tooltip("Hour when day starts (default: 12:00)")]
        [Range(0f, 24f)]
        public float dayStartHour = 12f;
        
        [Tooltip("Hour when evening starts (default: 18:00)")]
        [Range(0f, 24f)]
        public float eveningStartHour = 18f;
        
        [Tooltip("Hour when night starts (default: 21:00)")]
        [Range(0f, 24f)]
        public float nightStartHour = 21f;
        
        [Header("Skybox Materials")]
        [Tooltip("Skybox material for morning (dawn colors)")]
        public Material morningSkybox;
        
        [Tooltip("Skybox material for day (clear blue sky)")]
        public Material daySkybox;
        
        [Tooltip("Skybox material for evening (sunset colors)")]
        public Material eveningSkybox;
        
        [Tooltip("Skybox material for night (dark with stars)")]
        public Material nightSkybox;
        
        [Header("Skybox Transition")]
        [Tooltip("Duration in seconds to blend between skyboxes")]
        [Range(5f, 120f)]
        public float skyboxTransitionDuration = 30f;
        
        [Header("Lighting - Morning")]
        [Tooltip("Light color during morning (warm orange/yellow)")]
        public Color morningLightColor = new Color(1f, 0.9f, 0.7f);
        
        [Tooltip("Light intensity during morning")]
        [Range(0f, 2f)]
        public float morningLightIntensity = 0.8f;
        
        [Tooltip("Ambient light color during morning")]
        public Color morningAmbientColor = new Color(0.5f, 0.5f, 0.6f);
        
        [Tooltip("Ambient light intensity during morning")]
        [Range(0f, 2f)]
        public float morningAmbientIntensity = 1.0f;
        
        [Tooltip("Sun rotation during morning (X = pitch, Y = yaw, Z = roll)")]
        public Vector3 morningSunRotation = new Vector3(30f, 0f, 0f);
        
        [Header("Lighting - Day")]
        [Tooltip("Light color during day (bright white)")]
        public Color dayLightColor = new Color(1f, 0.95f, 0.9f);
        
        [Tooltip("Light intensity during day")]
        [Range(0f, 2f)]
        public float dayLightIntensity = 1.2f;
        
        [Tooltip("Ambient light color during day")]
        public Color dayAmbientColor = new Color(0.7f, 0.7f, 0.8f);
        
        [Tooltip("Ambient light intensity during day")]
        [Range(0f, 2f)]
        public float dayAmbientIntensity = 1.2f;
        
        [Tooltip("Sun rotation during day (X = pitch, Y = yaw, Z = roll)")]
        public Vector3 daySunRotation = new Vector3(90f, 0f, 0f);
        
        [Header("Lighting - Evening")]
        [Tooltip("Light color during evening (orange/red)")]
        public Color eveningLightColor = new Color(1f, 0.6f, 0.4f);
        
        [Tooltip("Light intensity during evening")]
        [Range(0f, 2f)]
        public float eveningLightIntensity = 0.7f;
        
        [Tooltip("Ambient light color during evening")]
        public Color eveningAmbientColor = new Color(0.5f, 0.4f, 0.5f);
        
        [Tooltip("Ambient light intensity during evening")]
        [Range(0f, 2f)]
        public float eveningAmbientIntensity = 0.8f;
        
        [Tooltip("Sun rotation during evening (X = pitch, Y = yaw, Z = roll)")]
        public Vector3 eveningSunRotation = new Vector3(10f, 0f, 0f);
        
        [Header("Lighting - Night")]
        [Tooltip("Light color during night (cool blue moonlight)")]
        public Color nightLightColor = new Color(0.5f, 0.6f, 0.8f);
        
        [Tooltip("Light intensity during night (lower = darker)")]
        [Range(0f, 2f)]
        public float nightLightIntensity = 0.2f;
        
        [Tooltip("Ambient light color during night")]
        public Color nightAmbientColor = new Color(0.15f, 0.15f, 0.2f);
        
        [Tooltip("Ambient light intensity during night (lower = darker)")]
        [Range(0f, 2f)]
        public float nightAmbientIntensity = 0.4f;
        
        [Tooltip("Moon rotation during night (X = pitch, Y = yaw, Z = roll)")]
        public Vector3 nightMoonRotation = new Vector3(-30f, 180f, 0f);
        
        [Header("Fog Settings (Optional)")]
        [Tooltip("Enable fog density changes based on time of day")]
        public bool useFog = true;
        
        [Tooltip("Fog color during morning")]
        public Color morningFogColor = new Color(0.8f, 0.85f, 0.9f);
        
        [Tooltip("Fog density during morning")]
        [Range(0f, 0.1f)]
        public float morningFogDensity = 0.01f;
        
        [Tooltip("Fog color during day")]
        public Color dayFogColor = new Color(0.9f, 0.95f, 1f);
        
        [Tooltip("Fog density during day")]
        [Range(0f, 0.1f)]
        public float dayFogDensity = 0.005f;
        
        [Tooltip("Fog color during evening")]
        public Color eveningFogColor = new Color(0.6f, 0.5f, 0.6f);
        
        [Tooltip("Fog density during evening")]
        [Range(0f, 0.1f)]
        public float eveningFogDensity = 0.015f;
        
        [Tooltip("Fog color during night (black for darker atmosphere)")]
        public Color nightFogColor = Color.black;
        
        [Tooltip("Fog density during night")]
        [Range(0f, 0.5f)]
        public float nightFogDensity = 0.02f;

        [Header("Ambient Sounds")]
        [Tooltip("SoundLibrary clip ID to play as ambient loop during morning (e.g. \"ambient_morning\")")]
        public string morningAmbientClipId = "ambient_morning";

        [Tooltip("SoundLibrary clip ID to play as ambient loop during day (e.g. \"ambient_day\")")]
        public string dayAmbientClipId = "ambient_day";

        [Tooltip("SoundLibrary clip ID to play as ambient loop during evening (e.g. \"ambient_evening\")")]
        public string eveningAmbientClipId = "ambient_evening";

        [Tooltip("SoundLibrary clip ID to play as ambient loop during night (e.g. \"ambient_night\")")]
        public string nightAmbientClipId = "ambient_night";

        /// <summary>
        /// Returns the ambient clip ID for the given time of day.
        /// </summary>
        public string GetAmbientClipId(TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                TimeOfDay.Morning => morningAmbientClipId,
                TimeOfDay.Day     => dayAmbientClipId,
                TimeOfDay.Evening => eveningAmbientClipId,
                TimeOfDay.Night   => nightAmbientClipId,
                _                 => dayAmbientClipId
            };
        }
        
        /// <summary>
        /// Get the time of day period for a given hour
        /// </summary>
        /// <param name="hours">Hour in 24-hour format (0-24)</param>
        /// <returns>Corresponding TimeOfDay enum</returns>
        public TimeOfDay GetTimeOfDay(float hours)
        {
            float h = hours % 24f;
            
            if (h >= morningStartHour && h < dayStartHour)
                return TimeOfDay.Morning;
            else if (h >= dayStartHour && h < eveningStartHour)
                return TimeOfDay.Day;
            else if (h >= eveningStartHour && h < nightStartHour)
                return TimeOfDay.Evening;
            else
                return TimeOfDay.Night;
        }
        
        /// <summary>
        /// Get the skybox material for a specific time of day
        /// </summary>
        /// <param name="timeOfDay">Target time period</param>
        /// <returns>Skybox material for that period</returns>
        public Material GetSkyboxForTime(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Morning:
                    return morningSkybox;
                case TimeOfDay.Day:
                    return daySkybox;
                case TimeOfDay.Evening:
                    return eveningSkybox;
                case TimeOfDay.Night:
                    return nightSkybox;
                default:
                    return daySkybox;
            }
        }
    }
}
