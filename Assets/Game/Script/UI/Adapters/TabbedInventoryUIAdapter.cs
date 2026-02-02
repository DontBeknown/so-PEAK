using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Adapter to make TabbedInventoryUI implement IUIPanel
    /// Uses Adapter Pattern to integrate legacy code
    /// </summary>
    [RequireComponent(typeof(TabbedInventoryUI))]
    public class TabbedInventoryUIAdapter : MonoBehaviour, IUIPanel
    {
        private TabbedInventoryUI _inventoryUI;
        
        public string PanelName => "Inventory";
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        public bool IsActive => _inventoryUI != null && _inventoryUI.IsActive;
        
        private void Awake()
        {
            _inventoryUI = GetComponent<TabbedInventoryUI>();
        }
        
        public void Show()
        {
            _inventoryUI?.OpenUI();
        }
        
        public void Hide()
        {
            _inventoryUI?.CloseUI();
        }
        
        public void Toggle()
        {
            _inventoryUI?.ToggleUI();
        }
    }
}
