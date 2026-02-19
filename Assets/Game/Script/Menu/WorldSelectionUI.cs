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
        [SerializeField] private Button loadWorldButton;
        [SerializeField] private Button deleteWorldButton;

        [Header("Confirmation Dialog")]
        [SerializeField] private ConfirmationDialogUI confirmationDialog;
        
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
        private SaveMetadata currentSelectedWorld = null;
        private WorldSlotUI currentSelectedSlot = null;

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
            loadWorldButton.onClick.AddListener(OnLoadWorldClicked);
            deleteWorldButton.onClick.AddListener(OnDeleteWorldClicked);
            
            // Initialize button states (Load/Delete disabled until selection)
            UpdateActionButtonStates();

            // Load worlds from save system
            RefreshWorldList();
        }

        private void OnDestroy()
        {
            // Clean up listeners
            backButton.onClick.RemoveListener(OnBackClicked);
            createWorldButton.onClick.RemoveListener(OnCreateWorldClicked);
            loadWorldButton.onClick.RemoveListener(OnLoadWorldClicked);
            deleteWorldButton.onClick.RemoveListener(OnDeleteWorldClicked);
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

            // Clear selection if the selected world was deleted
            if (currentSelectedWorld != null)
            {
                bool stillExists = worldMetadata.Any(w => w.worldGuid == currentSelectedWorld.worldGuid);
                if (!stillExists)
                {
                    ClearSelection();
                }
            }

            // Update button states
            UpdateActionButtonStates();
        }

        private void CreateWorldSlot(SaveMetadata metadata)
        {
            GameObject slotObj = Instantiate(worldSlotPrefab, worldListContainer);
            WorldSlotUI slot = slotObj.GetComponent<WorldSlotUI>();
            
            if (slot != null)
            {
                slot.Initialize(metadata, OnWorldSlotSelected);
                worldSlots.Add(slot);
            }
        }

        private void OnBackClicked()
        {
            if (enableDebug) Debug.Log("Back button clicked - Returning to main menu");
            
            // Clear selection
            ClearSelection();

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

        private void OnLoadWorldClicked()
        {
            if (currentSelectedWorld == null)
            {
                Debug.LogWarning("[WorldSelectionUI] No world selected");
                return;
            }

            if (enableDebug) Debug.Log($"Loading world: {currentSelectedWorld.worldName}");
            
            // Disable all buttons during loading
            SetButtonsInteractable(false);
            
            LoadWorld(currentSelectedWorld);
        }

        private void OnDeleteWorldClicked()
        {
            if (currentSelectedWorld == null)
            {
                Debug.LogWarning("[WorldSelectionUI] No world selected");
                return;
            }

            if (confirmationDialog == null)
            {
                Debug.LogError("[WorldSelectionUI] ConfirmationDialog not assigned!");
                return;
            }

            string worldName = currentSelectedWorld.worldName;
            if (enableDebug) Debug.Log($"Delete requested for world: {worldName}");

            // Show confirmation dialog
            confirmationDialog.Show(
                title: "Are you sure you want to delete this save?",
                message: $"Delete world '{worldName}' This action cannot be undone.",
                onConfirm: () => ConfirmDeleteWorld(currentSelectedWorld),
                onCancel: null,
                confirmText: "Yes",
                cancelText: "No"
            );
        }

        private void ConfirmDeleteWorld(SaveMetadata metadata)
        {
            if (saveLoadService == null)
            {
                Debug.LogError("[WorldSelectionUI] Cannot delete world - SaveLoadService not available");
                return;
            }
            
            if (enableDebug) Debug.Log($"Confirmed deletion of world: {metadata.worldName}");
            
            // Delete through save system
            saveLoadService.DeleteWorld(metadata.worldGuid);
            
            // Clear selection
            ClearSelection();
            
            // Refresh the list
            RefreshWorldList();
        }

        /// <summary>
        /// Called when a world slot is clicked - selects the world
        /// </summary>
        private void OnWorldSlotSelected(SaveMetadata metadata)
        {
            if (enableDebug) Debug.Log($"World slot selected: {metadata.worldName}");
            
            // Deselect previous slot
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.Deselect();
            }
            
            // Find and select new slot
            currentSelectedSlot = worldSlots.FirstOrDefault(s => s.WorldMetadata.worldGuid == metadata.worldGuid);
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.Select();
            }
            
            // Update current selection
            currentSelectedWorld = metadata;
            
            // Update button states
            UpdateActionButtonStates();
        }

        /// <summary>
        /// Clear the current world selection
        /// </summary>
        private void ClearSelection()
        {
            if (currentSelectedSlot != null)
            {
                currentSelectedSlot.Deselect();
                currentSelectedSlot = null;
            }
            
            currentSelectedWorld = null;
            UpdateActionButtonStates();
        }

        /// <summary>
        /// Update Load/Delete button interactability based on selection
        /// </summary>
        private void UpdateActionButtonStates()
        {
            bool hasSelection = currentSelectedWorld != null;
            
            if (loadWorldButton != null)
            {
                loadWorldButton.interactable = hasSelection;
            }
            
            if (deleteWorldButton != null)
            {
                deleteWorldButton.interactable = hasSelection;
            }
        }

        /// <summary>
        /// Set all buttons interactable/non-interactable (used during loading)
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (backButton != null) backButton.interactable = interactable;
            if (createWorldButton != null) createWorldButton.interactable = interactable;
            if (loadWorldButton != null) loadWorldButton.interactable = interactable;
            if (deleteWorldButton != null) deleteWorldButton.interactable = interactable;
        }

        /// <summary>
        /// Load the selected world
        /// </summary>
        private void LoadWorld(SaveMetadata metadata)
        {

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

        /// <summary>
        /// Select a world by its GUID (e.g., after creating a new world)
        /// </summary>
        public void SelectWorldByGuid(string worldGuid)
        {
            if (string.IsNullOrEmpty(worldGuid))
                return;

            var slot = worldSlots.FirstOrDefault(s => s.WorldMetadata.worldGuid == worldGuid);
            if (slot != null)
            {
                OnWorldSlotSelected(slot.WorldMetadata);
            }
        }
    }
}
