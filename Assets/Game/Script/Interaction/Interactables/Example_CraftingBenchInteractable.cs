using UnityEngine;
using Game.Core.DI;

namespace Game.Interaction
{
    /// <summary>
    /// EXAMPLE: New hold interactable created in ~30 lines!
    /// Demonstrates how easy it is to create new hold-to-interact objects with the base class.
    /// All you need: Override InteractionPrompt, CanInteract, and OnHoldComplete()
    /// </summary>
    public class CraftingBenchInteractable : HoldInteractableBase
    {
        [Header("Crafting Bench Settings")]
        [SerializeField] private string benchName = "Workbench";

        #region IInteractable Implementation

        public override string InteractionPrompt => $"Use {benchName}";

        public override bool CanInteract => !isCurrentlyHolding; // Always available

        #endregion

        #region Hold Interaction Override

        protected override void OnHoldComplete()
        {
            // Open crafting UI
            var uiService = ServiceContainer.Instance.TryGet<Game.UI.UIServiceProvider>();
            if (uiService != null)
            {
                // uiService.OpenCraftingPanel();
                Debug.Log($"[CraftingBench] Opening crafting UI for {benchName}");
            }
        }

        #endregion
    }
}
