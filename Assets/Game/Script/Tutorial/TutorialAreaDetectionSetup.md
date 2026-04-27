# Tutorial Area Detection Setup Guide

This guide shows you how to detect when the player enters tutorial areas using trigger colliders.

## Quick Start

### Step 1: Create Trigger Volume
- In your tutorial scene, create an empty GameObject (e.g., `MovementZone_Trigger`)
- Add a **Collider** component (Box, Sphere, or Capsule)
- **Important:** Check **Is Trigger** ✓

### Step 2: Attach Detection Script
- Add the `TutorialAreaTrigger` script to the same GameObject
- In the Inspector, configure:
  - **Area Name:** `"Movement Teaching Zone"`
  - **One Time Only:** ✓ (unchecked if you want repeated detection)
  - **Complete Current Step:** ✓ if you want the tutorial to auto-advance when entering
  - **Publish Custom Event:** ✓ if you need custom logic (e.g., spawn helpers, adjust difficulty)

### Step 3: Size the Collider
- Adjust the collider bounds to match your zone (the flat movement corridor, apple tree area, etc.)
- The player will trigger when their collider overlaps yours

## Example Scene Hierarchy

```
TutorialScene
├─ SpawnArea
├─ MovementZone
│  └─ MovementZone_Trigger (BoxCollider, Is Trigger)
│     └─ TutorialAreaTrigger script attached
├─ AppleTreeZone
│  └─ AppleTree_Trigger (SphereCollider, Is Trigger)
│     └─ TutorialAreaTrigger script attached
├─ WaterZone
│  └─ Water_Trigger (BoxCollider, Is Trigger)
│     └─ TutorialAreaTrigger script attached
├─ RestZone
│  └─ Campfire_Trigger (SphereCollider, Is Trigger)
│     └─ TutorialAreaTrigger script attached
└─ FinalAscentZone
   └─ Lighthouse_Trigger (SphereCollider, Is Trigger)
      └─ TutorialAreaTrigger script attached
```

## Detection Methods

### Method 1: OnTriggerEnter (Recommended)
- **What:** Player walks into a collider volume
- **When:** Once per entry (or every entry if `oneTimeOnly` is unchecked)
- **Use:** Transitioning between tutorial zones, opening gates, tracking completion

**Setup:**
1. Create trigger collider
2. Enable `TutorialAreaTrigger`
3. Set `oneTimeOnly = true` for one-time progression gates

### Method 2: Combine with Gate Logic
- **What:** Area trigger + `TutorialAreaGate` together
- **When:** Player enters zone → gate opens automatically
- **Use:** Seamless zone-by-zone progression

**Setup:**
1. Area trigger detects entry
2. Publishes `TutorialAreaEnteredEvent`
3. Gate listens to the corresponding tutorial step completion
4. Gate collider disables when step completes

### Method 3: Distance-Based Detection
If you prefer polling over colliders, use continuous distance checks:

```csharp
float distanceToZone = Vector3.Distance(player.position, zoneCenter);
if (distanceToZone < triggerRadius && !wasInZone)
{
    OnEnterZone();
    wasInZone = true;
}
```

## Zone-by-Zone Trigger Setup

### Movement Zone
- **Collider Type:** Box or Capsule (covers the flat corridor + slope)
- **Complete Current Step:** `false` (step completes via WalkDistance/LookAround/Sprint polling)
- **Area Name:** `"Movement Teaching Zone"`

### Food Zone
- **Collider Type:** Sphere (around the apple tree)
- **Complete Current Step:** `false` (step completes via PressInteract event)
- **Area Name:** `"Food Collection Zone"`

### Water Zone
- **Collider Type:** Sphere (around the water source)
- **Complete Current Step:** `false` (step completes via PressInteract event)
- **Area Name:** `"Water Refill Zone"`

### Rest Zone
- **Collider Type:** Sphere (around the campfire)
- **Complete Current Step:** `false` (step completes via HoldInteract event)
- **Area Name:** `"Rest and Recovery Zone"`

### Final Zone
- **Collider Type:** Sphere (around the lighthouse peak)
- **Complete Current Step:** `true` (auto-complete the final AutoAdvance step)
- **Area Name:** `"Lighthouse Objective Zone"`

## Integration with TutorialStatManipulator

You can stack both on the same zone or separate them:

**Stacked (Single Trigger):**
- GameObject: `MovementZone_Trigger`
- Scripts: `TutorialAreaTrigger` + `TutorialStatManipulator`
- Effect: Detect entry AND drain hunger/thirst in one trigger

**Separate (Multiple Triggers):**
- GameObject 1: `MovementZone_Trigger` → `TutorialAreaTrigger` (detection only)
- GameObject 2: `MovementZone_Damage` → `TutorialStatManipulator` (damage/drain only)
- Effect: Cleaner separation of concerns

## Debug & Troubleshooting

### Trigger Not Firing
1. ✓ Is the collider on the same GameObject as the script?
2. ✓ Is **Is Trigger** checked?
3. ✓ Does the player have a collider (not just a trigger)?
4. ✓ Are both colliders on the correct layers?

### Multiple Triggers
If you have overlapping triggers, set `oneTimeOnly = true` to prevent duplicate events.

### Testing in Editor
- Enable **Gizmos** → **Physics** to visualize colliders
- Set `debugLogs = true` to see when triggers fire in the Console
- Play the scene and walk through each zone

## Script Reference: TutorialAreaTrigger

### Public Fields
- `areaName` (string): Display name for debug logs
- `oneTimeOnly` (bool): Fire only once, then disable
- `debugLogs` (bool): Log to console when triggered
- `completeCurrentStep` (bool): Auto-complete the current tutorial step
- `publishCustomEvent` (bool): Publish TutorialAreaEnteredEvent
- `areaType` (enum): MovementZone, FoodZone, WaterZone, RestZone, FinalZone

### Example Inspector Setup

```
TutorialAreaTrigger
├─ Area Name: "Apple Tree Zone"
├─ One Time Only: ✓
├─ Debug Logs: ✓
├─ Complete Current Step: ☐
├─ Publish Custom Event: ☐
└─ Area Type: FoodZone
```

## Next Steps

1. Create one trigger per zone in your tutorial scene
2. Size each collider to match the zone bounds
3. Set `debugLogs = true` and playtest
4. Verify console shows "Player entered: [Area Name]"
5. Connect gates to step completion events if using TutorialAreaGate
