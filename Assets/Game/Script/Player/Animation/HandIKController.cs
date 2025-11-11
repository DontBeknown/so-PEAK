using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HandIKController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Rig handIKRig; // Reference to the HandIK Rig component
    
    [Header("Hand Targets (Optional)")]
    [SerializeField] private Transform leftHandTarget; // Left hand IK target
    [SerializeField] private Transform rightHandTarget; // Right hand IK target
    [SerializeField] private LayerMask climbableLayer; // Wall layer

    [Header("Hand IK Settings")]
    [SerializeField] private bool enableHandIK = true;
    [SerializeField][Range(0f, 1f)] private float handIKWeight = 1f;
    [SerializeField] private float handRigBlendSpeed = 10f; // Speed for rig weight transition
    
    [Header("Hand Positioning")]
    [SerializeField] private bool autoPositionHands = false; // Enable automatic hand positioning
    [SerializeField] private float handReachDistance = 1.5f; // How far to raycast for wall
    [SerializeField] private float handOffsetFromWall = 0.05f; // Distance from wall surface
    [SerializeField] private float handHorizontalSpread = 0.4f; // How far left/right from center
    [SerializeField] private float handHeightOffset = 0.2f; // Height offset from character center

    private float currentHandRigWeight;
    private bool isClimbing;

    private void Start()
    {
        // Auto-assign PlayerController if not set
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogWarning("HandIKController: No PlayerController found! Hand IK will not work properly.");
        }

        // Auto-assign HandIK Rig if not set
        if (handIKRig == null && enableHandIK)
        {
            // Try to find Rig component in children
            handIKRig = GetComponentInChildren<Rig>();
            
            if (handIKRig == null)
            {
                Debug.LogWarning("HandIKController: No HandIK Rig found! Hand IK will be disabled.");
                enableHandIK = false;
            }
        }
    }

    private void Update()
    {
        if (!enableHandIK || handIKRig == null) return;

        // Check if character is climbing from PlayerController state
        if (playerController != null)
        {
            isClimbing = playerController.GetCurrentState() is ClimbingState;
        }

        // Control Rig weight based on climbing state
        if (isClimbing)
        {
            // Smoothly blend rig weight to target when climbing
            currentHandRigWeight = Mathf.Lerp(currentHandRigWeight, handIKWeight, Time.deltaTime * handRigBlendSpeed);
            
            // Auto-position hands if enabled
            if (autoPositionHands && leftHandTarget != null && rightHandTarget != null)
            {
                PositionHandOnWall(leftHandTarget, -handHorizontalSpread);
                PositionHandOnWall(rightHandTarget, handHorizontalSpread);
            }
        }
        else
        {
            // Fade out rig weight when not climbing
            currentHandRigWeight = Mathf.Lerp(currentHandRigWeight, 0f, Time.deltaTime * handRigBlendSpeed);
        }
        
        // Apply the weight to the Rig
        handIKRig.weight = currentHandRigWeight;
    }

    private void PositionHandOnWall(Transform handTarget, float horizontalOffset)
    {
        // Calculate raycast origin (from character position with offset)
        Vector3 origin = transform.position + Vector3.up * handHeightOffset + transform.right * horizontalOffset;
        Vector3 direction = transform.forward;
        
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, handReachDistance, climbableLayer, QueryTriggerInteraction.Ignore))
        {
            // Position hand on wall surface with offset
            Vector3 targetPosition = hit.point - hit.normal * handOffsetFromWall;
            handTarget.position = targetPosition;
            
            // Optional: Rotate hand to face wall
            handTarget.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            
            // Debug visualization
            #if UNITY_EDITOR
            Debug.DrawLine(origin, hit.point, Color.cyan);
            Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.yellow);
            #endif
        }
        else
        {
            #if UNITY_EDITOR
            Debug.DrawLine(origin, origin + direction * handReachDistance, Color.red);
            #endif
        }
    }

    /// <summary>
    /// Enable or disable hand IK at runtime
    /// </summary>
    public void SetHandIKEnabled(bool enabled)
    {
        enableHandIK = enabled;
    }

    /// <summary>
    /// Set the hand IK weight (0-1)
    /// </summary>
    public void SetHandIKWeight(float weight)
    {
        handIKWeight = Mathf.Clamp01(weight);
    }

    /// <summary>
    /// Get the current hand rig weight
    /// </summary>
    public float GetCurrentRigWeight()
    {
        return currentHandRigWeight;
    }
}
