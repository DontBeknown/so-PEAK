# Sound System — Implementation Plan

**Last Updated:** March 9, 2026  
**Status:** Not Started  
**Replaces:** `InteractionAudioManager.cs` (delete after Phase 3)

---

## Summary

A fully EventBus-driven sound system. Game systems **never call `SoundService` directly** — they publish typed events to `IEventBus`. `SoundEventListener` is the only class that resolves `SoundService` and calls it. `SoundService` is registered in `ServiceContainer` like every other service.

```
Game System  →  EventBus.Publish(new PlaySFXEvent(...))
                         ↓
               SoundEventListener.OnPlaySFX(event)
                         ↓
                  SoundService.PlaySFX(...)
                         ↓
               AudioSource (pooled) → AudioMixer
```

---

## File Structure

```
Assets/Game/Script/Sound/
├── SoundCategory.cs                  ← Enum: Music, SFX, Ambient, UI
├── Events/
│   └── SoundEvents.cs                ← All typed event classes
├── SoundConfig.cs                    ← ScriptableObject: pool size, fade duration, 3D settings
├── SoundLibrary.cs                   ← ScriptableObject: named AudioClip[] groups
├── SoundService.cs                   ← MonoBehaviour: pool, AudioMixer routing, fades
└── SoundEventListener.cs             ← Subscribes to EventBus → calls SoundService

Assets/Audio/
├── Mixer/
│   └── GameAudioMixer.mixer          ← Create in Unity Editor (Master → Music/SFX/Ambient/UI)
├── Config/
│   └── SoundConfig.asset             ← Create in Unity Editor via right-click → Create
└── Library/
    └── SoundLibrary.asset            ← Create in Unity Editor via right-click → Create
```

---

## Phase 1 — Data Layer

### Step 1 — `SoundCategory.cs`

```csharp
namespace Game.Sound
{
    public enum SoundCategory
    {
        SFX,
        Ambient,
        Music,
        UI
    }
}
```

---

### Step 2 — `Events/SoundEvents.cs`

All events the EventBus will carry. One file keeps them together.

```csharp
using UnityEngine;

namespace Game.Sound.Events
{
    // One-shot SFX at a position in world space (3D, pooled AudioSource)
    public class PlayPositionalSFXEvent
    {
        public string ClipId;        // Key into SoundLibrary
        public Vector3 Position;
        public float VolumeScale;    // Multiplier on top of SoundConfig default (1 = normal)

        public PlayPositionalSFXEvent(string clipId, Vector3 position, float volumeScale = 1f)
        {
            ClipId = clipId;
            Position = position;
            VolumeScale = volumeScale;
        }
    }

    // One-shot UI / 2D SFX (no position, spatialBlend = 0)
    public class PlayUISoundEvent
    {
        public string ClipId;
        public float VolumeScale;

        public PlayUISoundEvent(string clipId, float volumeScale = 1f)
        {
            ClipId = clipId;
            VolumeScale = volumeScale;
        }
    }

    // Swap the current music track (crossfades over SoundConfig.MusicCrossfadeDuration)
    public class PlayMusicEvent
    {
        public string ClipId;
        public bool Loop;

        public PlayMusicEvent(string clipId, bool loop = true)
        {
            ClipId = clipId;
            Loop = loop;
        }
    }

    public class StopMusicEvent { }

    // Swap the current ambient loop (crossfades over SoundConfig.AmbientCrossfadeDuration)
    public class PlayAmbientEvent
    {
        public string ClipId;

        public PlayAmbientEvent(string clipId) => ClipId = clipId;
    }

    public class StopAmbientEvent { }

    // Adjust AudioMixer exposed parameter for a category (0–1 range, converted to dB internally)
    public class SetVolumeEvent
    {
        public SoundCategory Category;
        public float NormalizedVolume; // 0–1

        public SetVolumeEvent(SoundCategory category, float normalizedVolume)
        {
            Category = category;
            NormalizedVolume = normalizedVolume;
        }
    }
}
```

---

### Step 3 — `SoundConfig.cs`

