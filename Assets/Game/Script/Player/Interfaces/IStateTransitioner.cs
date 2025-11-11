namespace Game.Player.Interfaces
{
    /// <summary>
    /// Interface for changing player states.
    /// Breaks circular dependency between states and controller.
    /// </summary>
    public interface IStateTransitioner
    {
        /// <summary>
        /// Requests a transition to a new state
        /// </summary>
        /// <typeparam name="TState">Type of state to transition to</typeparam>
        void TransitionTo<TState>() where TState : IPlayerState, new();

        /// <summary>
        /// Requests a transition to a specific state instance
        /// </summary>
        /// <param name="newState">The state instance to transition to</param>
        void TransitionTo(IPlayerState newState);

        /// <summary>
        /// Gets the current active state
        /// </summary>
        IPlayerState CurrentState { get; }
    }
}
