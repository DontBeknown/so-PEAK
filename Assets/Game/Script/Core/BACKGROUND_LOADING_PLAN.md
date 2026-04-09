# Background Loading: DebugGameplay as Waiting Room for TerrainGenDemo

## Overview

When the player transitions from the Menu into `TerrainGenDemo`, terrain chunk generation
takes time. Instead of a static loading screen, `Scene_Debug_Gameplay` is loaded
**additively on top** of `TerrainGenDemo` so the player has something interactive to do
while the world builds in the background.

The flow uses **two gates**:

| Gate | Condition | What happens |
|------|-----------|--------------|
| **Gate 1 — Chunks Ready** | All terrain chunks finalized by `RenderController` | Progress bar hits 100%, prompt appears: *"Press Y to enter world"* |
| **Gate 2 — Player Confirm** | Player presses **Y** in `Scene_Debug_Gameplay` | Unload debug scene → **then** spawn player in terrain scene |

Player spawning is intentionally **deferred** until after the player confirms. This means
`RenderController` must expose a way to trigger spawn on-demand rather than automatically.

---

## Revised Load Flow

```
[Menu Scene]
  WorldSelectionUI → SaveLoadService.LoadWorld(guid)   ← save data stored in memory
  SceneManager.LoadScene("TerrainGenDemo")
        |
        v
[TerrainGenDemo boots]
  GameServiceBootstrapper.Awake()     ← registers all services          (UNCHANGED)
  RenderController.Start()           ← queues terrain generation        (MODIFIED: no auto-spawn)
  AsyncLoadCoordinator.Start()       ← NEW: orchestrator (see below)
        |                  \
        |                   v
        |         [Scene_Debug_Gameplay loads additively]
        |           Player sees progress bar climbing 10% → 100%
        |           LoadingProgressUI reads SharedLoadingState every frame
        |                   |
        v                   | ← Gate 1: all chunks built
  RenderController finishes         SharedLoadingState.isChunksReady = true
  all FinalizeChunk() calls         statusMessage = "Press Y to enter world"
        |                           |
        |                           | ← Gate 2: player presses Y
        |                   DebugGameplayConfirm sets SharedLoadingState.playerConfirmed = true
        |                           |
        v                           v
  AsyncLoadCoordinator detects playerConfirmed
  UnloadSceneAsync("Scene_Debug_Gameplay") → yield return
  Re-enables TerrainGenDemo camera
  SetActiveScene(TerrainGenDemo)
  RenderController.SpawnPlayerNow()  ← triggers player spawn manually
        |
        v
[TerrainGenDemo live]
  RenderController.PlayerSpawnComplete = true
  GameplaySceneInitializer resumes   ← already waits on PlayerSpawnComplete (UNCHANGED)
  Restores stats / inventory / equipment
```

---

## Key Design Change: No Auto-Spawn

`RenderController` previously spawned the player automatically at the end of chunk
generation. Now it **holds** after all chunks are finalized and only spawns when
`AsyncLoadCoordinator` explicitly calls `SpawnPlayerNow()`.

This requires a **small modification** to `RenderController`:

| Before | After |
|--------|-------|
| After last `FinalizeChunk()` → auto-call `SpawnPlayerSequence()` | After last `FinalizeChunk()` → set `AllChunksReady = true`, wait for `SpawnPlayerNow()` call |

---

## Files

| File | Status | Folder |
|------|--------|--------|
| `SharedLoadingState.cs` | **NEW** | `Assets/Game/Script/Core/` |
| `SharedLoadingState` *(asset)* | **NEW** *(create in Editor)* | `Assets/Resources/` |
| `AsyncLoadCoordinator.cs` | **NEW** | `Assets/Game/Script/Core/` |
| `LoadingProgressUI.cs` | **NEW** | `Assets/Game/Script/UI/` |
| `DebugGameplayConfirm.cs` | **NEW** | `Assets/Game/Script/UI/` |
| `RenderController.cs` | **MODIFY** *(defer spawn + expose SpawnPlayerNow)* | `Assets/TerrainGenerator/Display/` |
| `PlayerSpawner.cs` | **NO CHANGE** | — |
| `GameplaySceneInitializer.cs` | **NO CHANGE** | — |
| `GameServiceBootstrapper.cs` | **NO CHANGE** | — |

