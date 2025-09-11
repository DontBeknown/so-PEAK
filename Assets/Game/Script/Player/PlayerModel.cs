using UnityEngine;

public class PlayerModel
{
    public CharacterController Controller { get; }
    public Transform Transform { get; }
    public PlayerAnimator Animator { get; }

    // Settings
    public float WalkSpeed { get; }
    public float ClimbSpeed { get; }
    public float JumpForce { get; }
    public float RotationSmoothness { get; }
    public float ClimbDetectionRange { get; }
    public LayerMask ClimbableLayer { get; }

    // Physics
    public Vector3 Velocity;

    public PlayerModel(GameObject owner, float walkSpeed, float climbSpeed, float jumpForce,
                       float rotationSmoothness, float climbDetectionRange, LayerMask climbableLayer)
    {
        Transform = owner.transform;
        Controller = owner.GetComponent<CharacterController>();
        Animator = new PlayerAnimator(owner.GetComponentInChildren<Animator>(), Transform);

        WalkSpeed = walkSpeed;
        ClimbSpeed = climbSpeed;
        JumpForce = jumpForce;
        RotationSmoothness = rotationSmoothness;
        ClimbDetectionRange = climbDetectionRange;
        ClimbableLayer = climbableLayer;
    }

    public bool IsGrounded() => Controller.isGrounded;

    public void Move(Vector3 motion) => Controller.Move(motion * Time.fixedDeltaTime);

    public void ApplyGravity(float gravity)
    {
        if (IsGrounded() && Velocity.y < 0f)
            Velocity.y = -2f;
        else
            Velocity.y += gravity * Time.fixedDeltaTime;
    }

    public void Jump()
    {
        if (IsGrounded())
            Velocity.y = JumpForce;
    }

    public bool TryClimb(out RaycastHit hit)
    {
        return Physics.SphereCast(Transform.position + Vector3.up * 0.5f, 0.3f,
                                  Transform.forward, out hit, ClimbDetectionRange, ClimbableLayer);
    }
}
