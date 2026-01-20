using UnityEngine;
using Game.Player.Interfaces;
using Game.Player.Services;

namespace Game.Player
{
    /// <summary>
    /// Fully refactored PlayerController using SOLID principles.
    /// This is an alternative implementation showcasing full dependency injection.
    /// Use this as a reference or migrate to it gradually.
    /// </summary>
    public class PlayerControllerRefactored : MonoBehaviour, IStateTransitioner
    {
        [Header("Configuration")]
        [SerializeField] private PlayerConfig config;
        
        [Header("Debug Settings")]
        [SerializeField] private bool enableInventoryCommandDebugLogs = false;

        [Header("Inventory System References")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private CraftingManager craftingManager;
        [SerializeField] private ItemDetector itemDetector;
        [SerializeField] private TabbedInventoryUI tabbedInventoryUI;
        [SerializeField] private CinemachinePlayerCamera playerCamera;

        // Core Components
        private PlayerModelRefactored _model;
        private IPlayerState _currentState;
        
        // Services
        private PlayerInputHandler _inputHandler;
        private PlayerInventoryFacade _inventoryFacade;
        private IPhysicsService _physicsService;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeModel();
            InitializeServices();
            InitializeInventory();
        }

        private void Start()
        {
            // Start in walking state
            TransitionTo(new WalkingState(this));
        }

        private void OnEnable()
        {
            _inputHandler?.Enable();
        }

        private void OnDisable()
        {
            _inputHandler?.Disable();
        }

        private void OnDestroy()
        {
            _inputHandler?.Dispose();
        }

        private void FixedUpdate()
        {
            UpdateState();
            HandleAutomaticTransitions();
        }

        #endregion

        #region Initialization

        private void InitializeModel()
        {
            _model = new PlayerModelRefactored(gameObject, config);
            _physicsService = _model.GetPhysicsService();
        }

        private void InitializeServices()
        {
            // Create input handler with dependencies
            _inputHandler = new PlayerInputHandler(this, _model);
            
            // Subscribe to input events
            _inputHandler.OnPickupRequested += HandlePickupInput;
            _inputHandler.OnInventoryToggleRequested += HandleInventoryToggle;
            _inputHandler.OnQuickUseRequested += HandleQuickUse;
        }

        private void InitializeInventory()
        {
            // Auto-assign components
            inventoryManager ??= GetComponent<InventoryManager>();
            craftingManager ??= GetComponent<CraftingManager>();
            itemDetector ??= GetComponent<ItemDetector>();
            tabbedInventoryUI ??= FindFirstObjectByType<TabbedInventoryUI>();
            playerCamera ??= FindFirstObjectByType<CinemachinePlayerCamera>();

            // Create facade with Command Pattern support
            _inventoryFacade = new PlayerInventoryFacade(
                inventoryManager,
                craftingManager,
                itemDetector,
                tabbedInventoryUI,
                _model.Stats,
                transform,
                enableInventoryCommandDebugLogs,
                playerCamera
            );
            
            // Connect inventory facade to input handler for input blocking
            _inputHandler?.SetInventoryFacade(_inventoryFacade);
        }

        #endregion

        #region State Management

        private void UpdateState()
        {
            if (_currentState != null && _inputHandler != null)
            {
                _currentState.FixedUpdate(_model, _inputHandler.MoveInput);
            }
        }

        private void HandleAutomaticTransitions()
        {
            // Don't interrupt climbing with automatic transitions
            if (_currentState is ClimbingState)
                return;

            // Check for falling
            if (!_physicsService.IsGrounded())
            {
                if (!(_currentState is FallingState))
                {
                    //TransitionTo(new FallingState(this));
                }
            }
            // Return to walking when landed
            else if (!(_currentState is WalkingState))
            {
                TransitionTo(new WalkingState(this));
            }
        }

        #endregion

        #region IStateTransitioner Implementation

        public void TransitionTo(IPlayerState newState)
        {
            if (newState == null)
            {
                Debug.LogError("PlayerControllerRefactored: Cannot transition to null state!");
                return;
            }

            _currentState?.Exit(_model);
            _currentState = newState;
            _currentState.Enter(_model);

            #if UNITY_EDITOR
            //Debug.Log($"State: {newState.GetType().Name}");
            #endif
        }

        public void TransitionTo<TState>() where TState : IPlayerState, new()
        {
            TransitionTo(new TState());
        }

        public IPlayerState CurrentState => _currentState;

        #endregion

        #region Input Handlers

        private void HandlePickupInput()
        {
            _inventoryFacade?.TryPickupNearestItem();
        }

        private void HandleInventoryToggle()
        {
            _inventoryFacade?.ToggleInventory();
        }

        private void HandleQuickUse()
        {
            _inventoryFacade?.QuickUseConsumable();
        }

        #endregion

        #region Public API (for UI and other systems)

        public ResourceCollector GetTargetItem() => _inventoryFacade?.GetNearestItem();
        public void ConsumeItem(InventoryItem item) => _inventoryFacade?.ConsumeItem(item);
        public void StartCrafting(CraftingRecipe recipe) => _inventoryFacade?.StartCrafting(recipe);
        
        public InventoryManager GetInventoryManager() => _inventoryFacade?.InventoryManager;
        public ItemDetector GetItemDetector() => _inventoryFacade?.ItemDetector;
        public IPlayerState GetCurrentState() => _currentState;

        // Inventory Command Pattern - Undo/Redo
        public bool UndoLastInventoryAction() => _inventoryFacade?.UndoLastAction() ?? false;
        public bool RedoLastInventoryAction() => _inventoryFacade?.RedoLastAction() ?? false;
        public string GetUndoDescription() => _inventoryFacade?.GetUndoDescription() ?? "Nothing to undo";
        public string GetRedoDescription() => _inventoryFacade?.GetRedoDescription() ?? "Nothing to redo";
        public void ClearInventoryCommandHistory() => _inventoryFacade?.ClearCommandHistory();

        // Input Control
        public void SetInputBlocked(bool blocked)
        {
            _inputHandler?.SetInputBlocked(blocked);
        }

        #endregion
    }
}
