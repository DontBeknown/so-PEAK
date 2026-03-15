# Collectable System

## What it is
The collectable system unlocks lore/content entries and triggers the correct UI behavior when the player interacts with a collectable object.

Current supported types:
- TextDocument
- ScriptDialog

Main data model:
- CollectableItem ScriptableObject with id, headerName, content, type, icon, and optional dialogData.

## How it works (short)
1. A world object with CollectableInteractable is interacted with by the player.
2. CollectableInteractable checks CanInteract and calls ICollectableManager.Unlock.
3. CollectableManager stores the collectable id in memory and publishes CollectableUnlockedEvent.
4. Branch by type:
- TextDocument: opens inventory, switches to Collectables tab, publishes CollectableHubFocusRequestedEvent.
- ScriptDialog: publishes CollectableOpenRequestedEvent.
5. UI listeners react:
- CollectablesHubUI refreshes list and can auto-focus/open selected document.
- DocumentPageUI opens document view for TextDocument open events.
- DialogUI starts dialog for ScriptDialog open events.
6. Save system persists unlocked collectables and triggered dialogs on save/load.

## Key runtime files
- Data: Assets/Game/Script/Collectable/CollectableItem.cs
- Types: Assets/Game/Script/Collectable/CollectableType.cs
- Manager: Assets/Game/Script/Collectable/CollectableManager.cs
- World pickup: Assets/Game/Script/Interaction/Interactables/CollectableInteractable.cs
- Hub UI: Assets/Game/Script/UI/Collectable/CollectablesHubUI.cs
- Document page UI: Assets/Game/Script/UI/Collectable/DocumentPageUI.cs
- Dialog UI: Assets/Game/Script/UI/Dialog/DialogUI.cs
- Events: Assets/Game/Script/Core/Events/GameEvents.cs
- DI registration: Assets/Game/Script/Core/GameServiceBootstrapper.cs
- Save/load integration: Assets/Game/Script/Core/SaveSystem/SaveLoadService.cs

## How to extend

### A) Add more collectables with existing behavior
1. Create a new CollectableItem asset from menu:
- Create -> Game -> Collectable -> Collectable Item
2. Fill fields:
- id: unique string (required)
- headerName
- content
- icon
- type
- dialogData only for ScriptDialog
3. Add this asset to CollectablesHubUI.allCollectables so it appears in the hub list.
4. Assign this asset to a world CollectableInteractable component.

### B) Add a new collectable type
1. Add enum value in CollectableType.
2. Handle pickup behavior in CollectableInteractable.Interact.
3. Add UI/logic listener for the new type (subscribe to existing event or define a new event).
4. Update CollectablesHubUI button binding rules if the new type needs an action button.
5. If needed, add new data fields in CollectableItem for the type.

## Code snippets

### 1) Add a new collectable type
```csharp
namespace Game.Collectable
{
	public enum CollectableType
	{
		TextDocument,
		ScriptDialog,
		AudioLog
	}
}
```

### 2) Handle new type on interact
Use this pattern in CollectableInteractable.Interact after unlock succeeds.

```csharp
if (collectableItem.type == CollectableType.AudioLog)
{
	var eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
	eventBus?.Publish(new CollectableOpenRequestedEvent(collectableItem, false));
}
```

### 3) Add a dedicated open event (optional)
If AudioLog needs extra payload or different routing, add a dedicated event.

```csharp
public class AudioLogOpenRequestedEvent
{
	public CollectableItem Collectable { get; }

	public AudioLogOpenRequestedEvent(CollectableItem collectable)
	{
		Collectable = collectable;
	}
}
```

### 4) Listen in a UI/player component
```csharp
private IEventBus _eventBus;

private void Start()
{
	_eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
	_eventBus?.Subscribe<AudioLogOpenRequestedEvent>(OnAudioLogOpenRequested);
}

private void OnDestroy()
{
	_eventBus?.Unsubscribe<AudioLogOpenRequestedEvent>(OnAudioLogOpenRequested);
}

private void OnAudioLogOpenRequested(AudioLogOpenRequestedEvent evt)
{
	if (evt?.Collectable == null)
		return;

	// Play clip, open panel, or show subtitle here.
}
```

### 5) Guard against duplicate ids
Use this check in editor tooling or validation scripts.

```csharp
var ids = new HashSet<string>();
foreach (var c in allCollectables)
{
	if (c == null)
		continue;

	if (string.IsNullOrWhiteSpace(c.id))
		Debug.LogWarning($"Collectable '{c.name}' has empty id.");
	else if (!ids.Add(c.id))
		Debug.LogError($"Duplicate collectable id found: {c.id}");
}
```

## How to set up in Unity

### 1) Scene services
1. Ensure a CollectableManager exists in scene.
2. Ensure a DialogManager exists in scene if ScriptDialog is used.
3. Ensure GameServiceBootstrapper can register both managers.
- Either assign references in inspector or rely on FindFirstObjectByType path.

### 2) Inventory + collectables tab
1. Ensure TabbedInventoryUI is in scene and wired.
2. Assign CollectablesHubUI reference in TabbedInventoryUI.
3. Ensure collectables tab button is connected.

### 3) Collectables hub
1. In CollectablesHubUI, assign:
- hubRoot
- listContainer
- entryPrefab
- documentPageUI
- allCollectables array
2. In entry prefab, keep button names containing Open and Replay so current binding logic detects them.

### 4) Document page panel
1. In DocumentPageUI, assign:
- panelRoot
- headerText
- contentText
- iconImage
2. Optional animation tuning:
- slideDuration
- fadeDuration
- slideDistance
- slideOvershoot

### 5) World pickup object
1. Add CollectableInteractable to the interactable GameObject.
2. Assign collectableItem.
3. Choose destroyOnInteract behavior.

## Save/load behavior
- Unlocked collectable ids are saved in worldState.unlockedCollectables.
- Triggered dialog ids are saved in worldState.triggeredDialogs.
- On load, both states are restored into CollectableManager and DialogManager.

## Quick test checklist
1. Collect a TextDocument in play mode.
2. Verify inventory opens on Collectables tab and focused entry is shown.
3. Verify document panel opens and content matches asset data.
4. Collect a ScriptDialog item and verify dialog starts.
5. Save and reload, then verify previously unlocked entries stay unlocked.
