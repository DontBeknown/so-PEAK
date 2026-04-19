using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound;
using Game.Sound.Events;
using UnityEngine;

namespace Game.Environment.Tornado
{
    public enum TornadoPhase
    {
        Warning,
        Action,
        Ended
    }

    /// <summary>
    /// Controls the lifecycle and phase transitions of a tornado.
    /// Manages camera shake, sounds, and GameObject toggling across Warning and Action phases.
    /// </summary>
    [DisallowMultipleComponent]
    public class TornadoPhaseController : MonoBehaviour
    {
        #region Serialized Fields - Phases
        [Header("Phases")]
        [SerializeField, Min(0.1f)] private float phase1DurationSeconds = 5f;
        [SerializeField, Min(0.1f)] private float phase2DurationSeconds = 6f;
        [SerializeField, Min(0f)] private float delayBeforeDestroy = 2f;
        [SerializeField] private bool destroyOnEnd = true;

        [Header("Phase 1 & 2 GameObjects")]
        [SerializeField] private GameObject tornadoVisuals;
        [SerializeField] private List<GameObject> gameObjectsToToggleOnPhase2 = new List<GameObject>();
        #endregion

        #region Serialized Fields - Camera Shake
        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float phase1ShakeAmplitude = 0.3f;
        [SerializeField, Min(0.01f)] private float phase1ShakeDuration = 0.1f;
        [SerializeField, Min(0f)] private float phase2ShakeAmplitude = 0.6f;
        [SerializeField, Min(0.01f)] private float phase2ShakeDuration = 0.15f;
        #endregion

        #region Serialized Fields - Sounds
        [Header("Sounds")]
        [SerializeField] private string phase1SoundId = "tornado_warning";
        [SerializeField, Min(0f)] private float phase1SoundVolumeScale = 1f;
        [SerializeField] private string phase2SoundId = "tornado_active";
        [SerializeField, Min(0f)] private float phase2SoundVolumeScale = 1f;

        [Header("Tornado Sound Radius")]
        [SerializeField, Min(0f)] private float tornadoSoundMinDistance = 5f;
        [SerializeField, Min(0f)] private float tornadoSoundMaxDistance = 60f;
        #endregion

        #region Serialized Fields - Weather
        [Header("Weather Effects")]
        [SerializeField] private TornadoConfig tornadoConfig;
        #endregion

        #region Runtime State
        private TornadoPhase _currentPhase = TornadoPhase.Ended;
        private float _phaseTimer = 0f;
        private bool _isSoundPlaying = false;
        private bool _isActionShakeActive = false;
        private int _activeSoundHandle = -1;
        private bool _isRiskTrackingActive = false;
        private bool _wasRiskEncountered = false;
        private bool _hasSubmittedRiskEvent = false;
        private Vector3 _riskStartPosition;
        private float _riskStartTimestamp;
        private Vector3 _riskEncounterPosition;
        private float _riskEncounterTimestamp;
        private float _riskEncounterSeverity;

        private CinemachinePlayerCamera _playerCameraCache;
        private IEventBus _eventBusCache;
        private SoundService _soundServiceCache;
        private PlayerStatsTrackerService _statsTrackerCache;

        // Weather transition state
        private Color _baselineLightColor;
        private float _baselineLightIntensity;
        private Color _baselineAmbientColor;
        private float _baselineAmbientIntensity;
        private Color _baselineFogColor;
        private float _baselineFogDensity;
        private Tween _weatherTransitionTween;

        public TornadoPhase CurrentPhase => _currentPhase;
        #endregion

        #region Lazy Service Resolution

        /// <summary>
        /// Lazily resolves and caches the player camera on first access.
        /// </summary>
        private CinemachinePlayerCamera PlayerCamera
        {
            get
            {
                if (_playerCameraCache == null)
                {
                    _playerCameraCache = ServiceContainer.Instance?.TryGet<CinemachinePlayerCamera>();
                }
                return _playerCameraCache;
            }
        }

        /// <summary>
        /// Lazily resolves and caches the event bus on first access.
        /// </summary>
        private IEventBus EventBus
        {
            get
            {
                if (_eventBusCache == null)
                {
                    _eventBusCache = ServiceContainer.Instance?.TryGet<IEventBus>();
                }
                return _eventBusCache;
            }
        }

