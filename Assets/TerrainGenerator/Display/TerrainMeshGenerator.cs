using System;
using System.Collections;
using UnityEngine;

public static class PerlinTerrainMeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesX = (width - 1) / meshSimplificationIncrement + 1;
        int verticesY = (height - 1) / meshSimplificationIncrement + 1;


        MeshData meshData = new MeshData(verticesX, verticesY);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                int yVertex = y / meshSimplificationIncrement;
                int xVertex = x / meshSimplificationIncrement;

                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                // Fix: Use simplified vertex indices for boundary check
                if (xVertex < verticesX - 1 && yVertex < verticesY - 1)
                {
                    int current = vertexIndex;
                    int nextRow = current + verticesX;

                    meshData.AddTriangle(current, current + 1 + verticesX, nextRow);
                    meshData.AddTriangle(current + 1 + verticesX, current, current + 1);
                }

                float vertexHeight = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, vertexHeight, topLeftZ - y);

                // Log a few sample points for height diagnostic
                if (vertexIndex == 0 || vertexIndex == (verticesX * verticesY) / 2 || vertexIndex == verticesX * verticesY - 1)
                {
                    Debug.Log($"Vertex {vertexIndex} at ({x},{y}) - heightMap={heightMap[x, y]}, curveOut={heightCurve.Evaluate(heightMap[x, y])}, height={vertexHeight}");
                }

                vertexIndex++;
            }
        }
        Debug.Log($"TriangleIndex after generation: {meshData.triangleIndex}");

        Debug.Log("Mesh Gen");

        return meshData;

    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    public int triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
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
        mesh.RecalculateNormals();

        Debug.Log($"Vertices: {vertices.Length}, Triangles: {triangles.Length}");
        return mesh;
    }

}