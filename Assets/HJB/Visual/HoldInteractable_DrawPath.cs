using UnityEngine;
using Game.Interaction;

/// <summary>
/// Hold interactable that, when completed, calls DrawCachedPath() on a target HJBClickPathController.
/// </summary>
public class HoldInteractable_DrawPath : HoldInteractableBase
{
    [Header("Draw Path Reference")]
    [SerializeField] private HJBClickPathController hjbClickPathController;

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
            hjbClickPathController.DrawCachedPath();
        }
        else
        {
            Debug.LogWarning("HoldInteractable_DrawPath: No HJBClickPathController found in scene.");
        }
    }
}