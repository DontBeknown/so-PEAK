using UnityEngine;

/// <summary>
/// ScriptableObject data bus shared between TerrainGenDemo and Scene_Debug_Gameplay.
/// Both scenes reference the same asset — no direct scene-to-scene coupling.
///
/// Gate 1: isChunksReady  — set by RenderController when all terrain chunks are finalized.
/// Gate 2: playerConfirmed — set by DebugGameplayConfirm when the player presses Y.
/// </summary>
[CreateAssetMenu(fileName = "SharedLoadingState", menuName = "Game/Shared Loading State")]
public class SharedLoadingState : ScriptableObject
{
    [Range(0f, 1f)]
    public float progress = 0f;
    public string statusMessage = "Generating world...";

    /// <summary>Gate 1 — all terrain chunks have been finalized by RenderController.</summary>
    public bool isChunksReady = false;

    /// <summary>Gate 2 — player pressed Y in Scene_Debug_Gameplay.</summary>
    public bool playerConfirmed = false;

    /// <summary>Set by AsyncLoadCoordinator after the full transition completes.</summary>
    public bool isComplete = false;

    /// <summary>Call this at the start of every new load to wipe leftover state.</summary>
    public void Reset()
    {
        progress = 0f;
        statusMessage = "Generating world...";
        isChunksReady = false;
        playerConfirmed = false;
        isComplete = false;
    }
}
