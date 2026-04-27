using UnityEngine;
using Game.Core.DI;
using Game.Interaction;

namespace Game.Player.Inventory
{
    /// <summary>
    /// Spawns a dropped item's world prefab in front of the player.
    /// Used by all drop code paths (InventoryUI, GridInventoryUI, DropItemCommand).
    /// </summary>
    public static class WorldItemSpawner
    {
        private const float ForwardDistance = 1.5f;
        private const float HeightOffset   = 0.5f;
        private const float ThrowImpulse   = 2f;
        private const float GroundRayStartOffset = 2f;
        private const float GroundRayDistance    = 10f;
        private const float GroundClearance      = 0.05f;
        private const float GroundSampleOffset   = 0.6f;
        private const float MinGroundUpDot       = 0.45f;
        private const float ForwardBlockRadius   = 0.25f;
        private const float ForwardBlockDistance = 1.2f;
        private const float WallClearance        = 0.35f;
        private const float FeetFallbackOffset   = 0.2f;

        /// <summary>
        /// Spawns the item's worldPrefab in front of the player.
        /// Player position is resolved from ServiceContainer.
        /// Does nothing (with a warning) if worldPrefab is null.
        /// </summary>
        public static void SpawnDroppedItem(InventoryItem item, int quantity)
        {
            var player = ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();
            if (player == null)
            {
                Debug.LogWarning("[WorldItemSpawner] PlayerControllerRefactored not found in ServiceContainer.");
                return;
            }

            Transform t = player.transform;
            Vector3 spawnPos;
            bool shouldThrow;
            ResolveSpawnPositionWithFallback(t, out spawnPos, out shouldThrow);

            SpawnDroppedItem(item, quantity, spawnPos, shouldThrow ? t.forward : Vector3.zero);
        }

        /// <summary>
        /// Spawns the item's worldPrefab at an explicit position / direction.
        /// Does nothing (with a warning) if worldPrefab is null.
        /// </summary>
        public static void SpawnDroppedItem(InventoryItem item, int quantity, Vector3 position, Vector3 direction)
        {
            if (item == null) return;

            if (item.worldPrefab == null)
            {
                Debug.LogWarning($"[WorldItemSpawner] {item.itemName} has no worldPrefab assigned — item removed but not spawned in world.");
                return;
            }

            Quaternion rotation = direction != Vector3.zero
                ? Quaternion.LookRotation(direction)
                : Quaternion.identity;

            GameObject spawnedGO = Object.Instantiate(item.worldPrefab, position, rotation);

            // Ensure there's an ItemInteractable so the player can pick it back up.
            ItemInteractable interactable = spawnedGO.GetComponent<ItemInteractable>();
            if (interactable == null)
                interactable = spawnedGO.AddComponent<ItemInteractable>();

            interactable.Init(item, quantity);

            // Apply a small forward impulse if a Rigidbody is present.
            Rigidbody rb = spawnedGO.GetComponent<Rigidbody>();
            if (rb != null && direction != Vector3.zero)
                rb.AddForce(direction.normalized * ThrowImpulse, ForceMode.Impulse);
        }

        private static bool TryGetGroundedSpawnPosition(Vector3 desiredPosition, out Vector3 groundedPosition)
        {
            if (TryRaycastGround(desiredPosition, out groundedPosition))
                return true;

            groundedPosition = desiredPosition;
            return false;
        }

        private static void ResolveSpawnPositionWithFallback(Transform playerTransform, out Vector3 spawnPosition, out bool shouldThrow)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.forward;
            else
                flatForward.Normalize();

            Vector3 headHeightOrigin = playerTransform.position + Vector3.up * HeightOffset;
            Vector3 desiredForwardPosition = headHeightOrigin + flatForward * ForwardDistance;

            if (TryAdjustAwayFromWall(headHeightOrigin, flatForward, ref desiredForwardPosition) &&
                TryGetGroundedSpawnPosition(desiredForwardPosition, out spawnPosition))
            {
                shouldThrow = false;
                return;
            }

            if (TryGetGroundedSpawnPosition(desiredForwardPosition, out spawnPosition))
            {
                shouldThrow = true;
                return;
            }

            if (TryGetGroundedSpawnPosition(headHeightOrigin, out spawnPosition))
            {
                shouldThrow = false;
                return;
            }

            Vector3 behindPosition = headHeightOrigin - flatForward * GroundSampleOffset;
            if (TryGetGroundedSpawnPosition(behindPosition, out spawnPosition))
            {
                shouldThrow = false;
                return;
            }

            spawnPosition = playerTransform.position + Vector3.up * FeetFallbackOffset;
            shouldThrow = false;
        }

        private static bool TryRaycastGround(Vector3 desiredPosition, out Vector3 groundedPosition)
        {
            Vector3 rayStart = desiredPosition + Vector3.up * GroundRayStartOffset;
            if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                GroundRayDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) &&
                Vector3.Dot(hit.normal, Vector3.up) >= MinGroundUpDot)
            {
                groundedPosition = hit.point + Vector3.up * GroundClearance;
                return true;
            }

            groundedPosition = desiredPosition;
            return false;
        }

        private static bool TryAdjustAwayFromWall(Vector3 rayOrigin, Vector3 forward, ref Vector3 targetPosition)
        {
            if (Physics.SphereCast(
                rayOrigin,
                ForwardBlockRadius,
                forward,
                out RaycastHit wallHit,
                ForwardBlockDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) &&
                Vector3.Dot(wallHit.normal, Vector3.up) < MinGroundUpDot)
            {
                targetPosition = wallHit.point + wallHit.normal * WallClearance + Vector3.up * HeightOffset;
                return true;
            }

            return false;
        }
    }
}
