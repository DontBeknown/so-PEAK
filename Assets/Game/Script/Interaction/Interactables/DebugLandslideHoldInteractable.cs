using Game.Environment.Landslide;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Debug-only hold interactable that triggers a landslide spawner.
    /// </summary>
    public class DebugLandslideHoldInteractable : HoldInteractableBase
    {
        [Header("Debug Landslide Trigger")]
        [SerializeField] private LandslideRockSpawner landslideRockSpawner;
        [SerializeField] private string interactionPrompt = "Trigger Landslide";
        [SerializeField] private string interactionVerb = "Hold to";
        [SerializeField] private bool triggerAtThisObjectPosition = true;
        [SerializeField] private bool oneTimeUse;

        private bool _hasTriggered;

        public override string InteractionPrompt => interactionPrompt;
        public override string InteractionVerb => interactionVerb;
        public override bool CanInteract
        {
            get
            {
                if (isCurrentlyHolding)
                {
                    return false;
                }

                if (oneTimeUse && _hasTriggered)
                {
                    return false;
                }

                return ResolveSpawner() != null;
            }
        }

        protected override void OnHoldComplete()
        {
            LandslideRockSpawner spawner = ResolveSpawner();
            if (spawner == null)
            {
                Debug.LogWarning("[DebugLandslideHoldInteractable] No LandslideRockSpawner assigned or found.");
                return;
            }

            if (triggerAtThisObjectPosition)
            {
                spawner.TriggerLandslideAtPosition(transform.position);
            }
            else
            {
                spawner.TriggerLandslide();
            }

            _hasTriggered = true;
        }

        private LandslideRockSpawner ResolveSpawner()
        {
            if (landslideRockSpawner != null)
            {
                return landslideRockSpawner;
            }

            landslideRockSpawner = GetComponentInParent<LandslideRockSpawner>();
            return landslideRockSpawner;
        }
    }
}