using UnityEngine;
using UnityEngine.Rendering;
using Unity.Cinemachine;
using Game.Core.DI;
using DG.Tweening;

/// <summary>
/// Fall damage feedback: triggers Cinemachine Basic Multi Channel Perlin camera shake
/// and a post-process volume flash, both scaled by damage received.
/// </summary>
public class FallImpactFeedback : MonoBehaviour
{
    [Header("Camera Shake Settings")]
    [SerializeField] private float maxShakeAmplitude = 3f;
    [SerializeField] private float minDamageForShake = 5f;
    [SerializeField] private float maxDamageForShake = 50f;
    [SerializeField] private float shakeDuration = 0.55f;

    [Header("Post-Process Volume Settings")]
    [Tooltip("VolumeProfile for the fall impact flash (e.g. chromatic aberration, vignette).")]
    [SerializeField] private VolumeProfile impactVolumeProfile;
    [SerializeField] private int volumePriority = 1100;
    [SerializeField] private float volumeHoldDuration = 0.08f;
    [SerializeField] private float volumeFadeOutDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // Runtime
    private PlayerStats playerStats;
    private CinemachineBasicMultiChannelPerlin[] perlinComponents;
    private float currentShakeAmplitude;
    private GameObject volumeGameObject;
    private Volume impactVolume;
    private Tween shakeTween;
    private Tween volumeTween;

    private void Start()
    {
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("FallImpactFeedback: PlayerStats not found in ServiceContainer!");
            enabled = false;
            return;
        }

        playerStats.OnFallDamaged += OnFallDamaged;
        CachePerlinComponents();
        CreateImpactVolume();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnFallDamaged -= OnFallDamaged;

        shakeTween?.Kill();
        volumeTween?.Kill();

        if (volumeGameObject != null)
            Destroy(volumeGameObject);
    }

    private void CachePerlinComponents()
    {
        var cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        var list = new System.Collections.Generic.List<CinemachineBasicMultiChannelPerlin>();
        foreach (var cam in cameras)
        {
            var p = cam.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (p != null) list.Add(p);
        }
        perlinComponents = list.ToArray();

        if (enableDebugLogs)
            Debug.Log($"FallImpactFeedback: Cached {perlinComponents.Length} CinemachineBasicMultiChannelPerlin component(s).");
    }

    private void CreateImpactVolume()
    {
        if (impactVolumeProfile == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("FallImpactFeedback: No impactVolumeProfile assigned — volume flash disabled.");
            return;
        }

        volumeGameObject = new GameObject("FallImpactVolume");
        volumeGameObject.transform.SetParent(transform);

        impactVolume = volumeGameObject.AddComponent<Volume>();
        impactVolume.isGlobal = true;
        impactVolume.priority = volumePriority;
        impactVolume.weight = 0f;
        impactVolume.profile = impactVolumeProfile;

        if (enableDebugLogs)
            Debug.Log($"FallImpactFeedback: Created impact volume with profile '{impactVolumeProfile.name}'.");
    }

    private void OnFallDamaged(float damage)
    {
        float t = Mathf.InverseLerp(minDamageForShake, maxDamageForShake, damage);
        if (t <= 0f) return;

        if (enableDebugLogs)
            Debug.Log($"FallImpactFeedback: Fall damage {damage:F1} → intensity {t:F2}");

        TriggerShake(Mathf.Lerp(0f, maxShakeAmplitude, t));
        TriggerVolumeFlash(1);
    }

    private void TriggerShake(float amplitude)
    {
        if (perlinComponents == null || perlinComponents.Length == 0) return;

        shakeTween?.Kill();
        currentShakeAmplitude = amplitude;
        SetAllAmplitudes(amplitude);

        shakeTween = DOTween.To(
            () => currentShakeAmplitude,
            val => { currentShakeAmplitude = val; SetAllAmplitudes(val); },
            0f,
            shakeDuration
        ).SetEase(Ease.OutCubic).SetUpdate(true);
    }

    private void SetAllAmplitudes(float amplitude)
    {
        foreach (var p in perlinComponents)
            if (p != null) p.AmplitudeGain = amplitude;
    }

    private void TriggerVolumeFlash(float intensity)
    {
        if (impactVolume == null) return;

        volumeTween?.Kill();
        impactVolume.weight = intensity;

        volumeTween = DOTween.Sequence()
            .AppendInterval(volumeHoldDuration)
            .Append(
                DOTween.To(
                    () => impactVolume.weight,
                    val => impactVolume.weight = val,
                    0f,
                    volumeFadeOutDuration
                ).SetEase(Ease.OutQuad)
            )
            .SetUpdate(true);
    }

    #region Editor Helpers
#if UNITY_EDITOR
    [ContextMenu("Test Light Fall (10 dmg)")]
    private void TestLight() => OnFallDamaged(10f);

    [ContextMenu("Test Heavy Fall (40 dmg)")]
    private void TestHeavy() => OnFallDamaged(40f);

    [ContextMenu("Test Max Fall (50+ dmg)")]
    private void TestMax() => OnFallDamaged(50f);
#endif
    #endregion
}