```csharp
using UnityEngine;

namespace Game.Sound
{
    [CreateAssetMenu(menuName = "Config/SoundConfig", fileName = "SoundConfig")]
    public class SoundConfig : ScriptableObject
    {
        [Header("SFX Pool")]
        [Tooltip("Number of pooled AudioSources created at startup")]
        public int PoolSize = 10;

        [Header("Default Volumes (0–1)")]
        [Range(0f, 1f)] public float DefaultSFXVolume    = 0.8f;
        [Range(0f, 1f)] public float DefaultUIVolume     = 0.7f;
        [Range(0f, 1f)] public float DefaultMusicVolume  = 0.6f;
        [Range(0f, 1f)] public float DefaultAmbientVolume= 0.5f;

        [Header("Crossfade Durations (seconds)")]
        public float MusicCrossfadeDuration   = 1.5f;
        public float AmbientCrossfadeDuration = 2f;

        [Header("3D Positional Audio")]
        [Tooltip("spatialBlend for pooled SFX sources (1 = full 3D)")]
        [Range(0f, 1f)] public float SpatialBlend = 1f;
        public float MinDistance = 1f;
        public float MaxDistance = 25f;
    }
}
```

---

### Step 4 — `SoundLibrary.cs`

Keyed clip groups. `SoundService` looks up a `ClipId` string and picks a random clip from the group, preventing repetition.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Sound
{
    [Serializable]
    public class SoundEntry
    {
        [Tooltip("Unique key used in events (e.g. \"footstep_walk\", \"item_pickup\")")]
        public string Id;
        [Tooltip("One or more clips — a random one is chosen each time")]
        public AudioClip[] Clips;
    }

    [CreateAssetMenu(menuName = "Config/SoundLibrary", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private SoundEntry[] _entries;

        private Dictionary<string, AudioClip[]> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, AudioClip[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _entries)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Clips is { Length: > 0 })
                    _lookup[entry.Id] = entry.Clips;
            }
        }

        // Returns a random clip for the given id, or null if not found
        public AudioClip Get(string id)
        {
            if (_lookup == null) BuildLookup();
            if (!_lookup.TryGetValue(id, out var clips)) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
```

---

## Phase 2 — Core Implementation

### Step 5 — Create `GameAudioMixer.mixer` in Unity

1. **Project window** → right-click → **Create → Audio Mixer** → name it `GameAudioMixer`
2. In the **Audio Mixer window** add four child groups under `Master`:
   - `Music`
   - `SFX`
   - `Ambient`
   - `UI`
3. For each group, right-click its `Volume` parameter → **Expose** → rename the exposed parameter:
   - `MusicVolume`, `SFXVolume`, `AmbientVolume`, `UIVolume`
4. Save the mixer (Ctrl+S)

> These exact parameter names are used in `SoundService` to set volume.

---

### Step 6 — `SoundService.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Game.Core.DI;
using Game.Core.Events;

namespace Game.Sound
{
    public class SoundService : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private SoundConfig config;
        [SerializeField] private SoundLibrary library;

        [Header("AudioMixer Group Names")]
        [SerializeField] private string sfxGroupName      = "SFX";
        [SerializeField] private string uiGroupName       = "UI";
        [SerializeField] private string musicGroupName    = "Music";
        [SerializeField] private string ambientGroupName  = "Ambient";

        [Header("AudioMixer Parameter Names")]
        [SerializeField] private string sfxVolumeParam    = "SFXVolume";
        [SerializeField] private string uiVolumeParam     = "UIVolume";
        [SerializeField] private string musicVolumeParam  = "MusicVolume";
        [SerializeField] private string ambientVolumeParam= "AmbientVolume";

        // Dedicated, non-pooled sources for continuous streams
        private AudioSource _musicSource;
        private AudioSource _musicSourceB;     // For crossfade double-buffering
        private AudioSource _ambientSource;
        private AudioSource _ambientSourceB;
        private AudioSource _uiSource;

        // SFX object pool
        private readonly Queue<AudioSource> _pool = new();
        private readonly List<AudioSource> _active = new();

        // AudioMixer group cache
        private AudioMixerGroup _sfxGroup;
        private AudioMixerGroup _uiGroup;
        private AudioMixerGroup _musicGroup;
        private AudioMixerGroup _ambientGroup;

        private void Awake()
        {
            CacheGroups();
            CreateDedicatedSources();
            InitPool();
        }

        // Called by GameServiceBootstrapper after Awake
        public void Initialize()
        {
            SetVolume(SoundCategory.Music,   config.DefaultMusicVolume);
            SetVolume(SoundCategory.SFX,     config.DefaultSFXVolume);
            SetVolume(SoundCategory.Ambient, config.DefaultAmbientVolume);
            SetVolume(SoundCategory.UI,      config.DefaultUIVolume);
        }

        // ───────────────────────────── Public API ─────────────────────────────

        public void PlayPositionalSFX(string clipId, Vector3 position, float volumeScale = 1f)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            var source = RentSource();
            source.transform.position = position;
            source.clip    = clip;
            source.volume  = config.DefaultSFXVolume * volumeScale;
            source.loop    = false;
            source.Play();
        }

        public void PlayUISound(string clipId, float volumeScale = 1f)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            _uiSource.PlayOneShot(clip, config.DefaultUIVolume * volumeScale);
        }

        public void PlayMusic(string clipId, bool loop = true)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            StopAllCoroutines();
            StartCoroutine(CrossfadeMusic(clip, loop));
        }

        public void StopMusic() => StartCoroutine(FadeOut(_musicSource, config.MusicCrossfadeDuration));

        public void PlayAmbient(string clipId)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            StartCoroutine(CrossfadeAmbient(clip));
        }

        public void StopAmbient() => StartCoroutine(FadeOut(_ambientSource, config.AmbientCrossfadeDuration));

        // normalizedVolume: 0–1, converted to decibels
        public void SetVolume(SoundCategory category, float normalizedVolume)
        {
            normalizedVolume = Mathf.Clamp01(normalizedVolume);
            float dB = normalizedVolume > 0.0001f ? 20f * Mathf.Log10(normalizedVolume) : -80f;

            string param = category switch
            {
                SoundCategory.Music   => musicVolumeParam,
                SoundCategory.SFX     => sfxVolumeParam,
                SoundCategory.Ambient => ambientVolumeParam,
                SoundCategory.UI      => uiVolumeParam,
                _                     => null
            };

            if (param != null)
                audioMixer.SetFloat(param, dB);
        }

        // ───────────────────────────── Pool ─────────────────────────────

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!_active[i].isPlaying)
                    ReturnSource(_active[i]);
            }
        }

        private void InitPool()
        {
            for (int i = 0; i < config.PoolSize; i++)
                CreatePooledSource();
        }

        private AudioSource CreatePooledSource()
        {
            var go = new GameObject($"SFX_Pool_{_pool.Count + _active.Count}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake   = false;
            src.spatialBlend  = config.SpatialBlend;
            src.minDistance   = config.MinDistance;
            src.maxDistance   = config.MaxDistance;
            src.outputAudioMixerGroup = _sfxGroup;
            _pool.Enqueue(src);
            return src;
        }

        private AudioSource RentSource()
        {
            var src = _pool.Count > 0 ? _pool.Dequeue() : CreatePooledSource();
            _active.Add(src);
            return src;
        }

        private void ReturnSource(AudioSource src)
        {
            src.Stop();
            src.clip = null;
            _active.Remove(src);
            _pool.Enqueue(src);
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private void CacheGroups()
        {
            _sfxGroup     = FindGroup(sfxGroupName);
            _uiGroup      = FindGroup(uiGroupName);
            _musicGroup   = FindGroup(musicGroupName);
            _ambientGroup = FindGroup(ambientGroupName);
        }

        private AudioMixerGroup FindGroup(string name)
        {
            var results = audioMixer.FindMatchingGroups(name);
            if (results.Length == 0) Debug.LogError($"[SoundService] AudioMixer group not found: {name}");
            return results.Length > 0 ? results[0] : null;
        }

        private AudioSource CreateDedicatedSource(string label, AudioMixerGroup group, float spatialBlend = 0f)
        {
            var go  = new GameObject(label);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake            = false;
            src.spatialBlend           = spatialBlend;
            src.outputAudioMixerGroup  = group;
            return src;
        }

        private void CreateDedicatedSources()
        {
            _musicSource   = CreateDedicatedSource("Music_A",   _musicGroup);
            _musicSourceB  = CreateDedicatedSource("Music_B",   _musicGroup);
            _ambientSource = CreateDedicatedSource("Ambient_A", _ambientGroup);
            _ambientSourceB= CreateDedicatedSource("Ambient_B", _ambientGroup);
            _uiSource      = CreateDedicatedSource("UI",        _uiGroup);
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip, bool loop)
        {
            float duration = config.MusicCrossfadeDuration;
            _musicSourceB.clip   = newClip;
            _musicSourceB.volume = 0f;
            _musicSourceB.loop   = loop;
            _musicSourceB.Play();

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = t / duration;
                _musicSource.volume  = 1f - ratio;
                _musicSourceB.volume = ratio;
                yield return null;
            }

            _musicSource.Stop();
            // Swap references so A is always the active track
            (_musicSource, _musicSourceB) = (_musicSourceB, _musicSource);
        }

        private IEnumerator CrossfadeAmbient(AudioClip newClip)
        {
            float duration = config.AmbientCrossfadeDuration;
            _ambientSourceB.clip   = newClip;
            _ambientSourceB.volume = 0f;
            _ambientSourceB.loop   = true;
            _ambientSourceB.Play();

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = t / duration;
                _ambientSource.volume  = 1f - ratio;
                _ambientSourceB.volume = ratio;
                yield return null;
            }

            _ambientSource.Stop();
            (_ambientSource, _ambientSourceB) = (_ambientSourceB, _ambientSource);
        }

        private IEnumerator FadeOut(AudioSource src, float duration)
        {
            float startVolume = src.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }
            src.Stop();
            src.volume = startVolume;
        }
    }
}
```

---

### Step 7 — `SoundEventListener.cs`

The **only** class that touches `SoundService`. Subscribes to EventBus events in `Start`, unsubscribes in `OnDestroy`.

```csharp
using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;

