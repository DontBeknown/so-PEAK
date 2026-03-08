# Interaction System Overview

**Location:** `Assets/Game/Script/Interaction/`  
**Last Updated:** February 16, 2026

---

## What is This System?

The **Interaction System** provides a flexible, priority-based interaction framework with:
- **Detection** - Automatic detection of nearby interactables
- **Highlighting** - Visual feedback for current target
- **Priority System** - Multiple interactables ranked by priority
- **Template Pattern** - Hold-to-interact base class
- **Prompt Display** - UI feedback ("Press E to interact")
- **Progress Tracking** - Progress bar for long interactions

This system uses **Observer Pattern** and **Template Method Pattern** for extensibility.

---

## Key Components

### 1. IInteractable Interface

**File:** `Interaction/IInteractable.cs`

**Purpose:** Contract for all interactable objects.

```csharp
public interface IInteractable
{
    // Properties
    bool CanInteract { get; }
    string InteractionPrompt { get; }
    string InteractionVerb { get; } // "Press", "Hold"
    float InteractionPriority { get; } // Higher = more important
    Transform GetTransform();
    
    // Methods
    void OnHighlighted(bool highlighted);
    void Interact(Game.Player.PlayerControllerRefactored player);
}
```

**Priority Levels:**
- **1.0** - Standard pickup (ItemInteractable)
- **1.2** - Resource gathering (GatheringInteractable)
- **1.5** - Important NPCs
- **2.0** - Quest objectives

### 2. InteractionDetector

**File:** `Interaction/InteractionDetector.cs`

**Purpose:** Detects nearby interactables and manages current target.

**Pattern:** Observer

**Configuration:**
```csharp
[Header("Detection Settings")]
[SerializeField] private float detectionRadius = 3f;
[SerializeField] private LayerMask interactableLayer;
[SerializeField] private float detectionInterval = 0.1f; // Check every 0.1s

[Header("References")]
[SerializeField] private Transform playerTransform;
```

**Events:**
```csharp
public event Action<IInteractable> OnInteractableInRange;
public event Action OnNoInteractableInRange;
```

**Key Methods:**
```csharp
private void Update()
{
    detectionTimer += Time.deltaTime;
    
    if (detectionTimer >= detectionInterval)
    {
        DetectInteractables();
        detectionTimer = 0f;
    }
}

private void DetectInteractables()
{
    // Find all colliders in range
    Collider[] colliders = Physics.OverlapSphere(
        playerTransform.position,
        detectionRadius,
        interactableLayer
    );
    
    // Get all IInteractable components
    List<IInteractable> interactables = new List<IInteractable>();
    foreach (var collider in colliders)
    {
        var interactable = collider.GetComponent<IInteractable>();
        if (interactable != null && interactable.CanInteract)
        {
            interactables.Add(interactable);
        }
    }
    
    // Get highest priority
    IInteractable target = GetHighestPriorityInteractable(interactables);
    
    // Update current target
    UpdateCurrentTarget(target);
}

private IInteractable GetHighestPriorityInteractable(List<IInteractable> interactables)
{
    if (interactables.Count == 0) return null;
    
    IInteractable highest = interactables[0];
    foreach (var interactable in interactables)
    {
        if (interactable.InteractionPriority > highest.InteractionPriority)
        {
            highest = interactable;
        }
    }
    
    return highest;
}

private void UpdateCurrentTarget(IInteractable newTarget)
{
    if (currentTarget == newTarget) return;
    
    // Unhighlight old
    if (currentTarget != null)
    {
        currentTarget.OnHighlighted(false);
    }
    
    // Highlight new
    currentTarget = newTarget;
    if (currentTarget != null)
    {
        currentTarget.OnHighlighted(true);
        OnInteractableInRange?.Invoke(currentTarget);
    }
    else
    {
        OnNoInteractableInRange?.Invoke();
    }
}
```

### 3. ItemInteractable

**File:** `Interaction/ItemInteractable.cs`

