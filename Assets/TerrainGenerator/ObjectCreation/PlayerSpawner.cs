using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Game.UI;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;
using Game.Environment.DayNight;
using Game.Player;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Coordinates")]
    public Vector3 targetSpawnPosition;

    [Header("Player References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnMarkerTransform;

    [Header("UI References")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [SerializeField] private bool loadFromSave = true;
    
    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight = 100f; // Height above position to start raycast
    [SerializeField] private float raycastDistance = 200f; // Max distance to raycast down
    [SerializeField] private LayerMask groundLayers = -1; // Layers to check for ground (default: everything)
    [SerializeField] private float spawnDelay = 0.5f; // Delay in seconds before spawning (to let terrain load)
    [SerializeField] private float spawnHeightOffset = 10f; // Small offset to prevent ground clipping
    
    // Stores the spawned player reference after successful spawn
    public Transform SpawnedPlayer { get; private set; }
    
    public IEnumerator SpawnPlayer(Vector3 proceduralSpawnPosition)
    {
        return SpawnPlayerDelayed(proceduralSpawnPosition);
    }
    
    private System.Collections.IEnumerator SpawnPlayerDelayed(Vector3 proceduralSpawnPosition)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player Prefab is not assigned!");
            yield break;
        }

        if (spawnMarkerTransform == null)
        {
            Debug.LogError("[PlayerSpawner] Spawn Marker Transform is not assigned!");
            yield break;
        }

        if (loadingScreen != null)
        {
            yield return FadeInLoadingScreen();
        }

        // 1. DETERMINE TARGET XZ POSITION (without proper Y yet)
        Vector3 targetXZ;
        // Use proceduralSpawnPosition if it's a fresh level entry, otherwise use saved position
        bool isFreshLevelEntry = SaveLoadService.Instance.IsFreshLevelEntry();
        bool usingSavedPosition = !isFreshLevelEntry && !SaveLoadService.Instance.IsNewWorld() && loadFromSave;
        
        //Debug.Log($"[PlayerSpawner] FreshLevelEntry={isFreshLevelEntry}, IsNewWorld={SaveLoadService.Instance.IsNewWorld()}, loadFromSave={loadFromSave}");
        if(usingSavedPosition)
        {
           WorldSaveData saveData = SaveLoadService.Instance.CurrentWorldSave;
           targetXZ = new Vector3(saveData.playerData.position[0], saveData.playerData.position[1], saveData.playerData.position[2]);
           //Debug.Log($"[PlayerSpawner] Using saved position: {targetXZ}");
        }
        else
        {
            targetXZ = proceduralSpawnPosition;
            //Debug.Log($"[PlayerSpawner] Using procedural spawn position: {targetXZ}");
        }

        // 2. MOVE SPAWN MARKER TO TARGET POSITION (to trigger chunk generation)
        spawnMarkerTransform.position = targetXZ;
        //Debug.Log($"[PlayerSpawner] Moved spawn marker to {targetXZ} to trigger chunk generation");

        // 3. WAIT FOR CHUNKS TO GENERATE AND MESH COLLIDERS TO BAKE
        yield return new WaitForSeconds(spawnDelay);

        // 4. NOW DO THE RAYCAST TO FIND GROUND
        Vector3 raycastStart = new Vector3(targetXZ.x, targetXZ.y + raycastHeight, targetXZ.z);
        Vector3 raycastEnd = raycastStart + Vector3.down * raycastDistance;
        RaycastHit hit;
        
        //Debug.Log($"[PlayerSpawner] Raycasting from {raycastStart} down {raycastDistance} units");
        
        Vector3 finalSpawnPosition;
        if (Physics.Raycast(raycastStart, Vector3.down, out hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            // Found ground - use hit point Y position with small offset
            finalSpawnPosition = new Vector3(targetXZ.x, hit.point.y + spawnHeightOffset, targetXZ.z);
            //Debug.Log($"[PlayerSpawner] Found ground at Y={hit.point.y}, spawning at {finalSpawnPosition} (collider: {hit.collider.name})");
            //Debug.DrawLine(raycastStart, hit.point, Color.green, 120f);
        }
        else
        {
            // No ground found - if loaded from save, fall back to default spawn position.
            if (usingSavedPosition)
            {
                Vector3 defaultRaycastStart = new Vector3(targetSpawnPosition.x, targetSpawnPosition.y + raycastHeight, targetSpawnPosition.z);
                RaycastHit defaultHit;

                if (Physics.Raycast(defaultRaycastStart, Vector3.down, out defaultHit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
                {
                    finalSpawnPosition = new Vector3(targetSpawnPosition.x, defaultHit.point.y + spawnHeightOffset, targetSpawnPosition.z);
                    //Debug.LogWarning($"[PlayerSpawner] Raycast found no ground at saved position. Falling back to default ground at Y={defaultHit.point.y}: {finalSpawnPosition}");
                }
                else
                {
                    finalSpawnPosition = targetSpawnPosition;
                    //Debug.LogWarning($"[PlayerSpawner] Raycast found no ground at saved or default position. Using default position as-is: {finalSpawnPosition}");
                }
            }
            else
            {
                finalSpawnPosition = targetXZ;
                //Debug.LogWarning($"[PlayerSpawner] Raycast found no ground. Using position as-is: {finalSpawnPosition}");
            }
            //Debug.DrawLine(raycastStart, raycastEnd, Color.red, 120f);

        }

        // 5. INSTANTIATE PLAYER PREFAB AT FINAL POSITION
        GameObject spawnedPlayerObj = Instantiate(playerPrefab, finalSpawnPosition, Quaternion.identity);
        SpawnedPlayer = spawnedPlayerObj.transform;

        if (isFreshLevelEntry && SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.PerformAutoSave(SpawnedPlayer);
        }
        
        //FootIKControllerRefactored footIK = spawnedPlayerObj.GetComponentInChildren<FootIKControllerRefactored>();
       //Debug.Log($"[PlayerSpawner] Player instantiated at {finalSpawnPosition}");
        
        // 5.5. UPDATE UI SERVICE PROVIDER WITH NEW PLAYER REFERENCE
        UIServiceProvider uiService = ServiceContainer.Instance.TryGet<UIServiceProvider>();
        if (uiService != null)
        {
            uiService.UpdatePlayerReferences(SpawnedPlayer);
            //Debug.Log("[PlayerSpawner] Updated UIServiceProvider with new player reference");
        }
        
        // 5.6. INITIALIZE PLAYER INVENTORY (after UI is ready)
        var playerController = spawnedPlayerObj.GetComponent<Game.Player.PlayerControllerRefactored>();
        if (playerController != null)
        {
            playerController.InitializeInventory();
            //Debug.Log("[PlayerSpawner] Initialized player inventory");
        }

        // 6. DESTROY THE SPAWN MARKER (no longer needed)
        if (spawnMarkerTransform != null)
        {
            Destroy(spawnMarkerTransform.gameObject);
            //Debug.Log("[PlayerSpawner] Spawn marker destroyed");
        }

        yield return new WaitForSeconds(spawnDelay);
        
        if (loadingScreen != null)
        {
            yield return FadeOutLoadingScreen();
        }

        ServiceContainer.Instance.TryGet<IEventBus>()?.Publish(new PlayMusicEvent("music_gameplay"));
        FindFirstObjectByType<DayNightCycleManager>()?.PlayAmbientForCurrentTime();

        //Debug.Log("[PlayerSpawner] Spawn sequence complete!");
    }
    
    private IEnumerator FadeInLoadingScreen()
    {
        CanvasGroup canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = loadingScreen.AddComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        loadingScreen.SetActive(true);
        
        yield return canvasGroup.DOFade(1f, fadeInDuration).WaitForCompletion();
    }
    
    private IEnumerator FadeOutLoadingScreen()
    {
        CanvasGroup canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = loadingScreen.AddComponent<CanvasGroup>();
        }
        
        yield return canvasGroup.DOFade(0f, fadeOutDuration).WaitForCompletion();
        loadingScreen.SetActive(false);
    }
}