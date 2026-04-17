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
        [SerializeField] private HJBClickPathController hjbClickPathController;
        [SerializeField] private WorldDataManager worldDataManager;

        [Header("Shared State")]
        [SerializeField] private SharedLoadingState loadingState;

        [Header("Options")]
        [SerializeField] private bool enableAsyncLoadFlow = true;
        [SerializeField] private bool enableCoordinatorLogs = true;

        private IEnumerator Start()
        {
            if (loadingState != null) loadingState.Reset();

            bool shouldUseAsyncLoadFlow = ShouldUseAsyncLoadFlow();

            if (!shouldUseAsyncLoadFlow)
            {
                if (enableCoordinatorLogs)
                    Debug.Log("[AsyncLoadCoordinator] Async loading flow disabled. Waiting for chunks then spawning.");

                if (loadingState != null)
                    yield return new WaitUntil(() => loadingState.isChunksReady);

                CompleteTransitionAndSpawnPlayer();
                yield break;
            }

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

            // --- Gate 1.5: Trigger HJB path calculation and wait for completion ---
            if (hjbClickPathController != null && worldDataManager != null)
            {
                if (enableCoordinatorLogs)
                    Debug.Log("[AsyncLoadCoordinator] Triggering HJB path calculation from spawn coord...");
                if (loadingState != null)
                {
                    loadingState.statusMessage = "Calculating optimal path to peak...";
                }
                yield return hjbClickPathController.CalculatePathFromSpawnToPeak(worldDataManager.completeSpawnCoord);
            }
            else if (enableCoordinatorLogs)
            {
                Debug.LogWarning("[AsyncLoadCoordinator] HJBClickPathController or WorldDataManager not assigned. Skipping HJB path calculation.");
            }

            // Snap progress to 100% and show confirm prompt message
            if (loadingState != null)
            {
                loadingState.progress = 1f;
                loadingState.statusMessage = "Press Y to enter world";
            }

            if (enableCoordinatorLogs)
                Debug.Log("[AsyncLoadCoordinator] Gate 1 passed  all chunks ready and HJB path calculated. Waiting for player confirmation.");

            // --- Gate 2: Wait for player to press Y in Scene_Debug_Gameplay ---
            yield return new WaitUntil(() =>
                loadingState != null && loadingState.playerConfirmed);

            if (enableCoordinatorLogs)
                Debug.Log("[AsyncLoadCoordinator] Gate 2 passed — player confirmed. Transitioning.");

            // --- Step 3: Unload the waiting-room scene ---
            yield return SceneManager.UnloadSceneAsync(debugGameplaySceneName);

            CompleteTransitionAndSpawnPlayer();
        }

        private void CompleteTransitionAndSpawnPlayer()
        {
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
            {
                if (enableCoordinatorLogs)
                    Debug.LogError("[AsyncLoadCoordinator] RenderController not assigned! Cannot spawn player.");
            }

            if (loadingState != null)
                loadingState.isComplete = true;

            if (enableCoordinatorLogs)
                Debug.Log("[AsyncLoadCoordinator] Transition complete. Player spawning.");
        }

        private bool ShouldUseAsyncLoadFlow()
        {
            if (!enableAsyncLoadFlow)
            {
                return false;
            }

            SaveLoadService saveLoadService = SaveLoadService.Instance;
            if (saveLoadService == null)
            {
                if (enableCoordinatorLogs)
                    Debug.LogWarning("[AsyncLoadCoordinator] SaveLoadService not available. Async flow will be skipped.");
                return false;
            }

            WorldSaveData currentSave = saveLoadService.CurrentWorldSave;
            if (currentSave == null)
            {
                if (enableCoordinatorLogs)
                    Debug.LogWarning("[AsyncLoadCoordinator] CurrentWorldSave is null. Async flow will be skipped.");
                return false;
            }

            if (saveLoadService.IsNewWorld())
            {
                return true;
            }

            WorldStateSaveData worldState = currentSave.worldState;
            if (worldState == null)
            {
                return false;
            }

            int currentLevel = Mathf.Max(1, worldState.level);
            if (worldState.cachedPathsByLevel == null)
            {
                return true;
            }

            for (int i = 0; i < worldState.cachedPathsByLevel.Count; i++)
            {
                LevelPathSaveData levelPath = worldState.cachedPathsByLevel[i];
                if (levelPath != null && levelPath.level == currentLevel)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