**Purpose:** Simple instant pickup.

**Implementation:**
```csharp
public class ItemInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryItem item;
    [SerializeField] private int quantity = 1;
    [SerializeField] private GameObject visualPrefab;
    
    public bool CanInteract => true;
    public string InteractionPrompt => $"Pick up {item.itemName}";
    public string InteractionVerb => "Press to";
    public float InteractionPriority => 1.0f;
    public Transform GetTransform() => transform;
    
    public void OnHighlighted(bool highlighted)
    {
        if (visualPrefab != null)
        {
            // Apply outline shader or emission
            var renderer = visualPrefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (highlighted)
                    renderer.material.EnableKeyword("_EMISSION");
                else
                    renderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
    
    public void Interact(PlayerControllerRefactored player)
    {
        // Add to inventory
        var inventoryService = ServiceContainer.Instance.TryGet<IInventoryService>();
        if (inventoryService != null)
        {
            bool added = inventoryService.AddItem(item, quantity);
            
            if (added)
            {
                // Success - destroy pickup
                Destroy(gameObject);
            }
            else
            {
                // Inventory full
                Debug.Log("Inventory full!");
            }
        }
    }
}
```

### 4. HoldInteractableBase

**File:** `Interaction/HoldInteractableBase.cs`

**Purpose:** Abstract base class for hold-to-interact objects.

**Pattern:** Template Method

**Features:**
- Duration-based interaction
- Progress bar integration
- Cancellable (release button)
- Locks player movement during interaction

**Template Methods:**
```csharp
public abstract class HoldInteractableBase : MonoBehaviour, IInteractable
{
    [Header("Hold Settings")]
    [SerializeField] protected float interactionDuration = 3f;
    [SerializeField] protected bool lockPlayerDuringInteraction = true;
    
    // Abstract methods (must override)
    protected abstract string GetInteractionPrompt();
    protected abstract void OnInteractionComplete(PlayerControllerRefactored player);
    protected abstract void OnInteractionCancelled();
    
    // Template method
    public void Interact(PlayerControllerRefactored player)
    {
        if (isInteracting) return;
        
        StartCoroutine(InteractionCoroutine(player));
    }
    
    private IEnumerator InteractionCoroutine(PlayerControllerRefactored player)
    {
        isInteracting = true;
        float elapsed = 0f;
        
        // Lock player
        if (lockPlayerDuringInteraction)
        {
            player.SetInputBlocked(true);
        }
        
        // Show progress bar
        var promptUI = ServiceContainer.Instance.TryGet<InteractionPromptUI>();
        promptUI?.ShowProgressBar();
        
        // Track progress
        while (elapsed < interactionDuration)
        {
            // Check if still holding button
            if (!IsButtonHeld())
            {
                OnInteractionCancelled();
                Cleanup(player, promptUI);
                yield break;
            }
            
            elapsed += Time.deltaTime;
            float progress = elapsed / interactionDuration;
            
            // Update progress bar
            promptUI?.UpdateProgress(progress);
            
            yield return null;
        }
        
        // Complete
        OnInteractionComplete(player);
        Cleanup(player, promptUI);
    }
    
    private void Cleanup(PlayerControllerRefactored player, InteractionPromptUI promptUI)
    {
        isInteracting = false;
        
        // Unlock player
        if (lockPlayerDuringInteraction)
        {
            player.SetInputBlocked(false);
        }
        
        // Hide progress bar
        promptUI?.HideProgressBar();
    }
    
    private bool IsButtonHeld()
    {
        // Check if E key still held
        return Input.GetKey(KeyCode.E);
    }
}
```

### 5. GatheringInteractable

**File:** `Interaction/GatheringInteractable.cs`

**Purpose:** Resource gathering (berry bush, ore node, tree/twig). Can combine with `ScaleDownDestroyAnimation.cs` to animate scaling out when destroyed.

**Extends:** HoldInteractableBase

