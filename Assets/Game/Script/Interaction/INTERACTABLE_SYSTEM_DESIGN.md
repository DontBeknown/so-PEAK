# Interactable Object System Design

## Overview
A flexible, extensible interaction system that allows the player to press **E** to interact with various objects in the game world. This refactors the existing `ItemDetector` into a more generic `InteractionDetector` that can handle multiple types of interactions.

---

## System Architecture

### Core Components

#### 1. **IInteractable Interface**
The base contract for all interactable objects.

```csharp
public interface IInteractable
{
    // Display name shown in UI prompt (e.g., "Wooden Log", "Assessment Terminal")
    string InteractionPrompt { get; }
    
    // Optional custom interaction key text (default: "Press E to")
    string InteractionVerb { get; }
    
    // Can this object be interacted with right now?
    bool CanInteract { get; }
    
    // Visual feedback when player is near
    void OnHighlighted(bool highlighted);
    
    // Execute the interaction
    void Interact(PlayerControllerRefactored player);
    
    // Distance-based priority (for when multiple items overlap)
    float InteractionPriority { get; }
    
    // Transform position for distance calculations
    Transform GetTransform();
}
```

#### 2. **InteractionDetector** (Refactored from ItemDetector)
Detects and manages nearby interactable objects.

**Key Features:**
- Sphere-based detection using `Physics.OverlapSphere`
- Automatically finds nearest interactable
- Handles highlighting for visual feedback
- Triggers UI prompts when in range
- Supports multiple interactables with priority system

**Events:**
```csharp
public static event Action<IInteractable> OnNearestInteractableChanged;
public static event Action<bool> OnInteractableInRange;
```

**Public API:**
```csharp
public IInteractable NearestInteractable { get; }
public bool HasInteractableInRange { get; }
public bool TryInteractWithNearest();
public List<IInteractable> GetInteractablesInRange();
```

#### 3. **InteractionPromptUI**
UI overlay showing interaction prompt at bottom-center of screen.

**Features:**
- Shows/hides based on `InteractionDetector` events
- Displays custom prompt text from interactable
- Animated fade in/out with subtle pulse
- **Built-in progress bar** for gathering interactions
- Position: Bottom-center of screen

**Display Formats:**
```
Standard Interaction:
[F] Press E to Pick up Wooden Log
[F] Press E to Check Assessment Report

Gathering with Progress:
[F] Hold E to Gather Berries
[████████░░] 80%
```

**Progress Bar Mode:**
- Activated during timed gathering interactions
- Shows horizontal fill bar (0% → 100%)
- Displays percentage text
- Replaces world-space UI for cleaner experience
- Uses same UI container as prompt

---

## Interactable Types

### Type 1: **ItemInteractable** (Resource Collection)
Replaces current `ResourceCollector` interaction logic.

**Responsibilities:**
- Wraps existing `ResourceCollector` component
- Adds item to player inventory on interaction
- Plays pickup animation/sound
- Destroys or disables object after collection

**Properties:**
```csharp
- InteractionPrompt: "Pick up [ItemName]"
- InteractionVerb: "Press E to"
- CanInteract: ResourceCollector.CanBeCollected
```

**Interaction Flow:**
1. Player approaches item → Detector highlights it
2. UI shows "Press E to Pick up Wooden Log"
3. Player presses E → `ItemInteractable.Interact()` called
4. Adds item to inventory via `InventoryManager`
5. Plays pickup feedback (sound/particles)
6. Object is collected/destroyed

### Type 2: **AssessmentTerminalInteractable** (UI Trigger)
Opens the assessment report UI.

**Responsibilities:**
- Triggers `AssessmentReportUI` to generate and display report
- Can be attached to 3D objects (terminals, signs, etc.)
- Optionally pauses game or locks player input
- Can be reusable (interact multiple times)

**Properties:**
```csharp
- InteractionPrompt: "Check Assessment Report"
- InteractionVerb: "Press E to"
- CanInteract: true (always available)
```

