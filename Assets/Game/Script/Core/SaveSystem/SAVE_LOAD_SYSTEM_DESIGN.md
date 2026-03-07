# Save & Load System Design

**Location:** `Assets/Game/Script/Core/SaveSystem/`  
**Last Updated:** March 6, 2026

---

## Overview

The Save/Load System provides persistent storage for all world and player data. It supports multiple save slots (one per world), automatic backups, optional compression, and an auto-save timer. The system is built around a singleton `SaveLoadService` component and a set of plain serializable data classes.

---

## File Structure

```
SaveSystem/
├── ISaveLoadService.cs      # Public interface contract
├── SaveLoadService.cs       # Core implementation (MonoBehaviour singleton)
├── WorldSaveData.cs         # All serializable data classes
├── SeedConfig.cs            # ScriptableObject: seed digit configuration
├── SeedData.cs              # Struct: 3-part seed value
├── SaveExitButton.cs        # UI button: save then return to menu
└── WorldSeedLoader.cs       # Example: read seed from active save
```

---

## Data Model

### WorldSaveData — Top-Level Save Object

Every save file maps to one `WorldSaveData` instance serialized to JSON.

| Field | Type | Description |
|---|---|---|
| `worldName` | `string` | Display name chosen by the player |
| `worldGuid` | `string` | Unique identifier — used as the filename |
| `createdDate` | `DateTime` | When the world was first created |
| `lastPlayedDate` | `DateTime` | Updated every save |
| `totalPlayTime` | `float` | Seconds of total play time |
| `seedData` | `SeedData` | The three-part procedural seed |
| `playerData` | `PlayerSaveData` | Player transform, stats, and inventory |
| `worldState` | `WorldStateSaveData` | Time of day, weather, level, interactables |
| `gameVersion` | `string` | Unity `Application.version` at save time |
| `saveVersion` | `int` | Schema version (currently `1`) |

### PlayerSaveData

| Field | Type | Description |
|---|---|---|
| `position` | `float[3]` | World-space XYZ |
| `rotation` | `float[4]` | Quaternion XYZW |
| `health` / `maxHealth` | `float` | Current and maximum HP |
| `hunger` / `maxHunger` | `float` | Current and maximum hunger |
| `stamina` / `maxStamina` | `float` | Current and maximum stamina |
| `temperature` | `float` | Body temperature |
| `inventoryItems` | `List<InventoryItemSaveData>` | All carried items |
| `equippedItems` | `List<EquipmentSlotSaveData>` | Currently equipped gear |

### WorldStateSaveData

| Field | Type | Description |
|---|---|---|
| `currentTimeOfDay` | `float` | 0–24 hour clock |
| `dayNumber` | `int` | In-game day counter |
| `currentWeather` | `string` | Weather state name |
| `temperature` | `float` | Ambient world temperature |
| `level` | `int` | Current progression level |
| `interactableStates` | `List<InteractableStateSaveData>` | Per-object use state + respawn timer |
| `resourceNodes` | `List<ResourceNodeSaveData>` | Depletion state of gatherable nodes |

### SeedData

A three-part numeric seed of configurable digit length, controlled by `SeedConfig`.

```
FullSeed = seed1 + seed2 + seed3   // e.g. "12345678 87654321 11223344"
```

`SeedConfig` (ScriptableObject) defines how many digits each part has (default: 8-8-8 = 24 digits total). The seed is converted to an `int` via `GetHashCode()` for systems that need a numeric seed (terrain generation, etc.).

---

## File Storage Layout

```
Application.persistentDataPath/
├── Saves/
│   ├── metadata.json               # Index of all worlds (name, last played, seed)
│   ├── <worldGuid>.sav             # One file per world (JSON, optionally compressed)
│   └── ...
└── Backups/
    └── <worldGuid>/
        ├── backup_20260306_120000.sav
        ├── backup_20260306_130000.sav
        └── ...                     # Up to maxBackupCount (default: 5)
```

- **`metadata.json`** — A fast index used to list worlds in the main menu without loading each full save file. Contains: `worldGuid`, `worldName`, `lastPlayedDate`, `totalPlayTime`, `seed1/2/3`, and `playerHealth`.
- **`.sav` files** — Full JSON saves. When `enableCompression = true` (default), the JSON string is Base64-encoded via `CompressString()` before writing.

