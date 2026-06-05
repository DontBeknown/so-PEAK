using System.Collections;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Threading.Tasks;
using Game.UI;
using UnityEngine;

public class HJBPathCalculationRunner
{
    readonly HJBMeshDataProvider provider;
    readonly HJBPathSolver solver;
    readonly HJBBacktracker backtracker;
    readonly HJB.Pathfind.AStarPathSolver aStarSolver;
    readonly HJBPathCacheStore cacheStore;
    readonly System.Func<bool> aStarEnabled;
    readonly System.Action<string> debugLog;

    class PathCalculationResult
    {
        public List<Vector3> AStarPath;
        public double HjbSolveMilliseconds = -1d;
        public double AStarMilliseconds = -1d;
    }

    public HJBPathCalculationRunner(
        HJBMeshDataProvider provider,
        HJBPathSolver solver,
        HJBBacktracker backtracker,
        HJB.Pathfind.AStarPathSolver aStarSolver,
        HJBPathCacheStore cacheStore,
        System.Func<bool> aStarEnabled,
        System.Action<string> debugLog)
    {
        this.provider = provider;
        this.solver = solver;
        this.backtracker = backtracker;
        this.aStarSolver = aStarSolver;
        this.cacheStore = cacheStore;
        this.aStarEnabled = aStarEnabled;
        this.debugLog = debugLog;
    }

