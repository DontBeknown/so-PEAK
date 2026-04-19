using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using System.Collections;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

/// <summary>
/// Drives separate cold/hot post-process volumes from player temperature.
/// Uses PlayerConfig penalty thresholds and smoothly tweens each volume weight (0..1).
/// </summary>
public class TemperaturePostProcessFeedback : MonoBehaviour
{
    [Header("Volume Profiles")]
    [Tooltip("Post-process profile used when the player is too cold.")]
    [SerializeField] private VolumeProfile coldVolumeProfile;
    [Tooltip("Post-process profile used when the player is too hot.")]
    [SerializeField] private VolumeProfile hotVolumeProfile;
    [SerializeField] private int volumePriority = 1025;

    [Header("Tween Settings")]
    [SerializeField] private float fadeInDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private Ease fadeInEase = Ease.InQuad;
    [SerializeField] private Ease fadeOutEase = Ease.OutQuad;

    [Header("Update Settings")]
    [SerializeField] private bool updateFromTemperatureEvent = true;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float startupDelaySeconds = 1f;

    [Header("UI Feedback")]
    [Tooltip("Cold warning UI CanvasGroup. Alpha is driven by cold weight (0..1).")]
    [SerializeField] private CanvasGroup coldFeedbackCanvasGroup;
    [Tooltip("Hot warning UI CanvasGroup. Alpha is driven by hot weight (0..1).")]
    [SerializeField] private CanvasGroup hotFeedbackCanvasGroup;

    [Header("Temperature SFX")]
    [SerializeField] private string coldPenaltySoundId = "temp_cold_penalty";
    [SerializeField] private float coldPenaltySoundVolume = 0.8f;
    [SerializeField] private string coldDamageSoundId = "temp_cold_damage";
    [SerializeField] private float coldDamageSoundVolume = 1f;
    [SerializeField] private string hotPenaltySoundId = "temp_hot_penalty";
    [SerializeField] private float hotPenaltySoundVolume = 0.8f;
    [SerializeField] private string hotDamageSoundId = "temp_hot_damage";
    [SerializeField] private float hotDamageSoundVolume = 1f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private PlayerStats playerStats;

    private float coldPenaltyThreshold;
    private float hotPenaltyThreshold;
    private float coldDamageThreshold;
    private float hotDamageThreshold;
    private float coldWarningStartThreshold;
    private float hotWarningStartThreshold;

    private GameObject coldVolumeObject;
    private GameObject hotVolumeObject;
    private Volume coldVolume;
    private Volume hotVolume;

    private Tween coldTween;
    private Tween hotTween;

    private IEventBus eventBus;

    private bool wasColdPenaltyActive;
    private bool wasColdDamageActive;
    private bool wasHotPenaltyActive;
    private bool wasHotDamageActive;

    private float updateTimer;
    private bool isFeedbackActive;
    private bool isTemperatureSubscribed;

    private IEnumerator Start()
    {
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("TemperaturePostProcessFeedback: PlayerStats not found in ServiceContainer!");
            enabled = false;
            yield break;
        }

        if (playerStats.Config == null)
        {
            Debug.LogError("TemperaturePostProcessFeedback: PlayerConfig is missing on PlayerStats.");
            enabled = false;
            yield break;
        }

        coldPenaltyThreshold = playerStats.Config.tempColdHungerPenaltyThreshold;
        hotPenaltyThreshold = playerStats.Config.tempHotThirstPenaltyThreshold;
        coldDamageThreshold = playerStats.Config.tempColdThreshold;
        hotDamageThreshold = playerStats.Config.tempHotThreshold;

        float coldWarningOffset = Mathf.Max(0f, playerStats.Config.tempColdWarningOffset);
        float hotWarningOffset = Mathf.Max(0f, playerStats.Config.tempHotWarningOffset);

        coldWarningStartThreshold = Mathf.Max(coldPenaltyThreshold + coldWarningOffset, coldPenaltyThreshold);
        if (coldWarningStartThreshold <= coldDamageThreshold)
            coldWarningStartThreshold = coldDamageThreshold + 0.01f;

        hotWarningStartThreshold = Mathf.Min(hotPenaltyThreshold - hotWarningOffset, hotPenaltyThreshold);
        if (hotWarningStartThreshold >= hotDamageThreshold)
            hotWarningStartThreshold = hotDamageThreshold - 0.01f;

        CreateVolumes();

        float delay = Mathf.Max(0f, startupDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        isFeedbackActive = true;
        if (updateFromTemperatureEvent)
            SubscribeToTemperatureEvent();

        RefreshWeights();
    }

    private void Update()
    {
        if (!isFeedbackActive)
            return;

        if (updateFromTemperatureEvent)
            return;

        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval)
            return;

        updateTimer = 0f;
        RefreshWeights();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTemperatureEvent();

        coldTween?.Kill();
        hotTween?.Kill();

        if (coldVolumeObject != null)
            Destroy(coldVolumeObject);

