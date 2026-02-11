using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks; // Needed for Threading
using System.Collections.Concurrent; // Needed for the Queue

public class RenderController : MonoBehaviour
{
    public NoiseTranslator worldGen;

    [Header("References")]
    public Transform player;
    public Material mapMaterial;

    [Header("Tree Settings")]
    public GameObject treePrefab;
    public float treeSpacing = 2.0f;
    public float minTreeHeight = 2.0f;
    public float maxTreeHeight = 12.0f;
    public int treeLimiter = 100;
    // --- SETTINGS ---
    // Remember to set this back to 241 after testing small chunks!
    public int chunkSize = 41;
    public int chunkVisibleDistance = 1;

    // --- GLOBAL DATA ---
    private float[,] globalHeightMap;
    private Color[,] globalColorMap;
    public Color fieldColor;


    // NEW: Store the mathematical data for all trees here!
    private Dictionary<Vector2Int, List<TreeInstance>> globalTreeData;

    // --- STATE ---
    // Dictionary value is 'null' while loading
    Dictionary<Vector2Int, GameObject> terrainChunks = new Dictionary<Vector2Int, GameObject>();
    Vector2Int currentChunkCoord;

    // THE QUEUE: Thread-safe bridge between Background and Main thread
    ConcurrentQueue<ChunkBuildRequest> meshCreationQueue = new ConcurrentQueue<ChunkBuildRequest>();

    struct ChunkBuildRequest
    {
        public Vector2Int coord;
        public MeshData meshData;
    }

    void Start()
    {
        if (worldGen == null) worldGen = GetComponent<NoiseTranslator>();

        if (worldGen != null)
        {
            // 1. Generate Data (Matches your old function name)
            worldGen.TerrainDrawing();

            // 2. Grab References (Matches your old variable names)
            globalHeightMap = worldGen.completeMap;
            globalColorMap = worldGen.colorMap;

            // 3. GENERATE TREE DATA (Math only, no lag!)
            // Make sure these maps are accessible from your NoiseTranslator script
            //  Setup the new 1100x1100 arrays
            float[,] expandedTreeNoise = new float[worldGen.mapWidth + worldGen.bufferLength, worldGen.mapLength + worldGen.bufferLength];
            float[,] expandedRoadMask = new float[worldGen.mapWidth + worldGen.bufferLength, worldGen.mapLength + worldGen.bufferLength];

            //  Expand them!
            // Trees fade to 0 (No trees at the edge)
            BufferGen.GenMapWithBuffer(worldGen.treeNoiseMap, expandedTreeNoise, worldGen.bufferLength);

            // Roads default to 1 (No roads at the edge)
            BufferGen.GenRoadMaskWithBuffer(worldGen.roadRidge, expandedRoadMask, worldGen.bufferLength);


            globalTreeData = TreePlanter.GenerateTreeData(
                expandedTreeNoise,    
                globalHeightMap,
                expandedRoadMask,
                minTreeHeight,
                maxTreeHeight,        
                worldGen.meshHeightMultiplier,
                chunkSize - 1,            // CRITICAL: Pass (chunkSize - 1) so keys match RenderController!
                treeSpacing
            );

            // 4. Start Loop
            UpdateVisibleChunks();
        }
        else
        {
            Debug.LogError("RenderController: Missing NoiseTranslator script!");
        }
    }

    void Update()
    {
        if (globalHeightMap == null) return;

        // --- 1. PROCESS QUEUE (The New Part) ---
        // Every frame, check if a thread finished a job
        while (meshCreationQueue.TryDequeue(out ChunkBuildRequest request))
        {
            FinalizeChunk(request.coord, request.meshData);
        }

        // --- 2. CHECK PLAYER POSITION ---
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

        // Cleanup Logic
        List<Vector2Int> coordsToRemove = new List<Vector2Int>();
        foreach (Vector2Int coord in terrainChunks.Keys)
        {
            if (!coordsThatShouldBeActive.Contains(coord))
            {
                coordsToRemove.Add(coord);
            }
        }

        foreach (Vector2Int coord in coordsToRemove)
        {
            // Because trees are parented to the chunk, Destroying the chunk destroys the trees automatically!
            if (terrainChunks[coord] != null) Destroy(terrainChunks[coord]);
            terrainChunks.Remove(coord);
        }
    }

    void CreateChunkThreaded(Vector2Int coord)
    {
        // 1. RESERVE SPOT
        terrainChunks.Add(coord, null);

        // 2. BACKGROUND WORK
        Task.Run(() =>
        {
            MapData mapData = MapSlicer.GetChunkData(coord, chunkSize, globalHeightMap, globalColorMap, fieldColor);

            MeshData meshData = PerlinTerrainMeshGenerator.GenerateTerrainMesh(
                mapData.heightMap,
                mapData.colourMap,
                worldGen.meshHeightMultiplier,
                worldGen.levelOfDetail
            );

            // 3. SEND TO MAIN THREAD
            meshCreationQueue.Enqueue(new ChunkBuildRequest { coord = coord, meshData = meshData });
        });
    }

    void FinalizeChunk(Vector2Int coord, MeshData meshData)
    {
        if (!terrainChunks.ContainsKey(coord)) return;

        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");

        // THE FIX: Calculate the center offset
        float meshCenterOffset = (chunkSize - 1) / 2f;

        // Shift the chunk so its bottom-left corner is the anchor, not its center
        float worldX = (coord.x * (chunkSize - 1)) + meshCenterOffset;

        // NOTE: If MapSlicer is drawing Z normally now, we add the offset. 
        // (If it was inverted, we might have to subtract, but try adding first!)
        float worldZ = (coord.y * (chunkSize - 1)) + meshCenterOffset;

        chunkObj.transform.position = new Vector3(worldX, 0, worldZ);
        chunkObj.transform.parent = this.transform;

        var mf = chunkObj.AddComponent<MeshFilter>();
        var mr = chunkObj.AddComponent<MeshRenderer>();
        var mc = chunkObj.AddComponent<MeshCollider>();

        Mesh finalMesh = meshData.CreateMesh();
        mf.mesh = finalMesh;
        mc.sharedMesh = finalMesh;
        mr.material = mapMaterial;

        // --- NEW: SPAWN THE TREES FOR THIS CHUNK ---
        if (treePrefab != null && globalTreeData != null && globalTreeData.ContainsKey(coord))
        {
            int treeLimit = 0; // NEW: Start a counter

            foreach (var treeData in globalTreeData[coord])
            {
                if (treeLimit >= treeLimiter) break; // NEW: Abort if we hit 100 trees in this chunk!

                // Instantiate at the exact world position
                GameObject t = Instantiate(treePrefab, treeData.position, treeData.rotation);
                t.transform.localScale = treeData.scale;

                // Parent the tree to the Chunk GameObject
                t.transform.parent = chunkObj.transform;

                treeLimit++; // NEW: Increase the counter
            }
        }
        // -------------------------------------------
        terrainChunks[coord] = chunkObj;
    }
}