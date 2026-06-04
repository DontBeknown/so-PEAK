# Landslide System Overview

## Components

| Component | Role |
|-----------|------|
| `LandslideRockSpawner` | Public trigger API, spawn sequencing, rock pooling, high-level phases |
| `LandslideRockBehavior` | Per-rock collision, damage, push impulse, impact decals/FX, recycle requests |
| `LandslideDecalService` | Pools decals/FX; fades and returns them during cleanup |
| `LandslideShakeController` | Finds Cinemachine Perlin components; transitions shake amplitude |
| `DebugLandslideHoldInteractable` | Debug hold-interaction that calls spawner trigger methods |

## Public Trigger API

`LandslideRockSpawner` exposes three entry points:

```csharp
spawner.TriggerLandslide();                        // uses configured spawnAnchors
spawner.Spawn(anchor, biome);                      // single provided Transform
spawner.TriggerLandslideAtPosition(worldPos);      // creates a temporary anchor at position
```

`DebugLandslideHoldInteractable` calls `TriggerLandslideAtPosition(transform.position)` when `triggerAtThisObjectPosition = true`, otherwise `TriggerLandslide()`.

## Runtime Flow

1. `Awake`: resolves EventBus, validates target layer, prewarms rock pool.
2. Spawner ensures `LandslideDecalService` and `LandslideShakeController` exist (auto-adds if missing).
3. Decal pool is configured/prewarmed; shake controller caches Perlin noise components.
4. Trigger method starts `SpawnRoutine` (or `SpawnRoutineWithCleanup` for temporary anchors).
5. **Phase 1:** anchor crack + rumble sounds play; shake transitions to anchor amplitude; pre-decal cracks spawn around each anchor.
6. Wait `anchorToRockStartDelay`.
7. **Phase 2:** shake transitions to stronger amplitude; hard rumble loop starts repeating.
8. For each anchor, spawns `rocksPerAnchorRange` rocks with random speed, direction bias, and angular velocity.
9. Each rock on collision can:
   - Spawn one impact decal/FX (one-shot per rock, with optional arm delay)
   - Deal velocity-scaled damage
   - Apply push impulse to `Rigidbody` or `CharacterController` targets
10. Rock requests recycle when timed out or sleeping long enough.
11. When active rock count reaches zero: hard rumble stops, shake fades out, all tracked decals/FX fade and return to pool.

## Key Parameters

### Spawn & Launch
- `spawnAnchors` — trigger points for `TriggerLandslide()`
- `rocksPerAnchorRange` — random count of rocks per anchor
- `horizontalScatter`, `heightJitter` — spawn position randomization
- `delayBetweenRocks` — interval between each rock
- `launchSpeedRange`, `downwardBias`, `angularSpeedRange` — launch force profile

### Camera Shake
- `anchorPhaseShakeAmplitude` / `rockSpawnPhaseShakeAmplitude` — shake amounts per phase
- `shakeTransitionDuration` / `shakeFadeOutDuration` — blend times

### Damage
- `minImpactDamage`, `maxImpactDamage`, `minDamageVelocity`, `maxDamageVelocity` — velocity-to-damage curve
- `pushImpulse` — impulse strength for rigidbody/controller targets
- `hitCooldownSeconds` — per-rock cooldown between hit evaluations

Damage formula: `Lerp(min, max, InverseLerp(minVel, maxVel, speed)) × damageMultiplier`. Zero damage if speed < `minDamageVelocity`.

### Pooling
- `prewarmCount`, `maxPoolSize` — rock pool capacity
- `recycleAfterSeconds` / `sleepRecycleDelaySeconds` — recycle triggers
- `recycleScaleDownDuration` — visual shrink before return to pool
- `decalPoolPrewarmCount`, `maxDecalPoolSizePerPrefab` — decal pool capacity

### Audio
All audio published through `IEventBus` as positional SFX events:
- `phaseOneAnchorCrackSoundId` / `phaseOneAnchorRumbleSoundId`
- `phaseTwoHardRumbleSoundId` / `phaseTwoHardRumbleRepeatInterval`
- `impactDecalSoundId`

## Rock Behavior Details

- Damage permission and decal permission checked against separate layer masks (`_damageLayers`, `_decalSpawnLayers`).
- Impact decal/FX is one-shot per rock (`_hasSpawnedImpactDecal`); can be delayed via `_impactDecalSpawnDelay`.
- CharacterController push uses a helper component with damping for smooth movement.

## Integration Notes

- Spawner requires at least one valid trigger strategy: configured `spawnAnchors`, or a direct `Spawn`/`TriggerLandslideAtPosition` call.
- If `spawnedRockLayerName` is invalid, spawner falls back to its own layer.
- `LandslideDecalService` and `LandslideShakeController` are auto-added to the spawner's GameObject if missing.