namespace Game.Sound
{
    public class SoundEventListener : MonoBehaviour
    {
        private IEventBus _eventBus;
        private SoundService _sound;

        private void Start()
        {
            _eventBus = ServiceContainer.Instance.Get<IEventBus>();
            _sound    = ServiceContainer.Instance.Get<SoundService>();

            _eventBus.Subscribe<PlayPositionalSFXEvent>(OnPlayPositionalSFX);
            _eventBus.Subscribe<PlayUISoundEvent>(OnPlayUISound);
            _eventBus.Subscribe<PlayMusicEvent>(OnPlayMusic);
            _eventBus.Subscribe<StopMusicEvent>(OnStopMusic);
            _eventBus.Subscribe<PlayAmbientEvent>(OnPlayAmbient);
            _eventBus.Subscribe<StopAmbientEvent>(OnStopAmbient);
            _eventBus.Subscribe<SetVolumeEvent>(OnSetVolume);
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<PlayPositionalSFXEvent>(OnPlayPositionalSFX);
            _eventBus.Unsubscribe<PlayUISoundEvent>(OnPlayUISound);
            _eventBus.Unsubscribe<PlayMusicEvent>(OnPlayMusic);
            _eventBus.Unsubscribe<StopMusicEvent>(OnStopMusic);
            _eventBus.Unsubscribe<PlayAmbientEvent>(OnPlayAmbient);
            _eventBus.Unsubscribe<StopAmbientEvent>(OnStopAmbient);
            _eventBus.Unsubscribe<SetVolumeEvent>(OnSetVolume);
        }

