using UnityEngine;
using UnityEngine.UI;
using Game.Core.DI;
using DG.Tweening;

public class StatOutlineFeedback : MonoBehaviour
{
    [Header("Outline Images")]
    [SerializeField] private Image healthOutline;
    [SerializeField] private Image hungerOutline;
    [SerializeField] private Image thirstOutline;
    [SerializeField] private Image staminaOutline;

    [Header("Damage Flash (Health)")]
    [SerializeField] private float flashInDuration  = 0.05f;
    [SerializeField] private float flashOutDuration = 0.35f;
    [Tooltip("Seconds after last hit before DOT blink stops.")]
    [SerializeField] private float dotWindow        = 0.4f;

    [Header("DOT / Low-Stat Blink")]
    [SerializeField] private float blinkOnDuration  = 0.15f;
    [SerializeField] private float blinkOffDuration = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float maxAlpha = 0.8f;

    [Header("Low-Stat Thresholds (0–1)")]
    [SerializeField] private float hungerBlinkThreshold  = 0.3f;
    [SerializeField] private float thirstBlinkThreshold  = 0.3f;
    [SerializeField] private float staminaBlinkThreshold = 0.3f;

    private PlayerStats _stats;

    private enum HealthState { None, Flashing, DotBlink }
    private HealthState _healthState = HealthState.None;
    private float       _lastDamageTime = float.MinValue;
    private Sequence    _healthSeq;

    private Sequence _hungerSeq;
    private Sequence _thirstSeq;
    private Sequence _staminaSeq;
    private bool _hungerBlink;
    private bool _thirstBlink;
    private bool _staminaBlink;

    private void Start()
    {
        _stats = ServiceContainer.Instance.TryGet<PlayerStats>();
        if (!_stats) return;

        SetAlpha(healthOutline,  0f);
        SetAlpha(hungerOutline,  0f);
        SetAlpha(thirstOutline,  0f);
        SetAlpha(staminaOutline, 0f);

        _stats.OnHealthDamaged  += OnHealthDamaged;
        _stats.OnStaminaChanged += OnStaminaChanged;
    }

    private void OnDestroy()
    {
        _healthSeq?.Kill();
        _hungerSeq?.Kill();
        _thirstSeq?.Kill();
        _staminaSeq?.Kill();

        if (_stats != null)
        {
            _stats.OnHealthDamaged  -= OnHealthDamaged;
            _stats.OnStaminaChanged -= OnStaminaChanged;
        }
    }

    private void Update()
    {
        if (!_stats) return;

        // DOT expiry: stop blinking once hits dry up
        if (_healthState == HealthState.DotBlink &&
            Time.unscaledTime - _lastDamageTime > dotWindow)
        {
            _healthSeq?.Kill();
            SetAlpha(healthOutline, 0f);
            _healthState = HealthState.None;
        }

        HandleLowStat(_stats.HungerPercent < hungerBlinkThreshold, ref _hungerBlink, hungerOutline, ref _hungerSeq);
        HandleLowStat(_stats.ThirstPercent < thirstBlinkThreshold, ref _thirstBlink, thirstOutline, ref _thirstSeq);
    }

    private void OnHealthDamaged(float amount)
    {
        _lastDamageTime = Time.unscaledTime;

        switch (_healthState)
        {
            case HealthState.None:
                _healthState = HealthState.Flashing;
                StartHealthFlash();
                break;

            case HealthState.Flashing:
                // Second damage while flash is running → upgrade to continuous DOT blink
                _healthState = HealthState.DotBlink;
                StartHealthDotBlink();
                break;

            // DotBlink: already blinking, just update _lastDamageTime (done above)
        }
    }

    private void OnStaminaChanged(float cur, float max)
    {
        HandleLowStat(cur / max < staminaBlinkThreshold, ref _staminaBlink, staminaOutline, ref _staminaSeq);
    }

    private void StartHealthFlash()
    {
        _healthSeq?.Kill();
        _healthSeq = DOTween.Sequence()
            .SetUpdate(true)
            .Append(FadeAlpha(healthOutline, maxAlpha, flashInDuration,  Ease.OutQuad))
            .Append(FadeAlpha(healthOutline, 0f, flashOutDuration, Ease.InQuad))
            .OnComplete(() =>
            {
                if (_healthState == HealthState.Flashing)
                    _healthState = HealthState.None;
            });
    }

    private void StartHealthDotBlink()
    {
        _healthSeq?.Kill();
        _healthSeq = DOTween.Sequence()
            .SetUpdate(true)
            .SetLoops(-1)
            .Append(FadeAlpha(healthOutline, maxAlpha, blinkOnDuration,  Ease.OutQuad))
            .Append(FadeAlpha(healthOutline, 0f, blinkOffDuration, Ease.InQuad));
    }

    private void HandleLowStat(bool isLow, ref bool blinking, Image outline, ref Sequence seq)
    {
        if (isLow && !blinking)
        {
            blinking = true;
            seq?.Kill();
            seq = DOTween.Sequence()
                .SetUpdate(true)
                .SetLoops(-1)
                .Append(FadeAlpha(outline, maxAlpha, blinkOnDuration,  Ease.OutQuad))
                .Append(FadeAlpha(outline, 0f, blinkOffDuration, Ease.InQuad));
        }
        else if (!isLow && blinking)
        {
            blinking = false;
            seq?.Kill();
            SetAlpha(outline, 0f);
        }
    }

    private static Tweener FadeAlpha(Image img, float target, float duration, Ease ease)
    {
        return DOTween.To(
            () => img ? img.color.a : 0f,
            a => SetAlpha(img, a),
            target,
            duration).SetEase(ease);
    }

    private static void SetAlpha(Image img, float a)
    {
        if (!img) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}
