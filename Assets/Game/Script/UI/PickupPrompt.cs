using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PickupPrompt : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI keyPromptText;
    [SerializeField] private Image itemIcon;

    [Header("Settings")]
    [SerializeField] private KeyCode pickupKey = KeyCode.F;

    private void Start()
    {
        // Subscribe to item detection events
        ItemDetector.OnNearestItemChanged += OnNearestItemChanged;
        ItemDetector.OnItemInRange += OnItemInRange;

        // Set the key prompt text
        if (keyPromptText != null)
        {
            keyPromptText.text = $"Press {pickupKey.ToString()} to collect";
        }

        // Hide prompt initially
        SetPromptVisible(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        ItemDetector.OnNearestItemChanged -= OnNearestItemChanged;
        ItemDetector.OnItemInRange -= OnItemInRange;
    }

    private void OnNearestItemChanged(ResourceCollector nearestItem)
    {
        if (nearestItem != null)
        {
            UpdatePromptContent(nearestItem);
        }
    }

    private void OnItemInRange(bool hasItem)
    {
        SetPromptVisible(hasItem);
    }

    private void UpdatePromptContent(ResourceCollector item)
    {
        if (item == null) SetPromptVisible(false);

        if (itemNameText != null)
        {
            itemNameText.text = item.GetDisplayName();
        }

        // Update icon if you have item icons set up
        if (itemIcon != null && item != null)
        {
            // You'll need to get the icon from the ResourceCollector's item
            // itemIcon.sprite = item.ResourceItem.icon;
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }
}