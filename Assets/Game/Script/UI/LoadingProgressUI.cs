using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reads SharedLoadingState every frame and drives a Slider + status label.
/// Smoothly animates the bar even when progress advances in discrete chunk-sized jumps.
///
/// Inspector setup (inside Scene_Debug_Gameplay):
///   - loadingState   → the SharedLoadingState asset (same one used in TerrainGenDemo)
///   - progressSlider → your Canvas Slider
///   - statusText     → your TextMeshProUGUI label
///   - smoothSpeed    → how fast the bar catches up (default 5)
/// </summary>
public class LoadingProgressUI : MonoBehaviour
{
    [SerializeField] private SharedLoadingState loadingState;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float smoothSpeed = 5f;

    private float displayedProgress = 0f;

    private void Update()
    {
        if (loadingState == null) return;

        // Smoothly lerp displayed value toward the real value
        displayedProgress = Mathf.MoveTowards(
            displayedProgress,
            loadingState.progress,
            smoothSpeed * Time.deltaTime);

        if (progressSlider != null)
            progressSlider.value = displayedProgress;

        if (statusText != null)
            statusText.text = loadingState.statusMessage;
    }
}