        private SoundService SoundService
        {
            get
            {
                if (_soundServiceCache == null)
                {
                    _soundServiceCache = ServiceContainer.Instance?.TryGet<SoundService>();
                }
                return _soundServiceCache;
            }
        }

        private PlayerStatsTrackerService StatsTracker
        {
            get
            {
                if (_statsTrackerCache == null)
                {
                    _statsTrackerCache = ServiceContainer.Instance?.TryGet<PlayerStatsTrackerService>();
                }
                return _statsTrackerCache;
            }
        }

        #endregion

        private void Start()
        {
            tornadoVisuals.SetActive(false);
        }

        private void OnDisable()
        {
            FinalizeRiskEvent();
        }

        private void Update()
        {
            // Update phase timer and transitions
            if (_currentPhase == TornadoPhase.Warning)
            {
                _phaseTimer += Time.deltaTime;
                if (_phaseTimer >= phase1DurationSeconds)
                {
                    TransitionToActionPhase();
                }
            }
            else if (_currentPhase == TornadoPhase.Action)
            {
                _phaseTimer += Time.deltaTime;
                if (_phaseTimer >= phase2DurationSeconds)
                {
                    EndTornado();
                }
            }
        }

        #region Phase Lifecycle Methods

        [ContextMenu("Start Warning Phase")]
        public void StartWarningPhase()
        {
            _currentPhase = TornadoPhase.Warning;
            _phaseTimer = 0f;
            _isActionShakeActive = false;
            BeginRiskTracking();

            // EndTornado disables the collider; re-enable it for re-use.
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }

            tornadoVisuals.SetActive(true);
            
            // Toggle GameObjects to inactive during warning phase
            foreach (GameObject obj in gameObjectsToToggleOnPhase2)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }

            // Keep shake off until players are in pull range
            if (PlayerCamera != null)
            {
                PlayerCamera.StopAndReset();
            }

            // Capture baseline lighting and transition to tornado weather
            CaptureBaselineLightingSettings();
            TransitionToTornadoWeather();
            
            // Publish event to notify DayNightCycleManager
            if (EventBus != null)
            {
                EventBus.Publish(new TornadoStartedEvent());
            }