**Implementation:**
```csharp
public class GatheringInteractable : HoldInteractableBase
{
    [Header("Resource Settings")]
    [SerializeField] private InventoryItem resourceItem;
    [SerializeField] private int minYield = 1;
    [SerializeField] private int maxYield = 3;
    [SerializeField] private bool canRespawn = true;
    [SerializeField] private float respawnTime = 60f;
    
    [Header("Visuals")]
    [SerializeField] private GameObject resourceVisual;
    
    private bool isDepleted = false;
    
    public bool CanInteract => !isDepleted && !isInteracting;
    public string InteractionPrompt => GetInteractionPrompt();
    public string InteractionVerb => "Hold to";
    public float InteractionPriority => 1.2f;
    
    protected override string GetInteractionPrompt()
    {
        return $"Gather {resourceItem.itemName}";
    }
    
    protected override void OnInteractionComplete(PlayerControllerRefactored player)
    {
        // Give resources
        int yield = Random.Range(minYield, maxYield + 1);
        
        var inventoryService = ServiceContainer.Instance.TryGet<IInventoryService>();
        if (inventoryService != null)
        {
            inventoryService.AddItem(resourceItem, yield);
        }
        
        // Deplete
        isDepleted = true;
        if (resourceVisual != null)
        {
            resourceVisual.SetActive(false);
        }
        
        // Schedule respawn
        if (canRespawn)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }
    
    protected override void OnInteractionCancelled()
    {
        Debug.Log("Gathering cancelled");
    }
    
    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);
        
        isDepleted = false;
        if (resourceVisual != null)
        {
            resourceVisual.SetActive(true);
        }
    }
}
```

### 6. WaterSourceInteractable

**File:** `Interaction/WaterSourceInteractable.cs`

**Purpose:** Refill canteen at water source.

**Extends:** HoldInteractableBase

**Implementation:**
```csharp
public class WaterSourceInteractable : HoldInteractableBase
{
    public bool CanInteract => GetEquippedCanteen() != null && !GetEquippedCanteen().IsFull();
    public string InteractionPrompt => GetInteractionPrompt();
    public string InteractionVerb => "Hold to";
    public float InteractionPriority => 1.2f;
    
    protected override string GetInteractionPrompt()
    {
        var canteen = GetEquippedCanteen();
        
        if (canteen == null)
        {
            // Check inventory
            var inventoryManager = ServiceContainer.Instance.TryGet<InventoryManagerRefactored>();
            if (inventoryManager != null && inventoryManager.HasItem(canteenItemReference))
            {
                return "Equip Canteen to Refill";
            }
            return "No Canteen";
        }
        
        if (canteen.IsFull())
        {
            return "Canteen Full";
        }
        
        return "Refill Canteen";
    }
    
    protected override void OnInteractionComplete(PlayerControllerRefactored player)
    {
        var canteen = GetEquippedCanteen();
        if (canteen != null)
        {
            canteen.Refill();
            
            // Play refill sound
            if (refillSound != null)
            {
                AudioSource.PlayClipAtPoint(refillSound, transform.position);
            }
        }
    }
    
    protected override void OnInteractionCancelled()
    {
        Debug.Log("Refilling cancelled");
    }
    
    private CanteenItem GetEquippedCanteen()
    {
        var equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
        if (equipmentManager != null)
        {
            var equipped = equipmentManager.GetEquippedItem(EquipmentSlotType.HeldItem);
            return equipped as CanteenItem;
        }
        return null;
    }
}
```

---

## How It Works in Game

### Detection Flow

```
Every 0.1 seconds:
│
▼
InteractionDetector.DetectInteractables()
│
├─► Physics.OverlapSphere(player position, radius, layer)
│   └─► Returns Collider[] nearby
│
├─► Filter for IInteractable components
│   └─► Only include if CanInteract == true
│
├─► GetHighestPriorityInteractable(list)
│   └─► Compare InteractionPriority values
│
└─► UpdateCurrentTarget(newTarget)
    │
    ├─► If target changed:
    │   ├─► oldTarget.OnHighlighted(false)
    │   ├─► newTarget.OnHighlighted(true)
    │   └─► Fire OnInteractableInRange event
    │
    └─► If no target:
        └─► Fire OnNoInteractableInRange event
```

