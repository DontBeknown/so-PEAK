# Item Notification System - Setup Guide

## Overview
A toast-style notification system that displays brief, animated messages when players interact with items (collecting, equipping, consuming, etc.). Uses **DOTween** for smooth, performant animations.

## Requirements
- **DOTween** (Free or Pro version)
- Unity 2020.3 or newer
- TextMeshPro package

## Features
- ✓ Automatic notifications for item pickups
- ✓ Equipment equip/unequip notifications
- ✓ Item consumption notifications
- ✓ Smooth DOTween-powered animations with easing
- ✓ Multiple notifications stack vertically
- ✓ Customizable colors per notification type
- ✓ Automatic cleanup after display duration
- ✓ Optimized with DOTween sequences

## Setup Instructions

### 0. Install DOTween (if not already installed)

1. Download DOTween from the Unity Asset Store (free)
   - Or install via Package Manager if you have it
2. Import DOTween into your project
3. In Unity menu: `Tools > Demigiant > DOTween Utility Panel`
4. Click "Setup DOTween" and follow the prompts
5. Select your target platforms and click "Apply"

### 1. Create the Notification Prefab

1. In Unity Hierarchy, create: `UI > Canvas > ItemNotificationPanel`
2. Add a `Vertical Layout Group` component to organize notifications
3. Set the layout group properties:
   - Child Alignment: Upper Right
   - Spacing: 10
   - Child Force Expand: Width = false, Height = false

4. Create the notification item prefab:
   - Right-click ItemNotificationPanel > UI > Panel
   - Rename it to "NotificationItem"
   - Add a `Horizontal Layout Group` component:
     - Padding: Left=10, Right=10, Top=5, Bottom=5
     - Spacing: 10
     - Child Force Expand: Width = false, Height = false
   - Set preferred size (RectTransform): Width = 300, Height = 60

5. Add child UI elements to NotificationItem:
   ```
   NotificationItem
   ├── Icon (Image)
   │   └── Set size: 40x40
   ├── ItemName (TextMeshPro)
   │   └── Font size: 18, Color: White
   ├── Quantity (TextMeshPro)
   │   └── Font size: 14, Color: Yellow
   └── Action (TextMeshPro)
       └── Font size: 14, Alignment: Right
   ```

6. Add a `CanvasGroup` component to NotificationItem
7. Drag NotificationItem to your Project folder to create a prefab
8. Delete the NotificationItem from the hierarchy (keep the prefab)

### 2. Setup the UI Manager

1. Find your UI Manager GameObject in the scene
2. Add the `ItemNotificationUI` component:
   - Notification Panel: Drag the ItemNotificationPanel GameObject
   - Notification Container: Drag the ItemNotificationPanel GameObject
   - Notification Prefab: Drag the NotificationItem prefab from Project
   
3. Configure settings:
   - Display Duration: 3 seconds (default)
   - Fade In Duration: 0.3 seconds
   - Fade Out Duration: 0.5 seconds
   - Max Visible Notifications: 5

4. In your UIManager component:
   - Assign the ItemNotificationUI reference in the inspector

### 3. Position the Notification Panel

Position the ItemNotificationPanel in the upper-right corner of your UI:

1. Select ItemNotificationPanel
2. Set Anchor Preset to **Top Right**
3. Set position:
   - Pos X: -20
   - Pos Y: -20
4. Adjust Width/Height as needed (recommended: 350 width)

### 4. Test the System

The notifications will automatically work when:
- Items are picked up (via InventoryManager.AddItem)
- Items are consumed (via InventoryManager.ConsumeItem)
- Equipment is equipped/unequipped (via EquipmentManager)

To manually test, create a test button that calls:
```csharp
UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
    "Test Item", 
    yourItemIcon, 
    1, 
    NotificationType.Added
);
```

## Notification Types & Colors