            // Play phase 1 sound
            PlayPhaseSound(phase1SoundId, phase1SoundVolumeScale);
        }

        private void TransitionToActionPhase()
        {
            _currentPhase = TornadoPhase.Action;
            _phaseTimer = 0f;

            // Toggle GameObjects to active during action phase
            foreach (GameObject obj in gameObjectsToToggleOnPhase2)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }

            // Stop phase 1 sound, start phase 2 sound
            StopPhaseSound();
            PlayPhaseSound(phase2SoundId, phase2SoundVolumeScale);

            _isActionShakeActive = false;
        }

        public void SetActionShakeActive(bool active)
        {
            if (_currentPhase != TornadoPhase.Action)
            {
                active = false;
            }

            if (_isActionShakeActive == active)
            {
                return;
            }

            _isActionShakeActive = active;
            if (PlayerCamera == null)
            {
                return;
            }

            if (active)
            {
                PlayerCamera.TransitionShake(phase2ShakeAmplitude, phase2ShakeDuration);
            }
            else
            {
                PlayerCamera.TransitionShake(0f, phase2ShakeDuration);
            }
        }

        /// <summary>
        /// Ends the tornado, cleans up resources, and schedules destruction.
        /// </summary>
        public void EndTornado()
        {
            FinalizeRiskEvent();
            _currentPhase = TornadoPhase.Ended;
            _isActionShakeActive = false;

            // Stop all sounds
            StopPhaseSound();

            // Ensure phase-2 objects are disabled when tornado ends
            foreach (GameObject obj in gameObjectsToToggleOnPhase2)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }

            // Stop camera shake
            if (PlayerCamera != null)
            {
                PlayerCamera.StopAndReset();
            }

            // Disable trigger collision so no new players register
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            // Transition weather back to baseline and publish event
            TransitionBackToBaseline();
            if (EventBus != null)
            {
                EventBus.Publish(new TornadoEndedEvent());
            }

            // Schedule destruction
            StartCoroutine(DestroyAfterDelay());
        }

        public void RegisterHazardEncounter(Vector3 position, float severity)
        {
            if (!_isRiskTrackingActive)
            {
                return;
            }

            if (!_wasRiskEncountered)
            {
                _riskEncounterPosition = position;
                _riskEncounterTimestamp = Time.time;
            }

            _wasRiskEncountered = true;
            _riskEncounterSeverity = Mathf.Max(_riskEncounterSeverity, Mathf.Clamp01(severity));
        }

        private void BeginRiskTracking()
        {
            FinalizeRiskEvent();

            _isRiskTrackingActive = true;
            _wasRiskEncountered = false;
            _hasSubmittedRiskEvent = false;
            _riskStartPosition = transform.position;
            _riskStartTimestamp = Time.time;
            _riskEncounterPosition = _riskStartPosition;
            _riskEncounterTimestamp = _riskStartTimestamp;
            _riskEncounterSeverity = 0f;
        }

        private void FinalizeRiskEvent()
        {
            if (!_isRiskTrackingActive || _hasSubmittedRiskEvent)
            {
                return;
            }

            if (StatsTracker != null)
            {
                RiskEvent riskEvent = new RiskEvent
                {
                    riskType = RiskType.WeatherHazard,
                    location = _wasRiskEncountered ? _riskEncounterPosition : _riskStartPosition,
                    timestamp = _wasRiskEncountered ? _riskEncounterTimestamp : Time.time,
                    wasEncountered = _wasRiskEncountered,
                    severity = _wasRiskEncountered ? _riskEncounterSeverity : 0f
                };

                StatsTracker.RegisterRiskEvent(riskEvent);
            }

            _hasSubmittedRiskEvent = true;
            _isRiskTrackingActive = false;
        }

        private IEnumerator DestroyAfterDelay()
        {
            yield return new WaitForSeconds(delayBeforeDestroy);
            if (destroyOnEnd)
            {
                Destroy(gameObject);
            }
            else
            {
                tornadoVisuals.SetActive(false);
            }
        }

        #endregion

        #region Sound Management

        private void PlayPhaseSound(string soundId, float volumeScale)
        {
            StopPhaseSound();

            if (string.IsNullOrWhiteSpace(soundId))
            {
                return;
            }

            if (SoundService != null)
            {
                _activeSoundHandle = SoundService.PlayPositionalSFXTracked(
                    soundId,
                    transform.position,
                    loop: true,
                    volumeScale: volumeScale,
                    minDistanceOverride: tornadoSoundMinDistance,
                    maxDistanceOverride: tornadoSoundMaxDistance);
                _isSoundPlaying = _activeSoundHandle > 0;
                return;
            }

            if (EventBus == null)
            {
                return;
            }

            Debug.Log($"Playing tornado phase sound: {soundId} at position {transform.position}");
            EventBus.Publish(new PlayPositionalSFXEvent(
                soundId,
                transform.position,
                volumeScale,
                tornadoSoundMinDistance,
                tornadoSoundMaxDistance));
            _isSoundPlaying = true;
        }

        private void StopPhaseSound()
        {
            if (_activeSoundHandle > 0 && SoundService != null)
            {
                SoundService.StopPositionalSFX(_activeSoundHandle);
                _activeSoundHandle = -1;
            }

            _isSoundPlaying = false;
        }

        #endregion

        #region Weather Management

        /// <summary>
        /// Captures the current lighting and fog settings to restore after tornado ends.
        /// </summary>
        private void CaptureBaselineLightingSettings()
        {
            Light directionalLight = RenderSettings.sun;
            if (directionalLight != null)
            {
                _baselineLightColor = directionalLight.color;
                _baselineLightIntensity = directionalLight.intensity;
            }

            _baselineAmbientColor = RenderSettings.ambientLight;
            _baselineAmbientIntensity = RenderSettings.ambientIntensity;
            _baselineFogColor = RenderSettings.fogColor;
            _baselineFogDensity = RenderSettings.fogDensity;
        }

        /// <summary>
        /// Smoothly transitions lighting and fog to tornado weather settings over transitionDuration.
        /// </summary>
        private void TransitionToTornadoWeather()
        {
            if (tornadoConfig == null)
            {
                Debug.LogWarning("[TornadoPhaseController] TornadoConfig not assigned!");
                return;
            }

            float duration = tornadoConfig.transitionDuration;
            Light directionalLight = RenderSettings.sun;

            // Kill any in-flight tweens first
            _weatherTransitionTween?.Kill();

            // Create new sequence for all weather transitions
            Sequence weatherSeq = DOTween.Sequence();

            // Transition directional light
            if (directionalLight != null)
            {
                weatherSeq.Join(
                    directionalLight.DOColor(tornadoConfig.warningPhaseLightColor, duration)
                        .SetEase(Ease.InOutSine));
                weatherSeq.Join(
                    directionalLight.DOIntensity(tornadoConfig.warningPhaseLightIntensity, duration)
                        .SetEase(Ease.InOutSine));
            }

            // Transition ambient light
            weatherSeq.Join(
                DOTween.To(() => RenderSettings.ambientLight,
                           x => RenderSettings.ambientLight = x,
                           tornadoConfig.warningPhaseAmbientColor, duration)
                    .SetEase(Ease.InOutSine));
            
            weatherSeq.Join(
                DOTween.To(() => RenderSettings.ambientIntensity,
                           x => RenderSettings.ambientIntensity = x,
                           tornadoConfig.warningPhaseAmbientIntensity, duration)
                    .SetEase(Ease.InOutSine));

            // Transition fog
            if (tornadoConfig.useFogOverride)
            {
                RenderSettings.fog = true;
                
                weatherSeq.Join(
                    DOTween.To(() => RenderSettings.fogColor,
                               x => RenderSettings.fogColor = x,
                               tornadoConfig.warningPhaseFogColor, duration)
                        .SetEase(Ease.InOutSine));
                
                weatherSeq.Join(
                    DOTween.To(() => RenderSettings.fogDensity,
                               x => RenderSettings.fogDensity = x,
                               tornadoConfig.warningPhaseFogDensity, duration)
                        .SetEase(Ease.InOutSine));
            }

            weatherSeq.SetLink(gameObject);
            _weatherTransitionTween = weatherSeq;
        }

        /// <summary>
        /// Smoothly transitions weather back to baseline settings over transitionDuration.
        /// </summary>
        private void TransitionBackToBaseline()
        {
            if (tornadoConfig == null) return;

            float duration = tornadoConfig.transitionDuration;
            Light directionalLight = RenderSettings.sun;

            // Kill any in-flight tweens first
            _weatherTransitionTween?.Kill();

            // Create new sequence for all weather transitions
            Sequence weatherSeq = DOTween.Sequence();

            // Transition directional light back to baseline
            if (directionalLight != null)
            {
                weatherSeq.Join(
                    directionalLight.DOColor(_baselineLightColor, duration)
                        .SetEase(Ease.InOutSine));
                weatherSeq.Join(
                    directionalLight.DOIntensity(_baselineLightIntensity, duration)
                        .SetEase(Ease.InOutSine));
            }

            // Transition ambient light back to baseline
            weatherSeq.Join(
                DOTween.To(() => RenderSettings.ambientLight,
                           x => RenderSettings.ambientLight = x,
                           _baselineAmbientColor, duration)
                    .SetEase(Ease.InOutSine));
            
            weatherSeq.Join(
                DOTween.To(() => RenderSettings.ambientIntensity,
                           x => RenderSettings.ambientIntensity = x,
                           _baselineAmbientIntensity, duration)
                    .SetEase(Ease.InOutSine));

            // Transition fog back to baseline
            weatherSeq.Join(
                DOTween.To(() => RenderSettings.fogColor,
                           x => RenderSettings.fogColor = x,
                           _baselineFogColor, duration)
                    .SetEase(Ease.InOutSine));
            
            weatherSeq.Join(
                DOTween.To(() => RenderSettings.fogDensity,
                           x => RenderSettings.fogDensity = x,
                           _baselineFogDensity, duration)
                    .SetEase(Ease.InOutSine));

            weatherSeq.SetLink(gameObject);
            _weatherTransitionTween = weatherSeq;
        }

        #endregion
    }
}
