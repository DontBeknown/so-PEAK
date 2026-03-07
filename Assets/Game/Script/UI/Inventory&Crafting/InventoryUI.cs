using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player.Inventory;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Button closeButton;
    
    [Header("Equipment Panel")]
    [SerializeField] private EquipmentUI equipmentUI; // Equipment displayed alongside inventory
    
    [Header("Tooltip")]
    [SerializeField] private TooltipUI tooltipUI;
    
    [Header("Context Menu")]
    [SerializeField] private ContextMenuUI contextMenuUI;

    [Header("Stats Display")]
    // Sliders have been moved to `SimpleStatsHUD`; keep text elements here only.
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI thirstText;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    // REFACTORED: Removed InventoryManager field, now uses IInventoryService
    private IInventoryService inventoryService;
    private IInventoryStorage inventoryStorage;
    private EquipmentManager equipmentManager;
    private PlayerStats playerStats;
    private IEventBus eventBus;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private bool isOpen = false;
    private PlayerInput playerInput;
    private InputAction openInventoryAction;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        // Start closed
        CloseInventory();
    }

    private void Start()
    {
        // Get required components from ServiceContainer (DI)
        // Done in Start() to ensure InventoryManagerRefactored has registered services in its Awake()
        inventoryService = ServiceContainer.Instance.Get<IInventoryService>();
        inventoryStorage = ServiceContainer.Instance.Get<IInventoryStorage>();
        equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
        playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
        if (tooltipUI == null)
            tooltipUI = ServiceContainer.Instance.TryGet<TooltipUI>();
        
        if (contextMenuUI == null)
            contextMenuUI = ServiceContainer.Instance.TryGet<ContextMenuUI>();
        
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
        if (eventBus != null)
        {
            eventBus.Subscribe<Game.Player.Inventory.Events.ItemAddedEvent>(OnItemAdded);
            eventBus.Subscribe<Game.Player.Inventory.Events.ItemRemovedEvent>(OnItemRemoved);
            eventBus.Subscribe<Game.Player.Inventory.Events.InventoryChangedEvent>(OnInventoryChanged);
        }
        else
        {
            Debug.LogWarning("[InventoryUI] EventBus not available, inventory updates will not work!");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (eventBus != null)
        {
            eventBus.Unsubscribe<Game.Player.Inventory.Events.ItemAddedEvent>(OnItemAdded);
            eventBus.Unsubscribe<Game.Player.Inventory.Events.ItemRemovedEvent>(OnItemRemoved);
            eventBus.Unsubscribe<Game.Player.Inventory.Events.InventoryChangedEvent>(OnInventoryChanged);
        }
    }

    private void CreateSlotUIs()
    {
        if (slotsContainer == null || slotPrefab == null || inventoryStorage == null) return;

        // Clear existing slots
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        // Create slot UIs
        var slots = inventoryStorage.GetAllSlots();
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

        if (tooltipUI != null)
        {
            tooltipUI.HideTooltip();
        }

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

        if (tooltipUI != null)
        {
            tooltipUI.HideTooltip();
        }
        
        if (contextMenuUI != null)
        {
            contextMenuUI.HideMenu();
        }
    }

    public void UpdateAllSlots()
    {
        if (inventoryStorage == null) return;

        var slots = inventoryStorage.GetAllSlots();

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
        if (slotIndex < 0 || slotIndex >= slotUIs.Count || inventoryService == null) return;

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
            // ConsumeItem is a facade method on InventoryManagerRefactored
            var inventoryManager = ServiceContainer.Instance.Get<InventoryManagerRefactored>();
            if (inventoryManager != null)
            {
                inventoryManager.ConsumeItem(item);
                UpdateStatsDisplay(); // Refresh stats after consumption
            }
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
        
        //Debug.Log($"Equipped {equipItem.itemName} to {equipItem.EquipmentSlot} slot");
        
        // Update UI to show equipped status
        UpdateAllSlots();
    }

    public void DropItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Count && inventoryService != null)
        {
            var slotUI = slotUIs[slotIndex];
            if (!slotUI.IsEmpty)
            {
                var item = slotUI.InventorySlot.item;
                bool removed = inventoryService.RemoveItem(item, 1);
                if (removed)
                    WorldItemSpawner.SpawnDroppedItem(item, 1);
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

    // EventBus event handlers
    private void OnItemAdded(Game.Player.Inventory.Events.ItemAddedEvent evt)
    {
        UpdateStatsDisplay();
        UpdateAllSlots();
    }

    private void OnItemRemoved(Game.Player.Inventory.Events.ItemRemovedEvent evt)
    {
        UpdateStatsDisplay();
        UpdateAllSlots();
    }

    private void OnInventoryChanged(Game.Player.Inventory.Events.InventoryChangedEvent evt)
    {
        UpdateAllSlots();
    }

    private void Update()
    {
      // Update stats display in real-time when inventory is open
        if (isOpen)
        {
            UpdateStatsDisplay();
        }
    }

    // Methods for tabbed UI support - show/hide panel without managing pause/cursor state
    public void ShowInventoryPanel()
    {
        isOpen = true;  
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
        isOpen = false;  
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
        
        // Hide tooltip when hiding inventory panel
        if (tooltipUI != null)
        {
            tooltipUI.HideTooltip();
        }
        
        // Also hide equipment panel
        if (equipmentUI != null)
        {
            equipmentUI.HideEquipmentPanel();
        }

        if (contextMenuUI != null)
        {
            contextMenuUI.HideMenu();
        }
    }
}