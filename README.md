# This is so PEAK

This is a Unity 3D survival/hiking game with procedural terrain, systemic gameplay
services, and dynamic environmental events.

## Overview

- Procedural terrain generation and previews live in [Assets/TerrainGenerator](Assets/TerrainGenerator).
- Core gameplay systems and services live in [Assets/Game/Script](Assets/Game/Script).
- Environmental systems include day/night, landslides, and tornado events.
- Pathfinding utilities live in [Assets/HJB](Assets/HJB).

## Unity Version

- 6000.3.9f1

## Quick Start

1. Install Unity 6000.3.9f1 in Unity Hub.
2. Open this folder as a Unity project.
3. Open a scene (for example [Assets/TerrainGenerator/TerrainGenDemo.unity](Assets/TerrainGenerator/TerrainGenDemo.unity)).
4. Press Play.

## Project Layout

- [Assets/Game/Script](Assets/Game/Script) - gameplay code, services, UI, and systems.
- [Assets/TerrainGenerator](Assets/TerrainGenerator) - procedural terrain generation pipeline.
- [Assets/HJB](Assets/HJB) - HJB pathfinding implementation and docs.
- [ProjectSettings](ProjectSettings) - Unity project settings.
- [Packages](Packages) - Unity package manifest and dependencies.
- [InputSystem_Actions.inputactions](InputSystem_Actions.inputactions) - input actions asset.

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

