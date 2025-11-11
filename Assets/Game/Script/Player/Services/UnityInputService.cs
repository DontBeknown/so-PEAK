using UnityEngine;
using UnityEngine.InputSystem;
using Game.Player.Interfaces;

namespace Game.Player.Services
{
    /// <summary>
    /// Unity Input System implementation of IInputService.
    /// Handles all player input using the new Input System.
    /// </summary>
    public class UnityInputService : IInputService
    {
        private readonly IA_PlayerController _inputActions;
        private Vector2 _movementInput;
        private bool _jumpPressed;
        private bool _climbPressed;
        private bool _sprintHeld;
        private bool _pickupPressed;
        private bool _inventoryPressed;

        public Vector2 MovementInput => _movementInput;
        public bool JumpPressed => ConsumeInput(ref _jumpPressed);
        public bool ClimbPressed => ConsumeInput(ref _climbPressed);
        public bool SprintHeld => _sprintHeld;
        public bool PickupPressed => ConsumeInput(ref _pickupPressed);
        public bool InventoryPressed => ConsumeInput(ref _inventoryPressed);

        public UnityInputService()
        {
            _inputActions = new IA_PlayerController();
            BindInputs();
        }

        private void BindInputs()
        {
            // Movement (continuous)
            _inputActions.Player.Move.performed += ctx => _movementInput = ctx.ReadValue<Vector2>();
            _inputActions.Player.Move.canceled += _ => _movementInput = Vector2.zero;

            // Jump (button press)
            _inputActions.Player.Jump.performed += _ => _jumpPressed = true;

            // Climb (button press)
            _inputActions.Player.Climb.performed += _ => _climbPressed = true;

            // Sprint (hold)
            // Note: Add Sprint action to IA_PlayerController if not present
            // _inputActions.Player.Sprint.performed += _ => _sprintHeld = true;
            // _inputActions.Player.Sprint.canceled += _ => _sprintHeld = false;

            // Pickup (button press)
            _inputActions.Player.Pickup.performed += _ => _pickupPressed = true;

            // Inventory (button press)
            _inputActions.Player.OpenInventory.performed += _ => _inventoryPressed = true;
        }

        public void Enable()
        {
            _inputActions.Enable();
        }

        public void Disable()
        {
            _inputActions.Disable();
        }

        /// <summary>
        /// Consumes a one-time input (returns true once, then resets)
        /// </summary>
        private bool ConsumeInput(ref bool input)
        {
            bool value = input;
            input = false;
            return value;
        }
    }
}
