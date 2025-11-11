using UnityEngine;
using Game.Player.Interfaces;
using Game.Player.Services;
using Game.Player.Data;

/// <summary>
/// Refactored PlayerModel following SOLID principles.
/// Acts as an aggregate root coordinating player components and services.
/// Separates concerns: physics, movement, animation via services.
/// </summary>
public class PlayerModelRefactored
{
    // Core Components
    public Transform Transform { get; private set; }
    public CharacterController Controller { get; private set; }
    public PlayerStats Stats { get; private set; }
    
    // Configuration Data
    private readonly PlayerData _data;
    
    // Services (Dependency Injection)
    private readonly IPhysicsService _physicsService;
    private readonly IAnimationService _animationService;
    private readonly ICameraProvider _cameraProvider;
    
    // Movement Context
    private readonly IMovementContext _movementContext;
    
    // State
    public Vector3 Velocity
    {
        get => _movementContext.Velocity;
        set => _movementContext.Velocity = value;
    }

    /// <summary>
    /// Constructor using Dependency Injection
    /// </summary>
    public PlayerModelRefactored(
        GameObject owner,
        PlayerConfig config,
        IPhysicsService physicsService = null,
        IAnimationService animationService = null,
        ICameraProvider cameraProvider = null)
    {
        // Get core components
        Transform = owner.transform;
        Controller = owner.GetComponent<CharacterController>();
        Stats = owner.GetComponent<PlayerStats>();

        // Validate required components
        if (Controller == null)
        {
            Debug.LogError("PlayerModel: CharacterController is required!");
        }

        // Convert config to data
        _data = PlayerData.FromConfig(config);

        // Initialize services (with defaults if not provided)
        _physicsService = physicsService ?? new PlayerPhysicsService(Transform, Controller, config);
        
        Animator animator = owner.GetComponentInChildren<Animator>();
        _animationService = animationService ?? new PlayerAnimationService(animator, Transform);
        
        _cameraProvider = cameraProvider ?? new MainCameraProvider();

        // Create movement context
        _movementContext = new PlayerMovementContext(
            Transform,
            Controller,
            _data,
            _physicsService,
            _cameraProvider,
            _animationService,
            Stats
        );
    }

    /// <summary>
    /// Gets the movement context for use by states and strategies
    /// </summary>
    public IMovementContext GetMovementContext() => _movementContext;

    /// <summary>
    /// Gets the physics service
    /// </summary>
    public IPhysicsService GetPhysicsService() => _physicsService;

    /// <summary>
    /// Gets the animation service
    /// </summary>
    public IAnimationService GetAnimationService() => _animationService;

    /// <summary>
    /// Gets the camera provider
    /// </summary>
    public ICameraProvider GetCameraProvider() => _cameraProvider;

    // Keep backward compatibility properties
    public float WalkSpeed => _data.WalkSpeed;
    public float ClimbSpeed => _data.ClimbSpeed;
    public float JumpForce => _data.JumpForce;
    public float RotationSmoothness => _data.RotationSmoothness;
    public float ClimbDetectionRange => _data.ClimbDetectionRange;
    public LayerMask ClimbableLayer => _data.ClimbableLayer;
    public float GroundCheckDistance => _data.GroundCheckDistance;
    public LayerMask GroundLayer => _data.GroundLayer;

    // Delegate methods to services for backward compatibility
    public bool IsGrounded() => _physicsService.IsGrounded();
    
    public void Move(Vector3 motion) => _movementContext.Move(motion);
    
    public void ApplyGravity(float gravity) => _movementContext.ApplyGravity(gravity);

    public void Jump()
    {
        if (IsGrounded())
        {
            Vector3 vel = Velocity;
            vel.y = JumpForce;
            Velocity = vel;
        }
    }

    public void JumpWithMomentum(Vector3 moveDirection)
    {
        if (!IsGrounded()) return;

        Vector3 horizontal = new Vector3(moveDirection.x, 0f, moveDirection.z) * WalkSpeed;
        Velocity = new Vector3(horizontal.x, JumpForce, horizontal.z);
    }

    public bool TryClimb(out RaycastHit hit)
    {
        return _physicsService.TryDetectClimbable(out hit);
    }

    public bool TryMantleTopFrom(Vector3 contactPoint, Vector3 wallNormal, float upCheck, float forwardOffset, float downCheck, out Vector3 topPoint)
    {
        return _physicsService.CanMantle(contactPoint, wallNormal, out topPoint);
    }

    public void SnapToTop(Vector3 topPoint)
    {
        bool wasEnabled = Controller.enabled;
        Controller.enabled = false;

        float halfHeight = Controller.height * 0.5f;
        Transform.position = topPoint + Vector3.up * (halfHeight + Controller.skinWidth + 0.01f);
        Velocity = Vector3.zero;

        Controller.enabled = wasEnabled;
    }

    /// <summary>
    /// Gets player configuration data (read-only)
    /// </summary>
    public PlayerData GetData() => _data;
}
