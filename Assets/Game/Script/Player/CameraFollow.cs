using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 pivot;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float lookSensitivity = 2f;

    private Vector3 velo;
    private IA_PlayerController inputActions;
    private Vector2 lookInput;

    void Awake()
    {
        inputActions = new IA_PlayerController();
    }

    void OnEnable()
    {
        inputActions.Enable();
        //inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        //inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    void OnDisable()
    {
        //inputActions.Player.Look.performed -= ctx => lookInput = ctx.ReadValue<Vector2>();
        //inputActions.Player.Look.canceled -= ctx => lookInput = Vector2.zero;
        inputActions.Disable();
    }

    void Update()
    {
        Debug.Log("Look Input: " + lookInput);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        Vector2 rotationDelta = lookInput * lookSensitivity;

        // rotate offset by mouse delta from Look action
        Vector3 localRight = Vector3.Cross(Vector3.up, offset);
        offset = Quaternion.AngleAxis(rotationDelta.x, Vector3.up)
                * Quaternion.AngleAxis(-rotationDelta.y, localRight)
                * offset;


    }

    void FixedUpdate()
    {
        transform.position = Vector3.SmoothDamp(
            transform.position,
            target.position + pivot + offset,
            ref velo,
            0.5f,
            20f,
            Time.fixedDeltaTime);

        transform.forward = target.position - transform.position;
    }
}
