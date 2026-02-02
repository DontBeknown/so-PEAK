namespace Game.UI
{
    /// <summary>
    /// Interface for all UI panels
    /// Follows Interface Segregation Principle
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// Shows the panel
        /// </summary>
        void Show();
        
        /// <summary>
        /// Hides the panel
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Toggles the panel visibility
        /// </summary>
        void Toggle();
        
        /// <summary>
        /// Returns true if panel is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Panel name for identification
        /// </summary>
        string PanelName { get; }
        
        /// <summary>
        /// Should this panel block player input?
        /// </summary>
        bool BlocksInput { get; }
        
        /// <summary>
        /// Should this panel unlock the cursor?
        /// </summary>
        bool UnlocksCursor { get; }
    }
}
