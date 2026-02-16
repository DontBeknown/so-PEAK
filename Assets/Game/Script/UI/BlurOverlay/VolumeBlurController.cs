using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Game.Core.DI;
using DG.Tweening;

/// <summary>
/// Post-processing volume-based blur controller for survival stats.
/// Creates and manages a Global Volume with Depth of Field and Motion Blur effects.
/// Gradually fades in blur intensity when hunger or thirst reach critical thresholds.
/// </summary>
public class VolumeBlurController : MonoBehaviour
{
    [Header("Calculator Settings")]
    [SerializeField] private float hungerCriticalThreshold = 50;
    [SerializeField] private float thirstCriticalThreshold = 50f;
    [SerializeField] private float hungerSevereThreshold = 20f;
    [SerializeField] private float thirstSevereThreshold = 20f;
    [SerializeField] private float maxBlurIntensity = 1f;
    [SerializeField] private bool useWorstStat = true;
    
    [Header("Effect Settings")]
    [SerializeField] private float fadeInDuration = 0.67f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    
    [Header("Depth of Field Settings")]
    [SerializeField] private float dofFocusDistance = 10f;
    [SerializeField] private float dofMinAperture = 5.6f;
    [SerializeField] private float dofMaxAperture = 32f;
    [SerializeField] private float dofFocalLength = 50f;
    
    [Header("Motion Blur Settings")]
    [SerializeField] private float motionBlurMinIntensity = 0f;
    [SerializeField] private float motionBlurMaxIntensity = 0.5f;
    [SerializeField] private float motionBlurClamp = 0.05f;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Volume Settings")]
    [Tooltip("Optional: Drag your custom VolumeProfile asset here. If empty, creates a new profile at runtime.")]
    [SerializeField] private VolumeProfile customVolumeProfile; // Optional: Use existing profile instead of creating at runtime
    [SerializeField] private int volumePriority = 1000;
    [SerializeField] bool useCustomVolumeProfile = false;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Runtime components
    private GameObject volumeGameObject;
    private Volume volume;
    private VolumeProfile volumeProfile;
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;
    
    // Dependencies
    private SurvivalStatBlurCalculator intensityCalculator;
    private PlayerStats playerStats;
    
    // State
    private float currentWeight = 0f;
    private float targetWeight = 0f;
    private float lastIntensity = 0f;
    private float updateTimer = 0f;
    private Tweener weightTween;
    
    private void Awake()
    {
        // Create Global Volume at runtime (doesn't require PlayerStats)
        CreateVolumeGameObject();
    }
    
    private void Start()
    {
        // Get PlayerStats from ServiceContainer (deferred to Start to ensure registration)
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        
        if (playerStats == null)
        {
            Debug.LogError("VolumeBlurController requires PlayerStats to be registered in ServiceContainer!");
            enabled = false;
            return;
        }
        
        // Initialize intensity calculator (requires PlayerStats)
        InitializeIntensityCalculator();
        
        // Initialize calculator
        intensityCalculator?.Initialize();
        
        // Subscribe to intensity changes
        if (intensityCalculator != null)
        {
            intensityCalculator.OnIntensityChanged += OnIntensityChanged;
        }
        
        // Initial update
        UpdateBlurEffect();
    }
    
    private void Update()
    {
        // Periodically update intensity calculation
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateBlurEffect();
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup tweens
        weightTween?.Kill();
        
        // Unsubscribe from events
        if (intensityCalculator != null)
        {
            intensityCalculator.OnIntensityChanged -= OnIntensityChanged;
        }
        
        intensityCalculator?.Cleanup();
        
        // Destroy created volume GameObject
        if (volumeGameObject != null)
        {
            Destroy(volumeGameObject);
        }
        
        // Only destroy runtime-created profile (not custom assets)
        if (volumeProfile != null && customVolumeProfile == null)
        {
            Destroy(volumeProfile);
        }
    }
    