---

## SaveLoadService — Core API

`SaveLoadService` is a `MonoBehaviour` singleton that persists across scenes (`DontDestroyOnLoad`).

### World Lifecycle

```csharp
// Create a brand-new world and immediately write it to disk
WorldSaveData world = SaveLoadService.Instance.CreateNewWorld("My World", seedData, level: 1);

// Load an existing world from disk by GUID
WorldSaveData world = SaveLoadService.Instance.LoadWorld(worldGuid);

// Write the current world state to disk
bool success = SaveLoadService.Instance.SaveWorld(currentWorldSave);

// Permanently delete a world (save file + all backups)
SaveLoadService.Instance.DeleteWorld(worldGuid);

// List all saved worlds (from metadata index)
List<SaveMetadata> worlds = SaveLoadService.Instance.GetAllWorlds();
```

### Reading Player Data

```csharp
// Direct access to the loaded save
WorldSaveData save = SaveLoadService.Instance.CurrentWorldSave;

// Convenience helpers
Vector3 pos      = SaveLoadService.Instance.GetSavedPlayerPosition();
Quaternion rot   = SaveLoadService.Instance.GetSavedPlayerRotation();
PlayerSaveData p = SaveLoadService.Instance.GetSavedPlayerData();
```

### New World vs. Existing World Detection

```csharp
// True when totalPlayTime == 0 (world was just created, never played)
bool isNew = SaveLoadService.Instance.IsNewWorld();

// Equivalent shorthand used by GameplaySceneInitializer for spawn logic
bool useDefaultSpawn = SaveLoadService.Instance.ShouldUseDefaultSpawn();
```

### Level Progression

```csharp
// Increment level by 1 and immediately save
SaveLoadService.Instance.ProgressToNextLevel();

// Read current level without saving
int level = SaveLoadService.Instance.GetCurrentLevel();
```

### Auto-Save

```csharp
// Enable with a custom interval (seconds)
SaveLoadService.Instance.EnableAutoSave(300f); // every 5 minutes

// Disable
SaveLoadService.Instance.DisableAutoSave();

// Trigger manually (also called internally by the timer)
SaveLoadService.Instance.PerformAutoSave();
```

`PerformAutoSave()` calls `UpdatePlayerDataFromGame()` first, which pulls the latest position, rotation, and stats from the live `PlayerController` and `PlayerStats` objects via the `ServiceContainer`.

### Backup System

```csharp
// Manually create a backup right now
SaveLoadService.Instance.CreateBackup(worldGuid);

// List available backup timestamps
List<DateTime> backups = SaveLoadService.Instance.GetBackups(worldGuid);

// Restore a specific backup (overwrites the current save file)
SaveLoadService.Instance.RestoreFromBackup(worldGuid, backupDate);
```

A backup is created automatically after every `SaveWorld()` call when `createBackupOnSave = true`. Old backups beyond `maxBackupCount` (default: 5) are deleted, keeping only the most recent.

### Validation

```csharp
bool ok = SaveLoadService.Instance.ValidateSaveFile(worldGuid);
```

Checks that the save file exists, can be deserialized, has a valid GUID, name, seed, and player data.

---

## Events

Subscribe to these to react to save/load/delete operations from any system:

```csharp
SaveLoadService.Instance.OnWorldSaved   += (WorldSaveData data) => { ... };
SaveLoadService.Instance.OnWorldLoaded  += (WorldSaveData data) => { ... };
SaveLoadService.Instance.OnWorldDeleted += (string worldGuid)   => { ... };
```

Always unsubscribe in `OnDestroy` to avoid memory leaks:

```csharp
private void OnDestroy()
{
    if (SaveLoadService.Instance != null)
    {
        SaveLoadService.Instance.OnWorldSaved -= HandleSaved;
    }
}
```

---

## How a Save Works (Step-by-Step)

```
1. SaveWorld(saveData) called
       ↓
2. saveData.lastPlayedDate = DateTime.Now
       ↓
3. JsonUtility.ToJson(saveData)          ← full serialization
       ↓
4. CompressString(json)                  ← Base64 encode (if compression enabled)
       ↓
5. File.WriteAllText(<worldGuid>.sav)    ← write to Saves/
       ↓
6. UpdateMetadata(saveData)              ← update metadata.json index
       ↓
7. CreateBackup(worldGuid)               ← copy .sav → Backups/<guid>/backup_<timestamp>.sav
       ↓
8. CleanupOldBackups()                   ← delete oldest if > maxBackupCount
       ↓
9. OnWorldSaved event fired
```

