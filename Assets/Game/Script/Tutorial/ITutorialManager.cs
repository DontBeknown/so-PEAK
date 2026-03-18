namespace Game.Tutorial
{
    public interface ITutorialManager
    {
        bool IsActive { get; }
        bool IsCompleted { get; }
        int CurrentStepIndex { get; }

        void StartTutorial();
        void SkipTutorial();
    }
}
