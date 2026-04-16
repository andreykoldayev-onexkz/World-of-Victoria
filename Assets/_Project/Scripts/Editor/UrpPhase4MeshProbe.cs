using UnityEngine;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;

public static class UrpPhase4MeshProbe
{
    public static string Report()
    {
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            return "GameManager missing.";
        }

        gameManager.GenerateRuntimeWorld();
        gameManager.BuildChunkPreviewMeshes();

        var chunkRenderer = Object.FindAnyObjectByType<ChunkRenderer>();
        if (chunkRenderer == null)
        {
            return "ChunkRenderer missing after preview build.";
        }

        var meshFilter = chunkRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return "Chunk mesh missing.";
        }

        var mesh = meshFilter.sharedMesh;
        return
            $"Chunk={chunkRenderer.name}; " +
            $"Vertices={mesh.vertexCount}; " +
            $"Normals={mesh.normals.Length}; " +
            $"Tangents={mesh.tangents.Length}; " +
            $"UV0={mesh.uv.Length}; " +
            $"UV1={mesh.uv2.Length}; " +
            $"SubMeshes={mesh.subMeshCount}; " +
            $"Triangles0={mesh.GetTriangles(0).Length / 3}; " +
            $"Triangles1={mesh.GetTriangles(1).Length / 3}";
    }
}
