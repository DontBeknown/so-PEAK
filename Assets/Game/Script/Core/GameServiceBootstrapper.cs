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
using Game.Collectable;
using Game.Dialog;
using Game.Tutorial;
using Game.Progression;

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
        [SerializeField] private CollectableManager collectableManager;
        [SerializeField] private DialogManager dialogManager;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private StarterCollectableService starterCollectableService;
        [SerializeField] private LevelBonusCollectableService levelBonusCollectableService;

        
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
            var eventBus  = container.Get<IEventBus>(); // Already registered above
            
            // Find and register player controller
            var player = FindAndRegister<PlayerControllerRefactored>("[GameServiceBootstrapper] PlayerControllerRefactored found and registered");
            
            // Find and register player stats
            var stats = FindAndRegister<PlayerStats>("[GameServiceBootstrapper] PlayerStats found and registered");
            
            var inventoryRefactored = FindFirstObjectByType<InventoryManagerRefactored>();
            if (inventoryRefactored != null && enableDebugLogs)
            {
                Debug.Log("[GameServiceBootstrapper] InventoryManagerRefactored found (services will be auto-registered)");
            }
            
            // Find and register crafting
            var crafting = FindAndRegister<CraftingManager>("[GameServiceBootstrapper] CraftingManager found and registered");
            
            // Find and register UI
            var inventoryUi = FindAndRegister<TabbedInventoryUI>("[GameServiceBootstrapper] TabbedInventoryUI found and registered");
            
            // Find and register camera
            var camera = FindAndRegister<CinemachinePlayerCamera>("[GameServiceBootstrapper] CinemachinePlayerCamera found and registered");
            
            // Find and register equipment manager
            var equipment = FindAndRegister<EquipmentManager>("[GameServiceBootstrapper] EquipmentManager found and registered");
            
            // Find and register inventory UI
            var invUI = FindAndRegister<InventoryUI>("[GameServiceBootstrapper] InventoryUI found and registered");
            
            // Register TooltipUI
            var tooltip = FindAndRegister<TooltipUI>("[GameServiceBootstrapper] TooltipUI found and registered");
            
            // Register ContextMenuUI
            var contextMenu = FindAndRegister<ContextMenuUI>("[GameServiceBootstrapper] ContextMenuUI found and registered");
            
            // Register InteractionDetector
            var interactionDetector = FindAndRegister<InteractionDetector>("[GameServiceBootstrapper] InteractionDetector found and registered");
            
            // Register InteractionPromptUI
            var interactionPromptUI = FindAndRegister<Game.Interaction.UI.InteractionPromptUI>("[GameServiceBootstrapper] InteractionPromptUI found and registered");
            
            // Register ItemNotificationUI
            var itemNotificationUI = FindAndRegister<ItemNotificationUI>("[GameServiceBootstrapper] ItemNotificationUI found and registered");
            
            // Register SimpleStatsHUD
            var simpleStatsHUD = FindAndRegister<SimpleStatsHUD>("[GameServiceBootstrapper] SimpleStatsHUD found and registered");
            
            // Find and register SoundService first - DayNightCycleManager needs it.
            var soundService = FindFirstObjectByType<SoundService>();
            if (soundService != null)
            {
                soundService.Initialize();
                container.Register(soundService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] SoundService found and registered");
            }

            // Register DayNightCycleManager
            var dayNightManager = FindFirstObjectByType<DayNightCycleManager>();
            if (dayNightManager != null)
            {
                container.Register<IDayNightCycleService>(dayNightManager);
                container.Register<DayNightCycleManager>(dayNightManager);
                dayNightManager.Initialize(eventBus, soundService, equipment);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] DayNightCycleManager found and registered");
            }
            
            // Register PlayerStatsTrackerUI
            var playerStatsTrackerUI = FindAndRegister<PlayerStatsTrackerUI>("[GameServiceBootstrapper] PlayerStatsTrackerUI found and registered");
            
            // Register AssessmentReportUI
            var assessmentReportUI = FindAndRegister<AssessmentReportUI>("[GameServiceBootstrapper] AssessmentReportUI found and registered");
            
            // Register EndingScreenUI
            var endingScreenUI = FindAndRegister<Game.UI.EndingScreen.EndingScreenUI>("[GameServiceBootstrapper] EndingScreenUI found and registered");
            
            // Register LearningAssessmentService
            var learningAssessmentService = FindAndRegister<LearningAssessmentService>("[GameServiceBootstrapper] LearningAssessmentService found and registered");
            
            // Register PlayerStatsTrackerService
            var playerStatsTrackerService = FindAndRegister<PlayerStatsTrackerService>("[GameServiceBootstrapper] PlayerStatsTrackerService found and registered");
            
            // Find and register SaveLoadService
            var saveLoadService = FindFirstObjectByType<SaveLoadService>();
            if (saveLoadService != null)
            {
                container.Register<ISaveLoadService>(saveLoadService);
                container.Register(saveLoadService);
                saveLoadService.Initialize();
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] SaveLoadService found and registered");
            }

            // Find and register collectable manager
            var cm = collectableManager ?? FindFirstObjectByType<CollectableManager>();
            if (cm != null)
            {
                container.Register<ICollectableManager>(cm);
                container.Register(cm);
                cm.Initialize(eventBus);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] CollectableManager found and registered");
            }

            // Find and register dialog manager
            var dm = dialogManager ?? FindFirstObjectByType<DialogManager>();
            if (dm != null)
            {
                container.Register<IDialogManager>(dm);
                container.Register(dm);
                dm.Initialize(eventBus);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] DialogManager found and registered");
            }

            // Find and register TutorialManager
            var tm = tutorialManager ?? FindFirstObjectByType<TutorialManager>();
            if (tm != null)
            {
                container.Register<ITutorialManager>(tm);
                container.Register(tm);

                var playerController = container.TryGet<PlayerControllerRefactored>();
                var playerCamera = container.TryGet<CinemachinePlayerCamera>();
                tm.Initialize(eventBus, saveLoadService, playerController, playerCamera);

                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] TutorialManager found and registered");
            }

            // Find and register UIServiceProvider
            var uiServiceProvider = FindFirstObjectByType<UIServiceProvider>();
            if (uiServiceProvider != null)
            {
                container.Register(uiServiceProvider);
                uiServiceProvider.EnsureInitialized();
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] UIServiceProvider found and registered");
            }

            // Find and register StarterCollectableService
            var starterService = starterCollectableService ?? FindFirstObjectByType<StarterCollectableService>();
            if (starterService != null)
            {
                container.Register(starterService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] StarterCollectableService found and registered");
            }

            // Find and initialize LevelBonusCollectableService
            var levelBonusService = levelBonusCollectableService ?? FindFirstObjectByType<LevelBonusCollectableService>();
            if (levelBonusService != null)
            {
                container.Register(levelBonusService);
                levelBonusService.Initialize(eventBus, cm, saveLoadService, starterService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] LevelBonusCollectableService found, registered, and initialized");
            }
        }
        
        private void RegisterManualServices()
        {
            var container = ServiceContainer.Instance;
            var eventBus  = container.Get<IEventBus>();

            RegisterAndLog(playerController, "[GameServiceBootstrapper] PlayerControllerRefactored manually registered");
            RegisterAndLog(playerStats, "[GameServiceBootstrapper] PlayerStats manually registered");
            RegisterAndLog(craftingManager, "[GameServiceBootstrapper] CraftingManager manually registered");
            RegisterAndLog(inventoryUI, "[GameServiceBootstrapper] TabbedInventoryUI manually registered");
            RegisterAndLog(playerCamera, "[GameServiceBootstrapper] CinemachinePlayerCamera manually registered");
            RegisterAndLog(equipmentManager, "[GameServiceBootstrapper] EquipmentManager manually registered");
            RegisterAndLog(legacyInventoryUI, "[GameServiceBootstrapper] InventoryUI manually registered");

            if (collectableManager != null)
            {
                container.Register<ICollectableManager>(collectableManager);
                container.Register(collectableManager);
                collectableManager.Initialize(eventBus);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] CollectableManager manually registered");
            }

            if (dialogManager != null)
            {
                container.Register<IDialogManager>(dialogManager);
                container.Register(dialogManager);
                dialogManager.Initialize(eventBus);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] DialogManager manually registered");
            }

            if (tutorialManager != null)
            {
                container.Register<ITutorialManager>(tutorialManager);
                container.Register(tutorialManager);

                var player = container.TryGet<PlayerControllerRefactored>();
                var camera = container.TryGet<CinemachinePlayerCamera>();
                var svc    = container.TryGet<ISaveLoadService>();
                tutorialManager.Initialize(eventBus, svc, player, camera);

                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] TutorialManager manually registered");
            }

            if (starterCollectableService != null)
            {
                container.Register(starterCollectableService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] StarterCollectableService manually registered");
            }

            if (levelBonusCollectableService != null)
            {
                container.Register(levelBonusCollectableService);
                var cm = container.TryGet<ICollectableManager>();
                var svc = container.TryGet<ISaveLoadService>();
                levelBonusCollectableService.Initialize(eventBus, cm, svc, starterCollectableService);
                if (enableDebugLogs)
                    Debug.Log("[GameServiceBootstrapper] LevelBonusCollectableService manually registered and initialized");
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

        private T FindAndRegister<T>(string logMessage) where T : Component
        {
            var instance = FindFirstObjectByType<T>();
            RegisterAndLog(instance, logMessage);
            return instance;
        }

        private void RegisterAndLog<T>(T instance, string logMessage) where T : class
        {
            if (instance == null)
            {
                return;
            }

            ServiceContainer.Instance.Register(instance);
            if (enableDebugLogs)
            {
                Debug.Log(logMessage);
            }
        }
    }
}
