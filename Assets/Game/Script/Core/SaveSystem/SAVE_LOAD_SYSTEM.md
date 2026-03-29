# Save/Load Guide (Current Flow + Extension Patterns)

Location: Assets/Game/Script/Core/SaveSystem
Last Updated: 2026-03-25

---

## 1) What this system does

This save system stores one world per file and keeps a lightweight world index for menus.

Core pieces:
- Save service: SaveLoadService.cs
- Save schema: WorldSaveData.cs
- Runtime destroyed-spawn registry: SpawnedObjectStateRegistry.cs
- Public contract: ISaveLoadService.cs

Main storage:
- Saves/<worldGuid>.sav (full save payload)
- Saves/metadata.json (world list and quick preview)
- Backups/<worldGuid>/backup_yyyyMMdd_HHmmss.sav

Notes:
- If compression is enabled, the save payload is Base64-encoded JSON.
- Metadata is plain JSON.

---

## 2) Save flow (actual runtime order)

When SaveLoadService.SaveWorld(saveData) is called:

1. Update timestamp:
- saveData.lastPlayedDate = DateTime.Now

2. Export destroyed spawned-object state into save:
- SpawnedObjectStateRegistry.ExportToSave(saveData)

3. Serialize to JSON:
- JsonUtility.ToJson(saveData, true)

4. Optional compression:
- If enableCompression is true, JSON is converted to Base64 text

5. Write file:
- Saves/<worldGuid>.sav

6. Update metadata index:
- Saves/metadata.json

7. Optional backup:
- Copy save to Backups/<worldGuid>/backup_<timestamp>.sav
- Keep only maxBackupCount newest backups

8. Raise event:
- OnWorldSaved(saveData)

---

## 3) Load flow (actual runtime order)

When SaveLoadService.LoadWorld(worldGuid) is called:

1. Read Saves/<worldGuid>.sav

2. Optional decompression:
- If enableCompression is true, Base64 text is decoded to JSON

3. Deserialize:
- JsonUtility.FromJson<WorldSaveData>(json)

4. Validate minimum required fields:
- worldGuid, worldName, seedData validity, playerData

5. Set active in-memory save:
- currentWorldSave = saveData

6. Import destroyed spawned-object state into runtime cache:
- SpawnedObjectStateRegistry.ImportFromSave(saveData)

7. Raise event:
- OnWorldLoaded(saveData)

Important:
- Save currently calls ImportFromSave directly and does not run HydrateWorldServices (that call is commented). If you rely on collectable/dialog hydration from SaveLoadService, restore that call or hydrate from another bootstrap path.

### 3.1 End-to-end existing save load order (Menu -> Gameplay)

When player loads an existing world from menu:

1. `WorldSelectionUI` load action calls `SaveLoadService.LoadWorld(worldGuid)` in menu scene.
2. Save is deserialized and assigned to in-memory `CurrentWorldSave`.
3. Menu flow loads gameplay scene (`TerrainGenDemo`).
4. `RenderController.Start()` reads save context and starts terrain/world setup.
5. Terrain-ready pass triggers player spawn sequence.
6. `PlayerSpawner.SpawnPlayer()` instantiates player at saved/default position.
7. `GameServiceBootstrapper.UpdatePlayerServices(newPlayer)` rebinds player-related service references post-spawn.
8. `GameplaySceneInitializer` waits for player spawn completion, then applies player/world restore (stats, inventory, equipment, world state).

Clarifications:
- Save loading happens before scene transition (menu scene), not after entering gameplay scene.
- `GameServiceBootstrapper` is a global service bootstrapper; it is not only for player initialization.

---

## 4) Save schema map

Top object:
- WorldSaveData

Major sections:
- world identity: worldName, worldGuid, dates, totalPlayTime
- seed: SeedData
- playerData: transform, stats, inventory, equipment
- worldState: time/weather/level/interactable/resource/spawned state
- tutorial: TutorialSaveData
- meta: gameVersion, saveVersion

Destroyed procedural objects are stored in:
- WorldStateSaveData.spawnedObjectStates : List<SpawnedObjectStateSaveData>
- Each item has:
  - spawnId
  - isDestroyed

---

## 5) How destroyed spawned-object persistence works

Runtime behavior:
1. Interactable object is spawned with a SpawnId and SpawnedObjectState component.
2. When object is consumed/destroyed, gameplay script calls SpawnedObjectState.MarkDestroyed().
3. MarkDestroyed forwards to SpawnedObjectStateRegistry.MarkDestroyed(spawnId).
4. Registry updates:
- In-memory HashSet destroyedSpawnIds
- Current save list worldState.spawnedObjectStates (upsert)
5. On save, ExportToSave writes HashSet -> spawnedObjectStates list.
6. On load, ImportFromSave reads list -> HashSet.
7. Spawn pipeline checks IsDestroyed(spawnId) and skips spawn if true.

Level progression behavior:
- SaveLoadService.ProgressToNextLevel() now calls SpawnedObjectStateRegistry.ClearAllDestroyed().
- This clears destroyed spawn state before saving next level.

---

## 6) How to extend safely

### A) Add new data to saves

