using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the ItemNotificationUI system.
/// This script can be attached to a test GameObject or used as a reference.
/// </summary>
public class ItemNotificationExample : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private Sprite testItemIcon;
    [SerializeField] private KeyCode testKey = KeyCode.N;
    
    private void Update()
    {
        // Press 'N' key to test notifications
        if (Input.GetKeyDown(testKey))
        {
            TestNotification();
        }
        
        // Press number keys for specific notification types
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TestAddedNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TestRemovedNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TestConsumedNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TestEquippedNotification();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TestUnequippedNotification();
        }
    }
    
    /// <summary>
    /// Test a basic notification
    /// </summary>
    private void TestNotification()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
                "Test Item",
                testItemIcon,
                1,
                NotificationType.Added
            );
            Debug.Log("Test notification shown!");
        }
        else
        {
            Debug.LogWarning("ItemNotificationUI not found! Make sure UIManager is set up correctly.");
        }
    }
    
    /// <summary>
    /// Test "Item Added" notification
    /// </summary>
    private void TestAddedNotification()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
                "Health Potion",
                testItemIcon,
                3,
                NotificationType.Added
            );
        }
    }
    
    /// <summary>
    /// Test "Item Removed" notification
    /// </summary>
    private void TestRemovedNotification()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
                "Stone",
                testItemIcon,
                5,
                NotificationType.Removed
            );
        }
    }
    
    /// <summary>
    /// Test "Item Consumed" notification
    /// </summary>
    private void TestConsumedNotification()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
                "Energy Drink",
                testItemIcon,
                1,
                NotificationType.Consumed
            );
        }
    }
    
    /// <summary>
    /// Test "Item Equipped" notification
    /// </summary>
    private void TestEquippedNotification()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
                "Steel Sword",
                testItemIcon,
                1,
                NotificationType.Equipped
            );
        }
    }
    
    /// <summary>
    /// Test "Item Unequipped" notification
    /// </summary>
    private void TestUnequippedNotification()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
                "Leather Armor",
                testItemIcon,
                1,
                NotificationType.Unequipped
            );
        }
    }
    
    /// <summary>
    /// Test multiple notifications at once
    /// </summary>
    [ContextMenu("Test Multiple Notifications")]
    private void TestMultipleNotifications()
    {
        if (UIManager.Instance?.ItemNotificationUI == null) return;
        
        // Show 5 different notifications in quick succession
        StartCoroutine(ShowMultipleNotificationsCoroutine());
    }
    
    private System.Collections.IEnumerator ShowMultipleNotificationsCoroutine()
    {
        var notificationUI = UIManager.Instance.ItemNotificationUI;
        
        notificationUI.ShowCustomNotification("Wood", testItemIcon, 10, NotificationType.Added);
        yield return new WaitForSeconds(0.2f);
        
        notificationUI.ShowCustomNotification("Stone", testItemIcon, 15, NotificationType.Added);
        yield return new WaitForSeconds(0.2f);
        
        notificationUI.ShowCustomNotification("Iron Ore", testItemIcon, 8, NotificationType.Added);
        yield return new WaitForSeconds(0.2f);
        
        notificationUI.ShowCustomNotification("Health Potion", testItemIcon, 2, NotificationType.Consumed);
        yield return new WaitForSeconds(0.2f);
        
        notificationUI.ShowCustomNotification("Diamond Sword", testItemIcon, 1, NotificationType.Equipped);
    }
    
    /// <summary>
    /// Clear all active notifications
    /// </summary>
    [ContextMenu("Clear All Notifications")]
    private void ClearNotifications()
    {
        if (UIManager.Instance?.ItemNotificationUI != null)
        {
            UIManager.Instance.ItemNotificationUI.ClearAllNotifications();
            Debug.Log("All notifications cleared!");
        }
    }
}
