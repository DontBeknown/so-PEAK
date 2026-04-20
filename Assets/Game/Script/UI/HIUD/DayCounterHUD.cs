using UnityEngine;
using TMPro;
using DG.Tweening;
using Game.Collectable;
using Game.Core.DI;
using Game.Core.Events;
using Game.Environment.DayNight;
using Game.Progression;

namespace Game.UI
{
    /// <summary>
    /// Always-visible corner HUD element that displays the current in-game day number.
    /// Place on a TextMeshProUGUI GameObject in a Screen Space - Overlay canvas.
    /// Automatically updates whenever a new day begins.
    /// </summary>
    public class DayCounterHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI biomeIntroText;
        [SerializeField] private CanvasGroup biomeIntroCanvasGroup;
        [SerializeField] private RectTransform biomeIntroRectTransform;
        [SerializeField] private CanvasGroup dayCounterCanvasGroup;
        [SerializeField] private RectTransform dayCounterRectTransform;

        [Header("Format")]
        [SerializeField] private string displayFormat = "Day {0}";

        [Header("Intro Timing")]
        [SerializeField] private float introFadeInDuration = 0.2f;
        [SerializeField] private float introHoldDuration = 1.5f;
        [SerializeField] private float introExitDuration = 0.6f;
        [SerializeField] private float dayCounterFadeInDuration = 0.4f;

        [Header("Intro Exit Transform")]
        [SerializeField] private float introMoveUpDistance = 60f;
        [SerializeField] private float introExitScale = 0.8f;

        private IDayNightCycleService _dayNightService;
        private ISaveLoadService _saveLoadService;
        private IEventBus _eventBus;
        private Sequence _activeSequence;
        private Vector2 _introBaseAnchoredPosition;
        private Vector3 _introBaseScale;
        private bool _hasDecidedIntroFlow = false;
        private bool _isIntroRunning;
        private bool _hasQueuedDayNumber;
        private int _queuedDayNumber;
        private bool _hasLoggedMissingIntroWarning;

        private void Start()
        {
            ResolveServices();
            ResolveAnimationTargets();

            if (biomeIntroRectTransform != null)
            {
                _introBaseAnchoredPosition = biomeIntroRectTransform.anchoredPosition;
                _introBaseScale = biomeIntroRectTransform.localScale;
            }

            InitializeHiddenState();
            UpdateDisplay();

            if (_eventBus != null)
            {
                _eventBus.Subscribe<DayCompletedEvent>(OnDayCompleted);
                _eventBus.Subscribe<PlayerSpawnCompletedEvent>(OnPlayerSpawnCompleted);
            }
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<DayCompletedEvent>(OnDayCompleted);
            _eventBus?.Unsubscribe<PlayerSpawnCompletedEvent>(OnPlayerSpawnCompleted);

            KillActiveSequence();
            _isIntroRunning = false;
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }

        private void ResolveServices()
        {
            _dayNightService = ServiceContainer.Instance.TryGet<IDayNightCycleService>();
            _saveLoadService = SaveLoadService.Instance;
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

            if (_dayNightService == null)
            {
                Debug.LogWarning("[DayCounterHUD] IDayNightCycleService not found in ServiceContainer. " +
                                 "Ensure DayNightCycleManager registers itself before this HUD is enabled.");
            }
        }

        private void ResolveAnimationTargets()
        {
            if (dayCounterRectTransform == null && dayText != null)
            {
                dayCounterRectTransform = dayText.rectTransform;
            }

            if (dayCounterCanvasGroup == null && dayCounterRectTransform != null)
            {
                dayCounterCanvasGroup = dayCounterRectTransform.GetComponent<CanvasGroup>();
            }

            if (biomeIntroRectTransform == null && biomeIntroText != null)
            {
                biomeIntroRectTransform = biomeIntroText.rectTransform;
            }

            if (biomeIntroCanvasGroup == null && biomeIntroRectTransform != null)
            {
                biomeIntroCanvasGroup = biomeIntroRectTransform.GetComponent<CanvasGroup>();
            }
        }

        private void InitializeHiddenState()
        {
            if (biomeIntroCanvasGroup != null)
            {
                biomeIntroCanvasGroup.alpha = 0f;
            }

            if (dayCounterCanvasGroup != null)
            {
                dayCounterCanvasGroup.alpha = 0f;
            }

            if (biomeIntroRectTransform != null)
            {
                biomeIntroRectTransform.localScale = _introBaseScale == Vector3.zero ? Vector3.one : _introBaseScale;
                biomeIntroRectTransform.anchoredPosition = _introBaseAnchoredPosition;
            }
        }

        private void OnPlayerSpawnCompleted(PlayerSpawnCompletedEvent _)
        {
            DecideIntroFlow();
        }

        private void OnDayCompleted(DayCompletedEvent evt)
        {
            if (_isIntroRunning)
            {
                _hasQueuedDayNumber = true;
                _queuedDayNumber = evt.dayNumber;
                return;
            }

            UpdateDisplay(evt.dayNumber);
        }

