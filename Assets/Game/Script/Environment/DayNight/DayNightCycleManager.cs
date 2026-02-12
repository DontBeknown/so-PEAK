using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;

namespace Game.Environment.DayNight
{
    /// <summary>
    /// Manages the day/night cycle including time progression, lighting, and skybox transitions.
    /// Implements IDayNightCycleService for dependency injection.
    /// </summary>
    public class DayNightCycleManager : MonoBehaviour, IDayNightCycleService
    {
        [Header("Configuration")]
        [SerializeField] private DayNightConfig config;
        
        [Header("Scene References")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private SkyboxBlender skyboxBlender;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // State
        private float _currentTime;
        private TimeOfDay _currentTimeOfDay;
        private TimeOfDay _previousTimeOfDay;
        private bool _isPaused = false;
        
        // Skybox blending
        private float _skyboxBlendProgress = 0f;
        private bool _isTransitioning = false;
        
        // Services
        private IEventBus _eventBus;
        
        #region IDayNightCycleService Implementation
        
        public float CurrentTime => _currentTime;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;
        public float DayProgress => _currentTime / 24f;
        public bool IsPaused => _isPaused;
        
        public void SetTime(float hours)
        {
            _currentTime = Mathf.Clamp(hours, 0f, 24f);
            UpdateTimeOfDay();
        }
        
        public void SetTimeOfDay(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Morning:
                    _currentTime = config.morningStartHour;
                    break;
                case TimeOfDay.Day:
                    _currentTime = config.dayStartHour;
                    break;
                case TimeOfDay.Evening:
                    _currentTime = config.eveningStartHour;
                    break;
                case TimeOfDay.Night:
                    _currentTime = config.nightStartHour;
                    break;
            }
            UpdateTimeOfDay();
        }
        
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Cycle {(paused ? "paused" : "resumed")}");
            }
        }
        
        public float GetLightIntensity()
        {
            var settings = GetLightingSettingsForTime();
            return settings.intensity;
        }
        
        public Color GetAmbientColor()
        {
            var settings = GetLightingSettingsForTime();
            return settings.ambientColor;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Validate configuration
            if (config == null)
            {
                Debug.LogError("[DayNightCycleManager] No DayNightConfig assigned!");
                enabled = false;
                return;
            }
            
            // Resolve dependencies
            _eventBus = ServiceContainer.Instance.Get<IEventBus>();
            
            // Initialize time
            _currentTime = config.startTime;
            _currentTimeOfDay = config.GetTimeOfDay(_currentTime);
            _previousTimeOfDay = _currentTimeOfDay;
            
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Initialized at {_currentTime:F1}h ({_currentTimeOfDay})");
            }
        }
        
        private void Start()
        {
            // Validate references
            if (directionalLight == null)
            {
                Debug.LogError("[DayNightCycleManager] No directional light assigned! Please assign a directional light in the inspector.");
                enabled = false;
                return;
            }
            
            // Apply initial settings
            ApplyLightingSettings();
            RenderSettings.skybox = config.GetSkyboxForTime(_currentTimeOfDay);
            DynamicGI.UpdateEnvironment();
        }
        
        private void Update()
        {
            if (_isPaused) return;
            
            // Update time
            float timeIncrement = (24f / config.dayDurationInSeconds) * Time.deltaTime;
            _currentTime += timeIncrement;
            
            // Wrap time at 24 hours
            if (_currentTime >= 24f)
            {
                _currentTime -= 24f;
                
                // Publish day completed event
                if (_eventBus != null)
                {
                    _eventBus.Publish(new DayCompletedEvent { dayNumber = GetCurrentDay() });
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"[DayNightCycle] Day {GetCurrentDay()} completed!");
                }
            }
            
            // Check for time of day change
            UpdateTimeOfDay();
            
            // Update lighting
            ApplyLightingSettings();
            
            // Update skybox transition
            if (_isTransitioning)
            {
                UpdateSkyboxTransition();
            }
        }
        
        #endregion
        
        #region Time Management
        
        private void UpdateTimeOfDay()
        {
            TimeOfDay newTimeOfDay = config.GetTimeOfDay(_currentTime);
            
            if (newTimeOfDay != _currentTimeOfDay)
            {
                _previousTimeOfDay = _currentTimeOfDay;
                _currentTimeOfDay = newTimeOfDay;
                
                // Start skybox transition
                StartSkyboxTransition();
                
                // Publish event
                if (_eventBus != null)
                {
                    _eventBus.Publish(new TimeOfDayChangedEvent
                    {
                        previousTimeOfDay = _previousTimeOfDay,
                        newTimeOfDay = _currentTimeOfDay,
                        currentTime = _currentTime
                    });
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"[DayNightCycle] Time changed: {_previousTimeOfDay} → {_currentTimeOfDay} ({_currentTime:F1}h)");
                }
            }
        }
        
        private int GetCurrentDay()
        {
            return Mathf.FloorToInt(Time.time / config.dayDurationInSeconds);
        }
        
        #endregion
        
        #region Lighting
        
        private void ApplyLightingSettings()
        {
            if (directionalLight == null) return;
            
            var settings = GetLightingSettingsForTime();
            
            // Smooth interpolation for gradual changes
            directionalLight.color = Color.Lerp(directionalLight.color, settings.lightColor, Time.deltaTime * 2f);
            directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, settings.intensity, Time.deltaTime * 2f);
            
            // Rotate light (sun/moon movement)
            Quaternion targetRotation = Quaternion.Euler(settings.rotation);
            directionalLight.transform.rotation = Quaternion.Slerp(
                directionalLight.transform.rotation,
                targetRotation,
                Time.deltaTime * 0.5f
            );
            
            // Ambient lighting
            RenderSettings.ambientLight = Color.Lerp(
                RenderSettings.ambientLight,
                settings.ambientColor,
                Time.deltaTime * 2f
            );
            
            // Ambient intensity
            RenderSettings.ambientIntensity = Mathf.Lerp(
                RenderSettings.ambientIntensity,
                settings.ambientIntensity,
                Time.deltaTime * 2f
            );
            
            // Fog (optional)
            if (config.useFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(
                    RenderSettings.fogColor,
                    settings.fogColor,
                    Time.deltaTime * 2f
                );
                RenderSettings.fogDensity = Mathf.Lerp(
                    RenderSettings.fogDensity,
                    settings.fogDensity,
                    Time.deltaTime * 2f
                );
            }
        }
        
        private (Color lightColor, float intensity, Vector3 rotation, Color ambientColor, float ambientIntensity, Color fogColor, float fogDensity) GetLightingSettingsForTime()
        {
            // Return settings based on current time of day
            switch (_currentTimeOfDay)
            {
                case TimeOfDay.Morning:
                    return (
                        config.morningLightColor,
                        config.morningLightIntensity,
                        config.morningSunRotation,
                        config.morningAmbientColor,
                        config.morningAmbientIntensity,
                        config.morningFogColor,
                        config.morningFogDensity
                    );
                    
                case TimeOfDay.Day:
                    return (
                        config.dayLightColor,
                        config.dayLightIntensity,
                        config.daySunRotation,
                        config.dayAmbientColor,
                        config.dayAmbientIntensity,
                        config.dayFogColor,
                        config.dayFogDensity
                    );
                    
                case TimeOfDay.Evening:
                    return (
                        config.eveningLightColor,
                        config.eveningLightIntensity,
                        config.eveningSunRotation,
                        config.eveningAmbientColor,
                        config.eveningAmbientIntensity,
                        config.eveningFogColor,
                        config.eveningFogDensity
                    );
                    
                case TimeOfDay.Night:
                    return (
                        config.nightLightColor,
                        config.nightLightIntensity,
                        config.nightMoonRotation,
                        config.nightAmbientColor,
                        config.nightAmbientIntensity,
                        config.nightFogColor,
                        config.nightFogDensity
                    );
                    
                default:
                    return (Color.white, 1f, Vector3.zero, Color.gray, 1f, Color.white, 0.01f);
            }
        }
        
        #endregion
        
        #region Skybox Transition
        
        private void StartSkyboxTransition()
        {
            _isTransitioning = true;
            _skyboxBlendProgress = 0f;
            
            // Use SkyboxBlender for smooth transitions if available
            if (skyboxBlender != null)
            {
                Material fromSkybox = config.GetSkyboxForTime(_previousTimeOfDay);
                Material toSkybox = config.GetSkyboxForTime(_currentTimeOfDay);
                skyboxBlender.StartBlend(fromSkybox, toSkybox);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Starting skybox transition from {_previousTimeOfDay} to {_currentTimeOfDay}");
            }
        }
        
        private void UpdateSkyboxTransition()
        {
            _skyboxBlendProgress += Time.deltaTime / config.skyboxTransitionDuration;
            
            if (_skyboxBlendProgress >= 1f)
            {
                // Transition complete
                _isTransitioning = false;
                
                // Finish blend with SkyboxBlender if available
                if (skyboxBlender != null)
                {
                    skyboxBlender.FinishBlend(config.GetSkyboxForTime(_currentTimeOfDay));
                }
                else
                {
                    RenderSettings.skybox = config.GetSkyboxForTime(_currentTimeOfDay);
                    DynamicGI.UpdateEnvironment();
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"[DayNightCycle] Skybox transition complete");
                }
                return;
            }
            
            // Update blend progress using SkyboxBlender for smooth transitions
            if (skyboxBlender != null)
            {
                skyboxBlender.SetBlend(_skyboxBlendProgress);
            }
            else
            {
                // Fallback: Simple cross-fade at 50% progress
                if (_skyboxBlendProgress >= 0.5f && RenderSettings.skybox != config.GetSkyboxForTime(_currentTimeOfDay))
                {
                    RenderSettings.skybox = config.GetSkyboxForTime(_currentTimeOfDay);
                    DynamicGI.UpdateEnvironment();
                }
            }
        }
        
        #endregion
    }
}
