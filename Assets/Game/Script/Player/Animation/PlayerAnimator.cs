using UnityEngine;

public class PlayerAnimator
{
    private readonly Animator animator;
    private readonly Transform root;
    private FootIKControllerRefactored footIKController;

    public PlayerAnimator(Animator animator, Transform root)
    {
        this.animator = animator;
        this.root = root;
        
        // Try to get FootIKControllerRefactored component
        if (root != null)
        {
            footIKController = root.GetComponent<FootIKControllerRefactored>();
        }
    }

    public void UpdateMovement(Vector3 velocity, float maxSpeed)
    {
        Vector3 local = root.InverseTransformDirection(velocity);
        float normX = local.x / maxSpeed;
        float normZ = local.z / maxSpeed;

        animator.SetFloat("Horizontal", normX, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", normZ, 0.1f, Time.deltaTime);
    }

    public void SetClimbing(bool value)
    {
        animator.SetBool("isClimbing", value);
        // Disable foot IK while climbing
        if (footIKController != null)
        {
            footIKController.SetFootIKEnabled(!value);
        }
    }

    public void SetWalking(bool value) => animator.SetBool("isWalking", value);
    public void SetFalling(bool value) => animator.SetBool("isFalling", value);
    public void SetGrounded(bool value) => animator.SetBool("isGround", value);

    /// <summary>
    /// Enable or disable foot IK
    /// </summary>
    public void EnableFootIK(bool enable)
    {
        if (footIKController != null)
        {
            footIKController.SetFootIKEnabled(enable);
        }
    }
}
