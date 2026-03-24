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
        [SerializeField] private string sfxGroupName     = "SFX";
        [SerializeField] private string uiGroupName      = "UI";
        [SerializeField] private string musicGroupName   = "Music";
        [SerializeField] private string ambientGroupName = "Ambient";

        [Header("AudioMixer Parameter Names")]
        [SerializeField] private string sfxVolumeParam     = "SFXVolume";
        [SerializeField] private string uiVolumeParam      = "UIVolume";
        [SerializeField] private string musicVolumeParam   = "MusicVolume";
        [SerializeField] private string ambientVolumeParam = "AmbientVolume";

        [Header("UI Rapid Trigger Pitch")]
        [SerializeField] private float rapidTriggerWindow = 0.5f;
        [SerializeField] private float pitchIncrement     = 0.1f;
        [SerializeField] private float maxPitchScale      = 2.0f;

        // Dedicated, non-pooled sources for continuous streams
        private AudioSource _musicSource;
        private AudioSource _musicSourceB;      // For crossfade double-buffering
        private AudioSource _ambientSource;
        private AudioSource _ambientSourceB;
        private AudioSource _uiSource;

        // Rapid-trigger pitch state
        private float _rapidUIPitch    = 1f;
        private float _lastUISoundTime = -999f;

        // Tracked coroutines — stopped individually so music ≠ ambient
        private Coroutine _musicCoroutine;
        private Coroutine _ambientCoroutine;

        // SFX object pool
        private readonly Queue<AudioSource> _pool   = new();
        private readonly List<AudioSource>  _active = new();

        // AudioMixer group cache
        private AudioMixerGroup _sfxGroup;
        private AudioMixerGroup _uiGroup;
        private AudioMixerGroup _musicGroup;
        private AudioMixerGroup _ambientGroup;
        
        private IEventBus _eventBus;

        private void Awake()
        {
            CacheGroups();
            CreateDedicatedSources();
            InitPool();
        }

        // Called by GameServiceBootstrapper after Awake
        public void Initialize()
        {
            // AudioMixer resets exposed parameters from its default snapshot at the end of
            // the first frame, so any SetFloat calls made during Awake/Initialize are silently
            // overwritten. Defer by one frame to guarantee our values win.
            StartCoroutine(ApplyDefaultVolumesNextFrame());

            _eventBus = ServiceContainer.Instance.TryGet<IEventBus>();
            _eventBus?.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
        }

        private IEnumerator ApplyDefaultVolumesNextFrame()
        {
            yield return null;
            SetVolume(SoundCategory.Music,   config.DefaultMusicVolume);
            SetVolume(SoundCategory.SFX,     config.DefaultSFXVolume);
            SetVolume(SoundCategory.Ambient, config.DefaultAmbientVolume);
            SetVolume(SoundCategory.UI,      config.DefaultUIVolume);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
        }

        private void OnPlayerDeath(PlayerDeathEvent _) => StopAllSFX();

        // ───────────────────────────── Public API ─────────────────────────────

        public void PlayPositionalSFX(string clipId, Vector3 position, float volumeScale = 1f)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            var source = RentSource();
            source.transform.position = position;
            source.clip   = clip;
            source.volume = config.DefaultSFXVolume * volumeScale;
            source.loop   = false;
            source.Play();
        }

        public void PlayPositionalSFX(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source = RentSource();
            source.transform.position = position;
            source.clip   = clip;
            source.volume = config.DefaultSFXVolume * volumeScale;
            source.loop   = false;
            source.Play();
        }

        public void PlayUISound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            PlayUISoundInternal(clip, volumeScale);
        }

        public void PlayUISound(string clipId, float volumeScale = 1f)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            PlayUISoundInternal(clip, volumeScale);
        }

        private void PlayUISoundInternal(AudioClip clip, float volumeScale)
        {
            float now = Time.unscaledTime;
            if (now - _lastUISoundTime > rapidTriggerWindow)
                _rapidUIPitch = 1f;
            else
            {
                _rapidUIPitch += pitchIncrement;
                if (_rapidUIPitch > maxPitchScale)
                    _rapidUIPitch = 1f;
            }

            _lastUISoundTime  = now;
            _uiSource.pitch   = _rapidUIPitch;
            _uiSource.PlayOneShot(clip, config.DefaultUIVolume * volumeScale);
        }

        public void PlayMusic(string clipId, bool loop = true)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            if (_musicCoroutine != null) StopCoroutine(_musicCoroutine);
            _musicCoroutine = StartCoroutine(CrossfadeMusic(clip, loop));
        }

        public void StopMusic()
        {
            if (_musicCoroutine != null) StopCoroutine(_musicCoroutine);
            _musicCoroutine = StartCoroutine(FadeOut(_musicSource, config.MusicCrossfadeDuration));
        }

        public void StopAllSFX()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                ReturnSource(_active[i]);
        }

        public void PlayAmbient(string clipId)
        {
            var clip = library.Get(clipId);
            if (clip == null) { Debug.LogWarning($"[SoundService] Clip not found: {clipId}"); return; }

            if (_ambientCoroutine != null) StopCoroutine(_ambientCoroutine);
            _ambientCoroutine = StartCoroutine(CrossfadeAmbient(clip));
        }

        public void StopAmbient()
        {
            if (_ambientCoroutine != null) StopCoroutine(_ambientCoroutine);
            _ambientCoroutine = StartCoroutine(FadeOut(_ambientSource, config.AmbientCrossfadeDuration));
        }

        // normalizedVolume: 0–1, converted to decibels
        public void SetVolume(SoundCategory category, float normalizedVolume)
        {
            if (audioMixer == null)
            {
                Debug.LogError("[SoundService] AudioMixer is not assigned!");
                return;
            }

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

            if (param == null) return;

            if (!audioMixer.SetFloat(param, dB))
                Debug.LogError($"[SoundService] AudioMixer parameter \"{param}\" not found. Make sure it is exposed in the AudioMixer asset.");
            /*audioMixer.GetFloat(param, out float currentDB);
            Debug.Log($"[SoundService] Set {category} volume: {normalizedVolume:P0} ({dB:F1} dB), current dB: {currentDB:F1}");*/
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
            src.playOnAwake              = false;
            src.spatialBlend             = config.SpatialBlend;
            src.minDistance              = config.MinDistance;
            src.maxDistance              = config.MaxDistance;
            src.outputAudioMixerGroup    = _sfxGroup;
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
            src.playOnAwake           = false;
            src.spatialBlend          = spatialBlend;
            src.outputAudioMixerGroup = group;
            return src;
        }

        private void CreateDedicatedSources()
        {
            _musicSource    = CreateDedicatedSource("Music_A",   _musicGroup);
            _musicSourceB   = CreateDedicatedSource("Music_B",   _musicGroup);
            _ambientSource  = CreateDedicatedSource("Ambient_A", _ambientGroup);
            _ambientSourceB = CreateDedicatedSource("Ambient_B", _ambientGroup);
            _uiSource       = CreateDedicatedSource("UI",        _uiGroup);
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
