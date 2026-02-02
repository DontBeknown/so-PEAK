using UnityEngine;
using Game.Player.Inventory.Commands;
using Game.Core.DI;
using Game.UI;

namespace Game.Player.Services
{
    /// <summary>
    /// Facade pattern for player inventory interactions.
    /// Provides a simplified interface for inventory, crafting, and item detection.
    /// Now uses Command Pattern for undo/redo support.
    /// Follows Facade Pattern to hide complexity of inventory subsystem.
    /// </summary>
    public class PlayerInventoryFacade
    {
        private readonly InventoryManager _inventoryManager;
        private readonly CraftingManager _craftingManager;
        private readonly UIServiceProvider _uiServiceProvider;
        private readonly PlayerStats _playerStats;
        private readonly Transform _playerTransform;
        private readonly CinemachinePlayerCamera _playerCamera;
        
        // Command Pattern
        private readonly InventoryCommandInvoker _commandInvoker;

        public PlayerInventoryFacade(
            InventoryManager inventoryManager,
            CraftingManager craftingManager,
            UIServiceProvider uiServiceProvider,
            PlayerStats playerStats = null,
            Transform playerTransform = null,
            bool enableCommandDebugLogs = false,
            CinemachinePlayerCamera playerCamera = null)
        {
            _inventoryManager = inventoryManager;
            _craftingManager = craftingManager;
            _uiServiceProvider = uiServiceProvider ?? ServiceContainer.Instance.TryGet<UIServiceProvider>();
            _playerStats = playerStats;
            _playerTransform = playerTransform;
            // Use ServiceContainer instead of FindFirstObjectByType
            _playerCamera = playerCamera ?? ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
            
            _commandInvoker = new InventoryCommandInvoker(enableDebugLogs: enableCommandDebugLogs);
        }

        #region Inventory Management

        /// <summary>
        /// Toggles the inventory UI using UIServiceProvider (SOLID: Facade pattern)
        /// </summary>
        public void ToggleInventory()
        {
            _uiServiceProvider?.TogglePanel("Inventory");
        }
        
        /// <summary>
        /// Returns whether the inventory is currently open
        /// </summary>
        public bool IsInventoryOpen => _uiServiceProvider?.GetPanel("Inventory")?.IsActive ?? false;

        /// <summary>
        /// Consumes a specific item from inventory using Command Pattern
        /// </summary>
        public bool ConsumeItem(InventoryItem item)
        {
            if (_inventoryManager == null || item == null)
                return false;

            var command = new UseItemCommand(_inventoryManager, item, _playerStats);
            return _commandInvoker.Execute(command);
        }

        /// <summary>
        /// Quick use the first consumable item in inventory
        /// </summary>
        public bool QuickUseConsumable()
        {
            if (_inventoryManager == null)
                return false;

            var slots = _inventoryManager.GetInventorySlots();
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item.isConsumable)
                {
                    var command = new UseItemCommand(_inventoryManager, slot.item, _playerStats);
                    return _commandInvoker.Execute(command);
                }
            }

            return false;
        }

        /// <summary>
        /// Drops an item from inventory using Command Pattern
        /// </summary>
        public bool DropItem(InventoryItem item)
        {
            if (_inventoryManager == null || item == null || _playerTransform == null)
                return false;

            Vector3 dropPosition = _playerTransform.position + Vector3.up;
            Vector3 dropDirection = _playerTransform.forward;
            
            var command = new DropItemCommand(_inventoryManager, item, dropPosition, dropDirection);
            return _commandInvoker.Execute(command);
        }

        #endregion

        #region Crafting

        /// <summary>
        /// Starts crafting a recipe using Command Pattern
        /// </summary>
        public void StartCrafting(CraftingRecipe recipe)
        {
            if (_craftingManager == null || _inventoryManager == null || recipe == null)
                return;

            var command = new CraftItemCommand(_craftingManager, _inventoryManager, recipe);
            _commandInvoker.Execute(command);
        }

        /// <summary>
        /// Checks if a recipe can be crafted
        /// </summary>
        public bool CanCraft(CraftingRecipe recipe)
        {
            return _craftingManager != null && _craftingManager.CanCraftRecipe(recipe);
        }

        #endregion

        #region Command Pattern - Undo/Redo

        /// <summary>
        /// Undo the last inventory action
        /// </summary>
        public bool UndoLastAction()
        {
            return _commandInvoker.Undo();
        }

        /// <summary>
        /// Redo the last undone action
        /// </summary>
        public bool RedoLastAction()
        {
            return _commandInvoker.Redo();
        }

        /// <summary>
        /// Clear all command history
        /// </summary>
        public void ClearCommandHistory()
        {
            _commandInvoker.ClearHistory();
        }

        /// <summary>
        /// Get description of what can be undone
        /// </summary>
        public string GetUndoDescription() => _commandInvoker.GetUndoDescription();

        /// <summary>
        /// Get description of what can be redone
        /// </summary>
        public string GetRedoDescription() => _commandInvoker.GetRedoDescription();

        #endregion

        #region Component Access (for backward compatibility)

        public InventoryManager InventoryManager => _inventoryManager;
        public CraftingManager CraftingManager => _craftingManager;

        #endregion
    }
}
