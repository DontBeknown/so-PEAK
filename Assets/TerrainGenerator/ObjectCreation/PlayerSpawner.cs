using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Coordinates")]
    public Vector3 targetSpawnPosition;

    [Header("Player References")]
    public Transform playerTransform;

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

        // 3. MOVE THE TRANSFORM (Perform the surgery)
        playerTransform.position = targetSpawnPosition;

        // 4. RE-ENABLE IT (Wake the patient up)
        if (cc != null) cc.enabled = true;

        Debug.Log($"Teleported to {targetSpawnPosition}. Controller was temporarily disabled.");
    }
}