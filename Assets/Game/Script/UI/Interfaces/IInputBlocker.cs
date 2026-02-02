namespace Game.UI
{
    /// <summary>
    /// Interface for blocking player input
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IInputBlocker
    {
        void BlockInput();
        void UnblockInput();
        bool IsInputBlocked { get; }
    }
}
