using System;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Coordinates")]
    public Vector3 targetSpawnPosition;

    [Header("Player References")]
    public Transform playerTransform;

    [SerializeField] private bool loadFromSave = true;
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
           targetSpawnPosition = new Vector3(saveData.playerData.position[0], saveData.playerData.position[1] + 10, saveData.playerData.position[2]);
           Debug.Log($"Loaded player position from save: {targetSpawnPosition}");
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