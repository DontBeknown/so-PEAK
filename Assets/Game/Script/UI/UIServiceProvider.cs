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
            
            // Register self in service container
            ServiceContainer.Instance.Register(this);
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
        
        // Convenience methods
        public void OpenPanel(string panelName) => _panelController.OpenPanel(panelName);
        public void ClosePanel(string panelName) => _panelController.ClosePanel(panelName);
        public void TogglePanel(string panelName) => _panelController.TogglePanel(panelName);
        public void CloseAllPanels() => _panelController.CloseAllPanels();
        public bool IsAnyPanelOpen() => _panelController.IsAnyPanelOpen();
        public IUIPanel GetPanel(string panelName) => _panelController.GetPanel(panelName);
        public T GetPanel<T>() where T : class, IUIPanel => _panelController.GetPanel<T>();
    }
}
