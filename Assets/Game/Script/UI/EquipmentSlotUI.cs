using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI component for a single equipment slot.
/// Displays equipped item and handles equip/unequip interactions.
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image slotIcon; // Background icon showing slot type (head, body, etc.)
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private TextMeshProUGUI slotLabel; // "Head", "Body", etc.

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highlightColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color equippedColor = new Color(0.3f, 0.5f, 0.3f, 1f);

    private EquipmentSlotType slotType;
    private EquipmentManager equipmentManager;
    private IEquippable equippedItem;
    private EquipmentUI equipmentUI;

    public EquipmentSlotType SlotType => slotType;
    public bool IsEmpty => equippedItem == null;
    public IEquippable EquippedItem => equippedItem;

    public void Initialize(EquipmentUI ui, EquipmentSlotType type, EquipmentManager manager)
    {
        equipmentUI = ui;
        slotType = type;
        equipmentManager = manager;

        // Set slot label
        if (slotLabel != null)
        {
            slotLabel.text = type.ToString();
        }

        // Hide highlight initially
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);

        UpdateVisuals();
    }

    public void UpdateSlot(IEquippable item)
    {
        equippedItem = item;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (equippedItem == null)
        {
            // Empty slot - show slot icon, hide item icon
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false; // Disable Image component instead of GameObject
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }

            // Show slot icon if available
            if (slotIcon != null)
            {
                slotIcon.enabled = true; // Enable Image component
            }
        }
        else
        {
            // Slot has equipment - show item icon, hide slot icon
            EquipmentItem equipItem = equippedItem as EquipmentItem;
            if (equipItem != null && itemIcon != null)
            {
                if (equipItem.icon != null)
                {
                    itemIcon.sprite = equipItem.icon;
                    itemIcon.enabled = true; // Enable Image component
                }
                else
                {
                    // No icon assigned to equipment
                    itemIcon.sprite = null;
                    itemIcon.enabled = false;
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = equippedColor;
            }

            // Hide slot icon when equipped
            if (slotIcon != null)
            {
                slotIcon.enabled = false; // Disable Image component
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left click - select this equipment slot
            if (equipmentUI != null)
            {
                equipmentUI.SelectEquipmentSlot(slotType);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click - quick unequip
            if (!IsEmpty && equipmentManager != null)
            {
                equipmentManager.Unequip(slotType);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(true);

        if (backgroundImage != null && IsEmpty)
            backgroundImage.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);

        if (backgroundImage != null && IsEmpty)
            backgroundImage.color = normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Handle drag-and-drop from inventory
        // This will be implemented when drag-and-drop is added
        Debug.Log($"Item dropped on {slotType} slot");
    }

    /// <summary>
    /// Attempts to equip an item to this slot.
    /// </summary>
    public bool TryEquipItem(EquipmentItem item)
    {
        if (item == null) return false;
        if (item.EquipmentSlot != slotType) return false;
        if (equipmentManager == null) return false;

        equipmentManager.Equip(item);
        return true;
    }

    /// <summary>
    /// Unequips the current item from this slot.
    /// </summary>
    public void UnequipItem()
    {
        if (equipmentManager != null && !IsEmpty)
        {
            equipmentManager.Unequip(slotType);
        }
    }
}
