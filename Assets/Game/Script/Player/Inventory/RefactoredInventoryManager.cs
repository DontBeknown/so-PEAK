using UnityEngine;
using System.Collections.Generic;

namespace Game.Inventory
{
    /// <summary>
    /// Refactored inventory manager using SOLID principles
    /// Acts as a facade to the inventory service layer
    /// MonoBehaviour wrapper for dependency injection
    /// Can run alongside old InventoryManager during migration
    /// </summary>
    public class RefactoredInventoryManager : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int initialSlots = 10;
        [SerializeField] private int maxSlots = 30;
        
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Services
        private IInventoryStorage _storage;
        private InventoryService _service;
        private InventoryEvents _events;
        
        // Public properties
        public InventoryEvents Events => _events;
        
        private void Awake()
        {
            // Get PlayerStats from ServiceContainer (DI principle)
            if (playerStats == null)
            {
                playerStats = Game.Core.DI.ServiceContainer.Instance.TryGet<PlayerStats>();
                
                // Fallback to GetComponent if not in container yet
                if (playerStats == null)
                    playerStats = GetComponent<PlayerStats>();
            }
            
            // Initialize services
            InitializeServices();
            
            if (enableDebugLogs)
                Debug.Log("[RefactoredInventoryManager] Initialized with SOLID architecture");
        }
        
        private void InitializeServices()
        {
            // Create storage
            _storage = new InMemoryInventoryStorage(initialSlots, maxSlots);
            
            // Create events
            _events = new InventoryEvents();
            
            // Create service with explicit dependency injection
            _service = new InventoryService(_storage, _events, playerStats);
        }
        
        #region Public API - Delegates to service
        
        public bool AddItem(InventoryItem item, int quantity = 1)
        {
            bool result = _service.AddItem(item, quantity);
            if (enableDebugLogs && result)
                Debug.Log($"[RefactoredInventoryManager] Added {quantity}x {item.itemName}");
            return result;
        }
        
        public bool RemoveItem(InventoryItem item, int quantity = 1)
        {
            bool result = _service.RemoveItem(item, quantity);
            if (enableDebugLogs && result)
                Debug.Log($"[RefactoredInventoryManager] Removed {quantity}x {item.itemName}");
            return result;
        }
        
        public bool ConsumeItem(InventoryItem item)
        {
            bool result = _service.ConsumeItem(item);
            if (enableDebugLogs && result)
                Debug.Log($"[RefactoredInventoryManager] Consumed {item.itemName}");
            return result;
        }
        
        public bool HasItem(InventoryItem item, int quantity = 1)
        {
            return _service.HasItem(item, quantity);
        }
        
        public int GetItemQuantity(InventoryItem item)
        {
            return _service.GetItemQuantity(item);
        }
        
        public IReadOnlyList<InventorySlot> GetInventorySlots()
        {
            return _service.GetSlots();
        }
        
        // Legacy support - converts to List for backward compatibility
        public List<InventorySlot> GetInventorySlotsLegacy()
        {
            return new List<InventorySlot>(_service.GetSlots());
        }
        
        #endregion
    }
}
