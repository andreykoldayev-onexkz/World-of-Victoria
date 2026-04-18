using UnityEditor;
using UnityEngine;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;
using WorldOfVictoria.Rendering;

public static class UrpPhase5ShaderSetup
{
    private const string ShaderPath = "Assets/_Project/Shaders/VoxelPBR.shader";
    private const string LibraryPath = "Assets/_Project/Config/Rendering/VoxelPbrTextureLibrary.asset";
    private const string BrightMaterialPath = "Assets/_Project/Materials/PBR/ChunkPBR_Bright.mat";
    private const string ShadowMaterialPath = "Assets/_Project/Materials/PBR/ChunkPBR_Shadow.mat";
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        var library = AssetDatabase.LoadAssetAtPath<VoxelPbrTextureLibrary>(LibraryPath);
        if (shader == null || library == null)
        {
            Debug.LogError("VoxelPBR shader or VoxelPbrTextureLibrary is missing.");
            return;
        }

        var brightMaterial = CreateOrUpdateChunkMaterial(BrightMaterialPath, shader, library, 0f);
        var shadowMaterial = CreateOrUpdateChunkMaterial(ShadowMaterialPath, shader, library, 0.08f);

        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GameScenePath);
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager missing in Game scene.");
            return;
        }

        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        if (presentationController == null)
        {
            Debug.LogError("ChunkPresentationController missing in Game scene.");
            return;
        }

        var controllerSerialized = new SerializedObject(presentationController);
        var settings = controllerSerialized.FindProperty("settings").objectReferenceValue as ChunkPresentationSettings;
        if (settings == null)
        {
            Debug.LogError("ChunkPresentationSettings missing.");
            return;
        }

        var settingsSerialized = new SerializedObject(settings);
        settingsSerialized.FindProperty("brightMaterial").objectReferenceValue = brightMaterial;
        settingsSerialized.FindProperty("shadowMaterial").objectReferenceValue = shadowMaterial;
        settingsSerialized.FindProperty("textureLibrary").objectReferenceValue = library;
        settingsSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    public static string Report()
    {
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GameScenePath);
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            return "GameManager missing.";
        }

        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        if (presentationController == null || presentationController.Settings == null)
        {
            return "Chunk presentation settings missing.";
        }

        var bright = presentationController.Settings.BrightMaterial;
        var shadow = presentationController.Settings.ShadowMaterial;
        return
            $"BrightMaterial={(bright != null ? bright.name : "null")}; " +
            $"BrightShader={(bright != null && bright.shader != null ? bright.shader.name : "null")}; " +
            $"ShadowMaterial={(shadow != null ? shadow.name : "null")}; " +
            $"ShadowShader={(shadow != null && shadow.shader != null ? shadow.shader.name : "null")}";
    }

    private static Material CreateOrUpdateChunkMaterial(string assetPath, Shader shader, VoxelPbrTextureLibrary library, float shadowBoost)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            material = new Material(shader)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(assetPath)
            };
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_AlbedoArray", library.AlbedoArray);
        material.SetTexture("_NormalArray", library.NormalArray);
        material.SetTexture("_RoughnessArray", library.RoughnessArray);
        material.SetColor("_BaseTint", Color.white);
        material.SetFloat("_NormalScale", 1f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_VertexLightBlend", 0.42f);
        material.SetFloat("_AoStrength", 0.03f);
        material.SetFloat("_LightVolumeStrength", 0f);
        material.SetFloat("_UseVertexBrightness", 1f);
        material.SetFloat("_BrightnessFloor", 0f);
        material.SetFloat("_BrightnessBlackPoint", 0.055f);
        material.SetFloat("_BrightnessWhitePoint", 0.98f);
        material.SetFloat("_BrightnessGamma", 1.28f);
        material.SetFloat("_ProbeGiStrength", shadowBoost > 0.01f ? 0.14f : 0.16f);
        material.SetFloat("_ShadowBoost", shadowBoost);
        material.SetFloat("_RoughnessBias", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }
}
