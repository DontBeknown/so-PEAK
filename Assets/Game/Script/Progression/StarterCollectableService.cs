using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;
using Game.Collectable;
using UnityEngine.SceneManagement;

namespace Game.Progression
{
    /// <summary>
    /// Awards starter collectables when entering a new world or fresh level.
    /// 
    /// Checks IsNewWorld() or IsFreshLevelEntry() to determine if starters should be awarded.
    /// Maps each level to its starter collectables via config, then unlocks them in CollectableManager.
    /// 
    /// Place this component in the scene or assign via inspector. Services are resolved at runtime.
    /// </summary>
    public class StarterCollectableService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private StarterCollectableConfig starterConfig;

        [Header("Debug")]
        [SerializeField] private bool enableDebug = false;

        private ICollectableManager _collectableManager;
        private ISaveLoadService _saveLoadService;

        /// <summary>
        /// Awards starter collectables if this is a fresh world entry or fresh level progression.
        /// Should be called after HydrateWorldServices() to ensure game state is ready.
        /// </summary>
        public void AwardStarterCollectables()
        {

            _collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
            _saveLoadService = SaveLoadService.Instance;

            if (_saveLoadService == null)
            {
                Debug.LogError("[StarterCollectableService] SaveLoadService not available!");
                return;
            }

            if (_collectableManager == null)
            {
                Debug.LogError("[StarterCollectableService] CollectableManager not available!");
                return;
            }

            if (starterConfig == null)
            {
                Debug.LogError("[StarterCollectableService] StarterCollectableConfig not assigned!");
                return;
            }

            // Check if this is a fresh entry (new world or fresh level progression)
            bool isNewWorld = _saveLoadService.IsNewWorld();
            bool isFreshLevelEntry = _saveLoadService.IsFreshLevelEntry();

            if (enableDebug)
            {
                Debug.Log($"[StarterCollectableService] IsNewWorld={isNewWorld}, IsFreshLevelEntry={isFreshLevelEntry}");
            }

            if (!isNewWorld && !isFreshLevelEntry)
            {
                if (enableDebug)
                {
                    Debug.Log("[StarterCollectableService] Not a fresh entry - skipping starter award");
                }
                return;
            }

            // Get current level and fetch starters for this level
            int currentLevel = _saveLoadService.GetCurrentLevel();
            List<CollectableItem> startersForLevel = starterConfig.GetStartersForLevel(currentLevel);

            if (startersForLevel.Count == 0)
            {
                if (enableDebug)
                {
                    Debug.Log($"[StarterCollectableService] No starter collectables configured for level {currentLevel}");
                }
                return;
            }

            // Award each starter
            int awardedCount = 0;
            foreach (var collectable in startersForLevel)
            {
                if (collectable == null)
                {
                    Debug.LogWarning("[StarterCollectableService] Null collectable in starter list");
                    continue;
                }

                // Skip if already unlocked (shouldn't happen, but safety check)
                if (_collectableManager.IsUnlocked(collectable.id))
                {
                    if (enableDebug)
                    {
                        Debug.Log($"[StarterCollectableService] Collectable '{collectable.id}' already unlocked - skipping");
                    }
                    continue;
                }

                _collectableManager.Unlock(collectable);
                awardedCount++;

                if (enableDebug)
                {
                    Debug.Log($"[StarterCollectableService] Awarded starter collectible: {collectable.headerName} (ID: {collectable.id})");
                }
            }

            if (enableDebug)
            {
                Debug.Log($"[StarterCollectableService] Awarded {awardedCount}/{startersForLevel.Count} starter collectables for level {currentLevel}");
            }
        }

        /// <summary>
        /// Manual API to award starters for a specific level (useful for testing/debugging).
        /// </summary>
        public void ManualAwardForLevel(int level)
        {
            if (_collectableManager == null)
            {
                Debug.LogError("[StarterCollectableService] CollectableManager not available!");
                return;
            }

            if (starterConfig == null)
            {
                Debug.LogError("[StarterCollectableService] StarterCollectableConfig not assigned!");
                return;
            }

            List<CollectableItem> startersForLevel = starterConfig.GetStartersForLevel(level);
            foreach (var collectable in startersForLevel)
            {
                if (collectable != null && !_collectableManager.IsUnlocked(collectable.id))
                {
                    _collectableManager.Unlock(collectable);
                    if (enableDebug)
                    {
                        Debug.Log($"[StarterCollectableService] Manually awarded: {collectable.headerName}");
                    }
                }
            }
        }

        /// <summary>
        /// Gets unique starter collectable count for a level.
        /// Used by other progression services to offset level goals.
        /// </summary>
        public int GetStarterCollectableCountForLevel(int level)
        {
            if (starterConfig == null)
            {
                return 0;
            }

            var startersForLevel = starterConfig.GetStartersForLevel(level);
            if (startersForLevel == null || startersForLevel.Count == 0)
            {
                return 0;
            }

            var uniqueIds = new HashSet<string>();
            for (int i = 0; i < startersForLevel.Count; i++)
            {
                var collectable = startersForLevel[i];
                if (collectable == null || string.IsNullOrWhiteSpace(collectable.id))
                {
                    continue;
                }

                uniqueIds.Add(collectable.id);
            }

            return uniqueIds.Count;
        }
    }
}
