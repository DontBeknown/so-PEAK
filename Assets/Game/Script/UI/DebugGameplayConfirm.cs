using UnityEngine;

/// <summary>
/// Lives inside Scene_Debug_Gameplay.
/// Watches SharedLoadingState.isChunksReady (Gate 1) and, once that is true,
/// shows a confirm prompt and listens for the player to press Y (Gate 2).
///
/// When Y is pressed, sets SharedLoadingState.playerConfirmed = true, which
/// releases AsyncLoadCoordinator's Gate 2 and triggers the scene transition.
///
/// Inspector setup:
///   - loadingState        → same SharedLoadingState asset used in TerrainGenDemo
///   - confirmPromptObject → a disabled UI panel/text: "Press Y to enter world"
///                          (start it disabled in the scene — this script shows it)
/// </summary>
public class DebugGameplayConfirm : MonoBehaviour
{
    [SerializeField] private SharedLoadingState loadingState;

    [Tooltip("UI panel shown only once all chunks are ready and we are awaiting player confirmation.")]
    [SerializeField] private GameObject confirmPromptObject;

    private bool _confirmed = false;

    private void Update()
    {
        if (loadingState == null || _confirmed) return;

        // Show the prompt only once Gate 1 has been satisfied
        if (confirmPromptObject != null)
            confirmPromptObject.SetActive(loadingState.isChunksReady);

        // Listen for Y only after Gate 1
        if (loadingState.isChunksReady && Input.GetKeyDown(KeyCode.Y))
        {
            _confirmed = true;
            loadingState.playerConfirmed = true;

            // Hide the prompt so it doesn't linger during scene unload
            if (confirmPromptObject != null)
                confirmPromptObject.SetActive(false);

            //Debug.Log("[DebugGameplayConfirm] Player confirmed — releasing Gate 2.");
        }
    }
}
