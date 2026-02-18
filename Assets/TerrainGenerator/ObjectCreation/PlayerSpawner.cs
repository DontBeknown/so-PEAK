using System;
using System.Collections;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Coordinates")]
    public Vector3 targetSpawnPosition;

    [Header("Player References")]
    public Transform playerTransform;
    [SerializeField] private FootIKControllerRefactored footIKController;

    [Header("UI References")]
    [SerializeField] private GameObject loadingScreen; 

    [SerializeField] private bool loadFromSave = true;
    
    [Header("Raycast Settings")]
    [SerializeField] private float raycastHeight = 100f; // Height above position to start raycast
    [SerializeField] private float raycastDistance = 200f; // Max distance to raycast down
    [SerializeField] private LayerMask groundLayers = -1; // Layers to check for ground (default: everything)
    [SerializeField] private float spawnDelay = 0.5f; // Delay in seconds before teleporting (to let terrain load)
    [SerializeField] private float spawnHeightOffset = 10f; // Small offset to prevent ground clipping
    
    public void TeleportToSpawn()
    {
        StartCoroutine(TeleportToSpawnDelayed());
    }
    
    private System.Collections.IEnumerator TeleportToSpawnDelayed()
    {
        if (playerTransform == null)
        {
            //Debug.LogError("[PlayerSpawner] Player Transform is not assigned!");
            yield break;
        }

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Disable FootIK during spawn to prevent pelvis adjustment interference
        if (footIKController != null)
        {
            footIKController.SetFootIKEnabled(false);
        }

        // 1. GET THE CONTROLLER
        CharacterController cc = playerTransform.GetComponent<CharacterController>();

        // 2. DISABLE IT (Sedate the patient)
        if (cc != null) cc.enabled = false;

        // 3. DETERMINE TARGET XZ POSITION (without proper Y yet)
        Vector3 targetXZ;
        float savedY = targetSpawnPosition.y; // Store default Y
        
        if(!SaveLoadService.Instance.IsNewWorld() && loadFromSave)
        {
           WorldSaveData saveData = SaveLoadService.Instance.CurrentWorldSave;
           targetXZ = new Vector3(saveData.playerData.position[0], saveData.playerData.position[1], saveData.playerData.position[2]);
           //Debug.Log($"[Saved Spawn] Using saved XZ position: {targetXZ}");
        }
        else
        {
            targetXZ = targetSpawnPosition;
            //Debug.Log($"[Default Spawn] Using default spawn position: {targetXZ}");
        }

        // 4. MOVE PLAYER TO TARGET XZ FIRST (to trigger chunk generation)
        playerTransform.position = targetXZ;
        //Debug.Log($"[Step 1] Moved player to target position to trigger chunk generation: {targetXZ}");

        // 5. WAIT FOR CHUNKS TO GENERATE AND MESH COLLIDERS TO BAKE
        yield return new WaitForSeconds(spawnDelay);

        // 6. NOW DO THE RAYCAST TO FIND GROUND
        Vector3 raycastStart = new Vector3(targetXZ.x, targetXZ.y + raycastHeight, targetXZ.z);
        Vector3 raycastEnd = raycastStart + Vector3.down * raycastDistance;
        RaycastHit hit;
        
        //Debug.Log($"[Step 2] Raycast Start: {raycastStart}, End: {raycastEnd}, Distance: {raycastDistance}, LayerMask: {groundLayers.value}");
        
        if (Physics.Raycast(raycastStart, Vector3.down, out hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            // Found ground - use hit point Y position with small offset
            targetSpawnPosition = new Vector3(targetXZ.x, hit.point.y + spawnHeightOffset, targetXZ.z);
            //Debug.Log($"[Step 2] Found ground with raycast: {targetSpawnPosition} (hit: {hit.collider.name}, distance: {hit.distance})");
            //Debug.DrawLine(raycastStart, hit.point, Color.green, 120f);
        }
        else
        {
            // No ground found - keep current Y
            targetSpawnPosition = targetXZ;
            //Debug.LogWarning($"[Step 2] Raycast found no ground. Keeping current position: {targetSpawnPosition}");
            //Debug.DrawLine(raycastStart, raycastEnd, Color.red, 120f);
        }

        // 7. ADJUST PLAYER Y POSITION TO GROUND
        playerTransform.position = targetSpawnPosition;
        //Debug.Log($"[Step 3] Final player position: {targetSpawnPosition}");

        // 8. RE-ENABLE IT (Wake the patient up)
        if (cc != null) cc.enabled = true;

        // Re-enable FootIK after spawn is complete
        if (footIKController != null)
        {
            footIKController.SetFootIKEnabled(true);
        }

        yield return new WaitForSeconds(spawnDelay);
        
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
}