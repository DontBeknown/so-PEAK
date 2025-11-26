using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
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

        private List<WorldSlotUI> worldSlots = new List<WorldSlotUI>();
        private bool isDeleteMode = false;

        // Placeholder for world data - will be replaced with actual save/load system
        private List<WorldData> worlds = new List<WorldData>();

        private void Start()
        {
            // Setup button listeners
            backButton.onClick.AddListener(OnBackClicked);
            createWorldButton.onClick.AddListener(OnCreateWorldClicked);
            deleteModeButton.onClick.AddListener(OnDeleteModeToggled);
            
            if (cancelDeleteButton != null)
            {
                cancelDeleteButton.onClick.AddListener(OnCancelDeleteClicked);
            }

            // Initialize with some placeholder worlds for testing
            InitializePlaceholderWorlds();
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

        private void InitializePlaceholderWorlds()
        {
            // TODO: Replace with actual save/load system
            worlds.Add(new WorldData("My First World", 12345, "2025-11-26", 120));
            worlds.Add(new WorldData("Adventure Time", 67890, "2025-11-25", 45));
            worlds.Add(new WorldData("Mountain Peak", 11111, "2025-11-24", 200));
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

            // Create new slots for each world
            foreach (var worldData in worlds)
            {
                CreateWorldSlot(worldData);
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

        private void CreateWorldSlot(WorldData worldData)
        {
            GameObject slotObj = Instantiate(worldSlotPrefab, worldListContainer);
            WorldSlotUI slot = slotObj.GetComponent<WorldSlotUI>();
            
            if (slot != null)
            {
                slot.Initialize(worldData, OnWorldSelected, OnWorldDeleted);
                worldSlots.Add(slot);
            }
        }

        private void OnBackClicked()
        {
            Debug.Log("Back button clicked - Returning to main menu");
            
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
            
            gameObject.SetActive(false);
        }

        private void OnCreateWorldClicked()
        {
            Debug.Log("Create World button clicked");
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

            Debug.Log($"Delete mode: {(isDeleteMode ? "ENABLED" : "DISABLED")}");
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

        private void OnWorldSelected(WorldData worldData)
        {
            if (isDeleteMode)
            {
                Debug.LogWarning("Cannot select world in delete mode");
                return;
            }

            //Debug.Log($"World selected: {worldData.WorldName}");
            // TODO: Load the world and switch to game scene
            LoadWorld(worldData);
        }

        private void OnWorldDeleted(WorldData worldData)
        {
            Debug.Log($"Deleting world: {worldData.WorldName}");
            // TODO: Implement actual world deletion from save system
            worlds.Remove(worldData);
            RefreshWorldList();

            // Exit delete mode if no worlds left
            if (worlds.Count == 0)
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

        public void ShowWorldSelection()
        {
            worldSelectionPanel.SetActive(true);
            if (worldCreateUI != null)
            {
                worldCreateUI.gameObject.SetActive(false);
            }
            RefreshWorldList();
        }

        public void AddWorld(WorldData worldData)
        {
            worlds.Add(worldData);
            RefreshWorldList();
        }

        private void LoadWorld(WorldData worldData)
        {
            // TODO: Implement actual world loading
            Debug.Log($"Loading world: {worldData.WorldName} (Seed: {worldData.Seed})");
            
            // Placeholder: Load game scene
            SceneManager.LoadScene("TerrainGenDemo");
        }
    }
}
