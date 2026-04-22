using System.Collections.Generic;
using DG.Tweening;
using Game.UI;
using UnityEngine;

public class HJBClickPathController : MonoBehaviour
{
    public Camera cam;

    public HJBMeshDataProvider provider;
    public HJBPathSolver solver;
    public HJBBacktracker backtracker;
    public HJBPathVisualizer visualizer;

    [Header("Markers")]
    public GameObject startMarkerPrefab;
    public GameObject goalMarkerPrefab;

    GameObject startMarker;
    GameObject goalMarker;

    Vector2Int? start = null;
    Vector2Int? goal = null;

    [Header("Player Reference")]
    public Transform playerTransform; // Assign in inspector or find dynamically

    [Header("Path Fade Settings")]
    public bool useFadeWhenDrawing = true;
    [Min(0f)] public float fadeInDuration = 0.4f;
    [Min(0f)] public float holdDuration = 3f;
    [Min(0f)] public float fadeOutDuration = 0.8f;

    [Header("Debug")]
    [Tooltip("Enables debug hotkeys and verbose pathing logs.")]
    public bool enableDebugLog;

    bool isPathToggledOn;

    // Cached path data for manual drawing or exporting to save file (supports up to 3 levels)
    [HideInInspector] public Dictionary<WorldLevel, List<Vector3>> savedPathsByLevel = new Dictionary<WorldLevel, List<Vector3>>();

    void OnEnable()
    {
        if (SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.OnWorldLoaded += HandleWorldLoaded;
        }

        LoadPathsFromCurrentSave();
        LoadSnapshotsFromCurrentSave();
    }

    void OnDisable()
    {
        if (SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.OnWorldLoaded -= HandleWorldLoaded;
        }

        if (visualizer != null)
        {
            visualizer.CancelFadeAnimation(false);
        }

        isPathToggledOn = false;
    }