        private void DecideIntroFlow()
        {
            if (_hasDecidedIntroFlow)
            {
                return;
            }

            _hasDecidedIntroFlow = true;

            bool isFreshEntry = _saveLoadService != null && (_saveLoadService.IsFreshLevelEntry() || _saveLoadService.IsNewWorld());
            //Debug.Log($"[DayCounterHUD] Deciding intro flow. Is fresh level entry: {isFreshEntry} saveloadService: {_saveLoadService} isnewworld: {_saveLoadService?.IsNewWorld()}", this);
            if (!isFreshEntry)
            {
                FadeInDayCounter(dayCounterFadeInDuration);
                return;
            }

            PlayBiomeIntroThenDayCounter();
        }

        private void PlayBiomeIntroThenDayCounter()
        {
            if (biomeIntroText == null)
            {
                if (!_hasLoggedMissingIntroWarning)
                {
                    Debug.LogWarning("[DayCounterHUD] Biome intro text reference is missing. Falling back to day counter only.", this);
                    _hasLoggedMissingIntroWarning = true;
                }

                FadeInDayCounter(dayCounterFadeInDuration);
                return;
            }

            ResolveAnimationTargets();
            KillActiveSequence();

            int currentLevel = _saveLoadService != null ? _saveLoadService.GetCurrentLevel() : 1;
            CollectableBiome biome = LevelBonusCollectableService.GetBiomeForLevel(currentLevel);
            biomeIntroText.text = biome.ToString();

            if (biomeIntroRectTransform != null)
            {
                biomeIntroRectTransform.localScale = _introBaseScale == Vector3.zero ? Vector3.one : _introBaseScale;
                biomeIntroRectTransform.anchoredPosition = _introBaseAnchoredPosition;
            }

            if (biomeIntroCanvasGroup != null)
            {
                biomeIntroCanvasGroup.alpha = 0f;
            }

            if (dayCounterCanvasGroup != null)
            {
                dayCounterCanvasGroup.alpha = 0f;
            }

            _isIntroRunning = true;
            _activeSequence = DOTween.Sequence();

            if (biomeIntroCanvasGroup != null)
            {
                _activeSequence.Append(biomeIntroCanvasGroup.DOFade(1f, introFadeInDuration));
            }
            else
            {
                _activeSequence.AppendInterval(introFadeInDuration);
            }

            _activeSequence.AppendInterval(introHoldDuration);

            if (biomeIntroCanvasGroup != null)
            {
                _activeSequence.Append(biomeIntroCanvasGroup.DOFade(0f, introExitDuration));
            }
            else
            {
                _activeSequence.AppendInterval(introExitDuration);
            }

            if (biomeIntroRectTransform != null)
            {
                Vector3 startScale = _introBaseScale == Vector3.zero ? Vector3.one : _introBaseScale;
                _activeSequence.Join(biomeIntroRectTransform.DOScale(startScale * introExitScale, introExitDuration));
                _activeSequence.Join(biomeIntroRectTransform.DOAnchorPosY(_introBaseAnchoredPosition.y + introMoveUpDistance, introExitDuration));
            }

            if (dayCounterCanvasGroup != null)
            {
                _activeSequence.Append(dayCounterCanvasGroup.DOFade(1f, dayCounterFadeInDuration));
            }

            _activeSequence.OnComplete(() =>
            {
                _isIntroRunning = false;
                ApplyQueuedDayIfAny();
            });

            _activeSequence.SetLink(gameObject);
        }

        private void FadeInDayCounter(float duration)
        {
            ResolveAnimationTargets();
            KillActiveSequence();
            UpdateDisplay();

            if (dayCounterCanvasGroup == null)
            {
                _isIntroRunning = false;
                ApplyQueuedDayIfAny();
                return;
            }

            dayCounterCanvasGroup.alpha = 0f;
            _activeSequence = DOTween.Sequence();
            _activeSequence.Append(dayCounterCanvasGroup.DOFade(1f, duration));
            _activeSequence.OnComplete(() =>
            {
                _isIntroRunning = false;
                ApplyQueuedDayIfAny();
            });
            _activeSequence.SetLink(gameObject);
        }

        private void ApplyQueuedDayIfAny()
        {
            if (!_hasQueuedDayNumber)
            {
                UpdateDisplay();
                return;
            }

            UpdateDisplay(_queuedDayNumber);
            _hasQueuedDayNumber = false;
        }

        private void KillActiveSequence()
        {
            if (_activeSequence == null)
            {
                return;
            }

            if (_activeSequence.IsActive())
            {
                _activeSequence.Kill();
            }

            _activeSequence = null;
        }

        private void UpdateDisplay(int? explicitDayNumber = null)
        {
            if (dayText == null)
            {
                return;
            }

            int dayValue = explicitDayNumber ?? _dayNightService?.CurrentDay ?? 1;
            dayText.text = string.Format(displayFormat, dayValue);
        }
    }
}