        private void OnPlayPositionalSFX(PlayPositionalSFXEvent e) =>
            _sound.PlayPositionalSFX(e.ClipId, e.Position, e.VolumeScale);

        private void OnPlayUISound(PlayUISoundEvent e) =>
            _sound.PlayUISound(e.ClipId, e.VolumeScale);

        private void OnPlayMusic(PlayMusicEvent e) =>
            _sound.PlayMusic(e.ClipId, e.Loop);

        private void OnStopMusic(StopMusicEvent _) =>
            _sound.StopMusic();

        private void OnPlayAmbient(PlayAmbientEvent e) =>
            _sound.PlayAmbient(e.ClipId);

        private void OnStopAmbient(StopAmbientEvent _) =>
            _sound.StopAmbient();

        private void OnSetVolume(SetVolumeEvent e) =>
            _sound.SetVolume(e.Category, e.NormalizedVolume);
    }
}
```

---

## Phase 3 — Integration

### Step 8 — Register in `GameServiceBootstrapper.cs`

In `FindAndRegisterServices()`, add after the existing registrations:

```csharp
// Find and register SoundService
var soundService = FindFirstObjectByType<SoundService>();
if (soundService != null)
{
    soundService.Initialize();
    container.Register(soundService);
    if (enableDebugLogs)
        Debug.Log("[GameServiceBootstrapper] SoundService found and registered");
}
```

Also add `using Game.Sound;` at the top of `GameServiceBootstrapper.cs`.

---

### Step 9 — Scene Setup

1. Create a `SoundManager` GameObject in the scene
2. Attach both `SoundService` and `SoundEventListener` to it
3. In the `SoundService` inspector, assign:
   - `GameAudioMixer` → **Audio Mixer** field
   - `SoundConfig.asset` → **Config** field
   - `SoundLibrary.asset` → **Library** field
4. Ensure `GameServiceBootstrapper` has the `SoundService` in its execution window (default order is fine since Bootstrapper is -100)

---

### Step 10 — Publish Events from Existing Systems

These are the emit points. Add one line each — no other audio code needed.

| System | Where | Event |
|--------|-------|-------|
| `WalkingState.cs` | Each footstep interval | `PlayPositionalSFXEvent("footstep_walk", player.transform.position)` |
| `ClimbingState.cs` | While climbing, interval | `PlayPositionalSFXEvent("footstep_climb", player.transform.position)` |
| `FallingState.cs` → Walk transition | On land impact | `PlayPositionalSFXEvent("land_impact", player.transform.position)` |
| `HoldInteractableBase.cs` | `OnHoldComplete()` | `PlayPositionalSFXEvent("interact_complete", transform.position)` |
| `ItemInteractable.cs` | On pickup | `PlayPositionalSFXEvent("item_pickup", transform.position)` |
| `MainMenuUI.cs` | On scene load | `PlayMusicEvent("music_menu")` |
| `GameplaySceneInitializer.cs` | On gameplay start | `PlayMusicEvent("music_gameplay")` |

**Example — how to publish in any system:**

```csharp
// At the top of the file
using Game.Sound.Events;
using Game.Core.DI;
using Game.Core.Events;

