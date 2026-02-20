using UnityEngine;
using Game.Player.Interfaces;

namespace Game.Player.Services
{
    /// <summary>
    /// Implementation of IAnimationService wrapping Unity's Animator.
    /// Provides a clean interface for animation control without exposing Animator directly.
    /// </summary>
    public class PlayerAnimationService : IAnimationService
    {
        private readonly Animator _animator;
        private readonly Transform _root;
        private readonly FootIKControllerRefactored _footIKController;

        // Animation parameter hashes (for performance)
        private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
        private static readonly int VerticalHash = Animator.StringToHash("Vertical");
        private static readonly int IsClimbingHash = Animator.StringToHash("isClimbing");
        private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("isRunning");
        private static readonly int SpeedMultiplierHash = Animator.StringToHash("SpeedMultiplier");
        private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGround");

        public PlayerAnimationService(Animator animator, Transform root)
        {
            _animator = animator;
            _root = root;

            // Try to get FootIKControllerRefactored if it exists
            if (root != null)
            {
                _footIKController = root.GetComponent<FootIKControllerRefactored>();
            }
        }

        public void UpdateMovement(Vector3 velocity, float maxSpeed)
        {
            if (_animator == null) return;

            // Convert world velocity to local space
            Vector3 localVelocity = _root.InverseTransformDirection(velocity);
            
            // Normalize by max speed
            float normalizedX = maxSpeed > 0 ? localVelocity.x / maxSpeed : 0f;
            float normalizedZ = maxSpeed > 0 ? localVelocity.z / maxSpeed : 0f;

            // Update animator parameters with smoothing
            _animator.SetFloat(HorizontalHash, normalizedX, 0.1f, Time.deltaTime);
            _animator.SetFloat(VerticalHash, normalizedZ, 0.1f, Time.deltaTime);
        }

        public void SetClimbing(bool isClimbing)
        {
            if (_animator == null) return;

            _animator.SetBool(IsClimbingHash, isClimbing);

            // Disable foot IK while climbing
            if (_footIKController != null)
            {
                _footIKController.SetFootIKEnabled(!isClimbing);
            }
        }

        public void SetWalking(bool isWalking)
        {
            if (_animator == null) return;
            _animator.SetBool(IsWalkingHash, isWalking);
        }

        public void SetRunning(bool isRunning)
        {
            if (_animator == null) return;
            _animator.SetBool(IsRunningHash, isRunning);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            if (_animator == null) return;
            _animator.SetFloat(SpeedMultiplierHash, multiplier);
        }

        public void SetFalling(bool isFalling)
        {
            if (_animator == null) return;
            _animator.SetBool(IsFallingHash, isFalling);
        }

        public void SetGrounded(bool isGrounded)
        {
            if (_animator == null) return;
            _animator.SetBool(IsGroundedHash, isGrounded);
        }

        public void SetFootIKEnabled(bool enabled)
        {
            if (_footIKController != null)
            {
                _footIKController.SetFootIKEnabled(enabled);
            }
        }

        public void TriggerJump()
        {
            // Could add a jump trigger if needed
            // _animator.SetTrigger("Jump");
        }
    }
}
