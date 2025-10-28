using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;

    private PlayerModel model;
    private IPlayerState currentState;
    private IA_PlayerController inputActions;
    private Vector2 moveInput;

    [Header("Inventory System")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private CraftingManager craftingManager;
    [SerializeField] private ItemDetector itemDetector;
    [SerializeField] private InventoryUI inventoryUI;

    void Awake()
    {
        model = new PlayerModel(gameObject, config);

        inputActions = new IA_PlayerController();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

        inputActions.Player.Jump.performed += _ => currentState?.OnJump(model, moveInput);
        inputActions.Player.Climb.performed += _ => currentState?.OnClimb(model);


        inputActions.Player.Pickup.performed += _ => TryPickupItem();
        //inputActions.Player.QuickUse.performed += _ => QuickUseItem();


        if (inventoryManager == null)
            inventoryManager = GetComponent<InventoryManager>();
        if (craftingManager == null)
            craftingManager = GetComponent<CraftingManager>();
        if (itemDetector == null)
            itemDetector = GetComponent<ItemDetector>();
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUI>();

        inputActions.Player.OpenInventory.performed += _ => inventoryUI.ToggleInventory();

    }

    void Start()
    {
        ChangeState(new WalkingState());
    }

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void FixedUpdate()
    {
        currentState?.FixedUpdate(model, moveInput);

        if (!(currentState is ClimbingState))
        {
            if (!model.IsGrounded())
                ChangeState(new FallingState());
            else if (!(currentState is WalkingState))
                ChangeState(new WalkingState());
        }
    }

    public void ChangeState(IPlayerState newState)
    {
        currentState?.Exit(model);
        currentState = newState;
        currentState.Enter(model);
    }

    private void TryPickupItem()
    {
        if (itemDetector != null)
        {
            itemDetector.TryCollectNearestItem();
        }
    }

    // Optional: Method to get current target item for UI
    public ResourceCollector GetTargetItem()
    {
        return itemDetector != null ? itemDetector.NearestItem : null;
    }

    private void QuickUseItem()
    {
        // Quick use the first consumable item in inventory
        if (inventoryManager != null)
        {
            var slots = inventoryManager.GetInventorySlots();
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty && slot.item.isConsumable)
                {
                    inventoryManager.ConsumeItem(slot.item);
                    break;
                }
            }
        }
    }

    public void ConsumeItem(InventoryItem item)
    {
        if (inventoryManager != null)
        {
            inventoryManager.ConsumeItem(item);
        }
    }

    public void StartCrafting(CraftingRecipe recipe)
    {
        if (craftingManager != null)
        {
            craftingManager.StartCrafting(recipe);
        }
    }

    // Optional: Add getter methods for other systems to access
    public InventoryManager GetInventoryManager() => inventoryManager;
    public ItemDetector GetItemDetector() => itemDetector;
}
