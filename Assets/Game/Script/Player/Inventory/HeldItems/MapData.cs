using UnityEngine;

/// <summary>
/// Data asset for a held map.
/// Stores the Resources sprite path so multiple map items can reuse the same viewer.
/// </summary>
[CreateAssetMenu(fileName = "New Map Data", menuName = "Inventory/Held Items/Map Data")]
public class HeldMapData : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string mapTitle;

    [Header("Resources")]
    [Tooltip("Relative path under a Resources folder, without file extension.")]
    [SerializeField] private string mapSpriteResourcePath;

    public string MapTitle => string.IsNullOrWhiteSpace(mapTitle) ? name : mapTitle;
    public string MapSpriteResourcePath => mapSpriteResourcePath;
}