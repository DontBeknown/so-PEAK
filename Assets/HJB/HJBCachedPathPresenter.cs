using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class HJBCachedPathPresenter
{
    readonly HJBPathCacheStore cacheStore;
    readonly System.Action<string> debugLog;

    public bool IsPathToggledOn { get; private set; }

    public HJBCachedPathPresenter(HJBPathCacheStore cacheStore, System.Action<string> debugLog)
    {
        this.cacheStore = cacheStore;
        this.debugLog = debugLog;
    }

    public void ResetToggleState()
    {
        IsPathToggledOn = false;
    }

    public void HidePath(HJBPathVisualizer visualizer)
    {
        if (visualizer == null)
        {
            return;
        }

        visualizer.CancelFadeAnimation(false);
        visualizer.Clear();
        IsPathToggledOn = false;
    }

    public void CancelFadeAnimations(
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool aStarVisualEnabled)
    {
        if (visualizer != null)
        {
            visualizer.CancelFadeAnimation(false);
        }

        if (aStarVisualEnabled && aStarVisualizer != null)
        {
            aStarVisualizer.CancelFadeAnimation(false);
        }

        IsPathToggledOn = false;
    }

    public void ToggleCachedPathDisplay(
        HJBMeshDataProvider provider,
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool aStarVisualEnabled,
        bool useFadeWhenDrawing,
        float fadeInDuration,
        float holdDuration,
        float fadeOutDuration,
        float fadeInSeconds = -1f,
        float displaySeconds = -1f,
        float fadeOutSeconds = -1f)
    {
        if (!ValidateWorldAndVisualizer(provider, visualizer, "toggle path"))
        {
            return;
        }

        float localFadeIn = fadeInSeconds >= 0f ? fadeInSeconds : fadeInDuration;
        float localDisplay = displaySeconds >= 0f ? displaySeconds : holdDuration;
        float localFadeOut = fadeOutSeconds >= 0f ? fadeOutSeconds : fadeOutDuration;

        if (IsPathToggledOn)
        {
            HideVisiblePaths(visualizer, aStarVisualizer, aStarVisualEnabled, useFadeWhenDrawing, localFadeOut);
            IsPathToggledOn = false;
            return;
        }

        WorldLevel currentLevel = provider.worldDataManager.currentLevel;
        bool hasHjbPath = TryGetCachedPath(cacheStore.HjbPathsByLevel, currentLevel, out var hjbPath);
        List<Vector3> aStarPath = null;
        bool hasAStarPath = aStarVisualEnabled
                            && TryGetCachedPath(cacheStore.AStarPathsByLevel, currentLevel, out aStarPath);

        if (!hasHjbPath && !hasAStarPath)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLevel}! Press P to calculate first.");
            return;
        }

        DrawAvailablePaths(visualizer, aStarVisualizer, hasHjbPath, hjbPath, hasAStarPath, aStarPath);

        if (useFadeWhenDrawing)
        {
            AnimateToggledPaths(visualizer, aStarVisualizer, aStarVisualEnabled, hasHjbPath, hasAStarPath, localFadeIn, localDisplay, localFadeOut);
            if (localDisplay > 0f)
            {
                return;
            }
        }

        IsPathToggledOn = true;
    }

    public void DrawCachedPath(
        HJBMeshDataProvider provider,
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool aStarVisualEnabled)
    {
        if (!ValidateWorldAndVisualizer(provider, visualizer, "draw path"))
        {
            return;
        }

        WorldLevel currentLevel = provider.worldDataManager.currentLevel;
        bool drewAnything = false;

        if (TryGetCachedPath(cacheStore.HjbPathsByLevel, currentLevel, out var hjbPath))
        {
            debugLog?.Invoke($"[HJBClickPath] Drawing cached HJB path for {currentLevel} manually...");
            visualizer.DrawPathWorld(hjbPath);
            drewAnything = true;
        }

        if (aStarVisualEnabled && TryGetCachedPath(cacheStore.AStarPathsByLevel, currentLevel, out var aStarPath))
        {
            debugLog?.Invoke($"[HJBClickPath] Drawing cached AStar path for {currentLevel} manually...");
            aStarVisualizer.DrawPathWorld(aStarPath);
            drewAnything = true;
        }

        if (!drewAnything)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLevel}! Press P to calculate first.");
        }
    }

    public void DrawCachedPathWithFade(
        HJBMeshDataProvider provider,
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool aStarVisualEnabled,
        float defaultFadeInDuration,
        float defaultHoldDuration,
        float defaultFadeOutDuration,
        float fadeInSeconds = -1f,
        float holdSeconds = -1f,
        float fadeOutSeconds = -1f)
    {
        if (!ValidateWorldAndVisualizer(provider, visualizer, "draw path with fade"))
        {
            return;
        }

        WorldLevel currentLevel = provider.worldDataManager.currentLevel;
        bool hasHjbPath = TryGetCachedPath(cacheStore.HjbPathsByLevel, currentLevel, out var hjbPath);
        List<Vector3> aStarPath = null;
        bool hasAStarPath = aStarVisualEnabled
                            && TryGetCachedPath(cacheStore.AStarPathsByLevel, currentLevel, out aStarPath);

        if (!hasHjbPath && !hasAStarPath)
        {
            Debug.LogWarning($"[HJBClickPath] No path data cached to draw for {currentLevel}! Press P to calculate first.");
            return;
        }

        float localFadeIn = fadeInSeconds >= 0f ? fadeInSeconds : defaultFadeInDuration;
        float localHold = holdSeconds >= 0f ? holdSeconds : defaultHoldDuration;
        float localFadeOut = fadeOutSeconds >= 0f ? fadeOutSeconds : defaultFadeOutDuration;

        if (hasHjbPath)
        {
            debugLog?.Invoke($"[HJBClickPath] Drawing cached HJB path for {currentLevel} with fade...");
            visualizer.DrawPathWorld(hjbPath);
            visualizer.AnimatePathFadeInOut(localFadeIn, localHold, localFadeOut);
        }

        if (hasAStarPath)
        {
            debugLog?.Invoke($"[HJBClickPath] Drawing cached AStar path for {currentLevel} with fade...");
            aStarVisualizer.DrawPathWorld(aStarPath);
            aStarVisualizer.AnimatePathFadeInOut(localFadeIn, localHold, localFadeOut);
        }
    }

    static bool TryGetCachedPath(
        Dictionary<WorldLevel, List<Vector3>> pathsByLevel,
        WorldLevel level,
        out List<Vector3> path)
    {
        return pathsByLevel.TryGetValue(level, out path) && path != null && path.Count > 0;
    }

    static bool ValidateWorldAndVisualizer(HJBMeshDataProvider provider, HJBPathVisualizer visualizer, string action)
    {
        if (provider == null || provider.worldDataManager == null)
        {
            Debug.LogWarning($"[HJBClickPath] Cannot {action} because world data is missing.");
            return false;
        }

        if (visualizer == null)
        {
            Debug.LogWarning($"[HJBClickPath] Cannot {action} because visualizer is missing.");
            return false;
        }

        return true;
    }

    void HideVisiblePaths(
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool aStarVisualEnabled,
        bool useFadeWhenDrawing,
        float fadeOutSeconds)
    {
        if (useFadeWhenDrawing)
        {
            visualizer.HidePathWithFade(fadeOutSeconds, true);
            if (aStarVisualEnabled)
            {
                aStarVisualizer.HidePathWithFade(fadeOutSeconds, true);
            }

            return;
        }

        visualizer.CancelFadeAnimation(false);
        visualizer.Clear();
        if (aStarVisualEnabled)
        {
            aStarVisualizer.CancelFadeAnimation(false);
            aStarVisualizer.Clear();
        }
    }

    void DrawAvailablePaths(
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool hasHjbPath,
        List<Vector3> hjbPath,
        bool hasAStarPath,
        List<Vector3> aStarPath)
    {
        if (hasHjbPath)
        {
            visualizer.DrawPathWorld(hjbPath);
        }

        if (hasAStarPath)
        {
            aStarVisualizer.DrawPathWorld(aStarPath);
        }
    }

    void AnimateToggledPaths(
        HJBPathVisualizer visualizer,
        HJBPathVisualizer aStarVisualizer,
        bool aStarVisualEnabled,
        bool hasHjbPath,
        bool hasAStarPath,
        float fadeInSeconds,
        float displaySeconds,
        float fadeOutSeconds)
    {
        if (displaySeconds > 0f)
        {
            Sequence sequence = hasHjbPath
                ? visualizer.AnimatePathFadeInOut(fadeInSeconds, displaySeconds, fadeOutSeconds)
                : null;

            if (hasAStarPath)
            {
                aStarVisualizer.AnimatePathFadeInOut(fadeInSeconds, displaySeconds, fadeOutSeconds);
            }

            if (sequence != null)
            {
                sequence.OnComplete(() =>
                {
                    visualizer.Clear();
                    if (aStarVisualEnabled)
                    {
                        aStarVisualizer.Clear();
                    }

                    IsPathToggledOn = false;
                });
            }
            else
            {
                visualizer.Clear();
                if (aStarVisualEnabled)
                {
                    aStarVisualizer.Clear();
                }

                IsPathToggledOn = false;
            }

            return;
        }

        if (hasHjbPath)
        {
            visualizer.ShowPathWithFade(fadeInSeconds);
        }

        if (hasAStarPath)
        {
            aStarVisualizer.ShowPathWithFade(fadeInSeconds);
        }
    }
}
