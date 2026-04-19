# Async Loading System Overview

## Purpose

When the player loads a world from the Menu, terrain generation takes time. Instead of a static screen, `Scene_Debug_Gameplay` loads additively on top of `TerrainGenDemo` so the player has something interactive while the world builds.

## Two-Gate Flow

| Gate | Condition | Effect |
|------|-----------|--------|
| **Gate 1 — Chunks Ready** | `RenderController` finalizes all terrain chunks | Progress bar hits 100%, "Press Y to enter world" prompt appears |
| **Gate 2 — Player Confirm** | Player presses **Y** in `Scene_Debug_Gameplay` | Debug scene unloads → player spawns in terrain scene |

Player spawning is intentionally deferred until after the player confirms (Gate 2), so `RenderController` does not auto-spawn — it waits for `AsyncLoadCoordinator.SpawnPlayerNow()`.

## Runtime Flow

```
[Menu Scene]
  WorldSelectionUI → SaveLoadService.LoadWorld(guid)
  SceneManager.LoadScene("TerrainGenDemo")
        |
[TerrainGenDemo boots]
  GameServiceBootstrapper.Awake()    ← registers all services
  RenderController.Start()          ← queues terrain generation (no auto-spawn)
  AsyncLoadCoordinator.Start()      ← orchestrator
        |                  \
        |                   [Scene_Debug_Gameplay loads additively]
        |                     LoadingProgressUI reads SharedLoadingState each frame
        |                             |
        v                             | ← Gate 1: all chunks built
  RenderController: AllChunksReady=true   SharedLoadingState.isChunksReady = true
        |                             |
        |                             | ← Gate 2: player presses Y
        |                   DebugGameplayConfirm → SharedLoadingState.playerConfirmed = true
        v                             v
  AsyncLoadCoordinator: unloads Scene_Debug_Gameplay
  Re-enables TerrainGenDemo camera
  RenderController.SpawnPlayerNow()
        |
[TerrainGenDemo live]
  GameplaySceneInitializer resumes (waits on PlayerSpawnComplete)
  Restores stats / inventory / equipment
```

## Key Components

| Component | Location | Role |
|-----------|----------|------|
| `AsyncLoadCoordinator` | `Core/AsyncLoadCoordinator.cs` | Orchestrates the two-gate flow |
| `SharedLoadingState` | `Core/SharedLoadingState.cs` | ScriptableObject data bus between scenes |
| `LoadingProgressUI` | `UI/LoadingProgressUI.cs` | Reads `SharedLoadingState.progress` to drive a slider + label |
| `DebugGameplayConfirm` | `UI/DebugGameplayConfirm.cs` | Watches for Y key after Gate 1; sets `playerConfirmed` |
| `RenderController` | `Assets/TerrainGenerator/` | Reports chunk progress; exposes `SpawnPlayerNow()` |

## SharedLoadingState Fields

| Field | Set by | Meaning |
|-------|--------|---------|
| `progress` | `RenderController` (0.1→0.95), `AsyncLoadCoordinator` (1.0) | Progress bar value |
| `statusMessage` | `RenderController`, `AsyncLoadCoordinator` | UI label text |
| `isChunksReady` | `RenderController` | Gate 1 open |
| `playerConfirmed` | `DebugGameplayConfirm` | Gate 2 open |
| `isComplete` | `AsyncLoadCoordinator` | Full transition done |

## Progress Mapping

Chunks report `0.10 → 0.95`. `AsyncLoadCoordinator` sets `1.0` when Gate 1 opens, giving a clean "done" moment before the confirm prompt shows.

## Key Design Constraints

- `TerrainGenDemo` camera is disabled before `Scene_Debug_Gameplay`'s camera wakes — both must not render simultaneously.
- `Scene_Debug_Gameplay` is self-contained; it has no dependency on `TerrainGenDemo` services.
- `SaveLoadService` persists via `DontDestroyOnLoad` from the Menu scene — available when `RenderController.Start()` runs.
