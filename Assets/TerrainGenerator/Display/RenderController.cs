using NUnit.Framework;
using Pathfinding;
using System.Collections.Concurrent;
using Game.Interaction;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class RenderController : MonoBehaviour
{
    [Header("Core Links")]
    public WorldDataManager dataManager; // LINK TO YOUR NEW SCRIPT HERE

    [Header("References")]
    public Transform player;
    public Material mapMaterial; // This is your base Shader Graph material!
    private Material currentStageMaterial; // ADD THIS: Holds the clone for the current stage
    public PlayerSpawner spawner;

    [Header("Camera Settings")]
    public string cameraAimTargetName = "CameraTarget";

    [Header("Chunk Settings")]
    public int chunkSize = 41;
    public int chunkVisibleDistance = 1; // High-Res Radius
    public int farRenderDistance = 8;    // Low-Res Radius
    public int lowResLOD = 5;            // Step size for low-res chunks
    public int chunkObjectLimiter = 10000; //limits in case of emergency? but i think it shouldn't
                                          //if we optimized okay at first 

    [Header("Climbable")]
    [SerializeField] private int climbableLayer;

    // State
    private bool playerHasSpawned = false;
    public bool PlayerSpawnComplete { get; private set; } = false;
    Dictionary<Vector2Int, GameObject> terrainChunks = new Dictionary<Vector2Int, GameObject>();
    Vector2Int currentChunkCoord;

    // Bounds (Calculated once at Start)
    private int maxChunkX;
    private int maxChunkZ;

    //nowhere land material
    private Material sharedNowhereMaterial;

    // Queue
    ConcurrentQueue<ChunkBuildRequest> highResQueue = new ConcurrentQueue<ChunkBuildRequest>();
    ConcurrentQueue<ChunkBuildRequest> lowResQueue = new ConcurrentQueue<ChunkBuildRequest>();
    struct ChunkBuildRequest { public Vector2Int coord; public MeshData meshData; public bool isHighRes; }

    void Start()
    {
        Debug.Log("[RenderController] Start Func");

        if (dataManager != null)
        {
            dataManager.GenerateWorldData(chunkSize);

            SetupStageMaterial();

            maxChunkX = (dataManager.globalHeightMap.GetLength(0) - 1) / (chunkSize - 1);
            maxChunkZ = (dataManager.globalHeightMap.GetLength(1) - 1) / (chunkSize - 1);

            // --- THE DYNAMIC SPAWN FIX (WITHOUT TOUCHING SPAWNER SCRIPT) ---

            // 1. Default to the public variable your friend set in the Inspector
            Vector3 plannedSpawn = spawner.targetSpawnPosition;

            // 2. Peek at the SaveLoadService just like the spawner does!
            if (SaveLoadService.Instance != null && !SaveLoadService.Instance.IsNewWorld())
            {
                var saveData = SaveLoadService.Instance.CurrentWorldSave;
                if (saveData != null && saveData.playerData != null)
                {
                    plannedSpawn = new Vector3(
                        saveData.playerData.position[0],
                        saveData.playerData.position[1],
                        saveData.playerData.position[2]
                    );
                }
            }

            // 3. Convert that world position into your grid chunks
            int startX = Mathf.FloorToInt(plannedSpawn.x / (chunkSize - 1));
            int startZ = Mathf.FloorToInt(plannedSpawn.z / (chunkSize - 1));

            // Safety Clamp: Just in case the target is somehow out of bounds
            startX = Mathf.Clamp(startX, 0, maxChunkX - 1);
            startZ = Mathf.Clamp(startZ, 0, maxChunkZ - 1);

            // 4. Set the center of the world and build the High-Res safe zone!
            currentChunkCoord = new Vector2Int(startX, startZ);
            UpdateVisibleChunks();

            // 5. Fill the rest of the world with Low-Res background chunks
            for (int z = 0; z < maxChunkZ; z++)
            {
                for (int x = 0; x < maxChunkX; x++)
                {
                    Vector2Int coord = new Vector2Int(x, z);

                    // Only queue it for Low-Res if the High-Res queue didn't already grab it!
                    if (!terrainChunks.ContainsKey(coord))
                    {
                        CreateChunkThreaded(coord, false);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("[RenderController] Missing WorldDataManager reference! Drag it into the Inspector.");
        }
    }

    void Update()
    {
        if (dataManager == null || dataManager.globalHeightMap == null) return;

        // --- PRIORITY QUEUE PROCESSING ---
        int chunksProcessedThisFrame = 0;
        while (chunksProcessedThisFrame < 3)
        {
            // 1. ALWAYS build High-Res first (we name the variable highReq)
            if (highResQueue.TryDequeue(out ChunkBuildRequest highReq))
            {
                FinalizeChunk(highReq.coord, highReq.meshData, highReq.isHighRes);
                chunksProcessedThisFrame++;
            }
            // 2. Only build Low-Res if no High-Res are waiting (we name it lowReq)
            else if (lowResQueue.TryDequeue(out ChunkBuildRequest lowReq))
            {
                FinalizeChunk(lowReq.coord, lowReq.meshData, lowReq.isHighRes);
                chunksProcessedThisFrame++;
            }
            else
            {
                // Both queues are empty! Stop checking.
                break;
            }
        }

        if (player == null) return;

        //while (meshCreationQueue.TryDequeue(out ChunkBuildRequest request))
        //{
        //    FinalizeChunk(request.coord, request.meshData);
        //}

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
        // 1. Process the FOV (High Res & Nowhere Land)
        for (int yOffset = -chunkVisibleDistance; yOffset <= chunkVisibleDistance; yOffset++)
        {
            for (int xOffset = -chunkVisibleDistance; xOffset <= chunkVisibleDistance; xOffset++)
            {
                Vector2Int viewCoord = new Vector2Int(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset);

                // We removed the 'continue' here! Let ManageChunkState handle the bounds.
                ManageChunkState(viewCoord, true);
            }
        }

        // 2. Process Everything Outside FOV
        List<Vector2Int> outOfFovCoords = new List<Vector2Int>();
        foreach (var coord in terrainChunks.Keys)
        {
            float dist = Vector2Int.Distance(currentChunkCoord, coord);
            if (dist > chunkVisibleDistance)
            {
                outOfFovCoords.Add(coord);
            }
        }

        // Tell the state manager that we no longer want these to be High-Res
        foreach (var coord in outOfFovCoords)
        {
            ManageChunkState(coord, false);
        }
    }

    void CreateChunkThreaded(Vector2Int coord, bool wantedHighRes)
    {
        if (terrainChunks.ContainsKey(coord)) return;

        terrainChunks.Add(coord, null);

        if (dataManager.activeGen == null)
        {
            Debug.LogError("[RenderController] activeGen is null! Is the WorldDataManager initialized?");
            return;
        }

        float heightMult = dataManager.activeGen.meshHeightMultiplier;
        int lod = wantedHighRes ? dataManager.activeGen.levelOfDetail : lowResLOD;
        float[,] hMap = dataManager.globalHeightMap;
        //Color[,] cMap = dataManager.globalColorMap;
        Color fCol = dataManager.fieldColor;

        Task.Run(() =>
        {
            // Use the captured map data
            float[,] mapData = MapSlicer.GetChunkData(coord, chunkSize, hMap);

            MeshData meshData = PerlinTerrainMeshGenerator.GenerateTerrainMesh(
                mapData,
                heightMult,
                lod
            );


            ChunkBuildRequest request = new ChunkBuildRequest
            {
                coord = coord,
                meshData = meshData,
                isHighRes = wantedHighRes
            };

            if (wantedHighRes) highResQueue.Enqueue(request);
            else lowResQueue.Enqueue(request);
        });
    }

    void FinalizeChunk(Vector2Int coord, MeshData meshData, bool isHighRes)
    {
        if (!terrainChunks.ContainsKey(coord)) return;

        // YOUR CODE: Naming the chunk based on resolution
        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}_{(isHighRes ? "High" : "Low")}");
        float meshCenterOffset = (chunkSize - 1) / 2f;
        float worldX = (coord.x * (chunkSize - 1)) + meshCenterOffset;
        float worldZ = (coord.y * (chunkSize - 1)) + meshCenterOffset;

        chunkObj.transform.position = new Vector3(worldX, 0, worldZ);
        chunkObj.transform.parent = this.transform;

        var mf = chunkObj.AddComponent<MeshFilter>();
        var mr = chunkObj.AddComponent<MeshRenderer>();

        Mesh finalMesh = meshData.CreateMesh();
        mf.mesh = finalMesh;
        mr.sharedMaterial = currentStageMaterial;

        // --- YOUR SMART STATE LOGIC ---
        if (isHighRes)
        {
            var mc = chunkObj.AddComponent<MeshCollider>();
            mc.sharedMesh = finalMesh;
            chunkObj.layer = climbableLayer;

            // Only ask DataManager for objects on High-Res chunks
            List<PlacedObject> objectsToSpawn = dataManager.GetObjectsForChunk(coord);

            if (objectsToSpawn != null)
            {
                int objectLimit = 0; // Restored from friend's code

                // Restored the missing loop from your friend's code
                foreach (PlacedObject objData in objectsToSpawn)
                {
                    if (objectLimit >= chunkObjectLimiter) break;

                    // FRIEND'S CODE: Skip destroyed objects
                    if (SpawnedObjectStateRegistry.IsDestroyed(objData.SpawnId)) continue;

                    GameObject spawnedObj = Instantiate(objData.Prefab, objData.Position, objData.Rotation);
                    spawnedObj.transform.localScale = objData.Scale;
                    spawnedObj.transform.parent = chunkObj.transform;

                    // FRIEND'S CODE: Attach save states to interactables
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
        }
        else
        {
            // YOUR CODE: Low Res fallback
            chunkObj.layer = 2; // Ignore Raycast Layer
        }

        terrainChunks[coord] = chunkObj;

        if (!playerHasSpawned)
        {
            StartCoroutine(SpawnPlayerSequence());
            playerHasSpawned = true;
        }
    }

    void ManageChunkState(Vector2Int coord, bool wantHighRes)
    {
        bool inWorld = IsCoordInWorld(coord);
        bool exists = terrainChunks.TryGetValue(coord, out GameObject currentObj);

        // THE GUARD: If it exists in the dictionary but has no GameObject yet, 
        // it means it is currently building in the thread. Leave it alone!
        bool isBuilding = exists && currentObj == null;

        // --- RULE 3: NOWHERE LAND ---
        if (!inWorld)
        {
            if (wantHighRes && !exists) CreateNowhereChunk(coord);
            else if (!wantHighRes && exists && !isBuilding) // Add !isBuilding here
            {
                if (currentObj != null) Destroy(currentObj);
                terrainChunks.Remove(coord);
            }
            return;
        }

        // --- RULES 1 & 2: REAL WORLD ---
        if (isBuilding) return; // Wait for it to finish before evaluating!

        bool isCurrentlyHigh = exists && currentObj != null && currentObj.name.Contains("High");

        if (!exists || (wantHighRes && !isCurrentlyHigh) || (!wantHighRes && isCurrentlyHigh))
        {
            if (exists && currentObj != null) Destroy(currentObj);
            terrainChunks.Remove(coord);
            CreateChunkThreaded(coord, wantHighRes);
        }
    }

    void CreateNowhereChunk(Vector2Int coord)
    {
        // Create a basic flat plane
        GameObject nowhereObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        nowhereObj.name = $"Nowhere_{coord.x}_{coord.y}";

        // Unity Planes are 10x10 units by default. Scale it to match your chunkSize.
        float scale = (chunkSize - 1) / 10f;
        nowhereObj.transform.localScale = new Vector3(scale, 1, scale);

        // Position it
        float meshCenterOffset = (chunkSize - 1) / 2f;
        float worldX = (coord.x * (chunkSize - 1)) + meshCenterOffset;
        float worldZ = (coord.y * (chunkSize - 1)) + meshCenterOffset;

        // Set Y to 0 (or whatever your water/base level is)
        nowhereObj.transform.position = new Vector3(worldX, 0, worldZ);
        nowhereObj.transform.parent = this.transform;

        // --- APPLY THE COLOR ---
        var mr = nowhereObj.GetComponent<MeshRenderer>();

        if (sharedNowhereMaterial == null)
        {
            sharedNowhereMaterial = new Material(mapMaterial);

            // 1. Use your exact Shader Graph property name!
            sharedNowhereMaterial.SetColor("_Field_Color", dataManager.fieldColor);

            // 2. Pass a pure black texture so the Shader Graph doesn't try 
            // to paint random roads on your empty background planes!
            sharedNowhereMaterial.SetTexture("_Road_Mask", Texture2D.blackTexture);
        }

        mr.sharedMaterial = sharedNowhereMaterial;
        // -----------------------

        // Destroy the collider to save physics calculations
        Destroy(nowhereObj.GetComponent<MeshCollider>());
        nowhereObj.layer = 2; // Ignore Raycast Layer

        // Save it to the dictionary
        terrainChunks[coord] = nowhereObj;
    }

    private void SetupStageMaterial()
    {
        // 1. Clone the base Shader Graph material
        currentStageMaterial = new Material(mapMaterial);

        // 2. Pass the colors (Grab these from your dataManager!)
        Debug.Log($"[RenderController] Field Color is: {dataManager.fieldColor}");
        currentStageMaterial.SetColor("_Field_Color", dataManager.fieldColor);

        // Assuming you add a rock color and road color to your dataManager...
        currentStageMaterial.SetColor("_Side_Rock_Color", dataManager.sideRockColor);
        currentStageMaterial.SetColor("_Road_Color", dataManager.roadColor);

        // 3. Generate and pass the Road Mask Texture
        // NOTE: Replace 'dataManager.globalRoadMap' with wherever your road float array is stored!
        if (dataManager.roadRidgeTexture != null)
        {
            currentStageMaterial.SetTexture("_Road_Mask", dataManager.roadRidgeTexture);
            currentStageMaterial.SetFloat("_Global_Map_Size", dataManager.expandedRoadRidge.GetLength(0));
        }
    }

    private bool IsCoordInWorld(Vector2Int coord)
    {
        // Use the pre-calculated bounds
        return coord.x >= 0 && coord.y >= 0 && coord.x < maxChunkX && coord.y < maxChunkZ;
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

    //just cleaning up material
    private void OnDestroy()
    {
        // Clean up our runtime material to prevent memory leaks in RAM
        if (sharedNowhereMaterial != null)
        {
            Destroy(sharedNowhereMaterial);
        }
    }
}

