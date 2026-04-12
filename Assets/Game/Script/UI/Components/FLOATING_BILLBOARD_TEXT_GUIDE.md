# Floating Billboard Text Setup Guide

## Overview
This system allows you to create floating text in the world that always faces the camera, perfect for:
- Floating damage numbers
- Loot drop notifications
- Status indicators
- Quest markers
- Interactive prompts

## Architecture

### Files Created
1. **BillboardText** (`UI/Components/BillboardText.cs`)
   - Core component that makes UI always face camera
   - Features:
      - Automatic camera detection via Cinemachine + Camera.main fallback
     - Optional distance-based fade
     - Debug logging support

2. **FloatingNumber** (`UI/Components/FloatingNumber.cs`)
   - Example component for floating damage/notification numbers
   - Features:
     - Upward float animation
     - Fade out over time
     - Customizable animation curves
     - Reusable/poolable design

## Setup Steps

### Step 1: Camera Resolution Behavior
`BillboardText` resolves camera in this order:
1. `overrideCamera` (if assigned in Inspector)
2. Searches `FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None)`
3. If Cinemachine cameras exist, finds a `CinemachineBrain` and uses its Unity `Camera`
4. Falls back to `Camera.main`

No bootstrap registration is required.

### Step 2: Create a World Canvas with Floating Text

1. **Create Canvas:**
   - In Hierarchy: Right-click → UI → Canvas
   - Select the Canvas
   - In Inspector → Canvas component → Render Mode: **World Space**
   - Adjust canvas size to desired world size (e.g., 100 x 100)

2. **Add Text:**
   - Right-click Canvas → 3D Objects → TextMeshPro - Text
   - Adjust text size and styling as needed
   - Position relative to the canvas origin

3. **Add BillboardText:**
   - Select the Canvas (not the text)
   - Add Component → BillboardText
   - Configure:
    - Optional: assign **Override Camera** if you want a fixed camera source
     - ✅ **Enable Distance Fade**: Check (optional)
     - Fade Start Distance: 50m
     - Fade End Distance: 100m

### Step 3: Use in Code

#### Simple Floating Text
```csharp
// In your game code
public class DamageSystem : MonoBehaviour
{
    [SerializeField] private Canvas floatingTextPrefab; // Prefab with BillboardText
    
    public void ShowDamage(Vector3 position, float damage)
    {
        var canvas = Instantiate(floatingTextPrefab, position, Quaternion.identity);
        var text = canvas.GetComponentInChildren<TextMeshProUGUI>();
        text.text = damage.ToString("F0");
    }
}
```

#### Using FloatingNumber Component
```csharp
public class HealthComponent : MonoBehaviour
{
    [SerializeField] private FloatingNumber damageNumberPrefab;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    
    public void TakeDamage(float amount)
    {
        // ... damage logic ...
        
        // Show floating number
        var floatingNum = Instantiate(damageNumberPrefab, transform.position, Quaternion.identity);
        floatingNum.Show(transform.position, amount, damageColor);
    }
    
    public void Heal(float amount)
    {
        // ... heal logic ...
        
        var floatingNum = Instantiate(damageNumberPrefab, transform.position, Quaternion.identity);
        floatingNum.Show(transform.position, "+" + amount, healColor);
    }
}
```

## Features

### BillboardText Component
- **Automatic Billboard:** Rotates to face camera every frame (LateUpdate)
- **Camera Detection:** 
    - Manual override via Inspector (`overrideCamera`)
    - Auto-detect via Cinemachine camera search
    - Fallback to `Camera.main`
- **Distance Fade:** Automatically fades out based on distance from camera
- **Debug Logging:** Toggle debug logs to verify camera detection

### FloatingNumber Component
- **Float Animation:** Rises upward over time
- **Fade Curve:** Position-based alpha fade
- **Reusable:** Design for pooling/reuse (just call Show() again)
- **Customizable:** All animation durations and heights are serialized

