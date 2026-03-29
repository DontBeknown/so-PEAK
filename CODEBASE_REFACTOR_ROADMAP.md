# CODEBASE REFACTOR ROADMAP

Date: 2026-03-29
Scope: Whole codebase
Primary goal: Architecture cleanup and maintainability
Timeline: 2-3 months
Behavior policy: Behavior changes are allowed if they improve architecture
Constraint: Keep single Assembly-CSharp (no asmdef split for now)

## Objectives

1. Make startup and service lifecycles deterministic.
2. Remove hidden singleton/static coupling across core systems.
3. Decouple gameplay, interaction, UI, and persistence boundaries.
4. Add regression protection to safely continue refactoring.
5. Improve save/load robustness and long-term evolvability.

## Guiding Principles

- Keep changes incremental and reversible.
- Protect player-facing flows with tests before high-risk edits.
- Preserve data compatibility during persistence refactors.
- Prefer explicit interfaces and events over direct component lookups.
- Update architecture docs at the end of each phase.

## Phase Plan

## Phase 1 - Baseline and Guardrails (Week 1-2)

### Goals
- Capture current runtime architecture behavior.
- Establish safety nets before structural changes.

### Work
1. Build baseline maps:
   - Startup sequence and ownership chain.
   - Service registration and resolution graph.
   - Event publish/subscribe map.
   - Save/load call graph and hydration sequence.
2. Add regression tests for critical loops:
   - Player state transitions.
   - Hold interaction and prompt/progress flow.
   - Inventory and equipment operations.
   - Save -> load -> save roundtrip integrity.
3. Add PlayMode scene smoke tests:
   - Scene_Menu
   - Scene_Debug_Gameplay

### Exit Criteria
- Baseline docs are complete and reviewed.
- Core smoke tests are green.
- No high-risk refactor starts without test coverage for affected flows.

## Phase 2 - Composition Root Hardening (Week 3-4)

### Goals
- Centralize object creation and lifecycle ownership.
- Remove hidden runtime instantiation paths.

### Work
1. Harden bootstrap path:
   - Keep service registration deterministic in bootstrap + scene initializer.
   - Remove service self-instantiation and hidden startup side effects.
2. Service ownership rules:
   - One owner per service lifetime.
   - Explicit startup/shutdown responsibility.
3. Add diagnostics for service graph health:
   - Missing registration checks.
   - Duplicate registration warnings.

### Exit Criteria
- Service creation path is explicit and stable.
- Runtime no longer depends on hidden singleton spin-up.
- Bootstrap diagnostics pass in both scenes.

## Phase 3 - UI and Interaction Boundary Refactor (Week 5-6)

### Goals
- Stabilize UI lifecycle and remove race-prone coupling from interaction flow.

### Work
1. UI lifecycle stability:
   - Prevent panel/service recreation during player reference refresh.
   - Keep panel state ownership consistent through scene and runtime transitions.
2. Interaction-to-UI contract cleanup:
   - Move direct dependencies behind narrow interfaces/events.
   - Enforce one owner for prompt/progress visibility state.
3. Preserve user experience while reducing coupling:
   - Keep behavior equivalent unless intentional architecture-driven improvements are made.

### Exit Criteria
- No stale or recreated panel service references during gameplay.
- Interaction prompt/progress behavior is deterministic.
- UI interaction tests pass under repeated open/close and hold/cancel cycles.

## Phase 4 - Domain Decoupling (Week 7-9)

### Goals
- Reduce tight coupling across player, stats, inventory, and equipment systems.

### Work
1. Replace direct cross-component lookups with explicit interfaces/events.
2. Keep controller API stable while extracting orchestration to focused services.
3. Normalize event lifecycle hygiene:
   - Subscription ownership rules.
   - Guaranteed unsubscribe on disable/destroy/scene transition.

### Exit Criteria
- Core domain managers collaborate through contracts, not hidden lookups.
- Event subscriptions are balanced across scene reloads.
- Player loop remains functionally stable in smoke and targeted tests.

## Phase 5 - Save/Load Robustness and Data Evolution (Week 10-11)

### Goals
- Make persistence flows explicit, complete, and version-safe.

### Work
1. Consolidate hydration into one documented restore pipeline.
2. Ensure dialog, collectable, and world state restoration is consistent.
3. Add schema evolution support:
   - Save version field.
   - Migration hooks for older save formats.
   - Compatibility assertions and fallback handling.

### Exit Criteria
- Save/load flows are deterministic and documented.
- Old and current save data pass compatibility checks.
- No data loss in repeated save/load cycles.

## Phase 6 - Operational Readiness (Week 12)

### Goals
- Lock in architecture quality and prevent regressions.

### Work
1. Add architecture conformance checks:
   - Ban new forbidden static access paths.
   - Ban new cross-layer direct lookup patterns where contracts are required.
2. Add runtime diagnostics:
   - Service and event health counters.
   - Warning surfaces for lifecycle misuse.
3. Final documentation pass:
   - Dependency map updates.
   - Mermaid diagram updates.
   - Refactor completion checklist.

### Exit Criteria
- Conformance checks integrated and passing.
- Observability is sufficient to detect lifecycle regressions quickly.
- Architecture docs match implementation.

## Risk Register

1. Core bootstrap regressions can cascade widely.
   - Mitigation: Small slices, compile/test after each slice, strict ownership rules.
2. Save/load refactors can corrupt player progress.
   - Mitigation: Roundtrip tests, fixture saves, compatibility harness.
3. UI and interaction race conditions can reappear during decoupling.
   - Mitigation: Deterministic state owner, stress tests for hold/cancel/panel transitions.
4. Event leaks can silently degrade performance.
   - Mitigation: Subscription audits and lifecycle instrumentation.

## Verification Gates (Every Phase)

1. Compile with no new errors/warnings in touched systems.
2. Run menu and gameplay scene smoke tests.
3. Run targeted tests for modified flows.
4. Validate event subscribe/unsubscribe balance.
5. Validate save/load roundtrip integrity for affected data.

## Suggested Workstream Order

1. Phase 1 baseline + tests.
2. Phase 2 composition root/lifecycle.
3. Phase 3 UI and interaction boundaries.
4. Phase 4 domain decoupling.
5. Phase 5 persistence evolution.
6. Phase 6 conformance + docs lock.

## Definition of Done

- Maintainability improves measurably (reduced coupling hotspots and clearer ownership).
- Critical gameplay loops and persistence are protected by automated tests.
- Architecture docs and code behavior are aligned.
- New features can be added without reintroducing hidden dependencies.
