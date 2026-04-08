using UnityEngine;
using Game.Core.DI;

namespace Game.Interaction
{
    /// <summary>
    /// Hold-based water refill interactable implemented on top of HoldInteractableBase.
    /// </summary>
    public class WaterSourceInteractable : HoldInteractableBase
    {
        [Header("Water Source Settings")]
        [SerializeField] private string customPrompt = "";
        [SerializeField] private float refillDuration = 3f;

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

        private void Awake()
        {
            holdDuration = refillDuration;
        }

        private void OnValidate()
        {
            holdDuration = refillDuration;
        }

        protected override void OnHoldComplete()
        {
            var canteen = GetEquippedCanteen();
            if (canteen != null)
            {
                canteen.Refill();
                ShowCompletionNotification(canteen);
            }
        }

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
            return null;
        }

        private void ShowCompletionNotification(CanteenItem canteen)
        {
            string message = $"Canteen Refilled [{canteen.GetStateDescription()}]";
            _ = message;
        }
    }
}
