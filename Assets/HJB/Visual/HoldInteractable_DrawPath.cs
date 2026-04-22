using UnityEngine;
using Game.Interaction;
using Game.UI;

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

    [Header("Map Data")]
    [SerializeField] private HeldMapData mapData;

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

            var uiServiceProvider = UIServiceProvider.Instance;
            if (uiServiceProvider == null)
            {
                Debug.LogWarning("[MapBehavior] UIServiceProvider not found in scene.");
                return;
            }

            var mapPanel = uiServiceProvider.GetPanel<MapViewerPanel>();
            if (mapPanel == null)
            {
                Debug.LogWarning("[MapBehavior] MapViewerPanel not found in the UI scene.");
                return;
            }

            if (!mapPanel.SetMapData(mapData))
                return;

            uiServiceProvider.OpenPanel(mapPanel.PanelName);

            MapPathRevealState.Reveal(displayDuration);

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