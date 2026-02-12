/// <summary>
/// Interface for calculating blur intensity based on game state.
/// Follows Interface Segregation Principle - focused on single responsibility.
/// </summary>
public interface IBlurIntensityCalculator
{
    /// <summary>
    /// Calculates the target blur intensity (0-1 range).
    /// </summary>
    /// <returns>Target blur intensity value between 0 (no blur) and 1 (maximum blur)</returns>
    float CalculateIntensity();
    
    /// <summary>
    /// Initializes the calculator and subscribes to necessary events.
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Cleans up subscriptions and resources.
    /// </summary>
    void Cleanup();
}
