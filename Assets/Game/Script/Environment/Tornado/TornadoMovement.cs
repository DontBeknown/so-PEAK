using UnityEngine;

namespace Game.Environment.Tornado
{
    /// <summary>
    /// Moves the tornado using a storm-heading drift plus Perlin-noise wobble.
    /// While moving, the tornado follows valid ground surfaces from a dedicated layer mask.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TornadoPhaseController))]
    public class TornadoMovement : MonoBehaviour
    {
        [Header("Movement Control")]
        [SerializeField] private bool canMove = true;

        [Header("Storm Translation")]
        [SerializeField] private Vector3 stormDirection = Vector3.right;
        [SerializeField, Min(0f)] private float warningPhaseSpeed = 2f;
        [SerializeField, Min(0f)] private float actionPhaseSpeed = 3f;

        [Header("Wobble (Perlin Noise)")]
        [SerializeField, Min(0f)] private float wobbleAmplitude = 1.5f;
        [SerializeField, Min(0f)] private float wobbleFrequency = 0.5f;
        [SerializeField] private float noiseSeedX = 12.731f;
        [SerializeField] private float noiseSeedZ = 48.217f;

        [Header("Terrain Follow")]
        [SerializeField] private LayerMask traversableSurfaceMask = ~0;
        [SerializeField, Min(0.1f)] private float surfaceProbeStartHeight = 25f;
        [SerializeField, Min(0.1f)] private float surfaceProbeDistance = 80f;
        [SerializeField] private float surfaceHeightOffset = 0f;
        [SerializeField] private bool keepLastValidHeightWhenNoHit = true;

        private TornadoPhaseController _phaseController;
        private bool _hasLastValidHeight = false;
        private float _lastValidSurfaceHeight = 0f;

        private void Awake()
        {
            _phaseController = GetComponent<TornadoPhaseController>();
        }

        private void Update()
        {
            if (!ShouldMoveThisFrame())
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 position = transform.position;
            Vector3 planarVelocity = GetBaseStormDirection() * GetCurrentPhaseSpeed();
            planarVelocity += GetNoiseVelocity();

            Vector3 nextPosition = position + (planarVelocity * deltaTime);
            nextPosition = ApplySurfaceFollow(nextPosition);
            transform.position = nextPosition;
        }

        private bool ShouldMoveThisFrame()
        {
            if (!canMove)
            {
                return false;
            }

            if (_phaseController == null)
            {
                return false;
            }

            TornadoPhase phase = _phaseController.CurrentPhase;
            return phase == TornadoPhase.Warning || phase == TornadoPhase.Action;
        }

        private float GetCurrentPhaseSpeed()
        {
            return _phaseController.CurrentPhase == TornadoPhase.Action
                ? actionPhaseSpeed
                : warningPhaseSpeed;
        }

        private Vector3 GetBaseStormDirection()
        {
            Vector3 horizontalDirection = new Vector3(stormDirection.x, 0f, stormDirection.z);
            if (horizontalDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return horizontalDirection.normalized;
        }

        private Vector3 GetNoiseVelocity()
        {
            if (wobbleAmplitude <= 0f || wobbleFrequency <= 0f)
            {
                return Vector3.zero;
            }

            float sampleTime = Time.time * wobbleFrequency;
            float wobbleX = (Mathf.PerlinNoise(noiseSeedX, sampleTime) - 0.5f) * 2f;
            float wobbleZ = (Mathf.PerlinNoise(noiseSeedZ, sampleTime) - 0.5f) * 2f;

            Vector3 noiseVelocity = new Vector3(wobbleX, 0f, wobbleZ) * wobbleAmplitude;
            return noiseVelocity;
        }

        private Vector3 ApplySurfaceFollow(Vector3 targetPosition)
        {
            Vector3 rayOrigin = targetPosition + (Vector3.up * surfaceProbeStartHeight);
            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                surfaceProbeDistance,
                traversableSurfaceMask,
                QueryTriggerInteraction.Ignore))
            {
                float followedHeight = hit.point.y + surfaceHeightOffset;
                _lastValidSurfaceHeight = followedHeight;
                _hasLastValidHeight = true;
                targetPosition.y = followedHeight;
                return targetPosition;
            }

            if (keepLastValidHeightWhenNoHit && _hasLastValidHeight)
            {
                targetPosition.y = _lastValidSurfaceHeight;
            }

            return targetPosition;
        }

        public void SetStormDirection(Vector3 direction)
        {
            stormDirection = direction;
        }
    }
}
