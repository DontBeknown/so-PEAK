using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Core.DI;
using Game.Core.Events;

public class CraftingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject craftingPanel;
    [SerializeField] private Transform recipeSlotsContainer;
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Selected Recipe Panel")]
    [SerializeField] private GameObject selectedRecipePanel;
    [SerializeField] private Image selectedRecipeIcon;
    [SerializeField] private TextMeshProUGUI selectedRecipeName;
    [SerializeField] private TextMeshProUGUI selectedRecipeDescription;
    [SerializeField] private Transform selectedRequirementsContainer;
    [SerializeField] private GameObject requirementDisplayPrefab;
    [SerializeField] private Button craftButton;
    [SerializeField] private TextMeshProUGUI craftTimeText;
    [SerializeField] private GameObject craftingProgressBar;
    [SerializeField] private Slider craftingProgressSlider;

    [Header("References")]
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private InventoryManager inventoryManager;

    private IEventBus eventBus;
    private List<CraftingSlotUI> recipeSlotUIs = new List<CraftingSlotUI>();
    private CraftingSlotUI selectedSlot = null;
    private CraftingRecipe currentRecipe = null;
    private bool isCrafting = false;
    private List<GameObject> requirementDisplays = new List<GameObject>();

    public bool IsActive => craftingPanel != null && craftingPanel.activeSelf;

    private void Awake()
    {
        // Get references from ServiceContainer (DI)
        if (craftingManager == null)
            craftingManager = ServiceContainer.Instance.TryGet<CraftingManager>();

        if (inventoryManager == null)
            inventoryManager = ServiceContainer.Instance.TryGet<InventoryManager>();
            
        eventBus = ServiceContainer.Instance.TryGet<IEventBus>();

        // Setup craft button
        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }

        // Hide progress bar initially
        if (craftingProgressBar != null)
            craftingProgressBar.SetActive(false);
    }

    private void OnEnable()
    {
        // Subscribe to EventBus events
        if (eventBus != null)
        {
            eventBus.Subscribe<CraftingStartedEvent>(OnCraftingStartedEvent);
            eventBus.Subscribe<CraftingCompletedEvent>(OnCraftingCompletedEvent);
            eventBus.Subscribe<CraftingFailedEvent>(OnCraftingFailedEvent);
        }
        
        InventoryManager.OnInventoryChanged += RefreshRecipeList;
    }

    private void OnDisable()
    {
        // Unsubscribe from EventBus events
        if (eventBus != null)
        {
            eventBus.Unsubscribe<CraftingStartedEvent>(OnCraftingStartedEvent);
            eventBus.Unsubscribe<CraftingCompletedEvent>(OnCraftingCompletedEvent);
            eventBus.Unsubscribe<CraftingFailedEvent>(OnCraftingFailedEvent);
        }
        
        InventoryManager.OnInventoryChanged -= RefreshRecipeList;
    }

    public void ShowCraftingPanel()
    {
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(true);
        }

        RefreshRecipeList();
        ShowEmptySelection();
    }

    public void HideCraftingPanel()
    {
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }

        ClearSelection();
    }

    private void RefreshRecipeList()
    {
        if (craftingManager == null || recipeSlotsContainer == null || recipeSlotPrefab == null)
            return;

        // Clear existing slots
        foreach (var slot in recipeSlotUIs)
        {
            if (slot != null && slot.gameObject != null)
                Destroy(slot.gameObject);
        }
        recipeSlotUIs.Clear();

        // Get all recipes (not just craftable ones)
        List<CraftingRecipe> recipes = craftingManager.GetAllRecipes();


        // Create UI slots for each recipe
        foreach (var recipe in recipes)
        {
            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeSlotsContainer);
            CraftingSlotUI slotUI = slotObj.GetComponent<CraftingSlotUI>();

            if (slotUI != null)
            {
                slotUI.Initialize(this, recipe, inventoryManager);
                recipeSlotUIs.Add(slotUI);

                // Add click handler
                Button button = slotObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => SelectRecipe(slotUI));
                }
            }
        }

        // Update selected recipe if it's still available
        if (selectedSlot != null)
        {
            UpdateSelectedRecipeDisplay();
        }
    }

    public void SelectRecipe(CraftingSlotUI slotUI)
    {
        if (slotUI == null) return;

        // Deselect previous
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // Select new
        selectedSlot = slotUI;
        selectedSlot.SetSelected(true);
        currentRecipe = slotUI.Recipe;

        UpdateSelectedRecipeDisplay();
    }

    private void UpdateSelectedRecipeDisplay()
    {
        if (selectedRecipePanel == null || currentRecipe == null)
        {
            ShowEmptySelection();
            return;
        }

        selectedRecipePanel.SetActive(true);

        // Update icon
        if (selectedRecipeIcon != null && currentRecipe.icon != null)
        {
            selectedRecipeIcon.sprite = currentRecipe.icon;
            selectedRecipeIcon.enabled = true;
        }

        // Update name
        if (selectedRecipeName != null)
        {
            selectedRecipeName.text = currentRecipe.recipeName;
        }

        // Update description
        if (selectedRecipeDescription != null)
        {
            selectedRecipeDescription.text = currentRecipe.description;
        }

        // Update craft time
        if (craftTimeText != null)
        {
            craftTimeText.text = $"Craft Time: {currentRecipe.craftingTime}s";
        }

        // Update requirements
        UpdateRequirementsDisplay();

        // Update craft button
        UpdateCraftButton();
    }

    private void UpdateRequirementsDisplay()
    {
        if (selectedRequirementsContainer == null || requirementDisplayPrefab == null)
            return;

        // Clear existing displays
        foreach (var display in requirementDisplays)
        {
            if (display != null) Destroy(display);
        }
        requirementDisplays.Clear();

        if (currentRecipe == null) return;

        // Create requirement displays
        foreach (var requirement in currentRecipe.requirements)
        {
            GameObject displayObj = Instantiate(requirementDisplayPrefab, selectedRequirementsContainer);
            requirementDisplays.Add(displayObj);

            // Setup display
            Image icon = displayObj.GetComponentInChildren<Image>();
            TextMeshProUGUI text = displayObj.GetComponentInChildren<TextMeshProUGUI>();

            if (icon != null && requirement.item != null && requirement.item.icon != null)
            {
                icon.sprite = requirement.item.icon;
                icon.enabled = true;
            }

            if (text != null && inventoryManager != null)
            {
                int hasAmount = inventoryManager.GetItemCount(requirement.item);
                int needAmount = requirement.quantity;
                bool hasEnough = hasAmount >= needAmount;

                text.text = $"{requirement.item.itemName}: {hasAmount}/{needAmount}";
                text.color = hasEnough ? Color.white : Color.red;
            }
        }
    }

    private void UpdateCraftButton()
    {
        if (craftButton == null) return;

        bool canCraft = currentRecipe != null && 
                       craftingManager != null && 
                       craftingManager.CanCraftRecipe(currentRecipe) &&
                       !isCrafting;

        craftButton.interactable = canCraft;

        TextMeshProUGUI buttonText = craftButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isCrafting)
                buttonText.text = "Crafting...";
            else if (canCraft)
                buttonText.text = "Craft";
            else
                buttonText.text = "Cannot Craft";
        }
    }

    private void ShowEmptySelection()
    {
        if (selectedRecipePanel != null)
        {
            selectedRecipePanel.SetActive(false);
        }
    }

    private void ClearSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
        currentRecipe = null;
        ShowEmptySelection();
    }

    private void OnCraftButtonClicked()
    {
        if (currentRecipe != null && !isCrafting)
        {
            CraftRecipe(currentRecipe);
        }
    }

    public void CraftRecipe(CraftingRecipe recipe)
    {
        if (craftingManager == null || recipe == null || isCrafting) return;

        if (craftingManager.CanCraftRecipe(recipe))
        {
            craftingManager.StartCrafting(recipe);
        }
        else
        {
            Debug.Log("Cannot craft - missing requirements or crafting station");
        }
    }

    // EventBus event handlers
    private void OnCraftingStartedEvent(CraftingStartedEvent evt)
    {
        OnCraftingStarted(evt.Recipe);
    }
    
    private void OnCraftingCompletedEvent(CraftingCompletedEvent evt)
    {
        OnCraftingCompleted(evt.Recipe);
    }
    
    private void OnCraftingFailedEvent(CraftingFailedEvent evt)
    {
        OnCraftingFailed(evt.Recipe);
    }

    private void OnCraftingStarted(CraftingRecipe recipe)
    {
        isCrafting = true;
        UpdateCraftButton();

        if (craftingProgressBar != null)
        {
            craftingProgressBar.SetActive(true);
        }

        if (recipe != null)
        {
            StartCoroutine(UpdateCraftingProgress(recipe.craftingTime));
        }
    }

    private void OnCraftingCompleted(CraftingRecipe recipe)
    {
        isCrafting = false;
        
        if (craftingProgressBar != null)
        {
            craftingProgressBar.SetActive(false);
        }

        RefreshRecipeList();
        UpdateSelectedRecipeDisplay();

        //Debug.Log($"Successfully crafted {recipe.recipeName}!");
    }

    private void OnCraftingFailed(CraftingRecipe recipe)
    {
        isCrafting = false;
        
        if (craftingProgressBar != null)
        {
            craftingProgressBar.SetActive(false);
        }

        UpdateCraftButton();
        Debug.Log($"Failed to craft {recipe.recipeName}");
    }

    private System.Collections.IEnumerator UpdateCraftingProgress(float craftTime)
    {
        float elapsed = 0f;

        while (elapsed < craftTime)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for pause compatibility
            
            if (craftingProgressSlider != null)
            {
                craftingProgressSlider.value = elapsed / craftTime;
            }

            yield return null;
        }

        if (craftingProgressSlider != null)
        {
            craftingProgressSlider.value = 1f;
        }
    }
}
