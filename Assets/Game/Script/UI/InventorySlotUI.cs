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

    private InventorySlot inventorySlot;
    private int slotIndex;
    private InventoryUI inventoryUI;
    private bool isSelected = false;

    public InventorySlot InventorySlot => inventorySlot;
    public int SlotIndex => slotIndex;
    public bool IsEmpty => inventorySlot == null || inventorySlot.IsEmpty;

    public void Initialize(InventoryUI ui, int index)
    {
        inventoryUI = ui;
        slotIndex = index;

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

        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
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