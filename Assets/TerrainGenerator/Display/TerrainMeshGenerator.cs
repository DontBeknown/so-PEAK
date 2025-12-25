using System;
using System.Collections;
using UnityEngine;

public static class PerlinTerrainMeshGenerator
{

    public static MeshData GenerateTerrainMesh(float[,] heightMap, Color[,] colorMap, float heightMultiplier, int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int length = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (length - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesX = (width - 1) / meshSimplificationIncrement + 1;
        int verticesY = (length - 1) / meshSimplificationIncrement + 1;


        MeshData meshData = new MeshData(verticesX, verticesY);
        int vertexIndex = 0;
        float maxHeight = float.MinValue; // Track max value
        float minHeight = float.MaxValue; // Track max value

        for (int y = 0; y < length; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                int yVertex = y / meshSimplificationIncrement;
                int xVertex = x / meshSimplificationIncrement;

                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x,heightMap[x, y] * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)length);

                // Fix: Use simplified vertex indices for boundary check
                if (xVertex < verticesX - 1 && yVertex < verticesY - 1)
                {
                    int current = vertexIndex;
                    int nextRow = current + verticesX;

                    meshData.AddTriangle(current, current + 1 + verticesX, nextRow);
                    meshData.AddTriangle(current + 1 + verticesX, current, current + 1);
                }

                float vertexHeight = heightMap[x, y] * heightMultiplier;
                if (vertexHeight > maxHeight) maxHeight = vertexHeight; // Update max
                if (vertexHeight < minHeight) minHeight = vertexHeight; // Update max
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, vertexHeight, topLeftZ - y);


                //Color finalColor;

                //// 1. Road check
                //if (roadNoise[x, y] < 0.25f)
                //{
                //    finalColor = new Color(0.70f, 0.55f, 0.35f); // light brown
                //}
                //else
                //{
                //    // 2. Compute steepness (compare with right and down neighbors)
                //    float heightHere = heightMap[x, y];
                //    float heightRight = (x < width - 1) ? heightMap[x + 1, y] : heightHere;
                //    float heightDown = (y < height - 1) ? heightMap[x, y + 1] : heightHere;

                //    float steepness = Mathf.Max(
                //        Mathf.Abs(heightHere - heightRight),
                //        Mathf.Abs(heightHere - heightDown)
                //    );

                //    if (steepness > 0.15f)
                //    {
                //        finalColor = new Color(0.35f, 0.20f, 0.10f); // dark brown rock
                //    }
                //    else
                //    {
                //        finalColor = new Color(0.2f, 0.7f, 0.2f); // green grass
                //    }
                //}

                // Assign into mesh
                meshData.colors[vertexIndex] = colorMap[x,y];

                vertexIndex++;
            }
        }
        Debug.Log("Max mesh height: " + maxHeight); // Output max height
        Debug.Log("Min mesh height: " + minHeight); // Output max height
        return meshData;

    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    public Color[] colors;

    public int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        colors = new Color[meshWidth * meshHeight];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();

        // Quick fix for large meshes:
        if (vertices.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.colors = colors;

        mesh.RecalculateNormals();


        return mesh;
    }

}