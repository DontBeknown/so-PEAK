using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using Game.Core.DI;
using Game.UI;

namespace Game.UI.EndingScreen
{
    /// <summary>
    /// Displays ending screen with credits when player reaches level 3.
    /// Implements IUIPanel for integration with UIServiceProvider.
    /// </summary>
    public class EndingScreenUI : MonoBehaviour, IUIPanel
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI creditsText;

        [Header("Buttons")]
        [SerializeField] private Button mainMenuButton;

        [Header("Scene")]
        [SerializeField] private string menuSceneName = "Menu";

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 1.5f;

        // IUIPanel
        public string PanelName => "EndingScreen";
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void Start()
        {
            // UI is discovered and registered by UIServiceProvider automatically
        }

        // --- IUIPanel Implementation ---

        public void Show()
        {
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.InQuad);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.DOKill();
        }

        public void Toggle()
        {
            if (IsActive) Hide();
            else Show();
        }

        // --- Public API ---

        public void ShowEnding()
        {
            if (titleText != null)
                titleText.text = "Expedition Complete";

            if (creditsText != null)
            {
                creditsText.text = GetCreditsText();
            }

            Show();
        }

        // --- Private ---

        private string GetCreditsText()
        {
            return @"
            Thank you for playing 'This is so PEAK'!

            YOUR EXPEDITION HAS REACHED ITS CONCLUSION

            You have successfully completed the three levels of this wilderness expedition.
            Your perseverance, resourcefulness, and survival skills were truly tested.

            CREDITS

            Game Design & Development: [Your Team]
            Programming: [Your Team]
            Art & Assets: [Your Team]

            Special Thanks to all the players who contributed to making this adventure possible.

            Your final assessment report has been recorded for posterity.

            Press 'Return to Menu' to conclude your expedition.
            ";
        }

        private void OnMainMenuClicked()
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
