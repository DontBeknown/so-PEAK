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

    [Header("Collision")]
    public LayerMask collisionMask;

    private Vector2 lookInput;
    private Vector3 velocity;
    private float yaw;
    private float pitch = 15f;

    private IA_PlayerController inputActions;

    void Awake()
    {
        inputActions = new IA_PlayerController();
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void LateUpdate()
    {
        if (!target) return;

        // --- Look Rotation ---
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, -30f, 70f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // --- Desired Camera Position ---
        Vector3 desiredPosition = target.position - rotation * Vector3.forward * distance + Vector3.up * height;

        // --- Collision Check ---
        Vector3 dir = desiredPosition - target.position;
        float desiredDist = dir.magnitude;

        if (Physics.SphereCast(target.position + Vector3.up * height * 0.5f, 0.2f, dir.normalized, out RaycastHit hit, desiredDist, collisionMask))
        {
            desiredPosition = target.position + dir.normalized * (hit.distance - 0.2f);
        }

        // --- Clamp Min Distance ---
        float distToTarget = Vector3.Distance(target.position, desiredPosition);
        if (distToTarget < minDistance)
        {
            desiredPosition = target.position - rotation * Vector3.forward * minDistance + Vector3.up * height;
        }

        // --- Smooth Move & Look ---
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(target.position + Vector3.up * height * 0.5f);
    }
}
