using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example script showing how to use the Player Stats Tracking system.
/// Attach this to a GameObject in your scene.
/// </summary>
public class StatTrackerExample : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private Key toggleKey = Key.P;
    
    private void Update()
    {
        // Toggle stats UI with configured key
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ToggleStatsTracker();
            }
        }
    }
    
    /// <summary>
    /// Call this from a UI button or other trigger to show stats.
    /// </summary>
    public void ShowStatsUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenStatsTracker();
        }
    }
}
