using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Displays a context menu near the mouse cursor with available actions for the selected item/equipment.
/// </summary>
public class ContextMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject contextMenuPanel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(5f, -5f);
    [SerializeField] private float padding = 10f;

    private RectTransform menuRect;
    private Canvas canvas;
    private RectTransform canvasRect;
    private List<GameObject> activeButtons = new List<GameObject>();

    public bool IsVisible => contextMenuPanel.activeSelf;

    private void Awake()
    {
        menuRect = contextMenuPanel.GetComponent<RectTransform>();
        
        // Set pivot to bottom-left for easier positioning
        if (menuRect != null)
        {
            menuRect.pivot = new Vector2(0, 0);
        }
        
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        HideMenu();
    }

    private void Update()
    {
        // Hide menu on click outside or ESC
        if (contextMenuPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HideMenu();
            }
            
            // Check if clicked outside the menu
            if (Input.GetMouseButtonDown(0))
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    menuRect, 
                    Input.mousePosition, 
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera))
                {
                    HideMenu();
                }
            }
        }
    }

    /// <summary>
    /// Show context menu for an inventory item.
    /// </summary>
    public void ShowInventoryMenu(InventorySlotUI slotUI, InventoryUI inventoryUI, EquipmentManager equipmentManager)
    {
        if (slotUI == null || slotUI.IsEmpty) return;

        ClearButtons();

        var item = slotUI.InventorySlot.item;
        EquipmentItem equipItem = item as EquipmentItem;

        // Check if item is equipment
        if (equipItem != null)
        {
            // Check if already equipped
            bool isEquipped = equipmentManager != null && 
                             (UnityEngine.Object)equipmentManager.GetEquippedItem(equipItem.EquipmentSlot) == equipItem;

            if (isEquipped)
            {
                AddButton("Unequip", () => {
                    equipmentManager?.Unequip(equipItem.EquipmentSlot);
                    inventoryUI?.UpdateAllSlots();
                    HideMenu();
                });
            }
            else
            {
                AddButton("Equip", () => {
                    equipmentManager?.Equip(equipItem);
                    inventoryUI?.UpdateAllSlots();
                    HideMenu();
                });
            }
        }
        else if (item.isConsumable)
        {
            // Consumable item
            AddButton("Consume", () => {
                inventoryUI?.UseItem(slotUI.SlotIndex);
                HideMenu();
            });
        }

        // All items can be dropped
        AddButton("Drop", () => {
            inventoryUI?.DropItem(slotUI.SlotIndex);
            HideMenu();
        });

        ShowMenu();
    }

    /// <summary>
    /// Show context menu for an equipment slot.
    /// </summary>
    public void ShowEquipmentMenu(EquipmentSlotUI slotUI, EquipmentUI equipmentUI)
    {
        if (slotUI == null || slotUI.IsEmpty) return;

        ClearButtons();

        // Only option for equipped items is to unequip
        AddButton("Unequip", () => {
            slotUI.UnequipItem();
            HideMenu();
        });

        ShowMenu();
    }

    private void AddButton(string label, Action onClick)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        buttonObj.SetActive(true);

        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            buttonText.text = label;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        activeButtons.Add(buttonObj);
    }

    private void ClearButtons()
    {
        foreach (var button in activeButtons)
        {
            Destroy(button);
        }
        activeButtons.Clear();
    }

    private void ShowMenu()
    {
        contextMenuPanel.SetActive(true);
        
        // Force rebuild layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
        
        UpdatePosition();
    }

    public void HideMenu()
    {
        contextMenuPanel.SetActive(false);
        ClearButtons();
    }

    private void UpdatePosition()
    {
        if (menuRect == null) return;

        // Get menu size
        Vector2 menuSize = new Vector2(menuRect.rect.width, menuRect.rect.height);

        // Start with default offset (right side of mouse)
        Vector2 currentOffset = offset;

        // Check if menu would go off the right edge of screen
        if (Input.mousePosition.x + offset.x + menuSize.x > Screen.width)
        {
            // Move to left side of mouse instead
            currentOffset.x = -menuSize.x - Mathf.Abs(offset.x);
        }

        // Get mouse position and add offset
        Vector3 mousePos = Input.mousePosition + (Vector3)currentOffset;

        // Set position directly in screen space
        menuRect.position = mousePos;
    }
}
