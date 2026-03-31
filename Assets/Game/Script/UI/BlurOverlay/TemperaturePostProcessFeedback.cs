using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using Game.Core.DI;

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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private PlayerStats playerStats;

    private float coldPenaltyThreshold;
    private float hotPenaltyThreshold;
    private float coldDamageThreshold;
    private float hotDamageThreshold;

    private GameObject coldVolumeObject;
    private GameObject hotVolumeObject;
    private Volume coldVolume;
    private Volume hotVolume;

    private Tween coldTween;
    private Tween hotTween;

    private float updateTimer;

    private void Start()
    {
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("TemperaturePostProcessFeedback: PlayerStats not found in ServiceContainer!");
            enabled = false;
            return;
        }

        if (playerStats.Config == null)
        {
            Debug.LogError("TemperaturePostProcessFeedback: PlayerConfig is missing on PlayerStats.");
            enabled = false;
            return;
        }

        coldPenaltyThreshold = playerStats.Config.tempColdHungerPenaltyThreshold;
        hotPenaltyThreshold = playerStats.Config.tempHotThirstPenaltyThreshold;
        coldDamageThreshold = playerStats.Config.tempColdThreshold;
        hotDamageThreshold = playerStats.Config.tempHotThreshold;

        CreateVolumes();

        if (updateFromTemperatureEvent)
        {
            playerStats.OnTemperatureChanged += OnTemperatureChanged;
        }

        RefreshWeights();
    }

    private void Update()
    {
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
        if (playerStats != null)
        {
            playerStats.OnTemperatureChanged -= OnTemperatureChanged;
        }

        coldTween?.Kill();
        hotTween?.Kill();

        if (coldVolumeObject != null)
            Destroy(coldVolumeObject);

        if (hotVolumeObject != null)
            Destroy(hotVolumeObject);
    }

    private void OnTemperatureChanged(float current, float max)
    {
        RefreshWeights();
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

        float coldTargetWeight = CalculateColdWeight(temperature);
        float hotTargetWeight = CalculateHotWeight(temperature);

        TweenVolumeWeight(coldVolume, ref coldTween, coldTargetWeight);
        TweenVolumeWeight(hotVolume, ref hotTween, hotTargetWeight);

        if (enableDebugLogs)
        {
            Debug.Log($"TemperaturePostProcessFeedback: temp={temperature:F1}C cold={coldTargetWeight:F2} hot={hotTargetWeight:F2}");
        }
    }

    private float CalculateColdWeight(float temperature)
    {
        if (temperature >= coldPenaltyThreshold)
            return 0f;

        float minTemp = Mathf.Min(coldDamageThreshold, coldPenaltyThreshold - 0.01f);
        if (Mathf.Approximately(coldPenaltyThreshold, minTemp))
            return 1f;

        return Mathf.Clamp01(Mathf.InverseLerp(coldPenaltyThreshold, minTemp, temperature));
    }

    private float CalculateHotWeight(float temperature)
    {
        if (temperature <= hotPenaltyThreshold)
            return 0f;

        float maxTemp = Mathf.Max(hotDamageThreshold, hotPenaltyThreshold + 0.01f);
        return Mathf.Clamp01(Mathf.InverseLerp(hotPenaltyThreshold, maxTemp, temperature));
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
