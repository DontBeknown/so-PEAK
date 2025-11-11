using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// Strategy interface for different movement types.
    /// Implements Strategy Pattern for movement behaviors (Walk, Climb, Fall, etc.)
    /// </summary>
    public interface IMovementStrategy
    {
        /// <summary>
        /// Executes the movement logic for this strategy
        /// </summary>
        /// <param name="context">Context containing all necessary movement data</param>
        /// <param name="input">Player input vector</param>
        void Execute(IMovementContext context, Vector2 input);

        /// <summary>
        /// Called when this movement strategy becomes active
        /// </summary>
        /// <param name="context">Context containing all necessary movement data</param>
        void OnEnter(IMovementContext context);

        /// <summary>
        /// Called when this movement strategy stops being active
        /// </summary>
        /// <param name="context">Context containing all necessary movement data</param>
        void OnExit(IMovementContext context);

        /// <summary>
        /// Handles jump input for this movement type
        /// </summary>
        /// <param name="context">Context containing all necessary movement data</param>
        /// <param name="input">Player input vector</param>
        void HandleJump(IMovementContext context, Vector2 input);
    }
}
