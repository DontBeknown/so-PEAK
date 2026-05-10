#if DEV_CHEATS
using System.Reflection;
using UnityEngine;

namespace Game.DebugTools
{
    [DefaultExecutionOrder(1000)]
    public class DevCheatPanel : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F1;
        private const float MinSpeedMult = 1f;
        private const float MaxSpeedMult = 5f;
        private const float TeleportYOffset = 1.5f;

        private static DevCheatPanel _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            if (_instance != null) return;
            var go = new GameObject("[DevCheatPanel]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DevCheatPanel>();
        }

        private bool _open;
        private Rect _windowRect = new Rect(20, 20, 320, 460);

        private float _speedMult = 1f;
        private bool _speedApplied;
        private float _origWalk, _origRun, _origClimb;

        private bool _immune;
        private bool _pathRevealed;

        private PlayerStats _playerStats;
        private Transform _playerTransform;
        private CharacterController _playerController;
        private WatchTower[] _towers;
        private NaturalEventDirector _director;
        private WorldDataManager _worldData;

        private Vector2 _scroll;

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _open = !_open;
                if (_open) RefreshReferences();
            }
        }

        private void RefreshReferences()
        {
            if (_playerStats == null) _playerStats = FindFirstObjectByType<PlayerStats>();
            if (_playerStats != null && _playerTransform == null)
            {
                _playerTransform = _playerStats.transform;
                _playerController = _playerStats.GetComponent<CharacterController>();
            }
            _towers = FindObjectsByType<WatchTower>(FindObjectsSortMode.None);
            if (_director == null) _director = FindFirstObjectByType<NaturalEventDirector>();
            if (_worldData == null) _worldData = FindFirstObjectByType<WorldDataManager>();
        }

        private void OnGUI()
        {
            if (!_open) return;
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "Dev Cheats (F1)");
        }

        private void DrawWindow(int id)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            DrawSpeedSection();
            GUILayout.Space(6);
            DrawImmunitySection();
            GUILayout.Space(6);
            DrawPathSection();
            GUILayout.Space(6);
            DrawNextLevelSection();
            GUILayout.Space(6);
            DrawTeleportSection();
            GUILayout.Space(6);
            DrawHazardSection();

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawSpeedSection()
        {
            GUILayout.Label($"<b>Speed Multiplier: {_speedMult:0.0}x</b>", RichLabel());
            float newMult = GUILayout.HorizontalSlider(_speedMult, MinSpeedMult, MaxSpeedMult);
            if (!Mathf.Approximately(newMult, _speedMult))
            {
                _speedMult = newMult;
                ApplySpeed();
            }
            if (GUILayout.Button("Reset Speed (1x)"))
            {
                _speedMult = 1f;
                ApplySpeed();
            }
        }

        private void DrawImmunitySection()
        {
            bool newImmune = GUILayout.Toggle(_immune, "  Status Immunity (no hunger/thirst/temp/damage)");
            if (newImmune != _immune)
            {
                _immune = newImmune;
                if (_playerStats == null) _playerStats = FindFirstObjectByType<PlayerStats>();
                _playerStats?.SetImmunity(_immune);
            }
        }

        private void DrawPathSection()
        {
            bool newReveal = GUILayout.Toggle(_pathRevealed, "  Reveal Cached Path");
            if (newReveal != _pathRevealed)
            {
                _pathRevealed = newReveal;
                var ctl = HJBClickPathController.Instance;
                if (ctl == null)
                {
                    Debug.LogWarning("[DevCheats] HJBClickPathController.Instance is null.");
                    _pathRevealed = false;
                }
                else if (_pathRevealed) ctl.DrawCachedPath();
                else ctl.HidePath();
            }
        }

        private void DrawNextLevelSection()
        {
            if (GUILayout.Button("Progress To Next Level"))
            {
                if (SaveLoadService.Instance != null)
                {
                    RestoreSpeedIfApplied();
                    SaveLoadService.Instance.ProgressToNextLevel();
                }
                else Debug.LogWarning("[DevCheats] SaveLoadService.Instance is null.");
            }
        }

        private void DrawTeleportSection()
        {
            GUILayout.Label("<b>Teleport to WatchTower</b>", RichLabel());
            if (_towers == null || _towers.Length == 0)
            {
                GUILayout.Label("  (none in scene — re-open panel)");
                if (GUILayout.Button("Refresh")) RefreshReferences();
                return;
            }
            foreach (var t in _towers)
            {
                if (t == null) continue;
                if (GUILayout.Button($"→ {t.name}")) TeleportToTower(t);
            }
        }

        private void DrawHazardSection()
        {
            GUILayout.Label("<b>Summon Natural Event</b>", RichLabel());
            if (_director == null)
            {
                GUILayout.Label("  (NaturalEventDirector not found)");
                if (GUILayout.Button("Refresh")) RefreshReferences();
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Landslide")) TriggerHazard(landslide: true);
            if (GUILayout.Button("Tornado")) TriggerHazard(landslide: false);
            GUILayout.EndHorizontal();
        }

        private void ApplySpeed()
        {
            if (_playerStats == null) _playerStats = FindFirstObjectByType<PlayerStats>();
            var cfg = _playerStats != null ? _playerStats.Config : null;
            if (cfg == null) return;

            if (!_speedApplied)
            {
                _origWalk = cfg.baseWalkSpeed;
                _origRun = cfg.baseRunSpeed;
                _origClimb = cfg.baseClimbSpeed;
                _speedApplied = true;
            }
            cfg.baseWalkSpeed = _origWalk * _speedMult;
            cfg.baseRunSpeed = _origRun * _speedMult;
            cfg.baseClimbSpeed = _origClimb * _speedMult;
        }

        private void RestoreSpeedIfApplied()
        {
            if (!_speedApplied) return;
            var cfg = _playerStats != null ? _playerStats.Config : null;
            if (cfg != null)
            {
                cfg.baseWalkSpeed = _origWalk;
                cfg.baseRunSpeed = _origRun;
                cfg.baseClimbSpeed = _origClimb;
            }
            _speedApplied = false;
            _speedMult = 1f;
        }

        private void TeleportToTower(WatchTower tower)
        {
            if (_playerTransform == null || _playerController == null)
            {
                RefreshReferences();
                if (_playerTransform == null || _playerController == null)
                {
                    Debug.LogWarning("[DevCheats] Player not found for teleport.");
                    return;
                }
            }

            Vector3 pos = tower.transform.position;
            float? groundY = SampleGroundY(pos.x, pos.z);
            if (!groundY.HasValue)
            {
                if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out var hit, 200f))
                    groundY = hit.point.y;
            }

            Vector3 target = new Vector3(pos.x, (groundY ?? pos.y) + TeleportYOffset, pos.z);

            _playerController.enabled = false;
            _playerTransform.position = target;
            _playerController.enabled = true;
        }

        private float? SampleGroundY(float worldX, float worldZ)
        {
            if (_worldData == null) _worldData = FindFirstObjectByType<WorldDataManager>();
            if (_worldData == null || _worldData.globalHeightMap == null || _worldData.activeGen == null) return null;

            var map = _worldData.globalHeightMap;
            int w = map.GetLength(0);
            int h = map.GetLength(1);
            if (worldX < 0 || worldZ < 0 || worldX >= w || worldZ >= h) return null;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(worldX), 0, w - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(worldX), 0, w - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(worldZ), 0, h - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt(worldZ), 0, h - 1);
            float tx = worldX - x0;
            float tz = worldZ - z0;
            float h0 = Mathf.Lerp(map[x0, z0], map[x1, z0], tx);
            float h1 = Mathf.Lerp(map[x0, z1], map[x1, z1], tx);
            float raw = Mathf.Lerp(h0, h1, tz);
            return raw * _worldData.activeGen.meshHeightMultiplier;
        }

        private void TriggerHazard(bool landslide)
        {
            if (_director == null) return;
            if (_director.dataManager == null)
            {
                Debug.LogWarning("[DevCheats] NaturalEventDirector.dataManager is null.");
                return;
            }

            var profile = _director.levelProfiles.Find(p => p.targetLevel == _director.dataManager.currentLevel);
            if (profile == null)
            {
                Debug.LogWarning($"[DevCheats] No HazardLevelProfile for level {_director.dataManager.currentLevel}.");
                return;
            }

            float origLandslide = profile.landslideWeight;
            float origTornado = profile.tornadoWeight;
            profile.landslideWeight = landslide ? 1f : 0f;
            profile.tornadoWeight = landslide ? 0f : 1f;

            try
            {
                var method = typeof(NaturalEventDirector).GetMethod(
                    "PickAndTriggerHazard",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                {
                    Debug.LogWarning("[DevCheats] PickAndTriggerHazard not found via reflection.");
                    return;
                }
                method.Invoke(_director, new object[] { profile });
            }
            finally
            {
                profile.landslideWeight = origLandslide;
                profile.tornadoWeight = origTornado;
            }
        }

        private static GUIStyle _richLabel;
        private static GUIStyle RichLabel()
        {
            if (_richLabel == null)
            {
                _richLabel = new GUIStyle(GUI.skin.label) { richText = true };
            }
            return _richLabel;
        }

        private void OnDisable() => RestoreSpeedIfApplied();
        private void OnDestroy() => RestoreSpeedIfApplied();
        private void OnApplicationQuit() => RestoreSpeedIfApplied();
    }
}
#endif
