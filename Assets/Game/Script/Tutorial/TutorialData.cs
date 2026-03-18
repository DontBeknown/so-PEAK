using System.Collections.Generic;
using UnityEngine;

namespace Game.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialData", menuName = "Game/Tutorial/Data")]
    public class TutorialData : ScriptableObject
    {
        public string tutorialId = "main_onboarding";
        public List<TutorialStepData> steps = new List<TutorialStepData>();
    }
}
