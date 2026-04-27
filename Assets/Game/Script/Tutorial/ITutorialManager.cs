namespace Game.Tutorial
{
    public interface ITutorialManager
    {
        bool IsActive { get; }
        bool IsCompleted { get; }
        int CurrentStepIndex { get; }
        float CurrentStepProgress { get; }

        void StartTutorial();
        void SkipTutorial();
        void CompleteCurrentStep();
        void SyncToSaveData(TutorialSaveData tutorialSaveData);
    }
}
