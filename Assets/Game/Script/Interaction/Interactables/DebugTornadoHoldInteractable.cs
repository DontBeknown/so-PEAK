using Game.Environment.Tornado;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Debug-only hold interactable that starts a tornado warning phase.
    /// </summary>
    public class DebugTornadoHoldInteractable : HoldInteractableBase
    {
        [Header("Debug Tornado Trigger")]
        [SerializeField] private TornadoPhaseController tornadoPhaseController;
        [SerializeField] private string interactionPrompt = "Trigger Tornado";
        [SerializeField] private string interactionVerb = "Hold to";
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

                TornadoPhaseController controller = ResolveController();
                if (controller == null)
                {
                    return false;
                }

                return controller.CurrentPhase == TornadoPhase.Ended;
            }
        }

        protected override void OnHoldComplete()
        {
            TornadoPhaseController controller = ResolveController();
            if (controller == null)
            {
                Debug.LogWarning("[DebugTornadoHoldInteractable] No TornadoPhaseController assigned or found.");
                return;
            }

            if (controller.CurrentPhase != TornadoPhase.Ended)
            {
                return;
            }

            controller.StartWarningPhase();
            _hasTriggered = true;
        }

        private TornadoPhaseController ResolveController()
        {
            if (tornadoPhaseController != null)
            {
                return tornadoPhaseController;
            }

            tornadoPhaseController = GetComponentInParent<TornadoPhaseController>();
            return tornadoPhaseController;
        }
    }
}
