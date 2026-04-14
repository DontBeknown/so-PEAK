# Landslide System Guide

This guide explains:
- The landslide execution flow
- Important parameters and what they control
- How to trigger landslides from code and interactables
- How damage, decals, shake, audio, and pooling work together

## Main Components

- `LandslideRockSpawner`: Owns trigger API, spawn sequencing, rock pooling, and high-level phases.
- `LandslideRockBehavior`: Per-rock collision, damage, push impulse, impact decals/fx, and recycle requests.
- `LandslideDecalService`: Pools decals/fx and fades/returns them during cleanup.
- `LandslideShakeController`: Finds Cinemachine Perlin components and transitions shake amplitude.
- `DebugLandslideHoldInteractable`: Debug hold interaction that calls spawner trigger methods.

## Public Trigger API (How To Call)

`LandslideRockSpawner` exposes three entry points:

1. `TriggerLandslide()`
   Uses configured `spawnAnchors`.

2. `TriggerLandslideAt(Transform anchor)`
   Uses a single provided anchor transform.

3. `TriggerLandslideAtPosition(Vector3 position)`
   Creates a temporary anchor object at world position and runs the same flow.

### Example: Direct code call

```csharp
using Game.Environment.Landslide;
using UnityEngine;

public class LandslideTriggerExample : MonoBehaviour
{
    [SerializeField] private LandslideRockSpawner spawner;
    [SerializeField] private Transform optionalAnchor;

    public void TriggerDefault()
    {
        spawner.TriggerLandslide();
    }

    public void TriggerAtAnchor()
    {
        spawner.TriggerLandslideAt(optionalAnchor);
    }

    public void TriggerAtPosition(Vector3 worldPos)
    {
        spawner.TriggerLandslideAtPosition(worldPos);
    }
}
```

### Example: Existing debug interactable

`DebugLandslideHoldInteractable` does:
- `TriggerLandslideAtPosition(transform.position)` when `triggerAtThisObjectPosition = true`
- otherwise `TriggerLandslide()`

## End-To-End Runtime Flow

1. `Awake` on spawner resolves EventBus, validates target layer, prewarms rock pool.
2. Spawner ensures collaborators (`LandslideDecalService`, `LandslideShakeController`) exist.
3. Decal pool is configured/prewarmed, shake controller caches Perlin components.
4. Trigger method starts `SpawnRoutine` (or `SpawnRoutineWithCleanup` for temporary anchors).
5. Phase 1: play anchor crack + rumble sounds; transition to anchor shake amplitude.
6. Phase 1 decals: pre-decal cracks are spawned around each anchor.
7. Wait `anchorToRockStartDelay`.
8. Phase 2: transition to stronger shake and start repeating hard rumble loop.
9. For each anchor, spawn random count (`rocksPerAnchorRange`) of rocks.
10. Each rock is launched with random speed, direction bias, and angular velocity.
11. Rock collisions can:
    - Spawn one impact decal/fx (first valid impact after optional arm delay)
    - Deal velocity-scaled damage
    - Apply push impulse to Rigidbody or CharacterController targets
12. Rock asks spawner to recycle when timed out or sleeping long enough.
13. When active rocks reach zero:
    - hard rumble loop stops
    - shake fades out
    - all tracked decals/fx fade and return to pool

## Parameters Explained

## Spawner: Core Trigger + Spawn

- `rockPrefab`, `randomRockPrefabDamageMap`
  Source prefabs. Optional map allows prefab-specific damage/decal multipliers.
- `spawnAnchors`
  Main trigger points for `TriggerLandslide()`.
- `rocksPerAnchorRange`
  Random number of rocks per anchor.
- `horizontalScatter`, `heightJitter`
  Spawn position randomization around anchor.
- `delayBetweenRocks`
  Interval between each spawned rock.
- `launchSpeedRange`, `downwardBias`, `angularSpeedRange`
  Launch force profile and spin randomness.

## Spawner: Camera Shake

- `anchorPhaseShakeAmplitude`
  Shake amount before rocks spawn.
- `rockSpawnPhaseShakeAmplitude`
  Stronger shake during active rock spawn.
- `shakeTransitionDuration`
  Blend time between shake amplitudes.