    /// <summary>
    /// Creates a Global Volume GameObject at runtime with Depth of Field and Motion Blur.
    /// </summary>
    private void CreateVolumeGameObject()
    {
        // Create GameObject
        volumeGameObject = new GameObject("SurvivalBlurVolume");
        volumeGameObject.transform.SetParent(transform);
        volumeGameObject.transform.localPosition = Vector3.zero;
        
        // Add Volume component
        volume = volumeGameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = volumePriority;
        volume.weight = 0f; // Start invisible
        
        // Use custom VolumeProfile if provided, otherwise create new one at runtime
        if (customVolumeProfile != null)
        {
            volume.profile = customVolumeProfile;
            volumeProfile = customVolumeProfile;
            if (enableDebugLogs)
                Debug.Log($"VolumeBlurController: Using custom VolumeProfile '{customVolumeProfile.name}'");
        }
        else
        {
            // Create VolumeProfile at runtime
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volumeProfile.name = "SurvivalBlurProfile";
            volume.profile = volumeProfile;
            if (enableDebugLogs)
                Debug.Log("VolumeBlurController: Created runtime VolumeProfile");
        }
        
        // Add or get Depth of Field override
        if (volumeProfile.TryGet(out depthOfField))
        {
            if (enableDebugLogs)
                Debug.Log("VolumeBlurController: Using existing Depth of Field from profile");
        }
        else
        {
            depthOfField = volumeProfile.Add<DepthOfField>(false);
            if (enableDebugLogs)
                Debug.Log("VolumeBlurController: Added Depth of Field to profile");
        }
        
        if(useCustomVolumeProfile)
        {
            return;
        }
        
        ConfigureDepthOfField();
        
        // Add or get Motion Blur override
        if (volumeProfile.TryGet(out motionBlur))
        {
            if (enableDebugLogs)
                Debug.Log("VolumeBlurController: Using existing Motion Blur from profile");
        }
        else
        {
            motionBlur = volumeProfile.Add<MotionBlur>(false);
            if (enableDebugLogs)
                Debug.Log("VolumeBlurController: Added Motion Blur to profile");
        }
        
        ConfigureMotionBlur();
        
        if (enableDebugLogs)
            Debug.Log($"VolumeBlurController: Created Global Volume '{volumeGameObject.name}' with priority {volumePriority}");
    }
    
    /// <summary>
    /// Configures Depth of Field effect parameters.
    /// </summary>
    private void ConfigureDepthOfField()
    {
        if (depthOfField == null) return;
        
        depthOfField.active = true;
        
        // Set mode to Bokeh for realistic blur
        depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
        
        // Focus distance (far focus = blur entire screen when active)
        depthOfField.focusDistance.Override(dofFocusDistance);
        
        // Aperture controls blur amount - will be animated based on intensity
        depthOfField.aperture.Override(dofMinAperture);
        
        // Focal length
        depthOfField.focalLength.Override(dofFocalLength);
        
        // Blade count for bokeh shape (higher = more circular)
        depthOfField.bladeCount.Override(5);
        
        // Blade curvature
        depthOfField.bladeCurvature.Override(1f);
        
        // Blade rotation
        depthOfField.bladeRotation.Override(0f);
    }
    
    /// <summary>
    /// Configures Motion Blur effect parameters.
    /// </summary>
    private void ConfigureMotionBlur()
    {
        if (motionBlur == null) return;
        
        motionBlur.active = true;
        
        // Set mode to Camera and Objects
        motionBlur.mode.Override(MotionBlurMode.CameraAndObjects);
        
        // Quality (higher = better but more expensive)
        motionBlur.quality.Override(MotionBlurQuality.Medium);
        
        // Intensity - will be animated based on blur intensity
        motionBlur.intensity.Override(motionBlurMinIntensity);
        
        // Clamp to prevent excessive smearing
        motionBlur.clamp.Override(motionBlurClamp);
    }
    
    /// <summary>
    /// Initializes the intensity calculator with configuration.
    /// </summary>
    private void InitializeIntensityCalculator()
    {
        intensityCalculator = new SurvivalStatBlurCalculator(playerStats);
        intensityCalculator.SetHungerThresholds(hungerCriticalThreshold, hungerSevereThreshold);
        intensityCalculator.SetThirstThresholds(thirstCriticalThreshold, thirstSevereThreshold);
        intensityCalculator.SetIntensityRange(0f, maxBlurIntensity);
        intensityCalculator.SetUseWorstStat(useWorstStat);
    }
    
    /// <summary>
    /// Updates the blur effect based on current intensity calculation.
    /// </summary>
    private void UpdateBlurEffect()
    {
        if (intensityCalculator == null || volume == null)
            return;
        
        // Update calculator to get latest intensity
        intensityCalculator.UpdateIntensity();
        
        float targetIntensity = intensityCalculator.CalculateIntensity();
        
        // Apply to effect if changed
        if (!Mathf.Approximately(targetIntensity, lastIntensity))
        {
            bool isFadingIn = targetIntensity > lastIntensity;
            SetTargetIntensityInternal(targetIntensity, isFadingIn);
            lastIntensity = targetIntensity;
            
            if (enableDebugLogs)
                Debug.Log($"VolumeBlurController: Intensity changed to {targetIntensity:F2}, {(isFadingIn ? "fading in" : "fading out")}");
        }
    }
    
