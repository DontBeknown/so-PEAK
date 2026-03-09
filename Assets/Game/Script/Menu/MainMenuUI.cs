using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Game.Sound;

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
        
        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        [Header("Audio")]
        [SerializeField] private SoundService soundService;
        [SerializeField] private string menuMusicId = "music_menu";

        private void Start()
        {
            // Setup button listeners
            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);

            // Show main menu by default
            ShowMainMenu();

            if (soundService == null)
                soundService = FindFirstObjectByType<SoundService>();
            soundService?.PlayMusic(menuMusicId);
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
            if (enableDebug) Debug.Log("Settings button clicked - TODO: Implement settings menu");
            // TODO: Open settings menu
        }

        private void OnQuitClicked()
        {
            if (enableDebug) Debug.Log("Quit button clicked");
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
                worldSelectionUI.ShowWorldSelection(false);
            }
        }

        private void ShowWorldSelection()
        {

            if (worldSelectionUI != null)
            {
                worldSelectionUI.ShowWorldSelection(true);
                worldSelectionUI.RefreshWorldList();
            }
            mainMenuPanel.SetActive(false);
        }
    }
}
