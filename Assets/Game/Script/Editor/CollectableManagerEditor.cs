using System.Collections.Generic;
using System.Linq;
using Game.Collectable;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollectableManager))]
public class CollectableManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var manager = (CollectableManager)target;
        var unlockedCount = manager.GetUnlockedIds()?.Count ?? 0;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use these runtime tools to lock or unlock every collectable.",
            MessageType.Info);
        EditorGUILayout.LabelField("Unlocked Count", unlockedCount.ToString());

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Unlock All Collectables"))
            {
                UnlockAllCollectables(manager);
            }

            if (GUILayout.Button("Lock All Collectables"))
            {
                manager.LoadState(new List<string>());
                Debug.Log("[CollectableManagerEditor] Locked all collectables (cleared unlocked state).", manager);
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use debug actions.", MessageType.None);
        }
    }

    private static void UnlockAllCollectables(CollectableManager manager)
    {
        var collectables = FindAllCollectables();
        var unlocked = 0;

        foreach (var collectable in collectables)
        {
            if (collectable == null || string.IsNullOrWhiteSpace(collectable.id))
                continue;

            if (!manager.IsUnlocked(collectable.id))
                unlocked++;

            manager.Unlock(collectable);
        }

        Debug.Log($"[CollectableManagerEditor] Unlocked {unlocked} collectable(s).", manager);
    }

    private static List<CollectableItem> FindAllCollectables()
    {
        return AssetDatabase
            .FindAssets("t:CollectableItem")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CollectableItem>)
            .Where(item => item != null)
            .ToList();
    }
}