- `shakeFadeOutDuration`
  Blend time to return shake to zero.

## Spawner: Decal/FX and Cleanup

- `impactDecalProjectorPrefab`, `impactFxPrefab`, `impactDecalMaterials`
  Impact visual assets.
- `impactDecalRevealDuration`, `impactDecalHoldDuration`, `impactDecalFadeDuration`, `impactDecalSpawnDelay`
  Impact timing.
- `anchorDecalCountRange`, `anchorDecalScatter`, `anchorDecalWidthRange`
  Anchor pre-decal amount/distribution/size.
- `anchorDecalProbeHeight`, `anchorDecalProbeDistance`, `anchorDecalSurfaceMask`, `anchorDecalSurfaceOffset`
  Anchor pre-decal ground placement raycast config.
- `anchorToRockStartDelay`
  Delay between pre-decal phase and first rock spawn.
- `delayBetweenDecalCleanup`
  Gap between sequential cleanup operations.
- `decalPoolPrewarmCount`, `maxDecalPoolSizePerPrefab`
  Decal pooling capacity.

## Spawner: Rock Pool

- `prewarmCount`, `maxPoolSize`
  Rock pool warmup and hard limit.
- `recycleAfterSeconds`
  Absolute max lifetime before recycle request.
- `sleepRecycleDelaySeconds`
  Recycle if sleeping this long.
- `recycleScaleDownDuration`
  Visual shrink before returning rock to pool.

## Spawner: Interaction and Damage Curve

- `interactionLayers`
  Layers eligible for damage and push checks.
- `decalSpawnLayers`
  Layers that allow decal spawning.
- `minImpactDamage`, `maxImpactDamage`
  Damage bounds.
- `minDamageVelocity`, `maxDamageVelocity`
  Speed window for damage lerp.
- `pushImpulse`
  Impulse strength for rigidbody/controller push.
- `hitCooldownSeconds`
  Per-rock cooldown between hit evaluations.

Damage function in `LandslideRockBehavior`:

$$
\text{damage}=\text{Lerp}(\text{minImpactDamage},\text{maxImpactDamage},t)\times\text{damageMultiplier}
$$

where

$$
t=\text{InverseLerp}(\text{minDamageVelocity},\text{maxDamageVelocity},\text{speed})
$$

and if speed is below `minDamageVelocity`, damage is `0`.

## Spawner: Audio

- `phaseOneAnchorCrackSoundId`, `phaseOneAnchorCrackVolumeScale`
- `phaseOneAnchorRumbleSoundId`, `phaseOneAnchorRumbleVolumeScale`
- `phaseTwoHardRumbleSoundId`, `phaseTwoHardRumbleVolumeScale`, `phaseTwoHardRumbleRepeatInterval`
- `impactDecalSoundId`, `impactDecalSoundVolumeScale`

Audio is published through `IEventBus` as positional SFX events.

## Rock Behavior Internals

- Collision checks are split into:
  - damage permission (`_damageLayers`)
  - decal permission (`_decalSpawnLayers`)
- Impact decal/fx is one-shot per rock (`_hasSpawnedImpactDecal`).
- Decal can be delayed by `_impactDecalSpawnDelay` using `_impactDecalArmedAtTime`.
- Push supports both Rigidbody and CharacterController targets.
- CharacterController push uses a helper component with damping for smooth movement.

## Integration Notes

- Ensure spawner has valid rock prefab(s) and at least one trigger strategy:
  - configured `spawnAnchors`, or
  - trigger via `TriggerLandslideAt(...)` / `TriggerLandslideAtPosition(...)`
- If `spawnedRockLayerName` is invalid, spawner falls back to its own layer.
- `LandslideDecalService` and `LandslideShakeController` are auto-added if missing.

## Quick Setup Checklist

1. Add `LandslideRockSpawner` to a scene object.
2. Assign rock prefab(s), decal/fx prefabs, and optional materials.
3. Assign `spawnAnchors` for default trigger mode.
4. Tune damage/velocity and layer masks.
5. Tune shake and audio IDs.
6. Add `DebugLandslideHoldInteractable` (optional) and bind spawner for manual test.
7. Play and validate:
   - anchor pre-decals
   - rock spawning and collisions
   - damage/push behavior
   - cleanup and pooling
