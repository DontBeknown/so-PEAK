using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core.DI;
using Game.Core.Events;
using Game.Collectable;
using Game.Dialog;
using Game.UI;

namespace Game.UI.Dialog
{
    public class DialogUI : MonoBehaviour, IUIPanel
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private float charactersPerSecond = 45f;

        [Header("Panel Identity")]
        [SerializeField] private string panelName = "Dialog";

        private IEventBus _eventBus;
        private IDialogManager _dialogManager;
        private UIServiceProvider _uiServiceProvider;
        private Coroutine _typingCoroutine;
        private string _targetLine = string.Empty;
        private bool _isTyping;

        public string PanelName => panelName;
        public bool BlocksInput => false;
        public bool UnlocksCursor => false;
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            Hide();
        }

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _dialogManager = ServiceContainer.Instance.TryGet<IDialogManager>();
            _uiServiceProvider = ServiceContainer.Instance.TryGet<UIServiceProvider>();

            _eventBus?.Subscribe<DialogStartedEvent>(OnDialogStarted);
            _eventBus?.Subscribe<DialogLineChangedEvent>(OnDialogLineChanged);
            _eventBus?.Subscribe<DialogEndedEvent>(OnDialogEnded);
            _eventBus?.Subscribe<CollectableOpenRequestedEvent>(OnCollectableOpenRequested);
            _eventBus?.Subscribe<PanelOpenedEvent>(OnPanelOpened);
            _eventBus?.Subscribe<PanelClosedEvent>(OnPanelClosed);
        }

        private void OnDestroy()
        {
            StopTyping();
            _eventBus?.Unsubscribe<DialogStartedEvent>(OnDialogStarted);
            _eventBus?.Unsubscribe<DialogLineChangedEvent>(OnDialogLineChanged);
            _eventBus?.Unsubscribe<DialogEndedEvent>(OnDialogEnded);
            _eventBus?.Unsubscribe<CollectableOpenRequestedEvent>(OnCollectableOpenRequested);
            _eventBus?.Unsubscribe<PanelOpenedEvent>(OnPanelOpened);
            _eventBus?.Unsubscribe<PanelClosedEvent>(OnPanelClosed);
        }

        private void Update()
        {
            if (!IsActive || _dialogManager == null || !_dialogManager.IsActive || _dialogManager.IsPaused)
                return;

            // Dedicated advance key bind (Mouse Right).
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (CompleteCurrentLine())
                    return;

                _dialogManager.AdvanceLine();
            }
        }

        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void Hide()
        {
            StopTyping();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (IsActive)
                Hide();
            else
                Show();
        }

        private void OnDialogStarted(DialogStartedEvent evt)
        {
            Show();

            if (lineText != null)
                lineText.text = string.Empty;
        }

        private void OnDialogLineChanged(DialogLineChangedEvent evt)
        {
            SetLine(evt.Line.speakerName, evt.Line.text);
        }

        private void OnDialogEnded(DialogEndedEvent _)
        {
            Hide();
        }

        private void OnCollectableOpenRequested(CollectableOpenRequestedEvent evt)
        {
            if (evt?.Collectable == null || evt.Collectable.type != CollectableType.ScriptDialog)
                return;

            if (evt.Collectable.dialogData == null)
                return;

            _dialogManager?.StartDialog(evt.Collectable.dialogData, evt.ReplayDialog);
        }

        private void OnPanelOpened(PanelOpenedEvent evt)
        {
            if (_dialogManager == null || !_dialogManager.IsActive)
                return;

            if (evt != null && evt.PanelName == PanelName)
                return;

            _dialogManager.PauseDialog();
            Hide();
        }

        private void OnPanelClosed(PanelClosedEvent _)
        {
            if (_dialogManager == null || !_dialogManager.IsActive || !_dialogManager.IsPaused)
                return;

            if (_uiServiceProvider != null && _uiServiceProvider.IsAnyPanelOpen())
                return;

            _dialogManager.ResumeDialog();
            Show();
        }

        private void SetLine(string speaker, string line)
        {
            if (speakerText != null)
                speakerText.text = speaker;

            StartTypingLine(line);
        }

        private void StartTypingLine(string line)
        {
            StopTyping();

            _targetLine = line ?? string.Empty;

            if (lineText == null)
                return;

            if (_targetLine.Length == 0)
            {
                lineText.text = string.Empty;
                _isTyping = false;
                return;
            }

            lineText.text = string.Empty;
            _typingCoroutine = StartCoroutine(TypeLineRoutine());
        }

        private IEnumerator TypeLineRoutine()
        {
            _isTyping = true;

            if (charactersPerSecond <= 0f)
            {
                if (lineText != null)
                    lineText.text = _targetLine;

                _isTyping = false;
                _typingCoroutine = null;
                yield break;
            }

            var delay = 1f / charactersPerSecond;
            for (int i = 1; i <= _targetLine.Length; i++)
            {
                if (lineText != null)
                    lineText.text = _targetLine.Substring(0, i);

                yield return new WaitForSecondsRealtime(delay);
            }

            _isTyping = false;
            _typingCoroutine = null;
        }

        private bool CompleteCurrentLine()
        {
            if (!_isTyping)
                return false;

            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);

            _typingCoroutine = null;
            _isTyping = false;

            if (lineText != null)
                lineText.text = _targetLine;

            return true;
        }

        private void StopTyping()
        {
            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);

            _typingCoroutine = null;
            _isTyping = false;
        }
    }
}
