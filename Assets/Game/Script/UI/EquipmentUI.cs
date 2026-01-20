using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Main UI controller for the equipment panel.
/// Displays all equipment slots and selected equipment details.
/// Integrates with InventoryUI for seamless item management.
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private Transform equipmentSlotsContainer;
    [SerializeField] private GameObject equipmentSlotPrefab;

    [Header("Character Preview (Optional)")]
    [SerializeField] private Image characterPreview;

    private EquipmentManager equipmentManager;
    private InventoryUI inventoryUI; // Reference to refresh inventory when unequipping
    private Dictionary<EquipmentSlotType, EquipmentSlotUI> slotUIs = new Dictionary<EquipmentSlotType, EquipmentSlotUI>();
    private bool isInitialized = false; // Track if slots have been created

    private void Awake()
    {
        // Find equipment manager
        equipmentManager = FindFirstObjectByType<EquipmentManager>();
        
        // Find inventory UI to refresh it when unequipping
        inventoryUI = FindFirstObjectByType<InventoryUI>();

        // Start hidden
        if (equipmentPanel != null)
            equipmentPanel.SetActive(false);
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged += OnEquipmentChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (equipmentManager != null)
        {
            equipmentManager.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }

    private void CreateEquipmentSlots()
    {
        if (isInitialized) return; // Already created
        
        if (equipmentSlotsContainer == null || equipmentSlotPrefab == null)
        {
            Debug.LogWarning("EquipmentUI: Missing required references for creating slots");
            return;
        }
        
        if (equipmentManager == null)
        {
            equipmentManager = FindFirstObjectByType<EquipmentManager>();
            if (equipmentManager == null)
            {
                Debug.LogWarning("EquipmentUI: EquipmentManager not found");
                return;
            }
        }

        // Clear existing slots (if any)
        foreach (Transform child in equipmentSlotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        // Create a slot for each equipment type
        foreach (EquipmentSlotType slotType in Enum.GetValues(typeof(EquipmentSlotType)))
        {
            GameObject slotObj = Instantiate(equipmentSlotPrefab, equipmentSlotsContainer);
            slotObj.SetActive(true);
            
            EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();

            if (slotUI != null)
            {
                slotUI.Initialize(this, slotType, equipmentManager);
                slotUIs[slotType] = slotUI;
            }
            else
            {
                Debug.LogError($"EquipmentUI: Slot prefab missing EquipmentSlotUI component");
            }
        }
        
        isInitialized = true;
        //Debug.Log($"EquipmentUI: Created {slotUIs.Count} equipment slots");
    }

    private void UpdateAllSlots()
    {
        if (equipmentManager == null) return;

        foreach (var kvp in slotUIs)
        {
            EquipmentSlotType slotType = kvp.Key;
            EquipmentSlotUI slotUI = kvp.Value;

            IEquippable equippedItem = equipmentManager.GetEquippedItem(slotType);
            slotUI.UpdateSlot(equippedItem);
        }
    }

    private void OnEquipmentChanged(EquipmentSlotType slotType, IEquippable item)
    {
        // Update the specific slot
        if (slotUIs.TryGetValue(slotType, out EquipmentSlotUI slotUI))
        {
            slotUI.UpdateSlot(item);
        }
    }

    /// <summary>
    /// Attempts to equip an item from the inventory.
    /// Called by inventory UI when double-clicking equipment items.
    /// </summary>
    public bool TryEquipFromInventory(EquipmentItem item)
    {
        if (item == null || equipmentManager == null) return false;

        equipmentManager.Equip(item);
        return true;
    }

    // Methods for tabbed UI support
    public void ShowEquipmentPanel()
    {
        // Create slots on first show (lazy initialization)
        if (!isInitialized)
        {
            CreateEquipmentSlots();
            SubscribeToEvents(); // Subscribe after creation
        }
        
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(true);
        }

        UpdateAllSlots();
    }

    public void HideEquipmentPanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }
}
