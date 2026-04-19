using UnityEngine;
using Game.Interaction;

/// <summary>
/// Hold interactable that toggles the cached path display on a target HJBClickPathController.
/// </summary>
public class HoldInteractable_DrawPath : HoldInteractableBase
{
    [Header("Draw Path Reference")]
    [SerializeField] private HJBClickPathController hjbClickPathController;

    [Header("Fade Timing (seconds)")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.4f;
    [SerializeField, Min(0f)] private float displayDuration = 10f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.8f;

    [Header("Optional: Destroy Interactable After Use")]
    [SerializeField] private bool destroyAfterUse;
    [SerializeField] private ScaleDownDestroyAnimation destroyAnimation;

    public override string InteractionPrompt => "Draw Path to Peak";
    public override bool CanInteract => true;

    protected override void OnHoldComplete()
    {
        if (hjbClickPathController == null)
        {
            hjbClickPathController = FindFirstObjectByType<HJBClickPathController>();
        }
        if (hjbClickPathController != null)
        {
            hjbClickPathController.ToggleCachedPathDisplay(fadeInDuration, displayDuration, fadeOutDuration);

            if (destroyAfterUse)
            {
                if (destroyAnimation == null)
                {
                    destroyAnimation = GetComponent<ScaleDownDestroyAnimation>();
                }

                if (destroyAnimation != null)
                {
                    destroyAnimation.PlayAndDestroy();
                }
            }
        }
        else
        {
            Debug.LogWarning("HoldInteractable_DrawPath: No HJBClickPathController found in scene.");
        }
    }
}