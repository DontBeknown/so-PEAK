using System.Collections.Generic;
using UnityEngine;

namespace Game.Player.Inventory.Storage
{
    /// <summary>
    /// Wraps GridInventoryStorage to implement IInventoryStorage,
    /// keeping all existing systems (crafting, commands, equipment) working unchanged.
    /// </summary>
    public class GridStorageAdapter : IInventoryStorage
    {
        private readonly GridInventoryStorage _grid;

        public GridStorageAdapter(GridInventoryStorage grid)
        {
            _grid = grid;
        }

        #region IInventoryStorage — Item Operations

        public bool AddItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            for (int i = 0; i < quantity; i++)
            {
                var placement = _grid.AutoPlace(item);
                if (placement == null)
                {
                    Debug.LogWarning($"[GridStorageAdapter] Grid full — could not place {item.itemName} (placed {i}/{quantity})");
                    return i > 0; // partial success if at least one was placed
                }
            }
            return true;
        }

        public bool RemoveItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            int count = _grid.GetItemCount(item);
            if (count < quantity) return false;

            // LIFO — remove from the end of the placements list
            var placements = _grid.GetAllPlacements();
            int removed = 0;
            for (int i = placements.Count - 1; i >= 0 && removed < quantity; i--)
            {
                if (placements[i].Item == item)
                {
                    _grid.RemoveItem(placements[i]);
                    removed++;
                }
            }
            return removed >= quantity;
        }

        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            return _grid.GetItemCount(item) >= quantity;
        }

        public int GetItemQuantity(InventoryItem item)
        {
            return _grid.GetItemCount(item);
        }

        #endregion

        #region IInventoryStorage — Query Operations

        /// <summary>
        /// Synthesises a list of InventorySlot from grid placements.
        /// Each placement becomes one slot with quantity 1 (no stacking in grid mode).
        /// </summary>
        public IReadOnlyList<InventorySlot> GetAllSlots()
        {
            var placements = _grid.GetAllPlacements();
            var slots = new List<InventorySlot>(placements.Count);
            foreach (var p in placements)
            {
                slots.Add(new InventorySlot(p.Item, 1));
            }
            return slots.AsReadOnly();
        }

        public InventorySlot GetSlot(int index)
        {
            var placements = _grid.GetAllPlacements();
            if (index < 0 || index >= placements.Count) return null;
            var p = placements[index];
            return new InventorySlot(p.Item, 1);
        }

        public int GetSlotCount()
        {
            return _grid.GetAllPlacements().Count;
        }

        #endregion

        #region IInventoryStorage — Capacity Management

        public bool CanAddItem(InventoryItem item, int quantity)
        {
            // Conservative check: see if there is raw cell space
            if (item == null || quantity <= 0) return false;

            int cellsNeeded = item.gridSize.x * item.gridSize.y * quantity;
            int totalCells = _grid.Width * _grid.Height;

            // Count occupied cells
            int occupiedCells = 0;
            var placements = _grid.GetAllPlacements();
            foreach (var p in placements)
                occupiedCells += p.Size.x * p.Size.y;

            return (totalCells - occupiedCells) >= cellsNeeded;
        }

        public bool ExpandInventory(int additionalSlots)
        {
            // Grid size is fixed at construction; expansion not supported
            Debug.LogWarning("[GridStorageAdapter] Grid inventory does not support expansion.");
            return false;
        }

        public int GetEmptySlotCount()
        {
            // Return number of empty 1×1 cells
            int total = _grid.Width * _grid.Height;
            var placements = _grid.GetAllPlacements();
            int occupied = 0;
            foreach (var p in placements)
                occupied += p.Size.x * p.Size.y;
            return total - occupied;
        }

        #endregion

        #region IInventoryStorage — Utility

        public int FindStackableSlot(InventoryItem item, int quantity)
        {
            // Grid inventory has no stacking
            return -1;
        }

        #endregion
    }
}