- **Added** (Green): Item picked up
- **Removed** (Red): Item removed from inventory
- **Consumed** (Blue): Item consumed/used
- **Equipped** (Gold): Item equipped
- **Unequipped** (Gray): Item unequipped

## Customization

### Change Display Duration
```

### Change Animation Easing
Edit the `AnimateNotification()` method in ItemNotificationUI.cs:
```csharp
// Change fade in easing
notificationSequence.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));

// Change slide in easing
notificationSequence.Join(rectTransform.DOLocalMove(originalPos, fadeInDuration).SetEase(Ease.OutElastic));

// Available easing options: OutBack, OutBounce, OutElastic, OutQuad, OutCubic, etc.
```csharp
// In ItemNotificationUI inspector
Display Duration = 5f; // Show for 5 seconds
```

### Change Animation Speed
```csharp
Fade In Duration = 0.5f;  // Slower fade in
Fade Out Duration = 0.3f; // Faster fade out
```

### Change Max Visible Notifications
```csharp
Max Visible Notifications = 3; // Show maximum 3 at once
```

### Custom Notification Colors
Edit the `GetActionColor()` method in ItemNotificationUI.cs:
```csharp
private Color GetActionColor(NotificationType type)
{
    return type switch
    {
        NotificationType.Added => new Color(0.3f, 0.8f, 0.3f), // Your custom green
        // ... modify other colors
    };
}
```

## Advanced Usage

### Manual Notifications
```csharp
// Show a custom notification
UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
    "Custom Message",
    customSprite,
    1,
    NotificationType.Added
);
```

### Clear All Notifications
```csharp
// Clear all active notifications immediately
UIManager.Instance.ItemNotificationUI.ClearAllNotifications();
```

## Troubleshooting

### Notifications not appearing?
1. Check that ItemNotificationUI component is on an active GameObject
2. Verify the notification prefab is assigned
3. Ensure Canvas has a Canvas Scaler component
4. Check Console for any errors

### Notifications appearing in wrong position?
1. Verify ItemNotificationPanel anchor is set to Top Right
2. Check parent Canvas RenderMode (Screen Space Overlay recommended)
3. Adjust position offset values

### Multiple notifications overlap?
1. Ensure Vertical Layout Group is added to ItemNotificationPanel
2. Set appropriate spacing in the layout group
3. Check that Child Force Expand is disabled

### Events not firing?
1. Verify InventoryManager and EquipmentManager exist in scene
2. Check that ItemNotificationUI's OnEnable is called
3. Ensure you're using the static events (OnItemAdded, etc.)

## Files Modified/Created

### New Files:
- `Assets/Game/Script/UI/ItemNotificationUI.cs` - Main notification system

### Modified Files:
- `Assets/Game/Script/UI/UIManager.cs` - Added ItemNotificationUI reference
- `Assets/Game/Script/Player/Inventory/EquipmentManager.cs` - Added static events
DOTween sequences for optimal performance
- Old notifications are automatically destroyed
- Maximum notification limit prevents memory issues
- DOTween is highly optimized and GC-friendly
- Tweens are automatically cleaned up on destroy
The notification system automatically integrates with:
- **InventoryManager**: Listens to OnItemAdded, OnItemRemoved, OnItemConsumed
- **EquipmentManager**: Listens to OnItemEquipped, OnItemUnequipped
- **UIManager**: Provides centralized access to the notification system

No additional code changes needed in your existing inventory/equipment logic!

## Performance Notes

- Notifications use coroutines for smooth animations
- Old notifications are automatically destroyed
- Maximum notification limit prevents memory issues
- UI uses Canvas Groups for efficient alpha animations

## Future Enhancements

Possible additions you can impbounce, elastic, custom easing curves)
- Priority system for important notifications
- Persistent notification history panel
- Item rarity-based colors/effects
- Click-to-dismiss functionality
- Scale/rotate animations using DOTween
- Sequence multiple notifications with delaysy panel
- Item rarity-based colors/effects
- Click-to-dismiss functionality
