using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Game.Menu
{
    /// <summary>
    /// World selection menu - displays list of worlds and handles world selection, creation, and deletion
    /// </summary>
    public class WorldSelectionUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject worldSelectionPanel;
        [SerializeField] private WorldCreateUI worldCreateUI;

        [Header("World List")]
        [SerializeField] private Transform worldListContainer;
        [SerializeField] private GameObject worldSlotPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button createWorldButton;
        [SerializeField] private Button deleteModeButton;

        [Header("Delete Mode")]
        [SerializeField] private GameObject normalModeUI;
        [SerializeField] private GameObject deleteModeUI;
        [SerializeField] private Button cancelDeleteButton;
        [SerializeField] private TextMeshProUGUI deleteModeButtonText;
        
        [Header("System")]
        [SerializeField] private WorldPersistenceManager worldPersistence;
        [SerializeField] private SaveLoadService saveLoadService;
        [SerializeField] private string gameplaySceneName = "TerrainGenDemo";
        
        [Header("Loading")]
        [SerializeField] private LoadingPanelUI loadingPanel;
        [SerializeField] private float minLoadingDuration = 1.5f; // Minimum time to show loading screen
        
        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        private List<WorldSlotUI> worldSlots = new List<WorldSlotUI>();
        private bool isDeleteMode = false;

        private void Start()
        {
            // Validate references
            if (saveLoadService == null)
            {
                saveLoadService = SaveLoadService.Instance;
                if (saveLoadService == null)
                {
                    Debug.LogError("[WorldSelectionUI] SaveLoadService not available!");
                }
            }
            
            if (loadingPanel == null)
            {
                Debug.LogWarning("[WorldSelectionUI] LoadingPanelUI not assigned in Inspector!");
            }
            
            // Setup button listeners
            backButton.onClick.AddListener(OnBackClicked);
            createWorldButton.onClick.AddListener(OnCreateWorldClicked);
            deleteModeButton.onClick.AddListener(OnDeleteModeToggled);
            
            if (cancelDeleteButton != null)
            {
                cancelDeleteButton.onClick.AddListener(OnCancelDeleteClicked);
            }

            // Load worlds from save system
            RefreshWorldList();
        }

        private void OnDestroy()
        {
            // Clean up listeners
            backButton.onClick.RemoveListener(OnBackClicked);
            createWorldButton.onClick.RemoveListener(OnCreateWorldClicked);
            deleteModeButton.onClick.RemoveListener(OnDeleteModeToggled);
            
            if (cancelDeleteButton != null)
            {
                cancelDeleteButton.onClick.RemoveListener(OnCancelDeleteClicked);
            }
        }

        public void RefreshWorldList()
        {
            // Clear existing slots
            foreach (var slot in worldSlots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            worldSlots.Clear();

            if (saveLoadService == null)
            {
                saveLoadService = SaveLoadService.Instance;
                if (saveLoadService == null)
                {
                    Debug.LogError("[WorldSelectionUI] SaveLoadService not available!");
                    return;
                }
            }

            // Get all worlds from save system
            var worldMetadata = saveLoadService.GetAllWorlds();
            
            // Sort by last played date (most recent first)
            worldMetadata = worldMetadata.OrderByDescending(w => w.lastPlayedDate).ToList();

            // Create new slots for each world
            foreach (var metadata in worldMetadata)
            {
                CreateWorldSlot(metadata);
            }

            // Reapply delete mode to all slots if active
            if (isDeleteMode)
            {
                foreach (var slot in worldSlots)
                {
                    if (slot != null)
                    {
                        slot.SetDeleteMode(true);
                    }
                }
            }

            // Update delete mode UI
            UpdateDeleteModeUI();
        }

        private void CreateWorldSlot(SaveMetadata metadata)
        {
            GameObject slotObj = Instantiate(worldSlotPrefab, worldListContainer);
            WorldSlotUI slot = slotObj.GetComponent<WorldSlotUI>();
            
            if (slot != null)
            {
                slot.Initialize(metadata, OnWorldSelected, OnWorldDeleted);
                worldSlots.Add(slot);
            }
        }

        private void OnBackClicked()
        {
            if (enableDebug) Debug.Log("Back button clicked - Returning to main menu");
            
            // Exit delete mode if active
            if (isDeleteMode)
            {
                SetDeleteMode(false);
            }

            // Return to main menu
            MainMenuUI mainMenu = FindFirstObjectByType<MainMenuUI>();
            if (mainMenu != null)
            {
                mainMenu.ShowMainMenu();
            }
            
            worldSelectionPanel.SetActive(false);
        }

        private void OnCreateWorldClicked()
        {
            if (enableDebug) Debug.Log("Create World button clicked");
            ShowWorldCreation();
        }

        private void OnDeleteModeToggled()
        {
            SetDeleteMode(!isDeleteMode);
        }

        private void OnCancelDeleteClicked()
        {
            SetDeleteMode(false);
        }

        private void SetDeleteMode(bool enabled)
        {
            isDeleteMode = enabled;
            UpdateDeleteModeUI();

            // Update all world slots
            foreach (var slot in worldSlots)
            {
                if (slot != null)
                {
                    slot.SetDeleteMode(isDeleteMode);
                }
            }

            if (enableDebug) Debug.Log($"Delete mode: {(isDeleteMode ? "ENABLED" : "DISABLED")}");
        }

        private void UpdateDeleteModeUI()
        {
            if (normalModeUI != null)
                normalModeUI.SetActive(!isDeleteMode);
            
            if (deleteModeUI != null)
                deleteModeUI.SetActive(isDeleteMode);

            if (deleteModeButtonText != null)
            {
                deleteModeButtonText.text = isDeleteMode ? "Cancel Delete" : "Delete Worlds";
            }
        }

        private void OnWorldSelected(SaveMetadata metadata)
        {
            if (isDeleteMode)
            {
                Debug.LogWarning("Cannot select world in delete mode");
                return;
            }

            if (saveLoadService == null)
            {
                Debug.LogError("[WorldSelectionUI] Cannot load world - SaveLoadService not available");
                return;
            }

            // Show loading panel
            if (loadingPanel != null)
            {
                loadingPanel.Show();
            }

            // Hide world selection panel after loading panel is visible
            worldSelectionPanel.SetActive(false);

            if (enableDebug) Debug.Log($"Loading world: {metadata.worldName} (GUID: {metadata.worldGuid})");
            
            // Start loading with loading panel
            StartCoroutine(LoadWorldWithLoadingScreen(metadata));
        }

        private IEnumerator LoadWorldWithLoadingScreen(SaveMetadata metadata)
        {
            
            float startTime = Time.time;

            // Load the world data
            loadingPanel?.SetProgress(0.3f);
            loadingPanel?.SetLoadingMessage("Loading world data...");
            
            WorldSaveData worldData = saveLoadService.LoadWorld(metadata.worldGuid);
            
            if (worldData == null)
            {
                if (enableDebug) Debug.LogError($"Failed to load world: {metadata.worldName}");
                if (loadingPanel != null)
                {
                    loadingPanel.Hide();
                }
                yield break;
            }
            
            yield return new WaitForSeconds(0.3f);
            loadingPanel?.SetProgress(0.6f);
            loadingPanel?.SetLoadingMessage("Preparing environment...");
            
            // Prepare world persistence for scene transition
            if (worldPersistence != null)
            {
                worldPersistence.PrepareLoadWorld(worldData);
            }
            else
            {
                Debug.LogError("[WorldSelectionUI] WorldPersistenceManager not assigned!");
            }

            yield return new WaitForSeconds(0.3f);
            loadingPanel?.SetProgress(0.9f);

            // Ensure minimum loading duration for smooth UX
            float elapsed = Time.time - startTime;
            if (elapsed < minLoadingDuration)
            {
                yield return new WaitForSeconds(minLoadingDuration - elapsed);
            }

            loadingPanel?.SetProgress(1f);
            loadingPanel?.SetLoadingMessage("Loading scene...");
            
            yield return new WaitForSeconds(0.2f);

            // Load gameplay scene
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnWorldDeleted(SaveMetadata metadata)
        {
            if (saveLoadService == null)
            {
                Debug.LogError("[WorldSelectionUI] Cannot delete world - SaveLoadService not available");
                return;
            }
            
            if (enableDebug) Debug.Log($"Deleting world: {metadata.worldName}");
            
            // Delete through save system
            saveLoadService.DeleteWorld(metadata.worldGuid);
            
            // Refresh the list
            RefreshWorldList();

            // Exit delete mode if no worlds left
            if (worldSlots.Count == 0)
            {
                SetDeleteMode(false);
            }
        }

        private void ShowWorldCreation()
        {
            worldSelectionPanel.SetActive(false);
            if (worldCreateUI != null)
            {
                worldCreateUI.gameObject.SetActive(true);
            }
        }

        public void ShowWorldSelection(bool isShow = true)
        {
            worldSelectionPanel.SetActive(isShow);
            if (isShow)
            {
                RefreshWorldList();
            }
        }

        public void AddWorld(WorldSaveData worldData)
        {
            // Just refresh the list - save system handles persistence
            RefreshWorldList();
        }
    }
}
