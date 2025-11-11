namespace Game.Player.Inventory.Commands
{
    /// <summary>
    /// Command Pattern interface for inventory operations.
    /// Allows for undo/redo functionality and better testability.
    /// Follows Command Pattern and Single Responsibility Principle.
    /// </summary>
    public interface IInventoryCommand
    {
        /// <summary>
        /// Execute the inventory operation
        /// </summary>
        /// <returns>True if the command executed successfully, false otherwise</returns>
        bool Execute();

        /// <summary>
        /// Undo the inventory operation (if supported)
        /// </summary>
        /// <returns>True if the command was undone successfully, false otherwise</returns>
        bool Undo();

        /// <summary>
        /// Whether this command can be undone
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Description of the command for logging/debugging
        /// </summary>
        string Description { get; }
    }
}
