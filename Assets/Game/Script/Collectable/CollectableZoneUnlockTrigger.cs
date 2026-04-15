using UnityEngine;
using Game.Core.DI;

namespace Game.Collectable
{
    [RequireComponent(typeof(Collider))]
    public class CollectableZoneUnlockTrigger : MonoBehaviour
    {
        [Header("Collectable")]
        [SerializeField] private CollectableItem collectableToUnlock;
        [SerializeField] private ItemNotificationUI itemNotificationUI;

        [Header("Detection")]
        [SerializeField] private LayerMask playerLayers;

        [Header("Behavior")]
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private bool disableColliderAfterTrigger = true;

        private bool _wasTriggered;

        private void Awake()
        {
            var saveLoadService = SaveLoadService.Instance;
            if (saveLoadService != null)
            {
                int currentLevel = saveLoadService.GetCurrentLevel();
                if (currentLevel != 3)
                {
                    Destroy(gameObject);
                }
            }
        }
        private void Reset()
        {
            var zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_wasTriggered && triggerOnce)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            if (collectableToUnlock == null || string.IsNullOrWhiteSpace(collectableToUnlock.id))
            {
                return;
            }

            var collectableManager = ServiceContainer.Instance.TryGet<ICollectableManager>();
            if (collectableManager == null)
            {
                return;
            }

            if (collectableManager.IsUnlocked(collectableToUnlock.id))
            {
                return;
            }

            collectableManager.Unlock(collectableToUnlock);
            NotifyCollectableUnlocked(collectableToUnlock);
            _wasTriggered = true;

            if (disableColliderAfterTrigger)
            {
                var zoneCollider = GetComponent<Collider>();
                if (zoneCollider != null)
                {
                    zoneCollider.enabled = false;
                }
            }
        }

        private bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            return IsInLayerMask(other.gameObject.layer, playerLayers);
        }

        private static bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        private void NotifyCollectableUnlocked(CollectableItem collectable)
        {
            if (collectable == null)
            {
                return;
            }

            itemNotificationUI ??= ServiceContainer.Instance.TryGet<ItemNotificationUI>();
            if (itemNotificationUI == null)
            {
                return;
            }

            var displayName = string.IsNullOrWhiteSpace(collectable.headerName)
                ? collectable.id
                : collectable.headerName;

            itemNotificationUI.ShowCustomNotification(displayName, collectable.icon, 1, NotificationType.Added);
        }
    }
}
