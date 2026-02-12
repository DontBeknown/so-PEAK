using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

/// <summary>
/// Applies blur effect using DOTween for smooth transitions.
/// Follows Single Responsibility Principle - only handles blur rendering.
/// Uses Image alpha channel for blur intensity control.
/// </summary>
[Serializable]
public class DOTweenBlurEffect : IBlurEffect
{
    [Header("References")]
    [SerializeField] private Image blurOverlayImage;
    
    [Header("Transition Settings")]
    [SerializeField] private float fadeInDuration = 0.67f;   // 1.5 intensity/sec -> ~0.67s for full fade-in
    [SerializeField] private float fadeOutDuration = 0.4f;   // 2.5 intensity/sec -> ~0.4s for full fade-out
    [SerializeField] private Ease fadeInEase = Ease.InQuad;
    [SerializeField] private Ease fadeOutEase = Ease.OutQuad;
    
    private Tweener currentTween;
    private float currentIntensity;
    
    public float CurrentIntensity => currentIntensity;
    
    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    public DOTweenBlurEffect(Image blurOverlayImage)
    {
        this.blurOverlayImage = blurOverlayImage ?? throw new ArgumentNullException(nameof(blurOverlayImage));
    }
    
    public void Initialize()
    {
        if (blurOverlayImage == null)
        {
            Debug.LogError("Blur overlay Image is null. Cannot initialize DOTweenBlurEffect.");
            return;
        }
        
        // Initialize to zero intensity
        SetIntensityImmediate(0f);
    }
    
    public void Cleanup()
    {
        // Kill any active tweens
        currentTween?.Kill();
        currentTween = null;
        
        // Reset to zero
        SetIntensityImmediate(0f);
    }
    
    public void SetTargetIntensity(float targetIntensity, bool isFadingIn)
    {
        targetIntensity = Mathf.Clamp01(targetIntensity);
        
        // Kill any existing tween
        currentTween?.Kill();
        
        // Choose duration and ease based on fade direction
        float duration = isFadingIn ? fadeInDuration : fadeOutDuration;
        Ease ease = isFadingIn ? fadeInEase : fadeOutEase;
        
        // If target is same as current, no need to animate
        if (Mathf.Approximately(currentIntensity, targetIntensity))
        {
            return;
        }
        
        // Adjust duration based on intensity difference (proportional)
        float intensityDelta = Mathf.Abs(targetIntensity - currentIntensity);
        duration *= intensityDelta; // Scale duration by how much we need to change
        
        // Create tween
        currentTween = DOTween.To(
            () => currentIntensity,
            value => ApplyIntensity(value),
            targetIntensity,
            duration
        )
        .SetEase(ease)
        .OnComplete(() => currentTween = null);
    }
    
    private void ApplyIntensity(float intensity)
    {
        currentIntensity = intensity;
        
        if (blurOverlayImage != null)
        {
            Color color = blurOverlayImage.color;
            color.a = intensity;
            blurOverlayImage.color = color;
        }
    }
    
    private void SetIntensityImmediate(float intensity)
    {
        currentTween?.Kill();
        ApplyIntensity(intensity);
    }
    
    #region Configuration Setters (Optional - for runtime configuration)
    
    public void SetTransitionDurations(float fadeIn, float fadeOut)
    {
        fadeInDuration = Mathf.Max(0.01f, fadeIn);
        fadeOutDuration = Mathf.Max(0.01f, fadeOut);
    }
    
    public void SetTransitionEases(Ease fadeIn, Ease fadeOut)
    {
        fadeInEase = fadeIn;
        fadeOutEase = fadeOut;
    }
    
    #endregion
}
