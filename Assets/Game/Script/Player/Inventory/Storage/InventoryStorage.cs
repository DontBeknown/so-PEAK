using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Player.Inventory.Storage
{
    /// <summary>
    /// Pure data storage for inventory - no business logic
    /// </summary>
    public class InventoryStorage : IInventoryStorage
    {
        private readonly List<InventorySlot> _slots;
        private readonly int _maxSlots;

        public InventoryStorage(int initialSlots, int maxSlots)
        {
            _maxSlots = maxSlots;
            _slots = new List<InventorySlot>(initialSlots);
            
            // Initialize slots
            for (int i = 0; i < initialSlots; i++)
            {
                _slots.Add(new InventorySlot());
            }
        }

        #region Query Operations

        public IReadOnlyList<InventorySlot> GetAllSlots()
        {
            return _slots.AsReadOnly();
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                Debug.LogWarning($"[InventoryStorage] Invalid slot index: {index}");
                return null;
            }
            return _slots[index];
        }

        public int GetSlotCount()
        {
            return _slots.Count;
        }

        public int GetItemQuantity(InventoryItem item)
        {
            if (item == null) return 0;
            
            int total = 0;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && slot.item == item)
                {
                    total += slot.quantity;
                }
            }
            return total;
        }

        #endregion

        #region Item Operations

        public bool AddItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("[InventoryStorage] Invalid item or quantity");
                return false;
            }

            int remainingQuantity = quantity;

            // First, try to stack with existing items
            if (item.maxStackSize > 1)
            {
                foreach (var slot in _slots)
                {
                    if (!slot.IsEmpty && slot.item == item && slot.quantity < item.maxStackSize)
                    {
                        int spaceInSlot = item.maxStackSize - slot.quantity;
                        int amountToAdd = Mathf.Min(spaceInSlot, remainingQuantity);
                        
                        slot.quantity += amountToAdd;
                        remainingQuantity -= amountToAdd;

                        if (remainingQuantity <= 0)
                            return true;
                    }
                }
            }

            // Then, use empty slots
            while (remainingQuantity > 0)
            {
                int emptySlotIndex = FindEmptySlotIndex();
                if (emptySlotIndex == -1)
                {
                    Debug.LogWarning("[InventoryStorage] No empty slots available");
                    return false; // Inventory full
                }

                var slot = _slots[emptySlotIndex];
                int amountToAdd = item.maxStackSize > 1
                    ? Mathf.Min(item.maxStackSize, remainingQuantity)
                    : 1;

                slot.item = item;
                slot.quantity = amountToAdd;
                remainingQuantity -= amountToAdd;

                if (item.maxStackSize <= 1 && remainingQuantity > 0)
                {
                    Debug.LogWarning("[InventoryStorage] Cannot add more non-stackable items");
                    return false;
                }
            }

            return true;
        }

        public bool RemoveItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                Debug.LogWarning("[InventoryStorage] Invalid item or quantity");
                return false;
            }

            // Check if we have enough
            if (GetItemQuantity(item) < quantity)
            {
                Debug.LogWarning($"[InventoryStorage] Not enough {item.itemName}. Need {quantity}, have {GetItemQuantity(item)}");
                return false;
            }

            int remainingToRemove = quantity;

            // Remove from slots (LIFO - last in, first out)
            for (int i = _slots.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty && slot.item == item)
                {
                    int amountToRemove = Mathf.Min(slot.quantity, remainingToRemove);
                    slot.quantity -= amountToRemove;
                    remainingToRemove -= amountToRemove;

                    if (slot.quantity <= 0)
                    {
                        slot.Clear();
                    }
                }
            }

            return true;
        }

        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            return GetItemQuantity(item) >= quantity;
        }

        #endregion

        #region Capacity Management

        public bool CanAddItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            int remainingQuantity = quantity;

            // Check existing stacks
            if (item.maxStackSize > 1)
            {
                foreach (var slot in _slots)
                {
                    if (!slot.IsEmpty && slot.item == item && slot.quantity < item.maxStackSize)
                    {
                        int spaceInSlot = item.maxStackSize - slot.quantity;
                        remainingQuantity -= spaceInSlot;

                        if (remainingQuantity <= 0)
                            return true;
                    }
                }
            }

            // Check empty slots
            int emptySlots = GetEmptySlotCount();
            if (item.maxStackSize <= 1)
            {
                return emptySlots >= remainingQuantity;
            }
            else
            {
                int slotsNeeded = Mathf.CeilToInt((float)remainingQuantity / item.maxStackSize);
                return emptySlots >= slotsNeeded;
            }
        }

        public bool ExpandInventory(int additionalSlots)
        {
            if (additionalSlots <= 0)
            {
                Debug.LogWarning("[InventoryStorage] Invalid expansion amount");
                return false;
            }

            int newTotalSlots = _slots.Count + additionalSlots;
            if (newTotalSlots > _maxSlots)
            {
                Debug.LogWarning($"[InventoryStorage] Cannot expand beyond max slots ({_maxSlots})");
                return false;
            }

            for (int i = 0; i < additionalSlots; i++)
            {
                _slots.Add(new InventorySlot());
            }

            //Debug.Log($"[InventoryStorage] Expanded inventory by {additionalSlots} slots. Total: {_slots.Count}");
            return true;
        }

        public int GetEmptySlotCount()
        {
            return _slots.Count(slot => slot.IsEmpty);
        }

        #endregion

        #region Utility

        public int FindStackableSlot(InventoryItem item, int quantity)
        {
            if (item == null || item.maxStackSize <= 1) return -1;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty && slot.item == item)
                {
                    int availableSpace = item.maxStackSize - slot.quantity;
                    if (availableSpace >= quantity)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private int FindEmptySlotIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        #endregion
    }
}
