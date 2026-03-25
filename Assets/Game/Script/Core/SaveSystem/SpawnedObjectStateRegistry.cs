using System.Collections.Generic;
using UnityEngine;

public static class SpawnedObjectStateRegistry
{
    private static readonly HashSet<string> destroyedSpawnIds = new HashSet<string>();
    private static string cachedWorldGuid;

    public static void RefreshFromCurrentSave()
    {
        ImportFromSave(SaveLoadService.Instance?.CurrentWorldSave);
    }

    public static void ImportFromSave(WorldSaveData save)
    {
        destroyedSpawnIds.Clear();
        cachedWorldGuid = string.Empty;

        if (save == null)
        {
            return;
        }

        cachedWorldGuid = save.worldGuid ?? string.Empty;
        save.worldState ??= new WorldStateSaveData();
        save.worldState.spawnedObjectStates ??= new List<SpawnedObjectStateSaveData>();

        foreach (var state in save.worldState.spawnedObjectStates)
        {
            if (state == null || !state.isDestroyed || string.IsNullOrEmpty(state.spawnId))
            {
                continue;
            }

            destroyedSpawnIds.Add(state.spawnId);
        }

    }

    public static void ExportToSave(WorldSaveData save)
    {
        if (save == null)
        {
            return;
        }

        save.worldState ??= new WorldStateSaveData();
        save.worldState.spawnedObjectStates ??= new List<SpawnedObjectStateSaveData>();

        var target = save.worldState.spawnedObjectStates;
        target.Clear();

        foreach (string spawnId in destroyedSpawnIds)
        {
            target.Add(new SpawnedObjectStateSaveData
            {
                spawnId = spawnId,
                isDestroyed = true
            });
        }

    }

    public static bool IsDestroyed(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
        {
            return false;
        }

        EnsureCurrentWorldCache();
        return destroyedSpawnIds.Contains(spawnId);
    }

    public static void ClearAllDestroyed()
    {
        destroyedSpawnIds.Clear();

        var save = SaveLoadService.Instance?.CurrentWorldSave;
        if (save?.worldState?.spawnedObjectStates != null)
        {
            save.worldState.spawnedObjectStates.Clear();
        }

        cachedWorldGuid = save?.worldGuid ?? string.Empty;
    }

    public static void MarkDestroyed(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
        {
            Debug.LogWarning("[SpawnedObjectStateRegistry] MarkDestroyed called with empty spawnId");
            return;
        }

        EnsureCurrentWorldCache();

        bool addedToSet = destroyedSpawnIds.Add(spawnId);

        var save = SaveLoadService.Instance?.CurrentWorldSave;
        if (save == null)
        {
            Debug.LogWarning($"[SpawnedObjectStateRegistry] MarkDestroyed spawnId='{spawnId}' addedToSet={addedToSet} but CurrentWorldSave is null");
            return;
        }

        save.worldState ??= new WorldStateSaveData();
        save.worldState.spawnedObjectStates ??= new List<SpawnedObjectStateSaveData>();

        var list = save.worldState.spawnedObjectStates;
        var existing = list.Find(s => s != null && s.spawnId == spawnId);

        if (existing == null)
        {
            list.Add(new SpawnedObjectStateSaveData
            {
                spawnId = spawnId,
                isDestroyed = true
            });
        }
        else
        {
            existing.isDestroyed = true;
        }

        Debug.Log($"[SpawnedObjectStateRegistry] MarkDestroyed world='{save.worldGuid}' spawnId='{spawnId}' addedToSet={addedToSet} listCount={list.Count} hashCount={destroyedSpawnIds.Count}");
    }

    private static void EnsureCurrentWorldCache()
    {
        var save = SaveLoadService.Instance?.CurrentWorldSave;
        string worldGuid = save?.worldGuid ?? string.Empty;

        if (cachedWorldGuid != worldGuid)
        {
            RefreshFromCurrentSave();
        }
    }
}
