using UnityEngine;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player.Inventory;
using Game.Player.Inventory.HeldItems;
using Game.Sound;

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
        private int _currentDay = 1;
        private TimeOfDay _currentTimeOfDay;
        private TimeOfDay _previousTimeOfDay;
        private bool _isPaused = false;
        
        // Skybox blending
        private float _skyboxBlendProgress = 0f;
        private bool _isTransitioning = false;
        
        // DOTween lighting transition
        private Sequence _lightingSequence;
        private Tween _torchFogTween;
        
        // Services
        private IEventBus _eventBus;
        private SoundService _soundService;
        private EquipmentManager _equipmentManager;
        private bool _eventsSubscribed;

        // Torch fog override state
        private bool _isTorchEquipped;
        private TorchItem _equippedTorchItem;
        
        #region IDayNightCycleService Implementation
        
        public float CurrentTime => _currentTime;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;
        public float DayProgress => _currentTime / 24f;
        public bool IsPaused => _isPaused;
        public int CurrentDay => _currentDay;
        
        public void SetTime(float hours)
        {
            _currentTime = Mathf.Clamp(hours, 0f, 24f);
            UpdateTimeOfDay();
        }
        
        public void SetDay(int day)
        {
            _currentDay = Mathf.Max(1, day);
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Day set to {_currentDay}");
            }
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
        
        public void SkipToNextMorning()
        {
            _currentDay++;
            _previousTimeOfDay = _currentTimeOfDay;
            _currentTime = config.morningStartHour;
            _currentTimeOfDay = TimeOfDay.Morning;
            
            // Snap lighting instantly (no tween on a time skip)
            _lightingSequence?.Kill();
            ApplyLightingSettingsImmediate();
            StartSkyboxTransition();
            
            // Publish events
            if (_eventBus != null)
            {
                _eventBus.Publish(new DayCompletedEvent { dayNumber = _currentDay });
                _eventBus.Publish(new TimeOfDayChangedEvent
                {
                    previousTimeOfDay = _previousTimeOfDay,
                    newTimeOfDay = _currentTimeOfDay,
                    currentTime = _currentTime
                });
            }

            PlayAmbientForCurrentTime();
            
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Skipped to Day {_currentDay} morning ({config.morningStartHour:F1}h)");
            }
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
            
            // Initialize time
            _currentTime = config.startTime;
            _currentTimeOfDay = config.GetTimeOfDay(_currentTime);
            _previousTimeOfDay = _currentTimeOfDay;
            
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Initialized at {_currentTime:F1}h ({_currentTimeOfDay})");
            }
        }
        
        /// <summary>Called by GameServiceBootstrapper after registration.</summary>
        public void Initialize(IEventBus eventBus, SoundService soundService, EquipmentManager equipmentManager)
        {
            // Initialize can be called multiple times; avoid duplicate subscriptions.
            if (_eventBus != eventBus)
            {
                if (_eventsSubscribed && _eventBus != null)
                {
                    UnsubscribeFromEventBus(_eventBus);
                    _eventsSubscribed = false;
                }

                _eventBus = eventBus;
            }

            _soundService = soundService;
            _equipmentManager = equipmentManager;

            EnsureSubscribedToEvents();
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
            
            // Snap to initial settings instantly — no tween on first frame
            ApplyLightingSettingsImmediate();
            RenderSettings.skybox = config.GetSkyboxForTime(_currentTimeOfDay);
            DynamicGI.UpdateEnvironment();

            _equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
            EnsureSubscribedToEvents();
            RefreshTorchEquippedState();
            EvaluateTorchNightFogOverride();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            _lightingSequence?.Kill();
            _torchFogTween?.Kill();
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
                _currentDay++;
                
                // Publish day completed event
                if (_eventBus != null)
                {
                    _eventBus.Publish(new DayCompletedEvent { dayNumber = _currentDay });
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"[DayNightCycle] Day {_currentDay} started!");
                }
            }
            
            // Check for time of day change (fires DOTween lighting transition on period change)
            UpdateTimeOfDay();
            
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
                
                // Start skybox + lighting DOTween transition
                StartSkyboxTransition();
                StartLightingTransition();

                // Re-evaluate override after base transition starts.
                EvaluateTorchNightFogOverride();

                // Crossfade to the new ambient loop
                PlayAmbientForCurrentTime();
                
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
        
        #endregion
        
        #region Ambient Sound

        public void PlayAmbientForCurrentTime()
        {
            if (_soundService == null) return;

            string clipId = config.GetAmbientClipId(_currentTimeOfDay);
            if (!string.IsNullOrEmpty(clipId))
                _soundService.PlayAmbient(clipId);
        }

        #endregion

        #region Lighting
        
        /// <summary>
        /// Instantly snaps all lighting to the current time-of-day target values.
        /// Used on first frame and when skipping time.
        /// </summary>
        private void ApplyLightingSettingsImmediate()
        {
            if (directionalLight == null) return;
            
            var s = GetLightingSettingsForTime();
            directionalLight.color = s.lightColor;
            directionalLight.intensity = s.intensity;
            directionalLight.transform.rotation = Quaternion.Euler(s.rotation);
            RenderSettings.ambientLight = s.ambientColor;
            RenderSettings.ambientIntensity = s.ambientIntensity;
            
            if (config.useFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = s.fogColor;
                RenderSettings.fogDensity = s.fogDensity;
            }
        }
        
        /// <summary>
        /// Tweens all lighting values to the current time-of-day targets over skyboxTransitionDuration.
        /// Kills any previous lighting tween before starting.
        /// </summary>
        private void StartLightingTransition()
        {
            if (directionalLight == null) return;
            
            var s = GetLightingSettingsForTime();
            float duration = config.skyboxTransitionDuration;
            
            _lightingSequence?.Kill();
            _lightingSequence = DOTween.Sequence().SetLink(gameObject);
            
            // Directional light
            _lightingSequence.Join(directionalLight.DOColor(s.lightColor, duration).SetEase(Ease.InOutSine));
            _lightingSequence.Join(directionalLight.DOIntensity(s.intensity, duration).SetEase(Ease.InOutSine));
            _lightingSequence.Join(directionalLight.transform.DORotate(s.rotation, duration).SetEase(Ease.InOutSine));
            
            // Ambient
            _lightingSequence.Join(
                DOTween.To(() => RenderSettings.ambientLight,
                           x => RenderSettings.ambientLight = x,
                           s.ambientColor, duration).SetEase(Ease.InOutSine));
            _lightingSequence.Join(
                DOTween.To(() => RenderSettings.ambientIntensity,
                           x => RenderSettings.ambientIntensity = x,
                           s.ambientIntensity, duration).SetEase(Ease.InOutSine));
            
            // Fog
            if (config.useFog)
            {
                RenderSettings.fog = true;
                _lightingSequence.Join(
                    DOTween.To(() => RenderSettings.fogColor,
                               x => RenderSettings.fogColor = x,
                               s.fogColor, duration).SetEase(Ease.InOutSine));
                _lightingSequence.Join(
                    DOTween.To(() => RenderSettings.fogDensity,
                               x => RenderSettings.fogDensity = x,
                               s.fogDensity, duration).SetEase(Ease.InOutSine));
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[DayNightCycle] Lighting DOTween transition started ({duration}s)");
            }
        }

        private void EnsureSubscribedToEvents()
        {
            if (_eventsSubscribed || _eventBus == null) return;

            SubscribeToEventBus(_eventBus);
            _eventsSubscribed = true;
        }

        private void SubscribeToEventBus(IEventBus eventBus)
        {
            if (eventBus == null) return;

            eventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped);
            eventBus.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);
            eventBus.Subscribe<TimeOfDayChangedEvent>(OnTimeOfDayChanged);
        }

        private void UnsubscribeFromEvents()
        {
            if (!_eventsSubscribed || _eventBus == null) return;

            UnsubscribeFromEventBus(_eventBus);
            _eventsSubscribed = false;
        }

        private void UnsubscribeFromEventBus(IEventBus eventBus)
        {
            if (eventBus == null) return;

            eventBus.Unsubscribe<ItemEquippedEvent>(OnItemEquipped);
            eventBus.Unsubscribe<ItemUnequippedEvent>(OnItemUnequipped);
            eventBus.Unsubscribe<TimeOfDayChangedEvent>(OnTimeOfDayChanged);
        }

        private void OnItemEquipped(ItemEquippedEvent evt)
        {
            if (evt?.Item is TorchItem)
            {
                _equippedTorchItem = evt.Item as TorchItem;
                _isTorchEquipped = true;
                EvaluateTorchNightFogOverride();
            }
        }

        private void OnItemUnequipped(ItemUnequippedEvent evt)
        {
            if (evt?.Item is TorchItem)
            {
                _equippedTorchItem = null;
                _isTorchEquipped = false;
                EvaluateTorchNightFogOverride();
            }
        }

        private void OnTimeOfDayChanged(TimeOfDayChangedEvent evt)
        {
            EvaluateTorchNightFogOverride();
        }

        private void RefreshTorchEquippedState()
        {
            if (_equipmentManager == null)
            {
                _isTorchEquipped = false;
                _equippedTorchItem = null;
                return;
            }

            _equippedTorchItem = _equipmentManager.GetEquippedItem(EquipmentSlotType.HeldItem) as TorchItem;
            _isTorchEquipped = _equippedTorchItem != null;
        }

        private void EvaluateTorchNightFogOverride()
        {
            if (!config.useFog)
                return;

            TorchItem activeTorch = _isTorchEquipped ? _equippedTorchItem : null;
            bool torchCanOverrideFog = activeTorch != null && activeTorch.UseNightFogOverride;

            bool shouldUseTorchFog = _currentTimeOfDay == TimeOfDay.Night && torchCanOverrideFog;
            float targetDensity = shouldUseTorchFog ? activeTorch.NightFogOverrideDensity : config.nightFogDensity;

            // Outside of night, let regular day/night lighting transition own fog values.
            if (_currentTimeOfDay != TimeOfDay.Night)
                return;

            float fadeDuration = shouldUseTorchFog ? activeTorch.NightFogFadeDuration : 0.5f;
            FadeFogDensityQuick(targetDensity, fadeDuration);

            if (showDebugInfo)
            {
                Debug.Log(shouldUseTorchFog
                    ? $"[DayNightCycle] Torch fog override ON -> {targetDensity:F3}"
                    : $"[DayNightCycle] Torch fog override OFF -> {targetDensity:F3}");
            }
        }

        private void FadeFogDensityQuick(float targetDensity, float fadeDuration)
        {
            _torchFogTween?.Kill();
            _torchFogTween = DOTween.To(() => RenderSettings.fogDensity,
                x => RenderSettings.fogDensity = x,
                targetDensity,
            fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
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
