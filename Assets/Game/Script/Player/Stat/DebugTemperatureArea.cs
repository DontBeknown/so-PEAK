using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DebugTemperatureArea : MonoBehaviour
{
    [Header("Temperature Debug Zone")]
    [Tooltip("Temperature change applied every second while player stays in this trigger. Positive = hotter, negative = colder.")]
    [SerializeField] private float temperatureChangePerSecond = 3f;

    [Tooltip("Only colliders on these layers can trigger this area.")]
    [SerializeField] private LayerMask playerLayers = ~0;

    private readonly HashSet<PlayerStats> playersInside = new HashSet<PlayerStats>();
    private readonly Dictionary<PlayerStats, float> playerOffsets = new Dictionary<PlayerStats, float>();

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, playerLayers))
            return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null)
            return;

        playersInside.Add(stats);
        playerOffsets[stats] = 0f;
        stats.SetDebugAreaTemperatureOffset(0f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, playerLayers))
            return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null)
            return;

        playersInside.Remove(stats);
        playerOffsets.Remove(stats);
        stats.SetDebugAreaTemperatureOffset(0f);
    }

    private void Update()
    {
        if (playersInside.Count == 0)
            return;

        float delta = temperatureChangePerSecond * Time.deltaTime;
        foreach (PlayerStats stats in playersInside)
        {
            if (stats != null && playerOffsets.TryGetValue(stats, out float currentOffset))
            {
                currentOffset += delta;
                playerOffsets[stats] = currentOffset;
                stats.SetDebugAreaTemperatureOffset(currentOffset);
            }
        }
    }

    private void OnDisable()
    {
        ClearAllPlayerOffsets();
    }

    private void OnDestroy()
    {
        ClearAllPlayerOffsets();
    }

    private void ClearAllPlayerOffsets()
    {
        foreach (PlayerStats stats in playersInside)
        {
            if (stats != null)
            {
                stats.SetDebugAreaTemperatureOffset(0f);
            }
        }

        playersInside.Clear();
        playerOffsets.Clear();
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
