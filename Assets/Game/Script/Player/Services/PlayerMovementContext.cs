using UnityEngine;
using Game.Player.Interfaces;
using Game.Player.Data;

namespace Game.Player.Services
{
    /// <summary>
    /// Concrete implementation of IMovementContext.
    /// Provides all necessary data and services for movement strategies.
    /// </summary>
    public class PlayerMovementContext : IMovementContext
    {
        private readonly Transform _transform;
        private readonly CharacterController _controller;
        private readonly PlayerData _data;
        private readonly IPhysicsService _physicsService;
        private readonly ICameraProvider _cameraProvider;
        private readonly IAnimationService _animationService;
        private readonly PlayerStats _stats;

        public Transform Transform => _transform;
        public CharacterController Controller => _controller;
        public Vector3 Velocity { get; set; }

        public float WalkSpeed => _data.WalkSpeed;
        public float ClimbSpeed => _data.ClimbSpeed;
        public float JumpForce => _data.JumpForce;
        public float RotationSmoothness => _data.RotationSmoothness;

        public IPhysicsService PhysicsService => _physicsService;
        public ICameraProvider CameraProvider => _cameraProvider;
        public IAnimationService AnimationService => _animationService;
        public PlayerStats Stats => _stats;

        public PlayerMovementContext(
            Transform transform,
            CharacterController controller,
            PlayerData data,
            IPhysicsService physicsService,
            ICameraProvider cameraProvider,
            IAnimationService animationService,
            PlayerStats stats = null)
        {
            _transform = transform;
            _controller = controller;
            _data = data;
            _physicsService = physicsService;
            _cameraProvider = cameraProvider;
            _animationService = animationService;
            _stats = stats;
        }

        public void Move(Vector3 motion)
        {
            _controller.Move(motion * Time.fixedDeltaTime);
        }

        public void ApplyGravity(float gravity)
        {
            if (_physicsService.IsGrounded() && Velocity.y < 0f)
            {
                Velocity = new Vector3(Velocity.x, -2f, Velocity.z);
            }
            else
            {
                Velocity += Vector3.up * gravity * Time.fixedDeltaTime;
            }
        }
    }
}
