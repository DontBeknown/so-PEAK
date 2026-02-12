using UnityEngine;
using Game.Core.DI;

namespace Game.Interaction
{
    /// <summary>
    /// REFACTORED VERSION using HoldInteractableBase.
    /// Much simpler - only contains canteen refill-specific logic!
    /// All hold mechanics handled by base class.
    /// </summary>
    public class WaterSourceInteractable_Refactored : HoldInteractableBase
    {
        [Header("Water Source Settings")]
        [SerializeField] private string customPrompt = "";

        #region IInteractable Implementation

        public override string InteractionPrompt
        {
            get
            {
                var canteen = GetEquippedCanteen();
                
                if (canteen == null)
                {
                    var inventoryService = ServiceContainer.Instance.TryGet<Game.Player.Inventory.IInventoryService>();
                    if (inventoryService != null && inventoryService.HasItem(GetAnyCanteen()))
                    {
                        return "Equip Canteen to Refill";
                    }
                    return "No Canteen";
                }
                
                if (canteen.IsFull())
                {
                    return "Canteen Full";
                }
                
                if (!string.IsNullOrEmpty(customPrompt))
                    return $"Refill Canteen ({customPrompt})";
                
                return "Refill Canteen";
            }
        }

        public override bool CanInteract
        {
            get
            {
                if (isCurrentlyHolding)
                    return false;
                
                var canteen = GetEquippedCanteen();
                return canteen != null && !canteen.IsFull();
            }
        }

        #endregion

        #region Hold Interaction Override (Water-specific logic only!)

        protected override void OnHoldComplete()
        {
            // Refill the canteen
            var canteen = GetEquippedCanteen();
            if (canteen != null)
            {
                canteen.Refill();
                ShowCompletionNotification(canteen);
            }
        }

        #endregion

        #region Water Source-specific Methods

        private CanteenItem GetEquippedCanteen()
        {
            var equipmentManager = ServiceContainer.Instance.TryGet<EquipmentManager>();
            if (equipmentManager == null)
                return null;
            
            var equippedItem = equipmentManager.GetEquippedItem(EquipmentSlotType.HeldItem);
            return equippedItem as CanteenItem;
        }

        private InventoryItem GetAnyCanteen()
        {
            // Placeholder - search inventory for any canteen
            return null;
        }

        private void ShowCompletionNotification(CanteenItem canteen)
        {
            string message = $"Canteen Refilled [{canteen.GetStateDescription()}]";
            //Debug.Log(message);
            // TODO: Connect to notification system
        }

        #endregion

        #region Editor Helpers

        /*private void OnDrawGizmos()
        {
            Gizmos.color = isCurrentlyHolding ? Color.cyan : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            string label = !string.IsNullOrEmpty(customPrompt)
                ? $"Water Source: {customPrompt}\n{holdDuration}s refill"
                : $"Water Source\n{holdDuration}s refill";
            
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
        }*/

        #endregion
    }
}
