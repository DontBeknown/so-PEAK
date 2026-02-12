using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;

/// <summary>
/// Main controller for the blur overlay system.
/// Orchestrates blur intensity calculation and effect application.
/// Follows SOLID principles:
/// - Single Responsibility: Coordinates blur system components
/// - Open/Closed: Can use different calculators and effects without modification
/// - Liskov Substitution: Works with any IBlurIntensityCalculator and IBlurEffect implementation
/// - Interface Segregation: Depends on focused interfaces
/// - Dependency Inversion: Depends on abstractions (interfaces), not concrete implementations
/// </summary>
public class BlurOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blurOverlayImage;
    
    [Header("Calculator Settings")]
    [SerializeField] private float hungerCriticalThreshold = 30f;
    [SerializeField] private float thirstCriticalThreshold = 30f;
    [SerializeField] private float hungerSevereThreshold = 10f;
    [SerializeField] private float thirstSevereThreshold = 10f;
    [SerializeField] private float maxBlurIntensity = 0.8f;
    [SerializeField] private bool useWorstStat = true;
    
    [Header("Effect Settings")]
    [SerializeField] private float fadeInDuration = 0.67f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Check stats every 0.1 seconds
    
    // Dependencies (following Dependency Inversion Principle)
    private IBlurIntensityCalculator intensityCalculator;
    private IBlurEffect blurEffect;
    private PlayerStats playerStats;
    
    private float lastIntensity;
    private float updateTimer;
    
    private void Awake()
    {
        // Get dependencies from ServiceContainer
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        
        if (playerStats == null)
        {
            Debug.LogError("BlurOverlayController requires PlayerStats to be registered in ServiceContainer!");
            enabled = false;
            return;
        }
        
        if (blurOverlayImage == null)
        {
            Debug.LogError("BlurOverlayController requires a reference to the blur overlay Image!");
            enabled = false;
            return;
        }
        
        // Initialize dependencies (Dependency Injection through constructors)
        InitializeDependencies();
    }
    
    private void Start()
    {
        // Initialize components
        intensityCalculator?.Initialize();
        blurEffect?.Initialize();
        
        // Subscribe to intensity changes
        if (intensityCalculator is SurvivalStatBlurCalculator survivalCalc)
        {
            survivalCalc.OnIntensityChanged += OnIntensityChanged;
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
        // Cleanup
        if (intensityCalculator is SurvivalStatBlurCalculator survivalCalc)
        {
            survivalCalc.OnIntensityChanged -= OnIntensityChanged;
        }
        
        intensityCalculator?.Cleanup();
        blurEffect?.Cleanup();
    }
    
    /// <summary>
    /// Initializes dependencies. Can be overridden for custom implementations.
    /// Follows Open/Closed Principle - open for extension.
    /// </summary>
    protected virtual void InitializeDependencies()
    {
        // Create intensity calculator with configuration
        var calculator = new SurvivalStatBlurCalculator(playerStats);
        calculator.SetHungerThresholds(hungerCriticalThreshold, hungerSevereThreshold);
        calculator.SetThirstThresholds(thirstCriticalThreshold, thirstSevereThreshold);
        calculator.SetIntensityRange(0f, maxBlurIntensity);
        calculator.SetUseWorstStat(useWorstStat);
        intensityCalculator = calculator;
        
        // Create blur effect with configuration
        var effect = new DOTweenBlurEffect(blurOverlayImage);
        effect.SetTransitionDurations(fadeInDuration, fadeOutDuration);
        blurEffect = effect;
    }
    
    /// <summary>
    /// Updates the blur effect based on current intensity calculation.
    /// </summary>
    private void UpdateBlurEffect()
    {
        if (intensityCalculator == null || blurEffect == null)
            return;
        
        // Update calculator to get latest intensity
        if (intensityCalculator is SurvivalStatBlurCalculator survivalCalc)
        {
            survivalCalc.UpdateIntensity();
        }
        
        float targetIntensity = intensityCalculator.CalculateIntensity();
        
        // Apply to effect if changed
        if (!Mathf.Approximately(targetIntensity, lastIntensity))
        {
            bool isFadingIn = targetIntensity > lastIntensity;
            blurEffect.SetTargetIntensity(targetIntensity, isFadingIn);
            lastIntensity = targetIntensity;
        }
    }
    
    /// <summary>
    /// Callback for intensity changes (if calculator supports events).
    /// </summary>
    private void OnIntensityChanged(float newIntensity)
    {
        bool isFadingIn = newIntensity > lastIntensity;
        blurEffect?.SetTargetIntensity(newIntensity, isFadingIn);
        lastIntensity = newIntensity;
    }
    
    #region Public API
    
    /// <summary>
    /// Manually set the blur intensity (bypasses calculator).
    /// Useful for special events or cutscenes.
    /// </summary>
    public void SetManualIntensity(float intensity, bool fadeIn = true)
    {
        blurEffect?.SetTargetIntensity(Mathf.Clamp01(intensity), fadeIn);
    }
    
    /// <summary>
    /// Gets the current blur intensity.
    /// </summary>
    public float GetCurrentIntensity()
    {
        return blurEffect?.CurrentIntensity ?? 0f;
    }
    
    /// <summary>
    /// Enables or disables the blur overlay system.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            blurEffect?.SetTargetIntensity(0f, false);
        }
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
#endif
    
    #endregion
}
