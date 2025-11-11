using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player
{
    /// <summary>
    /// Context object containing all data needed by player states.
    /// Replaces PlayerModel to provide cleaner interface.
    /// </summary>
    public class PlayerStateContext
    {
        public IMovementContext MovementContext { get; }
        public IStateTransitioner StateTransitioner { get; }
        public IInputService InputService { get; }
        public IPhysicsService PhysicsService { get; }

        public PlayerStateContext(
            IMovementContext movementContext,
            IStateTransitioner stateTransitioner,
            IInputService inputService,
            IPhysicsService physicsService)
        {
            MovementContext = movementContext;
            StateTransitioner = stateTransitioner;
            InputService = inputService;
            PhysicsService = physicsService;
        }

        // Convenience accessors
        public Transform Transform => MovementContext.Transform;
        public Vector3 Velocity
        {
            get => MovementContext.Velocity;
            set => MovementContext.Velocity = value;
        }
        public PlayerStats Stats => MovementContext.Stats;
    }
}
