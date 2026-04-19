using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Disables the assigned EventSystem when the scene contains more than one EventSystem.
/// Assign the EventSystem to disable in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class DisableDuplicateEventSystem : MonoBehaviour
{
    [Tooltip("EventSystem to disable if multiple EventSystems are found in the scene.")]
    [SerializeField] private EventSystem eventSystemToDisable;

    [Tooltip("If true, disables the whole GameObject. If false, only disables the EventSystem component.")]
    [SerializeField] private bool disableGameObject = true;

    private void Awake()
    {
        if (eventSystemToDisable == null)
        {
            Debug.LogWarning("[DisableDuplicateEventSystem] No EventSystem assigned in Inspector.", this);
            return;
        }

        EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (allEventSystems.Length <= 1)
            return;

        if (disableGameObject)
            eventSystemToDisable.gameObject.SetActive(false);
        else
            eventSystemToDisable.enabled = false;

        Debug.Log($"[DisableDuplicateEventSystem] Found {allEventSystems.Length} EventSystems. Disabled: {eventSystemToDisable.name}", this);
    }
}