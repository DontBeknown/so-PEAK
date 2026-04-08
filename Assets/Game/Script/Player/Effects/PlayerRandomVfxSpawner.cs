using UnityEngine;
using Game.Core.DI;
using Game.Environment.DayNight;

namespace Game.Player.Effects
{
    /// <summary>
    /// Spawns a VFX prefab at a player child anchor on a random timer.
    /// The spawn rate is influenced by the current time of day and the current world level.
    /// </summary>
    public class PlayerRandomVfxSpawner : MonoBehaviour
    {
        [System.Serializable]
        private class VfxMapping
        {
            public TimeOfDay timeOfDay = TimeOfDay.Day;
            [Min(1)] public int minLevel = 1;
            [Min(1)] public int maxLevel = 3;
            public GameObject prefab;

            public bool Matches(TimeOfDay currentTimeOfDay, int currentLevel)
            {
                if (prefab == null)
                {
                    return false;
                }

                return timeOfDay == currentTimeOfDay && currentLevel >= minLevel && currentLevel <= maxLevel;
            }
        }

        [Header("References")]
        [SerializeField] private DayNightCycleManager _dayNightService;
        [SerializeField] private SaveLoadService _saveLoadService;
        
        [Header("Spawn Target")]
        [SerializeField] private GameObject vfxPrefab;
        [SerializeField] private VfxMapping[] vfxMappings;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField] private string fallbackAnchorName = "VFXSpawnAnchor";
        [SerializeField] private bool autoFindAnchor = true;
        [SerializeField] private Vector3 localSpawnOffset = new Vector3(0f, 0.05f, 0f);
        [SerializeField] private float localSpawnJitter = 0.12f;
        [SerializeField] private bool useAnchorRotation = true;

        [Header("Timing")]
        [SerializeField] private float baseMinDelaySeconds = 10f;
        [SerializeField] private float baseMaxDelaySeconds = 25f;
        [SerializeField] private float morningRateMultiplier = 0.85f;
        [SerializeField] private float dayRateMultiplier = 1f;
        [SerializeField] private float eveningRateMultiplier = 1.2f;
        [SerializeField] private float nightRateMultiplier = 1.45f;
        [SerializeField] private float levelRateIncreasePerLevel = 0.12f;
        [SerializeField] private float maxLevelRateMultiplier = 3f;

        [Header("Cleanup")]
        [SerializeField] private bool autoDestroySpawnedVfx = true;
        [SerializeField] private float fallbackLifetimeSeconds = 8f;

        private float _spawnTimer;
        private float _nextSpawnDelay;
        private bool _loggedMissingServiceWarning;
        private bool _loggedMissingSaveServiceWarning;
        private bool _loggedMissingPrefabWarning;

        private void Awake()
        {
            ResolveSpawnAnchor();
            ResetSpawnTimer();
        }

        private void OnEnable()
        {
            TryResolveServices();
            ResolveSpawnAnchor();
            ResetSpawnTimer();
        }

        private void Start()
        {
            TryResolveServices();
            ResolveSpawnAnchor();
        }

        private void Update()
        {
            if (!TryResolveServices())
            {
                return;
            }

            if (_dayNightService != null && _dayNightService.IsPaused)
            {
                return;
            }

            _spawnTimer += Time.deltaTime * GetRateMultiplier();

            if (_spawnTimer < _nextSpawnDelay)
            {
                return;
            }

            SpawnVfx();
            ResetSpawnTimer();
        }

        private void OnValidate()
        {
            if (baseMinDelaySeconds < 0.01f)
            {
                baseMinDelaySeconds = 0.01f;
            }

            if (baseMaxDelaySeconds < baseMinDelaySeconds)
            {
                baseMaxDelaySeconds = baseMinDelaySeconds;
            }

            if (localSpawnJitter < 0f)
            {
                localSpawnJitter = 0f;
            }

            if (maxLevelRateMultiplier < 1f)
            {
                maxLevelRateMultiplier = 1f;
            }
        }

        private bool TryResolveServices()
        {
            if (_dayNightService != null)
            {
                return true;
            }

            var container = ServiceContainer.Instance;
            _dayNightService ??= container.TryGet<DayNightCycleManager>();
            _saveLoadService ??= container.TryGet<SaveLoadService>();

            if (_saveLoadService == null && !_loggedMissingSaveServiceWarning)
            {
                _loggedMissingSaveServiceWarning = true;
                Debug.LogWarning("[PlayerRandomVfxSpawner] SaveLoadService not found. Falling back to level 1 for VFX selection and timing.");
            }

            bool ready = _dayNightService != null;
            if (!ready && !_loggedMissingServiceWarning)
            {
                _loggedMissingServiceWarning = true;
                Debug.LogWarning("[PlayerRandomVfxSpawner] Waiting for day/night service to register.");
            }

            return ready;
        }

        private void ResolveSpawnAnchor()
        {
            if (spawnAnchor != null)
            {
                return;
            }

            if (autoFindAnchor && !string.IsNullOrWhiteSpace(fallbackAnchorName))
            {
                spawnAnchor = transform.Find(fallbackAnchorName);
            }

            if (spawnAnchor == null && transform.childCount > 0)
            {
                spawnAnchor = transform.GetChild(0);
            }

            if (spawnAnchor == null)
            {
                spawnAnchor = transform;
            }
        }

