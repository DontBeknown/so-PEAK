using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;
using DG.Tweening;

/// <summary>
/// Plays a heartbeat sound when the player's health is critically low.
/// Volume and interval scale with how low health is — heartbeat quickens and
/// grows louder as health approaches zero.
/// </summary>
public class LowHealthHeartbeatFeedback : MonoBehaviour
{
    [Header("Health Thresholds")]
    [Tooltip("Health percent below which heartbeat starts (0–1).")]
    [SerializeField] private float heartbeatThreshold = 0.4f;
    [Tooltip("Health percent at which heartbeat fully stops. Should be >= heartbeatThreshold.")]
    [SerializeField] private float recoveryThreshold = 0.45f;

    [Header("Heartbeat Intervals")]
    [Tooltip("Seconds between beats at the threshold (low urgency).")]
    [SerializeField] private float maxBeatInterval = 1.2f;
    [Tooltip("Seconds between beats at near-zero health (high urgency).")]
    [SerializeField] private float minBeatInterval = 0.5f;

    [Header("Sound")]
    [SerializeField] private string heartbeatSoundId = "heartbeat";
    [Tooltip("Volume at the health threshold (low urgency).")]
    [SerializeField] private float minVolumeScale = 0.4f;
    [Tooltip("Volume at near-zero health (high urgency).")]
    [SerializeField] private float maxVolumeScale = 1f;

    [Header("Post-Process Volume")]
    [Tooltip("VolumeProfile to fade in when health is low (e.g. vignette, color grading).")]
    [SerializeField] private VolumeProfile lowHealthVolumeProfile;
    [SerializeField] private int volumePriority = 1050;
    [SerializeField] private float volumeFadeInDuration = 1f;
    [SerializeField] private float volumeFadeOutDuration = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Runtime
    private PlayerStats playerStats;
    private IEventBus eventBus;
    private Coroutine heartbeatCoroutine;
    private bool isBeating;
    private GameObject volumeGameObject;
    private Volume lowHealthVolume;
    private Tween volumeTween;

    private void Start()
    {
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("LowHealthHeartbeatFeedback: PlayerStats not found in ServiceContainer!");
            enabled = false;
            return;
        }

        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

        CreateLowHealthVolume();
        playerStats.OnHealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= OnHealthChanged;

        StopHeartbeat();
        volumeTween?.Kill();

        if (volumeGameObject != null)
            Destroy(volumeGameObject);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (max <= 0f) return;
        float percent = current / max;

        if (!isBeating && percent < heartbeatThreshold)
        {
            StartHeartbeat();
        }
        else if (isBeating && percent >= recoveryThreshold)
        {
            StopHeartbeat();
        }

        UpdateVolumeWeight(percent);
    }

    private void StartHeartbeat()
    {
        if (isBeating) return;
        isBeating = true;
        heartbeatCoroutine = StartCoroutine(HeartbeatLoop());

        if (enableDebugLogs)
            Debug.Log("LowHealthHeartbeatFeedback: Heartbeat started.");
    }

    private void StopHeartbeat()
    {
        if (!isBeating) return;
        isBeating = false;

        if (heartbeatCoroutine != null)
        {
            StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }

        if (enableDebugLogs)
            Debug.Log("LowHealthHeartbeatFeedback: Heartbeat stopped.");
    }

    private void CreateLowHealthVolume()
    {
        if (lowHealthVolumeProfile == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("LowHealthHeartbeatFeedback: No lowHealthVolumeProfile assigned — volume effect disabled.");
            return;
        }

        volumeGameObject = new GameObject("LowHealthVolume");
        volumeGameObject.transform.SetParent(transform);

        lowHealthVolume = volumeGameObject.AddComponent<Volume>();
        lowHealthVolume.isGlobal = true;
        lowHealthVolume.priority = volumePriority;
        lowHealthVolume.weight = 0f;
        lowHealthVolume.profile = lowHealthVolumeProfile;

        if (enableDebugLogs)
            Debug.Log($"LowHealthHeartbeatFeedback: Created low-health volume with profile '{lowHealthVolumeProfile.name}'.");
    }

    private void UpdateVolumeWeight(float healthPercent)
    {
        if (lowHealthVolume == null) return;

        float targetWeight = healthPercent < heartbeatThreshold
            ? Mathf.InverseLerp(heartbeatThreshold, 0f, healthPercent)
            : 0f;

        float duration = targetWeight > lowHealthVolume.weight ? volumeFadeInDuration : volumeFadeOutDuration;

        volumeTween?.Kill();
        volumeTween = DOTween.To(
            () => lowHealthVolume.weight,
            val => lowHealthVolume.weight = val,
            targetWeight,
            duration
        ).SetEase(targetWeight > 0f ? Ease.InQuad : Ease.OutQuad).SetUpdate(true);
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            float healthPercent = (playerStats.MaxHealth > 0f)
                ? playerStats.HealthPercent
                : 0f;

            // 0 = at threshold (calm), 1 = near death (urgent)
            float urgency = Mathf.InverseLerp(heartbeatThreshold, 0f, healthPercent);

            float volume = Mathf.Lerp(minVolumeScale, maxVolumeScale, urgency);
            float interval = Mathf.Lerp(maxBeatInterval, minBeatInterval, urgency);

            eventBus?.Publish(new PlayPositionalSFXEvent(heartbeatSoundId, transform.position, volume));

            if (enableDebugLogs)
                Debug.Log($"LowHealthHeartbeatFeedback: vol={volume:F2} next in {interval:F2}s (health={healthPercent:P0})");

            yield return new WaitForSeconds(interval);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Heartbeat (low urgency)")]
    private void TestLow()
    {
        eventBus ??= ServiceContainer.Instance?.TryGet<IEventBus>();
        eventBus?.Publish(new PlayPositionalSFXEvent(heartbeatSoundId, transform.position, minVolumeScale));
    }

    [ContextMenu("Test Heartbeat (high urgency)")]
    private void TestHigh()
    {
        eventBus ??= ServiceContainer.Instance?.TryGet<IEventBus>();
        eventBus?.Publish(new PlayPositionalSFXEvent(heartbeatSoundId, transform.position, maxVolumeScale));
    }
#endif
}
