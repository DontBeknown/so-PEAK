using UnityEngine;

namespace Game.Player.Interfaces
{
    /// <summary>
    /// Context interface providing all necessary data for movement strategies.
    /// </summary>
    public interface IMovementContext
    {
        // Transform and Physics
        Transform Transform { get; }
        CharacterController Controller { get; }
        Vector3 Velocity { get; set; }

        // Configuration
        float WalkSpeed { get; }
        float ClimbSpeed { get; }
        float JumpForce { get; }
        float RotationSmoothness { get; }

        // Services
        IPhysicsService PhysicsService { get; }
        ICameraProvider CameraProvider { get; }
        IAnimationService AnimationService { get; }

        // Stats (optional, can be null)
        PlayerStats Stats { get; }

        // Helper Methods
        void Move(Vector3 motion);
        void ApplyGravity(float gravity);
    }
}
