using System.Collections.Generic;
using UnityEngine;
using Game.Collectable;

namespace Game.Progression
{
    [System.Serializable]
    public class LevelBonusCollectableRule
    {
        [Min(1)] public int level = 1;
        [Min(0)] public int requiredCollectedAfterStarter = 5;
        public CollectableItem bonusCollectable;
    }

    [CreateAssetMenu(fileName = "LevelBonusCollectableConfig", menuName = "Game/Collectable/Level Bonus Collectable Config")]
    public class LevelBonusCollectableConfig : ScriptableObject
    {
        [SerializeField] private List<LevelBonusCollectableRule> rules = new List<LevelBonusCollectableRule>();

        public LevelBonusCollectableRule GetRuleForLevel(int level)
        {
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] != null && rules[i].level == level)
                {
                    return rules[i];
                }
            }

            return null;
        }

        public IReadOnlyList<LevelBonusCollectableRule> GetAllRules()
        {
            return rules;
        }
    }
}
