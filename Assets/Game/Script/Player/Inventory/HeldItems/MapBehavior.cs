using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Game.UI;
using Game.Player.Inventory.HeldItems;

/// <summary>
/// Runtime behavior for a held map item.
/// Opens the map viewer when left mouse is clicked while equipped.
/// </summary>
public class MapBehavior : MonoBehaviour, IHeldItemBehavior
{
    // Injected by HeldItemBehaviorManager (no Inspector assignment needed)
    [SerializeField] private Transform rightHandBone;
    [SerializeField] private MapItem mapItem;

    private GameObject visualPrefabInstance;
    private bool isEquipped;

    public void Initialize(MapItem item)
    {
        mapItem = item;
    }

    public void OnEquipped()
    {
        isEquipped = true;
        SpawnVisualPrefab();
    }

    public void OnUnequipped()
    {
        CloseMapPanel();
        DestroyVisualPrefab();
        isEquipped = false;
    }

    public void UpdateBehavior()
    {
        if (!isEquipped || mapItem == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryOpenMapPanel();
        }
    }

    public string GetStateDescription()
    {
        return mapItem?.GetStateDescription() ?? "Map";
    }

    public bool IsUsable()
    {
        return mapItem != null;
    }

    private void Update()
    {
        UpdateBehavior();
    }

    private void TryOpenMapPanel()
    {
        if (mapItem?.MapData == null)
        {
            Debug.LogWarning("[MapBehavior] No MapData assigned to map item.");
            return;
        }

        var uiServiceProvider = UIServiceProvider.Instance;
        if (uiServiceProvider == null)
        {
            Debug.LogWarning("[MapBehavior] UIServiceProvider not found in scene.");
            return;
        }

        var mapPanel = uiServiceProvider.GetPanel<MapViewerPanel>();
        if (mapPanel == null)
        {
            Debug.LogWarning("[MapBehavior] MapViewerPanel not found in the UI scene.");
            return;
        }

        if (!mapPanel.SetMapData(mapItem.MapData))
            return;

        uiServiceProvider.OpenPanel(mapPanel.PanelName);
    }

    private void CloseMapPanel()
    {
        var uiServiceProvider = UIServiceProvider.Instance;
        var mapPanel = uiServiceProvider != null ? uiServiceProvider.GetPanel<MapViewerPanel>() : null;

        if (uiServiceProvider != null && mapPanel != null)
        {
            uiServiceProvider.ClosePanel(mapPanel.PanelName);
            return;
        }

        mapPanel?.Hide();
    }

    private void SpawnVisualPrefab()
    {
        if (mapItem?.HeldItemPrefab == null)
            return;

        visualPrefabInstance = Instantiate(mapItem.HeldItemPrefab);

        if (rightHandBone != null)
        {
            visualPrefabInstance.transform.SetParent(rightHandBone);
            visualPrefabInstance.transform.localPosition = Vector3.zero;
            visualPrefabInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            visualPrefabInstance.transform.SetParent(transform);
            visualPrefabInstance.transform.localPosition = Vector3.right * 0.4f + Vector3.forward * 0.2f + Vector3.up * -0.2f;
            visualPrefabInstance.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Debug.LogWarning("[MapBehavior] rightHandBone not assigned! Using fallback hip position");
        }
    }

    private void DestroyVisualPrefab()
    {
        if (visualPrefabInstance != null)
        {
            Destroy(visualPrefabInstance);
            visualPrefabInstance = null;
        }
    }

    private void OnDestroy()
    {
        if (isEquipped)
        {
            OnUnequipped();
        }
    }
}