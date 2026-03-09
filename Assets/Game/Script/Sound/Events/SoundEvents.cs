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
