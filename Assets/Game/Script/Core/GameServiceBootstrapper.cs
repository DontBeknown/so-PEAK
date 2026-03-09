using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player;
using Game.Interaction;
using Game.UI;
using Game.Player.Stat.Assessment;
using Game.Player.Inventory;
using Game.Environment.DayNight;
using Game.Sound;

namespace Game.Core
{
    /// <summary>
    /// Bootstraps and registers all game services
    /// Should be attached to a persistent GameObject
    /// Executes early in the game lifecycle
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other scripts
    public class GameServiceBootstrapper : MonoBehaviour
    {
        [Header("Auto-Find Services")]
        [SerializeField] private bool autoFindServices = true;
        
        [Header("Manual References (Optional)")]
        [SerializeField] private PlayerControllerRefactored playerController;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryManagerRefactored inventoryManagerRefactored; 
        [SerializeField] private CraftingManager craftingManager;
        [SerializeField] private EquipmentManager equipmentManager;
        [SerializeField] private TabbedInventoryUI inventoryUI;
        [SerializeField] private InventoryUI legacyInventoryUI;
        [SerializeField] private CinemachinePlayerCamera playerCamera;

        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        private void Awake()
        {
            RegisterServices();
        }
        
        private void RegisterServices()
        {
            var container = ServiceContainer.Instance;
            
            // Register event bus first (other services may need it)
            var eventBus = new EventBus();
            container.Register<IEventBus>(eventBus);
            
            if (enableDebugLogs)
                Debug.Log("[GameServiceBootstrapper] Event Bus registered");
            
            // Auto-find or manually register services
            if (autoFindServices)
            {
                FindAndRegisterServices();
            }
            else
            {
                RegisterManualServices();
            }
            
            if (enableDebugLogs)
                Debug.Log("[GameServiceBootstrapper] All services registered and ready");
        }
        
        private void FindAndRegisterServices()
        {
            var container = ServiceContainer.Instance;
            
            // Find and register player controller
            var player = FindFirstObjectByType<PlayerControllerRefactored>();
            if (player != null)
            {
                container.Register(player);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerControllerRefactored found and registered");
            }
            
            // Find and register player stats
            var stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null)
            {
                container.Register(stats);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerStats found and registered");
            }
            
            // REFACTORED: InventoryManager services now registered by InventoryManagerRefactored.RegisterServices()
            // Find and ensure InventoryManagerRefactored initializes
            var inventoryRefactored = FindFirstObjectByType<InventoryManagerRefactored>();
            if (inventoryRefactored != null && enableDebugLogs)
            {
                Debug.Log("[GameServiceBootstrapper] InventoryManagerRefactored found (services will be auto-registered)");
            }
            
            // Find and register crafting
            var crafting = FindFirstObjectByType<CraftingManager>();
            if (crafting != null)
            {
                container.Register(crafting);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] CraftingManager found and registered");
            }
            
