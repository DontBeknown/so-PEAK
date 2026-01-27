# Item Notification System - Quick Reference

## What Was Implemented

A complete toast-style notification system for displaying item-related events in your Unity game.

## Files Created

1. **ItemNotificationUI.cs** - Main notification system component
   - Handles display of item notifications
   - Auto-subscribes to inventory and equipment events
   - Manages notification queue and animations

2. **ItemNotificationExample.cs** - Example/test script
   - Shows how to manually trigger notifications
   - Useful for testing during development
   - Keyboard shortcuts for quick testing

3. **ITEM_NOTIFICATION_SETUP.md** - Detailed setup guide
   - Step-by-step Unity setup instructions
   - Troubleshooting guide
   - Customization options

## Files Modified

1. **UIManager.cs**
   - Added ItemNotificationUI reference
   - Auto-finds ItemNotificationUI component
   - Provides centralized access via UIManager.Instance

2. **EquipmentManager.cs**
   - Added static events: OnItemEquipped, OnItemUnequipped
   - Enables notifications to listen for equipment changes

## Quick Start

### Unity Setup (5 minutes)

1. **Create UI Structure:**
   ```
   Canvas
   └── ItemNotificationPanel (with Vertical Layout Group)
       └── NotificationItem Prefab (create & save as prefab)
           ├── Icon (Image)
           ├── ItemName (TextMeshPro)
           ├── Quantity (TextMeshPro)
           └── Action (TextMeshPro)
   ```

2. **Add Component:**
   - Add `ItemNotificationUI` to your UI Manager GameObject
   - Assign references in inspector

3. **Position:**
   - Anchor ItemNotificationPanel to top-right
   - Set position: X=-20, Y=-20

4. **Test:**
   - Add `ItemNotificationExample` to any GameObject
   - Press '1' through '5' keys to test different notification types

### Usage in Code

**Automatic (already working):**
```csharp
// These automatically trigger notifications:
inventoryManager.AddItem(item, quantity);
inventoryManager.ConsumeItem(item);
equipmentManager.Equip(item);
equipmentManager.Unequip(slotType);
```

**Manual:**
```csharp
// Show custom notification
UIManager.Instance.ItemNotificationUI.ShowCustomNotification(
    "Item Name",
    itemSprite,
    quantity,
    NotificationType.Added
);
```

## Notification Types

| Type | Color | Use Case | Icon |
|------|-------|----------|------|
| Added | Green | Item picked up | + |
| Removed | Red | Item dropped/lost | - |
| Consumed | Blue | Item used | ✓ |
| Equipped | Gold | Item equipped | ⚔ |
| Unequipped | Gray | Item unequipped | ⚔ |

## Keyboard Test Shortcuts

When `ItemNotificationExample` is active:
- **1** - Test Added notification
- **2** - Test Removed notification
- **3** - Test Consumed notification
- **4** - Test Equipped notification
- **5** - Test Unequipped notification
- **N** - Test basic notification

## Customization Points

### In Unity Inspector:
- Display Duration (how long notification shows)
- Fade In/Out Duration (animation speed)
- Max Visible Notifications (stack limit)

### In Code:
- Colors: Edit `GetActionColor()` in ItemNotificationUI.cs
- Text: Edit `GetActionString()` in ItemNotificationUI.cs
- Animation: Edit `AnimateNotification()` coroutine
- Layout: Modify notification prefab structure

## Integration Notes

✅ **No changes needed to existing code!** 
The system automatically listens to:
- InventoryManager events
- EquipmentManager events

✅ **Works with existing systems:**
- TabbedInventoryUI
- EquipmentUI
- CraftingUI
- All item pickup/drop logic

## Common Issues & Solutions

**Notifications not showing?**
- Check ItemNotificationUI is enabled
- Verify prefab is assigned
- Check Canvas is active

**Wrong position?**
- Set anchor to Top Right
- Adjust position offset
- Check Canvas Scaler settings

**Events not firing?**
- Ensure managers exist in scene
- Check OnEnable is called
- Verify events are static

## Performance

- Uses object pooling-like approach (max limit)
- Coroutines for smooth animations
- Automatic cleanup after display
- Minimal GC allocation

## Next Steps

1. Follow ITEM_NOTIFICATION_SETUP.md for detailed Unity setup
2. Test with ItemNotificationExample script
3. Customize colors/timing to match your game's style
4. Remove ItemNotificationExample when done testing

## Support

For detailed setup instructions, see: `ITEM_NOTIFICATION_SETUP.md`

For code examples, see: `ItemNotificationExample.cs`

---

**Ready to use!** The system is fully integrated with your existing inventory and equipment systems.
