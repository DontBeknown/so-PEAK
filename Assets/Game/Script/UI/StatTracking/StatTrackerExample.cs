using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example script showing how to use the Player Stats Tracking system.
/// Attach this to a GameObject in your scene.
/// </summary>
public class StatTrackerExample : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStatsTrackerUI statsUI;
    
    [Header("Input Settings")]
    [SerializeField] private Key toggleKey = Key.P;
    
    private void Awake()
    {
        // Auto-find UI if not assigned
        if (statsUI == null)
        {
            statsUI = FindFirstObjectByType<PlayerStatsTrackerUI>();
        }
    }
    
    private void Update()
    {
        // Toggle stats UI with Tab key (or your configured key)
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            if (statsUI != null)
            {
                
                statsUI.Toggle();
            }
        }
    }
    
    /// <summary>
    /// Call this from a UI button or other trigger to show stats.
    /// </summary>
    public void ShowStatsUI()
    {
        if (statsUI != null)
        {
            statsUI.Show();
        }
    }
}
