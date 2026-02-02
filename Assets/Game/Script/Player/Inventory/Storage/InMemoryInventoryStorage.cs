using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// In-memory implementation of inventory storage
    /// Single Responsibility: Data storage only
    /// </summary>
    public class InMemoryInventoryStorage : IInventoryStorage
    {
        private readonly List<InventorySlot> _slots;
        private readonly int _maxSlots;
        
        public int SlotCount => _slots.Count;
        
        public InMemoryInventoryStorage(int initialSlots, int maxSlots)
        {
            _maxSlots = maxSlots;
            _slots = new List<InventorySlot>(initialSlots);
            
            for (int i = 0; i < initialSlots; i++)
            {
                _slots.Add(new InventorySlot());
            }
        }
        
        public IReadOnlyList<InventorySlot> GetSlots()
        {
            return _slots.AsReadOnly();
        }
        
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return null;
            
            return _slots[index];
        }
        
        public int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }
        
        public List<int> FindSlotsWithItem(InventoryItem item)
        {
            var result = new List<int>();
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].item == item)
                {
                    result.Add(i);
                }
            }
            return result;
        }
        
        public bool AddToSlot(int slotIndex, InventoryItem item, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;
            
            _slots[slotIndex].AddItem(item, quantity);
            return true;
        }
        
        public bool RemoveFromSlot(int slotIndex, int quantity)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return false;
            
            _slots[slotIndex].RemoveQuantity(quantity);
            return true;
        }
        
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < _slots.Count)
            {
                _slots[slotIndex].Clear();
            }
        }
        
        public bool IsFull()
        {
            return _slots.All(slot => !slot.IsEmpty);
        }
        
        /// <summary>
        /// Expands inventory capacity
        /// </summary>
        public bool TryAddSlots(int count)
        {
            if (_slots.Count + count > _maxSlots)
                return false;
            
            for (int i = 0; i < count; i++)
            {
                _slots.Add(new InventorySlot());
            }
            
            return true;
        }
    }
}
