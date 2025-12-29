using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int initialSlots = 10;
    [SerializeField] private int maxSlots = 30;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats; // Use your existing PlayerStats

    private List<InventorySlot> inventorySlots;

    // Events
    public static event Action<InventoryItem, int> OnItemAdded;
    public static event Action<InventoryItem, int> OnItemRemoved;
    public static event Action<InventoryItem, int> OnItemConsumed;
    public static event Action OnInventoryChanged;

    private void Awake()
    {
        InitializeInventory();

        // Get PlayerStats if not assigned
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void InitializeInventory()
    {
        inventorySlots = new List<InventorySlot>();
        for (int i = 0; i < initialSlots; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }
    }

    public bool AddItem(InventoryItem item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        // Try to stack with existing items first
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].CanAddItem(item, quantity))
            {
                inventorySlots[i].AddItem(item, quantity);
                OnItemAdded?.Invoke(item, quantity);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        // Find empty slot
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].IsEmpty)
            {
                inventorySlots[i].AddItem(item, quantity);
                OnItemAdded?.Invoke(item, quantity);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false; // Inventory full
    }

    public bool RemoveItem(InventoryItem item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int remainingToRemove = quantity;

        for (int i = 0; i < inventorySlots.Count && remainingToRemove > 0; i++)
        {
            if (inventorySlots[i].item == item)
            {
                int canRemove = Mathf.Min(inventorySlots[i].quantity, remainingToRemove);
                inventorySlots[i].RemoveQuantity(canRemove);
                remainingToRemove -= canRemove;
                OnItemRemoved?.Invoke(item, canRemove);
            }
        }

        if (remainingToRemove == 0)
        {
            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool ConsumeItem(InventoryItem item)
    {
        if (item == null || !item.isConsumable) return false;

        if (HasItem(item, 1))
        {
            // Apply consumable effects using your existing PlayerStats
            foreach (var effect in item.consumableEffects)
            {
                ApplyConsumableEffect(effect);
            }

            RemoveItem(item, 1);
            OnItemConsumed?.Invoke(item, 1);
            return true;
        }

        return false;
    }

    private void ApplyConsumableEffect(ConsumableEffect effect)
    {
        if (playerStats == null) return;

        float effectValue = effect.value;

        // Apply the effect based on stat type using your existing PlayerStats methods
        switch (effect.statType)
        {
            case StatType.Health:
                // Assuming you'll add a Heal method to PlayerStats or access health directly
                if (effectValue > 0)
                {
                    // For healing - you may need to add a Heal method to PlayerStats
                    // playerStats.Heal(effectValue);
                    Debug.Log($"Healed for {effectValue} health");
                }
                break;

            case StatType.Hunger:
                if (effectValue > 0)
                {
                    playerStats.Eat(effectValue); // Use your existing Eat method
                }
                break;

            case StatType.Thirst:
                if (effectValue > 0)
                {
                    playerStats.Drink(effectValue); // Use your existing Drink method
                }
                break;

            case StatType.Temperature:
                // You may need to add temperature modification methods to PlayerStats
                Debug.Log($"Temperature modified by {effectValue}");
                break;

            case StatType.Stamina:
                // Access stamina directly or add methods to PlayerStats
                Debug.Log($"Stamina modified by {effectValue}");
                break;
        }
    }

    public bool HasItem(InventoryItem item, int quantity = 1)
    {
        if (item == null) return false;

        int totalQuantity = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot.item == item)
            {
                totalQuantity += slot.quantity;
                if (totalQuantity >= quantity) return true;
            }
        }

        return false;
    }

    public int GetItemCount(InventoryItem item)
    {
        if (item == null) return 0;

        int totalQuantity = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot.item == item)
            {
                totalQuantity += slot.quantity;
            }
        }

        return totalQuantity;
    }

    public List<InventorySlot> GetInventorySlots()
    {
        return new List<InventorySlot>(inventorySlots);
    }

    public bool ExpandInventory(int additionalSlots)
    {
        if (inventorySlots.Count + additionalSlots > maxSlots) return false;

        for (int i = 0; i < additionalSlots; i++)
        {
            inventorySlots.Add(new InventorySlot());
        }

        OnInventoryChanged?.Invoke();
        return true;
    }
}