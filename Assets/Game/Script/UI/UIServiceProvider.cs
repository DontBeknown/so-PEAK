using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player;
using System.Linq;

namespace Game.UI
{
    /// <summary>
    /// Central service provider for UI system
    /// Uses Service Locator pattern with dependency injection
    /// Replaces the old UIManager
    /// </summary>
    public class UIServiceProvider : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerControllerRefactored playerController;
        [SerializeField] private CinemachinePlayerCamera playerCamera;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private UIPanelController _panelController;
        private ICursorManager _cursorManager;
        private IInputBlocker _inputBlocker;
        
        // Singleton for easy access (can be replaced with DI container later)
        private static UIServiceProvider _instance;
        public static UIServiceProvider Instance => _instance;
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Register self in service container
            ServiceContainer.Instance.Register(this);
            
            // Get player from service container
            if (playerController == null)
            {
                playerController = ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();
                
                if (playerController == null)
                    Debug.LogWarning("[UIServiceProvider] PlayerController not registered in ServiceContainer!");
            }
            
            // Get camera from service container
            if (playerCamera == null)
            {
                playerCamera = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
                
                if (playerCamera == null)
                    Debug.LogWarning("[UIServiceProvider] PlayerCamera not registered in ServiceContainer!");
            }
            
            // Initialize services
            InitializeServices();
            RegisterAllPanels();
            
        }
        
        private void InitializeServices()
        {
            // Get EventBus from ServiceContainer
            var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            
            // Create services with dependency injection
            _cursorManager = new CursorManager();
            _inputBlocker = new PlayerInputBlocker(playerController, playerCamera);
            _panelController = new UIPanelController(_cursorManager, _inputBlocker, eventBus);
            
            if (enableDebugLogs)
                Debug.Log("[UIServiceProvider] Services initialized");
        }
        
        /// <summary>
        /// Ensures services are initialized. Call this if you need to access UIServiceProvider early.
        /// </summary>
        public void EnsureInitialized()
        {
            if (_panelController == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[UIServiceProvider] Services not initialized, initializing now...");
                InitializeServices();
                RegisterAllPanels();
            }
        }
        
        /// <summary>
        /// Updates player references when a new player is spawned at runtime.
        /// Call this after instantiating a new player GameObject.
        /// </summary>
        public void UpdatePlayerReferences(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("[UIServiceProvider] Cannot update player references - playerTransform is null!");
                return;
            }
            
            // Update player controller reference
            playerController = playerTransform.GetComponent<PlayerControllerRefactored>();
            if (playerController == null)
            {
                Debug.LogError("[UIServiceProvider] PlayerControllerRefactored not found on new player!");
                return;
            }
            
            // Get camera from ServiceContainer (should be updated by RenderController)
            playerCamera = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
            
            // Recreate input blocker with new player references
            _inputBlocker = new PlayerInputBlocker(playerController, playerCamera);
            
            // Update panel controller with new input blocker
            if (_panelController != null && _inputBlocker != null)
            {
                // Panel controller needs to be recreated to use new input blocker
                var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
                _panelController = new UIPanelController(_cursorManager, _inputBlocker, eventBus);
                
                // Re-register all panels
                RegisterAllPanels();
            }
            
            if (enableDebugLogs)
                Debug.Log("[UIServiceProvider] Player references updated successfully");
        }
        
        private void RegisterAllPanels()
        {
            // Find all IUIPanel implementations in scene
            var panels = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IUIPanel>();
            
            foreach (var panel in panels)
            {
                _panelController.RegisterPanel(panel);
            }
            
            if (enableDebugLogs)
                Debug.Log($"[UIServiceProvider] Registered {panels.Count()} panels");
        }
        
        // Public API
        public UIPanelController PanelController => _panelController;
        public ICursorManager CursorManager => _cursorManager;
        public IInputBlocker InputBlocker => _inputBlocker;
        
        // Convenience methods with null safety
        public void OpenPanel(string panelName)
        {
            if (_panelController != null)
                _panelController.OpenPanel(panelName);
            else if (enableDebugLogs)
                Debug.LogWarning("[UIServiceProvider] PanelController not initialized yet");
        }
        
        public void ClosePanel(string panelName)
        {
            if (_panelController != null)
                _panelController.ClosePanel(panelName);
            else if (enableDebugLogs)
                Debug.LogWarning("[UIServiceProvider] PanelController not initialized yet");
        }
        
        public void TogglePanel(string panelName)
        {
            if (_panelController != null)
                _panelController.TogglePanel(panelName);
            else if (enableDebugLogs)
                Debug.LogWarning("[UIServiceProvider] PanelController not initialized yet");
        }
        
        public void CloseAllPanels()
        {
            if (_panelController != null)
                _panelController.CloseAllPanels();
        }
        
        public bool IsAnyPanelOpen() => _panelController?.IsAnyPanelOpen() ?? false;
        
        public IUIPanel GetPanel(string panelName) => _panelController?.GetPanel(panelName);
        
        public T GetPanel<T>() where T : class, IUIPanel => _panelController?.GetPanel<T>();
    }
}
