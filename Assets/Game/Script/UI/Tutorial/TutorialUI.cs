using Game.Core.DI;
using Game.Core.Events;
using Game.Tutorial;
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

        private IEventBus _eventBus;
        private ITutorialManager _tutorialManager;

        public string PanelName => "Tutorial";
        public bool BlocksInput => false;
        public bool UnlocksCursor => false;
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
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
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
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
    }
}
