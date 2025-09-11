using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float walkSpeed = 3f;
    public float climbSpeed = 2f;
    public float jumpForce = 5f;
    public float rotationSmoothness = 10f;
    public float climbDetectionRange = 1f;
    public LayerMask climbableLayer;
    public float gravity = -9.81f;

    private PlayerModel model;
    private IPlayerState currentState;
    private IA_PlayerController inputActions;
    private Vector2 moveInput;

    void Awake()
    {
        model = new PlayerModel(gameObject, walkSpeed, climbSpeed, jumpForce,
                                rotationSmoothness, climbDetectionRange, climbableLayer);

        inputActions = new IA_PlayerController();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
        inputActions.Player.Jump.performed += _ => currentState?.OnJump(model);
        inputActions.Player.Climb.performed += _ => currentState?.OnClimb(model);
    }

    void Start() => ChangeState(new WalkingState());
    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void FixedUpdate()
    {
        currentState?.FixedUpdate(model, moveInput);

        if (!(currentState is ClimbingState))
        {
            if (!model.IsGrounded())
                ChangeState(new FallingState());
            else
                ChangeState(new WalkingState());
        }

        Debug.Log(model.IsGrounded());
    }

    public void ChangeState(IPlayerState newState)
    {
        currentState?.Exit(model);
        currentState = newState;
        currentState.Enter(model);
    }
}
