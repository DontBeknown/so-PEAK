using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

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
    //[SerializeField] private float padding = 10f;
    [Header("Sound Setting")]
    [SerializeField] private string dropSoundId = "UI_ItemEndDrag";
    [SerializeField] private float dropVolumeScale = 0.3f;
    [SerializeField] private string rotateSoundId = "UI_ItemEndDrag";
    [SerializeField] private float rotateVolumeScale = 0.3f;
    [SerializeField] private string equipSoundId = "UI_ItemEquip";
    [SerializeField] private float equipVolumeScale = 0.3f;
    [SerializeField] private string drinkSoundId = "Player_Drink";
    [SerializeField] private float drinkVolumeScale = 0.5f;
    [SerializeField] private string eatingSoundId = "Player_Eat";
    [SerializeField] private float eatingVolumeScale = 0.5f;
    private RectTransform menuRect;
    private Canvas canvas;
    private RectTransform canvasRect;
    private List<GameObject> activeButtons = new List<GameObject>();
    
    // For dynamic menu updates
    private InventorySlotUI currentSlotUI;
    private InventoryUI currentInventoryUI;
    private EquipmentManager currentEquipmentManager;
    private bool isInventoryMenu;
    private bool lastCanteenCanDrink;
    private Coroutine cooldownCheckCoroutine; 

    // For grid item menu
    private GridInventoryUI currentGridUI;
    private GridItemUI currentGridItemUI;

    private IEventBus _eventBus;

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

    private void Start()
    {
        _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
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
    
    private System.Collections.IEnumerator CheckCanteenCooldown()
    {
        while (isInventoryMenu && currentSlotUI != null && !currentSlotUI.IsEmpty)
        {
            var item = currentSlotUI.InventorySlot.item;
            CanteenItem canteenItem = item as CanteenItem;
            
            if (canteenItem != null)
            {
                // Only refresh if state changed
                bool canDrinkNow = canteenItem.CanDrink();
                if (canDrinkNow != lastCanteenCanDrink)
                {
                    lastCanteenCanDrink = canDrinkNow;
                    RefreshInventoryMenu();
                }
            }
            else
            {
                // Not a canteen, stop checking
                yield break;
            }
            
            // Check every 0.1 seconds instead of every frame
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    /// <summary>
    /// Show context menu for an inventory item.
    /// </summary>
    public void ShowInventoryMenu(InventorySlotUI slotUI, InventoryUI inventoryUI, EquipmentManager equipmentManager)
    {
        if (slotUI == null || slotUI.IsEmpty) return;

        _eventBus?.Publish(new ContextMenuOpenedEvent());

        // Store context for dynamic updates
        currentSlotUI = slotUI;
        currentInventoryUI = inventoryUI;
        currentEquipmentManager = equipmentManager;
        isInventoryMenu = true;

        // Initialize canteen state tracking
        CanteenItem canteen = slotUI.InventorySlot.item as CanteenItem;
        if (canteen != null)
        {
            lastCanteenCanDrink = canteen.CanDrink();
            
            // Start cooldown check coroutine
            if (cooldownCheckCoroutine != null)
            {
                StopCoroutine(cooldownCheckCoroutine);
            }
            cooldownCheckCoroutine = StartCoroutine(CheckCanteenCooldown());
        }

        ClearButtons();

        var item = slotUI.InventorySlot.item;
        EquipmentItem equipItem = item as EquipmentItem;
        CanteenItem canteenItem = item as CanteenItem; // Check for canteen

        // Check if item is equipment
        if (equipItem != null)
        {
            // Special handling for canteen
            if (canteenItem != null)
            {
                // Add Drink option - always shown but disabled if can't drink
                if (canteenItem.CanDrink())
                {
                    AddButton("Drink", () => {
                        var playerStats = Game.Core.DI.ServiceContainer.Instance.TryGet<PlayerStats>();
                        if (canteenItem.Drink(playerStats))
                        {
                            inventoryUI?.UpdateAllSlots();
                        }
                        HideMenu();
                    });
                }
                else
                {
                    // Disabled button when empty or on cooldown
                    AddButton("Drink", null);
                }
            }
            
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

        // Clear inventory context
        currentSlotUI = null;
        currentInventoryUI = null;
        currentEquipmentManager = null;
        isInventoryMenu = false;

        ClearButtons();

        // Only option for equipped items is to unequip
        AddButton("Unequip", () => {
            slotUI.UnequipItem();
            HideMenu();
            _eventBus.Publish(new PlayUISoundEvent(equipSoundId, equipVolumeScale));
        });

        ShowMenu();
    }

    /// <summary>
    /// Show context menu for a grid inventory item.
    /// </summary>
    public void ShowGridItemMenu(GridInventoryUI gridUI, GridItemUI itemUI)
    {
        if (gridUI == null || itemUI == null || itemUI.Placement == null) return;

        _eventBus?.Publish(new ContextMenuOpenedEvent());

        // Clear slot-based context
        currentSlotUI = null;
        currentInventoryUI = null;
        currentEquipmentManager = null;
        isInventoryMenu = false;

        // Store grid context
        currentGridUI = gridUI;
        currentGridItemUI = itemUI;

        ClearButtons();

        var item = itemUI.Placement.Item;
        EquipmentItem equipItem = item as EquipmentItem;

        if (equipItem != null)
        {
            // Canteen special handling
            CanteenItem canteenItem = item as CanteenItem;
            if (canteenItem != null)
            {
                if (canteenItem.CanDrink())
                {
                    AddButton("Drink", () => {
                        var playerStats = Game.Core.DI.ServiceContainer.Instance.TryGet<PlayerStats>();
                        canteenItem.Drink(playerStats);
                        HideMenu();
                        _eventBus.Publish(new PlayUISoundEvent(drinkSoundId, drinkVolumeScale));
                    });
                }
                else
                {
                    AddButton("Drink", null);
                }
            }

            // Equip / Unequip
            var eqManager = Game.Core.DI.ServiceContainer.Instance.TryGet<EquipmentManager>();
            bool isEquipped = eqManager != null &&
                (UnityEngine.Object)eqManager.GetEquippedItem(equipItem.EquipmentSlot) == equipItem;

            if (isEquipped)
            {
                AddButton("Unequip", () => {
                    eqManager?.Unequip(equipItem.EquipmentSlot);
                    HideMenu();
                    _eventBus?.Publish(new PlayUISoundEvent(equipSoundId, equipVolumeScale));
                });
            }
            else
            {
                AddButton("Equip", () => {
                    gridUI.UseItem(itemUI);
                    HideMenu();
                    _eventBus?.Publish(new PlayUISoundEvent(equipSoundId, equipVolumeScale));
                });
            }
        }
        else if (item.isConsumable)
        {
            AddButton("Consume", () => {
                gridUI.UseItem(itemUI);
                HideMenu();
                _eventBus.Publish(new PlayUISoundEvent(eatingSoundId, eatingVolumeScale));
            });
        }

        // Rotate — only useful for non-square items
        if (item.gridSize.x != item.gridSize.y)
        {
            AddButton("Rotate", () => {
                gridUI.RotateItem(itemUI);
                HideMenu();
                _eventBus.Publish(new PlayUISoundEvent(rotateSoundId, rotateVolumeScale));
            });
        }

        // All items can be dropped
        AddButton("Drop", () => {
            gridUI.DropItem(itemUI);
            _eventBus.Publish(new PlayUISoundEvent(dropSoundId, dropVolumeScale));
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
            if (onClick != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke());
            }
            else
            {
                // Disabled button - null onClick means it's just informational
                button.interactable = false;
            }
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
        
        // Stop cooldown check coroutine
        if (cooldownCheckCoroutine != null)
        {
            StopCoroutine(cooldownCheckCoroutine);
            cooldownCheckCoroutine = null;
        }
        
        // Clear context
        currentSlotUI = null;
        currentInventoryUI = null;
        currentEquipmentManager = null;
        currentGridUI = null;
        currentGridItemUI = null;
        isInventoryMenu = false;
    }
    
    private void RefreshInventoryMenu()
    {
        if (currentSlotUI == null || currentSlotUI.IsEmpty) return;
        
        ClearButtons();
        
        var item = currentSlotUI.InventorySlot.item;
        EquipmentItem equipItem = item as EquipmentItem;
        CanteenItem canteenItem = item as CanteenItem;

        // Check if item is equipment
        if (equipItem != null)
        {
            // Special handling for canteen
            if (canteenItem != null)
            {
                // Add Drink option - always shown but disabled if can't drink
                if (canteenItem.CanDrink())
                {
                    AddButton("Drink", () => {
                        var playerStats = Game.Core.DI.ServiceContainer.Instance.TryGet<PlayerStats>();
                        if (canteenItem.Drink(playerStats))
                        {
                            currentInventoryUI?.UpdateAllSlots();
                        }
                        HideMenu();
                    });
                }
                else
                {
                    // Disabled button when empty or on cooldown
                    AddButton("Drink", null);
                }
            }
            
            // Check if already equipped
            bool isEquipped = currentEquipmentManager != null && 
                             (UnityEngine.Object)currentEquipmentManager.GetEquippedItem(equipItem.EquipmentSlot) == equipItem;

            if (isEquipped)
            {
                AddButton("Unequip", () => {
                    currentEquipmentManager?.Unequip(equipItem.EquipmentSlot);
                    currentInventoryUI?.UpdateAllSlots();
                    HideMenu();
                    _eventBus?.Publish(new PlayUISoundEvent(equipSoundId, equipVolumeScale));
                });
            }
            else
            {
                AddButton("Equip", () => {
                    currentEquipmentManager?.Equip(equipItem);
                    currentInventoryUI?.UpdateAllSlots();
                    HideMenu();
                    _eventBus?.Publish(new PlayUISoundEvent(equipSoundId, equipVolumeScale));
                });
            }
        }
        else if (item.isConsumable)
        {
            // Consumable item
            AddButton("Consume", () => {
                currentInventoryUI?.UseItem(currentSlotUI.SlotIndex);
                HideMenu();
            });
        }

        // All items can be dropped
        AddButton("Drop", () => {
            currentInventoryUI?.DropItem(currentSlotUI.SlotIndex);
            HideMenu();
        });
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuRect);
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
