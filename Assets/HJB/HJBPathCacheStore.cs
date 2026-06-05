using System.Collections.Generic;
using UnityEngine;

public class HJBPathCacheStore
{
    public readonly Dictionary<WorldLevel, List<Vector3>> HjbPathsByLevel;
    public readonly Dictionary<WorldLevel, List<Vector3>> AStarPathsByLevel;

    public HJBPathCacheStore(
        Dictionary<WorldLevel, List<Vector3>> hjbPathsByLevel,
        Dictionary<WorldLevel, List<Vector3>> aStarPathsByLevel)
    {
        HjbPathsByLevel = hjbPathsByLevel;
        AStarPathsByLevel = aStarPathsByLevel;
    }

    public bool HasRequiredCachedPathsForLevel(WorldLevel level, bool requiresAStarPath)
    {
        return HasCachedPath(HjbPathsByLevel, level)
               && (!requiresAStarPath || HasCachedPath(AStarPathsByLevel, level));
    }

    public bool HasMissingRequiredCachedPaths(bool requiresAStarPath)
    {
        foreach (WorldLevel level in System.Enum.GetValues(typeof(WorldLevel)))
        {
            if (!HasRequiredCachedPathsForLevel(level, requiresAStarPath))
            {
                return true;
            }
        }

        return false;
    }

    public void PersistToCurrentSave(bool saveToFile = false)
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

        worldState.cachedPathsByLevel = SerializePaths(HjbPathsByLevel);
        worldState.cachedAStarPathsByLevel = SerializePaths(AStarPathsByLevel);

        if (saveToFile)
        {
            SaveLoadService.Instance.SaveWorld(SaveLoadService.Instance.CurrentWorldSave, refreshFreshLevelEntryFlag: false);
        }
    }

    public void LoadFromCurrentSave()
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

        HjbPathsByLevel.Clear();
        AStarPathsByLevel.Clear();

        LoadSerializedPaths(worldState.cachedPathsByLevel, HjbPathsByLevel);
        LoadSerializedPaths(worldState.cachedAStarPathsByLevel, AStarPathsByLevel);
    }

    public static bool HasCachedPath(Dictionary<WorldLevel, List<Vector3>> pathsByLevel, WorldLevel level)
    {
        return pathsByLevel.TryGetValue(level, out var path) && path != null && path.Count > 0;
    }

    static List<LevelPathSaveData> SerializePaths(Dictionary<WorldLevel, List<Vector3>> pathsByLevel)
    {
        var serializedPaths = new List<LevelPathSaveData>();
        foreach (var kvp in pathsByLevel)
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

        return serializedPaths;
    }

    static void LoadSerializedPaths(List<LevelPathSaveData> serializedPaths, Dictionary<WorldLevel, List<Vector3>> target)
    {
        if (serializedPaths == null)
        {
            return;
        }

        foreach (var levelPath in serializedPaths)
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

            target[(WorldLevel)levelPath.level] = waypoints;
        }
    }
}
