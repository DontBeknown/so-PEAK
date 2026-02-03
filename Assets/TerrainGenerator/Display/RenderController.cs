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

    // --- SETTINGS ---
    // Remember to set this back to 241 after testing small chunks!
    public int chunkSize = 41;
    public int chunkVisibleDistance = 1;

    // --- GLOBAL DATA ---
    private float[,] globalHeightMap;
    private Color[,] globalColorMap;
    public Color fieldColor;

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

            // 3. Start Loop
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
        int currentX = Mathf.RoundToInt(player.position.x / (chunkSize - 1));
        int currentY = Mathf.RoundToInt(player.position.z / (chunkSize - 1));
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
        // Safety: If player left area, cancel spawn
        if (!terrainChunks.ContainsKey(coord)) return;

        // 4. UNITY OBJECT CREATION (Matches your old CreateChunk logic)
        GameObject chunkObj = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkObj.transform.position = new Vector3(coord.x * (chunkSize - 1), 0, coord.y * (chunkSize - 1));
        chunkObj.transform.parent = this.transform;

        var mf = chunkObj.AddComponent<MeshFilter>();
        var mr = chunkObj.AddComponent<MeshRenderer>();
        var mc = chunkObj.AddComponent<MeshCollider>();

        Mesh finalMesh = meshData.CreateMesh();
        mf.mesh = finalMesh;
        mc.sharedMesh = finalMesh; // Physics Fix Included!
        mr.material = mapMaterial;

        // 5. UPDATE DICTIONARY
        terrainChunks[coord] = chunkObj;
    }
}