using Game.Environment.Tornado;
using UnityEngine;

public class TornadoSpawner : NaturalEventSpawnerBase
{
    [Header("Tornado Settings")]
    [Tooltip("Drag your Tornado Prefab (which has the StartWarningPhase script) here.")]
    public GameObject tornadoPrefab;

    [Tooltip("How far away from the player should the tornado spawn?")]
    public float spawnDistance = 30f;

    protected override void SubscribeToDirector(NaturalEventDirector director)
    {
        director.OnTornadoTriggered += Spawn;
    }

    protected override void UnsubscribeFromDirector(NaturalEventDirector director)
    {
        director.OnTornadoTriggered -= Spawn;
    }

    protected override void SpawnInternal(Transform anchor, WorldLevel triggeredBiome)
    {
        if (tornadoPrefab == null)
        {
            Debug.LogWarning("[TornadoSpawner] Missing Tornado Prefab!");
            return;
        }

        Transform playerTransform = anchor;

        // 1. Calculate a spawn position (e.g., 30 meters behind the player)
        // We use the player's forward vector, reversed, multiplied by the distance.
        Vector3 spawnPosition = playerTransform.position - (playerTransform.forward * spawnDistance);

        // Ensure it spawns at ground level (matching the player's Y, or you can raycast to the terrain)
        spawnPosition.y = playerTransform.position.y;

        // 2. Instantiate the Tornado!
        GameObject newTornado = Instantiate(tornadoPrefab, spawnPosition, Quaternion.identity);

        Debug.Log($"[TornadoSpawner] Tornado spawned at {spawnPosition} for biome {triggeredBiome}!");

        // 3. Find your friend's phase controller on the newly spawned Tornado!
        TornadoPhaseController phaseController = newTornado.GetComponent<TornadoPhaseController>();

        if (phaseController != null)
        {
            phaseController.StartWarningPhase();
        }
        else
        {
            Debug.LogError("[TornadoSpawner] Could not find the TornadoPhaseController on the prefab!");
        }

        TornadoMovement movementScript = newTornado.GetComponent<TornadoMovement>();

        if (movementScript != null)
        {
            // Calculate the direction from the tornado to the player
            Vector3 directionToPlayer = (playerTransform.position - spawnPosition).normalized;

            // Tell the tornado to move that way!
            movementScript.SetStormDirection(directionToPlayer);
        }
    }

    [System.Obsolete("Use Spawn(Transform, WorldLevel) instead.")]
    public void SpawnTornado(Transform playerTransform, WorldLevel triggeredBiome)
    {
        Spawn(playerTransform, triggeredBiome);
    }
}