using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.DI;

namespace Game.Sound
{
    /// <summary>
    /// Persists sound volume settings across scenes via PlayerPrefs.
    /// Attach to a GameObject in the Menu scene — DontDestroyOnLoad keeps it alive.
    /// On every scene load it finds the active SoundService and re-applies all volumes.
    /// </summary>
    public class SoundSettingsManager : MonoBehaviour
    {
        // ─── PlayerPrefs keys ───────────────────────────────────────────────
        private const string KeyMaster  = "SoundSettings_Master";
        private const string KeyMusic   = "SoundSettings_Music";
        private const string KeySFX     = "SoundSettings_SFX";
        private const string KeyAmbient = "SoundSettings_Ambient";
        private const string KeyUI      = "SoundSettings_UI";

        // ─── Default volumes (SoundConfig values) ───────────────────────────
        private const float DefaultMaster  = 1.0f;
        private const float DefaultMusic   = 0.6f;
        private const float DefaultSFX     = 0.8f;
        private const float DefaultAmbient = 0.5f;
        private const float DefaultUI      = 0.7f;

        // ─── Singleton ──────────────────────────────────────────────────────
        public static SoundSettingsManager Instance { get; private set; }

        // ─── Volume properties (0–1) ────────────────────────────────────────
        public float MasterVolume  { get; private set; }
        public float MusicVolume   { get; private set; }
        public float SfxVolume     { get; private set; }
        public float AmbientVolume { get; private set; }
        public float UIVolume      { get; private set; }

        // ────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Enforce singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFromPrefs();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ─── Scene loading hook ─────────────────────────────────────────────

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Defer one frame so SoundService.Initialize() has already run
            StartCoroutine(ApplyNextFrame());
        }

        private System.Collections.IEnumerator ApplyNextFrame()
        {
            yield return null;
            ApplyAllVolumes();
        }

        // ─── Public setters ─────────────────────────────────────────────────

        public void SetMaster(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMaster, MasterVolume);
            PlayerPrefs.Save();
            ApplyAllVolumes();
        }

        public void SetMusic(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
            PlayerPrefs.Save();
            ApplyAllVolumes();
        }

        public void SetSFX(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeySFX, SfxVolume);
            PlayerPrefs.Save();
            ApplyAllVolumes();
        }

        public void SetAmbient(float value)
        {
            AmbientVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyAmbient, AmbientVolume);
            PlayerPrefs.Save();
            ApplyAllVolumes();
        }

        public void SetUI(float value)
        {
            UIVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyUI, UIVolume);
            PlayerPrefs.Save();
            ApplyAllVolumes();
        }

        // ─── Internal helpers ────────────────────────────────────────────────

        private void LoadFromPrefs()
        {
            MasterVolume  = PlayerPrefs.GetFloat(KeyMaster,  DefaultMaster);
            MusicVolume   = PlayerPrefs.GetFloat(KeyMusic,   DefaultMusic);
            SfxVolume     = PlayerPrefs.GetFloat(KeySFX,     DefaultSFX);
            AmbientVolume = PlayerPrefs.GetFloat(KeyAmbient, DefaultAmbient);
            UIVolume      = PlayerPrefs.GetFloat(KeyUI,      DefaultUI);
        }

        private void ApplyAllVolumes()
        {
            var soundService = ServiceContainer.Instance?.TryGet<SoundService>();
            if (soundService == null)
                soundService = FindFirstObjectByType<SoundService>();

            if (soundService == null) return;

            soundService.SetVolume(SoundCategory.Music,   MasterVolume * MusicVolume);
            soundService.SetVolume(SoundCategory.SFX,     MasterVolume * SfxVolume);
            soundService.SetVolume(SoundCategory.Ambient, MasterVolume * AmbientVolume);
            soundService.SetVolume(SoundCategory.UI,      MasterVolume * UIVolume);
        }
    }
}