---

## File Details

---

### [NEW] `SharedLoadingState.cs`

A `ScriptableObject` data bus. Both scenes reference the **same asset** from the project —
no direct scene-to-scene coupling needed.

**Location:** `Assets/Game/Script/Core/SharedLoadingState.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SharedLoadingState", menuName = "Game/Shared Loading State")]
public class SharedLoadingState : ScriptableObject
{
    [Range(0f, 1f)]
    public float progress = 0f;
    public string statusMessage = "Generating world...";

    // Gate 1: set by RenderController when all chunks are built
    public bool isChunksReady = false;

    // Gate 2: set by DebugGameplayConfirm when player presses Y
    public bool playerConfirmed = false;

    // Final flag: set by AsyncLoadCoordinator after transition completes
    public bool isComplete = false;

    public void Reset()
    {
        progress = 0f;
        statusMessage = "Generating world...";
        isChunksReady = false;
        playerConfirmed = false;
        isComplete = false;
    }
}
```

Create the asset via:
**Right-click in Project → Create → Game → Shared Loading State**
Save it anywhere (e.g. `Assets/Resources/SharedLoadingState.asset`).

---

### [NEW] `AsyncLoadCoordinator.cs`

The orchestrator. Lives on a new empty GameObject in `TerrainGenDemo`.

**Location:** `Assets/Game/Script/Core/AsyncLoadCoordinator.cs`

**Inspector fields:**

| Field | Type | Notes |
|-------|------|-------|
| `debugGameplaySceneName` | `string` | Must match scene name exactly: `"Scene_Debug_Gameplay"` |
| `renderController` | `RenderController` | Drag the existing RenderController here |
| `terrainCamera` | `Camera` | The TerrainGenDemo main Camera (disabled while DebugGameplay is active) |
| `loadingState` | `SharedLoadingState` | The shared ScriptableObject asset |

**`Start()` coroutine — step by step:**

1. `loadingState.Reset()` — clear old state
2. Disable `terrainCamera` — so only DebugGameplay camera renders
3. `LoadSceneAsync("Scene_Debug_Gameplay", Additive)` → `yield return`
4. **Gate 1:** `WaitUntil(() => loadingState.isChunksReady)` — terrain fully built
5. Set `loadingState.progress = 1f`, `statusMessage = "Press Y to enter world"`
6. **Gate 2:** `WaitUntil(() => loadingState.playerConfirmed)` — player pressed Y
7. `UnloadSceneAsync("Scene_Debug_Gameplay")` → `yield return`
8. Re-enable `terrainCamera`
9. `SceneManager.SetActiveScene(SceneManager.GetSceneByName("TerrainGenDemo"))`
10. `renderController.SpawnPlayerNow()` — **trigger player spawn now**
11. `loadingState.isComplete = true`

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    public class AsyncLoadCoordinator : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string debugGameplaySceneName = "Scene_Debug_Gameplay";

        [Header("References")]
        [SerializeField] private RenderController renderController;
        [SerializeField] private Camera terrainCamera;

        [Header("Shared State")]
        [SerializeField] private SharedLoadingState loadingState;

        private IEnumerator Start()
        {
            if (loadingState != null) loadingState.Reset();

            // 1. Hide terrain camera — only DebugGameplay camera should render
            if (terrainCamera != null)
                terrainCamera.gameObject.SetActive(false);

            // 2. Load the waiting-room scene on top
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(
                debugGameplaySceneName, LoadSceneMode.Additive);
            yield return loadOp;

            // 3. Gate 1 — wait until all terrain chunks are finalized
            yield return new WaitUntil(() =>
                loadingState != null && loadingState.isChunksReady);

            // 4. Notify player that world is ready — waiting for confirmation
            if (loadingState != null)
            {
                loadingState.progress = 1f;
                loadingState.statusMessage = "Press Y to enter world";
            }

            // 5. Gate 2 — wait for player to press Y in Scene_Debug_Gameplay
            yield return new WaitUntil(() =>
                loadingState != null && loadingState.playerConfirmed);

            // 6. Tear down the waiting-room scene
            yield return SceneManager.UnloadSceneAsync(debugGameplaySceneName);

            // 7. Restore terrain camera and activate the terrain scene
            if (terrainCamera != null)
                terrainCamera.gameObject.SetActive(true);

            Scene terrainScene = SceneManager.GetSceneByName("TerrainGenDemo");
            if (terrainScene.IsValid())
                SceneManager.SetActiveScene(terrainScene);

            // 8. NOW spawn the player (chunks are ready, debug scene is gone)
            if (renderController != null)
                renderController.SpawnPlayerNow();

            if (loadingState != null)
                loadingState.isComplete = true;
        }
    }
}
```

---

### [NEW] `DebugGameplayConfirm.cs`

Lives inside `Scene_Debug_Gameplay`. Watches for the Y key **only after** Gate 1 is
satisfied (chunks ready), then sets `playerConfirmed = true` to release Gate 2.

**Location:** `Assets/Game/Script/UI/DebugGameplayConfirm.cs`

**Inspector fields:**

| Field | Type | Notes |
|-------|------|-------|
| `loadingState` | `SharedLoadingState` | Same shared asset |
| `confirmPromptObject` | `GameObject` | UI panel/text shown when ready: *"Press Y to enter world"* |

```csharp
using UnityEngine;

