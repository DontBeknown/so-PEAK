using UnityEngine;

public class PlayerModel
{
    public CharacterController Controller { get; }
    public Transform Transform { get; }
    public PlayerAnimator Animator { get; }
    public PlayerStats Stats { get; private set; }

    // Settings
    public float WalkSpeed { get; }
    public float ClimbSpeed { get; }
    public float JumpForce { get; }
    public float RotationSmoothness { get; }
    public float ClimbDetectionRange { get; }
    public LayerMask ClimbableLayer { get; }
    public float GroundCheckDistance { get; }
    public LayerMask GroundLayer { get; }

    // Physics
    public Vector3 Velocity;

    public PlayerModel(GameObject owner, PlayerConfig config)
    {
        Transform = owner.transform;
        Controller = owner.GetComponent<CharacterController>();
        Animator = new PlayerAnimator(owner.GetComponentInChildren<Animator>(), Transform);
        Stats = owner.GetComponent<PlayerStats>();

        WalkSpeed = config.walkSpeed;
        ClimbSpeed = config.climbSpeed;
        JumpForce = config.jumpForce;
        RotationSmoothness = config.rotationSmoothness;
        ClimbDetectionRange = config.climbDetectionRange;
        ClimbableLayer = config.climbableLayer;
        GroundCheckDistance = config.groundCheckDistance;
        GroundLayer = config.groundLayer;
    }

    public bool IsGrounded()
    {
        // Use a combination of CharacterController's built-in check and a custom raycast
        // This helps detect ground on slopes more reliably
        if (Controller.isGrounded)
            return true;

        // Perform a spherecast from slightly above the bottom of the character controller
        // Start from high enough so the sphere doesn't overlap ground initially
        float sphereRadius = Controller.radius * 0.9f;
        Vector3 origin = Transform.position + Vector3.up * (Controller.height * 0.5f);
        float maxDistance = (Controller.height * 0.5f) + GroundCheckDistance;
        
        bool hit = Physics.SphereCast(origin, sphereRadius, Vector3.down, 
                                     out RaycastHit hitInfo, maxDistance, GroundLayer, QueryTriggerInteraction.Ignore);

        // Debug visualization
        #if UNITY_EDITOR
        /*Color debugColor = hit ? Color.green : Color.red;
        Vector3 endPoint = origin + Vector3.down * maxDistance;
        
        // Draw the spherecast path
        Debug.DrawLine(origin, endPoint, debugColor);
        
        // Draw sphere at start
        DrawDebugSphere(origin, sphereRadius, debugColor);
        
        // Draw sphere at end or hit point
        Vector3 sphereEndPos = hit ? origin + Vector3.down * hitInfo.distance : endPoint;
        DrawDebugSphere(sphereEndPos, sphereRadius, debugColor);*/
        #endif
        
        return hit;
    }

    #if UNITY_EDITOR
    private void DrawDebugSphere(Vector3 center, float radius, Color color)
    {
        // Draw sphere using debug lines (circles in 3 planes)
        int segments = 16;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            // XY plane
            Debug.DrawLine(
                center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0),
                center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0),
                color
            );
            
            // XZ plane
            Debug.DrawLine(
                center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius),
                center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius),
                color
            );
            
            // YZ plane
            Debug.DrawLine(
                center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius),
                center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius),
                color
            );
        }
    }
    #endif

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
    public void JumpWithMomentum(Vector3 moveDirection)
    {
        if (!IsGrounded()) return;

        Vector3 horiz = new Vector3(moveDirection.x, 0f, moveDirection.z) * WalkSpeed;
        Velocity = horiz;
        Velocity.y = JumpForce;
    }

    public bool TryClimb(out RaycastHit hit)
    {
        return Physics.SphereCast(Transform.position + Vector3.up * 0.5f, 0.3f,
                                  Transform.forward, out hit, ClimbDetectionRange, ClimbableLayer);
    }

    public bool TryMantleTopFrom(Vector3 contactPoint, Vector3 wallNormal, float upCheck, float forwardOffset, float downCheck, out Vector3 topPoint)
    {
        topPoint = default;

        // Peek from a little above the contact, then over the lip
        Vector3 startAbove = contactPoint + Vector3.up * upCheck - wallNormal * 0.03f;
        Vector3 overLip = startAbove - wallNormal * forwardOffset;

        // Make sure there's headroom where we intend to go (capsule check)
        float radius = Controller.radius * 0.9f;
        float height = Mathf.Max(Controller.height * 0.95f, radius * 2f);
        Vector3 bottom = overLip + Vector3.up * radius;
        Vector3 top = overLip + Vector3.up * (height - radius);

        if (Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
            return false; // space is blocked

        // Find ground under that spot
        if (Physics.Raycast(overLip, Vector3.down, out RaycastHit downHit, downCheck, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(downHit.normal, Vector3.up) <= Controller.slopeLimit + 0.1f)
            {
                topPoint = downHit.point;
                return true;
            }
        }

        return false;
    }

    // Place controller on top safely
    public void SnapToTop(Vector3 topPoint)
    {
        bool wasEnabled = Controller.enabled;
        Controller.enabled = false;

        float half = Controller.height * 0.5f;
        Transform.position = topPoint + Vector3.up * (half + Controller.skinWidth + 0.01f);
        Velocity = Vector3.zero;

        Controller.enabled = wasEnabled;
    }

}
