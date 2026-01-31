using UnityEngine;
using Game.Player;
using Game.Interaction.UI;
/// <summary>
/// Centralized UI Manager that holds references to all UI panels
/// and manages their visibility and interactions.
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIManager>();
            }
            return instance;
        }
    }

    [Header("UI Panels")]
    [SerializeField] private TabbedInventoryUI inventoryUI;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private EquipmentUI equipmentUI;
    [SerializeField] private PlayerStatsTrackerUI statsTrackerUI;
    [SerializeField] private SimpleStatsHUD simpleStatsHUD;
    [SerializeField] private ItemNotificationUI itemNotificationUI;
    [SerializeField] private InteractionPromptUI interactionPromptUI;
    
    [Header("Player References")]
    [SerializeField] private CinemachinePlayerCamera playerCamera;
    [SerializeField] private PlayerControllerRefactored playerController;
    
    private bool isAnyMenuOpen = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Auto-find components if not assigned
        FindUIReferences();
    }
    
    private void FindUIReferences()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<TabbedInventoryUI>();
        
        if (craftingUI == null)
            craftingUI = FindFirstObjectByType<CraftingUI>();
        
        if (equipmentUI == null)
            equipmentUI = FindFirstObjectByType<EquipmentUI>();
        
        if (statsTrackerUI == null)
            statsTrackerUI = FindFirstObjectByType<PlayerStatsTrackerUI>();
        
        if (simpleStatsHUD == null)
            simpleStatsHUD = FindFirstObjectByType<SimpleStatsHUD>();
        
        if (itemNotificationUI == null)
            itemNotificationUI = FindFirstObjectByType<ItemNotificationUI>();
        
        if (interactionPromptUI == null)
            interactionPromptUI = FindFirstObjectByType<InteractionPromptUI>();
        
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<CinemachinePlayerCamera>();
        
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerControllerRefactored>();
    }
    
    // Public getters for UI components
    public TabbedInventoryUI InventoryUI => inventoryUI;
    public CraftingUI CraftingUI => craftingUI;
    public EquipmentUI EquipmentUI => equipmentUI;
    public ItemNotificationUI ItemNotificationUI => itemNotificationUI;
    public PlayerStatsTrackerUI StatsTrackerUI => statsTrackerUI;

    public SimpleStatsHUD SimpleStatsHUD => simpleStatsHUD;
    public CinemachinePlayerCamera PlayerCamera => playerCamera;
    public PlayerControllerRefactored PlayerController => playerController;
    
    /// <summary>
    /// Opens the inventory UI
    /// </summary>
    public void OpenInventory()
    {
        if (inventoryUI == null) return;
        
        inventoryUI.OpenUI();
        HidePickupPrompt();
        SetMenuOpen(true);
    }
    
    /// <summary>
    /// Closes the inventory UI
    /// </summary>
    public void CloseInventory()
    {
        if (inventoryUI == null) return;
        
        inventoryUI.CloseUI();
        ShowPickupPromptIfNeeded();
        SetMenuOpen(false);
    }
    
    /// <summary>
    /// Toggles the inventory UI
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryUI == null) return;
        
        if (inventoryUI.IsActive)
            CloseInventory();
        else
            OpenInventory();
    }
    
    /// <summary>
    /// Opens the crafting UI
    /// </summary>
    public void OpenCrafting()
    {
        if (craftingUI == null) return;
        
        craftingUI.ShowCraftingPanel();
        HidePickupPrompt();
        SetMenuOpen(true);
    }
    
    /// <summary>
    /// Closes the crafting UI
    /// </summary>
    public void CloseCrafting()
    {
        if (craftingUI == null) return;
        
        craftingUI.HideCraftingPanel();
        ShowPickupPromptIfNeeded();
        SetMenuOpen(false);
    }
    
    /// <summary>
    /// Opens the equipment UI
    /// </summary>
    public void OpenEquipment()
    {
        if (equipmentUI == null) return;
        
        equipmentUI.ShowEquipmentPanel();
        HidePickupPrompt();
        SetMenuOpen(true);
    }
    
    /// <summary>
    /// Closes the equipment UI
    /// </summary>
    public void CloseEquipment()
    {
        if (equipmentUI == null) return;
        
        equipmentUI.HideEquipmentPanel();
        ShowPickupPromptIfNeeded();
        SetMenuOpen(false);
    }
    
    /// <summary>
    /// Opens the stats tracker UI
    /// </summary>
    public void OpenStatsTracker()
    {
        if (statsTrackerUI == null) return;
        
        statsTrackerUI.Show();
        HidePickupPrompt();
        SetMenuOpen(true);
    }
    
    /// <summary>
    /// Closes the stats tracker UI
    /// </summary>
    public void CloseStatsTracker()
    {
        if (statsTrackerUI == null) return;
        
        statsTrackerUI.Hide();
        ShowPickupPromptIfNeeded();
        SetMenuOpen(false);
    }
    
    /// <summary>
    /// Toggles the stats tracker UI
    /// </summary>
    public void ToggleStatsTracker()
    {
        if (statsTrackerUI == null) return;
        
        statsTrackerUI.Toggle();
        
        // Check if it's now open or closed
        if (statsTrackerUI.IsActive)
        {
            HidePickupPrompt();
            SetMenuOpen(true);
        }
        else
        {
            ShowPickupPromptIfNeeded();
            SetMenuOpen(false);
        }
    }
    
    /// <summary>
    /// Hides the interaction prompt
    /// </summary>
    public void HidePickupPrompt()
    {
        if (interactionPromptUI != null)
        {
            interactionPromptUI.ForceHide();
        }
    }
    
    /// <summary>
    /// Shows the interaction prompt if conditions are met
    /// InteractionPromptUI manages its own visibility via InteractionDetector events
    /// </summary>
    public void ShowPickupPromptIfNeeded()
    {
        // InteractionPromptUI automatically shows based on InteractionDetector events
        // No manual control needed - it will re-enable when menus close
    }
    
    /// <summary>
    /// Shows the interaction prompt with custom text
    /// </summary>
    public void ShowPickupPrompt(string itemName)
    {
        if (interactionPromptUI != null && !isAnyMenuOpen)
        {
            interactionPromptUI.ShowCustomPrompt($"[F] Press F to {itemName}");
        }
    }
    
    /// <summary>
    /// Closes all open UI panels
    /// </summary>
    public void CloseAllPanels()
    {
        CloseInventory();
        CloseCrafting();
        CloseEquipment();
        CloseStatsTracker();
    }
    
    /// <summary>
    /// Sets the menu open state
    /// </summary>
    private void SetMenuOpen(bool open)
    {
        isAnyMenuOpen = open;
        
        // Update cursor and player input
        if (playerCamera != null)
        {
            playerCamera.SetCursorLock(!open);
        }
        
        if (playerController != null)
        {
            playerController.SetInputBlocked(open);
        }
    }
    
    /// <summary>
    /// Checks if any menu is currently open
    /// </summary>
    public bool IsAnyMenuOpen()
    {
        return isAnyMenuOpen || 
               (inventoryUI != null && inventoryUI.IsActive) ||
               (craftingUI != null && craftingUI.IsActive) ||
               (equipmentUI != null && equipmentUI.IsActive) ||
               (statsTrackerUI != null && statsTrackerUI.IsActive);
    }
}