// In the method body — resolve once in Initialize/Start, cache the reference
_eventBus.Publish(new PlayPositionalSFXEvent("item_pickup", transform.position));
```

---

### Step 11 — Delete `InteractionAudioManager.cs`

Verify no remaining callers first:

```
grep -r "InteractionAudioManager" Assets/Game/Script/
```

If zero results, delete the file.

---

## SoundLibrary Clip IDs Reference

Populate `SoundLibrary.asset` with these IDs. Add more as needed — game code only references the string key.

| ID | Category | Description |
|----|----------|-------------|
| `footstep_walk` | SFX | Walking footsteps (add multiple clips for variation) |
| `footstep_climb` | SFX | Climbing scrape sounds |
| `footstep_run` | SFX | Running footsteps |
| `land_impact` | SFX | Landing after a fall |
| `jump` | SFX | Jump sound |
| `item_pickup` | SFX | Generic item collected |
| `item_equip` | SFX | Item equipped |
| `item_drop` | SFX | Item dropped |
| `interact_start` | SFX | Hold-interact begins |
| `interact_complete` | SFX | Hold-interact finished |
| `interact_cancel` | SFX | Interaction cancelled |
| `gather_hit` | SFX | Gathering resource hit |
| `water_fill` | SFX | Canteen filling |
| `death_sting` | UI | Player death stinger |
| `ui_notification` | UI | Item notification toast |
| `ui_button_click` | UI | Menu button press |
| `ambient_morning` | Ambient | Morning birds/forest |
| `ambient_day` | Ambient | Day background |
| `ambient_evening` | Ambient | Evening transition |
| `ambient_night` | Ambient | Night insects/wind |
| `music_menu` | Music | Main menu track |
| `music_gameplay` | Music | In-game background music |

---

## Verification Checklist

- [ ] Press Play → walk the player → SFX Audio Mixer group shows signal in Audio Mixer window
- [ ] Trigger `TimeOfDayChangedEvent` (or force time skip) → ambient track crossfades
- [ ] Pick up an item → positional sound heard, fades with distance (move camera away)
- [ ] Open main menu → `music_menu` starts; load game → crossfades to `music_gameplay`
- [ ] Die → death sting plays, music stops
- [ ] Adjust `SetVolumeEvent(SoundCategory.Music, 0.5f)` via a debug button → Music fader moves in Audio Mixer
- [ ] `grep -r "InteractionAudioManager"` → zero results after deletion

---

## Decisions Log

| Decision | Reason |
|----------|--------|
| No `ISoundService` interface | Single implementation, follows YAGNI; consistent with other concrete registrations like `TabbedInventoryUI` in the bootstrapper |
| EventBus-only entry point | Zero coupling between game systems and audio — a system only needs `IEventBus` to trigger sound |
| `SoundEventListener` is a separate MonoBehaviour | Keeps subscription lifecycle tied to a scene object; easy to disable per-scene |
| `SoundLibrary` clip lookup by string key | Game code never holds `AudioClip` references; swap clips in one asset without touching code |
| Crossfade uses double-buffered AudioSources | Simplest coroutine approach, no dependency on third-party libraries |
| `Time.unscaledDeltaTime` for fades | Fades work correctly during pause (time scale = 0) |
