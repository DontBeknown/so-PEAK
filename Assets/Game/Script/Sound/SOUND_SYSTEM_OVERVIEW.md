# Sound System — Overview & Extension Guide

## Architecture at a Glance

```
┌─────────────────────────────────────────────────────────┐
│  Any MonoBehaviour / System                             │
│  eventBus.Publish(new PlayMusicEvent("theme_main"))     │
└────────────────────┬────────────────────────────────────┘
                     │ IEventBus
                     ▼
┌────────────────────────────────────────────────────────┐
│  SoundEventListener  (subscribes to all sound events)  │
│  Bridges the event bus → SoundService public API       │
└────────────────────┬───────────────────────────────────┘
                     │ direct method calls
                     ▼
┌────────────────────────────────────────────────────────┐
│  SoundService  (MonoBehaviour, registered in DI)       │
│  • Object pool for 3-D SFX AudioSources               │
│  • Double-buffered crossfade for Music & Ambient       │
│  • Dedicated UI AudioSource with rapid-trigger pitch   │
│  • Converts 0–1 volumes → dB → AudioMixer             │
└──────┬─────────────────┬──────────────────────────────┘
       │                 │
       ▼                 ▼
SoundLibrary         SoundConfig
(ScriptableObject)   (ScriptableObject)
string ID → clip[]   pool size, volumes,
random pick          crossfades, 3-D settings
```

`SoundSettingsManager` sits alongside this pipeline, persisting per-category volumes to `PlayerPrefs` and re-applying them every time a new scene loads.

---

## Files

| File | Purpose |
|------|---------|
| `SoundCategory.cs` | `enum SoundCategory { SFX, Ambient, Music, UI }` |
| `SoundConfig.cs` | ScriptableObject — tuning knobs (pool size, volumes, crossfade, 3-D range) |
| `SoundLibrary.cs` | ScriptableObject — maps `string` IDs → `AudioClip[]`; picks randomly |
| `Events/SoundEvents.cs` | Plain C# event classes published on the `IEventBus` |
| `SoundEventListener.cs` | Subscribes to every sound event and calls `SoundService` |
| `SoundService.cs` | Core audio engine — pool, crossfades, mixer integration |
| `SoundSettingsManager.cs` | Persistent volume settings via `PlayerPrefs`, DontDestroyOnLoad |

---

## How It Works

### 1. SoundConfig (ScriptableObject)

Create via **Assets → Create → Config → SoundConfig**.

| Field | Description |
|-------|-------------|
| `PoolSize` | How many `AudioSource`s are pre-created for 3-D SFX at startup |
| `DefaultSFXVolume` | Applied per-clip as AudioSource volume (not the mixer level) |
| `DefaultUIVolume` | Same, for UI sounds |
| `DefaultMusicVolume` | Applied as the starting volume for music sources |
| `DefaultAmbientVolume` | Applied as the starting volume for ambient sources |
| `MusicCrossfadeDuration` | Fade time in seconds when switching music tracks |
| `AmbientCrossfadeDuration` | Fade time in seconds when switching ambient loops |
| `SpatialBlend` | `1` = full 3-D for pooled SFX sources |
| `MinDistance / MaxDistance` | AudioSource rolloff range for positional SFX |

---

### 2. SoundLibrary (ScriptableObject)

Create via **Assets → Create → Config → SoundLibrary**.

Each entry has:
- **Id** — the string key you use everywhere else (e.g. `"footstep_walk"`, `"ui_click"`)
- **Clips** — one or more `AudioClip`s; a random one is chosen each play

`SoundLibrary.Get(string id)` returns one random clip, or `null` if the ID doesn't exist.

---

### 3. Sound Events

All events live in `Game.Sound.Events`. They are plain data classes — no Unity dependency.

