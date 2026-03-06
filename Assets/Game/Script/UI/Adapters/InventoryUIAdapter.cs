using UnityEngine;
using Game.UI;

namespace Game.UI.Adapters
{
    /// <summary>
    /// Adapter for GridInventoryUI to work with UIServiceProvider.
    /// Wraps GridInventoryUI to implement the IUIPanel interface.
    /// </summary>
    public class InventoryUIAdapter : MonoBehaviour, IUIPanel
    {
        [SerializeField] private GridInventoryUI gridInventoryUI;
        
        public string PanelName => "Inventory";
        public bool IsActive => gridInventoryUI != null && gridInventoryUI.gameObject.activeInHierarchy;
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        
        private void Awake()
        {
            if (gridInventoryUI == null)
            {
                gridInventoryUI = GetComponent<GridInventoryUI>();
            }
        }
        
        public void Show()
        {
            if (gridInventoryUI != null)
            {
                gridInventoryUI.ShowInventoryPanel();
            }
        }
        
        public void Hide()
        {
            if (gridInventoryUI != null)
            {
                gridInventoryUI.HideInventoryPanel();
            }
        }
        
        public void Toggle()
        {
            if (gridInventoryUI != null)
            {
                if (gridInventoryUI.IsOpen)
                    gridInventoryUI.HideInventoryPanel();
                else
                    gridInventoryUI.ShowInventoryPanel();
            }
        }
    }
}
