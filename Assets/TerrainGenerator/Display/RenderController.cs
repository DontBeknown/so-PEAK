using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Game.Interaction;

public class RenderController : MonoBehaviour
{
    [Header("Core Links")]
    public WorldDataManager dataManager; // LINK TO YOUR NEW SCRIPT HERE

    [Header("References")]
    public Transform player;
    public Material mapMaterial;
    public PlayerSpawner spawner;

    [Header("Camera Settings")]
    public string cameraAimTargetName = "CameraTarget";

    [Header("Chunk Settings")]
    public int chunkSize = 41;
    public int chunkVisibleDistance = 1;
    public int chunkObjectLimiter = 1000;

    [Header("Climbable")]
    [SerializeField] private int climbableLayer;

    // State
    private bool playerHasSpawned = false;
    public bool PlayerSpawnComplete { get; private set; } = false;
    Dictionary<Vector2Int, GameObject> terrainChunks = new Dictionary<Vector2Int, GameObject>();
    Vector2Int currentChunkCoord;

    // Queue
    ConcurrentQueue<ChunkBuildRequest> meshCreationQueue = new ConcurrentQueue<ChunkBuildRequest>();
    struct ChunkBuildRequest { public Vector2Int coord; public MeshData meshData; }

    void Start()
    {
        Debug.Log("[RenderController] Start Func");

        if (dataManager != null)
        {
            // 1. Tell the Brain to do all the math
            dataManager.GenerateWorldData(chunkSize);

            // 2. Start building the world
            UpdateVisibleChunks();
        }
        else
        {
            Debug.LogError("[RenderController] Missing WorldDataManager reference! Drag it into the Inspector.");
        }
    }

    void Update()
    {
        // Notice we ask the DataManager for the globalHeightMap now!
        if (dataManager == null || dataManager.globalHeightMap == null) return;
        if (player == null) return;

        while (meshCreationQueue.TryDequeue(out ChunkBuildRequest request))
        {
            FinalizeChunk(request.coord, request.meshData);
        }

        int currentX = Mathf.FloorToInt(player.position.x / (chunkSize - 1));
        int currentY = Mathf.FloorToInt(player.position.z / (chunkSize - 1));
        Vector2Int playerCoord = new Vector2Int(currentX, currentY);

        if (playerCoord != currentChunkCoord)
        {
            currentChunkCoord = playerCoord;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2Int> coordsThatShouldBeActive = new HashSet<Vector2Int>();

        for (int yOffset = -chunkVisibleDistance; yOffset <= chunkVisibleDistance; yOffset++)
        {
            for (int xOffset = -chunkVisibleDistance; xOffset <= chunkVisibleDistance; xOffset++)
            {
                Vector2Int viewCoord = new Vector2Int(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset);
                coordsThatShouldBeActive.Add(viewCoord);

                if (!terrainChunks.ContainsKey(viewCoord))
                {
                    CreateChunkThreaded(viewCoord);
                }
            }
        }

        List<Vector2Int> coordsToRemove = new List<Vector2Int>();
        foreach (Vector2Int coord in terrainChunks.Keys)
        {
            if (!coordsThatShouldBeActive.Contains(coord)) coordsToRemove.Add(coord);
        }

        foreach (Vector2Int coord in coordsToRemove)
        {
            if (terrainChunks[coord] != null) Destroy(terrainChunks[coord]);
            terrainChunks.Remove(coord);
        }
    }

    void CreateChunkThreaded(Vector2Int coord)
    {
        terrainChunks.Add(coord, null);

        if (dataManager.activeGen == null)
        {
            Debug.LogError("[RenderController] activeGen is null! Is the WorldDataManager initialized?");
            return;
        }

        float heightMult = dataManager.activeGen.meshHeightMultiplier;
        int lod = dataManager.activeGen.levelOfDetail;
        float[,] hMap = dataManager.globalHeightMap;
        Color[,] cMap = dataManager.globalColorMap;
        Color fCol = dataManager.fieldColor;

        Task.Run(() =>
        {
            // Use the captured map data
            MapData mapData = MapSlicer.GetChunkData(coord, chunkSize, hMap, cMap, fCol);

            MeshData meshData = PerlinTerrainMeshGenerator.GenerateTerrainMesh(
                mapData.heightMap,
                mapData.colourMap,
                heightMult,
                lod
            );

            meshCreationQueue.Enqueue(new ChunkBuildRequest { coord = coord, meshData = meshData });
        });
    }

    void FinalizeChunk(Vector2Int coord, MeshData meshData)
    {
        if (!terrainChunks.ContainsKey(coord)) return;

        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");
        float meshCenterOffset = (chunkSize - 1) / 2f;
        float worldX = (coord.x * (chunkSize - 1)) + meshCenterOffset;
        float worldZ = (coord.y * (chunkSize - 1)) + meshCenterOffset;

        chunkObj.transform.position = new Vector3(worldX, 0, worldZ);
        chunkObj.transform.parent = this.transform;

        var mf = chunkObj.AddComponent<MeshFilter>();
        var mr = chunkObj.AddComponent<MeshRenderer>();
        var mc = chunkObj.AddComponent<MeshCollider>();

        chunkObj.layer = climbableLayer;

        Mesh finalMesh = meshData.CreateMesh();
        mf.mesh = finalMesh;
        mc.sharedMesh = finalMesh;
        mr.material = mapMaterial;

        // --- NEW: SPAWN THE OBJECTS ---
        // We just ask the DataManager: "Do you have objects for this chunk?"
        List<PlacedObject> objectsToSpawn = dataManager.GetObjectsForChunk(coord);

        if (objectsToSpawn != null)
        {
            int objectLimit = 0;
            foreach (PlacedObject objData in objectsToSpawn)
            {
                if (objectLimit >= chunkObjectLimiter) break;
                if (SpawnedObjectStateRegistry.IsDestroyed(objData.SpawnId)) continue;

                GameObject spawnedObj = Instantiate(objData.Prefab, objData.Position, objData.Rotation);
                spawnedObj.transform.localScale = objData.Scale;
                spawnedObj.transform.parent = chunkObj.transform;

                if (!string.IsNullOrEmpty(objData.SpawnId))
                {
                    var behaviours = spawnedObj.GetComponentsInChildren<MonoBehaviour>(true);
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        var behaviour = behaviours[i];
                        if (behaviour is not IInteractable)
                        {
                            continue;
                        }

                        var spawnedState = behaviour.GetComponent<SpawnedObjectState>();
                        if (spawnedState == null)
                        {
                            spawnedState = behaviour.gameObject.AddComponent<SpawnedObjectState>();
                        }

                        spawnedState.Initialize(objData.SpawnId);
                    }
                }

                objectLimit++;
            }
        }
        // ------------------------------

        terrainChunks[coord] = chunkObj;

        if (!playerHasSpawned)
        {
            StartCoroutine(SpawnPlayerSequence());
            playerHasSpawned = true;
        }
    }

