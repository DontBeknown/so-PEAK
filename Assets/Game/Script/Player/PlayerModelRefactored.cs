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
    
    // Configuration (direct reference for runtime debugging in Unity Inspector)
    private readonly PlayerConfig _config;
    
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

        // Store config reference (allows runtime changes in Inspector)
        _config = config;

        // Initialize services (with defaults if not provided)
        _physicsService = physicsService ?? new PlayerPhysicsService(Transform, Controller, config);
        
        Animator animator = owner.GetComponentInChildren<Animator>();
        _animationService = animationService ?? new PlayerAnimationService(animator, Transform);
        
        _cameraProvider = cameraProvider ?? new MainCameraProvider();

        // Create movement context
        _movementContext = new PlayerMovementContext(
            Transform,
            Controller,
            PlayerData.FromConfig(config),
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

    // Direct config properties (reads live values from Inspector for easy debugging)
    public float WalkSpeed => _config.walkSpeed;
    public float ClimbSpeed => _config.climbSpeed;
    public float JumpForce => _config.jumpForce;
    public float RotationSmoothness => _config.rotationSmoothness;
    public float ClimbDetectionRange => _config.climbDetectionRange;
    public LayerMask ClimbableLayer => _config.climbableLayer;
    public float GroundCheckDistance => _config.groundCheckDistance;
    public LayerMask GroundLayer => _config.groundLayer;

    // Delegate methods to services for backward compatibility
    public bool IsGrounded() => _physicsService.IsGrounded();
    
    public void Move(Vector3 motion) => _movementContext.Move(motion);
    
    public void ApplyGravity(float gravity) => _movementContext.ApplyGravity(gravity);

    public void Jump()
    {
        if (IsGrounded())
        {
            // Clear horizontal velocity for a pure vertical jump
            Velocity = new Vector3(0f, JumpForce, 0f);
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
    /// Gets the player configuration for direct access
    /// </summary>
    public PlayerConfig GetConfig() => _config;
}