        private void ResetSpawnTimer()
        {
            _spawnTimer = 0f;
            _nextSpawnDelay = Random.Range(GetMinDelaySeconds(), GetMaxDelaySeconds());
        }

        private float GetMinDelaySeconds()
        {
            return Mathf.Max(0.01f, baseMinDelaySeconds);
        }

        private float GetMaxDelaySeconds()
        {
            return Mathf.Max(GetMinDelaySeconds(), baseMaxDelaySeconds);
        }

        private float GetRateMultiplier()
        {
            float timeMultiplier = GetTimeOfDayMultiplier(_dayNightService != null ? _dayNightService.CurrentTimeOfDay : TimeOfDay.Day);
            int currentLevel = Mathf.Max(1, _saveLoadService != null ? _saveLoadService.GetCurrentLevel() : 1);
            float levelMultiplier = Mathf.Min(maxLevelRateMultiplier, 1f + ((currentLevel - 1) * levelRateIncreasePerLevel));

            return Mathf.Max(0.1f, timeMultiplier * levelMultiplier);
        }

        private float GetTimeOfDayMultiplier(TimeOfDay timeOfDay)
        {
            return timeOfDay switch
            {
                TimeOfDay.Morning => morningRateMultiplier,
                TimeOfDay.Day => dayRateMultiplier,
                TimeOfDay.Evening => eveningRateMultiplier,
                TimeOfDay.Night => nightRateMultiplier,
                _ => 1f
            };
        }

        private void SpawnVfx()
        {
            GameObject selectedVfxPrefab = GetMappedPrefab();
            if (selectedVfxPrefab == null)
            {
                if (!_loggedMissingPrefabWarning)
                {
                    _loggedMissingPrefabWarning = true;
                    Debug.LogWarning("[PlayerRandomVfxSpawner] No VFX prefab found for current time/level mapping and no fallback assigned.");
                }

                return;
            }

            ResolveSpawnAnchor();

            Vector3 offset = GetRandomizedLocalOffset();
            Vector3 spawnPosition = spawnAnchor.position + spawnAnchor.TransformVector(offset);
            Quaternion spawnRotation = useAnchorRotation ? spawnAnchor.rotation : Quaternion.identity;

            GameObject spawnedVfx = Instantiate(selectedVfxPrefab, spawnPosition, spawnRotation);
            PlayParticleSystems(spawnedVfx);

            if (autoDestroySpawnedVfx)
            {
                Destroy(spawnedVfx, GetAutoDestroyDelay(spawnedVfx));
            }
        }

        private GameObject GetMappedPrefab()
        {
            TimeOfDay currentTimeOfDay = _dayNightService != null ? _dayNightService.CurrentTimeOfDay : TimeOfDay.Day;
            int currentLevel = Mathf.Max(1, _saveLoadService != null ? _saveLoadService.GetCurrentLevel() : 1);

            if (vfxMappings != null)
            {
                int matchCount = 0;
                foreach (var mapping in vfxMappings)
                {
                    if (mapping != null && mapping.Matches(currentTimeOfDay, currentLevel))
                    {
                        matchCount++;
                    }
                }

                if (matchCount > 0)
                {
                    int selectedMatchIndex = Random.Range(0, matchCount);
                    int currentMatchIndex = 0;

                    foreach (var mapping in vfxMappings)
                    {
                        if (mapping == null || !mapping.Matches(currentTimeOfDay, currentLevel))
                        {
                            continue;
                        }

                        if (currentMatchIndex == selectedMatchIndex)
                        {
                            return mapping.prefab;
                        }

                        currentMatchIndex++;
                    }
                }
            }

            return vfxPrefab;
        }

        private Vector3 GetRandomizedLocalOffset()
        {
            if (localSpawnJitter <= 0f)
            {
                return localSpawnOffset;
            }

            float jitterX = Random.Range(-localSpawnJitter, localSpawnJitter);
            float jitterZ = Random.Range(-localSpawnJitter, localSpawnJitter);

            return localSpawnOffset + new Vector3(jitterX, 0f, jitterZ);
        }

        private void PlayParticleSystems(GameObject spawnedVfx)
        {
            if (spawnedVfx == null)
            {
                return;
            }

            var particleSystems = spawnedVfx.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var particleSystem in particleSystems)
            {
                particleSystem.Play(true);
            }
        }

        private float GetAutoDestroyDelay(GameObject spawnedVfx)
        {
            if (spawnedVfx == null)
            {
                return fallbackLifetimeSeconds;
            }

            float lifetime = fallbackLifetimeSeconds;
            var particleSystems = spawnedVfx.GetComponentsInChildren<ParticleSystem>(true);

            foreach (var particleSystem in particleSystems)
            {
                var main = particleSystem.main;
                float startLifetime = main.startLifetime.constant;
                float duration = main.duration;

                lifetime = Mathf.Max(lifetime, duration + startLifetime + 0.5f);
            }

            return lifetime;
        }
    }
}