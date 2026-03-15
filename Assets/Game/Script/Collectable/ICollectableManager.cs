using System.Collections.Generic;

namespace Game.Collectable
{
    public interface ICollectableManager
    {
        bool IsUnlocked(string collectableId);
        void Unlock(CollectableItem collectable);
        IReadOnlyCollection<string> GetUnlockedIds();
        void LoadState(List<string> unlockedIds);
    }
}
