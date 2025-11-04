using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabbedInventoryUI : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button closeButton;

    [Header("Tab Buttons")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button craftingTabButton;

    [Header("Tab Panels")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftingUI craftingUI;

    [Header("Tab Visual Feedback")]
    [SerializeField] private Color activeTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private TabType defaultTab = TabType.Inventory;

    private TabType currentTab = TabType.Inventory;
    private bool isOpen = false;

    public enum TabType
    {
        Inventory,
        Crafting
    }

    public bool IsOpen => isOpen;

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

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUI);
        }

        // Find components if not assigned
        if (inventoryUI == null)
            inventoryUI = GetComponentInChildren<InventoryUI>(true);

        if (craftingUI == null)
            craftingUI = GetComponentInChildren<CraftingUI>(true);

        // Start closed
        CloseUI();
    }

    private void Start()
    {
        // Set default tab
        currentTab = defaultTab;
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

        // Pause game if required
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

        // Resume game if it was paused
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
        TabType nextTab = currentTab == TabType.Inventory ? TabType.Crafting : TabType.Inventory;
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
}
