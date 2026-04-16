using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WorldOfVictoria.Chunking;

public static class RuntimeChunkMaterialDiagnostics
{
    public static string Execute()
    {
        var chunkRenderers = Object.FindObjectsByType<ChunkRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (chunkRenderers.Length == 0)
        {
            return "RuntimeChunks=0";
        }

        var uniqueShaders = new HashSet<string>();
        var uniqueMaterials = new HashSet<string>();
        var missingAlbedo = 0;
        var missingNormal = 0;
        var missingRoughness = 0;
        var wrongShader = 0;

        foreach (var chunkRenderer in chunkRenderers)
        {
            var renderer = chunkRenderer.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                continue;
            }

            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                uniqueMaterials.Add(material.name);
                uniqueShaders.Add(material.shader != null ? material.shader.name : "null");

                if (material.shader == null || material.shader.name != "WorldOfVictoria/VoxelPBR")
                {
                    wrongShader++;
                }

                if (material.GetTexture("_AlbedoArray") == null)
                {
                    missingAlbedo++;
                }

                if (material.GetTexture("_NormalArray") == null)
                {
                    missingNormal++;
                }

                if (material.GetTexture("_RoughnessArray") == null)
                {
                    missingRoughness++;
                }
            }
        }

        return
            $"RuntimeChunks={chunkRenderers.Length}; " +
            $"Materials={string.Join(",", uniqueMaterials.OrderBy(name => name))}; " +
            $"Shaders={string.Join(",", uniqueShaders.OrderBy(name => name))}; " +
            $"WrongShader={wrongShader}; " +
            $"MissingAlbedo={missingAlbedo}; " +
            $"MissingNormal={missingNormal}; " +
            $"MissingRoughness={missingRoughness}";
    }
}
