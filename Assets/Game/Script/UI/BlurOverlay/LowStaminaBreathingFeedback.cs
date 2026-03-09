using UnityEngine;
using System.Collections;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

/// <summary>
/// Plays a breathing sound when the player's stamina is critically low.
/// Volume and interval scale with how low stamina is.
/// Uses the same IEventBus / PlayPositionalSFXEvent pipeline as footstep/jump sounds.
/// </summary>
public class LowStaminaBreathingFeedback : MonoBehaviour
{
    [Header("Stamina Thresholds")]
    [Tooltip("Stamina percent below which breathing starts (0–1).")]
    [SerializeField] private float breathThreshold = 0.4f;
    [Tooltip("Stamina percent at which breathing fully stops. Should be >= breathThreshold.")]

    [Header("Breathing Intervals")]
    [SerializeField] private float maxBreathInterval = 2.2f;
    [Tooltip("Seconds between breaths at the threshold (low urgency).")]
    [SerializeField] private float minBreathInterval = 1.2f;
    [Tooltip("Seconds between breaths at near-zero stamina (high urgency).")]

    [Header("Sound")]
    [SerializeField] private string breathSoundId = "breathe";
    [Tooltip("Volume at the breath threshold (low urgency).")]
    [SerializeField] private float minVolumeScale = 0.4f;
    [Tooltip("Volume at near-zero stamina (high urgency).")]
    [SerializeField] private float maxVolumeScale = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Runtime
    private PlayerStats playerStats;
    private IEventBus eventBus;
    private Coroutine breathingCoroutine;
    private bool isBreathing;

    private void Start()
    {
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("LowStaminaBreathingFeedback: PlayerStats not found in ServiceContainer!");
            enabled = false;
            return;
        }

        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

        playerStats.OnStaminaChanged += OnStaminaChanged;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnStaminaChanged -= OnStaminaChanged;

        StopBreathing();
    }

    private void OnStaminaChanged(float current, float max)
    {
        if (max <= 0f) return;
        float percent = current / max;

        if (!isBreathing && percent < breathThreshold)
        {
            StartBreathing();
        }
        else if (isBreathing && percent >= breathThreshold)
        {
            StopBreathing();
        }
    }

    private void StartBreathing()
    {
        if (isBreathing) return;
        isBreathing = true;
        breathingCoroutine = StartCoroutine(BreathingLoop());

        if (enableDebugLogs)
            Debug.Log("LowStaminaBreathingFeedback: Breathing started.");
    }

    private void StopBreathing()
    {
        if (!isBreathing) return;
        isBreathing = false;

        if (breathingCoroutine != null)
        {
            StopCoroutine(breathingCoroutine);
            breathingCoroutine = null;
        }

        if (enableDebugLogs)
            Debug.Log("LowStaminaBreathingFeedback: Breathing stopped.");
    }

    private IEnumerator BreathingLoop()
    {
        while (true)
        {
            float staminaPercent = (playerStats.MaxStamina > 0f)
                ? playerStats.StaminaPercent
                : 0f;

            // 0 = at threshold (calm), 1 = empty stamina (urgent)
            float urgency = Mathf.InverseLerp(breathThreshold, 0f, staminaPercent);

            float volume = Mathf.Lerp(minVolumeScale, maxVolumeScale, urgency);
            float interval = Mathf.Lerp(maxBreathInterval, minBreathInterval, urgency);

            eventBus?.Publish(new PlayPositionalSFXEvent(breathSoundId, transform.position, volume));

            if (enableDebugLogs)
                Debug.Log($"LowStaminaBreathingFeedback: vol={volume:F2} next in {interval:F2}s (stamina={staminaPercent:P0})");

            yield return new WaitForSeconds(interval);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test Breathing (low urgency)")]
    private void TestLow()
    {
        eventBus ??= ServiceContainer.Instance?.TryGet<IEventBus>();
        eventBus?.Publish(new PlayPositionalSFXEvent(breathSoundId, transform.position, minVolumeScale));
    }

    [ContextMenu("Test Breathing (high urgency)")]
    private void TestHigh()
    {
        eventBus ??= ServiceContainer.Instance?.TryGet<IEventBus>();
        eventBus?.Publish(new PlayPositionalSFXEvent(breathSoundId, transform.position, maxVolumeScale));
    }
#endif
}
