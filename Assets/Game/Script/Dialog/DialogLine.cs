using System;

namespace Game.Dialog
{
    [Serializable]
    public struct DialogLine
    {
        public string speakerName;

        [UnityEngine.TextArea(2, 6)]
        public string text;
    }
}
