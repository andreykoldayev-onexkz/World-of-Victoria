using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WorldOfVictoria.Core;

public static class UrpPhase6LightingSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string UltraPipelinePath = "Assets/_Project/Config/Rendering/VoxelRemake_Ultra.asset";
    private const string HighPipelinePath = "Assets/_Project/Config/Rendering/VoxelRemake_High.asset";
    private const string MediumPipelinePath = "Assets/_Project/Config/Rendering/VoxelRemake_Medium.asset";
    private const string LowPipelinePath = "Assets/_Project/Config/Rendering/VoxelRemake_Low.asset";
    private const string BrightMaterialPath = "Assets/_Project/Materials/PBR/ChunkPBR_Bright.mat";
    private const string ShadowMaterialPath = "Assets/_Project/Materials/PBR/ChunkPBR_Shadow.mat";

    public static void Execute()
    {
        ConfigurePipelineAsset(UltraPipelinePath, true, 2048, 2, 60f, 4);
        ConfigurePipelineAsset(HighPipelinePath, true, 2048, 2, 50f, 4);
        ConfigurePipelineAsset(MediumPipelinePath, true, 1024, 1, 36f, 2);
        ConfigurePipelineAsset(LowPipelinePath, false, 1024, 1, 24f, 0);

        ConfigureChunkMaterials();
        ConfigureGameSceneLighting();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static string Report()
    {
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            EditorSceneManager.OpenScene(GameScenePath);
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }

        var light = GameObject.Find("Lighting/Directional Light");
        if (light == null)
        {
            light = GameObject.Find("Directional Light");
        }

        var directionalLight = light != null ? light.GetComponent<Light>() : null;
        var highPipeline = AssetDatabase.LoadAssetAtPath<ScriptableObject>(HighPipelinePath);
        if (directionalLight == null || highPipeline == null)
        {
            return "Lighting setup missing.";
        }

        var pipeline = new SerializedObject(highPipeline);
        return
            $"LightShadows={directionalLight.shadows}; " +
            $"LightIntensity={directionalLight.intensity}; " +
            $"ShadowStrength={directionalLight.shadowStrength}; " +
            $"HighShadowResolution={pipeline.FindProperty("m_MainLightShadowmapResolution").intValue}; " +
            $"HighCascades={pipeline.FindProperty("m_ShadowCascadeCount").intValue}; " +
            $"HighShadowDistance={pipeline.FindProperty("m_ShadowDistance").floatValue}";
    }

    private static void ConfigurePipelineAsset(string assetPath, bool shadowsEnabled, int shadowResolution, int cascades, float shadowDistance, int msaa)
    {
        var pipelineAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
        if (pipelineAsset == null)
        {
            Debug.LogError($"Pipeline asset missing: {assetPath}");
            return;
        }

        var serialized = new SerializedObject(pipelineAsset);
        serialized.FindProperty("m_MSAA").intValue = msaa;
        serialized.FindProperty("m_MainLightShadowsSupported").boolValue = shadowsEnabled;
        serialized.FindProperty("m_MainLightShadowmapResolution").intValue = shadowResolution;
        serialized.FindProperty("m_ShadowDistance").floatValue = shadowDistance;
        serialized.FindProperty("m_ShadowCascadeCount").intValue = cascades;
        serialized.FindProperty("m_AnyShadowsSupported").boolValue = shadowsEnabled;
        serialized.FindProperty("m_SoftShadowsSupported").boolValue = shadowsEnabled;
        serialized.FindProperty("m_SoftShadowQuality").intValue = shadowsEnabled ? 2 : 0;
        serialized.FindProperty("m_ShadowDepthBias").floatValue = 0.85f;
        serialized.FindProperty("m_ShadowNormalBias").floatValue = 1.25f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureChunkMaterials()
    {
        var brightMaterial = AssetDatabase.LoadAssetAtPath<Material>(BrightMaterialPath);
        var shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
        if (brightMaterial == null || shadowMaterial == null)
        {
            Debug.LogError("Phase 6 chunk PBR materials are missing.");
            return;
        }

        brightMaterial.SetFloat("_UseVertexBrightness", 1f);
        brightMaterial.SetFloat("_BrightnessFloor", 0f);
        brightMaterial.SetFloat("_ShadowBoost", 0.08f);
        brightMaterial.SetFloat("_RoughnessBias", -0.04f);

        shadowMaterial.SetFloat("_UseVertexBrightness", 1f);
        shadowMaterial.SetFloat("_BrightnessFloor", 0f);
        shadowMaterial.SetFloat("_ShadowBoost", 0.12f);
        shadowMaterial.SetFloat("_RoughnessBias", 0.02f);

        EditorUtility.SetDirty(brightMaterial);
        EditorUtility.SetDirty(shadowMaterial);
    }

    private static void ConfigureGameSceneLighting()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.54f, 0.58f, 0.62f, 1f);
        RenderSettings.subtractiveShadowColor = new Color(0.24f, 0.26f, 0.30f, 1f);
        RenderSettings.reflectionIntensity = 0.75f;

        var directionalLightObject = GameObject.Find("Lighting/Directional Light");
        if (directionalLightObject == null)
        {
            directionalLightObject = GameObject.Find("Directional Light");
        }

        if (directionalLightObject != null && directionalLightObject.TryGetComponent<Light>(out var directionalLight))
        {
            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(1f, 0.95f, 0.86f, 1f);
            directionalLight.intensity = 1.18f;
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.shadowStrength = 0.82f;
            directionalLight.shadowBias = 0.04f;
            directionalLight.shadowNormalBias = 0.45f;
            directionalLight.shadowNearPlane = 0.2f;
            directionalLight.renderMode = LightRenderMode.Auto;
            directionalLight.transform.rotation = Quaternion.Euler(48f, 326f, 0f);

            var additionalLightData = directionalLight.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            if (additionalLightData != null)
            {
                additionalLightData.usePipelineSettings = true;
            }

            if (visualManager != null)
            {
                visualManager.ConfigureMainLight(directionalLight);
                EditorUtility.SetDirty(visualManager);
            }
        }

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowDistance = 50f;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
    }
}
