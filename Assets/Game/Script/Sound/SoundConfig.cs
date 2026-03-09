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
        [Range(0f, 1f)] public float DefaultSFXVolume     = 0.8f;
        [Range(0f, 1f)] public float DefaultUIVolume      = 0.7f;
        [Range(0f, 1f)] public float DefaultMusicVolume   = 0.6f;
        [Range(0f, 1f)] public float DefaultAmbientVolume = 0.5f;

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