    void Update()
    {
        if (!enableDebugLog)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            // Sync the solver step with the current active level height multiplier if needed, 
            // or just ensure the solver picks up latest terrain updates.
            if (provider.worldDataManager != null && provider.worldDataManager.activeGen != null)
            {
                provider.heightMultiplier = provider.worldDataManager.activeGen.meshHeightMultiplier;
            }

            SetStartToPlayer();
            SetGoalToPeak();
            TrySolvePath();
            
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleCachedPathDisplay(fadeInDuration, 0f, fadeOutDuration);
        }
    }

    public void ToggleCachedPathDisplay(float fadeInSeconds = -1f, float displaySeconds = -1f, float fadeOutSeconds = -1f)
    {
        if (provider == null || provider.worldDataManager == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot toggle path because world data is missing.");
            return;
        }

        if (visualizer == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot toggle path because visualizer is missing.");
            return;
        }

        float localFadeIn = fadeInSeconds >= 0f ? fadeInSeconds : fadeInDuration;
        float localDisplay = displaySeconds >= 0f ? displaySeconds : holdDuration;
        float localFadeOut = fadeOutSeconds >= 0f ? fadeOutSeconds : fadeOutDuration;

        if (isPathToggledOn)
        {
            if (useFadeWhenDrawing)
            {
                visualizer.HidePathWithFade(localFadeOut, true);
            }
            else
            {
                visualizer.CancelFadeAnimation(false);
                visualizer.Clear();
            }

            isPathToggledOn = false;
            return;
        }

        WorldLevel currentLvl = provider.worldDataManager.currentLevel;
        if (!savedPathsByLevel.TryGetValue(currentLvl, out var calculatedPathData) || calculatedPathData == null || calculatedPathData.Count == 0)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLvl}! Press P to calculate first.");
            return;
        }

        //Debug.Log($"[HJBClickPath] Toggling path ON for {currentLvl}.");
        visualizer.DrawPathWorld(calculatedPathData);
        if (useFadeWhenDrawing)
        {
            if (localDisplay > 0f)
            {
                var sequence = visualizer.AnimatePathFadeInOut(localFadeIn, localDisplay, localFadeOut);
                if (sequence != null)
                {
                    sequence.OnComplete(() =>
                    {
                        visualizer.Clear();
                        isPathToggledOn = false;
                    });
                }
                else
                {
                    visualizer.Clear();
                    isPathToggledOn = false;
                }

                return;
            }

            visualizer.ShowPathWithFade(localFadeIn);
        }

        isPathToggledOn = true;
    }


    public void SetStartToPlayer(Vector3? overrideWorldPos = null)
    {
        Vector3 world;
        if (overrideWorldPos.HasValue)
        {
            world = overrideWorldPos.Value;
        }
        else
        {
            if (playerTransform == null)
            {
                // Try to find the player if not assigned
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
                else if (provider.renderController != null && provider.renderController.player != null)
                {
                    playerTransform = provider.renderController.player;
                }
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[HJBClickPath] Cannot find player to set start position.");
                return;
            }
            world = playerTransform.position;
        }
        Vector2Int g = provider.WorldToGrid(world);
        start = g;
        SpawnMarker(ref startMarker, startMarkerPrefab, world);
        if (enableDebugLog) Debug.Log($"[HJBClickPath] Start set to {(overrideWorldPos.HasValue ? "override position" : "player")} at {g}");
    }
    // Coroutine to trigger path calculation and wait until path is ready for the current level
    public System.Collections.IEnumerator CalculatePathFromSpawnToPeak(Vector3 spawnWorldPos)
    {
        // Sync solver height multiplier if needed
        if (provider.worldDataManager != null && provider.worldDataManager.activeGen != null)
        {
            provider.heightMultiplier = provider.worldDataManager.activeGen.meshHeightMultiplier;
        }
        SetStartToPlayer(spawnWorldPos);
        SetGoalToPeak();
        TrySolvePath();

        // Wait until path is available in savedPathsByLevel for the current level
        WorldLevel currentLvl = provider.worldDataManager.currentLevel;
        float timeout = 3600f; // seconds
        float timer = 0f;
        while ((!savedPathsByLevel.ContainsKey(currentLvl) || savedPathsByLevel[currentLvl] == null || savedPathsByLevel[currentLvl].Count == 0) && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (!savedPathsByLevel.ContainsKey(currentLvl) || savedPathsByLevel[currentLvl] == null || savedPathsByLevel[currentLvl].Count == 0)
        {
            Debug.LogWarning($"[HJBClickPath] Path calculation timed out for {currentLvl}!");
        }
        else
        {
            if (enableDebugLog) Debug.Log($"[HJBClickPath] Path calculation complete for {currentLvl}.");
        }
    }

    public void PreloadLevelSnapshots() => provider.PreloadLevelSnapshots();

    public System.Collections.IEnumerator PreloadLevelSnapshotsCoroutine(NextLevelLoadingScreen loadingScreen)
    {
        var levels = System.Enum.GetValues(typeof(WorldLevel));
        int total = levels.Length;
        int i = 0;
        foreach (WorldLevel level in levels)
        {
            bool hasSnapshot  = provider.levelSnapshots.ContainsKey(level);
            bool hasCachedPath = savedPathsByLevel.TryGetValue(level, out var cachedPath)
                                 && cachedPath != null && cachedPath.Count > 0;

            if (hasSnapshot && hasCachedPath)
            {
                if (loadingScreen != null)
                {
                    loadingScreen.SetStatus($"Terrain {level}: snapshot + path already cached, skipping...");
                    loadingScreen.SetProgress((float)(i + 1) / total);
                }
                i++;
                yield return null;
                continue;
            }

            if (loadingScreen != null)
            {
                loadingScreen.SetStatus($"Pre-loading terrain: {level}...");
                loadingScreen.SetProgress((float)i / total);
            }
            yield return null; // let status message render before heavy work
            provider.PreloadSnapshotForLevel(level);
            i++;
        }

        PersistSnapshotsToCurrentSave(saveToFile: true);

        if (loadingScreen != null)
        {
            loadingScreen.SetStatus("Terrain data ready.");
            loadingScreen.SetProgress(1f);
        }
    }

    public void StartBackgroundPathCalculationForAllLevels()
    {
        StartCoroutine(BackgroundCalculateAllLevelPaths());
    }

    System.Collections.IEnumerator BackgroundCalculateAllLevelPaths()
    {
        if (provider.levelSnapshots.Count == 0)
        {
            provider.PreloadLevelSnapshots();
            yield return null;
        }

        foreach (WorldLevel level in System.Enum.GetValues(typeof(WorldLevel)))
        {
            if (savedPathsByLevel.ContainsKey(level) && savedPathsByLevel[level] != null && savedPathsByLevel[level].Count > 0)
            {
                if (enableDebugLog) Debug.Log($"[HJBClickPath] Skipping {level} — path already cached.");
                continue;
            }
            yield return CalculatePathForLevel(level);
        }
    }

    System.Collections.IEnumerator CalculatePathForLevel(WorldLevel level)
    {
        if (!provider.levelSnapshots.TryGetValue(level, out var snap))
        {
            Debug.LogWarning($"[HJBClickPath] No snapshot for {level}, skipping path pre-calculation.");
            yield break;
        }

    if (enableDebugLog) Debug.Log($"[HJBClickPath] Starting path pre-calculation for {level}...");

        provider.ApplySnapshot(level);
        solver.cost.Build();

        Vector2Int nextStart = snap.pathStart;
        Vector2Int nextGoal  = snap.pathGoal;

        bool done = false;
        System.Threading.Tasks.Task.Run(() =>
        {
            solver.Solve(nextGoal);
        }).ContinueWith(t =>
        {
            var path = backtracker.BuildPath(nextStart, nextGoal);
            savedPathsByLevel[level] = path;
            PersistPathsToCurrentSave(true);
            provider.RestoreCurrentLevelData();
            done = true;
            if (enableDebugLog) Debug.Log($"[HJBClickPath] Background path calculation finished for {level}! Generated {path.Count} waypoints.");
        }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());

        float timeout = 3600f;
        float timer = 0f;
        while (!done && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!done)
        {
            Debug.LogWarning($"[HJBClickPath] Path calculation timed out for {level}!");
            provider.RestoreCurrentLevelData();
        }
    }

    void SetGoalToPeak()
    {
        if (provider.worldDataManager == null || provider.worldDataManager.activeGen == null)
        {
            Debug.LogWarning("[HJBClickPath] WorldDataManager or activeGen not ready.");
            return;
        }

        // Get the main peak directly from the generator
        Vector2Int peakCoord = provider.worldDataManager.activeGen.mainPeak;
        
        goal = peakCoord;
        Vector3 worldPos = provider.GridToWorld(peakCoord.x, peakCoord.y);

        SpawnMarker(ref goalMarker, goalMarkerPrefab, worldPos);
        if (enableDebugLog) Debug.Log($"[HJBClickPath] Goal set to Mountain Peak at {peakCoord}");
    }

    void TrySolvePath()
    {
        if (start != null && goal != null)
        {
            WorldLevel currentLvl = provider.worldDataManager.currentLevel;
            if (savedPathsByLevel.TryGetValue(currentLvl, out var cachedPath) && cachedPath != null && cachedPath.Count > 0)
            {
                if (enableDebugLog) Debug.Log($"[HJBClickPath] Valid cached path already loaded for {currentLvl}. Skipping recalculation.");
                return;
            }

            if (enableDebugLog) Debug.Log("Solving path from ClickController...");
            
            // First ensure cost surface is built (this happens very quickly)
            if (solver.cost.baseSpeed == null) 
            {
                solver.cost.Build();
            }

            solver.startPos = start.Value;

            // Optional: Show some UI loading indication here

            // Run the solver in a background task
            System.Threading.Tasks.Task.Run(() =>
            {
                solver.Solve(goal.Value);
            }).ContinueWith(t =>
            {
                // Retrieve but DO NOT draw the path immediately
                var generatedPath = backtracker.BuildPath(start.Value, goal.Value);
                
                // Store safely in the dictionary based on current WorldLevel
                currentLvl = provider.worldDataManager.currentLevel;
                savedPathsByLevel[currentLvl] = generatedPath;
                PersistPathsToCurrentSave(true);

                if (enableDebugLog) Debug.Log($"[HJBClickPath] Background path calculation finished for {currentLvl}! Generated {generatedPath.Count} waypoints. Ready to be saved or drawn.");
            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public void DrawCachedPath()
    {
        if (provider.worldDataManager == null) return;
        if (visualizer == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot draw path because visualizer is missing.");
            return;
        }

        WorldLevel currentLvl = provider.worldDataManager.currentLevel;

        if (savedPathsByLevel.ContainsKey(currentLvl) && savedPathsByLevel[currentLvl] != null && savedPathsByLevel[currentLvl].Count > 0)
        {
            var calculatedPathData = savedPathsByLevel[currentLvl];
            if (enableDebugLog) Debug.Log($"[HJBClickPath] Drawing cached path for {currentLvl} manually...");
            visualizer.DrawPathWorld(calculatedPathData);
        }
        else
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLvl}! Press P to calculate first.");
        }
    }

    public void DrawCachedPathWithFade(float fadeInSeconds = -1f, float holdSeconds = -1f, float fadeOutSeconds = -1f)
    {
        if (provider.worldDataManager == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot draw path with fade because world data is missing.");
            return;
        }

        if (visualizer == null)
        {
            Debug.LogWarning("[HJBClickPath] Cannot draw path with fade because visualizer is missing.");
            return;
        }

        WorldLevel currentLvl = provider.worldDataManager.currentLevel;
        if (!savedPathsByLevel.TryGetValue(currentLvl, out var calculatedPathData) || calculatedPathData == null || calculatedPathData.Count == 0)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLvl}! Press P to calculate first.");
            return;
        }

        if (enableDebugLog) Debug.Log($"[HJBClickPath] Drawing cached path for {currentLvl} with fade...");
        visualizer.DrawPathWorld(calculatedPathData);

        float localFadeIn = fadeInSeconds >= 0f ? fadeInSeconds : fadeInDuration;
        float localHold = holdSeconds >= 0f ? holdSeconds : holdDuration;
        float localFadeOut = fadeOutSeconds >= 0f ? fadeOutSeconds : fadeOutDuration;

        visualizer.AnimatePathFadeInOut(localFadeIn, localHold, localFadeOut);
    }

    void SpawnMarker(ref GameObject marker,
                     GameObject prefab,
                     Vector3 pos)
    {
        pos.y += 1f;

        if (marker == null && prefab != null)
        {
            marker = Instantiate(prefab, pos, Quaternion.identity);
        }
        else
        {
            marker.transform.position = pos;
        }
    }

    void HandleWorldLoaded(WorldSaveData loadedSave)
    {
        isPathToggledOn = false;
        LoadPathsFromCurrentSave();
        LoadSnapshotsFromCurrentSave();
    }

    void PersistPathsToCurrentSave(bool saveToFile = false)
    {
        if (SaveLoadService.Instance == null || SaveLoadService.Instance.CurrentWorldSave == null)
        {
            return;
        }

        var worldState = SaveLoadService.Instance.CurrentWorldSave.worldState;
        if (worldState == null)
        {
            return;
        }

        var serializedPaths = new List<LevelPathSaveData>();
        foreach (var kvp in savedPathsByLevel)
        {
            var pathEntry = new LevelPathSaveData
            {
                level = (int)kvp.Key,
                waypoints = new List<Vector3SaveData>()
            };

            if (kvp.Value != null)
            {
                foreach (var point in kvp.Value)
                {
                    pathEntry.waypoints.Add(new Vector3SaveData
                    {
                        x = point.x,
                        y = point.y,
                        z = point.z
                    });
                }
            }

            serializedPaths.Add(pathEntry);
        }

        worldState.cachedPathsByLevel = serializedPaths;

        if (saveToFile)
        {
            SaveLoadService.Instance.SaveWorld(SaveLoadService.Instance.CurrentWorldSave, refreshFreshLevelEntryFlag: false);
        }
    }

    void LoadPathsFromCurrentSave()
    {
        if (SaveLoadService.Instance == null || SaveLoadService.Instance.CurrentWorldSave == null)
        {
            return;
        }

        var worldState = SaveLoadService.Instance.CurrentWorldSave.worldState;
        if (worldState == null || worldState.cachedPathsByLevel == null)
        {
            return;
        }

        savedPathsByLevel.Clear();

        foreach (var levelPath in worldState.cachedPathsByLevel)
        {
            if (levelPath == null)
            {
                continue;
            }

            if (!System.Enum.IsDefined(typeof(WorldLevel), levelPath.level))
            {
                continue;
            }

            var waypoints = new List<Vector3>();
            if (levelPath.waypoints != null)
            {
                foreach (var point in levelPath.waypoints)
                {
                    if (point == null)
                    {
                        continue;
                    }

                    waypoints.Add(new Vector3(point.x, point.y, point.z));
                }
            }

            savedPathsByLevel[(WorldLevel)levelPath.level] = waypoints;
        }
    }

    void PersistSnapshotsToCurrentSave(bool saveToFile = false)
    {
        if (SaveLoadService.Instance == null || SaveLoadService.Instance.CurrentWorldSave == null)
            return;

        var worldState = SaveLoadService.Instance.CurrentWorldSave.worldState;
        if (worldState == null)
            return;

        var serialized = new List<LevelSnapshotSaveData>();
        foreach (var kvp in provider.levelSnapshots)
        {
            var snap = kvp.Value;
            serialized.Add(new LevelSnapshotSaveData
            {
                level            = (int)kvp.Key,
                width            = snap.width,
                height           = snap.height,
                heightMultiplier = snap.heightMultiplier,
                pathGoalX        = snap.pathGoal.x,
                pathGoalY        = snap.pathGoal.y,
                pathStartX       = snap.pathStart.x,
                pathStartY       = snap.pathStart.y,
                heightMap        = FlattenMap(snap.heightMap, snap.width, snap.height),
                slopeMap         = FlattenMap(snap.slopeMap,  snap.width, snap.height),
            });
        }

        worldState.cachedSnapshotsByLevel = serialized;

        if (saveToFile)
            SaveLoadService.Instance.SaveWorld(SaveLoadService.Instance.CurrentWorldSave,
                                               refreshFreshLevelEntryFlag: false);
    }

    void LoadSnapshotsFromCurrentSave()
    {
        if (SaveLoadService.Instance == null || SaveLoadService.Instance.CurrentWorldSave == null)
            return;

        var worldState = SaveLoadService.Instance.CurrentWorldSave.worldState;
        if (worldState == null || worldState.cachedSnapshotsByLevel == null)
            return;

        provider.levelSnapshots.Clear();

        foreach (var entry in worldState.cachedSnapshotsByLevel)
        {
            if (entry == null)
                continue;
            if (!System.Enum.IsDefined(typeof(WorldLevel), entry.level))
                continue;

            provider.levelSnapshots[(WorldLevel)entry.level] = new LevelTerrainSnapshot
            {
                width            = entry.width,
                height           = entry.height,
                heightMultiplier = entry.heightMultiplier,
                pathGoal         = new Vector2Int(entry.pathGoalX, entry.pathGoalY),
                pathStart        = new Vector2Int(entry.pathStartX, entry.pathStartY),
                heightMap        = UnflattenMap(entry.heightMap, entry.width, entry.height),
                slopeMap         = UnflattenMap(entry.slopeMap,  entry.width, entry.height),
            };
        }
    }

    static List<float> FlattenMap(float[,] map, int w, int h)
    {
        var list = new List<float>(w * h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                list.Add(map[x, y]);
        return list;
    }

    static float[,] UnflattenMap(List<float> list, int w, int h)
    {
        var map = new float[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                map[x, y] = list[y * w + x];
        return map;
    }

}