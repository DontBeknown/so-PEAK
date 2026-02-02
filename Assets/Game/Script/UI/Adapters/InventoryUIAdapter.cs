using UnityEngine;
using Game.UI;

namespace Game.UI.Adapters
{
    /// <summary>
    /// Adapter for InventoryUI (legacy) to work with UIServiceProvider
    /// Wraps the legacy InventoryUI to implement IUIPanel interface
    /// </summary>
    public class InventoryUIAdapter : MonoBehaviour, IUIPanel
    {
        [SerializeField] private InventoryUI inventoryUI;
        
        public string PanelName => "Inventory";
        public bool IsActive => inventoryUI != null && inventoryUI.gameObject.activeInHierarchy;
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        
        private void Awake()
        {
            if (inventoryUI == null)
            {
                inventoryUI = GetComponent<InventoryUI>();
            }
        }
        
        public void Show()
        {
            if (inventoryUI != null)
            {
                inventoryUI.ShowInventoryPanel();
            }
        }
        
        public void Hide()
        {
            if (inventoryUI != null)
            {
                inventoryUI.HideInventoryPanel();
            }
        }
        
        public void Toggle()
        {
            if (inventoryUI != null)
            {
                inventoryUI.ToggleInventory();
            }
        }
    }
}
