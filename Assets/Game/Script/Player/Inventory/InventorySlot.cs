using UnityEngine;
using System;

[System.Serializable]
public class InventorySlot
{
    public InventoryItem item;
    public int quantity;

    public InventorySlot()
    {
        item = null;
        quantity = 0;
    }

    public InventorySlot(InventoryItem newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
    }

    public bool IsEmpty => item == null || quantity <= 0;

    public bool CanAddItem(InventoryItem itemToAdd, int quantityToAdd)
    {
        if (IsEmpty) return true;
        if (item != itemToAdd) return false;
        return quantity + quantityToAdd <= item.maxStackSize;
    }

    public bool AddItem(InventoryItem itemToAdd, int quantityToAdd)
    {
        if (IsEmpty)
        {
            item = itemToAdd;
            quantity = quantityToAdd;
            return true;
        }

        if (item == itemToAdd && quantity + quantityToAdd <= item.maxStackSize)
        {
            quantity += quantityToAdd;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    public bool RemoveQuantity(int quantityToRemove)
    {
        if (quantity >= quantityToRemove)
        {
            quantity -= quantityToRemove;
            if (quantity <= 0)
            {
                Clear();
            }
            return true;
        }
        return false;
    }
}