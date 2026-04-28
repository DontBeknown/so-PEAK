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

    [Header("A* Optional")]
    public HJB.Pathfind.AStarPathSolver aStarSolver;
    public HJBPathVisualizer aStarVisualizer; // Assign a separate visualizer (with e.g. blue line) for A*

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

    bool isPathToggledOn;
    public static HJBClickPathController Instance { get; private set; }

    // Cached path data for manual drawing or exporting to save file (supports up to 3 levels)
    [HideInInspector] public Dictionary<WorldLevel, List<Vector3>> savedPathsByLevel = new Dictionary<WorldLevel, List<Vector3>>();

    // Cached A* path data
    [HideInInspector] public Dictionary<WorldLevel, List<Vector3>> savedAStarPathsByLevel = new Dictionary<WorldLevel, List<Vector3>>();

    void OnEnable()
    {
        Instance = this;

        if (SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.OnWorldLoaded += HandleWorldLoaded;
        }

        LoadPathsFromCurrentSave();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;

        if (SaveLoadService.Instance != null)
        {
            SaveLoadService.Instance.OnWorldLoaded -= HandleWorldLoaded;
        }

        if (visualizer != null)
        {
            visualizer.CancelFadeAnimation(false);
        }
        if (aStarVisualizer != null)
        {
            aStarVisualizer.CancelFadeAnimation(false);
        }

        isPathToggledOn = false;
    }

    public void HidePath()
    {
        if (visualizer == null) return;
        visualizer.CancelFadeAnimation(false);
        visualizer.Clear();
        isPathToggledOn = false;
    }

    void Update()
    {
#if UNITY_EDITOR
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
#endif
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
                if (aStarVisualizer != null) aStarVisualizer.HidePathWithFade(localFadeOut, true);
            }
            else
            {
                visualizer.CancelFadeAnimation(false);
                visualizer.Clear();
                if (aStarVisualizer != null)
                {
                    aStarVisualizer.CancelFadeAnimation(false);
                    aStarVisualizer.Clear();
                }
            }

            isPathToggledOn = false;
            return;
        }

        WorldLevel currentLvl = provider.worldDataManager.currentLevel;
        bool hasHJBPath = savedPathsByLevel.TryGetValue(currentLvl, out var calculatedPathData) && calculatedPathData != null && calculatedPathData.Count > 0;

        List<Vector3> calculatedAStarPathData = null;
        bool hasAStarPath = false;

        if (aStarVisualizer != null && savedAStarPathsByLevel.TryGetValue(currentLvl, out calculatedAStarPathData) && calculatedAStarPathData != null && calculatedAStarPathData.Count > 0)
        {
            hasAStarPath = true;
        }

        if (!hasHJBPath || !hasAStarPath)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLvl}! Press P to calculate first.");
            return;
        }

        //Debug.Log($"[HJBClickPath] Toggling path ON for {currentLvl}.");
        if (hasHJBPath) visualizer.DrawPathWorld(calculatedPathData);
        if (hasAStarPath && aStarVisualizer != null) aStarVisualizer.DrawPathWorld(calculatedAStarPathData);

        if (useFadeWhenDrawing)
        {
            if (localDisplay > 0f)
            {
                var sequence = hasHJBPath ? visualizer.AnimatePathFadeInOut(localFadeIn, localDisplay, localFadeOut) : null;
                var aStarSequence = (hasAStarPath && aStarVisualizer != null) ? aStarVisualizer.AnimatePathFadeInOut(localFadeIn, localDisplay, localFadeOut) : null;

                if (sequence != null)
                {
                    sequence.OnComplete(() =>
                    {
                        visualizer.Clear();
                        if (aStarVisualizer != null) aStarVisualizer.Clear();
                        isPathToggledOn = false;
                    });
                }
                else
                {
                    visualizer.Clear();
                    if (aStarVisualizer != null) aStarVisualizer.Clear();
                    isPathToggledOn = false;
                }

                return;
            }

            if (hasHJBPath) visualizer.ShowPathWithFade(localFadeIn);
            if (hasAStarPath && aStarVisualizer != null) aStarVisualizer.ShowPathWithFade(localFadeIn);
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

        // The snapshot's pathStart is the single source of truth so the live spawn-to-peak
        // flow and the background batch flow agree on the same start cell for this level.
        Vector2Int g;
        if (TryGetCurrentSnapshot(out var snap))
        {
            g = snap.pathStart;
            world = provider.GridToWorld(g.x, g.y);
        }
        else
        {
            g = provider.WorldToGrid(world);
        }

        start = g;
        SpawnMarker(ref startMarker, startMarkerPrefab, world);
        Debug.Log($"[HJBClickPath] Start set to snapshot pathStart at {g}");
    }

    bool TryGetCurrentSnapshot(out LevelTerrainSnapshot snap)
    {
        snap = null;
        if (provider == null || provider.worldDataManager == null) return false;
        if (provider.levelSnapshots == null) return false;

        WorldLevel lvl = provider.worldDataManager.currentLevel;
        if (!provider.levelSnapshots.ContainsKey(lvl))
        {
            provider.PreloadSnapshotForLevel(lvl);
        }
        return provider.levelSnapshots.TryGetValue(lvl, out snap) && snap != null;
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
            Debug.Log($"[HJBClickPath] Path calculation complete for {currentLvl}.");
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
            bool hasCachedPath = savedPathsByLevel.TryGetValue(level, out var cachedPath)
                                 && cachedPath != null && cachedPath.Count > 0;

            if (hasCachedPath)
            {
                if (loadingScreen != null)
                {
                    loadingScreen.SetStatus($"Terrain {level}: path already cached, skipping...");
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
                Debug.Log($"[HJBClickPath] Skipping {level} — path already cached.");
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

        Debug.Log($"[HJBClickPath] Starting path pre-calculation for {level}...");

        provider.ApplySnapshot(level);
        solver.cost.Build();

        Vector2Int nextStart = snap.pathStart;
        Vector2Int nextGoal = snap.pathGoal;

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
            Debug.Log($"[HJBClickPath] Background path calculation finished for {level}! Generated {path.Count} waypoints.");
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
        Debug.Log($"[HJBClickPath] Goal set to Mountain Peak at {peakCoord}");
    }

    void TrySolvePath()
    {
        if (start != null && goal != null)
        {
            WorldLevel currentLvl = provider.worldDataManager.currentLevel;
            bool hasHJBPath = savedPathsByLevel.TryGetValue(currentLvl, out var cachedPath) && cachedPath != null && cachedPath.Count > 0;
            bool hasAStarPath = savedAStarPathsByLevel.TryGetValue(currentLvl, out var cachedAStarPath) && cachedAStarPath != null && cachedAStarPath.Count > 0;

            if (hasHJBPath && (aStarSolver == null || hasAStarPath))
            {
                Debug.Log($"[HJBClickPath] Valid cached path already loaded for {currentLvl}. Skipping recalculation.");
                return;
            }

            Debug.Log("Solving path from ClickController...");

            // First ensure cost surface is built (this happens very quickly)
            if (solver.cost.baseSpeed == null)
            {
                solver.cost.Build();
            }

            // Capture the important values now so the background task doesn't observe mutated state later
            var levelAtStart = currentLvl;
            var startAt = start.Value;
            var goalAt = goal.Value;

            solver.startPos = startAt;

            // Optional: Show some UI loading indication here

            // Run the solvers in a background task
            System.Threading.Tasks.Task.Run(() =>
            {
                solver.Solve(goal.Value);

                List<Vector3> aStarResult = null;
                if (aStarSolver != null)
                {
                    aStarResult = aStarSolver.Solve(start.Value, goal.Value);
                }

                return aStarResult;
            }).ContinueWith(t =>
            {
                // Retrieve but DO NOT draw the path immediately
                var generatedPath = backtracker.BuildPath(start.Value, goal.Value);
                var generatedAStarPath = t.Result; // from Task return

                // Store safely in the dictionary based on current WorldLevel
                currentLvl = provider.worldDataManager.currentLevel;
                savedPathsByLevel[currentLvl] = generatedPath;
                if (generatedAStarPath != null)
                {
                    savedAStarPathsByLevel[currentLvl] = generatedAStarPath;
                }

                PersistPathsToCurrentSave(true);

                Debug.Log($"[HJBClickPath] Background path calculation finished for {currentLvl}! Generated HJB waypoints: {generatedPath.Count}, AStar waypoints: {generatedAStarPath?.Count ?? 0}. Ready to be saved or drawn.");
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

        bool drewAnything = false;
        if (savedPathsByLevel.ContainsKey(currentLvl) && savedPathsByLevel[currentLvl] != null && savedPathsByLevel[currentLvl].Count > 0)
        {
            var calculatedPathData = savedPathsByLevel[currentLvl];
            Debug.Log($"[HJBClickPath] Drawing cached HJB path for {currentLvl} manually...");
            visualizer.DrawPathWorld(calculatedPathData);
            drewAnything = true;
        }

        if (aStarVisualizer != null && savedAStarPathsByLevel.ContainsKey(currentLvl) && savedAStarPathsByLevel[currentLvl] != null && savedAStarPathsByLevel[currentLvl].Count > 0)
        {
            var calculatedPathDataAstar = savedAStarPathsByLevel[currentLvl];
            Debug.Log($"[HJBClickPath] Drawing cached AStar path for {currentLvl} manually...");
            aStarVisualizer.DrawPathWorld(calculatedPathDataAstar);
            drewAnything = true;
        }

        if (!drewAnything)
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

        List<Vector3> calculatedPathData = null;
        bool hasHJBPath = savedPathsByLevel.TryGetValue(currentLvl, out calculatedPathData) && calculatedPathData != null && calculatedPathData.Count > 0;

        List<Vector3> calculatedAStarPathData = null;
        bool hasAStarPath = aStarVisualizer != null && savedAStarPathsByLevel.TryGetValue(currentLvl, out calculatedAStarPathData) && calculatedAStarPathData != null && calculatedAStarPathData.Count > 0;

        if (!hasHJBPath && !hasAStarPath)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLvl}! Press P to calculate first.");
            return;
        }

        float localFadeIn = fadeInSeconds >= 0f ? fadeInSeconds : fadeInDuration;
        float localHold = holdSeconds >= 0f ? holdSeconds : holdDuration;
        float localFadeOut = fadeOutSeconds >= 0f ? fadeOutSeconds : fadeOutDuration;

        if (hasHJBPath)
        {
            Debug.Log($"[HJBClickPath] Drawing cached HJB path for {currentLvl} with fade...");
            visualizer.DrawPathWorld(calculatedPathData);
            visualizer.AnimatePathFadeInOut(localFadeIn, localHold, localFadeOut);
        }

        if (hasAStarPath && calculatedAStarPathData != null)
        {
            Debug.Log($"[HJBClickPath] Drawing cached AStar path for {currentLvl} with fade...");
            aStarVisualizer.DrawPathWorld(calculatedAStarPathData);
            aStarVisualizer.AnimatePathFadeInOut(localFadeIn, localHold, localFadeOut);
        }
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
}