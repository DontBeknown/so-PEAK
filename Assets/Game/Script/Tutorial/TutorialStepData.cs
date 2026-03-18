using UnityEngine;

namespace Game.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialStep", menuName = "Game/Tutorial/Step")]
    public class TutorialStepData : ScriptableObject
    {
        public string stepId;
        public string title;
        [TextArea(2, 5)] public string instructionText;
        public string inputHintText;
        public TutorialStepType completionType;
        public float completionThreshold = 1f;
    }
}
