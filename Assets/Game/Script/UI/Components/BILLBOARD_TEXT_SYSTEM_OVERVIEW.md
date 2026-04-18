# Billboard Text System Overview

## Purpose

Provides world-space UI text that always faces the camera. Used for floating damage numbers, loot notifications, status indicators, and interaction prompts.

## Components

### `BillboardText` (`UI/Components/BillboardText.cs`)
Makes a World Space Canvas rotate to face the camera every `LateUpdate`. Attach to the Canvas root, not the text child.

**Camera resolution order:**
1. `overrideCamera` — if assigned in Inspector
2. Searches for `CinemachineCamera` instances → finds a `CinemachineBrain` → uses its Unity `Camera`
3. Falls back to `Camera.main`

No service registration required.

**Optional distance fade:** fades the canvas out between a configurable start and end distance from the camera.

### `FloatingNumber` (`UI/Components/FloatingNumber.cs`)
Animates a text element upward and fades it out over time. Designed for reuse — call `Show()` again to reuse the instance.

| Feature | Detail |
|---------|--------|
| Float animation | Rises upward over configurable duration |
| Fade curve | Position-based alpha fade via animation curve |
| Reusable | `Show(position, value, color)` resets the animation |
| Poolable | Designed to work with an object pool |

## Prefab Structure

```
Canvas (World Space)
├── BillboardText.cs    ← on the Canvas
├── CanvasGroup         ← optional, required for distance fade
└── TextMeshProUGUI     ← the visible text
```

## Usage

```csharp
// Floating number (damage, loot, etc.)
var floatingNum = Instantiate(damageNumberPrefab, hitPosition, Quaternion.identity);
floatingNum.Show(hitPosition, damage, Color.red);

// Static world label (always faces camera, doesn't animate)
// Just use BillboardText on a Canvas — no FloatingNumber needed
```

## Integration with GatheringInteractable

`GatheringInteractable` accepts a `FloatingNumber` prefab reference. On interaction complete it instantiates it above the gather point and calls `Show()`.

## Performance Notes

- Use object pooling for high-frequency floating numbers (damage, hits).
- Disable distance fade on BillboardTexts that don't need it — saves a distance calculation per frame.
- Limit concurrent floating numbers via a pool with a fixed capacity.