## Common Use Cases

### Damage Numbers
```csharp
var floatingNum = Instantiate(damageNumberPrefab, hitPosition, Quaternion.identity);
floatingNum.Show(hitPosition, damage, Color.red);
```

### Healing Numbers
```csharp
var floatingNum = Instantiate(damageNumberPrefab, healPosition, Quaternion.identity);
floatingNum.Show(healPosition, "+" + healAmount, Color.green);
```

### Static Floating Labels
```csharp
// No FloatingNumber - just use BillboardText
// Creates a label that always faces camera but stays in one place
```

### Floating Damage Pool (Optimization)
```csharp
public class FloatingNumberPool : MonoBehaviour
{
    private Queue<FloatingNumber> _available = new();
    [SerializeField] private FloatingNumber prefab;
    [SerializeField] private int poolSize = 20;
    
    private void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }
    }
    
    public void ShowNumber(Vector3 position, float value, Color color)
    {
        if (!_available.TryDequeue(out var floatingNum))
        {
            floatingNum = Instantiate(prefab, transform);
        }
        
        floatingNum.Show(position, value, color);
        StartCoroutine(ReturnToPool(floatingNum, 2.5f)); // Duration + delay
    }
    
    private IEnumerator ReturnToPool(FloatingNumber num, float delay)
    {
        yield return new WaitForSeconds(delay);
        num.gameObject.SetActive(false);
        _available.Enqueue(num);
    }
}
```

## Prefab Structure Example

```
Canvas (WorldSpace)
├── BillboardText.cs
├── CanvasGroup (optional, for fade)
└── Text (TextMeshProUGUI)
```

## Troubleshooting

### Text Isn't Facing Camera
- [ ] Verify BillboardText is on the **Canvas**, not the Text child
- [ ] If using a specific camera, assign **Override Camera**
- [ ] Ensure there is at least one `CinemachineCamera` and one `CinemachineBrain` in scene
- [ ] Check debug logs: Enable "Debug Logs" on BillboardText
- [ ] Verify Camera.main is set in the scene

### Text Is Fading Immediately
- [ ] Disable "Enable Distance Fade" if not needed
- [ ] Adjust "Fade Start Distance" to be further away
- [ ] Check camera position is far enough from text

### NullReferenceException in BillboardText
- [ ] Ensure CanvasGroup is not required (it's optional for non-fading setups)
- [ ] Ensure either override camera, Cinemachine brain camera, or Camera.main exists

### Text Not Positioned Correctly
- [ ] Ensure Canvas RenderMode is **World Space** (not Overlay or Screen Space)
- [ ] Check RectTransform positioning on Text child
- [ ] Verify World Canvas scale/positioning

## Performance Tips

1. **Use Object Pooling** for frequently-spawned floating numbers
2. **Disable Distance Fade** if not needed (saves distance calculations)
3. **Batch Text Updates** - Wait a frame before showing numbers
4. **Use Simple Text Shaders** - Avoid complex materials on world UI
5. **Limit Concurrent Floating Numbers** - Use pools or destroy after duration

## Integration with GatheringInteractable

To show resource drop notifications when gathering:

```csharp
public class GatheringInteractable : HoldInteractableBase
{
    [SerializeField] private FloatingNumber interactionNotificationPrefab;
    
    protected override void OnInteractionComplete()
    {
        base.OnInteractionComplete();
        
        // Show floating text at gathering location
        if (interactionNotificationPrefab != null)
        {
            var floatingText = Instantiate(
                interactionNotificationPrefab, 
                transform.position + Vector3.up * 2, 
                Quaternion.identity
            );
            floatingText.Show(transform.position, "Gathered!", Color.yellow);
        }
    }
}
```

## Next Steps

1. Create a Canvas prefab with BillboardText configured
2. Test with a simple floating number in your scene
3. Implement pooling for performance-critical scenarios
4. Integrate with your damage/interaction systems
5. Customize animations and fade curves for your design