| Event class | When to publish |
|-------------|----------------|
| `PlayPositionalSFXEvent(clipId, position, volumeScale)` | World-space one-shot SFX (footsteps, impacts, explosions) |
| `PlayUISoundEvent(clipId, volumeScale)` | 2-D UI sounds (button clicks, notifications) |
| `PlayMusicEvent(clipId, loop)` | Start or crossfade to a new music track |
| `StopMusicEvent()` | Fade out current music |
| `PlayAmbientEvent(clipId)` | Start or crossfade to a new ambient loop |
| `StopAmbientEvent()` | Fade out current ambient |
| `SetVolumeEvent(category, normalizedVolume)` | Override a mixer category volume at runtime |

**Example — playing a sound from any MonoBehaviour:**

```csharp
// Inject or resolve however your DI is set up
var eventBus = ServiceContainer.Instance.Get<IEventBus>();

// 3-D footstep
eventBus.Publish(new PlayPositionalSFXEvent("footstep_walk", transform.position));

// UI click
eventBus.Publish(new PlayUISoundEvent("ui_click"));

// Start music with default loop
eventBus.Publish(new PlayMusicEvent("theme_main"));

// Stop music
eventBus.Publish(new StopMusicEvent());
```

---

### 4. SoundService

The `SoundService` `MonoBehaviour` should be placed in your bootstrapper scene and registered with the `ServiceContainer`.

**SFX Pool**  
At `Awake` it creates `PoolSize` child `GameObject`s each with an `AudioSource`. `PlayPositionalSFX` rents one, positions it, plays the clip, and `Update` returns it to the pool when playback finishes. The pool grows automatically if all sources are in use.

**Music & Ambient — Double-buffered Crossfade**  
Two `AudioSource`s per stream (`Music_A / Music_B`, `Ambient_A / Ambient_B`). When you switch tracks, the new clip fades in on B while A fades out. After the fade, references are swapped so A is always "active". Each stream has its own `Coroutine` so stopping music never cancels an ambient fade and vice versa.

**UI Rapid-Trigger Pitch Ramp**  
Consecutive UI sounds played within `rapidTriggerWindow` seconds of each other get their pitch incremented by `pitchIncrement`. Once `maxPitchScale` is reached it resets to 1. This prevents monotony on rapid button presses.  
These fields are serialised and tunable in the Inspector.

**Volume → dB Conversion**  
`SetVolume(category, 0–1)` converts to decibels via `20 * log10(value)` (clamped to −80 dB at silence) before calling `AudioMixer.SetFloat`. The AudioMixer's **exposed parameter names** must match the values in the `SoundService` Inspector fields (`SFXVolume`, `UIVolume`, `MusicVolume`, `AmbientVolume` by default).

---

### 5. SoundSettingsManager

A `DontDestroyOnLoad` singleton that:
1. Loads per-category floats from `PlayerPrefs` on `Awake`.
2. Re-applies all volumes one frame after every `SceneManager.sceneLoaded` event (so `SoundService.Initialize()` has run first).
3. Exposes `SetMaster`, `SetMusic`, `SetSFX`, `SetAmbient`, `SetUI` — call these from your settings UI sliders.

Master volume is a multiplier applied on top of each individual category volume before being passed to `SoundService.SetVolume`.

```csharp
// Wire up Unity UI sliders in the settings screen:
masterSlider.onValueChanged.AddListener(SoundSettingsManager.Instance.SetMaster);
musicSlider.onValueChanged.AddListener(SoundSettingsManager.Instance.SetMusic);
sfxSlider.onValueChanged.AddListener(SoundSettingsManager.Instance.SetSFX);
```

---

## Extension Guide

### A. Add a new sound clip

1. Open the **SoundLibrary** asset.
2. Add an entry: pick a unique **Id** string and assign one or more **AudioClip**s.
3. Reference the Id in your event: `new PlayPositionalSFXEvent("my_new_clip", pos)`.

---

### B. Add a new Sound Category

1. Add a value to the `SoundCategory` enum in `SoundCategory.cs`.

    ```csharp
    public enum SoundCategory { SFX, Ambient, Music, UI, Voice }
    ```

2. Add an **AudioMixer Group** called `Voice` (or your name) under the Master in the AudioMixer asset.

