# SOLID Principles Refactoring Guide - Part 2: UIManager Refactoring

## Overview

The current `UIManager` class violates the Single Responsibility Principle by managing multiple concerns. This guide shows how to refactor it into a maintainable, testable architecture.

---

## Current Problems

### UIManager Responsibilities (Too Many!)

```csharp
public class UIManager : MonoBehaviour
{
    // 1. Panel Management
    [SerializeField] private TabbedInventoryUI inventoryUI;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private EquipmentUI equipmentUI;
    // ... 5 more panels
    
    // 2. Player References
    [SerializeField] private CinemachinePlayerCamera playerCamera;
    [SerializeField] private PlayerControllerRefactored playerController;
    
    // 3. Cursor Management
    private void SetMenuOpen(bool open)
    {
        playerCamera.SetCursorLock(!open);
    }
    
    // 4. Input Blocking
    playerController.SetInputBlocked(open);
    
    // 5. Auto-Finding Dependencies
    private void FindUIReferences() { /* FindFirstObjectByType calls */ }
}
```

**Issues:**
1. Manages 8+ different UI panels
2. Controls player input
3. Manages cursor state
4. Auto-finds dependencies (DIP violation)
5. Acts as god object for all UI

---

## Refactoring Strategy

### Step 1: Identify Separate Concerns

Break UIManager into focused components:

1. **UI Panel Registry** - Tracks available UI panels
2. **UI Panel Controller** - Opens/closes panels
3. **Cursor Manager** - Manages cursor state
4. **Input Blocker** - Blocks player input when UI open
5. **UI Service Locator** - Provides access to UI components

---

## Refactored Architecture

### New Structure

```
┌─────────────────────────────────────┐
│     UIServiceProvider (New)         │  ← Service Locator Pattern
│  - Registers UI panels               │
│  - Provides access to UI services    │
└─────────────────────────────────────┘
            │
            ├─────────────────────────┐
            ▼                         ▼
┌─────────────────────┐   ┌─────────────────────┐
│  UIPanelController  │   │   CursorManager     │
│  (New)              │   │   (New)             │
│  - Opens panels     │   │  - Lock/unlock      │
│  - Closes panels    │   │  - Show/hide        │
│  - Coordinates      │   └─────────────────────┘
└─────────────────────┘
            │
            ▼
┌─────────────────────┐
│   IUIPanel          │  ← Interface for all panels
│  - Show()           │
│  - Hide()           │
│  - IsActive         │
└─────────────────────┘
```

---

## Implementation Plan

### Phase 1: Create Interfaces

#### 1.1 IUIPanel Interface

Create: `Assets/Game/Script/UI/Interfaces/IUIPanel.cs`

```csharp
namespace Game.UI
{
    /// <summary>
    /// Interface for all UI panels
    /// Follows Interface Segregation Principle
    /// </summary>
    public interface IUIPanel
    {
        /// <summary>
        /// Shows the panel
        /// </summary>
        void Show();
        
        /// <summary>
        /// Hides the panel
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Toggles the panel visibility
        /// </summary>
        void Toggle();
        
        /// <summary>
        /// Returns true if panel is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Panel name for identification
        /// </summary>
        string PanelName { get; }
        
        /// <summary>
        /// Should this panel block player input?
        /// </summary>
        bool BlocksInput { get; }
        
        /// <summary>
        /// Should this panel unlock the cursor?
        /// </summary>
        bool UnlocksCursor { get; }
    }
}
```

#### 1.2 ICursorManager Interface

Create: `Assets/Game/Script/UI/Interfaces/ICursorManager.cs`

```csharp
namespace Game.UI
{
    /// <summary>
    /// Interface for cursor management
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface ICursorManager
    {
        void LockCursor();
        void UnlockCursor();
        void ShowCursor();
        void HideCursor();
        bool IsCursorLocked { get; }
        bool IsCursorVisible { get; }
    }
}
```

#### 1.3 IInputBlocker Interface

Create: `Assets/Game/Script/UI/Interfaces/IInputBlocker.cs`

```csharp
namespace Game.UI
{
    /// <summary>
    /// Interface for blocking player input
    /// Follows Dependency Inversion Principle
    /// </summary>
    public interface IInputBlocker
    {
        void BlockInput();
        void UnblockInput();
        bool IsInputBlocked { get; }
    }
}
```

---

### Phase 2: Implement Core Services

#### 2.1 CursorManager

Create: `Assets/Game/Script/UI/Services/CursorManager.cs`

```csharp
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Manages cursor visibility and lock state
    /// Single Responsibility: Cursor control only
    /// </summary>
    public class CursorManager : ICursorManager
    {
        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;
        public bool IsCursorVisible => Cursor.visible;
        
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        public void ShowCursor()
        {
            Cursor.visible = true;
        }
        
        public void HideCursor()
        {
            Cursor.visible = false;
        }
    }
}
```

#### 2.2 PlayerInputBlocker

Create: `Assets/Game/Script/UI/Services/PlayerInputBlocker.cs`

```csharp
using UnityEngine;
using Game.Player;

namespace Game.UI
{
    /// <summary>
    /// Blocks player input when UI is open
    /// Single Responsibility: Input blocking only
    /// </summary>
    public class PlayerInputBlocker : IInputBlocker
    {
        private readonly PlayerControllerRefactored _playerController;
        private bool _isBlocked = false;
        
        public bool IsInputBlocked => _isBlocked;
        
        public PlayerInputBlocker(PlayerControllerRefactored playerController)
        {
            _playerController = playerController;
        }
        
        public void BlockInput()
        {
            if (_isBlocked) return;
            
            _isBlocked = true;
            _playerController?.SetInputBlocked(true);
        }
        
        public void UnblockInput()
        {
            if (!_isBlocked) return;
            
            _isBlocked = false;
            _playerController?.SetInputBlocked(false);
        }
    }
}
```

