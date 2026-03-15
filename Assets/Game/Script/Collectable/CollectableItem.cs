using UnityEngine;
using Game.Dialog;

namespace Game.Collectable
{
    [CreateAssetMenu(fileName = "CollectableItem", menuName = "Game/Collectable/Collectable Item")]
    public class CollectableItem : ScriptableObject
    {
        public string id;
        public string headerName;

        [TextArea(3, 12)]
        public string content;

        public CollectableType type;
        public Sprite icon;

        // Used when type is ScriptDialog to support hub replay.
        public DialogData dialogData;
    }
}