3. Expose a new **parameter** (e.g. `VoiceVolume`) on that group.

4. In `SoundService`, add a serialised field for the group name and parameter name:

    ```csharp
    [SerializeField] private string voiceGroupName      = "Voice";
    [SerializeField] private string voiceVolumeParam    = "VoiceVolume";
    ```

5. Cache the group in `CacheGroups()`:

    ```csharp
    _voiceGroup = FindGroup(voiceGroupName);
    ```

6. Extend the `switch` in `SetVolume()`:

    ```csharp
    SoundCategory.Voice => voiceVolumeParam,
    ```

7. If Voice needs its own dedicated `AudioSource`, create it in `CreateDedicatedSources()` the same way `_uiSource` is created.

8. Add a PlayerPrefs key and `SetVoice` method in `SoundSettingsManager`.

---

### C. Add a new Event type

1. Define a new event class in `Events/SoundEvents.cs`:

    ```csharp
    public class PlayVoiceLineEvent
    {
        public string ClipId;
        public float VolumeScale;
        public PlayVoiceLineEvent(string clipId, float volumeScale = 1f)
        {
            ClipId = clipId;
            VolumeScale = volumeScale;
        }
    }
    ```

2. Add the corresponding public method to `SoundService`:

    ```csharp
    public void PlayVoiceLine(string clipId, float volumeScale = 1f)
    {
        var clip = library.Get(clipId);
        if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }
        _voiceSource.PlayOneShot(clip, config.DefaultVoiceVolume * volumeScale);
    }
    ```

3. Subscribe and forward in `SoundEventListener`:

    ```csharp
    // In Start():
    _eventBus.Subscribe<PlayVoiceLineEvent>(OnPlayVoiceLine);

    // In OnDestroy():
    _eventBus.Unsubscribe<PlayVoiceLineEvent>(OnPlayVoiceLine);

    // Handler:
    private void OnPlayVoiceLine(PlayVoiceLineEvent e) =>
        _sound.PlayVoiceLine(e.ClipId, e.VolumeScale);
    ```

4. Publish from anywhere:

    ```csharp
    eventBus.Publish(new PlayVoiceLineEvent("npc_greeting"));
    ```

---

### D. Call SoundService directly (without the EventBus)

If you have access to the `ServiceContainer` (e.g. inside systems that aren't decoupled), you can skip the event and call the service directly:

```csharp
var sound = ServiceContainer.Instance.Get<SoundService>();
sound.PlayPositionalSFX("explosion_large", hitPoint);
sound.PlayMusic("boss_theme");
sound.SetVolume(SoundCategory.SFX, 0.5f);
```

---

### E. Replace a clip at runtime (e.g. dynamic difficulty)

`SoundLibrary` is a `ScriptableObject` — its contents are shared. To vary clips per-session without modifying the asset, call `SoundService.PlayPositionalSFX(AudioClip clip, ...)` / `PlayUISound(AudioClip clip, ...)` directly with an `AudioClip` reference you manage yourself. The overloads bypass the library lookup entirely.

---

## AudioMixer Setup Checklist

- [ ] Master group with child groups: **SFX**, **UI**, **Music**, **Ambient**
- [ ] Each child group has its volume parameter **exposed** (right-click the fader → *Expose*)
- [ ] Exposed parameter names match the `SoundService` Inspector fields (default: `SFXVolume`, `UIVolume`, `MusicVolume`, `AmbientVolume`)
- [ ] The `AudioMixer` asset is assigned in the `SoundService` Inspector
- [ ] `SoundConfig` and `SoundLibrary` assets are assigned in the `SoundService` Inspector

---

## Scene Setup Checklist

- [ ] A `GameObject` with `SoundService` exists in the bootstrapper scene
- [ ] `SoundService` is registered in `ServiceContainer` before any subscriber calls `Start`
- [ ] A `GameObject` with `SoundEventListener` exists in any scene that fires sound events
- [ ] A `GameObject` with `SoundSettingsManager` exists in the menu scene (persists via DontDestroyOnLoad)