---

## How a Load Works (Step-by-Step)

```
1. LoadWorld(worldGuid) called
       ↓
2. File.ReadAllText(<worldGuid>.sav)
       ↓
3. DecompressString(json)                ← Base64 decode (if compression enabled)
       ↓
4. JsonUtility.FromJson<WorldSaveData>()
       ↓
5. ValidateSaveData()                    ← null checks, GUID, seed validity
       ↓
6. currentWorldSave = saveData           ← assign to in-memory reference
       ↓
7. OnWorldLoaded event fired
       ↓
8. Caller uses CurrentWorldSave / helper methods to restore game state
```

---

## Integration with GameplaySceneInitializer

When transitioning to the gameplay scene, `GameplaySceneInitializer` reads the loaded save to set up the world:

```csharp
// Spawn point
if (SaveLoadService.Instance.ShouldUseDefaultSpawn())
    SpawnPlayerAt(defaultSpawnPoint);  // New world
else
    SpawnPlayerAt(saveService.GetSavedPlayerPosition(),
                  saveService.GetSavedPlayerRotation());  // Existing world

// Terrain seed
SeedData seed = saveService.CurrentWorldSave.seedData;
int seedInt    = seed.FullSeed.GetHashCode();
terrainGenerator.Generate(seedInt);

// Player stats
PlayerSaveData pData = saveService.GetSavedPlayerData();
playerStats.RestoreFrom(pData);

// Start auto-save
saveService.EnableAutoSave(300f);
```

---

## Adding Data to the Save System

### 1. Add a field to `PlayerSaveData` or `WorldStateSaveData`

```csharp
// WorldSaveData.cs
[Serializable]
public class PlayerSaveData
{
    // ... existing fields ...
    public int myCustomValue;
}
```

### 2. Capture data in `UpdatePlayerDataFromGame()`

```csharp
// SaveLoadService.cs — UpdatePlayerDataFromGame()
var myComponent = container.TryGet<MyComponent>();
if (myComponent != null)
    currentWorldSave.playerData.myCustomValue = myComponent.GetValue();
```

### 3. Restore data when loading

```csharp
// GameplaySceneInitializer or the owning component
PlayerSaveData data = SaveLoadService.Instance.GetSavedPlayerData();
myComponent.SetValue(data.myCustomValue);
```

---

## Inspector Configuration

| Property | Default | Description |
|---|---|---|
| `seedConfig` | (asset ref) | `SeedConfig` ScriptableObject defining seed digit lengths |
| `enableCompression` | `true` | Base64-encode the JSON before writing |
| `autoSaveEnabled` | `false` | Start auto-save timer on Awake |
| `autoSaveInterval` | `300s` | Seconds between auto-saves |
| `maxBackupCount` | `5` | Maximum backup files kept per world |
| `createBackupOnSave` | `true` | Create a backup on every save |
| `enableDebug` | `false` | Log detailed messages to Console |

---

## Known Limitations & Future Work

| Issue | Status | Notes |
|---|---|---|
| Inventory/Equipment save | ⚠️ TODO | `UpdatePlayerDataFromGame` has commented-out TODO blocks for inventory |
| Save file encryption | ❌ Not implemented | Infrastructure comment exists in `SaveWorld()` |
| GZIP compression | ❌ Not implemented | Currently uses Base64 (adds ~33% size overhead) |
| Background thread I/O | ❌ Not implemented | `File.WriteAllText` blocks the main thread |
| `ServiceContainer.Unregister` | ❌ Missing | Cannot cleanly unregister services on scene unload |

---

## Related Files

- [CORE_SYSTEM_OVERVIEW.md](../CORE_SYSTEM_OVERVIEW.md) — Full core architecture overview
- [WorldPersistenceManager.cs](../WorldPersistenceManager.cs) — Legacy scene-transfer ScriptableObject
- [GameplaySceneInitializer.cs](../GameplaySceneInitializer.cs) — Scene setup consumer of the save system
