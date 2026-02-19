using System;
using System.Collections;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Coordinates")]
    public Vector3 targetSpawnPosition;

    [Header("Player References")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnMarkerTransform;

    [Header("UI References")]
    [SerializeField] private GameObject loadingScreen; 

    [SerializeField] private bool loadFromSave = true;
    
    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight = 100f; // Height above position to start raycast
    [SerializeField] private float raycastDistance = 200f; // Max distance to raycast down
    [SerializeField] private LayerMask groundLayers = -1; // Layers to check for ground (default: everything)
    [SerializeField] private float spawnDelay = 0.5f; // Delay in seconds before spawning (to let terrain load)
    [SerializeField] private float spawnHeightOffset = 10f; // Small offset to prevent ground clipping
    
    // Stores the spawned player reference after successful spawn
    public Transform SpawnedPlayer { get; private set; }
    
    public IEnumerator SpawnPlayer()
    {
        return SpawnPlayerDelayed();
    }
    
    private System.Collections.IEnumerator SpawnPlayerDelayed()
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
            loadingScreen.SetActive(true);
        }

        // 1. DETERMINE TARGET XZ POSITION (without proper Y yet)
        Vector3 targetXZ;
        float savedY = targetSpawnPosition.y; // Store default Y
        
        if(!SaveLoadService.Instance.IsNewWorld() && loadFromSave)
        {
           WorldSaveData saveData = SaveLoadService.Instance.CurrentWorldSave;
           targetXZ = new Vector3(saveData.playerData.position[0], saveData.playerData.position[1], saveData.playerData.position[2]);
           //Debug.Log($"[PlayerSpawner] Using saved position: {targetXZ}");
        }
        else
        {
            targetXZ = targetSpawnPosition;
            //Debug.Log($"[PlayerSpawner] Using default spawn position: {targetXZ}");
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
            // No ground found - use target position as-is
            finalSpawnPosition = targetXZ;
            //Debug.LogWarning($"[PlayerSpawner] Raycast found no ground. Using position as-is: {finalSpawnPosition}");
            //Debug.DrawLine(raycastStart, raycastEnd, Color.red, 120f);
        }

        // 5. INSTANTIATE PLAYER PREFAB AT FINAL POSITION
        GameObject spawnedPlayerObj = Instantiate(playerPrefab, finalSpawnPosition, Quaternion.identity);
        SpawnedPlayer = spawnedPlayerObj.transform;
        
        //Debug.Log($"[PlayerSpawner] Player instantiated at {finalSpawnPosition}");

        // 6. DESTROY THE SPAWN MARKER (no longer needed)
        if (spawnMarkerTransform != null)
        {
            Destroy(spawnMarkerTransform.gameObject);
            //Debug.Log("[PlayerSpawner] Spawn marker destroyed");
        }

        yield return new WaitForSeconds(spawnDelay);
        
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }

        //Debug.Log("[PlayerSpawner] Spawn sequence complete!");
    }
}