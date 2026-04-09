using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// Orchestrates the two-gate background loading flow:
    ///
    ///   Gate 1 — waits for RenderController to finish all terrain chunks
    ///            (SharedLoadingState.isChunksReady).
    ///
    ///   Gate 2 — waits for the player to press Y in Scene_Debug_Gameplay
    ///            (SharedLoadingState.playerConfirmed).
    ///
    /// After both gates pass:
    ///   1. Scene_Debug_Gameplay is unloaded.
    ///   2. TerrainGenDemo camera is re-enabled.
    ///   3. RenderController.SpawnPlayerNow() is called to spawn the player.
    ///
    /// Attach this to an empty GameObject in TerrainGenDemo and wire up the Inspector fields.
    /// </summary>
    public class AsyncLoadCoordinator : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string debugGameplaySceneName = "Scene_Debug_Gameplay";

        [Header("References")]
        [SerializeField] private RenderController renderController;
        [SerializeField] private Camera terrainCamera;

        [Header("Shared State")]
        [SerializeField] private SharedLoadingState loadingState;

        private IEnumerator Start()
        {
            if (loadingState != null) loadingState.Reset();

            // --- Step 1: Hide terrain camera so only DebugGameplay camera renders ---
            if (terrainCamera != null)
                terrainCamera.gameObject.SetActive(false);

            // --- Step 2: Load the waiting-room scene additively ---
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(
                debugGameplaySceneName, LoadSceneMode.Additive);
            yield return loadOp;

            // --- Gate 1: Wait until all terrain chunks are finalized ---
            yield return new WaitUntil(() =>
                loadingState != null && loadingState.isChunksReady);

            // Snap progress to 100% and show confirm prompt message
            if (loadingState != null)
            {
                loadingState.progress = 1f;
                loadingState.statusMessage = "Press Y to enter world";
            }

            Debug.Log("[AsyncLoadCoordinator] Gate 1 passed — all chunks ready. Waiting for player confirmation.");

            // --- Gate 2: Wait for player to press Y in Scene_Debug_Gameplay ---
            yield return new WaitUntil(() =>
                loadingState != null && loadingState.playerConfirmed);

            Debug.Log("[AsyncLoadCoordinator] Gate 2 passed — player confirmed. Transitioning.");

            // --- Step 3: Unload the waiting-room scene ---
            yield return SceneManager.UnloadSceneAsync(debugGameplaySceneName);

            // --- Step 4: Restore terrain camera ---
            if (terrainCamera != null)
                terrainCamera.gameObject.SetActive(true);

            // --- Step 5: Make TerrainGenDemo the active scene ---
            Scene terrainScene = SceneManager.GetSceneByName("TerrainGenDemo");
            if (terrainScene.IsValid())
                SceneManager.SetActiveScene(terrainScene);

            // --- Step 6: Spawn the player (chunks are ready, debug scene is gone) ---
            if (renderController != null)
                renderController.SpawnPlayerNow();
            else
                Debug.LogError("[AsyncLoadCoordinator] RenderController not assigned! Cannot spawn player.");

            if (loadingState != null)
                loadingState.isComplete = true;

            Debug.Log("[AsyncLoadCoordinator] Transition complete. Player spawning.");
        }
    }
}