### Interaction Flow (Instant)

```
1. Player approaches ItemInteractable
   │
   ▼
2. InteractionDetector detects it
   │
   ├─► OnHighlighted(true) called
   │   └─► Visual feedback (outline/glow)
   │
   └─► InteractionPromptUI shows "Press E to Pick up Berry"
   │
   ▼
3. Player presses E
   │
   ▼
4. ItemInteractable.Interact(player)
   │
   ├─► IInventoryService.AddItem(item, quantity)
   │   │
   │   ├─► Success: EventBus.Publish(ItemAddedEvent)
   │   └─► Failure: Show "Inventory Full" message
   │
   └─► If success: Destroy(gameObject)
```

### Interaction Flow (Hold)

```
1. Player approaches GatheringInteractable
   │
   ▼
2. InteractionDetector detects it
   │
   └─► InteractionPromptUI shows "Hold E to Gather Wood"
   │
   ▼
3. Player holds E
   │
   ▼
4. GatheringInteractable.Interact(player)
   │
   ├─► StartCoroutine(InteractionCoroutine(player))
   │   │
   │   ├─► Lock player movement (optional)
   │   ├─► Show progress bar
   │   │
   │   ├─► Loop for duration (3 seconds):
   │   │   ├─► Check if E still held
   │   │   ├─► Update elapsed time
   │   │   ├─► Update progress bar (0.0 → 1.0)
   │   │   └─► If released: Cancel & cleanup
   │   │
   │   ├─► On complete:
   │   │   ├─► OnInteractionComplete(player)
   │   │   │   ├─► Roll random yield (1-3)
   │   │   │   ├─► AddItem(resource, yield)
   │   │   │   ├─► Mark as depleted
   │   │   │   └─► Start respawn timer
   │   │   │
   │   │   └─► Cleanup()
   │   │       ├─► Unlock player
   │   │       └─► Hide progress bar
   │   │
   │   └─► If cancelled:
   │       └─► OnInteractionCancelled()
   │
   └─► InteractionPromptUI hidden
```

---

## How to Use

### Creating Simple Interactable

```csharp
public class CustomInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteract => true;
    public string InteractionPrompt => "Interact with custom object";
    public string InteractionVerb => "Press to";
    public float InteractionPriority => 1.0f;
    public Transform GetTransform() => transform;
    
    public void OnHighlighted(bool highlighted)
    {
        // Visual feedback
        if (highlighted)
            Debug.Log("Highlighted!");
    }
    
    public void Interact(PlayerControllerRefactored player)
    {
        Debug.Log("Interacted!");
        // Your custom logic here
    }
}
```

### Creating Hold Interactable

```csharp
public class CustomHoldInteractable : HoldInteractableBase
{
    public bool CanInteract => true; // Add your conditions
    public float InteractionPriority => 1.2f;
    
    protected override string GetInteractionPrompt()
    {
        return "Do something cool";
    }
    
    protected override void OnInteractionComplete(PlayerControllerRefactored player)
    {
        Debug.Log("Interaction completed!");
        // Give reward, trigger event, etc.
    }
    
    protected override void OnInteractionCancelled()
    {
        Debug.Log("Cancelled!");
    }
    
    public void OnHighlighted(bool highlighted)
    {
        // Visual feedback
    }
}
```

### Setup in Scene

1. **Add InteractionDetector to player:**
```
Player
└── InteractionDetector
    • Detection Radius: 3
    • Interactable Layer: Interactable (custom layer)
    • Player Transform: (drag player transform)
```

2. **Create interactable object:**
```
BerryBush
├── Mesh/Sprite
├── Collider (on "Interactable" layer)
└── GatheringInteractable component
    • Resource Item: Berry ScriptableObject
    • Min Yield: 1
    • Max Yield: 3
    • Interaction Duration: 2
```

