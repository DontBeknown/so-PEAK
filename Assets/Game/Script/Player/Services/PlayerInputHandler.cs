using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player.Interfaces;

namespace Game.Player.Services
{
    /// <summary>
    /// Handles player input coordination between Input System and game logic.
    /// Separates input handling concerns from PlayerController.
    /// </summary>
    public class PlayerInputHandler
    {
        private readonly IA_PlayerController _inputActions;
        private readonly IStateTransitioner _stateTransitioner;
        private readonly PlayerModelRefactored _model;
        private PlayerInventoryFacade _inventoryFacade;
        
        private Vector2 _moveInput;
        private bool _inputBlocked = false;

        // Events for UI/Inventory actions
        public System.Action OnPickupRequested;
        public System.Action OnInventoryToggleRequested;
        public System.Action OnQuickUseRequested;

        public Vector2 MoveInput => _moveInput;

        public PlayerInputHandler(IStateTransitioner stateTransitioner, PlayerModelRefactored model)
        {
            _stateTransitioner = stateTransitioner;
            _model = model;
            _inputActions = new IA_PlayerController();
            
            BindInputActions();
        }
        
        /// <summary>
        /// Set the inventory facade reference to check inventory state
        /// </summary>
        public void SetInventoryFacade(PlayerInventoryFacade inventoryFacade)
        {
            _inventoryFacade = inventoryFacade;
        }
        
        /// <summary>
        /// Blocks or unblocks all player input
        /// Call with true to stop player movement (e.g., when UI is open)
        /// Call with false to resume normal input
        /// </summary>
        public void SetInputBlocked(bool blocked)
        {
            _inputBlocked = blocked;
            
            // Clear move input when blocking
            if (blocked)
            {
                _moveInput = Vector2.zero;
            }
        }

        private void BindInputActions()
        {
            // Movement input (continuous) - block when input is blocked or inventory is open
            _inputActions.Player.Move.performed += ctx => 
            {
                if (!IsInputBlocked())
                    _moveInput = ctx.ReadValue<Vector2>();
            };
            _inputActions.Player.Move.canceled += _ => _moveInput = Vector2.zero;

            // Action inputs - delegate to current state (block when input is blocked or inventory is open)
            _inputActions.Player.Jump.performed += _ => 
            {
                if (!IsInputBlocked())
                    HandleJumpInput();
            };
            _inputActions.Player.Climb.performed += _ => 
            {
                if (!IsInputBlocked())
                    HandleClimbInput();
            };

            // Item/UI inputs - trigger events (inventory toggle always works)
            _inputActions.Player.Pickup.performed += _ => 
            {
                if (!IsInputBlocked())
                    OnPickupRequested?.Invoke();
            };
            _inputActions.Player.OpenInventory.performed += _ => OnInventoryToggleRequested?.Invoke();
            
            // Uncomment when sprint is added to input actions
            // _inputActions.Player.QuickUse.performed += _ => 
            // {
            //     if (!IsInventoryOpen())
            //         OnQuickUseRequested?.Invoke();
            // };
        }
        
        private bool IsInventoryOpen()
        {
            return _inventoryFacade?.IsInventoryOpen ?? false;
        }
        
        private bool IsInputBlocked()
        {
            return _inputBlocked || IsInventoryOpen();
        }

        private void HandleJumpInput()
        {
            if (_stateTransitioner?.CurrentState != null)
            {
                _stateTransitioner.CurrentState.OnJump(_model, _moveInput);
            }
        }

        private void HandleClimbInput()
        {
            if (_stateTransitioner?.CurrentState != null)
            {
                _stateTransitioner.CurrentState.OnClimb(_model);
            }
        }

        public void Enable()
        {
            _inputActions?.Enable();
        }

        public void Disable()
        {
            _inputActions?.Disable();
        }

        public void Dispose()
        {
            Disable();
            
            // Unbind all actions
            _inputActions.Player.Move.performed -= ctx => _moveInput = ctx.ReadValue<Vector2>();
            _inputActions.Player.Move.canceled -= _ => _moveInput = Vector2.zero;
            _inputActions.Player.Jump.performed -= _ => HandleJumpInput();
            _inputActions.Player.Climb.performed -= _ => HandleClimbInput();
            _inputActions.Player.Pickup.performed -= _ => OnPickupRequested?.Invoke();
            _inputActions.Player.OpenInventory.performed -= _ => OnInventoryToggleRequested?.Invoke();
        }
    }
}
