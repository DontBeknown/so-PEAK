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
