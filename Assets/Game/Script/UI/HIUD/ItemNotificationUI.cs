using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;

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
    private IEventBus eventBus;
    
    private void Awake()
    {
        //Debug.Log("[ItemNotificationUI] Awake called");
        
        // Start hidden
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
            //Debug.Log("[ItemNotificationUI] Notification panel activated");
        }
        else
        {
            Debug.LogError("[ItemNotificationUI] Notification panel is not assigned in inspector!");
        }
        
        // Validate references
        if (notificationContainer == null)
        {
            Debug.LogError("[ItemNotificationUI] Notification container is not assigned in inspector!");
        }
        if (notificationPrefab == null)
        {
            Debug.LogError("[ItemNotificationUI] Notification prefab is not assigned in inspector!");
        }
    }

    private void Start()
    {
        //Debug.Log("[ItemNotificationUI] Start called");
        
        // Get EventBus from ServiceContainer
        // Done in Start() to ensure EventBus has been registered by GameServiceBootstrapper
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
        
        if (eventBus != null)
        {
            //Debug.Log("[ItemNotificationUI] EventBus successfully retrieved from ServiceContainer");
            
            // Subscribe here instead of OnEnable to ensure eventBus is available
            eventBus.Subscribe<Game.Player.Inventory.Events.ItemAddedEvent>(HandleItemAddedEvent);
            eventBus.Subscribe<Game.Player.Inventory.Events.ItemRemovedEvent>(HandleItemRemovedEvent);
            eventBus.Subscribe<Game.Player.Inventory.Events.ItemConsumedEvent>(HandleItemConsumedEvent);
            eventBus.Subscribe<Game.Player.Inventory.Events.InventoryFullEvent>(HandleInventoryFullEvent);
            eventBus.Subscribe<ItemEquippedEvent>(HandleItemEquippedEvent);
            eventBus.Subscribe<ItemUnequippedEvent>(HandleItemUnequippedEvent);
            
            //Debug.Log("[ItemNotificationUI] Successfully subscribed to all events");
        }
        else
        {
            Debug.LogError("[ItemNotificationUI] EventBus not found in ServiceContainer! Notifications will not work!");
        }
    }
    
    private void OnEnable()
    {
        // Moved subscription to Start() to ensure eventBus is available
        //Debug.Log("[ItemNotificationUI] OnEnable called");
    }
    
    private void OnDisable()
    {
        //Debug.Log("[ItemNotificationUI] OnDisable called");
        
        if (eventBus != null)
        {
            // Unsubscribe from EventBus events
            eventBus.Unsubscribe<Game.Player.Inventory.Events.ItemAddedEvent>(HandleItemAddedEvent);
            eventBus.Unsubscribe<Game.Player.Inventory.Events.ItemRemovedEvent>(HandleItemRemovedEvent);
            eventBus.Unsubscribe<Game.Player.Inventory.Events.ItemConsumedEvent>(HandleItemConsumedEvent);
            eventBus.Unsubscribe<Game.Player.Inventory.Events.InventoryFullEvent>(HandleInventoryFullEvent);
            eventBus.Unsubscribe<ItemEquippedEvent>(HandleItemEquippedEvent);
            eventBus.Unsubscribe<ItemUnequippedEvent>(HandleItemUnequippedEvent);
            
            //Debug.Log("[ItemNotificationUI] Successfully unsubscribed from all events");
        }
    }
    
    // EventBus event handlers
    private void HandleItemAddedEvent(Game.Player.Inventory.Events.ItemAddedEvent evt)
    {
        //Debug.Log($"[ItemNotificationUI] ItemAddedEvent received: {evt.Item?.itemName} x{evt.Quantity}");
        
        if (evt.Item == null)
        {
            //Debug.LogWarning("[ItemNotificationUI] ItemAddedEvent has null item!");
            return;
        }
        ShowNotification(evt.Item, evt.Quantity, NotificationType.Added);
    }
    
    private void HandleItemRemovedEvent(Game.Player.Inventory.Events.ItemRemovedEvent evt)
    {
        //Debug.Log($"[ItemNotificationUI] ItemRemovedEvent received: {evt.Item?.itemName} x{evt.Quantity}");
        
        if (evt.Item == null)
        {
            //Debug.LogWarning("[ItemNotificationUI] ItemRemovedEvent has null item!");
            return;
        }
        ShowNotification(evt.Item, evt.Quantity, NotificationType.Removed);
    }
    
    private void HandleItemConsumedEvent(Game.Player.Inventory.Events.ItemConsumedEvent evt)
    {
        //Debug.Log($"[ItemNotificationUI] ItemConsumedEvent received: {evt.Item?.itemName}");
        
        if (evt.Item == null)
        {
            //Debug.LogWarning("[ItemNotificationUI] ItemConsumedEvent has null item!");
            return;
        }
        ShowNotification(evt.Item, 1, NotificationType.Consumed);
    }
    
    private void HandleInventoryFullEvent(Game.Player.Inventory.Events.InventoryFullEvent evt)
    {
        if (evt.Item == null) return;
        ShowNotification(evt.Item, evt.Quantity, NotificationType.InventoryFull);
    }

    private void HandleItemEquippedEvent(ItemEquippedEvent evt)
    {
        //Debug.Log($"[ItemNotificationUI] ItemEquippedEvent received: {evt.Item}");
        
        InventoryItem invItem = evt.Item as InventoryItem;
        if (invItem == null)
        {
            //Debug.LogWarning("[ItemNotificationUI] ItemEquippedEvent item is not an InventoryItem!");
            return;
        }
        ShowNotification(invItem, 1, NotificationType.Equipped);
    }
    
    private void HandleItemUnequippedEvent(ItemUnequippedEvent evt)
    {
        //Debug.Log($"[ItemNotificationUI] ItemUnequippedEvent received: {evt.Item}");
        
        InventoryItem invItem = evt.Item as InventoryItem;
        if (invItem == null)
        {
            //Debug.LogWarning("[ItemNotificationUI] ItemUnequippedEvent item is not an InventoryItem!");
            return;
        }
        ShowNotification(invItem, 1, NotificationType.Unequipped);
    }
    
    /// <summary>
    /// Shows a notification for an item event
    /// </summary>
    public void ShowNotification(InventoryItem item, int quantity, NotificationType type)
    {
        //Debug.Log($"[ItemNotificationUI] ShowNotification called: {item?.itemName} x{quantity} ({type})");
        
        if (item == null)
        {
            Debug.LogError("[ItemNotificationUI] ShowNotification called with null item!");
            return;
        }
        
        if (notificationPrefab == null)
        {
            Debug.LogError("[ItemNotificationUI] Cannot show notification - notificationPrefab is null!");
            return;
        }
        
        if (notificationContainer == null)
        {
            Debug.LogError("[ItemNotificationUI] Cannot show notification - notificationContainer is null!");
            return;
        }
        
        // Create notification data
        NotificationData data = new NotificationData
        {
            item = item,
            quantity = quantity,
            type = type
        };
        
        //Debug.Log($"[ItemNotificationUI] Creating notification for: {data.item.itemName}");
        
        // Create and display notification
        CreateNotification(data);
    }
    
    private void CreateNotification(NotificationData data)
    {
        //Debug.Log($"[ItemNotificationUI] CreateNotification called for: {data.item.itemName}");
        
        // Check if we've reached max notifications
        if (activeNotifications.Count >= maxVisibleNotifications)
        {
            //Debug.Log($"[ItemNotificationUI] Max notifications reached ({maxVisibleNotifications}), removing oldest");
            
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
        //Debug.Log($"[ItemNotificationUI] Instantiating notification prefab");
        GameObject notificationObj = Instantiate(notificationPrefab, notificationContainer);
        notificationObj.SetActive(true);
        activeNotifications.Add(notificationObj);
        
        //Debug.Log($"[ItemNotificationUI] Notification created: {notificationObj.name}, Active count: {activeNotifications.Count}");
        
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
            NotificationType.InventoryFull => "Inventory Full",
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
            NotificationType.InventoryFull => new Color(0.9f, 0.4f, 0.1f), // Orange
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
    Unequipped,
    InventoryFull
}
