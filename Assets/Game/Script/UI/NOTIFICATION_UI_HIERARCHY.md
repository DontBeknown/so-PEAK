# Visual UI Hierarchy Guide

## Item Notification UI Structure

```
Canvas (Screen Space - Overlay)
│
└── ItemNotificationPanel
    │   Component: RectTransform
    │   - Anchor: Top-Right
    │   - Pivot: (1, 1)
    │   - Position: X = -20, Y = -20
    │   - Width: 350
    │   - Height: Auto
    │
    │   Component: Vertical Layout Group
    │   - Child Alignment: Upper Right
    │   - Spacing: 10
    │   - Child Control Size: Width ☑, Height ☑
    │   - Child Force Expand: Width ☐, Height ☐
    │   - Padding: Top=20, Right=20
    │
    └── [NotificationItem Prefab] (Create once, save as prefab, remove from hierarchy)
        │   Component: RectTransform
        │   - Width: 300
        │   - Height: 60
        │
        │   Component: Horizontal Layout Group
        │   - Padding: Left=10, Right=10, Top=5, Bottom=5
        │   - Spacing: 10
        │   - Child Control Size: Width ☑, Height ☑
        │   - Child Force Expand: Width ☐, Height ☐
        │
        │   Component: CanvasGroup (Required for fade animation)
        │   - Alpha: 1
        │   - Interactable: ☑
        │   - Block Raycasts: ☑
        │
        │   Component: Image (Background)
        │   - Color: Dark semi-transparent (e.g., 0, 0, 0, 200)
        │   - Sprite: UI Sprite (rounded corners recommended)
        │
        ├── Icon (Image)
        │   │   Component: RectTransform
        │   │   - Width: 40
        │   │   - Height: 40
        │   │
        │   └── Component: Image
        │       - Preserve Aspect: ☑
        │       - Color: White
        │
        ├── ItemName (TextMeshProUGUI)
        │   │   Component: RectTransform
        │   │   - Flexible Width: Yes (Layout Element)
        │   │
        │   └── Component: TextMeshProUGUI
        │       - Font Size: 18
        │       - Color: White (255, 255, 255)
        │       - Font Style: Bold
        │       - Alignment: Middle Left
        │       - Text: "Item Name"
        │
        ├── Quantity (TextMeshProUGUI)
        │   │   Component: RectTransform
        │   │   - Width: 40 (preferred)
        │   │
        │   └── Component: TextMeshProUGUI
        │       - Font Size: 16
        │       - Color: Yellow (255, 220, 0)
        │       - Font Style: Bold
        │       - Alignment: Middle Center
        │       - Text: "x1"
        │
        └── Action (TextMeshProUGUI)
            │   Component: RectTransform
            │   - Width: 80 (preferred)
            │
            └── Component: TextMeshProUGUI
                - Font Size: 14
                - Color: Changes based on action type
                - Font Style: Regular
                - Alignment: Middle Right
                - Text: "+ Added"
```

## Color Scheme

### Background:
- Main Panel: Transparent or very subtle
- Notification Item: Dark semi-transparent (0, 0, 0, 200)

### Text Colors:
- Item Name: White (255, 255, 255)
- Quantity: Yellow (255, 220, 0)
- Action Text (dynamic):
  - Added: Green (76, 204, 76)
  - Removed: Red (204, 76, 76)
  - Consumed: Blue (76, 153, 230)
  - Equipped: Gold (230, 179, 51)
  - Unequipped: Gray (153, 153, 153)

## Layout Measurements

```
┌────────────────────────────────────┐
│  ItemNotificationPanel             │
│  (Top-Right Corner)                │
│  ┌──────────────────────────────┐  │
│  │ [Icon] ItemName    x3  +Added│  │ ← 60px height
│  └──────────────────────────────┘  │
│  ↓ 10px spacing                    │
│  ┌──────────────────────────────┐  │
│  │ [Icon] ItemName    x1  ✓Used │  │
│  └──────────────────────────────┘  │
│  ↓ 10px spacing                    │
│  ┌──────────────────────────────┐  │
│  │ [Icon] ItemName    x5  -Lost │  │
│  └──────────────────────────────┘  │
└────────────────────────────────────┘
         ↑ 300px width
```

## Important Settings

### Canvas Settings:
- Render Mode: Screen Space - Overlay
- Pixel Perfect: ☑ (optional, for crisp text)
- Canvas Scaler:
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1920x1080
  - Match: 0.5 (balance width/height)

### Layout Groups:
**ItemNotificationPanel:**
- Vertical Layout Group ensures notifications stack properly
- Child Force Expand must be OFF for proper sizing

**NotificationItem:**
- Horizontal Layout Group keeps icon, text, and action aligned
- CanvasGroup is REQUIRED for fade animations

## Testing Checklist

After setup, verify:
- ☐ NotificationItem prefab created and saved
- ☐ CanvasGroup component on NotificationItem
- ☐ All TextMeshPro components use proper names
- ☐ Icon image has Preserve Aspect enabled
- ☐ Vertical Layout Group on panel
- ☐ Horizontal Layout Group on item
- ☐ Anchor set to Top-Right
- ☐ ItemNotificationUI component configured
- ☐ All references assigned in inspector

## Animation Preview

```
Frame 0:    [Slide in from right, fade in]
            ──────────►
            
Frame 0.3s: [Fully visible, layout enabled]
            ████████████

Frame 3.3s: [Start fade up and fade out]
            ↑
            ████████████ (fading)
            
Frame 3.8s: [Destroyed]
            (empty)
```

## Tips

1. **Use TextMeshPro** instead of Legacy Text for better quality
2. **Test with different screen sizes** using Game view aspect ratios
3. **Add padding** to prevent notifications from touching screen edge
4. **Keep icon size consistent** (40x40 recommended)
5. **Use Layout Elements** on texts for flexible width
6. **Save prefab before testing** to avoid losing changes

## Common Visual Issues

**Notifications overlap:**
- Add Vertical Layout Group to container
- Set spacing to 10+

**Text is cut off:**
- Enable Layout Element on TextMeshPro
- Set Flexible Width to 1+

**Icons are stretched:**
- Enable "Preserve Aspect" on Image component
- Set fixed width/height (40x40)

**Fade animation not working:**
- Add CanvasGroup to NotificationItem
- Ensure CanvasGroup.alpha starts at 0

**Position is wrong:**
- Set anchor preset to Top-Right
- Adjust position offset values
- Check parent Canvas anchors

---

For code implementation details, see ItemNotificationUI.cs
For setup instructions, see ITEM_NOTIFICATION_SETUP.md
