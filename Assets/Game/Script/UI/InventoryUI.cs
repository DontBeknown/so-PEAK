using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button closeButton;
    
    [Header("Equipment Panel")]
    [SerializeField] private EquipmentUI equipmentUI; // Equipment displayed alongside inventory

    [Header("Stats Display")]
    // Sliders have been moved to `SimpleStatsHUD`; keep text elements here only.
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI thirstText;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    private InventoryManager inventoryManager;
    private EquipmentManager equipmentManager;
    private PlayerStats playerStats;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private bool isOpen = false;
    private PlayerInput playerInput;
    private InputAction openInventoryAction;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        // Get required components
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        equipmentManager = FindFirstObjectByType<EquipmentManager>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        

        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        // Start closed
        CloseInventory();
    }

    private void Start()
    {
        CreateSlotUIs();
        SubscribeToEvents();
        UpdateAllSlots();
        UpdateStatsDisplay();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (openInventoryAction != null)
            openInventoryAction.performed -= OnToggleInventory;
    }

    private void SubscribeToEvents()
    {
        if (inventoryManager != null)
        {
            InventoryManager.OnItemAdded += OnItemChanged;
            InventoryManager.OnItemRemoved += OnItemChanged;
            InventoryManager.OnInventoryChanged += UpdateAllSlots;
        }
    }

    private void UnsubscribeFromEvents()
    {
        InventoryManager.OnItemAdded -= OnItemChanged;
        InventoryManager.OnItemRemoved -= OnItemChanged;
        InventoryManager.OnInventoryChanged -= UpdateAllSlots;
    }

    private void CreateSlotUIs()
    {
        if (slotsContainer == null || slotPrefab == null || inventoryManager == null) return;

        // Clear existing slots
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        // Create slot UIs
        var slots = inventoryManager.GetInventorySlots();
        for (int i = 0; i < slots.Count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Initialize(this, i);
                slotUIs.Add(slotUI);
            }
        }
    }

    private void OnToggleInventory(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (isOpen) return;

        isOpen = true;
        inventoryPanel.SetActive(true);

        // Pause game if required
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

        }

        UpdateAllSlots();
        UpdateStatsDisplay();
    }

    public void CloseInventory()
    {
        if (!isOpen) return;

        isOpen = false;
        inventoryPanel.SetActive(false);

        // Resume game if it was paused
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void UpdateAllSlots()
    {
        if (inventoryManager == null) return;

        var slots = inventoryManager.GetInventorySlots();

        // Update existing slot UIs
        for (int i = 0; i < slotUIs.Count && i < slots.Count; i++)
        {
            slotUIs[i].UpdateSlot(slots[i]);
        }

        // If inventory expanded, create new slots
        if (slots.Count > slotUIs.Count)
        {
            CreateSlotUIs();
        }
    }

    private void UpdateStatsDisplay()
    {
        if (playerStats == null) return;
        // Update textual stat displays (numbers)
        if (healthText != null)
        {
            healthText.text = $"{playerStats.Health:F0}/{playerStats.MaxHealth:F0}";
        }

        if (hungerText != null)
        {
            hungerText.text = $"{playerStats.Hunger:F0}/{playerStats.MaxHunger:F0}";
        }

        if (thirstText != null)
        {
            thirstText.text = $"{playerStats.Thirst:F0}/{playerStats.MaxThirst:F0}";
        }

        if (staminaText != null)
        {
            staminaText.text = $"{playerStats.Stamina:F0}/{playerStats.MaxStamina:F0}";
        }
    }



    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotUIs.Count || inventoryManager == null) return;

        var slotUI = slotUIs[slotIndex];
        if (slotUI.IsEmpty) return;

        var item = slotUI.InventorySlot.item;
        
        // Check if item is equipment
        if (item.itemType == ItemType.Equipment)
        {
            EquipItem(item);
            return;
        }
        
        // Otherwise, consume if consumable
        if (item.isConsumable)
        {
            inventoryManager.ConsumeItem(item);
            UpdateStatsDisplay(); // Refresh stats after consumption
        }
    }
    
    private void EquipItem(InventoryItem item)
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning("EquipmentManager not found! Cannot equip items.");
            return;
        }
        
        EquipmentItem equipItem = item as EquipmentItem;
        if (equipItem == null)
        {
            Debug.LogWarning($"Item {item.itemName} is marked as Equipment but is not an EquipmentItem!");
            return;
        }
        
        // Equip the item (keep it in inventory)
        IEquippable previousItem = equipmentManager.Equip(equipItem);
        
        Debug.Log($"Equipped {equipItem.itemName} to {equipItem.EquipmentSlot} slot");
        
        // Update UI to show equipped status
        UpdateAllSlots();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Count && inventoryManager != null)
        {
            var slotUI = slotUIs[slotIndex];
            if (!slotUI.IsEmpty)
            {
                // Remove one item from inventory
                var item = slotUI.InventorySlot.item;
                inventoryManager.RemoveItem(item, 1);
                Debug.Log($"Dropped {item.itemName}");
            }
        }
    }
    
    /// <summary>
    /// Get the equipment manager reference (for context menu).
    /// </summary>
    public EquipmentManager GetEquipmentManager()
    {
        return equipmentManager;
    }

    private void OnItemChanged(InventoryItem item, int quantity)
    {
        // Update stats display when items are added/removed
        UpdateStatsDisplay();
    }

    private void Update()
    {

    }

    // Methods for tabbed UI support - show/hide panel without managing pause/cursor state
    public void ShowInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }
        
        // Also show equipment panel alongside inventory
        if (equipmentUI != null)
        {
            equipmentUI.ShowEquipmentPanel();
        }

        UpdateAllSlots();
        UpdateStatsDisplay();
    }

    public void HideInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        
        // Also hide equipment panel
        if (equipmentUI != null)
        {
            equipmentUI.HideEquipmentPanel();
        }
    }
}