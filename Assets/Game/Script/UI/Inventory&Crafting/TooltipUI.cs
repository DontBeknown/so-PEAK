using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Displays item/equipment details in a tooltip near the mouse cursor.
/// </summary>
public class TooltipUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemStatsText;
    [SerializeField] private TextMeshProUGUI itemTypeText;
    [SerializeField] private TextMeshProUGUI itemQuantityText;

    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);
    //[SerializeField] private float padding = 10f;

    private RectTransform tooltipRect;
    private Canvas canvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        
        // Set pivot to bottom-left for easier positioning
        if (tooltipRect != null)
        {
            tooltipRect.pivot = new Vector2(0, 0);
        }
        
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        HideTooltip();
    }

    public void ShowTooltip(InventoryItem item, int quantity = 1)
    {
        if (item == null) return;

        tooltipPanel.SetActive(true);

        // Update icon
        if (itemIcon != null)
        {
            if (item.icon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }

        // Update name
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        // Update description
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.description;
        }

        // Update item type
        if (itemTypeText != null)
        {
            itemTypeText.text = GetItemTypeText(item);
        }

        // Update quantity
        if (itemQuantityText != null)
        {
            if (quantity > 1)
            {
                itemQuantityText.text = $"Quantity: {quantity}";
                itemQuantityText.gameObject.SetActive(true);
            }
            else
            {
                itemQuantityText.gameObject.SetActive(false);
            }
        }

        // Update stats (for equipment)
        if (itemStatsText != null)
        {
            System.Text.StringBuilder statsBuilder = new System.Text.StringBuilder();
            
            // Show state for held equipment items (torch durability, canteen charges)
            if (item is HeldEquipmentItem heldItem)
            {
                string stateDesc = GetHeldItemStateText(heldItem);
                if (!string.IsNullOrEmpty(stateDesc))
                {
                    statsBuilder.AppendLine(stateDesc);
                }
            }
            
            // Show stat modifiers for equipment
            EquipmentItem equipItem = item as EquipmentItem;
            if (equipItem != null && equipItem.StatModifiers != null && equipItem.StatModifiers.Count > 0)
            {
                if (statsBuilder.Length > 0) statsBuilder.AppendLine();
                statsBuilder.Append(GetStatModifiersText(equipItem));
            }
            
            if (statsBuilder.Length > 0)
            {
                itemStatsText.text = statsBuilder.ToString().TrimEnd();
                itemStatsText.gameObject.SetActive(true);
            }
            else
            {
                itemStatsText.gameObject.SetActive(false);
            }
        }

        // Force layout rebuild for Content Size Fitter
        Canvas.ForceUpdateCanvases();
        if (tooltipRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        // Update position after layout rebuild
        StartCoroutine(UpdatePositionNextFrame());
    }

    private System.Collections.IEnumerator UpdatePositionNextFrame()
    {
        // Wait for end of frame to ensure layout is complete
        yield return new WaitForEndOfFrame();
        UpdatePosition();
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        if (tooltipRect == null) return;

        // Get tooltip size
        Vector2 tooltipSize = new Vector2(tooltipRect.rect.width, tooltipRect.rect.height);

        // Start with default offset (right side of mouse)
        Vector2 currentOffset = offset;

        //Debug.Log($"Tooltip Size: {tooltipSize.x}, Screen Size: {Screen.width}x{Screen.height} Mouse Position: {Input.mousePosition.x}");
        // Check if tooltip would go off the right edge of screen
        if (Input.mousePosition.x + offset.x + tooltipSize.x > 1920)
        {
            // Move to left side of mouse instead
            currentOffset.x = -tooltipSize.x - Mathf.Abs(offset.x);
        }

        // Get mouse position and add offset
        Vector3 mousePos = Input.mousePosition + (Vector3)currentOffset;

        // Set position directly in screen space
        tooltipRect.position = mousePos;
    }

    private string GetItemTypeText(InventoryItem item)
    {
        EquipmentItem equipItem = item as EquipmentItem;
        if (equipItem != null)
        {
            return $"<color=#FFD700>Equipment ({equipItem.EquipmentSlot})</color>";
        }

        return item.isConsumable ? "<color=#4CAF50>Consumable</color>" : "<color=#888888>Item</color>";
    }
    
    private string GetHeldItemStateText(HeldEquipmentItem heldItem)
    {
        if (heldItem is TorchItem torchItem)
        {
            string durability = torchItem.GetStateDescription();
            return $"<b>Durability:</b> <color=#FFA500>{durability}</color>";
        }
        else if (heldItem is CanteenItem canteenItem)
        {
            string charges = canteenItem.GetStateDescription();
            return $"<b>Charges:</b> <color=#4FC3F7>{charges}</color>";
        }
        
        return "";
    }

    private string GetStatModifiersText(EquipmentItem equipment)
    {
        if (equipment.StatModifiers == null || equipment.StatModifiers.Count == 0)
        {
            return "";
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Stat Bonuses:</b>");

        foreach (var modifier in equipment.StatModifiers)
        {
            string modifierText = FormatStatModifier(modifier);
            sb.AppendLine($"<color=#4CAF50>• {modifierText}</color>");
        }

        return sb.ToString().TrimEnd();
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
}