    public void TrySolveCurrentPath(Vector2Int? start, Vector2Int? goal)
    {
        if (start == null || goal == null)
        {
            return;
        }

        WorldLevel currentLevel = provider.worldDataManager.currentLevel;
        bool needsHjbPath = !HJBPathCacheStore.HasCachedPath(cacheStore.HjbPathsByLevel, currentLevel);
        bool needsAStarPath = aStarEnabled() && !HJBPathCacheStore.HasCachedPath(cacheStore.AStarPathsByLevel, currentLevel);

        if (!needsHjbPath && !needsAStarPath)
        {
            debugLog?.Invoke($"[HJBClickPath] Valid cached path already loaded for {currentLevel}. Skipping recalculation.");
            return;
        }

        debugLog?.Invoke("Solving path from ClickController...");

        EnsureCostSurfaceBuilt();

        WorldLevel levelAtStart = currentLevel;
        Vector2Int startAt = start.Value;
        Vector2Int goalAt = goal.Value;

        solver.startPos = startAt;

        Task.Run(() =>
        {
            var result = new PathCalculationResult();

            if (needsHjbPath)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                solver.Solve(goalAt);
                stopwatch.Stop();
                result.HjbSolveMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            }

            if (needsAStarPath)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                result.AStarPath = aStarSolver.Solve(startAt, goalAt);
                stopwatch.Stop();
                result.AStarMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            }

            return result;
        }).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogException(t.Exception?.GetBaseException() ?? t.Exception);
                return;
            }

            PathCalculationResult result = t.Result;
            double hjbBacktrackMilliseconds = -1d;

            List<Vector3> generatedHjbPath = null;
            if (needsHjbPath)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                generatedHjbPath = backtracker.BuildPath(startAt, goalAt);
                stopwatch.Stop();
                hjbBacktrackMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                cacheStore.HjbPathsByLevel[levelAtStart] = generatedHjbPath;
            }

            List<Vector3> generatedAStarPath = result.AStarPath;
            if (generatedAStarPath != null)
            {
                cacheStore.AStarPathsByLevel[levelAtStart] = generatedAStarPath;
            }

            cacheStore.PersistToCurrentSave(true);

            LogCalculationTiming(levelAtStart, needsHjbPath, needsAStarPath, result.HjbSolveMilliseconds, hjbBacktrackMilliseconds, result.AStarMilliseconds);
            debugLog?.Invoke($"[HJBClickPath] Background path calculation finished for {levelAtStart}! Generated HJB waypoints: {generatedHjbPath?.Count ?? 0}, AStar waypoints: {generatedAStarPath?.Count ?? 0}. Ready to be saved or drawn.");
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public IEnumerator WaitForRequiredPaths(WorldLevel level, float timeoutSeconds)
    {
        float timer = 0f;
        while (!cacheStore.HasRequiredCachedPathsForLevel(level, aStarEnabled()) && timer < timeoutSeconds)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!cacheStore.HasRequiredCachedPathsForLevel(level, aStarEnabled()))
        {
            Debug.LogWarning($"[HJBClickPath] Required path calculation timed out for {level}!");
        }
        else
        {
            Debug.Log($"[HJBClickPath] Required path calculation complete for {level}.");
        }
    }

    public IEnumerator PreloadLevelSnapshotsCoroutine(NextLevelLoadingScreen loadingScreen)
    {
        var levels = System.Enum.GetValues(typeof(WorldLevel));
        int total = levels.Length;
        int i = 0;

        foreach (WorldLevel level in levels)
        {
            bool hasCachedPath = HJBPathCacheStore.HasCachedPath(cacheStore.HjbPathsByLevel, level);
            bool hasCachedAStarPath = HJBPathCacheStore.HasCachedPath(cacheStore.AStarPathsByLevel, level);

            if (hasCachedPath && (!aStarEnabled() || hasCachedAStarPath))
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

            yield return null;
            provider.PreloadSnapshotForLevel(level);
            i++;
        }

        if (loadingScreen != null)
        {
            loadingScreen.SetStatus("Terrain data ready.");
            loadingScreen.SetProgress(1f);
        }
    }

    public IEnumerator BackgroundCalculateAllLevelPaths()
    {
        if (provider.levelSnapshots.Count == 0)
        {
            provider.PreloadLevelSnapshots();
            yield return null;
        }

        foreach (WorldLevel level in System.Enum.GetValues(typeof(WorldLevel)))
        {
            bool hasHjbPath = HJBPathCacheStore.HasCachedPath(cacheStore.HjbPathsByLevel, level);
            bool hasAStarPath = HJBPathCacheStore.HasCachedPath(cacheStore.AStarPathsByLevel, level);

            if (hasHjbPath && (!aStarEnabled() || hasAStarPath))
            {
                debugLog?.Invoke($"[HJBClickPath] Skipping {level} - required paths already cached.");
                continue;
            }

            yield return CalculatePathForLevel(level);
        }
    }

    IEnumerator CalculatePathForLevel(WorldLevel level)
    {
        if (!provider.levelSnapshots.TryGetValue(level, out var snapshot))
        {
            Debug.LogWarning($"[HJBClickPath] No snapshot for {level}, skipping path pre-calculation.");
            yield break;
        }

        debugLog?.Invoke($"[HJBClickPath] Starting path pre-calculation for {level}...");

        provider.ApplySnapshot(level);
        solver.cost.Build();

        Vector2Int nextStart = snapshot.pathStart;
        Vector2Int nextGoal = snapshot.pathGoal;
        bool needsHjbPath = !HJBPathCacheStore.HasCachedPath(cacheStore.HjbPathsByLevel, level);
        bool needsAStarPath = aStarEnabled() && !HJBPathCacheStore.HasCachedPath(cacheStore.AStarPathsByLevel, level);

        if (!needsHjbPath && !needsAStarPath)
        {
            provider.RestoreCurrentLevelData();
            debugLog?.Invoke($"[HJBClickPath] Skipping {level} - required paths already cached.");
            yield break;
        }

        bool done = false;
        Task.Run(() =>
        {
            var result = new PathCalculationResult();

            if (needsHjbPath)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                solver.Solve(nextGoal);
                stopwatch.Stop();
                result.HjbSolveMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            }

            if (needsAStarPath)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                result.AStarPath = aStarSolver.Solve(nextStart, nextGoal);
                stopwatch.Stop();
                result.AStarMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            }

            return result;
        }).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogException(t.Exception?.GetBaseException() ?? t.Exception);
                provider.RestoreCurrentLevelData();
                done = true;
                return;
            }

            PathCalculationResult result = t.Result;
            double hjbBacktrackMilliseconds = -1d;

            List<Vector3> hjbPath = null;
            if (needsHjbPath)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                hjbPath = backtracker.BuildPath(nextStart, nextGoal);
                stopwatch.Stop();
                hjbBacktrackMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                cacheStore.HjbPathsByLevel[level] = hjbPath;
            }

            if (needsAStarPath && result.AStarPath != null)
            {
                cacheStore.AStarPathsByLevel[level] = result.AStarPath;
            }

            cacheStore.PersistToCurrentSave(true);
            provider.RestoreCurrentLevelData();
            done = true;
            LogCalculationTiming(level, needsHjbPath, needsAStarPath, result.HjbSolveMilliseconds, hjbBacktrackMilliseconds, result.AStarMilliseconds);
            debugLog?.Invoke($"[HJBClickPath] Background path calculation finished for {level}! Generated HJB waypoints: {hjbPath?.Count ?? 0}, AStar waypoints: {result.AStarPath?.Count ?? 0}.");
        }, TaskScheduler.FromCurrentSynchronizationContext());

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

    void EnsureCostSurfaceBuilt()
    {
        if (solver.cost.baseSpeed == null)
        {
            solver.cost.Build();
        }
    }

    void LogCalculationTiming(
        WorldLevel level,
        bool measuredHjb,
        bool measuredAStar,
        double hjbSolveMilliseconds,
        double hjbBacktrackMilliseconds,
        double aStarMilliseconds)
    {
        var lines = new List<string>
        {
            $"[HJBClickPath] Path calculation timing for {level}:"
        };

        if (measuredHjb)
        {
            double hjbTotalMilliseconds = hjbSolveMilliseconds + hjbBacktrackMilliseconds;
            lines.Add($"HJB solve: {FormatDuration(hjbSolveMilliseconds)}");
            lines.Add($"HJB backtrack: {FormatDuration(hjbBacktrackMilliseconds)}");
            lines.Add($"HJB total: {FormatDuration(hjbTotalMilliseconds)}");
        }

        if (measuredAStar)
        {
            lines.Add($"A* solve/path: {FormatDuration(aStarMilliseconds)}");
        }

        string message = string.Join("\n", lines);
        if (debugLog != null)
        {
            debugLog.Invoke(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    static string FormatDuration(double milliseconds)
    {
        if (milliseconds < 0d)
        {
            return "not measured";
        }

        int totalMilliseconds = Mathf.RoundToInt((float)milliseconds);
        int minutes = totalMilliseconds / 60000;
        int seconds = (totalMilliseconds % 60000) / 1000;
        int remainingMilliseconds = totalMilliseconds % 1000;

        return $"{minutes}m {seconds}s {remainingMilliseconds}ms";
    }
}
