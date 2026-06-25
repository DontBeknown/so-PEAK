# This is so PEAK

This is a Unity 3D survival/hiking game with procedural terrain, systemic gameplay
services, and dynamic environmental events.

## Overview

- Procedural terrain generation and previews live in [Assets/TerrainGenerator](Assets/TerrainGenerator).
- Core gameplay systems and services live in [Assets/Game/Script](Assets/Game/Script).
- Environmental systems include day/night, landslides, and tornado events.
- Pathfinding utilities live in [Assets/HJB](Assets/HJB).

## Requirements

- Unity Hub
- Unity Editor 6000.3.9f1
- Internet connection for Unity Package Manager dependencies
- Git, only if you install with the clone method

## Unity Version

- 6000.3.9f1

## Setup / Installation

You can install the project by downloading the repository as a ZIP file or by cloning
the repository with Git.

### Method 1: Download Repository ZIP

1. Open the repository page:
   [https://github.com/DontBeknown/so-PEAK](https://github.com/DontBeknown/so-PEAK)
2. Click **Code**.
3. Click **Download ZIP**.
4. Extract the ZIP file to a folder on your computer.
5. Open Unity Hub.
6. Click **Add** or **Open**.
7. Select the extracted project folder.
8. Open the project with Unity Editor 6000.3.9f1.
9. Wait for Unity to import assets and restore packages.

### Method 2: Clone Repository With Git

1. Open a terminal or command prompt.
2. Clone the repository:

   ```bash
   git clone https://github.com/DontBeknown/so-PEAK.git
   ```

3. Open Unity Hub.
4. Click **Add** or **Open**.
5. Select the cloned `so-PEAK` folder.
6. Open the project with Unity Editor 6000.3.9f1.
7. Wait for Unity to import assets and restore packages from
   [Packages/manifest.json](Packages/manifest.json).

## How to Play

1. Open the project in Unity.
2. Open [Assets/Scenes/Scene_Menu.unity](Assets/Scenes/Scene_Menu.unity).
3. Press **Play** in the Unity Editor.

Use `Scene_Menu.unity` to start and play the whole game. The scene
[Assets/TerrainGenerator/TerrainGenDemo.unity](Assets/TerrainGenerator/TerrainGenDemo.unity)
is mainly for terrain generation demo/testing.

The first import can take several minutes because Unity needs to generate the
`Library` folder, compile scripts, import assets, and download package dependencies.

## Troubleshooting

- Use Unity Editor 6000.3.9f1 if possible. Other Unity versions may upgrade project
  files or cause package differences.
- If Unity packages fail to load, close and reopen the project. You can also open
  **Window > Package Manager** and let Unity restore missing packages.
- Do not manually copy generated Unity folders such as `Library`, `Temp`, `Logs`,
  or `UserSettings`. Unity recreates these folders when the project opens.
- Generated IDE files such as `.csproj`, `.sln`, and `.user` files are also
  recreated automatically.

## Project Layout

- [Assets/Game/Script](Assets/Game/Script) - gameplay code, services, UI, and systems.
- [Assets/TerrainGenerator](Assets/TerrainGenerator) - procedural terrain generation pipeline.
- [Assets/HJB](Assets/HJB) - HJB pathfinding implementation and docs.
- [Assets/Scenes](Assets/Scenes) - main game scenes.
- [ProjectSettings](ProjectSettings) - Unity project settings.
- [Packages](Packages) - Unity package manifest and dependencies.
- [Assets/InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions) - input actions asset.

## Core Systems

- DI container: [Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs](Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs)
  registered in [Assets/Game/Script/Core/GameServiceBootstrapper.cs](Assets/Game/Script/Core/GameServiceBootstrapper.cs).
- Event bus: [Assets/Game/Script/Core/Events/IEventBus.cs](Assets/Game/Script/Core/Events/IEventBus.cs).
- Async loading: [Assets/Game/Script/Core/AsyncLoadCoordinator.cs](Assets/Game/Script/Core/AsyncLoadCoordinator.cs).
- Player state machine: [Assets/Game/Script/Player/PlayerControllerRefactored.cs](Assets/Game/Script/Player/PlayerControllerRefactored.cs).
- Interaction base: [Assets/Game/Script/Interaction/Core/HoldInteractableBase.cs](Assets/Game/Script/Interaction/Core/HoldInteractableBase.cs).
- Environment systems:
  - Day/night: [Assets/Game/Script/Environment/DayNight/DayNightCycleManager.cs](Assets/Game/Script/Environment/DayNight/DayNightCycleManager.cs)
  - Landslides: [Assets/Game/Script/Environment/Landslide/LandslideRockSpawner.cs](Assets/Game/Script/Environment/Landslide/LandslideRockSpawner.cs)
  - Tornado events: [Assets/TerrainGenerator/NaturalEvent/TornadoSpawner.cs](Assets/TerrainGenerator/NaturalEvent/TornadoSpawner.cs)
- Tutorials: [Assets/Game/Script/Tutorial/TutorialManager.cs](Assets/Game/Script/Tutorial/TutorialManager.cs)

## Documentation

- Architecture overview: [CODEBASE_ARCHITECTURE_OVERVIEW.md](CODEBASE_ARCHITECTURE_OVERVIEW.md)
- Dependency map: [CODEBASE_DEPENDENCY_MAP.md](CODEBASE_DEPENDENCY_MAP.md)
- Mermaid diagrams: [CODEBASE_MERMAID_DIAGRAMS.md](CODEBASE_MERMAID_DIAGRAMS.md)
- HJB notes: [Assets/HJB/HJB_HOW_IT_WORKS.md](Assets/HJB/HJB_HOW_IT_WORKS.md)
- HJB optimization: [Assets/HJB/HJB_OPTIMIZATION.md](Assets/HJB/HJB_OPTIMIZATION.md)

## Development Notes

- Register services in [Assets/Game/Script/Core/GameServiceBootstrapper.cs](Assets/Game/Script/Core/GameServiceBootstrapper.cs)
  and resolve via [Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs](Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs).
- Route cross-system communication through [Assets/Game/Script/Core/Events/IEventBus.cs](Assets/Game/Script/Core/Events/IEventBus.cs).
- Prefer extending [Assets/Game/Script/Interaction/Core/HoldInteractableBase.cs](Assets/Game/Script/Interaction/Core/HoldInteractableBase.cs)
  for hold-to-interact behaviors.
