using System;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Coordinates")]
    public Vector3 targetSpawnPosition;

    [Header("Player References")]
    public Transform playerTransform;

    [SerializeField] private bool loadFromSave = true;
    
    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight = 100f; // Height above position to start raycast
    [SerializeField] private float raycastDistance = 200f; // Max distance to raycast down
    [SerializeField] private LayerMask groundLayers = -1; // Layers to check for ground (default: everything)
    
    public void TeleportToSpawn()
    {
        if (playerTransform == null)
        {
            Debug.LogError("[PlayerSpawner] Player Transform is not assigned!");
            return;
        }

        // 1. GET THE CONTROLLER
        CharacterController cc = playerTransform.GetComponent<CharacterController>();

        // 2. DISABLE IT (Sedate the patient)
        if (cc != null) cc.enabled = false;

        
        if(!SaveLoadService.Instance.IsNewWorld() && loadFromSave)
        {
           WorldSaveData saveData = SaveLoadService.Instance.CurrentWorldSave;
           Vector3 savedXZ = new Vector3(saveData.playerData.position[0], 0f, saveData.playerData.position[2]);
           
           // Raycast down from above the saved position to find ground
           Vector3 raycastStart = new Vector3(savedXZ.x, saveData.playerData.position[1] + raycastHeight, savedXZ.z);
           RaycastHit hit;
           
           if (Physics.Raycast(raycastStart, Vector3.down, out hit, raycastDistance, groundLayers))
           {
               // Found ground - use hit point Y position
               targetSpawnPosition = new Vector3(savedXZ.x, hit.point.y, savedXZ.z);
               Debug.Log($"Loaded player position from save with raycast: {targetSpawnPosition} (hit: {hit.collider.name})");
           }
           else
           {
               // No ground found - use default Y position with saved XZ
               targetSpawnPosition = new Vector3(savedXZ.x, targetSpawnPosition.y, savedXZ.z);
               Debug.LogWarning($"Raycast found no ground at saved position. Using default Y: {targetSpawnPosition}");
           }
        }
        else
        {
            Debug.Log($"No saved player position found. Using default spawn: {targetSpawnPosition}");
        }

        // 3. MOVE THE TRANSFORM (Perform the surgery)
        playerTransform.position = targetSpawnPosition;

        // 4. RE-ENABLE IT (Wake the patient up)
        if (cc != null) cc.enabled = true;

        //Debug.Log($"Teleported to {targetSpawnPosition}. Controller was temporarily disabled.");
    }
}