**Interaction Flow:**
1. Player approaches terminal → Detector highlights it
2. UI shows "Press E to Check Assessment Report"
3. Player presses E → Opens assessment UI
4. (Optional) Locks player movement during UI
5. Player closes UI → Returns to gameplay

### Type 3: **GatheringInteractable** (Timed Collection)
Interactable with progress timer that locks player during gathering.

**Responsibilities:**
- Displays progress bar UI during gathering
- Requires player to HOLD E button continuously
- Locks player movement completely while holding E
- Cancels if player releases E button
- Cancels if player takes damage
- Can be multi-use (berries respawn) or single-use
- Plays gathering animation on player
- Adds item to inventory on completion

**Properties:**
```csharp
- InteractionPrompt: "Gather [ResourceName]"
- InteractionVerb: "Hold E to"
- CanInteract: !IsCurrentlyGathering && HasResourcesAvailable
- GatherDuration: float (e.g., 3.0f seconds)
- ResourcesPerGather: int (e.g., 3 berries)
- RespawnTime: float (for multi-use, e.g., 60.0f seconds)
```

**Interaction Flow:**
1. Player approaches berry bush → Detector highlights it
2. UI shows "Hold E to Gather Berries" at bottom
3. Player presses and HOLDS E → Gathering starts
4. Progress bar appears in prompt UI (0% → 100%)
5. Player locked in place while holding E (can't move, attack, or open inventory)
6. Timer counts up while E is held (e.g., 3 seconds)
7. If player releases E before completion → Gathering cancelled
8. On successful completion (held full duration):
   - Items added to inventory ("Collected 3 Berries")
   - Success sound/particles
   - Bush depletes or starts respawn timer
9. Player control returns

**Cancellation Cases:**
- Player releases E button → Gathering cancelled
- Player takes damage → Gathering interrupted
- Bush destroyed → Cancelled

**Note:** Player CANNOT move while holding E, so distance check is not needed

**Visual States:**
- **Ready**: Full berry bush, green UI marker
- **Gathering**: Progress bar in prompt UI, player animation playing
- **Depleted**: Empty bush, grey UI marker (if respawnable)
- **Respawning**: Gradually fills back up (optional visual)

**Examples:**
- Berry Bush (multi-use, respawns)
- Mining Ore Node (single-use, destroys after)
- Fishing Spot (multi-use, location-based)
- Herb Gathering (multi-use, respawns)

### Type 4: **GenericInteractable** (Extensible Base)
Abstract base class for quick custom interactables.

**Use Cases:**
- Door that opens on interaction
- Lever that triggers event
- NPC that shows dialogue
- Checkpoint marker
- Campfire that provides warmth

---

## Visual Feedback System

### Highlighting
Each interactable handles its own highlight effect via `OnHighlighted()`:

**Selected: UI Marker** ✅
- Floating sprite/icon above object
- Scales up when nearest (becomes primary target)
- Color: Yellow (in range) → Green (nearest/selected)
- Always faces camera (billboard effect)
- Smooth animations (fade in/out, scale pulse)
- Less intrusive than shader-based effects
- Easy to implement and customize per object type

### Audio Feedback
- **On Highlight**: Subtle "ping" sound
- **On Interact**: Context-specific sound (pickup, click, open, etc.)

---

## Integration Points

### 1. **PlayerControllerRefactored**
Minimal changes required:

```csharp
[Header("Interaction System")]
[SerializeField] private InteractionDetector interactionDetector;

private void InitializeInteraction()
{
    interactionDetector ??= GetComponent<InteractionDetector>();
}

// Updated input handler
private void HandleInteractInput() // Changed from HandlePickupInput
{
    interactionDetector?.TryInteractWithNearest();
}
```

### 2. **PlayerInputHandler**
Rename event from `OnPickupRequested` to `OnInteractRequested`:

```csharp
public event Action OnInteractRequested;

// In input action callback
private void OnInteract(InputValue value)
{
    OnInteractRequested?.Invoke();
}
```

### 3. **Existing ResourceCollector**
Add adapter component:

```csharp
// Add to existing ResourceCollector GameObjects
public class ResourceCollectorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ResourceCollector resourceCollector;
    
    public string InteractionPrompt => $"Pick up {resourceCollector.ResourceName}";
    // ... implement interface
}
```

---

## Configuration

### InteractionDetector Settings
```csharp
[Header("Detection Settings")]
[SerializeField] private float detectionRadius = 2.5f;
[SerializeField] private LayerMask interactableLayerMask;
[SerializeField] private Transform detectionCenter;
[SerializeField] private float updateInterval = 0.1f; // Optimize performance

[Header("Visual Feedback")]
[SerializeField] private bool enableGizmos = true;
[SerializeField] private Color gizmoColorWithTarget = Color.green;
[SerializeField] private Color gizmoColorNoTarget = Color.yellow;
```

### Layer Setup
Create new layer: **"Interactable"**
- Assign to all objects with IInteractable components
- Use in InteractionDetector's LayerMask

---

## File Structure

```
Assets/Game/Script/
├── Interaction/
│   ├── Core/
│   │   ├── IInteractable.cs
│   │   ├── InteractionDetector.cs
│   │   └── InteractionPromptUI.cs (includes progress bar)
│   │
│   ├── Interactables/
│   │   ├── ItemInteractable.cs
│   │   ├── AssessmentTerminalInteractable.cs
│   │   ├── GatheringInteractable.cs
│   │   ├── GenericInteractable.cs
│   │   └── ResourceCollectorInteractable.cs (adapter)
│   │
│   └── Utilities/
│       ├── InteractableUIMarker.cs (UI marker billboard)
│       └── InteractionAudioManager.cs
│
└── Player/
    ├── PlayerControllerRefactored.cs (minor updates)
    └── Services/
        └── PlayerInputHandler.cs (rename pickup → interact)
```

---

## Implementation Phases

### Phase 1: Core System ✅
1. Create `IInteractable` interface
2. Implement `InteractionDetector` (refactor from `ItemDetector`)
3. Create `InteractionPromptUI` with progress bar support
4. Update `PlayerControllerRefactored` integration
5. Update `PlayerInputHandler` for "interact" input

### Phase 2: Interactable Types ✅
1. Implement `ItemInteractable` (backwards compatible with existing items)
2. Implement `AssessmentTerminalInteractable`
3. Implement `GatheringInteractable` with integrated progress UI
4. Create adapter `ResourceCollectorInteractable`

### Phase 3: Polish & Feedback ✅
1. Add UI marker system with billboard effect
2. Add progress bar to prompt UI for gathering
3. Add audio feedback (gather start, loop, complete, cancel)
4. Add player gathering animation
5. Add interaction animations
6. Optimize detection performance (spatial hashing if needed)

### Phase 4: Testing & Migration 🔄
1. Test with existing ResourceCollector objects
2. Create assessment terminal prefab
3. Update existing items to use new system
4. Remove old `ItemDetector` (deprecated)

---

## Backwards Compatibility

To minimize disruption:

1. **Keep ItemDetector temporarily** - Mark as `[Obsolete]`
2. **Adapter Pattern** - Wrap existing ResourceCollector
3. **Gradual Migration** - New objects use IInteractable, old objects still work
4. **Same Input Key** - Still uses "E" key

---

## Example Usage Scenarios

### Scenario 1: Player Picks Up Wood
```
Player walks near wooden log
→ InteractionDetector finds ItemInteractable
→ Log gets highlighted outline
→ UI shows "Press E to Pick up Wooden Log"
→ Player presses E
→ Wood added to inventory
→ Pickup sound plays
→ Log disappears
→ UI prompt fades out
```

### Scenario 2: Player Checks Assessment
```
Player approaches stone terminal
→ InteractionDetector finds AssessmentTerminalInteractable
→ Terminal emits glow effect
→ UI shows "Press E to Check Assessment Report"
→ Player presses E
→ Assessment UI opens with stats
→ Player movement locked (optional)
→ Player closes UI
→ Returns to normal gameplay
```

### Scenario 3: Multiple Items Overlap
```
Player stands between log and terminal
→ InteractionDetector finds both
→ Calculates distances
→ Nearest item (log) becomes primary
→ Log's UI marker turns green + scales up
→ Terminal's UI marker stays yellow + smaller
→ UI prompt shows log interaction
→ Player moves → terminal becomes nearest
→ Markers switch (terminal green, log yellow)
→ UI prompt updates automatically
```

### Scenario 4: Player Gathers Berries Successfully
```
Player approaches berry bush
→ InteractionDetector finds GatheringInteractable
→ Bush shows green UI marker above it
→ UI prompt shows "Hold E to Gather Berries" at bottom
→ Player presses and HOLDS E button down
→ Gathering starts, progress bar appears in prompt UI
→ Player frozen in place (cannot move while holding E)
→ Gathering animation plays on player
→ Progress bar fills: 0% → 25% → 50% → 75% → 100%
→ Player keeps E held for full 3 seconds
→ On 100%: "Collected 3 Berries" notification
→ Bush depletes (marker turns grey)
→ Player releases E, control returns
→ Bush starts 60s respawn timer
```

### Scenario 5: Player Releases E Button Early
```
Player starts gathering berries
→ Holds E, progress bar at 50%
→ Player releases E button
→ Gathering cancelled immediately
→ Progress bar disappears from prompt UI
→ "Gathering cancelled!" message
→ Player control returns (can move again)
→ No items collected
→ Bush remains ready to gather again
```

### Scenario 6: Gathering Interrupted by Damage
```
Player starts gathering berries (70% complete)
→ Still holding E button, progress bar showing
→ Enemy attacks player from behind
→ Player takes damage
→ Gathering interrupted immediately
→ Progress bar disappears from prompt UI
→ "Gathering interrupted!" message
→ Player control returns
→ No items collected
→ Bush remains ready to gather
```

---

## Extension Examples

### Future Interactables
- **ChestInteractable** - Opens container UI
- **CampfireInteractable** - Rest/Cook menu
- **NPCInteractable** - Dialogue system
- **DoorInteractable** - Open/Close with animation
- **LeverInteractable** - Puzzle mechanic
- **CheckpointInteractable** - Save game state
- **FishingSpotInteractable** - Timed gathering with mini-game
- **TreeChoppingInteractable** - Multi-stage gathering (requires axe)

---

## Questions for Review

1. **Should the InteractionDetector replace ItemDetector completely, or coexist temporarily?**
   - Recommendation: Coexist initially, deprecate after migration

2. **Should we lock player input when Assessment UI is open?**
   - Recommendation: Yes, call `SetInputBlocked(true)` on UI open

3. **~~Highlight effect preference?~~** ✅ **DECIDED: UI Marker**
   - Using billboard UI markers (yellow → green when nearest)
   - Simple, clear, and non-intrusive

4. **Should we add interaction cooldowns?**
   - Could prevent spam-clicking on terminals
   - Recommendation: Optional per-interactable

5. **Priority system when multiple items overlap?**
   - Current: Nearest by distance
   - Alternative: Custom priority weights (items > terminals)

---

## Benefits of This Design

✅ **Extensible** - Easy to add new interactable types  
✅ **SOLID Principles** - Clean interfaces, single responsibility  
✅ **Backwards Compatible** - Works with existing code  
✅ **Type-Safe** - No string comparisons or tags  
✅ **Performant** - Layer-based detection, optimized updates  
✅ **User-Friendly** - Clear UI prompts and feedback  
✅ **Maintainable** - Organized file structure  

---

## Ready for Implementation?

Please review this design and let me know:
- Any changes or additions you'd like
- Which highlight effect you prefer
- Whether to lock input during Assessment UI
- Any other interaction types you want to add initially

Once approved, I'll implement the complete system! 🚀