1. Add fields to schema classes in WorldSaveData.cs.
2. Initialize defaults in SaveLoadService.CreateDefaultPlayerData or CreateDefaultWorldState.
3. Populate runtime values in UpdatePlayerDataFromGame (if needed).
4. Load/apply values during world initialization.
5. Keep null-safe guards for old saves.

Example 1: Add a new field to save schema

```csharp
// WorldSaveData.cs
[Serializable]
public class WorldStateSaveData
{
  public float currentTimeOfDay;
  public int dayNumber;

  // New field
  public int playerDeaths;
}
```

Example 2: Initialize default in SaveLoadService

```csharp
// SaveLoadService.cs
private WorldStateSaveData CreateDefaultWorldState(int level = 1)
{
  return new WorldStateSaveData
  {
    currentTimeOfDay = 6f,
    dayNumber = 1,
    level = level,

    // New default
    playerDeaths = 0,

    interactableStates = new List<InteractableStateSaveData>(),
    resourceNodes = new List<ResourceNodeSaveData>(),
    spawnedObjectStates = new List<SpawnedObjectStateSaveData>()
  };
}
```

Example 3: Update runtime value before save

```csharp
// SaveLoadService.cs (inside UpdatePlayerDataFromGame)
if (currentWorldSave.worldState == null)
  currentWorldSave.worldState = CreateDefaultWorldState();

currentWorldSave.worldState.playerDeaths = deathCounter.TotalDeaths;
```

Example 4: Null-safe load usage

```csharp
// Any loader/bootstrapper code
int deaths = saveData?.worldState?.playerDeaths ?? 0;
ui.SetDeathCounter(deaths);
```

Rules:
- Add fields, do not rename/remove existing fields unless doing migration.
- Use nullable/optional behavior where possible.

### B) Add a new runtime state registry (pattern)

Use SpawnedObjectStateRegistry as reference pattern:
- Runtime cache for fast checks
- ImportFromSave at load
- ExportToSave before file write
- Idempotent write path (upsert, no duplicates)

Recommended hooks:
- LoadWorld: import to cache
- SaveWorld: export from cache

Minimal registry pattern example:

```csharp
public static class ExampleStateRegistry
{
  private static readonly HashSet<string> activeIds = new HashSet<string>();

  public static void ImportFromSave(WorldSaveData save)
  {
    activeIds.Clear();
    var list = save?.worldState?.interactableStates;
    if (list == null) return;

    foreach (var s in list)
    {
      if (s != null && s.isUsed && !string.IsNullOrEmpty(s.interactableGuid))
        activeIds.Add(s.interactableGuid);
    }
  }

  public static void ExportToSave(WorldSaveData save)
  {
    save.worldState ??= new WorldStateSaveData();
    save.worldState.interactableStates ??= new List<InteractableStateSaveData>();

    var target = save.worldState.interactableStates;
    target.Clear();

    foreach (var id in activeIds)
    {
      target.Add(new InteractableStateSaveData
      {
        interactableGuid = id,
        isUsed = true,
        respawnTimer = 0f
      });
    }
  }
}
```

### C) Add new destroy/consume flows

If any interactable can remove/deactivate itself, ensure it calls persistence before removal:
- GetComponent<SpawnedObjectState>()?.MarkDestroyed();

Do this in every path:
- Destroy(gameObject)
- SetActive(false)
- custom animation destroy paths

Example destroy hook:

```csharp
private void PersistSpawnDestroyedState()
{
  var spawnedState = GetComponent<SpawnedObjectState>();
  spawnedState?.MarkDestroyed();
}

private void ConsumeAndRemove()
{
  PersistSpawnDestroyedState();
  Destroy(gameObject);
}
```

Example disable hook:

```csharp
private void ConsumeAndDisable()
{
  PersistSpawnDestroyedState();
  gameObject.SetActive(false);
}
```

### D) Add pruning rules

If you want to reduce save size:
- Add targeted prune methods in SpawnedObjectStateRegistry
- Example: remove IDs for old levels or specific prefixes
- Call prune before SaveWorld

---

## 7) Common pitfalls

- MarkDestroyed not called:
  - Happens when destroy path bypasses persistence hook.
- Save appears fine but list empty:
  - ExportToSave not invoked (or save not triggered after destroy).
- Parse issues in external tools:
  - Remember .sav may be Base64-encoded JSON.
- Level transitions unexpectedly keep/clear state:
  - Check ProgressToNextLevel behavior (currently clears destroyed spawned-object list).

---

## 8) Quick test checklist

1. Create/load world.
2. Destroy one spawned interactable.
3. Confirm MarkDestroyed path runs.
4. Save world.
5. Inspect world save through editor debug tool.
6. Reload world and verify object stays removed (except when level progression intentionally clears state).
7. Progress to next level and verify destroyed list is cleared by design.

---

## 9) Suggested next improvements

- Add schema migration guard by using saveVersion explicitly.
- Add dedicated diagnostics method in SaveLoadService to dump key counts (interactables/resources/spawnedObjectStates).
- Add unit/integration tests for:
  - SaveWorld export of spawnedObjectStates
  - LoadWorld import and spawn filter behavior
  - ProgressToNextLevel clear behavior
