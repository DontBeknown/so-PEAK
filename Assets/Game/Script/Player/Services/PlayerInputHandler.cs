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

        private void BindInputActions()
        {
            // Movement input (continuous) - block when inventory is open
            _inputActions.Player.Move.performed += ctx => 
            {
                if (!IsInventoryOpen())
                    _moveInput = ctx.ReadValue<Vector2>();
            };
            _inputActions.Player.Move.canceled += _ => _moveInput = Vector2.zero;

            // Action inputs - delegate to current state (block when inventory is open)
            _inputActions.Player.Jump.performed += _ => 
            {
                if (!IsInventoryOpen())
                    HandleJumpInput();
            };
            _inputActions.Player.Climb.performed += _ => 
            {
                if (!IsInventoryOpen())
                    HandleClimbInput();
            };

            // Item/UI inputs - trigger events (inventory toggle always works)
            _inputActions.Player.Pickup.performed += _ => 
            {
                if (!IsInventoryOpen())
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
