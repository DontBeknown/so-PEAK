using UnityEngine;
using Game.Player.Interfaces;
using Game.Player.Services;
using Game.Core.DI;
using Game.UI;
using Game.Player.Inventory;

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
        // InventoryManager removed - now uses IInventoryService from ServiceContainer
        [SerializeField] private CraftingManager craftingManager;
        [SerializeField] private UIServiceProvider uiServiceProvider;
        [SerializeField] private CinemachinePlayerCamera playerCamera;

        [Header("Interaction System")]
        [SerializeField] private Game.Interaction.InteractionDetector interactionDetector;

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
            InitializeInteraction();
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
            _inputHandler.OnInteractRequested += HandleInteractInput;
            _inputHandler.OnInventoryToggleRequested += HandleInventoryToggle;
            _inputHandler.OnQuickUseRequested += HandleQuickUse;
        }

        private void InitializeInventory()
        {
            // Resolve services from ServiceContainer
            var inventoryService = ServiceContainer.Instance.Get<IInventoryService>();
            craftingManager ??= GetComponent<CraftingManager>();
            
            // Use ServiceContainer for cross-scene references
            if (uiServiceProvider == null)
                uiServiceProvider = ServiceContainer.Instance.TryGet<UIServiceProvider>();
            if (playerCamera == null)
                playerCamera = ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();

            // Create facade with IInventoryService (SOLID: Dependency Injection)
            _inventoryFacade = new PlayerInventoryFacade(
                inventoryService,
                craftingManager,
                uiServiceProvider,
                _model.Stats,
                transform,
                enableInventoryCommandDebugLogs,
                playerCamera
            );
            
            // Connect inventory facade to input handler for input blocking
            _inputHandler?.SetInventoryFacade(_inventoryFacade);
        }

        private void InitializeInteraction()
        {
            // Auto-assign InteractionDetector component
            interactionDetector ??= GetComponent<Game.Interaction.InteractionDetector>();
        }

        #endregion

        #region State Management

        private void UpdateState()
        {
            // Skip if controller is disabled
            if (_model?.Controller != null && !_model.Controller.enabled)
                return;

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

            bool isGrounded = _physicsService.IsGrounded();
            bool isSprintHeld = _inputHandler != null && _inputHandler.IsSprintHeld;
            bool isMoving = _inputHandler != null && _inputHandler.MoveInput.sqrMagnitude > 0.01f;

            // ── Airborne check ─────────────────────────────────────────
            if (!isGrounded)
            {
                if (!(_currentState is FallingState))
                {
                    //TransitionTo(new FallingState(this));
                }
                return;
            }

            // ── Grounded transitions ───────────────────────────────────

            // Landing from a fall — sprint-held lands into RunningState
            if (_currentState is FallingState)
            {
                if (isSprintHeld && isMoving)
                {
                    TransitionTo(new RunningState(this));
                }
                else
                {
                    TransitionTo(new WalkingState(this));
                }
                return;
            }

            // Walking → Running (sprint pressed while moving)
            if (_currentState is WalkingState && isSprintHeld && isMoving)
            {
                TransitionTo(new RunningState(this));
                return;
            }

            // Running → Walking (sprint released, stopped moving, or input ceased)
            if (_currentState is RunningState && (!isSprintHeld || !isMoving))
            {
                TransitionTo(new WalkingState(this));
                return;
            }

            // Fallback: ensure we're in a grounded state
            if (!(_currentState is WalkingState) && !(_currentState is RunningState))
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

        private void HandleInteractInput()
        {
            // Use new interaction system
            if (interactionDetector != null)
            {
                interactionDetector.TryInteractWithNearest();
            }
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
        
        public void ConsumeItem(InventoryItem item) => _inventoryFacade?.ConsumeItem(item);
        public void StartCrafting(CraftingRecipe recipe) => _inventoryFacade?.StartCrafting(recipe);
        
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
        
        /// <summary>
        /// Check if the pickup/interact button is currently held down
        /// </summary>
        public bool IsPickupButtonHeld => _inputHandler?.IsPickupButtonHeld ?? false;
        
        /// <summary>
        /// Check the raw physical state of the pickup button, ignoring input blocking.
        /// Used by gathering system to detect button release even when movement is locked.
        /// </summary>
        public bool IsPickupButtonPhysicallyHeld => _inputHandler?.IsPickupButtonPhysicallyHeld ?? false;
        #endregion
    }
}
