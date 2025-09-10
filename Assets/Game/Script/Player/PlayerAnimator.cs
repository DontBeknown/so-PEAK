using UnityEngine;

public class PlayerAnimator
{
    private readonly Animator animator;
    private readonly Transform root;

    public PlayerAnimator(Animator animator, Transform root)
    {
        this.animator = animator;
        this.root = root;
    }

    public void UpdateMovement(Vector3 velocity, float maxSpeed)
    {
        Vector3 local = root.InverseTransformDirection(velocity);
        float normX = local.x / maxSpeed;
        float normZ = local.z / maxSpeed;

        animator.SetFloat("Horizontal", normX, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", normZ, 0.1f, Time.deltaTime);

    }

    public void SetClimbing(bool value) => animator.SetBool("isClimbing", value);
    public void SetWalking(bool value) => animator.SetBool("isWalking", value);
    public void SetFalling(bool value) => animator.SetBool("isFalling", value);
    public void SetGrounded(bool value) => animator.SetBool("isGround", value);
}
