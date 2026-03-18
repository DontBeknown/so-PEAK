using System.Collections.Generic;
using UnityEngine;
using Game.Core.DI;
using Game.Core.Events;

namespace Game.Collectable
{
    public class CollectableManager : MonoBehaviour, ICollectableManager
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();
        private IEventBus _eventBus;

        /// <summary>Called by GameServiceBootstrapper after registration.</summary>
        public void Initialize(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public bool IsUnlocked(string collectableId)
        {
            return !string.IsNullOrWhiteSpace(collectableId) && _unlocked.Contains(collectableId);
        }

        public void Unlock(CollectableItem collectable)
        {
            if (collectable == null || string.IsNullOrWhiteSpace(collectable.id))
                return;

            if (!_unlocked.Add(collectable.id))
                return;

            _eventBus?.Publish(new CollectableUnlockedEvent(collectable));
        }

        public IReadOnlyCollection<string> GetUnlockedIds()
        {
            return new List<string>(_unlocked);
        }

        public void LoadState(List<string> unlockedIds)
        {
            _unlocked.Clear();
            if (unlockedIds == null)
                return;

            foreach (var id in unlockedIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _unlocked.Add(id);
                }
            }
        }
    }
}
