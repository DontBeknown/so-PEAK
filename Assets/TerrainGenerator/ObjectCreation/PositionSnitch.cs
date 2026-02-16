using Unity.AppUI.UI;
using UnityEngine;

public class PositionSnitch : MonoBehaviour
{
    private Vector3 lastPosition;
    private bool hasSuccessfullyTeleported = false;

    public PlayerSpawner Spawner;
    void Start()
    {
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        // Flatten the coordinates to ignore height (Y axis)
        Vector2 currentXZ = new Vector2(transform.position.x, transform.position.z);

        // 1. Detect the INTENTIONAL warp (Moving away from X:0, Z:0)
        if (!hasSuccessfullyTeleported && distanceMoved > 5f && Vector2.Distance(currentXZ, Vector2.zero) > 5f)
        {
            Debug.Log($"[SNITCH] Legitimate teleport detected. Arrived at {transform.position}. Watching for rubber-banding...");
            hasSuccessfullyTeleported = true;
        }

        // 2. Detect the BUG (Getting yanked back to X:0, Z:0 AFTER the good warp)
        if (hasSuccessfullyTeleported && distanceMoved > 5f && Vector2.Distance(currentXZ, Vector2.zero) < 5f)
        {
            Debug.LogError($" CAUGHT IT! Player yanked back to {transform.position}! Click here for the Stack Trace.");
            transform.position = Spawner.targetSpawnPosition;
        }

        lastPosition = transform.position;
    }
}