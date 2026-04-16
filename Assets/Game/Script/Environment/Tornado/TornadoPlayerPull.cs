using System.Collections.Generic;
using Game.Core.DI;
using UnityEngine;

namespace Game.Environment.Tornado
{
    /// <summary>
    /// Handles pulling players toward the tornado center and applying distance-based damage.
    /// Requires a TornadoPhaseController on the same GameObject to manage phase transitions.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(TornadoPhaseController))]
    public class TornadoPlayerPull : MonoBehaviour
    {
        #region Serialized Fields - Pull
        [Header("Pull Settings")]
        [SerializeField] private LayerMask playerLayerMask;
        [SerializeField, Min(0f)] private float minPullSpeed = 1f;
        [SerializeField, Min(0f)] private float pullSpeed = 8f;
        [SerializeField, Min(0f)] private float upwardSpeed = 2.5f;
        [SerializeField, Min(0f)] private float stopDistance = 1.25f;
        [SerializeField, Min(0f)] private float maxInfluenceDistance = 15f;
        #endregion

        #region Serialized Fields - Damage
        [Header("Damage Settings")]
        [SerializeField, Min(0.1f)] private float damageIntervalSeconds = 2.5f;
        [SerializeField, Min(0f)] private float damageRangeMin = 1f;
        [SerializeField, Min(0f)] private float damageRangeMax = 10f;
        [SerializeField, Range(0f, 1f)] private float tiedStateDamageMultiplier = 0.5f;
        #endregion

        #region Runtime State
        private readonly List<Game.Player.PlayerControllerRefactored> _playersInRange = new List<Game.Player.PlayerControllerRefactored>();
        private readonly Dictionary<Game.Player.PlayerControllerRefactored, float> _lastDamageTime = new Dictionary<Game.Player.PlayerControllerRefactored, float>();

        private TornadoPhaseController _phaseController;
        private PlayerStats _playerStats;

        private PlayerStats PlayerStats
        {
            get
            {
                if (_playerStats == null)
                {
                    _playerStats = ServiceContainer.Instance?.TryGet<PlayerStats>();
                }

                return _playerStats;
            }
        }
        #endregion

        private void Awake()
        {
            // Ensure collider is a trigger
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
            }

            // Get references
            _phaseController = GetComponent<TornadoPhaseController>();
            if (_phaseController == null)
            {
                Debug.LogError("[TornadoPlayerPull] TornadoPhaseController not found on same GameObject!", gameObject);
            }

            _ = PlayerStats;
        }

        private void Reset()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryRegisterPlayer(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryRegisterPlayer(other);
        }

        private void OnTriggerExit(Collider other)
        {
            Game.Player.PlayerControllerRefactored playerController = other.GetComponentInParent<Game.Player.PlayerControllerRefactored>();
            if (playerController == null)
            {
                return;
            }

            _playersInRange.Remove(playerController);
            _lastDamageTime.Remove(playerController);
            if (_playersInRange.Count == 0)
            {
                _phaseController?.SetActionShakeActive(false);
            }

            TornadoProximityFeedback tornadoFeedback = playerController.GetComponentInChildren<TornadoProximityFeedback>();
            tornadoFeedback?.ClearTornadoDistance();
        }

        private void FixedUpdate()
        {
            // Only pull and damage during action phase
            if (_phaseController == null || _phaseController.CurrentPhase != TornadoPhase.Action)
            {
                _phaseController?.SetActionShakeActive(false);
                ClearTrackedPlayersFeedback();
                return;
            }

            if (_playersInRange.Count == 0)
            {
                _phaseController.SetActionShakeActive(false);
                return;
            }

            _phaseController.SetActionShakeActive(true);

            for (int i = _playersInRange.Count - 1; i >= 0; i--)
            {
                Game.Player.PlayerControllerRefactored playerController = _playersInRange[i];
                if (playerController == null || !playerController.isActiveAndEnabled)
                {
                    _playersInRange.RemoveAt(i);
                    _lastDamageTime.Remove(playerController);
                    continue;
                }

                PullTowardsTornado(playerController);
                ApplyDamage(playerController);
            }
        }

        private void ClearTrackedPlayersFeedback()
        {
            for (int i = _playersInRange.Count - 1; i >= 0; i--)
            {
                Game.Player.PlayerControllerRefactored playerController = _playersInRange[i];
                if (playerController == null)
                {
                    continue;
                }

                TornadoProximityFeedback tornadoFeedback = playerController.GetComponentInChildren<TornadoProximityFeedback>();
                tornadoFeedback?.ClearTornadoDistance();
            }

            _playersInRange.Clear();
            _lastDamageTime.Clear();
        }

