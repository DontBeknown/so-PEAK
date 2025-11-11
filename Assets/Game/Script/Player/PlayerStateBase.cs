using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player
{
    /// <summary>
    /// Abstract base class for all player states.
    /// Provides common functionality and enforces consistent structure.
    /// NOTE: Currently not used - all states implement IPlayerState directly.
    /// Keep for future use if needed.
    /// </summary>
    public abstract class PlayerStateBase : IPlayerState
    {
        protected IMovementStrategy MovementStrategy { get; set; }
        protected IStateTransitioner StateTransitioner { get; set; }

        protected PlayerStateBase()
        {
        }

        protected PlayerStateBase(IStateTransitioner stateTransitioner)
        {
            StateTransitioner = stateTransitioner;
        }

        public virtual void Enter(PlayerModelRefactored model)
        {
            if (MovementStrategy != null)
            {
                // Strategy can be used if needed
                // MovementStrategy.OnEnter(model.GetMovementContext());
            }
        }

        public virtual void Exit(PlayerModelRefactored model)
        {
            if (MovementStrategy != null)
            {
                // MovementStrategy.OnExit(model.GetMovementContext());
            }
        }

        public virtual void HandleInput(PlayerModelRefactored model, Vector2 input)
        {
        }

        public virtual void FixedUpdate(PlayerModelRefactored model, Vector2 input)
        {
            MovementStrategy?.Execute(model.GetMovementContext(), input);
        }

        public virtual void OnJump(PlayerModelRefactored model, Vector2 input)
        {
            MovementStrategy?.HandleJump(model.GetMovementContext(), input);
        }

        public virtual void OnClimb(PlayerModelRefactored model)
        {
            // Override in specific states
        }

        /// <summary>
        /// Validates that the state can be entered.
        /// Override to add custom validation logic.
        /// </summary>
        public virtual bool CanEnter(PlayerModelRefactored model)
        {
            return true;
        }

        /// <summary>
        /// Validates that the state can be exited.
        /// Override to add custom exit conditions.
        /// </summary>
        public virtual bool CanExit(PlayerModelRefactored model)
        {
            return true;
        }

        /// <summary>
        /// Helper method to transition to another state using the state transitioner
        /// </summary>
        protected void TransitionTo(IPlayerState newState)
        {
            if (StateTransitioner != null)
            {
                StateTransitioner.TransitionTo(newState);
            }
            else
            {
                Debug.LogWarning($"{GetType().Name}: No state transitioner available for transition!");
            }
        }

        /// <summary>
        /// Helper method to transition to another state using generic type
        /// </summary>
        protected void TransitionTo<TState>() where TState : IPlayerState, new()
        {
            TransitionTo(new TState());
        }
    }
}
