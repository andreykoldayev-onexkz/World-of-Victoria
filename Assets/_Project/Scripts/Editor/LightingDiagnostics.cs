using UnityEditor;
using UnityEngine;

public static class LightingDiagnostics
{
    public static string Execute()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Shaders/VoxelPBR.shader");
        var brightMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/PBR/ChunkPBR_Bright.mat");
        var shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/PBR/ChunkPBR_Shadow.mat");

        if (shader == null)
        {
            return "Shader missing.";
        }

        var message =
            $"ShaderSupported={shader.isSupported}; " +
            $"BrightShader={(brightMaterial != null && brightMaterial.shader != null ? brightMaterial.shader.name : "null")}; " +
            $"ShadowShader={(shadowMaterial != null && shadowMaterial.shader != null ? shadowMaterial.shader.name : "null")}; " +
            $"BrightAlbedo={(brightMaterial != null && brightMaterial.GetTexture("_AlbedoArray") != null ? "set" : "null")}; " +
            $"BrightVolume={(brightMaterial != null && brightMaterial.GetTexture("_SkyLightVolume") != null ? "set" : "null")}; " +
            $"ShadowAlbedo={(shadowMaterial != null && shadowMaterial.GetTexture("_AlbedoArray") != null ? "set" : "null")}; " +
            $"ShadowVolume={(shadowMaterial != null && shadowMaterial.GetTexture("_SkyLightVolume") != null ? "set" : "null")}";

        return message;
    }
}
