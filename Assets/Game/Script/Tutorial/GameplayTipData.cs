using UnityEngine;

namespace Game.Tutorial
{
    [System.Serializable]
    public class GameplayTipSlide
    {
        public string title;
        [TextArea(2, 5)] public string bodyText;
        public Sprite illustration;
    }

    [CreateAssetMenu(menuName = "Game/Tutorial/Gameplay Tip", fileName = "NewGameplayTip")]
    public class GameplayTipData : ScriptableObject
    {
        public string tipId;
        public GameplayTipSlide[] slides;
    }
}