        private void TryRegisterPlayer(Collider other)
        {
            int otherLayerMask = 1 << other.gameObject.layer;
            if ((playerLayerMask.value & otherLayerMask) == 0)
            {
                return;
            }

            Game.Player.PlayerControllerRefactored playerController = other.GetComponentInParent<Game.Player.PlayerControllerRefactored>();
            if (playerController == null || !playerController.isActiveAndEnabled)
            {
                return;
            }

            if (!_playersInRange.Contains(playerController))
            {
                _playersInRange.Add(playerController);
                if (_phaseController != null && _phaseController.CurrentPhase == TornadoPhase.Action)
                {
                    _phaseController.SetActionShakeActive(true);
                }
            }
        }

        #region Pull Logic

        private void PullTowardsTornado(Game.Player.PlayerControllerRefactored playerController)
        {
            TornadoProximityFeedback tornadoFeedback = playerController.GetComponentInChildren<TornadoProximityFeedback>();
            CharacterController controller = playerController.GetComponent<CharacterController>();
            if (controller == null || !controller.enabled)
            {
                return;
            }

            Vector3 controllerCenter = controller.transform.TransformPoint(controller.center);
            Vector3 tornadoCenter = transform.position;
            Vector3 toCenter = tornadoCenter - controllerCenter;

            float horizontalDistance = new Vector2(toCenter.x, toCenter.z).magnitude;
            if (horizontalDistance > maxInfluenceDistance)
            {
                tornadoFeedback?.ClearTornadoDistance();
                return;
            }

            Vector3 horizontalDirection = new Vector3(toCenter.x, 0f, toCenter.z);
            Vector3 move = Vector3.zero;

            if (horizontalDistance > stopDistance && horizontalDirection.sqrMagnitude > 0.0001f)
            {
                float distancePullT = Mathf.InverseLerp(maxInfluenceDistance, stopDistance, horizontalDistance);
                float distanceBasedPullSpeed = Mathf.Lerp(minPullSpeed, pullSpeed, distancePullT);
                move += horizontalDirection.normalized * distanceBasedPullSpeed;
            }

            float verticalT = maxInfluenceDistance <= 0.0001f
                ? 1f
                : Mathf.Clamp01(1f - (horizontalDistance / maxInfluenceDistance));
            move += Vector3.up * upwardSpeed * verticalT;

            playerController.AddExternalVelocity(move);
            tornadoFeedback?.SetTornadoDistance(horizontalDistance);
        }

        #endregion

        #region Damage Logic

        private void ApplyDamage(Game.Player.PlayerControllerRefactored playerController)
        {
            PlayerStats resolvedPlayerStats = PlayerStats;
            if (resolvedPlayerStats == null || playerController == null)
            {
                return;
            }

            // Track last damage time for this player
            if (!_lastDamageTime.ContainsKey(playerController))
            {
                _lastDamageTime[playerController] = Time.time;
                return;
            }

            // Check if enough time has passed since last damage
            float timeSinceLastDamage = Time.time - _lastDamageTime[playerController];
            if (timeSinceLastDamage < damageIntervalSeconds)
            {
                return;
            }

            // Calculate distance-based damage
            CharacterController controller = playerController.GetComponent<CharacterController>();
            if (controller == null)
            {
                return;
            }

            Vector3 controllerCenter = controller.transform.TransformPoint(controller.center);
            Vector3 tornadoCenter = transform.position;
            float horizontalDistance = new Vector2(tornadoCenter.x - controllerCenter.x, tornadoCenter.z - controllerCenter.z).magnitude;

            // Map distance to damage: far (maxInfluenceDistance) = damageRangeMin, close (stopDistance) = damageRangeMax
            float normalizedDistance = Mathf.Clamp01((horizontalDistance - stopDistance) / (maxInfluenceDistance - stopDistance));
            float damage = Mathf.Lerp(damageRangeMax, damageRangeMin, normalizedDistance);

            // Reduce damage if player is tied
            if (playerController.GetCurrentState() is TiedState)
            {
                damage *= tiedStateDamageMultiplier;
            }

            // Apply damage to player
            resolvedPlayerStats.TakeDamage(damage, DeathCause.Tornado);
            float severity = damageRangeMax > 0f ? Mathf.Clamp01(damage / damageRangeMax) : 0f;
            _phaseController?.RegisterDamageEncounter(controllerCenter, severity);

            // Update last damage time
            _lastDamageTime[playerController] = Time.time;
        }

        #endregion
    }
}
