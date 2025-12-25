using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image highlightImage;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private Color equippedColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Green tint for equipped items

    private InventorySlot inventorySlot;
    private int slotIndex;
    private InventoryUI inventoryUI;
    private bool isSelected = false;
    private EquipmentManager equipmentManager; // To check if item is equipped

    public InventorySlot InventorySlot => inventorySlot;
    public int SlotIndex => slotIndex;
    public bool IsEmpty => inventorySlot == null || inventorySlot.IsEmpty;

    public void Initialize(InventoryUI ui, int index)
    {
        inventoryUI = ui;
        slotIndex = index;
        
        // Get equipment manager reference
        equipmentManager = FindFirstObjectByType<EquipmentManager>();

        // Hide highlight initially
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    public void UpdateSlot(InventorySlot slot)
    {
        inventorySlot = slot;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (inventorySlot == null || inventorySlot.IsEmpty)
        {
            // Empty slot
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.gameObject.SetActive(false);
            }

            if (quantityText != null)
            {
                quantityText.text = "";
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Slot has item
            if (itemIcon != null && inventorySlot.item.icon != null)
            {
                itemIcon.sprite = inventorySlot.item.icon;
                itemIcon.gameObject.SetActive(true);
            }

            if (quantityText != null)
            {
                if (inventorySlot.quantity > 1)
                {
                    quantityText.text = inventorySlot.quantity.ToString();
                    quantityText.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.gameObject.SetActive(false);
                }
            }
        }

        UpdateBackgroundColor();
    }

    private void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;

        // Check if this item is currently equipped
        bool isEquipped = IsItemEquipped();
        
        if (isEquipped)
        {
            // Show equipped color (green tint)
            backgroundImage.color = equippedColor;
        }
        else if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }
    
    private bool IsItemEquipped()
    {
        if (inventorySlot == null || inventorySlot.IsEmpty || equipmentManager == null)
            return false;
        
        // Check if item is equipment
        EquipmentItem equipItem = inventorySlot.item as EquipmentItem;
        if (equipItem == null)
            return false;
        
        // Check if this specific item is currently equipped
        IEquippable equippedItem = equipmentManager.GetEquippedItem(equipItem.EquipmentSlot);
        return equippedItem == equipItem;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBackgroundColor();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left click - select slot and show in selected panel
            inventoryUI.SelectSlot(slotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click - quick use item
            inventoryUI.UseItem(slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only show highlight, no tooltip
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide highlight
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }
}