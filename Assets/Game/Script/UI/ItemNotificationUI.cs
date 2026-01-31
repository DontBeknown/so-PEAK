using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Displays toast-style notifications for item-related events (pickup, equip, consume, etc.)
/// Notifications appear briefly and stack if multiple items are collected quickly.
/// </summary>
public class ItemNotificationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private GameObject notificationPrefab;
    
    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private int maxVisibleNotifications = 5;
    
    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private List<GameObject> activeNotifications = new List<GameObject>();
    
    private void Awake()
    {
        // Start hidden
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to inventory events
        InventoryManager.OnItemAdded += HandleItemAdded;
        InventoryManager.OnItemRemoved += HandleItemRemoved;
        InventoryManager.OnItemConsumed += HandleItemConsumed;
        
        // Subscribe to equipment events if available
        EquipmentManager.OnItemEquipped += HandleItemEquipped;
        EquipmentManager.OnItemUnequipped += HandleItemUnequipped;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from events
        InventoryManager.OnItemAdded -= HandleItemAdded;
        InventoryManager.OnItemRemoved -= HandleItemRemoved;
        InventoryManager.OnItemConsumed -= HandleItemConsumed;
        
        EquipmentManager.OnItemEquipped -= HandleItemEquipped;
        EquipmentManager.OnItemUnequipped -= HandleItemUnequipped;
    }
    
    private void HandleItemAdded(InventoryItem item, int quantity)
    {
        if (item == null) return;
        ShowNotification(item, quantity, NotificationType.Added);
    }
    
    private void HandleItemRemoved(InventoryItem item, int quantity)
    {
        if (item == null) return;
        ShowNotification(item, quantity, NotificationType.Removed);
    }
    
    private void HandleItemConsumed(InventoryItem item, int quantity)
    {
        if (item == null) return;
        ShowNotification(item, quantity, NotificationType.Consumed);
    }
    
    private void HandleItemEquipped(IEquippable item)
    {
        InventoryItem invItem = item as InventoryItem;
        if (invItem == null) return;
        ShowNotification(invItem, 1, NotificationType.Equipped);
    }
    
    private void HandleItemUnequipped(IEquippable item)
    {
        InventoryItem invItem = item as InventoryItem;
        if (invItem == null) return;
        ShowNotification(invItem, 1, NotificationType.Unequipped);
    }
    
    /// <summary>
    /// Shows a notification for an item event
    /// </summary>
    public void ShowNotification(InventoryItem item, int quantity, NotificationType type)
    {
        if (item == null || notificationPrefab == null || notificationContainer == null)
            return;
        
        // Create notification data
        NotificationData data = new NotificationData
        {
            item = item,
            quantity = quantity,
            type = type
        };
        
        // Create and display notification
        CreateNotification(data);
    }
    
    private void CreateNotification(NotificationData data)
    {
        // Check if we've reached max notifications
        if (activeNotifications.Count >= maxVisibleNotifications)
        {
            // Remove oldest notification properly
            if (activeNotifications.Count > 0)
            {
                GameObject oldest = activeNotifications[0];
                activeNotifications.RemoveAt(0);
                
                // Simply destroy - tweens are linked and will auto-kill
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }
        }
        
        // Instantiate notification
        GameObject notificationObj = Instantiate(notificationPrefab, notificationContainer);
        notificationObj.SetActive(true);
        activeNotifications.Add(notificationObj);
        
        // Setup notification content
        SetupNotificationContent(notificationObj, data);
        
        // Start DOTween animation
        AnimateNotification(notificationObj);
    }
    
    private void SetupNotificationContent(GameObject notificationObj, NotificationData data)
    {
        // Get the NotificationUI component
        NotificationUI notificationUI = notificationObj.GetComponent<NotificationUI>();
        if (notificationUI == null)
        {
            Debug.LogError("NotificationUI component not found on notification prefab!");
            return;
        }
        
        // Set icon
        if (notificationUI.iconImage != null && data.item.icon != null)
        {
            notificationUI.iconImage.sprite = data.item.icon;
            notificationUI.iconImage.enabled = true;
        }
        
        // Set item name
        if (notificationUI.itemNameText != null)
        {
            notificationUI.itemNameText.text = data.item.itemName;
        }
        
        // Set quantity
        if (notificationUI.quantityText != null)
        {
            if (data.quantity > 1)
            {
                notificationUI.quantityText.text = $"x{data.quantity}";
                notificationUI.quantityText.gameObject.SetActive(true);
            }
            else
            {
                notificationUI.quantityText.gameObject.SetActive(false);
            }
        }
        
        // Set action text and color
        if (notificationUI.actionText != null)
        {
            string actionString = GetActionString(data.type);
            notificationUI.actionText.text = actionString;
            notificationUI.actionText.color = GetActionColor(data.type);
        }
    }
    
    private string GetActionString(NotificationType type)
    {
        return type switch
        {
            NotificationType.Added => "+ Added",
            NotificationType.Removed => "- Removed",
            NotificationType.Consumed => "Consumed",
            NotificationType.Equipped => "Equipped",
            NotificationType.Unequipped => "Unequipped",
            _ => ""
        };
    }
    
    private Color GetActionColor(NotificationType type)
    {
        return type switch
        {
            NotificationType.Added => new Color(0.3f, 0.8f, 0.3f), // Green
            NotificationType.Removed => new Color(0.8f, 0.3f, 0.3f), // Red
            NotificationType.Consumed => new Color(0.3f, 0.6f, 0.9f), // Blue
            NotificationType.Equipped => new Color(0.9f, 0.7f, 0.2f), // Gold
            NotificationType.Unequipped => new Color(0.6f, 0.6f, 0.6f), // Gray
            _ => Color.white
        };
    }
    
    private void AnimateNotification(GameObject notificationObj)
    {
        if (notificationObj == null) return;
        
        CanvasGroup canvasGroup = notificationObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = notificationObj.AddComponent<CanvasGroup>();
        }
        
        RectTransform rectTransform = notificationObj.GetComponent<RectTransform>();
        
        // Set initial state (invisible and slightly scaled down)
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * 0.8f;
        
        // Create animation sequence and link it to the GameObject
        Sequence notificationSequence = DOTween.Sequence();
        
        // Link tweens to GameObject - they'll auto-kill if the GameObject is destroyed
        notificationSequence.SetLink(notificationObj, LinkBehaviour.KillOnDestroy);
        notificationSequence.SetTarget(notificationObj);
        notificationSequence.SetAutoKill(true);
        
        // Fade in and scale up (pop in effect)
        notificationSequence.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad).SetLink(notificationObj));
        notificationSequence.Join(rectTransform.DOScale(1f, fadeInDuration).SetEase(Ease.OutBack).SetLink(notificationObj));
        
        // Wait for display duration
        notificationSequence.AppendInterval(displayDuration);
        
        // Fade out and scale down slightly
        notificationSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad).SetLink(notificationObj));
        notificationSequence.Join(rectTransform.DOScale(0.9f, fadeOutDuration).SetEase(Ease.InBack).SetLink(notificationObj));
        
        // Cleanup after animation completes
        notificationSequence.OnComplete(() =>
        {
            if (notificationObj != null && activeNotifications.Contains(notificationObj))
            {
                activeNotifications.Remove(notificationObj);
                Destroy(notificationObj);
            }
        });
    }
    
    /// <summary>
    /// Manually show a custom notification
    /// </summary>
    public void ShowCustomNotification(string itemName, Sprite icon, int quantity, NotificationType type)
    {
        // Create a temporary inventory item for display purposes
        var tempItem = ScriptableObject.CreateInstance<InventoryItem>();
        tempItem.itemName = itemName;
        tempItem.icon = icon;
        
        ShowNotification(tempItem, quantity, type);
        
        // Clean up temp item after a delay
        Destroy(tempItem, displayDuration + fadeInDuration + fadeOutDuration + 1f);
    }
    
    /// <summary>
    /// Clear all active notifications
    /// </summary>
    public void ClearAllNotifications()
    {
        // Create a copy to avoid modification during iteration
        var notificationsCopy = new List<GameObject>(activeNotifications);
        activeNotifications.Clear();
        
        foreach (GameObject notification in notificationsCopy)
        {
            if (notification != null)
            {
                // Simply destroy - tweens are linked and will auto-kill
                Destroy(notification);
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up any active notifications
        var notificationsCopy = new List<GameObject>(activeNotifications);
        foreach (GameObject notification in notificationsCopy)
        {
            if (notification != null)
            {
                Destroy(notification);
            }
        }
        activeNotifications.Clear();
    }
}

/// <summary>
/// Data structure for notification information
/// </summary>
public class NotificationData
{
    public InventoryItem item;
    public int quantity;
    public NotificationType type;
}

/// <summary>
/// Types of item notifications
/// </summary>
public enum NotificationType
{
    Added,
    Removed,
    Consumed,
    Equipped,
    Unequipped
}
