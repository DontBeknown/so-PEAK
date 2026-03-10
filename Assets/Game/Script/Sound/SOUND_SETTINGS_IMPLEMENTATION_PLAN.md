# Sound Settings Menu — Implementation Plan

## Overview

Add a **Sound Settings panel** accessible from the Main Menu that persists volume preferences across scene loads via PlayerPrefs. A `SoundSettingsManager` (DontDestroyOnLoad singleton) carries all settings from the Menu scene into the Gameplay scene and re-applies them whenever a new scene loads.

---

## Architecture

```
[Menu Scene]
  MainMenuUI (Settings button)
       │ Toggle()
       ▼
  SoundSettingsPanel (IUIPanel, Menu Canvas)
       │ SetMaster / SetCategoryVolume
       ▼
  SoundSettingsManager ──── DontDestroyOnLoad ────► [Gameplay Scene]
       │ PlayerPrefs (persist)                            │ SceneManager.sceneLoaded
       │                                                  ▼
       └─────────────────────────────────────► SoundService.SetVolume()
```

**Key decisions:**
- `SoundSettingsManager` is the only DontDestroyOnLoad object — the panel stays as a normal Menu Canvas UI component.
- Master volume is a **multiplier** applied over each category's raw value: `SetVolume(category, master * categoryValue)`. No AudioMixer topology changes needed.
- Volumes persist between play sessions via **PlayerPrefs**.
- Panel is accessible from the **Main Menu only** (not during gameplay).

---

## Phase 1 — Data & Persistence Layer

### Step 1: Create `SoundSettingsManager.cs`

**Path:** `Assets/Game/Script/Sound/SoundSettingsManager.cs`  
**Namespace:** `Game.Sound`

#### Responsibilities
- Singleton MonoBehaviour with `DontDestroyOnLoad`.
- Load all volume values from PlayerPrefs on `Awake()`, falling back to `SoundConfig` defaults.
- Subscribe to `SceneManager.sceneLoaded` → re-apply volumes to whichever `SoundService` is active in the new scene.
- Expose public setters that update the in-memory value, save to PlayerPrefs, and immediately call `ApplyAllVolumes()`.

#### PlayerPrefs Keys

| Key | Default |
|-----|---------|
| `SoundSettings_Master` | `1.0` |
| `SoundSettings_Music` | `0.6` |
| `SoundSettings_SFX` | `0.8` |
| `SoundSettings_Ambient` | `0.5` |
| `SoundSettings_UI` | `0.7` |

#### `ApplyAllVolumes()` logic

```
effectiveVolume = masterVolume * categoryVolume
SoundService.SetVolume(SoundCategory.Music,   master * musicVolume)
SoundService.SetVolume(SoundCategory.SFX,     master * sfxVolume)
SoundService.SetVolume(SoundCategory.Ambient, master * ambientVolume)
SoundService.SetVolume(SoundCategory.UI,      master * uiVolume)
```

`SoundService` is resolved via `ServiceContainer.Instance.TryGet<SoundService>()` with fallback to `FindFirstObjectByType<SoundService>()`.

#### Public API

```csharp
float MasterVolume  { get; }
float MusicVolume   { get; }
float SfxVolume     { get; }
float AmbientVolume { get; }
float UIVolume      { get; }

void SetMaster(float value)
void SetMusic(float value)
void SetSFX(float value)
void SetAmbient(float value)
void SetUI(float value)
```

---

## Phase 2 — UI Panel

### Step 2: Create `SoundSettingsPanel.cs`

**Path:** `Assets/Game/Script/UI/SoundSettingsPanel.cs`  
**Namespace:** `Game.UI`  
**Pattern:** Follow `DeathScreenUI.cs` as the reference IUIPanel implementation.

#### Implements `IUIPanel`

| Property / Method | Value |
|-------------------|-------|
| `PanelName` | `"SoundSettings"` |
| `BlocksInput` | `false` |
| `UnlocksCursor` | `true` |
| `IsActive` | `panelRoot != null && panelRoot.activeSelf` |

#### Serialized Fields

```
[SerializeField] GameObject panelRoot
[SerializeField] Button closeButton

[SerializeField] Slider masterSlider
[SerializeField] Slider musicSlider
[SerializeField] Slider sfxSlider
[SerializeField] Slider ambientSlider
[SerializeField] Slider uiSlider
```

#### Behaviour

- **`Awake()`**: Hook `closeButton.onClick → Hide()`. Add `onValueChanged` listeners to all 5 sliders.
- **`Start()`**: Find `SoundSettingsManager` via `FindFirstObjectByType<SoundSettingsManager>()`.
- **`Show()`**: `panelRoot.SetActive(true)`, then sync all slider values from the manager's current settings.
- **`Hide()`**: `panelRoot.SetActive(false)`.
- **`Toggle()`**: `IsActive ? Hide() : Show()`.
- **Slider callbacks**: Each calls the matching `SoundSettingsManager.Set...()` — volume updates happen in real time while dragging.
- **`OnDestroy()`**: Remove all slider listeners.