3. **Wire up to UI:**
```csharp
// In InteractionPromptUI
private void Start()
{
    var detector = FindFirstObjectByType<InteractionDetector>();
    if (detector != null)
    {
        detector.OnInteractableInRange += ShowPrompt;
        detector.OnNoInteractableInRange += HidePrompt;
    }
}

private void ShowPrompt(IInteractable interactable)
{
    promptText.text = $"{interactable.InteractionVerb} {interactable.InteractionPrompt}";
    promptPanel.SetActive(true);
}
```

---

## How to Expand

### Adding Conditional Interactions

```csharp
public class QuestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string requiredQuestId;
    
    public bool CanInteract => HasCompletedQuest();
    public string InteractionPrompt => 
        CanInteract ? "Collect reward" : "Complete quest first";
    
    private bool HasCompletedQuest()
    {
        var questManager = ServiceContainer.Instance.TryGet<QuestManager>();
        return questManager?.IsQuestCompleted(requiredQuestId) ?? false;
    }
    
    public void Interact(PlayerControllerRefactored player)
    {
        if (CanInteract)
        {
            GiveReward(player);
        }
    }
}
```

### Adding Multi-Stage Interactions

```csharp
public class MultiStageInteractable : HoldInteractableBase
{
    private enum Stage { First, Second, Third }
    private Stage currentStage = Stage.First;
    
    protected override string GetInteractionPrompt()
    {
        return currentStage switch
        {
            Stage.First => "Start process",
            Stage.Second => "Continue process",
            Stage.Third => "Finish process",
            _ => "Unknown"
        };
    }
    
    protected override void OnInteractionComplete(PlayerControllerRefactored player)
    {
        switch (currentStage)
        {
            case Stage.First:
                currentStage = Stage.Second;
                break;
            case Stage.Second:
                currentStage = Stage.Third;
                break;
            case Stage.Third:
                CompleteProcess();
                break;
        }
    }
}
```

---

## Best Practices

### ✅ DO
- Use priority system for overlapping interactables
- Provide clear visual feedback on highlight
- Lock player movement for immersive interactions
- Allow cancellation of hold interactions
- Use appropriate interaction duration (2-5 seconds)
- Show progress bar for hold interactions
- Place interactables on dedicated layer

### ❌ DON'T
- Don't use Update() for detection (use timer)
- Don't forget to set CanInteract conditions
- Don't block input without feedback
- Don't make interactions too long (>10s)
- Don't forget to cleanup coroutines
- Don't use Find operations in Interact()

---

## File Structure

```
Interaction/
├── IInteractable.cs                   # Interface
├── InteractionDetector.cs             # Detection system
├── HoldInteractableBase.cs            # Template base class
│
├── ItemInteractable.cs                # Instant pickup
├── GatheringInteractable.cs           # Resource gathering
├── WaterSourceInteractable.cs         # Canteen refill
└── DoorInteractable.cs                # Door open/close
```

---

## Integration Points

### With Player System
- InteractionDetector attached to player
- Player input triggers interactions

### With Inventory System
- ItemInteractable adds items
- GatheringInteractable gives resources

### With UI System
- InteractionPromptUI displays prompts
- Progress bar for hold interactions

---

## Related Documentation

- [PLAYER_SYSTEM_OVERVIEW.md](../Player/PLAYER_SYSTEM_OVERVIEW.md) - Player controller
- [INVENTORY_SYSTEM_OVERVIEW.md](../Player/Inventory/INVENTORY_SYSTEM_OVERVIEW.md) - Item system
- [INTERACTABLE_SYSTEM_DESIGN.md](INTERACTABLE_SYSTEM_DESIGN.md) - Detailed design

---

**Last Updated:** February 16, 2026  
**System Status:** ✅ Production Ready  
**Architecture Quality:** ⭐⭐⭐⭐⭐ (Template pattern, priority system, extensible)