public class DebugGameplayConfirm : MonoBehaviour
{
    [SerializeField] private SharedLoadingState loadingState;

    [Tooltip("GameObject shown only when chunks are ready and awaiting confirmation")]
    [SerializeField] private GameObject confirmPromptObject;

    private bool _confirmed = false;

    private void Update()
    {
        if (loadingState == null || _confirmed) return;

        // Show prompt only once chunks are ready
        if (confirmPromptObject != null)
            confirmPromptObject.SetActive(loadingState.isChunksReady);

        // Wait for player to press Y
        if (loadingState.isChunksReady && Input.GetKeyDown(KeyCode.Y))
        {
            _confirmed = true;
            loadingState.playerConfirmed = true;

            if (confirmPromptObject != null)
                confirmPromptObject.SetActive(false);
        }
    }
}
```

---

### [NEW] `LoadingProgressUI.cs`

UI component inside `Scene_Debug_Gameplay`. Reads `SharedLoadingState` every frame and
drives a `Slider` + label.

**Location:** `Assets/Game/Script/UI/LoadingProgressUI.cs`

**Inspector fields:**

| Field | Type |
|-------|------|
| `loadingState` | `SharedLoadingState` |
| `progressSlider` | `UnityEngine.UI.Slider` |
| `statusText` | `TMPro.TextMeshProUGUI` |
| `smoothSpeed` | `float` (default `5`) |

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingProgressUI : MonoBehaviour
{
    [SerializeField] private SharedLoadingState loadingState;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float smoothSpeed = 5f;

    private float displayedProgress = 0f;

    private void Update()
    {
        if (loadingState == null) return;

        displayedProgress = Mathf.MoveTowards(
            displayedProgress,
            loadingState.progress,
            smoothSpeed * Time.deltaTime);

        if (progressSlider != null)
            progressSlider.value = displayedProgress;

        if (statusText != null)
            statusText.text = loadingState.statusMessage;
    }
}
```

> The bar animates smoothly even though `SharedLoadingState.progress` advances in discrete
> chunk-sized jumps.

---

### [MODIFY] `RenderController.cs` — Defer spawn, expose `SpawnPlayerNow()`

#### New public properties / fields (add near top of class):

```csharp
[Header("Loading State (optional — assign SharedLoadingState asset)")]
[SerializeField] private SharedLoadingState loadingState;

/// <summary>True once every terrain chunk has been finalized.</summary>
public bool AllChunksReady { get; private set; } = false;

/// <summary>True once the player has been placed in the world.</summary>
public bool PlayerSpawnComplete { get; private set; } = false;
```

#### New import (add at the top of the file if not present):

```csharp
using System.Linq;
```

#### Changes inside `FinalizeChunk()` — after `terrainChunks[coord] = chunkObj;`:

```csharp
// Report real chunk-building progress to SharedLoadingState
if (loadingState != null)
{
    int totalExpected = maxChunkX * maxChunkZ;
    int builtSoFar = terrainChunks.Count(kv => kv.Value != null);
    loadingState.progress = Mathf.Lerp(0.1f, 0.95f, (float)builtSoFar / totalExpected);
    loadingState.statusMessage = $"Building terrain... {builtSoFar}/{totalExpected} chunks";
}
```

