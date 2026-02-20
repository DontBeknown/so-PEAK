using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Running/sprinting state — higher speed with increased stamina drain.
/// Speed ramps from walk → run via configurable acceleration.
/// When stamina is depleted the speed decelerates back toward walk speed
/// (the controller will transition back to WalkingState when sprint is released).
/// </summary>
public class RunningState : IPlayerState
{
    private IStateTransitioner _stateTransitioner;
    private float _currentSpeed;

    // ── Constructors ───────────────────────────────────────────────────

    public RunningState() { }

    public RunningState(IStateTransitioner stateTransitioner)
    {
        _stateTransitioner = stateTransitioner;
    }

    // ── IPlayerState ───────────────────────────────────────────────────

    public void Enter(PlayerModelRefactored model)
    {
        var animService = model.GetAnimationService();
        animService.SetRunning(true);
        animService.SetGrounded(true);

        // Begin at current walk speed — acceleration ramps up each tick
        _currentSpeed = model.WalkSpeed;
        //Debug.Log($"[RunningState] ENTERED — walkSpeed:{model.WalkSpeed} runSpeed:{model.RunSpeed}");

        // Mark sprint for stat tracking & disable passive stamina regen
        model.Stats?.OnSprint(true);
        model.Stats?.SetWalking(false);
        model.Stats?.SetRunning(true);
    }

    public void Exit(PlayerModelRefactored model)
    {
        var animService = model.GetAnimationService();
        animService.SetRunning(false);
        animService.SetSpeedMultiplier(0f);

        model.Stats?.OnSprint(false);
        model.Stats?.SetRunning(false);
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input) { }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        var cameraProvider = model.GetCameraProvider();
        var animService    = model.GetAnimationService();
        var config         = model.GetConfig();

        // ── Direction ──────────────────────────────────────────────────
        Vector3 moveDir = cameraProvider.GetWorldDirection(input);

        // ── Target speed (decelerate when out of stamina) ──────────────
        bool hasStamina = model.Stats == null || model.Stats.Stamina > 0.01f;
        float targetSpeed = hasStamina ? model.RunSpeed : model.WalkSpeed;

        float acceleration = config != null ? config.runAcceleration : 8f;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        // ── Horizontal movement ────────────────────────────────────────
        Vector3 horizontal = moveDir * _currentSpeed;

        if (horizontal.sqrMagnitude > 0.01f)
        {
            float toblerMultiplier = CalculateSlopeEffects(model, horizontal);

            // Fatigue penalty (additive with slope penalty)
            float fatigueSpeedPenalty = 1f;
            if (model.Stats?.FatigueStat != null && config != null)
            {
                fatigueSpeedPenalty = model.Stats.FatigueStat
                    .GetSpeedPenalty(config.fatigueSpeedPenaltyThreshold);
            }

            float toblerReduction  = 1f - toblerMultiplier;
            float fatigueReduction = 1f - fatigueSpeedPenalty;
            float combinedMultiplier = 1f - Mathf.Clamp01(toblerReduction + fatigueReduction);

            horizontal *= combinedMultiplier;
        }

        Vector3 motion = new Vector3(horizontal.x, model.Velocity.y, horizontal.z);
        model.Move(motion);

        // ── Rotation ───────────────────────────────────────────────────
        if (moveDir.sqrMagnitude > 0.01f)
        {
            model.Transform.forward = Vector3.Slerp(
                model.Transform.forward, moveDir,
                Time.fixedDeltaTime * model.RotationSmoothness);
        }

        // ── Animation blend ────────────────────────────────────────────
        float range = model.RunSpeed - model.WalkSpeed;
        float blend = range > 0.01f
            ? Mathf.Clamp01((_currentSpeed - model.WalkSpeed) / range)
            : 0f;
        animService.SetSpeedMultiplier(blend);
        // Pass WalkSpeed as the reference so values exceed 1.0 when running.
        // Walk (3 m/s) → magnitude 1.0, Run (4.5 m/s) → magnitude ~1.5
        // Place run clips at 1.5 in the 2D Freeform Directional blend tree.
        animService.UpdateMovement(horizontal, model.WalkSpeed);
        //Debug.Log($"[RunningState] _currentSpeed:{_currentSpeed:F2}  targetSpeed:{(model.Stats == null || model.Stats.Stamina > 0.01f ? model.RunSpeed : model.WalkSpeed):F2}  horizontal:{horizontal.magnitude:F2}");

        // ── Gravity ────────────────────────────────────────────────────
        model.ApplyGravity(-9.81f);
    }

    // ── Jump ───────────────────────────────────────────────────────────

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

        // Preserve sprint momentum through the jump
        Vector3 moveDir = model.GetCameraProvider().GetWorldDirection(input);
        model.JumpWithMomentum(moveDir.normalized, _currentSpeed);
    }

    public void OnClimb(PlayerModelRefactored model)
    {
        if (model.TryClimb(out RaycastHit _))
        {
            _stateTransitioner?.TransitionTo(new ClimbingState(_stateTransitioner));
        }
    }

    // ── Slope effects (Tobler + sprint drain) ──────────────────────────

    /// <summary>
    /// Re-uses WalkingState's Tobler slope formula but applies the higher
    /// sprint stamina drain rate.  Uphill slopes add a 0–50 % surcharge.
    /// </summary>
    private float CalculateSlopeEffects(PlayerModelRefactored model, Vector3 horizontalVelocity)
    {
        var config = model.Stats?.Config;
        if (config == null) return 1f;

        // Ground normal via raycast
        Vector3 groundNormal = Vector3.up;
        Vector3 origin = model.Transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, config.groundLayer))
        {
            groundNormal = hit.normal;
        }

        float slopeAngle   = Vector3.Angle(Vector3.up, groundNormal);
        float movementDot   = Vector3.Dot(horizontalVelocity.normalized, groundNormal);
        bool  isMovingUphill = movementDot < 0f;

        float slopeGradient = Mathf.Tan(slopeAngle * Mathf.Deg2Rad);
        if (!isMovingUphill) slopeGradient = -slopeGradient;

        // Tobler's hiking function
        float toblerRaw      = Mathf.Exp(-3.5f * Mathf.Abs(slopeGradient + 0.05f));
        float flatValue      = Mathf.Exp(-3.5f * 0.05f);            // ≈ 0.839
        float normalizedValue = toblerRaw / flatValue;
        float toblerMultiplier = Mathf.Lerp(
            config.minSlopeSpeedMultiplier,
            config.maxSlopeSpeedMultiplier,
            Mathf.Clamp01(normalizedValue));

        // ── Stamina drain (sprint rate, slope-adjusted) ────────────────
        if (model.Stats != null && horizontalVelocity.magnitude > config.movementThreshold)
        {
            float drainPerSecond = config.sprintStaminaDrainPerSecond;

            // Uphill surcharge: up to +50 % at 45°
            if (isMovingUphill && slopeAngle > 5f)
            {
                drainPerSecond *= 1f + (slopeAngle / 45f) * 0.5f;
            }

            // Fatigue multiplier compounds on top
            if (model.Stats.FatigueStat != null)
            {
                drainPerSecond *= model.Stats.FatigueStat.GetStaminaDrainMultiplier();
            }

            model.Stats.StaminaStat.ApplyTerrainDrain(drainPerSecond);

            // Update fatigue accumulation
            if (model.Stats.FatigueStat != null)
            {
                float movementSpeed = horizontalVelocity.magnitude * toblerMultiplier;
                model.Stats.FatigueStat.UpdateMovement(slopeGradient, movementSpeed, true);
            }
        }

        return toblerMultiplier;
    }
}
