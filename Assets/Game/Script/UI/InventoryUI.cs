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

    [Header("Selected Item Panel")]
    [SerializeField] private GameObject selectedItemPanel;
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemName;
    [SerializeField] private TextMeshProUGUI selectedItemDescription;
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private TextMeshProUGUI itemQuantityText; // Show "x5" etc.

    [Header("Stats Display")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider thirstSlider;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private TextMeshProUGUI thirstText;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;

    private InventoryManager inventoryManager;
    private PlayerStats playerStats;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private int selectedSlotIndex = -1;
    private bool isOpen = false;
    private PlayerInput playerInput;
    private InputAction openInventoryAction;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        // Get required components
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        playerStats = FindFirstObjectByType<PlayerStats>();

        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);

        if (dropButton != null)
            dropButton.onClick.AddListener(DropSelectedItem);

        // Start closed
        CloseInventory();
    }

    private void Start()
    {
        CreateSlotUIs();
        SubscribeToEvents();
        UpdateAllSlots();
        UpdateStatsDisplay();

        // Show "Select an item" message initially
        ShowEmptySelection();
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

        // Show empty selection or maintain current selection
        if (selectedSlotIndex < 0)
        {
            ShowEmptySelection();
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

        ClearSelection();
    }

    private void UpdateAllSlots()
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

        // Update health
        if (healthSlider != null)
        {
            healthSlider.value = playerStats.HealthPercent;
        }
        if (healthText != null)
        {
            healthText.text = $"{playerStats.Health:F0}/{playerStats.MaxHealth:F0}";
        }

        // Update hunger
        if (hungerSlider != null)
        {
            hungerSlider.value = 1f - playerStats.HungerPercent; // Inverted for hunger bar
        }
        if (hungerText != null)
        {
            hungerText.text = $"{playerStats.Hunger:F0}/{playerStats.MaxHunger:F0}";
        }

        // Update thirst
        if (thirstSlider != null)
        {
            thirstSlider.value = 1f - playerStats.ThirstPercent; // Inverted for thirst bar
        }
        if (thirstText != null)
        {
            thirstText.text = $"{playerStats.Thirst:F0}/{playerStats.MaxThirst:F0}";
        }

        // Update stamina
        if (staminaSlider != null)
        {
            staminaSlider.value = playerStats.StaminaPercent;
        }
        if (staminaText != null)
        {
            staminaText.text = $"{playerStats.Stamina:F0}/{playerStats.MaxStamina:F0}";
        }
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotUIs.Count) return;

        // Clear previous selection
        ClearSelection();

        // Select new slot
        selectedSlotIndex = slotIndex;
        slotUIs[selectedSlotIndex].SetSelected(true);

        // Update selected item panel
        UpdateSelectedItemPanel();
    }

    private void ClearSelection()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIs.Count)
        {
            slotUIs[selectedSlotIndex].SetSelected(false);
        }

        selectedSlotIndex = -1;
        ShowEmptySelection();
    }

    private void ShowEmptySelection()
    {
        if (selectedItemPanel == null) return;

        selectedItemPanel.SetActive(false);

    }

    private void UpdateSelectedItemPanel()
    {
        if (selectedItemPanel == null) return;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIs.Count)
        {
            var slotUI = slotUIs[selectedSlotIndex];
            if (!slotUI.IsEmpty)
            {
                selectedItemPanel.SetActive(true);
                var item = slotUI.InventorySlot.item;
                var quantity = slotUI.InventorySlot.quantity;

                // Update icon
                if (selectedItemIcon != null && item.icon != null)
                {
                    selectedItemIcon.sprite = item.icon;
                    selectedItemIcon.gameObject.SetActive(true);
                }

                // Update name
                if (selectedItemName != null)
                {
                    selectedItemName.text = item.itemName;
                }

                // Update description
                if (selectedItemDescription != null)
                {
                    selectedItemDescription.text = item.description;
                }

                // Update quantity
                if (itemQuantityText != null)
                {
                    itemQuantityText.text = quantity > 1 ? $"x{quantity}" : "";
                }

                // Update use button
                if (useButton != null)
                {
                    useButton.interactable = item.isConsumable;
                    var buttonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = item.isConsumable ? "Consume" : "Use";
                    }
                }

                // Enable drop button
                if (dropButton != null)
                {
                    dropButton.interactable = true;
                }

                return;
            }
        }

        ShowEmptySelection();
    }

    public void UseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotUIs.Count || inventoryManager == null) return;

        var slotUI = slotUIs[slotIndex];
        if (slotUI.IsEmpty) return;

        var item = slotUI.InventorySlot.item;
        if (item.isConsumable)
        {
            inventoryManager.ConsumeItem(item);
            UpdateStatsDisplay(); // Refresh stats after consumption

            // Update selected panel if this item is currently selected
            if (selectedSlotIndex == slotIndex)
            {
                UpdateSelectedItemPanel();
            }
        }
    }

    public void UseSelectedItem()
    {
        if (selectedSlotIndex >= 0)
        {
            UseItem(selectedSlotIndex);
        }
    }

    public void DropSelectedItem()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIs.Count && inventoryManager != null)
        {
            var slotUI = slotUIs[selectedSlotIndex];
            if (!slotUI.IsEmpty)
            {
                // Remove one item from inventory
                var item = slotUI.InventorySlot.item;
                inventoryManager.RemoveItem(item, 1);
                Debug.Log($"Dropped {item.itemName}");

                // Update selected panel after dropping
                UpdateSelectedItemPanel();
            }
        }
    }

    private void OnItemChanged(InventoryItem item, int quantity)
    {
        // Update stats display when items are added/removed
        UpdateStatsDisplay();
    }

    private void Update()
    {
        // Update stats display periodically
        if (isOpen)
        {
            UpdateStatsDisplay();
        }
    }
}