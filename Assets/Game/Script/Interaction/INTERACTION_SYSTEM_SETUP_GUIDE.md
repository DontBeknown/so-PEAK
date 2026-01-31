# Interactable System Setup Guide

Complete step-by-step guide to implement the interaction system in your Unity project.

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Unity Project Setup](#unity-project-setup)
3. [Player Setup](#player-setup)
4. [UI Setup](#ui-setup)
5. [Creating Interactables](#creating-interactables)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Packages
- ✅ **TextMeshPro** - For UI text
- ✅ **DOTween** - For smooth animations
  - Download from Asset Store: [DOTween (free)](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
  - Or via Package Manager if available
- ✅ **Input System** - Already in your project

### Required Scripts
All scripts are located in `Assets/Game/Script/Interaction/`:
- Core system files (already created)
- Interactable types (already created)
- Utilities (already created)

---

## Unity Project Setup

### Step 1: Create Interactable Layer

1. Open **Edit → Project Settings → Tags and Layers**
2. Find an empty **Layer** slot (e.g., Layer 8)
3. Name it: `Interactable`
4. Click **Save**

### Step 2: Import DOTween (if not already)

1. Download DOTween from Asset Store
2. Import into project
3. When prompted, click **Setup DOTween**
4. Accept default settings

---

## Player Setup

### Step 3: Add InteractionDetector to Player

1. Select your **Player** GameObject in hierarchy
2. Click **Add Component**
3. Search for `InteractionDetector`
4. Configure settings:

```
Detection Settings:
├─ Detection Radius: 2.5
├─ Interactable Layer Mask: Select "Interactable" layer
├─ Detection Center: (leave empty to use player transform)
└─ Update Interval: 0.1

Visual Feedback:
├─ Enable Gizmos: ✓ (check for debugging)
├─ Gizmo Color With Target: Green
└─ Gizmo Color No Target: Yellow
```

### Step 4: Verify PlayerControllerRefactored

Your `PlayerControllerRefactored` should already have:
- ✅ `interactionDetector` field (serialized)
- ✅ `InitializeInteraction()` method
- ✅ `HandleInteractInput()` method

**In Inspector:**
- The `InteractionDetector` field should auto-assign
- If not, drag the InteractionDetector component to the field

---

## UI Setup

### Step 5: Create Interaction Prompt UI

1. **Create Canvas** (if you don't have one)
   - Right-click Hierarchy → **UI → Canvas**
   - Name it: `InteractionUI`
   - Set **Render Mode**: Screen Space - Overlay
   - Set **Canvas Scaler**: Scale With Screen Size
   - Reference Resolution: 1920x1080

2. **Create Prompt Container**
   - Right-click Canvas → **UI → Empty**
   - Name: `InteractionPrompt`
   - Anchor: Bottom-Center
   - Position: X=0, Y=150, Z=0
   - Width: 600, Height: 100

3. **Add CanvasGroup** (for fading)
   - Select `InteractionPrompt`
   - Add Component → **Canvas Group**

4. **Create Prompt Text**
   - Right-click `InteractionPrompt` → **UI → Text - TextMeshPro**
   - Name: `PromptText`
   - Set text: `[E] Press E to Interact`
   - Font Size: 24
   - Alignment: Center
   - Color: White
   - Enable: Best Fit (optional)

5. **Add InteractionPromptUI Component**
   - Select `InteractionPrompt`
   - Add Component → `InteractionPromptUI`
   - Assign references:
     ```
     Detection Reference:
     ├─ Interaction Detector: (leave empty for auto-find)
     └─ Auto Find Detector: ✓ (check - will find player's detector)
     
     UI References:
     ├─ Prompt Container: InteractionPrompt (self)
     ├─ Prompt Text: PromptText (child)
     └─ Canvas Group: CanvasGroup (self)
     
     Animation Settings:
     ├─ Fade Duration: 0.2
     ├─ Fade Ease: OutQuad
     ├─ Pulse Duration: 1
     ├─ Pulse Scale Amount: 0.05
     └─ Pulse Ease: InOutSine
     
     Formatting:
     ├─ Key Format: [{0}]
     └─ Prompt Format: {0} {1}
     ```

### Step 6: Setup Audio Manager (Optional but Recommended)

1. **Create Empty GameObject**
   - Hierarchy → Create Empty
   - Name: `InteractionAudioManager`
   - Add Component → `InteractionAudioManager`

2. **Assign Default Sounds** (if you have them)
   ```
   Default Sounds:
   ├─ Default Highlight Sound: (optional)
   ├─ Default Interact Sound: (optional)
   ├─ Default Pickup Sound: (optional)
   └─ Default Cancel Sound: (optional)
   
   Volume Settings:
   ├─ Highlight Volume: 0.3
   ├─ Interact Volume: 0.7
   ├─ Pickup Volume: 0.8
   └─ Cancel Volume: 0.6
   ```

3. **Make it Persistent** (optional)
   - The component will auto-create singleton
   - It will persist between scenes automatically

---

## Creating Interactables

### Type 1: Item Pickup (Instant Collection)

#### Example: Create a Wooden Log Pickup

1. **Create GameObject**
   - Place your 3D model in scene
   - Name: `WoodenLog_Pickup`

2. **Add Collider**
   - Add Component → **Box Collider** (or appropriate type)
   - Adjust size to fit model
   - **Ensure "Is Trigger" is UNCHECKED** (for OverlapSphere detection)

3. **Set Layer**
   - In Inspector, set Layer: **Interactable**

4. **Add ItemInteractable Component**
   - Add Component → `ItemInteractable`
   - Configure:
     ```
     Item Settings:
     ├─ Item: Select your InventoryItem (e.g., Wooden Log)
     ├─ Quantity: 1
     └─ Custom Prompt: (leave empty to use default)
     
     Interaction Settings:
     ├─ Interaction Priority: 1.0
     └─ Interaction Verb: "Press E to"
     
     Feedback:
     ├─ Highlight Effect: (optional GameObject to enable/disable)
     ├─ Pickup Sound: (optional AudioClip)
     ├─ Pickup Particles: (optional particle prefab)
     └─ Destroy On Pickup: ✓ (check)
     ```

5. **Test It**
   - Enter Play Mode
   - Approach the log
   - You should see: `[E] Press E to Pick up Wooden Log`
   - Press E to collect

---

### Type 2: Gathering Node (Hold E with Timer)

#### Example: Create a Berry Bush

1. **Create GameObject**
   - Add your berry bush 3D model
   - Name: `BerryBush_Gathering`

2. **Add Collider**
   - Add Component → **Sphere Collider**
   - Adjust radius
   - **Is Trigger: Unchecked**

3. **Set Layer**
   - Layer: **Interactable**

4. **Create Progress Bar Spawn Point** (optional)
   - Create Empty child
   - Name: `ProgressBarSpawnPoint`
   - Position above bush: Y = +2

5. **Add GatheringInteractable Component**
   - Add Component → `GatheringInteractable`
   - Configure:
     ```
     Resource Settings:
     ├─ Resource Item: Select item (e.g., Berries)
     ├─ Resources Per Gather: 3
     └─ Custom Prompt: "Berries" (optional)
     
     Gathering Settings:
     ├─ Gather Duration: 3.0
     ├─ Is Multi Use: ✓ (check for respawning)
     └─ Respawn Time: 60.0
     
     Interaction Settings:
     └─ Interaction Priority: 1.2
     
     Visual Feedback:
     ├─ Highlight Effect: (optional)
     ├─ Depleted Visual: (optional empty bush model)
     ├─ Progress Bar Spawn Point: Drag child object here
     └─ Progress Bar Prefab: (leave empty for auto-generated)
     
     Audio:
     ├─ Gather Start Sound: (optional)
     ├─ Gather Loop Sound: (optional)
     ├─ Gather Complete Sound: (optional)
     └─ Gather Cancel Sound: (optional)
     ```

6. **Test It**
   - Approach bush
   - See: `[Hold E] Hold E to Gather Berries`
   - **Hold E** for 3 seconds
   - Progress bar appears above bush
   - On completion: `Collected 3 Berries`
   - Bush depletes and starts respawn timer

---

### Type 3: Assessment Terminal (UI Trigger)

#### Example: Create Assessment Terminal

1. **Create GameObject**
   - Add terminal 3D model
   - Name: `AssessmentTerminal`

2. **Add Collider**
   - Add Component → **Box Collider**
   - **Is Trigger: Unchecked**

3. **Set Layer**
   - Layer: **Interactable**

4. **Add AssessmentTerminalInteractable Component**
   - Add Component → `AssessmentTerminalInteractable`
   - Configure:
     ```
     UI Reference:
     ├─ Assessment Report UI: (auto-finds in scene)
     └─ Find UI Automatically: ✓ (check)
     
     Interaction Settings:
     ├─ Custom Prompt: "Check Assessment Report"
     ├─ Interaction Verb: "Press E to"
     └─ Interaction Priority: 0.8
     
     Player Control:
     ├─ Lock Player Input During UI: ✓ (recommended)
     └─ Pause Game During UI: (optional)
     
     Visual Feedback:
     ├─ Highlight Effect: (optional)
     ├─ Interact Sound: (optional)
     └─ Highlight Color: Cyan
     
     Cooldown (Optional):
     ├─ Use Cooldown: (optional)
     └─ Cooldown Time: 2.0
     ```

5. **Ensure AssessmentReportUI Exists**
   - Find your `AssessmentReportUI` in scene
   - Make sure it's in the scene (can be disabled by default)

6. **Test It**
   - Approach terminal
   - See: `[E] Press E to Check Assessment Report`
   - Press E
   - Assessment UI opens
   - Player input is locked

---

### Type 4: Backward Compatibility (Existing Items)

#### Migrate Existing ResourceCollector Objects

1. **Find Existing ResourceCollector**
   - Select object with `ResourceCollector` component

2. **Add Adapter Component**
   - Add Component → `ResourceCollectorInteractable`
   - It will auto-find the ResourceCollector
   - Configure:
     ```
     Adapter Settings:
     ├─ Resource Collector: (auto-assigned)
     ├─ Interaction Priority: 1.0
     └─ Interaction Verb: "Press E to"
     
     Optional Overrides:
     ├─ Use Custom Prompt: (optional)
     └─ Custom Prompt: (optional)
     ```

3. **Set Layer**
   - Change object layer to: **Interactable**

4. **Test It**
   - Works exactly like before
   - But now uses new interaction system
   - Falls back to old ItemDetector if needed

---

## Testing

### Basic Functionality Test

1. **Enter Play Mode**
2. **Move near any interactable**
   - ✅ UI prompt should appear at bottom
   - ✅ Green wireframe sphere in Scene view (if gizmos enabled)
3. **Move between multiple interactables**
   - ✅ Prompt should update automatically
   - ✅ Nearest item gets priority
4. **Press E to interact**
   - ✅ Item pickups: Instant collection
   - ✅ Gathering nodes: Progress bar appears, hold E
   - ✅ Terminals: UI opens
5. **Test gathering cancellation**
   - ✅ Release E early: Gathering cancelled
   - ✅ Take damage: Gathering interrupted (if implemented)

### Debug Gizmos

In **Scene View** while playing:
- **Yellow sphere**: Detection radius
- **Cyan line**: Points to nearest interactable
- **Green sphere**: Player has target
- **Yellow sphere**: Player has no target

---

## Troubleshooting

### Issue: Prompt UI doesn't appear

**Solutions:**
1. Check `InteractionDetector` is on player
2. Verify `InteractableLayerMask` includes "Interactable" layer
3. Ensure interactable object has correct layer
4. Check `InteractionPromptUI` is in scene and enabled
5. Verify Canvas is set to **Screen Space - Overlay**

### Issue: E key doesn't work

**Solutions:**
1. Check Input System action map is enabled
2. Verify `PlayerInputHandler` event is subscribed
3. Ensure `HandleInteractInput()` is called in PlayerControllerRefactored
4. Check player isn't input-blocked (inventory open?)

### Issue: Progress bar doesn't show

**Solutions:**
1. Verify DOTween is installed
2. Check `GatheringProgressUI` script has no errors
3. Ensure `progressBarSpawnPoint` is assigned (or will use default)
4. Check console for errors during gathering start

### Issue: Can't interact with objects

**Solutions:**
1. Verify object has **Collider** (not trigger)
2. Check object layer is **Interactable**
3. Ensure `IInteractable` component exists
4. Verify `CanInteract` returns true
5. Check detection radius covers object

### Issue: Items not highlighted

**Solutions:**
1. UI markers not created yet (Phase 3 optional)
2. Use `highlightEffect` GameObject instead
3. Or add outline shader manually

### Issue: No audio

**Solutions:**
1. Check `InteractionAudioManager` exists in scene
2. Assign audio clips to interactables
3. Verify volumes aren't set to 0
4. Check Audio Listener is on camera

---

## Advanced Configuration

### Custom Progress Bar Prefab

1. **Create UI Prefab**
   - Duplicate auto-generated progress bar from scene
   - Customize appearance
   - Save as prefab in `Assets/Prefabs/UI/`

2. **Assign to GatheringInteractable**
   - Drag prefab to `Progress Bar Prefab` field
   - Will use your custom design

### Custom UI Markers

1. **Create Marker Sprite**
   - Import sprite (e.g., diamond icon)
   - Set texture type: Sprite (2D and UI)

2. **Use InteractableUIMarker.CreateMarker()**
   - In interactable script:
   ```csharp
   private void Start()
   {
       Sprite markerSprite = // load your sprite
       var marker = InteractableUIMarker.CreateMarker(transform, markerSprite);
   }
   ```

### Damage Interruption for Gathering

In your damage system, when player takes damage:
```csharp
// In your player damage handler
public void TakeDamage(float damage)
{
    // ... damage logic
    
    // Interrupt any active gathering
    var gatherer = GetComponentInChildren<GatheringInteractable>();
    if (gatherer != null)
    {
        gatherer.OnPlayerDamaged();
    }
}
```

---

## Performance Optimization

### Recommended Settings

**InteractionDetector:**
- Update Interval: 0.1 - 0.15 (don't update every frame)
- Detection Radius: Keep reasonable (2-3 units)
- Layer Mask: Only "Interactable" layer

**Audio Manager:**
- Pool Size: 5-10 sources
- Only assign essential default sounds

**UI:**
- Use DOTween (zero GC)
- Keep Canvas Scaler on Scale With Screen Size

---

## Quick Reference

### Input Actions
- **E Key**: Interact / Hold for gathering
- Works with: `InputSystem_Actions.inputactions` → Pickup action

### Component Hierarchy
```
Player
├── PlayerControllerRefactored
├── InteractionDetector
├── InventoryManager
└── ItemDetector (deprecated, but kept for compatibility)

Canvas (UI)
└── InteractionPrompt
    ├── InteractionPromptUI
    ├── CanvasGroup
    └── PromptText (TMP)

Interactable Objects
├── [3D Model]
├── Collider (not trigger)
├── Layer: Interactable
└── [Interactable Component Type]
    - ItemInteractable
    - GatheringInteractable
    - AssessmentTerminalInteractable
    - ResourceCollectorInteractable
```

---

## Next Steps

After setup is complete:

1. ✅ **Test all interactable types**
2. ✅ **Create prefabs** for common interactables
3. ✅ **Add custom audio clips**
4. ✅ **Customize UI appearance**
5. ✅ **Create more interactable types** as needed
6. 🔄 **Migrate existing items** gradually
7. 🔄 **Remove old ItemDetector** when fully migrated

---

## Support & Extensions

### Creating Custom Interactable Types

Implement `IInteractable` interface:
```csharp
public class DoorInteractable : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "Open Door";
    public string InteractionVerb => "Press E to";
    public bool CanInteract => !isOpen;
    public float InteractionPriority => 1f;
    public Transform GetTransform() => transform;
    
    public void OnHighlighted(bool highlighted) 
    {
        // Visual feedback
    }
    
    public void Interact(PlayerControllerRefactored player)
    {
        // Open door logic
    }
}
```

### Useful Debug Commands

**In Inspector (Play Mode):**
- Check `InteractionDetector.NearestInteractable` (shows current target)
- Check `InteractionDetector.HasInteractableInRange` (true/false)
- Check `InteractionPromptUI.isVisible` (is prompt showing)

**In Console:**
```csharp
// Enable/disable interaction system
interactionDetector.enabled = false;

// Force update detection
interactionDetector.ForceUpdate();

// Check what's in range
var inRange = interactionDetector.GetInteractablesInRange();
```

---

## Congratulations! 🎉

Your interaction system is now fully set up and ready to use!

For questions or issues, refer to the **Troubleshooting** section above.
