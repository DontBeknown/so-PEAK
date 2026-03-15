using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Sound.Events;
using Game.Core.DI;
using Game.Core.Events;
using Game.UI.Collectable;

public class TabbedInventoryUI : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button closeButton;

    [Header("Tab Buttons")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button craftingTabButton;
    [SerializeField] private Button collectablesTabButton;

    [Header("Tab Panels")]
    [SerializeField] private GridInventoryUI inventoryUI;
    [SerializeField] private CraftingUI craftingUI;
    [SerializeField] private CollectablesHubUI collectablesHubUI;

    [Header("Tab Visual Feedback")]
    [SerializeField] private Color activeTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Settings")]
    //[SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private TabType defaultTab = TabType.Inventory;

    [Header("Sound IDs")]
    [SerializeField] private string soundInventoryOpen  = "UI_InventoryOpen";
    [SerializeField] private string soundInventoryClose = "UI_InventoryClose";
    [SerializeField] private string soundCraftingOpen   = "UI_CraftingOpen";
    [SerializeField] private string soundCollectablesOpen = "UI_InventoryOpen";

    private TabType currentTab = TabType.Inventory;
    private bool isOpen = false;
    private IEventBus _eventBus;

    public enum TabType
    {
        Inventory,
        Crafting,
        Collectables
    }

    public bool IsOpen => isOpen;
    public bool IsActive => isOpen;

    private void Awake()
    {
        // Setup tab buttons
        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.RemoveAllListeners();
            inventoryTabButton.onClick.AddListener(() => SwitchTab(TabType.Inventory));
        }

        if (craftingTabButton != null)
        {
            craftingTabButton.onClick.RemoveAllListeners();
            craftingTabButton.onClick.AddListener(() => SwitchTab(TabType.Crafting));
        }

        if (collectablesTabButton != null)
        {
            collectablesTabButton.onClick.RemoveAllListeners();
            collectablesTabButton.onClick.AddListener(() => SwitchTab(TabType.Collectables));
        }

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUI);
        }

        // Find components if not assigned
        if (inventoryUI == null)
            inventoryUI = GetComponentInChildren<GridInventoryUI>(true);

        if (craftingUI == null)
            craftingUI = GetComponentInChildren<CraftingUI>(true);

        if (collectablesHubUI == null)
            collectablesHubUI = GetComponentInChildren<CollectablesHubUI>(true);

        // Start closed
        CloseUI();
    }

    private void Start()
    {
        // Set default tab
        currentTab = defaultTab;

        _eventBus = ServiceContainer.Instance?.Get<IEventBus>();
    }


    public void ToggleUI()
    {
        if (isOpen)
            CloseUI();
        else
            OpenUI();
    }

    public void OpenUI()
    {
        OpenUI(defaultTab);
    }

    public void OpenUI(TabType tab)
    {
        if (isOpen) return;

        isOpen = true;

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        // Switch to the specified tab
        SwitchTab(tab);
    }

    public void CloseUI()
    {
        if (!isOpen && mainPanel != null && !mainPanel.activeSelf) return;

        isOpen = false;

        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        // Hide both panels
        if (inventoryUI != null)
        {
            inventoryUI.HideInventoryPanel();
        }

        if (craftingUI != null)
        {
            craftingUI.HideCraftingPanel();
        }

        if (collectablesHubUI != null)
        {
            collectablesHubUI.HideHubPanel();
        }

        _eventBus?.Publish(new PlayUISoundEvent(soundInventoryClose, volumeScale: 0.3f));
    }

    public void SwitchTab(TabType tab)
    {
        currentTab = tab;

        switch (tab)
        {
            case TabType.Inventory:
                ShowInventoryTab();
                break;

            case TabType.Crafting:
                ShowCraftingTab();
                break;

            case TabType.Collectables:
                ShowCollectablesTab();
                break;
        }

        UpdateTabVisuals();
    }

    private void ShowInventoryTab()
    {
        // Show inventory panel
        if (inventoryUI != null)
        {
            inventoryUI.ShowInventoryPanel();
        }

        // Hide crafting panel
        if (craftingUI != null)
        {
            craftingUI.HideCraftingPanel();
        }

        // Hide collectables panel
        if (collectablesHubUI != null)
        {
            collectablesHubUI.HideHubPanel();
        }

        _eventBus?.Publish(new PlayUISoundEvent(soundInventoryOpen, volumeScale: 0.1f));
    }

    private void ShowCraftingTab()
    {
        // Hide inventory panel
        if (inventoryUI != null)
        {
            inventoryUI.HideInventoryPanel();
        }

        // Show crafting panel
        if (craftingUI != null)
        {
            craftingUI.ShowCraftingPanel();
        }

        // Hide collectables panel
        if (collectablesHubUI != null)
        {
            collectablesHubUI.HideHubPanel();
        }

        _eventBus?.Publish(new PlayUISoundEvent(soundCraftingOpen,volumeScale: 0.1f));
    }

    private void ShowCollectablesTab()
    {
        if (inventoryUI != null)
        {
            inventoryUI.HideInventoryPanel();
        }

        if (craftingUI != null)
        {
            craftingUI.HideCraftingPanel();
        }

        if (collectablesHubUI != null)
        {
            collectablesHubUI.ShowHubPanel();
        }

        _eventBus?.Publish(new PlayUISoundEvent(soundCollectablesOpen, volumeScale: 0.1f));
    }

    private void UpdateTabVisuals()
    {
        // Update inventory tab button
        if (inventoryTabButton != null)
        {
            UpdateButtonVisual(inventoryTabButton, currentTab == TabType.Inventory);
        }

        // Update crafting tab button
        if (craftingTabButton != null)
        {
            UpdateButtonVisual(craftingTabButton, currentTab == TabType.Crafting);
        }

        if (collectablesTabButton != null)
        {
            UpdateButtonVisual(collectablesTabButton, currentTab == TabType.Collectables);
        }
    }

    private void UpdateButtonVisual(Button button, bool isActive)
    {
        if (button == null) return;

        // Update button color
        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? activeTabColor : inactiveTabColor;
        button.colors = colors;

        // Update text color if present
        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.color = isActive ? activeTabColor : inactiveTabColor;
        }

        // Update image color if present
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isActive ? activeTabColor : inactiveTabColor;
        }
    }

    public void ToggleTab()
    {
        TabType nextTab = currentTab switch
        {
            TabType.Inventory => TabType.Crafting,
            TabType.Crafting => TabType.Collectables,
            _ => TabType.Inventory
        };

        SwitchTab(nextTab);
    }

    public TabType GetCurrentTab()
    {
        return currentTab;
    }

    // Called by input system or player controller
    public void OnToggleUIInput()
    {
        ToggleUI();
    }

    // Allow external scripts to open specific tabs
    public void OpenInventoryTab()
    {
        OpenUI(TabType.Inventory);
    }

    public void OpenCraftingTab()
    {
        OpenUI(TabType.Crafting);
    }

    public void OpenCollectablesTab()
    {
        OpenUI(TabType.Collectables);
    }
}
