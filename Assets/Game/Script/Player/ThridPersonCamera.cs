using UnityEngine;

public class ThridPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    public float distance = 4f;
    public float minDistance = 1f;
    public float height = 2f;
    public float sensitivity = 2f;
    public float smoothTime = 0.1f;
    public float verticalSmoothTime = 0.2f;

    [Header("Collision")]
    public LayerMask collisionMask;

    private Vector2 lookInput;
    private Vector3 velocity;
    private float yaw;
    private float pitch = 15f;
    private float smoothedTargetY;
    private float verticalVelocity;

    private IA_PlayerController inputActions;

    void Awake()
    {
        inputActions = new IA_PlayerController();
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    void Start()
    {
        if (target)
        {
            smoothedTargetY = target.position.y;
        }
        
        // Hide and lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void LateUpdate()
    {
        if (!target) return;
        
        // Don't rotate camera when cursor is visible (inventory is open)
        bool canRotateCamera = Cursor.lockState == CursorLockMode.Locked;

        // --- Smooth Target Y Position (for stairs) ---
        smoothedTargetY = Mathf.SmoothDamp(smoothedTargetY, target.position.y, ref verticalVelocity, verticalSmoothTime);
        Vector3 smoothedTargetPosition = new Vector3(target.position.x, smoothedTargetY, target.position.z);

        // --- Look Rotation (only when camera can rotate) ---
        if (canRotateCamera)
        {
            yaw += lookInput.x * sensitivity;
            pitch -= lookInput.y * sensitivity;
            pitch = Mathf.Clamp(pitch, -30f, 70f);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // --- Target Look Position ---
        Vector3 targetLookPosition = smoothedTargetPosition + Vector3.up * height * 0.5f;

        // --- Desired Camera Position ---
        Vector3 desiredPosition = smoothedTargetPosition - rotation * Vector3.forward * distance + Vector3.up * height;

        // --- Collision Check ---
        Vector3 dir = desiredPosition - targetLookPosition;
        float desiredDist = dir.magnitude;

        if (Physics.SphereCast(targetLookPosition, 0.2f, dir.normalized, out RaycastHit hit, desiredDist, collisionMask))
        {
            desiredPosition = targetLookPosition + dir.normalized * Mathf.Max(hit.distance - 0.2f, minDistance);
        }

        // --- Clamp Min Distance ---
        float distToTarget = Vector3.Distance(targetLookPosition, desiredPosition);
        if (distToTarget < minDistance)
        {
            desiredPosition = smoothedTargetPosition - rotation * Vector3.forward * minDistance + Vector3.up * height;
        }

        // --- Smooth Move & Look ---
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetLookPosition - transform.position), smoothTime * 10f);
    }
}
