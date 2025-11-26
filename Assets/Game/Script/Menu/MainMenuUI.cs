using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game.Menu
{
    /// <summary>
    /// Main menu controller - handles the initial menu screen
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private WorldSelectionUI worldSelectionUI;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            // Setup button listeners
            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            // Show main menu by default
            ShowMainMenu();
        }

        private void OnDestroy()
        {
            // Clean up listeners
            playButton.onClick.RemoveListener(OnPlayClicked);
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnPlayClicked()
        {
            //Debug.Log("Play button clicked - Opening world selection");
            ShowWorldSelection();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("Settings button clicked - TODO: Implement settings menu");
            // TODO: Open settings menu
        }

        private void OnQuitClicked()
        {
            Debug.Log("Quit button clicked");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            if (worldSelectionUI != null)
            {
                worldSelectionUI.gameObject.SetActive(false);
            }
        }

        private void ShowWorldSelection()
        {
            mainMenuPanel.SetActive(false);
            if (worldSelectionUI != null)
            {
                worldSelectionUI.gameObject.SetActive(true);
                worldSelectionUI.RefreshWorldList();
            }
        }
    }
}