    /// <summary>
    /// Callback for intensity changes from calculator.
    /// </summary>
    private void OnIntensityChanged(float newIntensity)
    {
        bool isFadingIn = newIntensity > lastIntensity;
        SetTargetIntensityInternal(newIntensity, isFadingIn);
        lastIntensity = newIntensity;
    }
    
    /// <summary>
    /// Internal method to set target intensity and animate volume weight.
    /// </summary>
    private void SetTargetIntensityInternal(float intensity, bool isFadingIn)
    {
        intensity = Mathf.Clamp01(intensity);
        targetWeight = intensity;
        
        // Kill existing tween
        weightTween?.Kill();
        
        // Calculate duration based on intensity change
        float intensityDelta = Mathf.Abs(targetWeight - currentWeight);
        float duration = isFadingIn ? fadeInDuration : fadeOutDuration;
        float proportionalDuration = duration * intensityDelta;
        
        if (proportionalDuration < 0.01f)
        {
            // Instant change for very small changes
            ApplyWeight(targetWeight);
            return;
        }
        
        // Animate volume weight with DOTween
        Ease easeType = isFadingIn ? Ease.InQuad : Ease.OutQuad;
        
        weightTween = DOTween.To(
            () => currentWeight,
            value => ApplyWeight(value),
            targetWeight,
            proportionalDuration
        )
        .SetEase(easeType)
        .SetUpdate(true); // Use unscaled time
    }
    
    /// <summary>
    /// Applies weight to volume and updates effect parameters.
    /// </summary>
    private void ApplyWeight(float weight)
    {
        currentWeight = weight;
        
        if (volume != null)
        {
            volume.weight = currentWeight;
        }
        
        // Update Depth of Field aperture based on intensity
        if (depthOfField != null)
        {
            float aperture = Mathf.Lerp(dofMinAperture, dofMaxAperture, currentWeight);
            depthOfField.aperture.Override(aperture);
        }
        
        // Update Motion Blur intensity
        if (motionBlur != null)
        {
            float mbIntensity = Mathf.Lerp(motionBlurMinIntensity, motionBlurMaxIntensity, currentWeight);
            motionBlur.intensity.Override(mbIntensity);
        }
    }
    
    #region Public API
    
    /// <summary>
    /// Manually set the blur intensity (bypasses calculator).
    /// Useful for special events or cutscenes.
    /// </summary>
    public void SetManualIntensity(float intensity, bool fadeIn = true)
    {
        intensity = Mathf.Clamp01(intensity);
        if (enableDebugLogs)
            Debug.Log($"VolumeBlurController: Manual intensity set to {intensity:F2}, {(fadeIn ? "fading in" : "fading out")}");
        SetTargetIntensityInternal(intensity, fadeIn);
    }
    
    /// <summary>
    /// Gets the current blur intensity (volume weight).
    /// </summary>
    public float GetCurrentIntensity()
    {
        return currentWeight;
    }
    
    /// <summary>
    /// Enables or disables the blur overlay system.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (!enabled)
        {
            SetTargetIntensityInternal(0f, false);
        }
        else
        {
            // Re-enable and force update
            UpdateBlurEffect();
        }
    }
    
    /// <summary>
    /// Gets the volume component (for advanced manipulation).
    /// </summary>
    public Volume GetVolume()
    {
        return volume;
    }
    
    /// <summary>
    /// Gets the volume profile (for advanced manipulation).
    /// </summary>
    public VolumeProfile GetVolumeProfile()
    {
        return volumeProfile;
    }
    
    #endregion
    
    #region Editor Helpers
    
#if UNITY_EDITOR
    [ContextMenu("Test Max Blur")]
    private void TestMaxBlur()
    {
        SetManualIntensity(1f, true);
    }
    
    [ContextMenu("Test Clear Blur")]
    private void TestClearBlur()
    {
        SetManualIntensity(0f, false);
    }
    
    [ContextMenu("Force Update")]
    private void ForceUpdate()
    {
        UpdateBlurEffect();
    }
    
    [ContextMenu("Test Medium Blur")]
    private void TestMediumBlur()
    {
        SetManualIntensity(0.5f, true);
    }
#endif
    
    #endregion
}
