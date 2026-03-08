// SimpleStatsHUD.cs
using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;
using DG.Tweening;

public class SimpleStatsHUD : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;

    [Header("UI References")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider thirstSlider;
    [SerializeField] private Slider staminaSlider;

    [Header("Health Ghost Bar")]
    [Tooltip("A filled Image placed behind the health slider fill, used for the damage-echo effect.")]
    [SerializeField] private Image healthGhostFill;
    [SerializeField] private float healthAnimDuration = 0.25f;
    [SerializeField] private float ghostDelay = 0.5f;
    [SerializeField] private float ghostDrainDuration = 0.7f;

    private Tween _healthTween;
    private Sequence _ghostSequence;

    private void Start()
    {
        if (!playerStats)
            playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();

        if (!playerStats) return;

        // Initialise bars to current values immediately
        if (healthSlider)
            healthSlider.value = playerStats.HealthPercent;
        if (healthGhostFill)
            healthGhostFill.fillAmount = playerStats.HealthPercent;
        if (staminaSlider)
            staminaSlider.value = playerStats.StaminaPercent;

        playerStats.OnHealthChanged += OnHealthChanged;
        playerStats.OnStaminaChanged += OnStaminaChanged;
    }

    private void OnDestroy()
    {
        _healthTween?.Kill();
        _ghostSequence?.Kill();

        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= OnHealthChanged;
            playerStats.OnStaminaChanged -= OnStaminaChanged;
        }
    }

    private void OnHealthChanged(float cur, float max)
    {
        float target = cur / max;

        // ── Main bar: smooth tween ──────────────────────────────────────
        if (healthSlider)
        {
            _healthTween?.Kill();
            _healthTween = healthSlider.DOValue(target, healthAnimDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true); // keeps animating even when Time.timeScale == 0
        }

        // ── Ghost bar: hold, then drain ─────────────────────────────────
        if (healthGhostFill)
        {
            if (target < healthGhostFill.fillAmount)
            {
                // Damage: ghost stays at old value, then slowly catches up
                _ghostSequence?.Kill();
                _ghostSequence = DOTween.Sequence()
                    .SetUpdate(true)
                    .AppendInterval(ghostDelay)
                    .Append(DOTween.To(
                        () => healthGhostFill.fillAmount,
                        v => healthGhostFill.fillAmount = v,
                        target,
                        ghostDrainDuration)
                        .SetEase(Ease.InOutSine));
            }
            else
            {
                // Heal: ghost snaps to new (higher) value instantly
                _ghostSequence?.Kill();
                healthGhostFill.fillAmount = target;
            }
        }
    }

    private void OnStaminaChanged(float cur, float max)
    {
        if (staminaSlider)
            staminaSlider.DOValue(cur / max, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);
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