#### 2.3 UIPanelController

Create: `Assets/Game/Script/UI/Services/UIPanelController.cs`

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
        
        public UIPanelController(
            ICursorManager cursorManager,
            IInputBlocker inputBlocker)
        {
            _panels = new Dictionary<string, IUIPanel>();
            _cursorManager = cursorManager;
            _inputBlocker = inputBlocker;
            _activePanels = new HashSet<IUIPanel>();
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
```

---

### Phase 3: Implement Panel Adapters

Make existing UI classes implement `IUIPanel`. Here's an example for one:

#### 3.1 TabbedInventoryUIAdapter

Create: `Assets/Game/Script/UI/Adapters/TabbedInventoryUIAdapter.cs`

```csharp
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
```

**Repeat this pattern for all UI panels:**
- CraftingUIAdapter
- EquipmentUIAdapter
- StatsTrackerUIAdapter
- etc.

---

### Phase 4: Create Service Provider

#### 4.1 UIServiceProvider

Create: `Assets/Game/Script/UI/UIServiceProvider.cs`

```csharp
using UnityEngine;
using Game.Player;
using System.Linq;

namespace Game.UI
{
    /// <summary>
    /// Central service provider for UI system
    /// Uses Service Locator pattern with dependency injection
    /// Replaces the old UIManager
    /// </summary>
    public class UIServiceProvider : MonoBehaviour
    {
        [Header("Player References")]
        [SerializeField] private PlayerControllerRefactored playerController;
        [SerializeField] private CinemachinePlayerCamera playerCamera;
        
        private UIPanelController _panelController;
        private ICursorManager _cursorManager;
        private IInputBlocker _inputBlocker;
        
        // Singleton for easy access (can be replaced with DI container later)
        private static UIServiceProvider _instance;
        public static UIServiceProvider Instance => _instance;
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find player components if not assigned
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerControllerRefactored>();
            
            // Initialize services
            InitializeServices();
            RegisterAllPanels();
        }
        
        private void InitializeServices()
        {
            // Create services with dependency injection
            _cursorManager = new CursorManager();
            _inputBlocker = new PlayerInputBlocker(playerController);
            _panelController = new UIPanelController(_cursorManager, _inputBlocker);
        }
        
        private void RegisterAllPanels()
        {
            // Find all IUIPanel implementations in scene
            var panels = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IUIPanel>();
            
            foreach (var panel in panels)
            {
                _panelController.RegisterPanel(panel);
            }
        }
        
        // Public API
        public UIPanelController PanelController => _panelController;
        public ICursorManager CursorManager => _cursorManager;
        public IInputBlocker InputBlocker => _inputBlocker;
        
        // Convenience methods
        public void OpenPanel(string panelName) => _panelController.OpenPanel(panelName);
        public void ClosePanel(string panelName) => _panelController.ClosePanel(panelName);
        public void TogglePanel(string panelName) => _panelController.TogglePanel(panelName);
        public void CloseAllPanels() => _panelController.CloseAllPanels();
        public bool IsAnyPanelOpen() => _panelController.IsAnyPanelOpen();
    }
}
```

---

## Migration Guide

### Step 1: Create New Files

1. Create `Interfaces` folder under `Assets/Game/Script/UI/`
2. Create `Services` folder under `Assets/Game/Script/UI/`
3. Create `Adapters` folder under `Assets/Game/Script/UI/`
4. Add all interface files
5. Add all service files
6. Add UIServiceProvider

### Step 2: Create Adapters

For each existing UI class (TabbedInventoryUI, CraftingUI, etc.):
1. Create an adapter class
2. Attach adapter component to same GameObject
3. Test that adapter correctly wraps the UI

### Step 3: Update References

Replace all `UIManager.Instance.OpenInventory()` calls with:
```csharp
UIServiceProvider.Instance.OpenPanel("Inventory");
```

### Step 4: Remove Old UIManager

Once all references are updated:
1. Disable UIManager component
2. Test thoroughly
3. Delete UIManager.cs

---

## Benefits of Refactored Design

### ✅ Single Responsibility Principle

Each class has one clear purpose:
- `CursorManager` - Only manages cursor
- `PlayerInputBlocker` - Only blocks input
- `UIPanelController` - Only coordinates panels
- `UIServiceProvider` - Only provides services

### ✅ Open/Closed Principle

Add new UI panels without modifying existing code:
```csharp
// Just create adapter, no changes to controller
public class NewPanelAdapter : MonoBehaviour, IUIPanel
{
    // Implement interface
}
```

### ✅ Dependency Inversion Principle

Depends on abstractions (interfaces) not concrete classes:
```csharp
public UIPanelController(
    ICursorManager cursorManager,  // ← Interface
    IInputBlocker inputBlocker)    // ← Interface
```

### ✅ Testability

Easy to unit test with mocks:
```csharp
var mockCursor = new MockCursorManager();
var mockInput = new MockInputBlocker();
var controller = new UIPanelController(mockCursor, mockInput);
// Test without Unity dependencies!
```

---

## Next Steps

Continue to:
- **Part 3:** Inventory System Refactoring
- **Part 4:** Dependency Injection Container
- **Part 5:** Complete Migration Examples

---

**Document Version:** 1.0  
**Last Updated:** February 2, 2026
