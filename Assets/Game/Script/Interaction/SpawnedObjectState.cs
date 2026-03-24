using UnityEngine;

public class SpawnedObjectState : MonoBehaviour
{
    [SerializeField] private string spawnId;

    public string SpawnId => spawnId;

    public void Initialize(string id)
    {
        spawnId = id;
    }

    public void MarkDestroyed()
    {
        if (string.IsNullOrEmpty(spawnId))
        {
            return;
        }

        SpawnedObjectStateRegistry.MarkDestroyed(spawnId);
    }
}
