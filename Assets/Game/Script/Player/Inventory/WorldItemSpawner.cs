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
            Vector3 spawnPos = t.position + t.forward * ForwardDistance + Vector3.up * HeightOffset;
            SpawnDroppedItem(item, quantity, spawnPos, t.forward);
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
    }
}