---

## Phase 3 — MainMenuUI Wire-Up

### Step 3: Modify `MainMenuUI.cs`

**Path:** `Assets/Game/Script/Menu/MainMenuUI.cs`

#### Changes

1. Add field:
   ```csharp
   [Header("Settings")]
   [SerializeField] private SoundSettingsPanel soundSettingsPanel;
   ```
2. In `Start()`, add fallback after the existing `soundService` null-check:
   ```csharp
   if (soundSettingsPanel == null)
       soundSettingsPanel = FindFirstObjectByType<SoundSettingsPanel>();
   ```
3. Replace the TODO body of `OnSettingsClicked()`:
   ```csharp
   // Before
   if (enableDebug) Debug.Log("Settings button clicked - TODO: Implement settings menu");
   // TODO: Open settings menu

   // After
   soundSettingsPanel?.Toggle();
   ```

---

## Phase 4 — Unity Editor Scene Setup (manual steps)

### 4.1 Add SoundSettingsManager to the Menu Scene

1. In the **Menu** scene hierarchy, create an empty GameObject named `SoundSettingsManager`.
2. Attach `SoundSettingsManager.cs`.
3. _(Optional)_ Assign a `SoundConfig` ScriptableObject reference in the Inspector to use as defaults source.

> No setup needed in the Gameplay scene — the manager survives scene loads automatically.

### 4.2 Build the Panel UI in the Menu Canvas

Add the following hierarchy inside your existing **Menu Canvas**:

```
SoundSettingsPanel            ← Panel (Image background)
├── PanelBackground           ← optional additional styling
├── TitleText                 ← TMP_Text  "Sound Settings"
├── CloseButton               ← Button  "✕"
├── MasterRow
│   ├── MasterLabel           ← TMP_Text  "Master"
│   └── MasterSlider          ← Slider (Min: 0, Max: 1, Value: 1)
├── MusicRow
│   ├── MusicLabel            ← TMP_Text  "Music"
│   └── MusicSlider           ← Slider (Min: 0, Max: 1, Value: 0.6)
├── SFXRow
│   ├── SFXLabel              ← TMP_Text  "SFX"
│   └── SFXSlider             ← Slider (Min: 0, Max: 1, Value: 0.8)
├── AmbientRow
│   ├── AmbientLabel          ← TMP_Text  "Ambient"
│   └── AmbientSlider         ← Slider (Min: 0, Max: 1, Value: 0.5)
└── UIRow
    ├── UILabel               ← TMP_Text  "UI"
    └── UISlider              ← Slider (Min: 0, Max: 1, Value: 0.7)
```

4. Attach `SoundSettingsPanel.cs` to the `SoundSettingsPanel` root object.
5. Wire all serialized fields in the Inspector.
6. Set `SoundSettingsPanel` GameObject to **inactive** by default (`panelRoot.SetActive(false)`).

### 4.3 Wire MainMenuUI

1. Select the `MainMenuUI` GameObject in the Menu scene.
2. Drag the `SoundSettingsPanel` GameObject into the new **Sound Settings Panel** field in the Inspector.

---

## Files Modified / Created

| File | Action |
|------|--------|
| `Assets/Game/Script/Sound/SoundSettingsManager.cs` | **Create** |
| `Assets/Game/Script/UI/SoundSettingsPanel.cs` | **Create** |
| `Assets/Game/Script/Menu/MainMenuUI.cs` | **Modify** |

### Files Read (dependencies — no changes)

| File | Role |
|------|------|
| `Assets/Game/Script/Sound/SoundService.cs` | `SetVolume(SoundCategory, float)` target |
| `Assets/Game/Script/Sound/SoundCategory.cs` | Enum: `SFX, Ambient, Music, UI` |
| `Assets/Game/Script/Sound/SoundConfig.cs` | Default volume values for PlayerPrefs fallback |
| `Assets/Game/Script/UI/Interfaces/IUIPanel.cs` | Interface to implement |
| `Assets/Game/Script/UI/DeathScreen/DeathScreenUI.cs` | Reference IUIPanel implementation |
| `Assets/Game/Script/Core/DependencyInjection/ServiceContainer.cs` | `TryGet<T>()` for SoundService lookup |

---

## Verification Checklist

- [ ] Play Menu scene → clicking **Settings** opens the `SoundSettingsPanel`
- [ ] Adjust any slider → audio changes in real time
- [ ] Close panel → load Gameplay scene → audio plays at the volumes set in menu
- [ ] Quit and restart the game → reopen Menu → sliders show the previously saved values
- [ ] Check PlayerPrefs values via:
  ```csharp
  Debug.Log(PlayerPrefs.GetFloat("SoundSettings_Master"));
  ```

---

## Future Improvements (out of scope)

- Animate panel open/close (e.g. DOTween fade or slide)
- Add a **Reset to Defaults** button
- Display a numeric percentage label next to each slider (e.g. "80%")
- Support keyboard navigation between sliders
- Expose panel from a Pause Menu during gameplay
