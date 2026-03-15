using System.Collections.Generic;
using UnityEngine;

namespace Game.Dialog
{
    [CreateAssetMenu(fileName = "DialogData", menuName = "Game/Dialog/Dialog Data")]
    public class DialogData : ScriptableObject
    {
        public string dialogId;
        public List<DialogLine> lines = new List<DialogLine>();
    }
}
