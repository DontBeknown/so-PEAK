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
        if (item == null)
        {
            SetPromptVisible(false);
            return;
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.GetDisplayName();
        }

        // Update icon if you have item icons set up
        if (itemIcon != null)
        {
            // Use the new ItemIcon property on ResourceCollector. If there's no icon, disable the image to avoid showing a blank/old sprite.
            Sprite icon = item.ItemIcon;
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Shows the pickup prompt for a specific item
    /// </summary>
    public void Show(string itemName)
    {
        if (itemNameText != null)
        {
            itemNameText.text = itemName;
        }
        SetPromptVisible(true);
    }
    
    /// <summary>
    /// Hides the pickup prompt
    /// </summary>
    public void Hide()
    {
        SetPromptVisible(false);
    }
    
    /// <summary>
    /// Checks if there's an item in range and shows prompt accordingly
    /// This is called by UIManager when menus close
    /// </summary>
    public void CheckAndShow()
    {
        // The prompt visibility is already managed by ItemDetector events
        // This method exists for UIManager to call when needed
    }
}