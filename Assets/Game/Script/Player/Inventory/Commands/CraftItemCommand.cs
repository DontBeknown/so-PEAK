using UnityEngine;
using System.Collections.Generic;

namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command for crafting an item using a recipe.
    /// Supports undo by returning materials and removing crafted item.
    /// </summary>
    public class CraftItemCommand : IInventoryCommand
    {
        private readonly CraftingManager _craftingManager;
        private readonly InventoryManager _inventoryManager;
        private readonly CraftingRecipe _recipe;
        private bool _craftedSuccessfully;
        private List<(InventoryItem item, int quantity)> _consumedMaterials;

        public bool CanUndo => _craftedSuccessfully && _consumedMaterials != null;
        public string Description => $"Craft {_recipe?.resultItem?.itemName ?? "Unknown Item"}";

        public CraftItemCommand(CraftingManager craftingManager, InventoryManager inventoryManager, 
            CraftingRecipe recipe)
        {
            _craftingManager = craftingManager;
            _inventoryManager = inventoryManager;
            _recipe = recipe;
            _consumedMaterials = new List<(InventoryItem, int)>();
        }

        public bool Execute()
        {
            if (_craftingManager == null || _inventoryManager == null || _recipe == null)
            {
                Debug.LogWarning("CraftItemCommand: Invalid state - cannot execute");
                return false;
            }

            // Check if we have materials
            if (!_craftingManager.CanCraftRecipe(_recipe))
            {
                Debug.LogWarning($"Cannot craft {_recipe.resultItem.itemName} - missing materials");
                return false;
            }

            // Store consumed materials for undo
            foreach (var requirement in _recipe.requirements)
            {
                _consumedMaterials.Add((requirement.item, requirement.quantity));
            }

            // Consume materials using the recipe's built-in method
            bool materialsConsumed = _recipe.ConsumeMaterials(_inventoryManager);
            
            if (!materialsConsumed)
            {
                Debug.LogError("Failed to consume materials");
                _consumedMaterials.Clear();
                return false;
            }

            // Add crafted item
            bool craftedItemAdded = _inventoryManager.AddItem(_recipe.resultItem, _recipe.resultQuantity);
            
            if (craftedItemAdded)
            {
                _craftedSuccessfully = true;
                Debug.Log($"Crafted {_recipe.resultItem.itemName} x{_recipe.resultQuantity}");
                return true;
            }
            else
            {
                // Crafting failed - restore materials
                Debug.LogError($"Failed to add crafted item {_recipe.resultItem.itemName} - restoring materials");
                RestoreMaterials();
                return false;
            }
        }

        public bool Undo()
        {
            if (!CanUndo || _inventoryManager == null)
            {
                Debug.LogWarning("CraftItemCommand: Cannot undo - item not crafted");
                return false;
            }

            // Remove crafted item
            bool removed = _inventoryManager.RemoveItem(_recipe.resultItem, _recipe.resultQuantity);
            
            if (removed)
            {
                // Restore materials
                bool restored = RestoreMaterials();
                
                if (restored)
                {
                    Debug.Log($"Uncrafted {_recipe.resultItem.itemName} - materials restored");
                    _craftedSuccessfully = false;
                    return true;
                }
            }

            return false;
        }

        private bool RestoreMaterials()
        {
            if (_consumedMaterials == null || _consumedMaterials.Count == 0)
                return false;

            foreach (var (item, quantity) in _consumedMaterials)
            {
                bool restored = _inventoryManager.AddItem(item, quantity);
                if (!restored)
                {
                    Debug.LogWarning($"Failed to restore material {item.itemName}");
                    return false;
                }
            }

            return true;
        }
    }
}
