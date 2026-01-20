// SimpleStatsHUD.cs
using UnityEngine;
using UnityEngine.UI;

public class SimpleStatsHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [Header("UI References")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider thirstSlider;
    [SerializeField] private Slider staminaSlider;

    private void Start()
    {
        if (!playerStats)
            playerStats = FindFirstObjectByType<PlayerStats>();

        if (!playerStats) return;

        playerStats.OnHealthChanged += (cur, max) =>
        {
            if (healthSlider) healthSlider.value = cur / max;
        };
        playerStats.OnStaminaChanged += (cur, max) =>
        {
            if (staminaSlider) staminaSlider.value = cur / max;
        };
    }

    private void Update()
    {
        if (!playerStats) return;

        if (hungerSlider) hungerSlider.value = playerStats.HungerPercent;
        if (thirstSlider) thirstSlider.value = playerStats.ThirstPercent;
    }

    /// <summary>
    /// Shows the HUD panel
    /// </summary>
    public void Show()
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the HUD panel
    /// </summary>
    public void Hide()
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggles the HUD panel visibility
    /// </summary>
    public void Toggle()
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(!hudPanel.activeSelf);
        }
    }
}
