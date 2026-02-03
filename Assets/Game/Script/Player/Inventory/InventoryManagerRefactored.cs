using UnityEngine;
using System.Collections.Generic;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player.Inventory.Storage;
using Game.Player.Inventory.Services;
using Game.Player.Inventory.Effects;
using Game.Player.Stat;

namespace Game.Player.Inventory
{
    /// <summary>
    /// Refactored inventory manager following SOLID principles
    /// Coordinates between storage, service, and effect systems
    /// Acts as a facade for external systems
    /// </summary>
    public class InventoryManagerRefactored : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int initialInventorySize = 20;
        [SerializeField] private PlayerStats playerStats;

        #region Private Fields

        private IInventoryStorage _storage;
        private IInventoryService _service;
        private IConsumableEffectSystem _effectSystem;
        private IEventBus _eventBus;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            InitializeSystems();
            RegisterServices();
        }

        private void OnDestroy()
        {
            // Cleanup if needed
            //Debug.Log("[InventoryManagerRefactored] Destroyed");
        }

        #endregion

        #region Initialization

        private void ValidateReferences()
        {
            if (playerStats == null)
            {
                playerStats = ServiceContainer.Instance.TryGet<PlayerStats>();
                if (playerStats == null)
                {
                    Debug.LogError("[InventoryManagerRefactored] PlayerStats not found! Consumables will not work.");
                }
            }
        }

        private void InitializeSystems()
        {
            // Get EventBus from container
            _eventBus = ServiceContainer.Instance.Get<IEventBus>();
            if (_eventBus == null)
            {
                Debug.LogError("[InventoryManagerRefactored] EventBus not found in ServiceContainer!");
                return;
            }

            // Create storage layer
            _storage = new InventoryStorage(initialInventorySize, 100);
            //Debug.Log($"[InventoryManagerRefactored] Storage initialized with {initialInventorySize} slots");

            // Create service layer
            _service = new InventoryService(_storage, _eventBus);
            //Debug.Log("[InventoryManagerRefactored] Service layer initialized");

            // Create effect system
            _effectSystem = new ConsumableEffectSystem(playerStats);
            //Debug.Log("[InventoryManagerRefactored] Effect system initialized");

            //Debug.Log("[InventoryManagerRefactored] All systems initialized");
        }

        private void RegisterServices()
        {
            var container = ServiceContainer.Instance;

            // Register the component itself (for facade methods like ConsumeItem)
            container.Register(this);
            //Debug.Log("[InventoryManagerRefactored] Self registered");

            container.Register<IInventoryStorage>(_storage);
            //Debug.Log("[InventoryManagerRefactored] IInventoryStorage registered");

            container.Register<IInventoryService>(_service);
            //Debug.Log("[InventoryManagerRefactored] IInventoryService registered");

            container.Register<IConsumableEffectSystem>(_effectSystem);
            //Debug.Log("[InventoryManagerRefactored] IConsumableEffectSystem registered");

            //Debug.Log("[InventoryManagerRefactored] All services registered");
        }

        #endregion

        #region Public API (Facade Methods)

        // These methods delegate to the appropriate service

        /// <summary>
        /// Add an item to the inventory
        /// </summary>
        public bool AddItem(InventoryItem item, int quantity = 1)
        {
            return _service.AddItem(item, quantity);
        }

        /// <summary>
        /// Remove an item from the inventory
        /// </summary>
        public bool RemoveItem(InventoryItem item, int quantity = 1)
        {
            return _service.RemoveItem(item, quantity);
        }

        /// <summary>
        /// Consume an item and apply its effects
        /// </summary>
        public bool ConsumeItem(InventoryItem item)
        {
            if (item == null || !item.isConsumable) return false;

            // Check if we have the item
            if (!_service.HasItem(item, 1))
            {
                Debug.LogWarning($"[InventoryManagerRefactored] Cannot consume {item.itemName} - not in inventory");
                return false;
            }

            // Apply effects
            foreach (var effect in item.consumableEffects)
            {
                _effectSystem.ApplyEffect(effect, playerStats);
            }

            // Remove the consumed item
            _service.RemoveItem(item, 1);

            // Publish consumption event
            _eventBus.Publish(new Game.Player.Inventory.Events.ItemConsumedEvent(item));

            //Debug.Log($"[InventoryManagerRefactored] Consumed {item.itemName}");
            return true;
        }

        /// <summary>
        /// Check if inventory contains item
        /// </summary>
        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            return _service.HasItem(item, quantity);
        }

        /// <summary>
        /// Get total quantity of an item
        /// </summary>
        public int GetItemQuantity(InventoryItem item)
        {
            return _storage.GetItemQuantity(item);
        }

        /// <summary>
        /// Get all inventory slots (read-only)
        /// </summary>
        public IReadOnlyList<InventorySlot> GetAllSlots()
        {
            return _storage.GetAllSlots();
        }

        /// <summary>
        /// Expand inventory by additional slots
        /// </summary>
        public bool ExpandInventory(int additionalSlots)
        {
            return _storage.ExpandInventory(additionalSlots);
        }

        /// <summary>
        /// Get total weight of all items
        /// </summary>
        public float GetTotalWeight()
        {
            return _service.GetTotalWeight();
        }

        #endregion

        #region Direct Access (for migration period)

        /// <summary>
        /// Provides direct access to storage during migration
        /// Remove this after full migration
        /// </summary>
        public IInventoryStorage Storage => _storage;

        /// <summary>
        /// Provides direct access to service during migration
        /// Remove this after full migration
        /// </summary>
        public IInventoryService Service => _service;

        /// <summary>
        /// Provides direct access to effect system during migration
        /// Remove this after full migration
        /// </summary>
        public IConsumableEffectSystem EffectSystem => _effectSystem;

        #endregion
    }
}
