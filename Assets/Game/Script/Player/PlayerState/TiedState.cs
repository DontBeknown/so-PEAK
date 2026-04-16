using UnityEngine;
using Game.Player.Interfaces;

/// <summary>
/// Tied state: player can move at reduced speed within a fixed radius around an anchor.
/// </summary>
public class TiedState : IPlayerState
{
    private readonly IStateTransitioner _transitioner;
    private readonly Transform _anchor;
    private readonly float _radius;
    private readonly float _speedMultiplier;

    public TiedState(IStateTransitioner transitioner, Transform anchor, float radius, float speedMultiplier)
    {
        _transitioner = transitioner;
        _anchor = anchor;
        _radius = radius;
        _speedMultiplier = speedMultiplier;
    }

    public void Enter(PlayerModelRefactored model)
    {
        var animService = model.GetAnimationService();
        animService.SetWalking(false);
        animService.SetRunning(false);
        animService.SetFalling(false);
        animService.SetTied(true);
        animService.TriggerTiedStart();
    }

    public void Exit(PlayerModelRefactored model)
    {
        var animService = model.GetAnimationService();
        animService.SetTied(false);
        animService.TriggerTiedStop();
    }

    public void HandleInput(PlayerModelRefactored model, Vector2 input)
    {
    }

    public void FixedUpdate(PlayerModelRefactored model, Vector2 input)
    {
        if (_anchor == null)
        {
            _transitioner?.TransitionTo(new WalkingState(_transitioner));
            return;
        }

        var cameraProvider = model.GetCameraProvider();
        var animService = model.GetAnimationService();

        Vector3 moveDir = cameraProvider.GetWorldDirection(input);
        float tiedSpeed = model.WalkSpeed * _speedMultiplier;
        Vector3 velocity = moveDir * tiedSpeed;
        Vector3 externalVelocity = model.ConsumeExternalVelocity();

        model.ApplyGravity(-9.81f);
        model.Move(new Vector3(velocity.x, model.Velocity.y, velocity.z) + externalVelocity);

        ClampToAnchorRadius(model);

        Vector3 toAnchor = _anchor.position - model.Transform.position;
        toAnchor.y = 0f;

        if (toAnchor.sqrMagnitude > 0.0001f)
        {
            Vector3 faceDir = toAnchor.normalized;
            model.Transform.forward = Vector3.Slerp(
                model.Transform.forward,
                faceDir,
                Time.fixedDeltaTime * model.RotationSmoothness);
        }

        animService.UpdateMovement(velocity, tiedSpeed);
    }

    public void OnJump(PlayerModelRefactored model, Vector2 input)
    {
    }

    public void OnClimb(PlayerModelRefactored model)
    {
    }

    private void ClampToAnchorRadius(PlayerModelRefactored model)
    {
        Vector3 playerPos = model.Transform.position;
        Vector3 anchorPos = _anchor.position;

        Vector3 horizontalDelta = new Vector3(
            playerPos.x - anchorPos.x,
            0f,
            playerPos.z - anchorPos.z);

        if (horizontalDelta.sqrMagnitude <= _radius * _radius)
        {
            return;
        }

        Vector3 clampedHorizontal = horizontalDelta.normalized * _radius;
        Vector3 clampedPos = new Vector3(
            anchorPos.x + clampedHorizontal.x,
            playerPos.y,
            anchorPos.z + clampedHorizontal.z);

        bool wasEnabled = model.Controller.enabled;
        model.Controller.enabled = false;
        model.Transform.position = clampedPos;
        model.Controller.enabled = wasEnabled;
    }
}