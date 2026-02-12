/// <summary>
/// Interface for applying blur effects to the screen.
/// Follows Interface Segregation Principle - focused on effect rendering.
/// Follows Open/Closed Principle - open for extension (different blur implementations).
/// </summary>
public interface IBlurEffect
{
    /// <summary>
    /// Sets the target blur intensity with smooth transition.
    /// </summary>
    /// <param name="targetIntensity">Target intensity value (0-1)</param>
    /// <param name="isFadingIn">True if intensity is increasing, false if decreasing</param>
    void SetTargetIntensity(float targetIntensity, bool isFadingIn);
    
    /// <summary>
    /// Gets the current blur intensity.
    /// </summary>
    float CurrentIntensity { get; }
    
    /// <summary>
    /// Initializes the blur effect.
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Cleans up resources and stops any active transitions.
    /// </summary>
    void Cleanup();
}
