using System.Collections.Generic;
using UnityEngine;
using Game.Collectable;

namespace Game.Progression
{
    /// <summary>
    /// Maps collectables to level progressions.
    /// Level 1 (Forest) → Level 2 (Desert) → Level 3 (Snow)
    /// </summary>
    [System.Serializable]
    public class LevelStarterCollectables
    {
        public int level;
        public List<CollectableItem> starterCollectables = new List<CollectableItem>();
    }

    [CreateAssetMenu(fileName = "StarterCollectableConfig", menuName = "Game/Collectable/Starter Collectable Config")]
    public class StarterCollectableConfig : ScriptableObject
    {
        [SerializeField] private List<LevelStarterCollectables> startersByLevel = new List<LevelStarterCollectables>();

        /// <summary>
        /// Gets starter collectables for the specified level.
        /// Returns empty list if level not found.
        /// </summary>
        public List<CollectableItem> GetStartersForLevel(int level)
        {
            var entry = startersByLevel.Find(s => s.level == level);
            if (entry != null)
            {
                return entry.starterCollectables;
            }

            Debug.LogWarning($"[StarterCollectableConfig] No starter collectables configured for level {level}");
            return new List<CollectableItem>();
        }

        /// <summary>
        /// Gets all configured levels.
        /// </summary>
        public List<int> GetConfiguredLevels()
        {
            var levels = new List<int>();
            foreach (var entry in startersByLevel)
            {
                levels.Add(entry.level);
            }
            return levels;
        }
    }
}