            // Find and register UI
            var inventoryUi = FindFirstObjectByType<TabbedInventoryUI>();
            if (inventoryUi != null)
            {
                container.Register(inventoryUi);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] TabbedInventoryUI found and registered");
            }
            
            // Find and register camera
            var camera = FindFirstObjectByType<CinemachinePlayerCamera>();
            if (camera != null)
            {
                container.Register(camera);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] CinemachinePlayerCamera found and registered");
            }
            
            // Find and register equipment manager
            var equipment = FindFirstObjectByType<EquipmentManager>();
            if (equipment != null)
            {
                container.Register(equipment);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] EquipmentManager found and registered");
            }
            
            // Find and register inventory UI
            var invUI = FindFirstObjectByType<InventoryUI>();
            if (invUI != null)
            {
                container.Register(invUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] InventoryUI found and registered");
            }
            
            // Register TooltipUI
            var tooltip = FindFirstObjectByType<TooltipUI>();
            if (tooltip != null)
            {
                container.Register(tooltip);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] TooltipUI found and registered");
            }
            
            // Register ContextMenuUI
            var contextMenu = FindFirstObjectByType<ContextMenuUI>();
            if (contextMenu != null)
            {
                container.Register(contextMenu);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] ContextMenuUI found and registered");
            }
            
            // Register InteractionDetector
            var interactionDetector = FindFirstObjectByType<InteractionDetector>();
            if (interactionDetector != null)
            {
                container.Register(interactionDetector);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] InteractionDetector found and registered");
            }
            
            // Register InteractionPromptUI
            var interactionPromptUI = FindFirstObjectByType<Game.Interaction.UI.InteractionPromptUI>();
            if (interactionPromptUI != null)
            {
                container.Register(interactionPromptUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] InteractionPromptUI found and registered");
            }
            
            // Register ItemNotificationUI
            var itemNotificationUI = FindFirstObjectByType<ItemNotificationUI>();
            if (itemNotificationUI != null)
            {
                container.Register(itemNotificationUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] ItemNotificationUI found and registered");
            }
            
            // Register SimpleStatsHUD
            var simpleStatsHUD = FindFirstObjectByType<SimpleStatsHUD>();
            if (simpleStatsHUD != null)
            {
                container.Register(simpleStatsHUD);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] SimpleStatsHUD found and registered");
            }
            
            // Register DayNightCycleManager
            var dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
            if (dayNightManager != null)
            {
                container.Register<IDayNightCycleService>(dayNightManager);
                container.Register<DayNightCycleManager>(dayNightManager);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] DayNightCycleManager found and registered");
            }
            
            // Register PlayerStatsTrackerUI
            var playerStatsTrackerUI = FindFirstObjectByType<PlayerStatsTrackerUI>();
            if (playerStatsTrackerUI != null)
            {
                container.Register(playerStatsTrackerUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerStatsTrackerUI found and registered");
            }
            
            // Register AssessmentReportUI
            var assessmentReportUI = FindFirstObjectByType<AssessmentReportUI>();
            if (assessmentReportUI != null)
            {
                container.Register(assessmentReportUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] AssessmentReportUI found and registered");
            }
            
            // Register LearningAssessmentService
            var learningAssessmentService = FindFirstObjectByType<LearningAssessmentService>();
            if (learningAssessmentService != null)
            {
                container.Register(learningAssessmentService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] LearningAssessmentService found and registered");
            }
            
            // Register PlayerStatsTrackerService
            var playerStatsTrackerService = FindFirstObjectByType<PlayerStatsTrackerService>();
            if (playerStatsTrackerService != null)
            {
                container.Register(playerStatsTrackerService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerStatsTrackerService found and registered");
            }
            
            // Find and register SaveLoadService
            var saveLoadService = FindFirstObjectByType<SaveLoadService>();
            if (saveLoadService != null)
            {
                container.Register<ISaveLoadService>(saveLoadService);
                container.Register(saveLoadService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] SaveLoadService found and registered");
            }

            // Find and register SoundService
            var soundService = FindFirstObjectByType<SoundService>();
            if (soundService != null)
            {
                soundService.Initialize();
                container.Register(soundService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] SoundService found and registered");
            }

        }
        
        private void RegisterManualServices()
        {
            var container = ServiceContainer.Instance;
            
            if (playerController != null)
            {
                container.Register(playerController);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerControllerRefactored manually registered");
            }
            
            if (playerStats != null)
            {
                container.Register(playerStats);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerStats manually registered");
            }
            
            if (craftingManager != null)
            {
                container.Register(craftingManager);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] CraftingManager manually registered");
            }
            
            if (inventoryUI != null)
            {
                container.Register(inventoryUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] TabbedInventoryUI manually registered");
            }
            
            if (playerCamera != null)
            {
                container.Register(playerCamera);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] CinemachinePlayerCamera manually registered");
            }
            
            if (equipmentManager != null)
            {
                container.Register(equipmentManager);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] EquipmentManager manually registered");
            }
            
            if (legacyInventoryUI != null)
            {
                container.Register(legacyInventoryUI);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] InventoryUI manually registered");
            }
        }
        
        /// <summary>
        /// Updates player service references after runtime player instantiation.
        /// Call this after spawning a new player GameObject to update ServiceContainer registrations.
        /// </summary>
        public void UpdatePlayerServices(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                Debug.LogError("[GameServiceBootstrapper] Cannot update player services - playerTransform is null!");
                return;
            }
            
            var container = ServiceContainer.Instance;
            
            // Update PlayerControllerRefactored
            var playerController = playerTransform.GetComponent<PlayerControllerRefactored>();
            if (playerController != null)
            {
                container.Register(playerController);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerControllerRefactored updated in ServiceContainer");
            }
            else
            {
                Debug.LogWarning("[GameServiceBootstrapper] PlayerControllerRefactored component not found on player!");
            }
            
            // Update PlayerStats
            var stats = playerTransform.GetComponent<PlayerStats>();
            if (stats != null)
            {
                container.Register(stats);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] PlayerStats updated in ServiceContainer");
            }
            else
            {
                Debug.LogWarning("[GameServiceBootstrapper] PlayerStats component not found on player!");
            }
            
            if (enableDebugLogs)
                Debug.Log("[GameServiceBootstrapper] Player services updated successfully");
        }
        
        private void OnDestroy()
        {
            // Optional: Clear services when destroyed
            // Uncomment if you want to clean up on scene unload
            // ServiceContainer.Instance.Clear();
        }
    }
}
