using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Game.UI
{
    /// <summary>
    /// Pause menu panel controlled through UIServiceProvider.
    /// Esc toggle is routed from PlayerInputHandler via PlayerControllerRefactored.
    /// </summary>
    public class PauseMenuPanel : MonoBehaviour, IUIPanel
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float animDuration = 0.2f;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SaveExitButton saveExitButton;

        public string PanelName => "PauseMenu";
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private bool _pausedByThisPanel;

        private void Awake()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(HandleResumeClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(HandleSettingsClicked);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(HandleResumeClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(HandleSettingsClicked);

            EnsureGameplayResumed();
        }

        private void OnDisable()
        {
            EnsureGameplayResumed();
        }

        public void Show()
        {
            if (panelRoot == null)
                return;

            panelRoot.SetActive(true);
            SetPausedState(true);

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, animDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
            }
        }

        public void Hide()
        {
            if (panelRoot == null)
                return;

            SetPausedState(false);

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, animDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (panelRoot != null)
                            panelRoot.SetActive(false);
                    });
            }
            else
            {
                panelRoot.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsActive)
                Hide();
            else
                Show();
        }

        private void HandleResumeClicked()
        {
            if (UIServiceProvider.Instance != null)
            {
                UIServiceProvider.Instance.ClosePanel(PanelName);
                return;
            }

            Hide();
        }

        private void HandleSettingsClicked()
        {
            if (UIServiceProvider.Instance != null)
            {
                UIServiceProvider.Instance.OpenPanel("SoundSettings");
            }
        }

        private void SetPausedState(bool paused)
        {
            _pausedByThisPanel = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        private void EnsureGameplayResumed()
        {
            if (!_pausedByThisPanel)
                return;

            _pausedByThisPanel = false;
            Time.timeScale = 1f;
        }

        private void OnValidate()
        {
            if (saveExitButton == null)
                return;

            // Save & Exit behavior is delegated to SaveExitButton.
            // Keep this reference assigned for inspector clarity and validation.
        }
    }
}