    private System.Collections.IEnumerator SpawnPlayerSequence()
    {
        // Call PlayerSpawner's coroutine and wait for it to complete
        yield return StartCoroutine(spawner.SpawnPlayer());

        // Get the spawned player reference
        Transform spawnedPlayer = spawner.SpawnedPlayer;

        if (spawnedPlayer == null)
        {
            Debug.LogError("[RenderController] PlayerSpawner failed to spawn player!");
            yield break;
        }

        // Update our player reference
        player = spawnedPlayer;

        // Update all system references to the new player
        UpdatePlayerReferences(spawnedPlayer);

        // Signal that player spawn sequence is complete
        PlayerSpawnComplete = true;
    }

    private void UpdatePlayerReferences(Transform newPlayer)
    {
        // 1. Update ServiceContainer registrations via GameServiceBootstrapper
        var bootstrapper = FindFirstObjectByType<Game.Core.GameServiceBootstrapper>();
        if (bootstrapper != null)
        {
            bootstrapper.UpdatePlayerServices(newPlayer);
        }
        else
        {
            Debug.LogWarning("[RenderController] GameServiceBootstrapper not found - player services not updated!");
        }

        // 2. Update UIServiceProvider with new player references
        var uiServiceProvider = FindFirstObjectByType<Game.UI.UIServiceProvider>();
        if (uiServiceProvider != null)
        {
            uiServiceProvider.UpdatePlayerReferences(newPlayer);
        }
        else
        {
            Debug.LogWarning("[RenderController] UIServiceProvider not found - UI player references not updated!");
        }

        // 3. Find camera aim target on player
        Transform cameraAimTarget = newPlayer; // Default to player root
        if (!string.IsNullOrEmpty(cameraAimTargetName))
        {
            Transform foundTarget = newPlayer.Find(cameraAimTargetName);
            if (foundTarget != null)
            {
                cameraAimTarget = foundTarget;
            }
            else
            {
                Debug.LogWarning($"[RenderController] Camera aim target '{cameraAimTargetName}' not found on player. Using player root instead.");
            }
        }

        // 4. Update Cinemachine camera targets
        var cameraController = FindFirstObjectByType<CinemachinePlayerCamera>();
        if (cameraController != null)
        {
            cameraController.UpdateCameraTargets(newPlayer, cameraAimTarget);
        }
        else
        {
            var cinemachineCameras = FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None);
            foreach (var cam in cinemachineCameras)
            {
                cam.Follow = newPlayer;
                cam.LookAt = cameraAimTarget;
            }
        }
    }
}