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

    [Header("Selected Equipment Panel")]
    [SerializeField] private GameObject selectedEquipmentPanel;
    [SerializeField] private Image selectedEquipmentIcon;
    [SerializeField] private TextMeshProUGUI selectedEquipmentName;
    [SerializeField] private TextMeshProUGUI selectedEquipmentDescription;
    [SerializeField] private TextMeshProUGUI selectedEquipmentStats; // Shows stat modifiers
    [SerializeField] private Button unequipButton;

    [Header("Character Preview (Optional)")]
    [SerializeField] private Image characterPreview;

    private EquipmentManager equipmentManager;
    private InventoryUI inventoryUI; // Reference to refresh inventory when unequipping
    private Dictionary<EquipmentSlotType, EquipmentSlotUI> slotUIs = new Dictionary<EquipmentSlotType, EquipmentSlotUI>();
    private EquipmentSlotType? selectedSlotType = null;
    private bool isInitialized = false; // Track if slots have been created

    private void Awake()
    {
        // Find equipment manager
        equipmentManager = FindFirstObjectByType<EquipmentManager>();
        
        // Find inventory UI to refresh it when unequipping
        inventoryUI = FindFirstObjectByType<InventoryUI>();

        // Setup unequip button
        if (unequipButton != null)
            unequipButton.onClick.AddListener(UnequipSelectedItem);

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
        Debug.Log($"EquipmentUI: Created {slotUIs.Count} equipment slots");
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

        // Update selected panel if this slot is selected
        if (selectedSlotType.HasValue && selectedSlotType.Value == slotType)
        {
            UpdateSelectedEquipmentPanel();
        }
    }

    public void SelectEquipmentSlot(EquipmentSlotType slotType)
    {
        selectedSlotType = slotType;
        UpdateSelectedEquipmentPanel();
    }

    private void UpdateSelectedEquipmentPanel()
    {
        if (selectedEquipmentPanel == null) return;

        if (!selectedSlotType.HasValue)
        {
            ShowEmptySelection();
            return;
        }

        IEquippable equippedItem = equipmentManager?.GetEquippedItem(selectedSlotType.Value);

        if (equippedItem == null)
        {
            // Empty slot - just hide the panel
            ShowEmptySelection();
            return;
        }

        // Show equipped item details
        EquipmentItem equipItem = equippedItem as EquipmentItem;
        if (equipItem != null)
        {
            selectedEquipmentPanel.SetActive(true);

            // Update icon
            if (selectedEquipmentIcon != null && equipItem.icon != null)
            {
                selectedEquipmentIcon.sprite = equipItem.icon;
                selectedEquipmentIcon.gameObject.SetActive(true);
            }

            // Update name
            if (selectedEquipmentName != null)
            {
                selectedEquipmentName.text = equipItem.itemName;
            }

            // Update description
            if (selectedEquipmentDescription != null)
            {
                selectedEquipmentDescription.text = equipItem.description;
            }

            // Update stat modifiers
            if (selectedEquipmentStats != null)
            {
                string statsText = GetStatModifiersText(equippedItem);
                selectedEquipmentStats.text = statsText;
            }

            // Enable unequip button
            if (unequipButton != null)
            {
                unequipButton.interactable = true;
            }
        }
    }

    private string GetStatModifiersText(IEquippable equipment)
    {
        if (equipment == null || equipment.StatModifiers == null || equipment.StatModifiers.Count == 0)
        {
            return "<color=#888888>No stat bonuses</color>";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Stat Bonuses:</b>");

        foreach (var modifier in equipment.StatModifiers)
        {
            string modifierText = FormatStatModifier(modifier);
            sb.AppendLine($"<color=#4CAF50>• {modifierText}</color>");
        }

        return sb.ToString();
    }

    private string FormatStatModifier(IStatModifier modifier)
    {
        string modifierName = GetFriendlyModifierName(modifier.ModifierType);
        string valueText;

        if (modifier.IsMultiplicative)
        {
            float percentage = modifier.Value * 100f;
            string sign = percentage >= 0 ? "+" : "";
            valueText = $"{sign}{percentage:F0}%";
        }
        else
        {
            string sign = modifier.Value >= 0 ? "+" : "";
            valueText = $"{sign}{modifier.Value:F1}";
        }

        return $"{modifierName}: {valueText}";
    }

    private string GetFriendlyModifierName(StatModifierType type)
    {
        return type switch
        {
            StatModifierType.UniversalWalkSpeed => "Walk Speed",
            StatModifierType.NormalWalkSpeed => "Normal Walk Speed",
            StatModifierType.WalkSpeedSlope => "Slope Walk Speed",
            StatModifierType.ClimbSpeed => "Climb Speed",
            StatModifierType.UniversalStaminaReduce => "Stamina Efficiency",
            StatModifierType.WalkStaminaReduce => "Walk Stamina Efficiency",
            StatModifierType.ClimbStaminaReduce => "Climb Stamina Efficiency",
            StatModifierType.PenaltyFatigueReduce => "Fatigue Penalty Reduction",
            StatModifierType.UniversalFatigueReduce => "Fatigue Reduction",
            StatModifierType.SlopeFatigueReduce => "Slope Fatigue Reduction",
            StatModifierType.FatigueGainWhenRest => "Rest Recovery Bonus",
            _ => type.ToString()
        };
    }

    private void ShowEmptySelection()
    {
        if (selectedEquipmentPanel != null)
        {
            selectedEquipmentPanel.SetActive(false);
        }
    }

    private void UnequipSelectedItem()
    {
        if (selectedSlotType.HasValue && equipmentManager != null)
        {
            equipmentManager.Unequip(selectedSlotType.Value);
            
            // Refresh inventory UI to update equipped visual feedback
            if (inventoryUI != null)
            {
                inventoryUI.UpdateAllSlots();
            }
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

        if (!selectedSlotType.HasValue)
        {
            ShowEmptySelection();
        }
    }

    public void HideEquipmentPanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }

        selectedSlotType = null;
        ShowEmptySelection();
    }
}
