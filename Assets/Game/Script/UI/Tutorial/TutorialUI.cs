using Game.Core.DI;
using Game.Core.Events;
using Game.Tutorial;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Tutorial
{
    public class TutorialUI : MonoBehaviour, IUIPanel
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text inputHintText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text waitingText;
        [SerializeField] private Button skipButton;

        [Header("Animation")]
        [SerializeField] private float showDuration = 0.28f;
        [SerializeField] private float hideDuration = 0.2f;
        [SerializeField] private float hiddenScale = 0.9f;
        [SerializeField] private float stepChangeDuration = 0.18f;
        [SerializeField] private float stepChangeStartScale = 0.97f;
        [SerializeField] private float stepChangeStartAlpha = 0.8f;

        private IEventBus _eventBus;
        private ITutorialManager _tutorialManager;
        private CanvasGroup _panelCanvasGroup;
        private RectTransform _panelRectTransform;
        private Sequence _panelTween;
        private Sequence _stepChangeTween;

        public string PanelName => "Tutorial";
        public bool BlocksInput => false;
        public bool UnlocksCursor => false;
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            CachePanelAnimationComponents();

            if (panelRoot != null)
            {
                if (_panelCanvasGroup != null)
                {
                    _panelCanvasGroup.alpha = 0f;
                }

                if (_panelRectTransform != null)
                {
                    _panelRectTransform.localScale = Vector3.one * hiddenScale;
                }

                panelRoot.SetActive(false);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        private void OnDestroy()
        {
            _panelTween?.Kill();
            _stepChangeTween?.Kill();

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnSkipClicked);
            }
        }

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _tutorialManager = ServiceContainer.Instance.TryGet<ITutorialManager>();
        }

        private void OnEnable()
        {
            _eventBus ??= ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<TutorialStartedEvent>(OnTutorialStarted);
            _eventBus?.Subscribe<TutorialStepChangedEvent>(OnStepChanged);
            _eventBus?.Subscribe<TutorialCompletedEvent>(OnTutorialEnded);
            _eventBus?.Subscribe<TutorialSkippedEvent>(OnTutorialSkipped);
        }

        private void OnDisable()
        {
            _eventBus?.Unsubscribe<TutorialStartedEvent>(OnTutorialStarted);
            _eventBus?.Unsubscribe<TutorialStepChangedEvent>(OnStepChanged);
            _eventBus?.Unsubscribe<TutorialCompletedEvent>(OnTutorialEnded);
            _eventBus?.Unsubscribe<TutorialSkippedEvent>(OnTutorialSkipped);
        }

        public void Show()
        {
            if (panelRoot == null)
            {
                return;
            }

            CachePanelAnimationComponents();
            _panelTween?.Kill();

            panelRoot.SetActive(true);

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
            }

            if (_panelRectTransform != null)
            {
                _panelRectTransform.localScale = Vector3.one * hiddenScale;
            }

            _panelTween = DOTween.Sequence();

            if (_panelCanvasGroup != null)
            {
                _panelTween.Join(_panelCanvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));
            }

            if (_panelRectTransform != null)
            {
                _panelTween.Join(_panelRectTransform.DOScale(1f, showDuration).SetEase(Ease.OutBack));
            }
        }

        public void Hide()
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!panelRoot.activeSelf)
            {
                return;
            }

            CachePanelAnimationComponents();
            _panelTween?.Kill();
            _stepChangeTween?.Kill();

            _panelTween = DOTween.Sequence();

            if (_panelCanvasGroup != null)
            {
                _panelTween.Join(_panelCanvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));
            }

            if (_panelRectTransform != null)
            {
                _panelTween.Join(_panelRectTransform.DOScale(hiddenScale, hideDuration).SetEase(Ease.InBack));
            }

            _panelTween.OnComplete(() => panelRoot.SetActive(false));
        }

        public void Toggle()
        {
            if (IsActive)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void OnTutorialStarted(TutorialStartedEvent evt)
        {
            Show();
        }

        private void OnStepChanged(TutorialStepChangedEvent evt)
        {
            if (evt.IsWaitingForGate)
            {
                Hide();
                return;
            }
            
            if (!IsActive)
            {
                Show();
            }

            int displayIndex = evt.StepIndex + 1;

            if (progressText != null)
            {
                progressText.text = "Step " + displayIndex;
            }

            if (waitingText != null)
            {
                waitingText.gameObject.SetActive(false);
            }

            if (titleText != null)
            {
                titleText.text = evt.StepData != null ? evt.StepData.title : string.Empty;
            }

            if (instructionText != null)
            {
                instructionText.text = evt.StepData != null ? evt.StepData.instructionText : string.Empty;
            }

            if (inputHintText != null)
            {
                inputHintText.text = evt.StepData != null ? evt.StepData.inputHintText : string.Empty;
            }

            PlayStepChangeAnimation();
        }

        private void OnTutorialEnded(TutorialCompletedEvent evt)
        {
            Hide();
        }

        private void OnTutorialSkipped(TutorialSkippedEvent evt)
        {
            Hide();
        }

        private void OnSkipClicked()
        {
            _tutorialManager ??= ServiceContainer.Instance.TryGet<ITutorialManager>();
            _tutorialManager?.SkipTutorial();
        }

        private void PlayStepChangeAnimation()
        {
            if (!IsActive)
            {
                return;
            }

            CachePanelAnimationComponents();
            _stepChangeTween?.Kill();

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = stepChangeStartAlpha;
            }

            if (_panelRectTransform != null)
            {
                _panelRectTransform.localScale = Vector3.one * stepChangeStartScale;
            }

            _stepChangeTween = DOTween.Sequence();

            if (_panelCanvasGroup != null)
            {
                _stepChangeTween.Join(_panelCanvasGroup.DOFade(1f, stepChangeDuration).SetEase(Ease.OutQuad));
            }

            if (_panelRectTransform != null)
            {
                _stepChangeTween.Join(_panelRectTransform.DOScale(1f, stepChangeDuration).SetEase(Ease.OutBack));
            }
        }

        private void CachePanelAnimationComponents()
        {
            if (panelRoot == null)
            {
                return;
            }

            _panelRectTransform = panelRoot.GetComponent<RectTransform>();
            _panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();

            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }
    }
}
