using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Adapter to make EquipmentUI implement IUIPanel
    /// </summary>
    [RequireComponent(typeof(EquipmentUI))]
    public class EquipmentUIAdapter : MonoBehaviour, IUIPanel
    {
        private EquipmentUI _equipmentUI;
        
        public string PanelName => "Equipment";
        public bool BlocksInput => true;
        public bool UnlocksCursor => true;
        public bool IsActive => _equipmentUI != null && _equipmentUI.IsActive;
        
        private void Awake()
        {
            _equipmentUI = GetComponent<EquipmentUI>();
        }
        
        public void Show()
        {
            _equipmentUI?.ShowEquipmentPanel();
        }
        
        public void Hide()
        {
            _equipmentUI?.HideEquipmentPanel();
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
