using System.Collections.Generic;
using UnityEngine;
using Game.Collectable;
using Game.Core.DI;
using Game.Core.Events;
using Game.UI.Collectable;

namespace Game.Progression
{
    /// <summary>
    /// Unlocks a configured bonus collectable when the current level reaches:
    /// requiredCollectedAfterStarter + starter collectables for that level.
    /// </summary>
    public class LevelBonusCollectableService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LevelBonusCollectableConfig bonusConfig;
        [SerializeField] private StarterCollectableService starterCollectableService;
        [SerializeField] private CollectablesHubUI collectablesHubUI;
        [SerializeField] private ItemNotificationUI itemNotificationUI;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private ICollectableManager _collectableManager;
        private IEventBus _eventBus;
        private ISaveLoadService _saveLoadService;
        private bool _isSubscribed;

        /// <summary>Called by GameServiceBootstrapper after registration.</summary>
        public void Initialize(
            IEventBus eventBus,
            ICollectableManager collectableManager,
            ISaveLoadService saveLoadService,
            StarterCollectableService starterService)
        {
            _eventBus = eventBus;
            _collectableManager = collectableManager;
            _saveLoadService = saveLoadService;
            starterCollectableService = starterService ?? starterCollectableService;
            itemNotificationUI ??= ServiceContainer.Instance.TryGet<ItemNotificationUI>();

            SubscribeAllEvents();
            TryUnlockForCurrentLevel();
        }

        private void OnDisable()
        {
            if (!_isSubscribed)
            {
                return;
            }

            _eventBus?.Unsubscribe<CollectableUnlockedEvent>(OnCollectableUnlocked);
            _isSubscribed = false;
        }

        private void SubscribeAllEvents()
        {
            if (_eventBus == null)
            {
                Debug.LogWarning("[LevelBonusCollectableService] EventBus not available - cannot subscribe to events.", this);
                return;
            }

            _eventBus.Subscribe<CollectableUnlockedEvent>(OnCollectableUnlocked);
            _isSubscribed = true;
        }

        private void OnCollectableUnlocked(CollectableUnlockedEvent _)
        {

            TryUnlockForCurrentLevel();
        }

        private void TryUnlockForCurrentLevel()
        {

            if (_collectableManager == null || _saveLoadService == null || bonusConfig == null)
            {
                Debug.LogWarning("[LevelBonusCollectableService] Missing dependencies - cannot attempt unlock.", this);
                return;
            }

            int currentLevel = _saveLoadService.GetCurrentLevel();
            var rule = bonusConfig.GetRuleForLevel(currentLevel);
            if (rule == null || rule.bonusCollectable == null || string.IsNullOrWhiteSpace(rule.bonusCollectable.id))
            {
                Debug.LogWarning($"[LevelBonusCollectableService] No valid bonus rule found for level {currentLevel}.", this);
                return;
            }

            if (_collectableManager.IsUnlocked(rule.bonusCollectable.id))
            {
                return;
            }

            var biome = GetBiomeForLevel(currentLevel);
            int unlockedInBiome = CountUnlockedCollectablesInBiome(biome);
            int starterCount = CountStarterCollectables(currentLevel);
            int targetCount = Mathf.Max(0, rule.requiredCollectedAfterStarter) + starterCount;

            if (enableDebugLogs)
            {
                Debug.Log($"[LevelBonusCollectableService] Level {currentLevel}, Biome {biome}, unlocked={unlockedInBiome}, starter={starterCount}, target={targetCount}");
            }

            if (unlockedInBiome < targetCount)
            {
                return;
            }

            _collectableManager.Unlock(rule.bonusCollectable);
            NotifyBonusUnlocked(rule.bonusCollectable);

            if (enableDebugLogs)
            {
                Debug.Log($"[LevelBonusCollectableService] Bonus unlocked: {rule.bonusCollectable.id} for level {currentLevel}");
            }
        }

        private void NotifyBonusUnlocked(CollectableItem bonusCollectable)
        {
            if (bonusCollectable == null)
            {
                return;
            }

            itemNotificationUI ??= ServiceContainer.Instance.TryGet<ItemNotificationUI>();
            if (itemNotificationUI == null)
            {
                return;
            }

            var displayName = string.IsNullOrWhiteSpace(bonusCollectable.headerName)
                ? bonusCollectable.id
                : bonusCollectable.headerName;

            itemNotificationUI.ShowCustomNotification(displayName, bonusCollectable.icon, 1, NotificationType.Added);
        }

        private int CountStarterCollectables(int level)
        {
            if (starterCollectableService == null)
            {
                return 0;
            }

            return starterCollectableService.GetStarterCollectableCountForLevel(level);
        }

        private int CountUnlockedCollectablesInBiome(CollectableBiome biome)
        {
            var configuredCollectables = collectablesHubUI != null
                ? collectablesHubUI.GetConfiguredCollectables()
                : null;

            if (configuredCollectables == null || configuredCollectables.Length == 0)
            {
                return 0;
            }

            var uniqueIds = new HashSet<string>();
            int count = 0;

            for (int i = 0; i < configuredCollectables.Length; i++)
            {
                var item = configuredCollectables[i];
                if (item == null || item.biome != biome || string.IsNullOrWhiteSpace(item.id))
                {
                    continue;
                }

                if (!uniqueIds.Add(item.id))
                {
                    continue;
                }

                if (_collectableManager.IsUnlocked(item.id))
                {
                    count++;
                }
            }

            return count;
        }

        private void OnValidate()
        {
            if (bonusConfig == null)
            {
                return;
            }

            if (collectablesHubUI == null)
            {
                Debug.LogWarning("[LevelBonusCollectableService] CollectablesHubUI is not assigned. Bonus progression counting will not work.", this);
            }

            var seenLevels = new HashSet<int>();
            var rules = bonusConfig.GetAllRules();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    continue;
                }

                if (!seenLevels.Add(rule.level))
                {
                    Debug.LogWarning($"[LevelBonusCollectableService] Duplicate bonus rule detected for level {rule.level}.", this);
                }

                if (rule.bonusCollectable == null)
                {
                    Debug.LogWarning($"[LevelBonusCollectableService] Missing bonus collectable in level rule {rule.level}.", this);
                }
            }
        }

        public static CollectableBiome GetBiomeForLevel(int level)
        {
            return level switch
            {
                1 => CollectableBiome.Forest,
                2 => CollectableBiome.Desert,
                3 => CollectableBiome.Tundra,
                _ => CollectableBiome.Forest
            };
        }
    }
}
