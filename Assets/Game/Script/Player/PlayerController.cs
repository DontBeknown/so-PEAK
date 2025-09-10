using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float walkSpeed = 3f;
    public float climbSpeed = 2f;
    public float jumpForce = 5f;
    public float climbDetectionRange = 1f;
    public float capsuleRadius = 0.6f;
    public float wallOffset = 0.05f;
    public float rotationSmoothness = 10f;
    public LayerMask climbableLayer;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public PlayerAnimator playerAnimator;
    [HideInInspector] public IA_PlayerController inputActions;
    [HideInInspector] public Vector2 moveInput;

    private IPlayerState currentState;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        Animator animatorComponent = GetComponentInChildren<Animator>();
        playerAnimator = new PlayerAnimator(animatorComponent, transform);
        
        inputActions = new IA_PlayerController();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += ctx => Jump();
        inputActions.Player.Climb.performed += ctx => TryClimb();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        ChangeState(new WalkingState());
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void FixedUpdate()
    {
        currentState?.FixedUpdate(this);

        if (!(currentState is ClimbingState))
        {
            //bool grounded = IsGrounded();

            if (rb.linearVelocity.y <= 0.1f && !(currentState is WalkingState))
                ChangeState(new WalkingState());
            else if (currentState is WalkingState)
                ChangeState(new FallingState());
        }

        rb.useGravity = !(currentState is ClimbingState);
    }

    public void ChangeState(IPlayerState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState?.Enter(this);
    }

    public void Jump()
    {
        if (currentState is WalkingState)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            ChangeState(new FallingState());
        }
        else if (currentState is ClimbingState)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Vector3.up * jumpForce + -transform.forward * 2f, ForceMode.VelocityChange);
            ChangeState(new FallingState());
            rb.useGravity = true;
        }
    }

    public void TryClimb()
    {
        if (currentState is ClimbingState)
        {
            ChangeState(new FallingState());
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;
        float radius = 0.3f;

        if (Physics.SphereCast(origin,
                               radius,
                               dir,
                               out RaycastHit hit,
                               climbDetectionRange,
                               climbableLayer))
        {

            ChangeState(new ClimbingState());
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = false;
        }
    }

    public static Vector2 SquareToCircle(Vector2 input)
    {
        return (input.sqrMagnitude >= 1f) ? input.normalized : input;
    }

    public bool IsGrounded()
    {
        float checkDist = capsuleRadius + 0.05f;
        return Physics.Raycast(transform.position, Vector3.down, out _, checkDist);
    }
}
