using UnityEngine;

public class FootIKController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask climbableLayer; // For wall detection when climbing

    [Header("IK Settings")]
    [SerializeField] private bool enableFootIK = true;
    [SerializeField][Range(0f, 1f)] private float ikWeight = 1f;
    [SerializeField][Range(0f, 1f)] private float rotationWeight = 1f; 
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private float raycastUpOffset = 1f;
    [SerializeField] private float footOffset = 0.05f;
    [SerializeField] private float minFootDistance = 0.05f;
    
    [Header("Climbing Foot IK")]
    [SerializeField] private float climbingFootReachDistance = 1f; // Distance to raycast forward for wall
    [SerializeField] private float climbingFootOffset = 0.08f; // Distance from wall surface

    [Header("Pelvis Adjustment")]
    [SerializeField] private bool enablePelvisAdjustment = true;
    [SerializeField] private float pelvisUpDownSpeed = 5f;
    [SerializeField] private float pelvisOffset = 0f;
    [SerializeField] private float maxPelvisAdjustment = 0.3f; 

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 10f;

    private float leftFootIKWeight;
    private float rightFootIKWeight;
    private float lastPelvisPositionY;
    private bool isClimbing;

    private void Start()
    {
        // Auto-assign animator if not set
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError("FootIKController: No Animator found!");
            enabled = false;
            return;
        }

        // Auto-assign PlayerController if not set
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogWarning("FootIKController: No PlayerController found! Will use animator parameter for climbing detection.");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Check if character is climbing from PlayerController state
        if (playerController != null)
        {
            isClimbing = playerController.GetCurrentState() is ClimbingState;
        }
        else
        {
            // Fallback to animator parameter if PlayerController not available
            isClimbing = animator.GetBool("isClimbing");
        }

        // Process Foot IK (always active unless disabled)
        if (enableFootIK)
        {
            // Smoothly adjust foot IK weights
            leftFootIKWeight = Mathf.Lerp(leftFootIKWeight, ikWeight, Time.deltaTime * smoothSpeed);
            rightFootIKWeight = Mathf.Lerp(rightFootIKWeight, ikWeight, Time.deltaTime * smoothSpeed);

            // Set foot IK weights
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootIKWeight * rotationWeight);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootIKWeight * rotationWeight);

            // Process both feet and get their adjusted heights
            float leftFootHeight = isClimbing ? ProcessClimbingFootIK(AvatarIKGoal.LeftFoot) : ProcessFootIK(AvatarIKGoal.LeftFoot);
            float rightFootHeight = isClimbing ? ProcessClimbingFootIK(AvatarIKGoal.RightFoot) : ProcessFootIK(AvatarIKGoal.RightFoot);

            // Adjust pelvis height based on feet positions (only when not climbing)
            if (enablePelvisAdjustment && !isClimbing)
            {
                AdjustPelvisHeight(leftFootHeight, rightFootHeight);
            }
        }
        else
        {
            // Fade out foot IK if disabled
            leftFootIKWeight = Mathf.Lerp(leftFootIKWeight, 0f, Time.deltaTime * smoothSpeed);
            rightFootIKWeight = Mathf.Lerp(rightFootIKWeight, 0f, Time.deltaTime * smoothSpeed);
            
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootIKWeight);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootIKWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootIKWeight);
        }
    }

    private float ProcessFootIK(AvatarIKGoal foot)
    {
        // Get the current foot position from the animation
        Vector3 footPosition = animator.GetIKPosition(foot);
        
        // Raycast from above the foot position downward
        // Use the higher of character position or foot position to handle both up and down slopes
        float rayStartY = Mathf.Max(transform.position.y, footPosition.y) + raycastUpOffset;
        Vector3 rayOrigin = new Vector3(footPosition.x, rayStartY, footPosition.z);
        RaycastHit hit;
        
        float totalRayDistance = raycastDistance + raycastUpOffset + Mathf.Abs(rayStartY - footPosition.y);
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, totalRayDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            // Calculate how much we need to adjust the foot
            float footAdjustment = hit.point.y - footPosition.y;
            
            // Apply IK if the adjustment is within reasonable limits
            if (Mathf.Abs(footAdjustment) < raycastDistance)
            {
                // Only apply IK if adjustment is significant enough (reduces jittering)
                if (Mathf.Abs(footAdjustment) > minFootDistance)
                {
                    // Set the foot position to the ground with a small offset
                    Vector3 targetPosition = hit.point + Vector3.up * footOffset;
                    animator.SetIKPosition(foot, targetPosition);
                    
                    // Align foot rotation to ground normal with improved calculation
                    // Use the actual foot forward direction instead of character forward
                    Vector3 footForward = transform.forward;
                    
                    // Project onto the slope plane
                    Vector3 slopeForward = Vector3.ProjectOnPlane(footForward, hit.normal).normalized;
                    
                    // Create rotation that aligns foot up with slope normal
                    Quaternion targetRotation = Quaternion.LookRotation(slopeForward, hit.normal);
                    animator.SetIKRotation(foot, targetRotation);

                    // Debug visualization
                    #if UNITY_EDITOR
                    Debug.DrawLine(rayOrigin, hit.point, Color.green);
                    Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.blue);
                    Debug.DrawLine(footPosition, targetPosition, Color.yellow);
                    Debug.DrawRay(hit.point, slopeForward * 0.15f, Color.cyan); // Show foot forward
                    #endif

                    // Return the height offset needed (positive for upward, negative for downward)
                    return footAdjustment;
                }
            }
            else
            {
                #if UNITY_EDITOR
                Debug.DrawLine(rayOrigin, hit.point, Color.yellow);
                #endif
            }
        }
        else
        {
            #if UNITY_EDITOR
            Debug.DrawLine(rayOrigin, rayOrigin + Vector3.down * totalRayDistance, Color.red);
            #endif
        }

        return 0f;
    }

    private float ProcessClimbingFootIK(AvatarIKGoal foot)
    {
        // Get the current foot position from the animation
        Vector3 footPosition = animator.GetIKPosition(foot);
        
        // Raycast forward from foot position to find wall
        Vector3 rayOrigin = footPosition;
        Vector3 rayDirection = transform.forward;
        RaycastHit hit;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, climbingFootReachDistance, climbableLayer, QueryTriggerInteraction.Ignore))
        {
            // Set the foot position on the wall with offset
            Vector3 targetPosition = hit.point - hit.normal * climbingFootOffset;
            animator.SetIKPosition(foot, targetPosition);
            
            // Align foot rotation to wall normal (sole facing wall)
            Vector3 footUp = Vector3.up;
            Vector3 footForward = -hit.normal; // Foot points into wall
            Quaternion targetRotation = Quaternion.LookRotation(footForward, footUp);
            animator.SetIKRotation(foot, targetRotation);

            // Debug visualization
            #if UNITY_EDITOR
            Debug.DrawLine(rayOrigin, hit.point, Color.magenta);
            Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.cyan);
            Debug.DrawLine(footPosition, targetPosition, Color.yellow);
            #endif

            // Return the adjustment (for potential future use)
            return Vector3.Distance(footPosition, targetPosition);
        }
        else
        {
            #if UNITY_EDITOR
            Debug.DrawLine(rayOrigin, rayOrigin + rayDirection * climbingFootReachDistance, Color.red);
            #endif
        }

        return 0f;
    }

    private void AdjustPelvisHeight(float leftFootHeight, float rightFootHeight)
    {
        // Use the LOWEST foot to prevent overextension (keeps character grounded)
        float lowestFootOffset = Mathf.Min(leftFootHeight, rightFootHeight);
        
        // Clamp the adjustment to prevent extreme movements
        lowestFootOffset = Mathf.Clamp(lowestFootOffset, -maxPelvisAdjustment, maxPelvisAdjustment);
        
        // Only adjust if there's a meaningful difference
        if (Mathf.Abs(lowestFootOffset) > 0.01f)
        {
            Vector3 bodyPosition = animator.bodyPosition;
            float targetY = bodyPosition.y + lowestFootOffset + pelvisOffset;
            
            // Smooth the pelvis movement
            bodyPosition.y = Mathf.Lerp(lastPelvisPositionY, targetY, Time.deltaTime * pelvisUpDownSpeed);
            lastPelvisPositionY = bodyPosition.y;
            
            animator.bodyPosition = bodyPosition;
        }
    }

    /// <summary>
    /// Enable or disable foot IK at runtime
    /// </summary>
    public void SetFootIKEnabled(bool enabled)
    {
        enableFootIK = enabled;
    }

    /// <summary>
    /// Set the IK weight (0-1)
    /// </summary>
    public void SetIKWeight(float weight)
    {
        ikWeight = Mathf.Clamp01(weight);
    }
}