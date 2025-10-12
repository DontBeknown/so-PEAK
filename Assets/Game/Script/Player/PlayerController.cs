using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;

    private PlayerModel model;
    private IPlayerState currentState;
    private IA_PlayerController inputActions;
    private Vector2 moveInput;

    void Awake()
    {
        model = new PlayerModel(gameObject, config);

        inputActions = new IA_PlayerController();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

        inputActions.Player.Jump.performed += _ => currentState?.OnJump(model, moveInput);
        inputActions.Player.Climb.performed += _ => currentState?.OnClimb(model);
    }

    void Start()
    {
        ChangeState(new WalkingState());
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void FixedUpdate()
    {
        currentState?.FixedUpdate(model, moveInput);

        if (!(currentState is ClimbingState))
        {
            if (!model.IsGrounded())
                ChangeState(new FallingState());
            else if (!(currentState is WalkingState))
                ChangeState(new WalkingState());
        }
    }

    public void ChangeState(IPlayerState newState)
    {
        currentState?.Exit(model);
        currentState = newState;
        currentState.Enter(model);
    }
}
