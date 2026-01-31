using UnityEngine;
using System.Collections.Generic;

namespace Game.Interaction
{
    /// <summary>
    /// Centralized audio manager for interaction system.
    /// Manages sound playback with pooling for better performance.
    /// </summary>
    public class InteractionAudioManager : MonoBehaviour
    {
        private static InteractionAudioManager _instance;
        public static InteractionAudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("InteractionAudioManager");
                    _instance = managerObj.AddComponent<InteractionAudioManager>();
                    DontDestroyOnLoad(managerObj);
                }
                return _instance;
            }
        }

        [Header("Audio Source Pool")]
        [SerializeField] private int poolSize = 5;
        [SerializeField] private GameObject audioSourcePrefab;
        
        [Header("Default Sounds")]
        [SerializeField] private AudioClip defaultHighlightSound;
        [SerializeField] private AudioClip defaultInteractSound;
        [SerializeField] private AudioClip defaultPickupSound;
        [SerializeField] private AudioClip defaultCancelSound;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float highlightVolume = 0.3f;
        [Range(0f, 1f)]
        [SerializeField] private float interactVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float pickupVolume = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float cancelVolume = 0.6f;
        
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                CreateAudioSource();
            }
        }

        private AudioSource CreateAudioSource()
        {
            GameObject sourceObj = new GameObject($"PooledAudioSource_{audioSourcePool.Count}");
            sourceObj.transform.SetParent(transform);
            
            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f; // 3D sound
            source.minDistance = 1f;
            source.maxDistance = 20f;
            
            audioSourcePool.Enqueue(source);
            return source;
        }

        private AudioSource GetAudioSource()
        {
            if (audioSourcePool.Count == 0)
            {
                // Pool exhausted, create new one
                return CreateAudioSource();
            }
            
            AudioSource source = audioSourcePool.Dequeue();
            activeAudioSources.Add(source);
            return source;
        }

        private void ReturnAudioSource(AudioSource source)
        {
            if (source == null) return;
            
            source.Stop();
            source.clip = null;
            activeAudioSources.Remove(source);
            audioSourcePool.Enqueue(source);
        }

        private void Update()
        {
            // Return finished audio sources to pool
            for (int i = activeAudioSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = activeAudioSources[i];
                if (source != null && !source.isPlaying)
                {
                    ReturnAudioSource(source);
                }
            }
        }

        #region Public API

        /// <summary>
        /// Play a 3D sound at a specific position
        /// </summary>
        public void PlaySound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            
            AudioSource source = GetAudioSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.Play();
        }

        /// <summary>
        /// Play a looping sound at a position (returns the audio source for manual control)
        /// </summary>
        public AudioSource PlayLoopingSound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return null;
            
            AudioSource source = GetAudioSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.loop = true;
            source.Play();
            
            return source;
        }

        /// <summary>
        /// Stop a looping sound and return it to pool
        /// </summary>
        public void StopLoopingSound(AudioSource source)
        {
            if (source == null) return;
            
            source.loop = false;
            ReturnAudioSource(source);
        }

        /// <summary>
        /// Play highlight sound (when object is highlighted)
        /// </summary>
        public void PlayHighlightSound(Vector3 position, AudioClip customClip = null)
        {
            AudioClip clip = customClip != null ? customClip : defaultHighlightSound;
            PlaySound(clip, position, highlightVolume);
        }

        /// <summary>
        /// Play interact sound (when player presses E)
        /// </summary>
        public void PlayInteractSound(Vector3 position, AudioClip customClip = null)
        {
            AudioClip clip = customClip != null ? customClip : defaultInteractSound;
            PlaySound(clip, position, interactVolume);
        }

        /// <summary>
        /// Play pickup sound (when item is collected)
        /// </summary>
        public void PlayPickupSound(Vector3 position, AudioClip customClip = null)
        {
            AudioClip clip = customClip != null ? customClip : defaultPickupSound;
            PlaySound(clip, position, pickupVolume);
        }

        /// <summary>
        /// Play cancel sound (when action is cancelled)
        /// </summary>
        public void PlayCancelSound(Vector3 position, AudioClip customClip = null)
        {
            AudioClip clip = customClip != null ? customClip : defaultCancelSound;
            PlaySound(clip, position, cancelVolume);
        }

        /// <summary>
        /// Stop all playing sounds
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var source in activeAudioSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
            
            // Return all to pool
            while (activeAudioSources.Count > 0)
            {
                ReturnAudioSource(activeAudioSources[0]);
            }
        }

        #endregion

        #region Convenience Static Methods

        public static void Play(AudioClip clip, Vector3 position, float volume = 1f)
        {
            Instance?.PlaySound(clip, position, volume);
        }

        public static void PlayHighlight(Vector3 position, AudioClip customClip = null)
        {
            Instance?.PlayHighlightSound(position, customClip);
        }

        public static void PlayInteract(Vector3 position, AudioClip customClip = null)
        {
            Instance?.PlayInteractSound(position, customClip);
        }

        public static void PlayPickup(Vector3 position, AudioClip customClip = null)
        {
            Instance?.PlayPickupSound(position, customClip);
        }

        public static void PlayCancel(Vector3 position, AudioClip customClip = null)
        {
            Instance?.PlayCancelSound(position, customClip);
        }

        #endregion
    }
}
