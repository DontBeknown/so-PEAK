namespace Game.UI
{
    /// <summary>
    /// Interface for cursor management
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface ICursorManager
    {
        void LockCursor();
        void UnlockCursor();
        void ShowCursor();
        void HideCursor();
        bool IsCursorLocked { get; }
        bool IsCursorVisible { get; }
    }
}
