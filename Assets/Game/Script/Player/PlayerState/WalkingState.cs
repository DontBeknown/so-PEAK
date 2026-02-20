using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Walking state - handles ground-based movement.
/// Refactored to use dependency injection and remove Camera.main references.
/// </summary>
public class WalkingState : IPlayerState
{
    private IStateTransitioner _stateTransitioner;

    public WalkingState()
    {
    }

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public WalkingState(IStateTransitioner stateTransitioner)
    {
        _stateTransitioner = stateTransitioner;
    }

    public void Enter(PlayerModelRefactored model)
    {
        var animService = model.GetAnimationService();
        animService.SetWalking(true);
        animService.SetGrounded(true);
        
        // Enable stamina regeneration in walking state
        model.Stats?.SetWalking(true);
        
        // Clear downward velocity when entering walking state (landing from a fall/jump)
        if (model.Velocity.y < 0f)
        {
            Vector3 vel = model.Velocity;
            vel.y = 0f;
            model.Velocity = vel;
        }
    }

    public void Exit(PlayerModelRefactored model)
    {
        model.GetAnimationService().SetWalking(false);
        
        // Disable stamina regeneration when leaving walking state
        model.Stats?.SetWalking(false);
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        var context = model.GetMovementContext();
        var cameraProvider = model.GetCameraProvider();
        var animService = model.GetAnimationService();
        var physicsService = model.GetPhysicsService();

        // Get movement direction from camera
        Vector3 moveDir = cameraProvider.GetWorldDirection(input);

        // Apply horizontal movement with Tobler's hiking function for slope speed
        Vector3 horizontal = moveDir * model.WalkSpeed;
        
        // Calculate slope effects (stamina drain and fatigue accumulation)
        if (horizontal.sqrMagnitude > 0.01f)
        {
            float toblerSpeedMultiplier = CalculateSlopeEffects(model, horizontal);
            
            // Combine slope and fatigue penalties additively
            float fatigueSpeedPenalty = 1f;
            if (model.Stats?.FatigueStat != null && model.Stats.Config != null)
            {
                fatigueSpeedPenalty = model.Stats.FatigueStat.GetSpeedPenalty(model.Stats.Config.fatigueSpeedPenaltyThreshold);
            }
            
            // Sum the speed reductions, then apply to base speed
            float toblerReduction = 1f - toblerSpeedMultiplier;
            float fatigueReduction = 1f - fatigueSpeedPenalty;
            float totalReduction = Mathf.Clamp01(toblerReduction + fatigueReduction);
            float combinedMultiplier = 1f - totalReduction;
            
            // Stamina exhaustion: severely reduce speed when stamina hits 0
            bool isExhausted = model.Stats != null && model.Stats.Stamina <= 0f;
            if (isExhausted && model.Stats.Config != null)
            {
                combinedMultiplier *= model.Stats.Config.staminaExhaustedSpeedMultiplier;
            }
            
            horizontal *= combinedMultiplier;
        }
        
        Vector3 motion = new Vector3(horizontal.x, model.Velocity.y, horizontal.z);
        model.Move(motion);

        // Rotate to face movement direction
        if (moveDir.sqrMagnitude > 0.01f)
        {
            model.Transform.forward = Vector3.Slerp(
                model.Transform.forward, moveDir,
                Time.fixedDeltaTime * model.RotationSmoothness);
        }

        // Update animation
        animService.UpdateMovement(horizontal, model.WalkSpeed);
        
        // Apply minimal gravity to keep grounded on slopes
        // Gravity is handled differently in ApplyGravity when grounded
        model.ApplyGravity(-9.81f);
    }
    
    /// <summary>
    /// Calculate slope effects using Tobler's hiking function for speed
    /// Formula: speed = base_speed * exp(-3.5 * abs(slope + 0.05))
    /// Returns speed multiplier to apply
    /// </summary>
    private float CalculateSlopeEffects(PlayerModelRefactored model, Vector3 horizontalVelocity)
    {
        var config = model.Stats?.Config;
        if (config == null) return 1f;
        
        // Get ground normal via raycast
        Vector3 groundNormal = Vector3.up;
        Vector3 origin = model.Transform.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, config.groundLayer))
        {
            groundNormal = hit.normal;
        }
        
        // Calculate slope angle
        float slopeAngle = Vector3.Angle(Vector3.up, groundNormal);
        
        // Check if moving uphill or downhill
        float movementDot = Vector3.Dot(horizontalVelocity.normalized, groundNormal);
        bool isMovingUphill = movementDot < 0f;
        
        // Convert angle to slope gradient for Tobler's function
        float slopeAngleRad = slopeAngle * Mathf.Deg2Rad;
        float slopeGradient = Mathf.Tan(slopeAngleRad);
        
        // Apply sign based on direction (positive for uphill, negative for downhill)
        if (!isMovingUphill)
        {
            slopeGradient = -slopeGradient;
        }
        
        // Tobler's hiking function: speed = base_speed * exp(-3.5 * abs(slope + 0.05))
        float toblerRaw = Mathf.Exp(-3.5f * Mathf.Abs(slopeGradient + 0.05f));
        
        // Calculate flat ground reference (gradient = 0)
        float flatGroundValue = Mathf.Exp(-3.5f * 0.05f); // ≈ 0.839
        
        // Remap so flat ground = max speed, steep slopes = min speed
        float normalizedValue = toblerRaw / flatGroundValue; // 0 to 1+ range
        float toblerSpeedMultiplier = Mathf.Lerp(config.minSlopeSpeedMultiplier, config.maxSlopeSpeedMultiplier, Mathf.Clamp01(normalizedValue));
        
        // Apply constant stamina drain when moving and update fatigue with movement parameters
        if (model.Stats != null && horizontalVelocity.magnitude > config.movementThreshold)
        {
            float drainPerSecond = config.baseMovementStaminaDrain; // No multiplier, constant drain
            
            // Apply fatigue multiplier to stamina drain
            if (model.Stats.FatigueStat != null)
            {
                float fatigueDrainMultiplier = model.Stats.FatigueStat.GetStaminaDrainMultiplier();
                drainPerSecond *= fatigueDrainMultiplier;
            }
            
            model.Stats.StaminaStat.ApplyTerrainDrain(drainPerSecond);
            
            // Update fatigue with movement parameters
            if (model.Stats.FatigueStat != null)
            {
                float movementSpeed = horizontalVelocity.magnitude * toblerSpeedMultiplier;
                model.Stats.FatigueStat.UpdateMovement(slopeGradient, movementSpeed, true);
            }
        }
        
        return toblerSpeedMultiplier;
    }

    public void OnJump(PlayerModelRefactored model, Vector2 input)
    {
        if (model.Stats != null)
        {
            model.Stats.OnJump(); 
            if (model.Stats.Stamina < 0.01f) return; 
        }

        if (input.sqrMagnitude <= 0.01f)
        {
            model.Jump();
            return;
        }

        Vector3 moveDir = model.GetCameraProvider().GetWorldDirection(input);
        model.JumpWithMomentum(moveDir.normalized);
    }

    public void OnClimb(PlayerModelRefactored model)
    {
        if (model.TryClimb(out RaycastHit _))
        {
            if (_stateTransitioner != null)
            {
                _stateTransitioner.TransitionTo(new ClimbingState(_stateTransitioner));
            }
            else
            {
                Debug.LogWarning("WalkingState: No state transitioner available for climb transition!");
            }
        }
    }
}