#### Where spawn was previously triggered automatically — **remove or guard it**:

Find the location in `RenderController` where `SpawnPlayerSequence()` (or equivalent)
was called automatically after the last chunk. Change it to:

```csharp
// All chunks done — signal Gate 1 but DO NOT spawn yet.
// AsyncLoadCoordinator will call SpawnPlayerNow() after the player confirms.
AllChunksReady = true;
if (loadingState != null)
{
    loadingState.isChunksReady = true;
    // Leave progress at 0.95f — AsyncLoadCoordinator will set it to 1.0f
}
// Do NOT call SpawnPlayerSequence() here anymore.
```

#### New public method — called by `AsyncLoadCoordinator` after debug scene unloads:

```csharp
/// <summary>
/// Called by AsyncLoadCoordinator once Scene_Debug_Gameplay has been unloaded
/// and the player has confirmed they are ready to enter the world.
/// </summary>
public void SpawnPlayerNow()
{
    StartCoroutine(SpawnPlayerSequence());
}
```

> `SpawnPlayerSequence()` should set `PlayerSpawnComplete = true` at its end,
> exactly as it did before — `GameplaySceneInitializer` still polls that flag.

---

## Scene Setup Checklist

### TerrainGenDemo

- [ ] Create empty GameObject → name: `"AsyncLoadCoordinator"`
- [ ] Attach `AsyncLoadCoordinator.cs`
- [ ] Assign `renderController` → existing `RenderController` GameObject
- [ ] Assign `terrainCamera` → the scene's main `Camera`
- [ ] Assign `loadingState` → the `SharedLoadingState` asset
- [ ] Also assign `loadingState` on the `RenderController` → new `loadingState` field
- [ ] Add `Scene_Debug_Gameplay` to **Build Settings**
  *(File → Build Settings → drag scene in or click "Add Open Scenes" while it's open)*

### Scene_Debug_Gameplay

- [ ] Create a Canvas with a `Slider` and a `TextMeshProUGUI` label
- [ ] Attach `LoadingProgressUI.cs` to a UI Manager object
- [ ] Assign `loadingState` → same `SharedLoadingState` asset
- [ ] Assign `progressSlider` and `statusText`
- [ ] Create a separate **Confirm Prompt** UI panel (e.g. text: *"Press Y to enter world"*)
  — start it **disabled**
- [ ] Attach `DebugGameplayConfirm.cs` to a UI Manager object
- [ ] Assign `loadingState` → same `SharedLoadingState` asset
- [ ] Assign `confirmPromptObject` → the confirm prompt panel
- [ ] Confirm the scene has its own active `Camera`

---

## Verification Checklist

- [ ] Start from Menu → select save → Load → `Scene_Debug_Gameplay` appears immediately
- [ ] Progress bar climbs from ~10% → 95% in real chunk increments
- [ ] Once all chunks are done: bar snaps to 100%, *"Press Y to enter world"* prompt appears
- [ ] Pressing **Y** triggers unload of `Scene_Debug_Gameplay`
- [ ] Player spawns in terrain scene **after** debug scene has fully unloaded
- [ ] Camera switches cleanly — no frame with both cameras rendered simultaneously
- [ ] `GameplaySceneInitializer` restores stats / inventory / equipment correctly after transition
- [ ] Test **New World** and **Load Save** paths
- [ ] Open Hierarchy after transition — confirm no leftover `Scene_Debug_Gameplay` objects

---

## Notes

- `SaveLoadService.Instance` is guaranteed non-null when `RenderController.Start()` runs
  because it persists from the Menu scene via `DontDestroyOnLoad`. No changes needed.
- The `TerrainGenDemo` camera **must** be disabled before `Scene_Debug_Gameplay`'s camera
  wakes, otherwise Unity renders both simultaneously. `AsyncLoadCoordinator.Start()` handles
  this in its very first line.
- `Scene_Debug_Gameplay` is confirmed self-contained and has no dependency on TerrainGenDemo
  services.
- `DebugGameplayConfirm` guards itself with `_confirmed` so the Y key cannot fire twice
  even if the player holds it.
- **Progress mapping:** chunks report `0.10 → 0.95`. `AsyncLoadCoordinator` sets `1.0`
  when Gate 1 opens — giving a clean visual "done" moment before the confirm prompt shows.
