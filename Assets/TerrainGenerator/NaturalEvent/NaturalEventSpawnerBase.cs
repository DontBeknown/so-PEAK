using UnityEngine;

public abstract class NaturalEventSpawnerBase : MonoBehaviour
{
    [Header("Core Links")]
    public NaturalEventDirector eventDirector;

    [Header("Biome Target")]
    [Tooltip("Check this box if you want this spawner to trigger in EVERY biome!")]
    public bool spawnInAnyBiome = true;

    [Tooltip("If the box above is unchecked, which specific biome should this spawn in?")]
    public WorldLevel targetBiome;

    protected virtual void OnEnable()
    {
        if (eventDirector != null)
        {
            SubscribeToDirector(eventDirector);
        }
    }

    protected virtual void OnDisable()
    {
        if (eventDirector != null)
        {
            UnsubscribeFromDirector(eventDirector);
        }
    }

    public void Spawn(Transform anchor, WorldLevel triggeredBiome)
    {
        if (!spawnInAnyBiome && triggeredBiome != targetBiome) return;

        if (anchor == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Spawn called with null anchor.");
            return;
        }

        SpawnInternal(anchor, triggeredBiome);
    }

    protected abstract void SpawnInternal(Transform anchor, WorldLevel triggeredBiome);
    protected abstract void SubscribeToDirector(NaturalEventDirector director);
    protected abstract void UnsubscribeFromDirector(NaturalEventDirector director);
}
