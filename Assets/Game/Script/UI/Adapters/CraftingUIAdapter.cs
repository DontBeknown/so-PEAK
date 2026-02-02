using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Adapter to make CraftingUI implement IUIPanel
    /// </summary>
    [RequireComponent(typeof(CraftingUI))]
    public class CraftingUIAdapter : MonoBehaviour, IUIPanel
    {
        private CraftingUI _craftingUI;
        
        public string PanelName => "Crafting";
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        public bool IsActive => _craftingUI != null && _craftingUI.IsActive;
        
        private void Awake()
        {
            _craftingUI = GetComponent<CraftingUI>();
        }
        
        public void Show()
        {
            _craftingUI?.ShowCraftingPanel();
        }
        
        public void Hide()
        {
            _craftingUI?.HideCraftingPanel();
        }
        
        public void Toggle()
        {
            if (IsActive)
                Hide();
            else
                Show();
        }
    }
}