        if (hotVolumeObject != null)
            Destroy(hotVolumeObject);
    }

    private void OnTemperatureChanged(float current, float max)
    {
        if (!isFeedbackActive)
            return;

        RefreshWeights();
    }

    private void SubscribeToTemperatureEvent()
    {
        if (playerStats == null || isTemperatureSubscribed)
            return;

        playerStats.OnTemperatureChanged += OnTemperatureChanged;
        isTemperatureSubscribed = true;
    }

    private void UnsubscribeFromTemperatureEvent()
    {
        if (playerStats == null || !isTemperatureSubscribed)
            return;

        playerStats.OnTemperatureChanged -= OnTemperatureChanged;
        isTemperatureSubscribed = false;
    }

    private void CreateVolumes()
    {
        if (coldVolumeProfile != null)
        {
            coldVolumeObject = new GameObject("TemperatureColdVolume");
            coldVolumeObject.transform.SetParent(transform);
            coldVolumeObject.transform.localPosition = Vector3.zero;

            coldVolume = coldVolumeObject.AddComponent<Volume>();
            coldVolume.isGlobal = true;
            coldVolume.priority = volumePriority;
            coldVolume.profile = coldVolumeProfile;
            coldVolume.weight = 0f;
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("TemperaturePostProcessFeedback: Cold volume profile is not assigned.");
        }

        if (hotVolumeProfile != null)
        {
            hotVolumeObject = new GameObject("TemperatureHotVolume");
            hotVolumeObject.transform.SetParent(transform);
            hotVolumeObject.transform.localPosition = Vector3.zero;

            hotVolume = hotVolumeObject.AddComponent<Volume>();
            hotVolume.isGlobal = true;
            hotVolume.priority = volumePriority;
            hotVolume.profile = hotVolumeProfile;
            hotVolume.weight = 0f;
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("TemperaturePostProcessFeedback: Hot volume profile is not assigned.");
        }
    }

    private void RefreshWeights()
    {
        if (playerStats == null)
            return;

        float temperature = playerStats.Temperature;

        HandleTemperatureSfx(temperature);

        float coldTargetWeight = CalculateColdWeight(temperature);
        float hotTargetWeight = CalculateHotWeight(temperature);

        TweenVolumeWeight(coldVolume, ref coldTween, coldTargetWeight);
        TweenVolumeWeight(hotVolume, ref hotTween, hotTargetWeight);
        ApplyCanvasGroupAlpha(coldFeedbackCanvasGroup, coldTargetWeight);
        ApplyCanvasGroupAlpha(hotFeedbackCanvasGroup, hotTargetWeight);

        if (enableDebugLogs)
        {
            Debug.Log($"TemperaturePostProcessFeedback: temp={temperature:F1}C cold={coldTargetWeight:F2} hot={hotTargetWeight:F2} coldStart={coldWarningStartThreshold:F1}C hotStart={hotWarningStartThreshold:F1}C");
        }
    }

    private void HandleTemperatureSfx(float temperature)
    {
        bool isColdPenaltyActive = temperature <= coldPenaltyThreshold;
        bool isColdDamageActive = temperature <= coldDamageThreshold;
        bool isHotPenaltyActive = temperature >= hotPenaltyThreshold;
        bool isHotDamageActive = temperature >= hotDamageThreshold;

        if (isColdPenaltyActive && !wasColdPenaltyActive)
        {
            PlayTemperatureSfx(coldPenaltySoundId, coldPenaltySoundVolume);
        }

        if (isColdDamageActive && !wasColdDamageActive)
        {
            PlayTemperatureSfx(coldDamageSoundId, coldDamageSoundVolume);
        }

        if (isHotPenaltyActive && !wasHotPenaltyActive)
        {
            PlayTemperatureSfx(hotPenaltySoundId, hotPenaltySoundVolume);
        }

        if (isHotDamageActive && !wasHotDamageActive)
        {
            PlayTemperatureSfx(hotDamageSoundId, hotDamageSoundVolume);
        }

        wasColdPenaltyActive = isColdPenaltyActive;
        wasColdDamageActive = isColdDamageActive;
        wasHotPenaltyActive = isHotPenaltyActive;
        wasHotDamageActive = isHotDamageActive;
    }

    private void PlayTemperatureSfx(string clipId, float volume)
    {
        if (eventBus == null || string.IsNullOrWhiteSpace(clipId))
            return;

        eventBus.Publish(new PlayPositionalSFXEvent(clipId, transform.position, volume));
    }

    private float CalculateColdWeight(float temperature)
    {
        if (temperature >= coldWarningStartThreshold)
            return 0f;

        float minTemp = Mathf.Min(coldDamageThreshold, coldWarningStartThreshold - 0.01f);
        if (Mathf.Approximately(coldWarningStartThreshold, minTemp))
            return 1f;

        return Mathf.Clamp01(Mathf.InverseLerp(coldWarningStartThreshold, minTemp, temperature));
    }

    private float CalculateHotWeight(float temperature)
    {
        if (temperature <= hotWarningStartThreshold)
            return 0f;

        float maxTemp = Mathf.Max(hotDamageThreshold, hotWarningStartThreshold + 0.01f);
        return Mathf.Clamp01(Mathf.InverseLerp(hotWarningStartThreshold, maxTemp, temperature));
    }

    private static void ApplyCanvasGroupAlpha(CanvasGroup targetCanvasGroup, float alpha)
    {
        if (targetCanvasGroup == null)
            return;

        targetCanvasGroup.alpha = Mathf.Clamp01(alpha);
    }

    private void TweenVolumeWeight(Volume targetVolume, ref Tween activeTween, float targetWeight)
    {
        if (targetVolume == null)
            return;

        targetWeight = Mathf.Clamp01(targetWeight);
        float currentWeight = targetVolume.weight;

        if (Mathf.Approximately(currentWeight, targetWeight))
            return;

        bool isFadingIn = targetWeight > currentWeight;
        float duration = isFadingIn ? fadeInDuration : fadeOutDuration;
        float scaledDuration = Mathf.Max(0.01f, duration * Mathf.Abs(targetWeight - currentWeight));

        activeTween?.Kill();
        activeTween = DOTween.To(
            () => targetVolume.weight,
            w => targetVolume.weight = w,
            targetWeight,
            scaledDuration)
            .SetEase(isFadingIn ? fadeInEase : fadeOutEase)
            .SetUpdate(true);
    }
}
