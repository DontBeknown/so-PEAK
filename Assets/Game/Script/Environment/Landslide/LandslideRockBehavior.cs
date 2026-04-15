using UnityEngine;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;
using UnityEngine.Rendering.Universal;

namespace Game.Environment.Landslide
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class LandslideRockBehavior : MonoBehaviour
    {
        private Rigidbody _rb;
        private LandslideRockSpawner _owner;

        private LayerMask _damageLayers;
        private LayerMask _decalSpawnLayers;
        private float _minImpactDamage;
        private float _maxImpactDamage;
        private float _minDamageVelocity;
        private float _maxDamageVelocity;
        private float _damageMultiplier = 1f;
        private float _pushImpulse;
        private float _hitCooldownSeconds;
        private float _recycleAfterSeconds;
        private float _sleepRecycleDelaySeconds;
        private float _decalScaleMultiplier = 1f;
        private float _impactDecalRevealDuration = 0.2f;
        private float _impactDecalHoldDuration = 2f;
        private float _impactDecalFadeDuration = 0.3f;
        private float _impactDecalSpawnDelay = 0f;
        private float _decalSurfaceOffset = 0.02f;
        private string _impactDecalSoundId;
        private float _impactDecalSoundVolumeScale = 1f;
        private float _impactDecalArmedAtTime = 0f;
        private bool _hasSpawnedImpactDecal;

        [Header("Impact Decal")]
        [SerializeField] private GameObject decalProjectorPrefab;
        [SerializeField] private GameObject impactFxPrefab;
        [SerializeField] private Material[] decalMaterials;

        private float _spawnTime;
        private float _lastHitTime;
        private float _sleepingTimer;
        private Vector3 _defaultLocalScale;
        private Tween _recycleTween;
        private bool _isRecycling;
        private IEventBus _eventBus;
        private PlayerStatsTrackerService _statsTracker;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _defaultLocalScale = transform.localScale;
        }

        private void OnEnable()
        {
            _recycleTween?.Kill();
            _recycleTween = null;
            _isRecycling = false;
            _hasSpawnedImpactDecal = false;
            transform.localScale = _defaultLocalScale;

            _spawnTime = Time.time;
            _lastHitTime = -999f;
            _sleepingTimer = 0f;
            _rb.WakeUp();
        }

        private void Update()
        {
            if (Time.time - _spawnTime >= _recycleAfterSeconds)
            {
                RequestRecycle();
                return;
            }

            if (_rb.IsSleeping())
            {
                _sleepingTimer += Time.deltaTime;
                if (_sleepingTimer >= _sleepRecycleDelaySeconds)
                {
                    RequestRecycle();
                }
            }
            else
            {
                _sleepingTimer = 0f;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!TryGetCollisionPermissions(collision, out bool canDamage, out bool canSpawnDecal))
            {
                return;
            }

            if (Time.time - _lastHitTime < _hitCooldownSeconds)
            {
                return;
            }

            _lastHitTime = Time.time;

            // Spawn decal on first valid collision even if damage resolves to zero.
            if (canSpawnDecal)
            {
                TrySpawnImpactCrackDecal(collision);
            }

            if (canDamage)
            {
                HandleDamageCollision(collision);
                HandlePushCollision(collision);
            }
        }

        private bool TryGetCollisionPermissions(Collision collision, out bool canDamage, out bool canSpawnDecal)
        {
            canDamage = IsInLayerMask(collision.gameObject.layer, _damageLayers);
            canSpawnDecal = IsInLayerMask(collision.gameObject.layer, _decalSpawnLayers);
            return canDamage || canSpawnDecal;
        }

        private void HandleDamageCollision(Collision collision)
        {
            float impactDamage = EvaluateImpactDamage(_rb.linearVelocity.magnitude);
            if (impactDamage <= 0f)
            {
                return;
            }

            PlayerStats playerStats = collision.collider.GetComponentInParent<PlayerStats>();
            if (playerStats == null)
            {
                Debug.LogWarning($"[LandslideRockBehavior] Damage collision on '{collision.collider.name}' matched damage layers but no PlayerStats receiver was found.");
                return;
            }

            playerStats.TakeDamage(impactDamage, DeathCause.LandslideRock);
            RegisterEncounteredRiskEvent(collision, impactDamage);
        }

        private void HandlePushCollision(Collision collision)
        {
            if (_pushImpulse <= 0f)
            {
                return;
            }

            Vector3 pushDirection = _rb.linearVelocity.sqrMagnitude > 0.01f
                ? _rb.linearVelocity.normalized
                : transform.forward;

            if (collision.rigidbody != null)
            {
                collision.rigidbody.AddForce(pushDirection * _pushImpulse, ForceMode.Impulse);
                return;
            }

            CharacterController controller = collision.collider.GetComponentInParent<CharacterController>();
            if (controller == null || !controller.enabled)
            {
                return;
            }

            // Spread push over a short duration to avoid a snappy one-frame displacement.
            CharacterControllerImpactPush push = controller.GetComponent<CharacterControllerImpactPush>();
            if (push == null)
            {
                push = controller.gameObject.AddComponent<CharacterControllerImpactPush>();
            }

            push.ApplyImpulse(pushDirection * _pushImpulse);
        }

        public void Configure(LandslideRockSpawner owner, LandslideRockBehaviorConfig config)
        {
            _owner = owner;
            _damageLayers = config.DamageLayers;
            _decalSpawnLayers = config.DecalSpawnLayers;
            _minImpactDamage = Mathf.Max(0f, config.MinImpactDamage);
            _maxImpactDamage = Mathf.Max(_minImpactDamage, config.MaxImpactDamage);
            _minDamageVelocity = Mathf.Max(0f, config.MinDamageVelocity);
            _maxDamageVelocity = Mathf.Max(_minDamageVelocity, config.MaxDamageVelocity);
            _damageMultiplier = Mathf.Max(0f, config.DamageMultiplier);
            _decalScaleMultiplier = Mathf.Max(0.01f, config.DecalScaleMultiplier);
            if (config.DecalProjectorPrefab != null)
            {
                decalProjectorPrefab = config.DecalProjectorPrefab;
            }

            if (config.ImpactFxPrefab != null)
            {
                impactFxPrefab = config.ImpactFxPrefab;
            }

            if (config.DecalMaterials != null && config.DecalMaterials.Length > 0)
            {
                decalMaterials = config.DecalMaterials;
            }

            _impactDecalRevealDuration = Mathf.Max(0.01f, config.ImpactDecalRevealDuration);
            _impactDecalHoldDuration = Mathf.Max(0f, config.ImpactDecalHoldDuration);
            _impactDecalFadeDuration = Mathf.Max(0.01f, config.ImpactDecalFadeDuration);
            _impactDecalSpawnDelay = Mathf.Max(0f, config.ImpactDecalSpawnDelay);
            _impactDecalSoundId = config.ImpactDecalSoundId;
            _impactDecalSoundVolumeScale = Mathf.Max(0f, config.ImpactDecalSoundVolumeScale);
            _decalSurfaceOffset = Mathf.Max(0f, config.DecalSurfaceOffset);

            _pushImpulse = Mathf.Max(0f, config.PushImpulse);
            _hitCooldownSeconds = Mathf.Max(0f, config.HitCooldownSeconds);
            _recycleAfterSeconds = Mathf.Max(1f, config.RecycleAfterSeconds);
            _sleepRecycleDelaySeconds = Mathf.Max(0f, config.SleepRecycleDelaySeconds);
        }

        private float EvaluateImpactDamage(float speed)
        {
            if (_maxImpactDamage <= 0f)
            {
                return 0f;
            }

            // Prevent stationary/very slow rocks from dealing damage.
            if (speed < _minDamageVelocity)
            {
                return 0f;
            }

            if (_maxDamageVelocity <= _minDamageVelocity)
            {
                return _maxImpactDamage;
            }

            float t = Mathf.InverseLerp(_minDamageVelocity, _maxDamageVelocity, speed);
            return Mathf.Lerp(_minImpactDamage, _maxImpactDamage, t) * _damageMultiplier;
        }

        public void Launch(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            _spawnTime = Time.time;
            _sleepingTimer = 0f;
            _lastHitTime = -999f;
            _hasSpawnedImpactDecal = false;
            _impactDecalArmedAtTime = _spawnTime + _impactDecalSpawnDelay;

            _rb.linearVelocity = linearVelocity;
            _rb.angularVelocity = angularVelocity;
            _rb.WakeUp();
        }

        public void ResetForPool()
        {
            _recycleTween?.Kill();
            _recycleTween = null;
            _isRecycling = false;
            _hasSpawnedImpactDecal = false;
            transform.localScale = _defaultLocalScale;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();
        }

        public void PlayRecycleScaleDown(float duration, System.Action onComplete)
        {
            if (_isRecycling)
            {
                return;
            }

            _isRecycling = true;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();

            _recycleTween?.Kill();
            _recycleTween = transform
                .DOScale(Vector3.zero, Mathf.Max(0.01f, duration))
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _isRecycling = false;
                    onComplete?.Invoke();
                });
        }

        private void RequestRecycle()
        {
            _owner?.RecycleRock(this);
        }

        private void TrySpawnImpactCrackDecal(Collision collision)
        {
            if (_hasSpawnedImpactDecal || decalProjectorPrefab == null || collision.contactCount <= 0)
            {
                return;
            }

            if (Time.time < _impactDecalArmedAtTime)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            Vector3 decalPosition = contact.point + contact.normal * _decalSurfaceOffset;
            TrySpawnImpactFx(decalPosition, contact.normal);
            Quaternion spawnRotation = Quaternion.LookRotation(contact.normal);
            GameObject decalGO = _owner != null
                ? _owner.RentDecal(decalProjectorPrefab, decalPosition, spawnRotation)
                : Instantiate(decalProjectorPrefab, decalPosition, spawnRotation);

            if (decalGO == null)
            {
                return;
            }

            DecalProjector projector = decalGO.GetComponent<DecalProjector>();
            if (projector == null)
            {
                projector = decalGO.GetComponentInChildren<DecalProjector>();
            }

            if (projector != null)
            {
                projector.transform.forward = -contact.normal;
            }

            Material randomMaterial = GetRandomDecalMaterial();
            if (projector != null && randomMaterial != null)
            {
                projector.material = randomMaterial;
            }

            if (projector != null)
            {
                float uniformBaseSize = Mathf.Max(projector.size.x, projector.size.y);
                float targetSize = Mathf.Max(0.01f, uniformBaseSize * _decalScaleMultiplier);

                SetProjectorWidth(projector, 0.01f);
                SetProjectorHeight(projector, 0.01f);

                DOTween.Sequence()
                    .SetUpdate(true)
                    .Append(DOTween.To(() => projector.size.x, value => SetProjectorWidth(projector, value), targetSize, _impactDecalRevealDuration).SetEase(Ease.OutBack))
                    .Join(DOTween.To(() => projector.size.y, value => SetProjectorHeight(projector, value), targetSize, _impactDecalRevealDuration).SetEase(Ease.OutBack));

                if (_owner != null)
                {
                    _owner.RegisterSpawnedDecal(decalGO, _impactDecalFadeDuration);
                }
                else
                {
                    Destroy(decalGO, GetStandaloneImpactVisualLifetime());
                }

                PublishImpactDecalSound(decalPosition);
            }
            else
            {
                if (_owner != null)
                {
                    _owner.ReturnDecal(decalGO);
                }
                else
                {
                    Destroy(decalGO);
                }
                return;
            }
            
            _hasSpawnedImpactDecal = true;
        }

        private void PublishImpactDecalSound(Vector3 position)
        {
            if (string.IsNullOrWhiteSpace(_impactDecalSoundId) || _impactDecalSoundVolumeScale <= 0f)
            {
                return;
            }

            if (_owner != null)
            {
                _owner.PublishPositionalSound(_impactDecalSoundId, position, _impactDecalSoundVolumeScale);
                return;
            }

            ResolveEventBus();
            _eventBus?.Publish(new PlayPositionalSFXEvent(_impactDecalSoundId, position, _impactDecalSoundVolumeScale));
        }

        private void ResolveEventBus()
        {
            _eventBus ??= ServiceContainer.Instance?.TryGet<IEventBus>();
        }

        private void ResolveStatsTracker()
        {
            _statsTracker ??= ServiceContainer.Instance?.TryGet<PlayerStatsTrackerService>();
        }

        private void RegisterEncounteredRiskEvent(Collision collision, float impactDamage)
        {
            ResolveStatsTracker();
            if (_statsTracker == null)
            {
                return;
            }

            if (_owner != null && !_owner.TryReserveRockfallEncounterEvent())
            {
                return;
            }

            Vector3 eventPosition = collision.contactCount > 0
                ? collision.GetContact(0).point
                : collision.transform.position;

            RiskEvent riskEvent = new RiskEvent
            {
                riskType = RiskType.Rockfall,
                location = eventPosition,
                timestamp = Time.time,
                wasEncountered = true,
                severity = EvaluateEncounterSeverity(impactDamage)
            };

            _statsTracker.RegisterRiskEvent(riskEvent);
        }

        private float EvaluateEncounterSeverity(float impactDamage)
        {
            if (_maxImpactDamage <= 0f)
            {
                return 0f;
            }

            if (_maxImpactDamage <= _minImpactDamage)
            {
                return impactDamage > 0f ? 1f : 0f;
            }

            return Mathf.Clamp01(Mathf.InverseLerp(_minImpactDamage, _maxImpactDamage, impactDamage));
        }

        private static void SetProjectorWidth(DecalProjector projector, float width)
        {
            if (projector == null)
            {
                return;
            }

            Vector3 size = projector.size;
            size.x = Mathf.Max(0.01f, width);
            projector.size = size;
        }

        private static void SetProjectorHeight(DecalProjector projector, float height)
        {
            if (projector == null)
            {
                return;
            }

            Vector3 size = projector.size;
            size.y = Mathf.Max(0.01f, height);
            projector.size = size;
        }

        private Material GetRandomDecalMaterial()
        {
            if (decalMaterials == null || decalMaterials.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            for (int i = 0; i < decalMaterials.Length; i++)
            {
                if (decalMaterials[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                return null;
            }

            int selectedValid = Random.Range(0, validCount);
            int runningValid = 0;
            for (int i = 0; i < decalMaterials.Length; i++)
            {
                Material material = decalMaterials[i];
                if (material == null)
                {
                    continue;
                }

                if (runningValid == selectedValid)
                {
                    return material;
                }

                runningValid++;
            }

            return null;
        }

        private void TrySpawnImpactFx(Vector3 position, Vector3 surfaceNormal)
        {
            if (impactFxPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            GameObject fx = Instantiate(impactFxPrefab, position, rotation);

            ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);
            float multiplier = Mathf.Max(0.01f, _decalScaleMultiplier);

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                ScaleStartSize(system, multiplier);
                ScaleBurstCount(system, multiplier);
                ScaleVelocityOverLifetime(system, multiplier);
                system.Play(true);
            }

            if (_owner != null)
            {
                _owner.RegisterSpawnedDecal(fx, _impactDecalFadeDuration);
            }
            else
            {
                Destroy(fx, GetStandaloneImpactVisualLifetime());
            }
        }

        private float GetStandaloneImpactVisualLifetime()
        {
            return Mathf.Max(0.01f, _impactDecalRevealDuration + _impactDecalHoldDuration + _impactDecalFadeDuration);
        }

        private static void ScaleBurstCount(ParticleSystem system, float multiplier)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            int count = emission.burstCount;
            if (count <= 0)
            {
                return;
            }

            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[count];
            emission.GetBursts(bursts);

            for (int i = 0; i < bursts.Length; i++)
            {
                ParticleSystem.MinMaxCurve burstCountCurve = bursts[i].count;
                bursts[i].count = ScaleCurve(burstCountCurve, multiplier, minConstant: 1f);
            }

            emission.SetBursts(bursts, bursts.Length);
        }

        private static void ScaleVelocityOverLifetime(ParticleSystem system, float multiplier)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            if (!velocity.enabled)
            {
                return;
            }

            velocity.x = ScaleCurve(velocity.x, multiplier, minConstant: 0f);
            velocity.y = ScaleCurve(velocity.y, multiplier, minConstant: 0f);
            velocity.z = ScaleCurve(velocity.z, multiplier, minConstant: 0f);
            velocity.orbitalX = ScaleCurve(velocity.orbitalX, multiplier, minConstant: 0f);
            velocity.orbitalY = ScaleCurve(velocity.orbitalY, multiplier, minConstant: 0f);
            velocity.orbitalZ = ScaleCurve(velocity.orbitalZ, multiplier, minConstant: 0f);
            velocity.radial = ScaleCurve(velocity.radial, multiplier, minConstant: 0f);
            velocity.speedModifier = ScaleCurve(velocity.speedModifier, multiplier, minConstant: 0f);
        }

        private static void ScaleStartSize(ParticleSystem system, float multiplier)
        {
            ParticleSystem.MainModule main = system.main;
            main.startSize = ScaleCurve(main.startSize, multiplier, minConstant: 0.01f);
        }

        private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float multiplier, float minConstant)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curve.constant = Mathf.Max(minConstant, curve.constant * multiplier);
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curve.constantMin = Mathf.Max(minConstant, curve.constantMin * multiplier);
                    curve.constantMax = Mathf.Max(curve.constantMin, curve.constantMax * multiplier);
                    break;
                case ParticleSystemCurveMode.Curve:
                case ParticleSystemCurveMode.TwoCurves:
                    curve.curveMultiplier *= multiplier;
                    break;
            }

            return curve;
        }

        private static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private sealed class CharacterControllerImpactPush : MonoBehaviour
        {
            private CharacterController _controller;
            private Vector3 _velocity;

            // Larger values decay the impulse faster; tuned for a quick but smooth shove.
            private const float Damping = 12f;
            private const float MinVelocitySqr = 0.0001f;

            private void Awake()
            {
                _controller = GetComponent<CharacterController>();
            }

            public void ApplyImpulse(Vector3 impulse)
            {
                _velocity += impulse;
            }

            private void Update()
            {
                if (_controller == null || !_controller.enabled)
                {
                    return;
                }

                if (_velocity.sqrMagnitude <= MinVelocitySqr)
                {
                    _velocity = Vector3.zero;
                    return;
                }

                _controller.Move(_velocity * Time.deltaTime);
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, Damping * Time.deltaTime);
            }
        }
    }
}
