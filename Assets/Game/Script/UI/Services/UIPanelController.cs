using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;

namespace Game.UI
{
    /// <summary>
    /// Controls opening and closing of UI panels
    /// Coordinates cursor and input state
    /// Single Responsibility: Panel coordination
    /// </summary>
    public class UIPanelController
    {
        private readonly Dictionary<string, IUIPanel> _panels;
        private readonly ICursorManager _cursorManager;
        private readonly IInputBlocker _inputBlocker;
        private readonly HashSet<IUIPanel> _activePanels;
        private readonly IEventBus _eventBus;
        
        public UIPanelController(
            ICursorManager cursorManager,
            IInputBlocker inputBlocker,
            IEventBus eventBus)
        {
            _panels = new Dictionary<string, IUIPanel>();
            _cursorManager = cursorManager;
            _inputBlocker = inputBlocker;
            _activePanels = new HashSet<IUIPanel>();
            _eventBus = eventBus;
        }
        
        /// <summary>
        /// Registers a panel with the controller
        /// </summary>
        public void RegisterPanel(IUIPanel panel)
        {
            if (panel == null)
            {
                Debug.LogWarning("[UIPanelController] Attempted to register null panel");
                return;
            }
            
            if (!_panels.ContainsKey(panel.PanelName))
            {
                _panels[panel.PanelName] = panel;
                Debug.Log($"[UIPanelController] Registered panel: {panel.PanelName}");
            }
        }
        
        /// <summary>
        /// Opens a panel by name
        /// </summary>
        public void OpenPanel(string panelName)
        {
            if (!_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                Debug.LogWarning($"[UIPanelController] Panel '{panelName}' not found");
                return;
            }
            
            panel.Show();
            _activePanels.Add(panel);
            UpdateInputAndCursorState();
            
            //Debug.Log($"[UIPanelController] Opened panel: {panelName}");
            // Publish event via EventBus (SOLID: Dependency Inversion)
            _eventBus?.Publish(new PanelOpenedEvent(panelName));
        }
        
        /// <summary>
        /// Closes a panel by name
        /// </summary>
        public void ClosePanel(string panelName)
        {
            if (!_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                Debug.LogWarning($"[UIPanelController] Panel '{panelName}' not found");
                return;
            }
            
            panel.Hide();
            _activePanels.Remove(panel);
            UpdateInputAndCursorState();
            
            // Publish event via EventBus (SOLID: Dependency Inversion)
            _eventBus?.Publish(new PanelClosedEvent(panelName));
        }
        
        /// <summary>
        /// Toggles a panel by name
        /// </summary>
        public void TogglePanel(string panelName)
        {
            if (!_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                Debug.LogWarning($"[UIPanelController] Panel '{panelName}' not found");
                return;
            }
            
            if (panel.IsActive)
                ClosePanel(panelName);
            else
                OpenPanel(panelName);
        }
        
        /// <summary>
        /// Closes all open panels
        /// </summary>
        public void CloseAllPanels()
        {
            var panelsToClose = _activePanels.ToList();
            foreach (var panel in panelsToClose)
            {
                panel.Hide();
            }
            _activePanels.Clear();
            UpdateInputAndCursorState();
        }
        
        /// <summary>
        /// Gets a panel by name
        /// </summary>
        public IUIPanel GetPanel(string panelName)
        {
            _panels.TryGetValue(panelName, out IUIPanel panel);
            return panel;
        }
        
        /// <summary>
        /// Gets a panel by type
        /// </summary>
        public T GetPanel<T>() where T : class, IUIPanel
        {
            return _panels.Values.OfType<T>().FirstOrDefault();
        }
        
        /// <summary>
        /// Checks if any panel is currently open
        /// </summary>
        public bool IsAnyPanelOpen()
        {
            return _activePanels.Count > 0;
        }
        
        /// <summary>
        /// Updates input and cursor based on active panels
        /// </summary>
        private void UpdateInputAndCursorState()
        {
            bool shouldBlockInput = _activePanels.Any(p => p.BlocksInput);
            bool shouldUnlockCursor = _activePanels.Any(p => p.UnlocksCursor);
            
            // Update input
            if (shouldBlockInput)
                _inputBlocker.BlockInput();
            else
                _inputBlocker.UnblockInput();
            
            // Update cursor
            if (shouldUnlockCursor)
                _cursorManager.UnlockCursor();
            else
                _cursorManager.LockCursor();
        }
    }
}
