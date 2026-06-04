using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core.DI;
using Game.Core.Events;
using Game.Sound.Events;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Environment.Landslide
{
    [DisallowMultipleComponent]
    public class LandslideRockSpawner : NaturalEventSpawnerBase
    {
        #region Nested Types
        [System.Serializable]
        private class RockPrefabDamageEntry
        {
            public GameObject prefab;
            [Min(0f)] public float damageMultiplier = 1f;
            [Min(0.01f)] public float decalScaleMultiplier = 1f;
        }

        private struct RockSpawnSelection
        {
            public GameObject Prefab;
            public float DamageMultiplier;
            public float DecalScaleMultiplier;

            public RockSpawnSelection(GameObject prefab, float damageMultiplier, float decalScaleMultiplier)
            {
                Prefab = prefab;
                DamageMultiplier = damageMultiplier;
                DecalScaleMultiplier = decalScaleMultiplier;
            }
        }
        #endregion

        #region Serialized Fields
        [Header("Rock Prefab")]
        [SerializeField] private GameObject rockPrefab;
        [Tooltip("Optional prefab-specific damage map. When assigned, each prefab can use its own damage multiplier.")]
        [SerializeField] private RockPrefabDamageEntry[] randomRockPrefabDamageMap;
        [SerializeField] private string spawnedRockLayerName = "Default";

        [Header("Spawn Anchors")]
        [SerializeField] private Transform[] spawnAnchors;
        [SerializeField] private Vector2Int rocksPerAnchorRange = new Vector2Int(6, 12);
        [SerializeField] private Vector2 horizontalScatter = new Vector2(2f, 2f);
        [SerializeField] private float heightJitter = 1.5f;
        [SerializeField] private float delayBetweenRocks = 0.03f;

        [Header("Launch")]
        [SerializeField] private Vector2 launchSpeedRange = new Vector2(8f, 14f);
        [SerializeField] private float downwardBias = 0.6f;
        [SerializeField] private Vector2 angularSpeedRange = new Vector2(180f, 540f);

        [Header("Camera Shake")]
        [SerializeField, Min(0f)] private float anchorPhaseShakeAmplitude = 0.6f;
        [SerializeField, Min(0f)] private float rockSpawnPhaseShakeAmplitude = 2f;
        [SerializeField, Min(0.01f)] private float shakeTransitionDuration = 0.08f;
        [SerializeField, Min(0.01f)] private float shakeFadeOutDuration = 0.35f;

        [Header("Impact Decals")]
        [SerializeField] private GameObject impactDecalProjectorPrefab;
        [SerializeField] private GameObject impactFxPrefab;
        [SerializeField] private Material[] impactDecalMaterials;
        [SerializeField, Min(0.01f)] private float impactDecalRevealDuration = 0.2f;
        [SerializeField, Min(0f)] private float impactDecalHoldDuration = 2f;
        [SerializeField, Min(0.01f)] private float impactDecalFadeDuration = 0.3f;
        [SerializeField, Min(0f)] private float impactDecalSpawnDelay = 0.35f;

        [Header("Anchor Pre-Decals")]
        [SerializeField] private Vector2Int anchorDecalCountRange = new Vector2Int(2, 3);
        [SerializeField] private Vector2 anchorDecalScatter = new Vector2(2f, 2f);
        [SerializeField] private Vector2 anchorDecalWidthRange = new Vector2(0.8f, 1.6f);
        [SerializeField, Min(0f)] private float anchorDecalProbeHeight = 3f;
        [SerializeField, Min(0.1f)] private float anchorDecalProbeDistance = 8f;
        [SerializeField] private LayerMask anchorDecalSurfaceMask = ~0;
        [SerializeField, Min(0f)] private float anchorDecalSurfaceOffset = 0.02f;
        [SerializeField, Min(0.01f)] private float anchorDecalRevealDuration = 0.2f;
        [SerializeField, Min(0f)] private float anchorToRockStartDelay = 0.25f;
        [SerializeField, Min(0.01f)] private float anchorDecalFadeDuration = 0.3f;

        [Header("Decal Cleanup")]
        [SerializeField, Min(0f)] private float delayBetweenDecalCleanup = 0.15f;

        [Header("Decal Pooling")]
        [SerializeField, Min(0)] private int decalPoolPrewarmCount = 12;
        [SerializeField, Min(1)] private int maxDecalPoolSizePerPrefab = 120;

        [Header("Collaborators")]
        [SerializeField] private LandslideDecalService decalService;
        [SerializeField] private LandslideShakeController shakeController;

        [Header("Pooling")]
        [SerializeField] private int prewarmCount = 24;
        [SerializeField] private int maxPoolSize = 80;
        [SerializeField] private float recycleAfterSeconds = 15f;
        [SerializeField] private float sleepRecycleDelaySeconds = 1.5f;
        [SerializeField] private float recycleScaleDownDuration = 0.2f;

        [Header("Interaction")]
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField] private LayerMask decalSpawnLayers = ~0;
        [Tooltip("Damage applied when rock speed is at or below Min Damage Velocity.")]
        [SerializeField] private float minImpactDamage = 4f;
        [Tooltip("Damage applied when rock speed is at or above Max Damage Velocity.")]
        [SerializeField] private float maxImpactDamage = 12f;
        [Tooltip("Rock speed threshold to start dealing damage. Below this speed, damage is 0.")]
        [SerializeField] private float minDamageVelocity = 4f;
        [Tooltip("Rock speed where damage reaches Max Impact Damage.")]
        [SerializeField] private float maxDamageVelocity = 16f;
        [SerializeField] private float pushImpulse = 4f;
        [SerializeField] private float hitCooldownSeconds = 0.5f;

        [Header("Landslide Audio")]
        [SerializeField] private string phaseOneAnchorCrackSoundId = "landslide_rock_crack";
        [SerializeField, Min(0f)] private float phaseOneAnchorCrackVolumeScale = 1f;
        [SerializeField] private string phaseOneAnchorRumbleSoundId = "landslide_rumble";
        [SerializeField, Min(0f)] private float phaseOneAnchorRumbleVolumeScale = 1f;
        [SerializeField] private string phaseTwoHardRumbleSoundId = "landslide_rumble_hard";
        [SerializeField, Min(0f)] private float phaseTwoHardRumbleVolumeScale = 1f;
        [SerializeField, Min(0.05f)] private float phaseTwoHardRumbleRepeatInterval = 1.1f;
        [SerializeField] private string impactDecalSoundId = "landslide_impact";
        [SerializeField, Min(0f)] private float impactDecalSoundVolumeScale = 1f;
        #endregion

        #region Runtime State
        private readonly Queue<LandslideRockBehavior> _availableRocks = new Queue<LandslideRockBehavior>();
        private readonly List<LandslideRockBehavior> _activeRocks = new List<LandslideRockBehavior>();

        private int _spawnedRockLayer;
        private IEventBus _eventBus;
        private Coroutine _phaseTwoHardRumbleLoopRoutine;
        private bool _isPhaseTwoHardRumbleLoopActive;
        private bool _hasRegisteredRockfallEncounterEvent;
        private bool _isRockfallRiskTrackingActive;
        private bool _wasRockfallEncountered;
        private bool _hasSubmittedRockfallRiskEvent;
        private Vector3 _rockfallRiskStartPosition;
        private float _rockfallRiskStartTime;
        private Vector3 _rockfallEncounterPosition;
        private float _rockfallEncounterTimestamp;
        private float _rockfallEncounterSeverity;
        private PlayerStatsTrackerService _statsTracker;
        #endregion

        #region Unity Lifecycle


        private void Awake()
        {
            ResolveEventBus();

            _spawnedRockLayer = LayerMask.NameToLayer(spawnedRockLayerName);
            if (_spawnedRockLayer < 0)
            {
                _spawnedRockLayer = gameObject.layer;
                Debug.LogWarning($"[LandslideRockSpawner] Layer '{spawnedRockLayerName}' not found. Falling back to spawner layer '{LayerMask.LayerToName(_spawnedRockLayer)}'.");
            }

            PrewarmPool();
            EnsureCollaborators();

            if (decalService != null)
            {
                decalService.Configure(transform, maxDecalPoolSizePerPrefab, delayBetweenDecalCleanup);
                decalService.PrewarmDecalPool(impactDecalProjectorPrefab, decalPoolPrewarmCount);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            FinalizeRockfallRiskEvent();
            StopPhaseTwoHardRumbleLoop();

            if (decalService != null)
            {
                decalService.StopCleanupRoutine();
            }

            shakeController?.StopAndReset();

            for (int i = _activeRocks.Count - 1; i >= 0; i--)
            {
                RecycleRock(_activeRocks[i], immediate: true);
            }

            decalService?.DestroyAllDecalsImmediate();
        }

        protected override void SubscribeToDirector(NaturalEventDirector director)
        {
            director.OnLandslideTriggered += Spawn;
        }

        protected override void UnsubscribeFromDirector(NaturalEventDirector director)
        {
            director.OnLandslideTriggered -= Spawn;
        }
        #endregion

        #region Public API
        public void RegisterSpawnedDecal(GameObject decal, float fadeDuration)
        {
            if (decalService == null || decal == null)
            {
                return;
            }

            decalService.RegisterSpawnedDecal(decal, fadeDuration);
        }

        public GameObject RentDecal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (decalService == null)
            {
                return null;
            }

            return decalService.RentDecal(prefab, position, rotation);
        }

        public void ReturnDecal(GameObject decal)
        {
            if (decalService == null || decal == null)
            {
                return;
            }

            decalService.ReturnDecal(decal);
        }

        [ContextMenu("Trigger Landslide")]
        public void TriggerLandslide()
        {
            if (!HasAnyRockPrefab())
            {
                Debug.LogWarning("[LandslideRockSpawner] Missing rock prefab assignment.");
                return;
            }

            if (spawnAnchors == null || spawnAnchors.Length == 0)
            {
                Debug.LogWarning("[LandslideRockSpawner] No spawnAnchors assigned.");
                return;
            }

            _hasRegisteredRockfallEncounterEvent = false;
            BeginRockfallRiskTracking(ResolveAudioPosition(spawnAnchors));

            PlayPhaseOneAnchorSounds(ResolveAudioPosition(spawnAnchors));
            StartCoroutine(SpawnRoutine(spawnAnchors));
        }

        protected override void SpawnInternal(Transform anchor, WorldLevel triggeredBiome)
        {
            _hasRegisteredRockfallEncounterEvent = false;
            BeginRockfallRiskTracking(anchor.position);

            PlayPhaseOneAnchorSounds(anchor.position);
            StartCoroutine(SpawnRoutine(new[] { anchor }));
        }

        [System.Obsolete("Use Spawn(Transform, WorldLevel) instead.")]
        public void TriggerLandslideAt(Transform anchor, WorldLevel triggeredBiome)
        {
            Spawn(anchor, triggeredBiome);
        }

        /// <summary>
        /// Triggers a landslide at a specific world position.
        /// If spawnAnchors are configured, uses existing anchors; otherwise creates a temporary anchor.
        /// </summary>
        /// <param name="position">World position where the landslide should trigger.</param>
        public void TriggerLandslideAtPosition(Vector3 position)
        {
            if (!HasAnyRockPrefab())
            {
                Debug.LogWarning("[LandslideRockSpawner] Missing rock prefab assignment.");
                return;
            }

            _hasRegisteredRockfallEncounterEvent = false;
            BeginRockfallRiskTracking(position);

            // Otherwise, create a temporary anchor at the position
            Transform temporaryAnchor = new GameObject($"LandslideAnchor_Temporary").transform;
            temporaryAnchor.SetPositionAndRotation(position, Quaternion.identity);
            temporaryAnchor.SetParent(transform);
            PlayPhaseOneAnchorSounds(temporaryAnchor.position);
            StartCoroutine(SpawnRoutineWithCleanup(new[] { temporaryAnchor }, destroyAnchorsAfterSpawn: true));
        }

        public void RecycleRock(LandslideRockBehavior rock)
        {
            RecycleRock(rock, immediate: false);
        }

        public bool TryReserveRockfallEncounterEvent()
        {
            if (_hasRegisteredRockfallEncounterEvent && !IsCleanupComplete())
            {
                return false;
            }

            _hasRegisteredRockfallEncounterEvent = true;
            return true;
        }

        public void RegisterRockfallEncounter(Vector3 eventPosition, float severity)
        {
            if (!_isRockfallRiskTrackingActive)
            {
                return;
            }

            if (!_wasRockfallEncountered)
            {
                _rockfallEncounterPosition = eventPosition;
                _rockfallEncounterTimestamp = Time.time;
            }

            _wasRockfallEncountered = true;
            _rockfallEncounterSeverity = Mathf.Max(_rockfallEncounterSeverity, Mathf.Clamp01(severity));
            _hasRegisteredRockfallEncounterEvent = true;
        }
        #endregion

        #region Spawn Flow
        private void RecycleRock(LandslideRockBehavior rock, bool immediate)
        {
            if (rock == null)
            {
                return;
            }

            if (!_activeRocks.Remove(rock))
            {
                return;
            }

            if (immediate)
            {
                FinishRecycle(rock);
                return;
            }

            rock.PlayRecycleScaleDown(recycleScaleDownDuration, () => FinishRecycle(rock));
        }

        private void FinishRecycle(LandslideRockBehavior rock)
        {
            if (rock == null)
            {
                return;
            }

            rock.ResetForPool();
            rock.gameObject.SetActive(false);
            _availableRocks.Enqueue(rock);

            if (_activeRocks.Count == 0)
            {
                FinalizeRockfallRiskEvent();
                StopPhaseTwoHardRumbleLoop();
                shakeController?.TransitionShake(0f, shakeFadeOutDuration);
                decalService?.FadeAndDestroyAllDecals();
            }
        }

        private IEnumerator SpawnRoutine(IReadOnlyList<Transform> anchors)
        {
            WaitForSeconds wait = delayBetweenRocks > 0f ? new WaitForSeconds(delayBetweenRocks) : null;
            WaitForSeconds anchorPhaseWait = anchorToRockStartDelay > 0f ? new WaitForSeconds(anchorToRockStartDelay) : null;

            shakeController?.TransitionShake(anchorPhaseShakeAmplitude, shakeTransitionDuration);

            // Phase 1: place all anchor decals first.
            for (int i = 0; i < anchors.Count; i++)
            {
                Transform anchor = anchors[i];
                if (anchor == null)
                {
                    continue;
                }

                SpawnAnchorPreDecals(anchor);
            }

            // Phase 2: wait before starting any rock spawn.
            if (anchorPhaseWait != null)
            {
                yield return anchorPhaseWait;
            }

            shakeController?.TransitionShake(rockSpawnPhaseShakeAmplitude, shakeTransitionDuration);
            StartPhaseTwoHardRumbleLoop(ResolveAudioPosition(anchors));

            // Phase 3: spawn rocks for all anchors.
            for (int i = 0; i < anchors.Count; i++)
            {
                Transform anchor = anchors[i];
                if (anchor == null)
                {
                    continue;
                }

                int count = Random.Range(rocksPerAnchorRange.x, rocksPerAnchorRange.y + 1);
                for (int n = 0; n < count; n++)
                {
                    SpawnSingleRock(anchor);
                    if (wait != null)
                    {
                        yield return wait;
                    }
                }

            }

            // Phase 2 audio should end when spawning finishes, even if rocks are still active.
            StopPhaseTwoHardRumbleLoop();

        }

        private IEnumerator SpawnRoutineWithCleanup(IReadOnlyList<Transform> anchors, bool destroyAnchorsAfterSpawn)
        {
            yield return StartCoroutine(SpawnRoutine(anchors));
            
            if (destroyAnchorsAfterSpawn)
            {
                for (int i = 0; i < anchors.Count; i++)
                {
                    Transform anchor = anchors[i];
                    if (anchor != null)
                    {
                        Destroy(anchor.gameObject);
                    }
                }
            }

            if (_activeRocks.Count == 0)
            {
                FinalizeRockfallRiskEvent();
                StopPhaseTwoHardRumbleLoop();
                shakeController?.TransitionShake(0f, shakeFadeOutDuration);
                decalService?.FadeAndDestroyAllDecals();
            }
        }

        private void SpawnSingleRock(Transform anchor)
        {
            LandslideRockBehavior rock = GetOrCreateRock();
            if (rock == null)
            {
                return;
            }

            Vector3 localOffset = new Vector3(
                Random.Range(-horizontalScatter.x, horizontalScatter.x),
                Random.Range(0f, heightJitter),
                Random.Range(-horizontalScatter.y, horizontalScatter.y));

            Vector3 worldOffset = anchor.right * localOffset.x + Vector3.up * localOffset.y + anchor.forward * localOffset.z;
            rock.transform.SetPositionAndRotation(anchor.position + worldOffset, Random.rotation);
            rock.gameObject.layer = _spawnedRockLayer;
            rock.gameObject.SetActive(true);

            Vector3 randomDownward = Random.insideUnitSphere;
            randomDownward.y = -Mathf.Abs(randomDownward.y) - downwardBias;
            Vector3 launchDirection = (anchor.forward * 0.35f + randomDownward).normalized;

            float launchSpeed = Random.Range(launchSpeedRange.x, launchSpeedRange.y);
            float angularSpeed = Random.Range(angularSpeedRange.x, angularSpeedRange.y);
            Vector3 angularVelocity = Random.insideUnitSphere * angularSpeed * Mathf.Deg2Rad;

            rock.Launch(launchDirection * launchSpeed, angularVelocity);
            _activeRocks.Add(rock);
        }
        #endregion

        #region Pooling
        private LandslideRockBehavior GetOrCreateRock()
        {
            if (_availableRocks.Count > 0)
            {
                return _availableRocks.Dequeue();
            }

            int totalCount = _availableRocks.Count + _activeRocks.Count;
            if (totalCount < maxPoolSize)
            {
                return CreatePooledRock();
            }

            if (_activeRocks.Count > 0)
            {
                LandslideRockBehavior oldest = _activeRocks[0];
                RecycleRock(oldest, immediate: true);
                if (_availableRocks.Count > 0)
                {
                    return _availableRocks.Dequeue();
                }
            }

            Debug.LogWarning("[LandslideRockSpawner] Pool exhausted and no active rocks could be recycled.");
            return null;
        }

        private void PrewarmPool()
        {
            int target = Mathf.Clamp(prewarmCount, 0, maxPoolSize);
            for (int i = 0; i < target; i++)
            {
                LandslideRockBehavior behavior = CreatePooledRock();
                if (behavior != null)
                {
                    _availableRocks.Enqueue(behavior);
                }
            }
        }

        private LandslideRockBehavior CreatePooledRock()
        {
            RockSpawnSelection selection = GetRandomRockSelection();
            GameObject selectedPrefab = selection.Prefab;
            if (selectedPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(selectedPrefab, transform);
            instance.SetActive(false);
            instance.layer = _spawnedRockLayer;

            if (instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<SphereCollider>();
            }

            if (instance.GetComponent<Rigidbody>() == null)
            {
                instance.AddComponent<Rigidbody>();
            }

            LandslideRockBehavior behavior = instance.GetComponent<LandslideRockBehavior>();
            if (behavior == null)
            {
                behavior = instance.AddComponent<LandslideRockBehavior>();
            }

            LandslideRockBehaviorConfig behaviorConfig = CreateRockBehaviorConfig(selection);
            behavior.Configure(this, behaviorConfig);

            return behavior;
        }

        #endregion

        #region Config And Selection
        private LandslideRockBehaviorConfig CreateRockBehaviorConfig(RockSpawnSelection selection)
        {
            return new LandslideRockBehaviorConfig(
                interactionLayers,
                decalSpawnLayers,
                minImpactDamage,
                maxImpactDamage,
                minDamageVelocity,
                maxDamageVelocity,
                selection.DamageMultiplier,
                selection.DecalScaleMultiplier,
                impactDecalProjectorPrefab,
                impactFxPrefab,
                impactDecalMaterials,
                impactDecalRevealDuration,
                impactDecalHoldDuration,
                impactDecalFadeDuration,
                impactDecalSpawnDelay,
                impactDecalSoundId,
                impactDecalSoundVolumeScale,
                anchorDecalSurfaceOffset,
                pushImpulse,
                hitCooldownSeconds,
                recycleAfterSeconds,
                sleepRecycleDelaySeconds);
        }

        private bool HasAnyRockPrefab()
        {
            if (randomRockPrefabDamageMap != null)
            {
                for (int i = 0; i < randomRockPrefabDamageMap.Length; i++)
                {
                    if (randomRockPrefabDamageMap[i] != null && randomRockPrefabDamageMap[i].prefab != null)
                    {
                        return true;
                    }
                }
            }

            return rockPrefab != null;
        }

        private RockSpawnSelection GetRandomRockSelection()
        {
            if (randomRockPrefabDamageMap != null && randomRockPrefabDamageMap.Length > 0)
            {
                int validCount = 0;
                for (int i = 0; i < randomRockPrefabDamageMap.Length; i++)
                {
                    if (randomRockPrefabDamageMap[i] != null && randomRockPrefabDamageMap[i].prefab != null)
                    {
                        validCount++;
                    }
                }

                if (validCount > 0)
                {
                    int selectedValidIndex = Random.Range(0, validCount);
                    int runningValidIndex = 0;
                    for (int i = 0; i < randomRockPrefabDamageMap.Length; i++)
                    {
                        RockPrefabDamageEntry entry = randomRockPrefabDamageMap[i];
                        if (entry == null || entry.prefab == null)
                        {
                            continue;
                        }

                        if (runningValidIndex == selectedValidIndex)
                        {
                            return new RockSpawnSelection(
                                entry.prefab,
                                Mathf.Max(0f, entry.damageMultiplier),
                                Mathf.Max(0.01f, entry.decalScaleMultiplier));
                        }

                        runningValidIndex++;
                    }
                }
            }

            return new RockSpawnSelection(rockPrefab, 1f, 1f);
        }
        #endregion

        #region Anchor Decal And FX
        private void SpawnAnchorPreDecals(Transform anchor)
        {
            if (anchor == null || impactDecalProjectorPrefab == null)
            {
                return;
            }

            int minCount = Mathf.Min(anchorDecalCountRange.x, anchorDecalCountRange.y);
            int maxCount = Mathf.Max(anchorDecalCountRange.x, anchorDecalCountRange.y);
            int spawnCount = Random.Range(minCount, maxCount + 1);

            for (int i = 0; i < spawnCount; i++)
            {
                RockSpawnSelection selection = GetRandomRockSelection();
                float jitterX = Random.Range(-anchorDecalScatter.x, anchorDecalScatter.x);
                float jitterZ = Random.Range(-anchorDecalScatter.y, anchorDecalScatter.y);
                Vector3 horizontalOffset = anchor.right * jitterX + anchor.forward * jitterZ;

                ResolveAnchorDecalPlacement(anchor, horizontalOffset, out Vector3 spawnPosition, out Vector3 spawnNormal);

                GameObject decal = SpawnAndRevealAnchorDecal(selection, spawnPosition, spawnNormal);
                if (decal == null)
                {
                    continue;
                }

                RegisterSpawnedDecal(decal, anchorDecalFadeDuration);

                GameObject fx = SpawnAnchorFx(spawnPosition, spawnNormal);
                if (fx != null)
                {
                    RegisterSpawnedDecal(fx, anchorDecalFadeDuration);
                }
            }
        }

        private void ResolveAnchorDecalPlacement(Transform anchor, Vector3 horizontalOffset, out Vector3 spawnPosition, out Vector3 spawnNormal)
        {
            Vector3 rayOrigin = anchor.position + horizontalOffset + Vector3.up * anchorDecalProbeHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, anchorDecalProbeDistance, anchorDecalSurfaceMask, QueryTriggerInteraction.Ignore))
            {
                spawnPosition = hit.point + hit.normal * anchorDecalSurfaceOffset;
                spawnNormal = hit.normal;
                return;
            }

            spawnPosition = anchor.position + horizontalOffset + Vector3.up * anchorDecalSurfaceOffset;
            spawnNormal = Vector3.up;
        }

        private GameObject SpawnAndRevealAnchorDecal(RockSpawnSelection selection, Vector3 spawnPosition, Vector3 spawnNormal)
        {
            GameObject decal = RentDecal(
                impactDecalProjectorPrefab,
                spawnPosition,
                Quaternion.LookRotation(spawnNormal));

            if (decal == null)
            {
                return null;
            }

            DecalProjector projector = decal.GetComponent<DecalProjector>();
            if (projector == null)
            {
                projector = decal.GetComponentInChildren<DecalProjector>();
            }

            if (projector == null)
            {
                ReturnDecal(decal);
                return null;
            }

            projector.transform.forward = -spawnNormal;

            float randomSize = Random.Range(
                Mathf.Min(anchorDecalWidthRange.x, anchorDecalWidthRange.y),
                Mathf.Max(anchorDecalWidthRange.x, anchorDecalWidthRange.y));

            Material randomMaterial = GetRandomSharedDecalMaterial();
            if (randomMaterial != null)
            {
                projector.material = randomMaterial;
            }

            Vector3 size = projector.size;
            float uniformBaseSize = Mathf.Max(size.x, size.z);
            float targetSize = Mathf.Max(0.01f, uniformBaseSize * randomSize * selection.DecalScaleMultiplier);

            SetProjectorWidth(projector, 0.01f);
            SetProjectorHeight(projector, 0.01f);

            DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(projector)
                .Append(DOTween.To(() => projector.size.x, value => SetProjectorWidth(projector, value), targetSize, anchorDecalRevealDuration).SetEase(Ease.OutBack))
                .Join(DOTween.To(() => projector.size.y, value => SetProjectorHeight(projector, value), targetSize, anchorDecalRevealDuration).SetEase(Ease.OutBack));

            return decal;
        }

        private GameObject SpawnAnchorFx(Vector3 position, Vector3 surfaceNormal)
        {
            if (impactFxPrefab == null)
            {
                return null;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            GameObject fx = Instantiate(impactFxPrefab, position, rotation);
            ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);

            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Play(true);
            }
            
            return fx;
        }
        #endregion

        #region Utility Helpers
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

        private Material GetRandomSharedDecalMaterial()
        {
            if (impactDecalMaterials == null || impactDecalMaterials.Length == 0)
            {
                return null;
            }

            int validCount = 0;
            for (int i = 0; i < impactDecalMaterials.Length; i++)
            {
                if (impactDecalMaterials[i] != null)
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
            for (int i = 0; i < impactDecalMaterials.Length; i++)
            {
                Material material = impactDecalMaterials[i];
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

        private void EnsureCollaborators()
        {
            if (decalService == null)
            {
                decalService = GetComponent<LandslideDecalService>();
            }

            if (decalService == null)
            {
                decalService = gameObject.AddComponent<LandslideDecalService>();
            }

            if (shakeController == null)
            {
                shakeController = GetComponent<LandslideShakeController>();
            }

            if (shakeController == null)
            {
                shakeController = gameObject.AddComponent<LandslideShakeController>();
            }
        }

        public void PublishPositionalSound(string soundId, Vector3 position, float volumeScale)
        {
            if (string.IsNullOrWhiteSpace(soundId) || volumeScale <= 0f)
            {
                return;
            }

            ResolveEventBus();
            _eventBus?.Publish(new PlayPositionalSFXEvent(soundId, position, volumeScale));
        }

        private void PlayPhaseOneAnchorSounds(Vector3 position)
        {
            PublishPositionalSound(phaseOneAnchorCrackSoundId, position, phaseOneAnchorCrackVolumeScale);
            PublishPositionalSound(phaseOneAnchorRumbleSoundId, position, phaseOneAnchorRumbleVolumeScale);
        }

        private void StartPhaseTwoHardRumbleLoop(Vector3 position)
        {
            StopPhaseTwoHardRumbleLoop();

            if (string.IsNullOrWhiteSpace(phaseTwoHardRumbleSoundId) || phaseTwoHardRumbleVolumeScale <= 0f)
            {
                return;
            }

            _isPhaseTwoHardRumbleLoopActive = true;
            _phaseTwoHardRumbleLoopRoutine = StartCoroutine(PhaseTwoHardRumbleLoop(position));
        }

        private void StopPhaseTwoHardRumbleLoop()
        {
            _isPhaseTwoHardRumbleLoopActive = false;

            if (_phaseTwoHardRumbleLoopRoutine == null)
            {
                return;
            }

            StopCoroutine(_phaseTwoHardRumbleLoopRoutine);
            _phaseTwoHardRumbleLoopRoutine = null;
        }

        private IEnumerator PhaseTwoHardRumbleLoop(Vector3 position)
        {
            WaitForSeconds wait = new WaitForSeconds(phaseTwoHardRumbleRepeatInterval);
            while (_isPhaseTwoHardRumbleLoopActive)
            {
                PublishPositionalSound(phaseTwoHardRumbleSoundId, position, phaseTwoHardRumbleVolumeScale);
                yield return wait;
            }

            _phaseTwoHardRumbleLoopRoutine = null;
        }

        private Vector3 ResolveAudioPosition(IReadOnlyList<Transform> anchors)
        {
            if (anchors == null || anchors.Count == 0)
            {
                return transform.position;
            }

            Vector3 total = Vector3.zero;
            int validCount = 0;
            for (int i = 0; i < anchors.Count; i++)
            {
                Transform anchor = anchors[i];
                if (anchor == null)
                {
                    continue;
                }

                total += anchor.position;
                validCount++;
            }

            return validCount > 0 ? total / validCount : transform.position;
        }

        private void ResolveEventBus()
        {
            _eventBus ??= ServiceContainer.Instance?.TryGet<IEventBus>();
        }

        private void ResolveStatsTracker()
        {
            _statsTracker ??= ServiceContainer.Instance?.TryGet<PlayerStatsTrackerService>();
        }

        private void BeginRockfallRiskTracking(Vector3 eventPosition)
        {
            FinalizeRockfallRiskEvent();

            _isRockfallRiskTrackingActive = true;
            _wasRockfallEncountered = false;
            _hasSubmittedRockfallRiskEvent = false;
            _rockfallRiskStartPosition = eventPosition;
            _rockfallRiskStartTime = Time.time;
            _rockfallEncounterPosition = eventPosition;
            _rockfallEncounterTimestamp = _rockfallRiskStartTime;
            _rockfallEncounterSeverity = 0f;
        }

        private void FinalizeRockfallRiskEvent()
        {
            if (!_isRockfallRiskTrackingActive || _hasSubmittedRockfallRiskEvent)
            {
                return;
            }

            ResolveStatsTracker();
            if (_statsTracker != null)
            {
                RiskEvent riskEvent = new RiskEvent
                {
                    riskType = RiskType.Rockfall,
                    location = _wasRockfallEncountered ? _rockfallEncounterPosition : _rockfallRiskStartPosition,
                    timestamp = _wasRockfallEncountered ? _rockfallEncounterTimestamp : Time.time,
                    wasEncountered = _wasRockfallEncountered,
                    severity = _wasRockfallEncountered ? _rockfallEncounterSeverity : 0f
                };

                _statsTracker.RegisterRiskEvent(riskEvent);
            }

            _hasSubmittedRockfallRiskEvent = true;
            _isRockfallRiskTrackingActive = false;
        }

        private bool IsCleanupComplete()
        {
            bool hasActiveRocks = _activeRocks.Count > 0;
            bool decalsAreIdle = decalService == null || decalService.IsCleanupIdle;
            return !hasActiveRocks && decalsAreIdle;
        }
        #endregion
    }
}
