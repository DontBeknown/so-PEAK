// SimpleStatsHUD.cs
using UnityEngine;
using UnityEngine.UI;

public class SimpleStatsHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [Header("UI References")]
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
}
