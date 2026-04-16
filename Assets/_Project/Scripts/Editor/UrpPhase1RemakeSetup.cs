using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WorldOfVictoria.Core;

public static class UrpPhase1RemakeSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string RenderingFolder = "Assets/_Project/Config/Rendering";
    private const string RendererTemplatePath = "Assets/Settings/Mobile_Renderer.asset";
    private const string PipelineTemplatePath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string RendererPath = RenderingFolder + "/VoxelRemake_Renderer.asset";
    private const string VolumeProfilePath = RenderingFolder + "/VoxelRemake_DefaultVolumeProfile.asset";
    private const string UltraPipelinePath = RenderingFolder + "/VoxelRemake_Ultra.asset";
    private const string HighPipelinePath = RenderingFolder + "/VoxelRemake_High.asset";
    private const string MediumPipelinePath = RenderingFolder + "/VoxelRemake_Medium.asset";
    private const string LowPipelinePath = RenderingFolder + "/VoxelRemake_Low.asset";

    public static void Execute()
    {
        EnsureFolder(RenderingFolder);

        var renderer = EnsureRendererAsset();
        var volumeProfile = EnsureVolumeProfile();

        var ultra = EnsurePipelineAsset(UltraPipelinePath, "VoxelRemake_Ultra", renderer, volumeProfile, 1f, true, true, true, true, 2048, 4, 75f, 4);
        var high = EnsurePipelineAsset(HighPipelinePath, "VoxelRemake_High", renderer, volumeProfile, 1f, true, true, true, true, 2048, 2, 50f, 4);
        var medium = EnsurePipelineAsset(MediumPipelinePath, "VoxelRemake_Medium", renderer, volumeProfile, 0.9f, true, true, true, false, 1024, 2, 40f, 2);
        var low = EnsurePipelineAsset(LowPipelinePath, "VoxelRemake_Low", renderer, volumeProfile, 0.8f, false, false, false, false, 1024, 1, 20f, 0);

        ConfigureQualitySettingsYaml(ultra, high, medium, low);
        ConfigureGameScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static ScriptableObject EnsureRendererAsset()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
        if (renderer == null)
        {
            AssetDatabase.CopyAsset(RendererTemplatePath, RendererPath);
            renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
        }

        var serialized = new SerializedObject(renderer);
        serialized.FindProperty("m_Name").stringValue = "VoxelRemake_Renderer";
        serialized.FindProperty("m_RendererFeatures").ClearArray();
        serialized.FindProperty("m_UseNativeRenderPass").boolValue = true;
        serialized.FindProperty("m_RenderingMode").intValue = 0;
        serialized.FindProperty("m_DepthPrimingMode").intValue = 0;
        serialized.FindProperty("m_CopyDepthMode").intValue = 0;
        serialized.FindProperty("m_IntermediateTextureMode").intValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return renderer;
    }

    private static VolumeProfile EnsureVolumeProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null)
        {
            return profile;
        }

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        return profile;
    }

    private static ScriptableObject EnsurePipelineAsset(
        string path,
        string assetName,
        ScriptableObject renderer,
        VolumeProfile volumeProfile,
        float renderScale,
        bool hdr,
        bool depthTexture,
        bool mainLightShadows,
        bool softShadows,
        int shadowResolution,
        int cascades,
        float shadowDistance,
        int msaa)
    {
        var pipelineAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        if (pipelineAsset == null)
        {
            AssetDatabase.CopyAsset(PipelineTemplatePath, path);
            pipelineAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        }

        var serialized = new SerializedObject(pipelineAsset);
        serialized.FindProperty("m_Name").stringValue = assetName;
        serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        serialized.FindProperty("m_DefaultRendererIndex").intValue = 0;
        serialized.FindProperty("m_RequireDepthTexture").boolValue = depthTexture;
        serialized.FindProperty("m_RequireOpaqueTexture").boolValue = false;
        serialized.FindProperty("m_SupportsHDR").boolValue = hdr;
        serialized.FindProperty("m_MSAA").intValue = msaa;
        serialized.FindProperty("m_RenderScale").floatValue = renderScale;
        serialized.FindProperty("m_MainLightRenderingMode").intValue = 1;
        serialized.FindProperty("m_MainLightShadowsSupported").boolValue = mainLightShadows;
        serialized.FindProperty("m_MainLightShadowmapResolution").intValue = shadowResolution;
        serialized.FindProperty("m_AdditionalLightsRenderingMode").intValue = 1;
        serialized.FindProperty("m_AdditionalLightsPerObjectLimit").intValue = 8;
        serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
        serialized.FindProperty("m_ShadowDistance").floatValue = shadowDistance;
        serialized.FindProperty("m_ShadowCascadeCount").intValue = cascades;
        serialized.FindProperty("m_AnyShadowsSupported").boolValue = mainLightShadows;
        serialized.FindProperty("m_SoftShadowsSupported").boolValue = softShadows;
        serialized.FindProperty("m_SoftShadowQuality").intValue = softShadows ? 2 : 0;
        serialized.FindProperty("m_UseSRPBatcher").boolValue = true;
        serialized.FindProperty("m_SupportsDynamicBatching").boolValue = false;
        serialized.FindProperty("m_ColorGradingMode").intValue = 0;
        serialized.FindProperty("m_AllowPostProcessAlphaOutput").boolValue = false;
        serialized.FindProperty("m_VolumeProfile").objectReferenceValue = volumeProfile;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return pipelineAsset;
    }

    private static void ConfigureQualitySettingsYaml(
        ScriptableObject ultraPipeline,
        ScriptableObject highPipeline,
        ScriptableObject mediumPipeline,
        ScriptableObject lowPipeline)
    {
        var ultraGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(ultraPipeline));
        var highGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(highPipeline));
        var mediumGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mediumPipeline));
        var lowGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(lowPipeline));
        var qualitySettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings/QualitySettings.asset");

        var yaml = $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!47 &1
QualitySettings:
  m_ObjectHideFlags: 0
  serializedVersion: 5
  m_CurrentQuality: 1
  m_QualitySettings:
  - serializedVersion: 5
    name: Ultra
    pixelLightCount: 4
    shadows: 2
    shadowResolution: 3
    shadowProjection: 1
    shadowCascades: 4
    shadowDistance: 75
    shadowNearPlaneOffset: 3
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.123, y: 0.293, z: 0.536}}
    shadowmaskMode: 1
    skinWeights: 4
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 2
    antiAliasing: 4
    softParticles: 1
    softVegetation: 1
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 1
    useLegacyDetailDistribution: 1
    adaptiveVsync: 0
    vSyncCount: 0
    realtimeGICPUUsage: 100
    adaptiveVsyncExtraA: 0
    adaptiveVsyncExtraB: 0
    lodBias: 2
    meshLodThreshold: 1
    maximumLODLevel: 0
    enableLODCrossFade: 1
    streamingMipmapsActive: 0
    streamingMipmapsAddAllCameras: 1
    streamingMipmapsMemoryBudget: 512
    streamingMipmapsRenderersPerFrame: 512
    streamingMipmapsMaxLevelReduction: 2
    streamingMipmapsMaxFileIORequests: 1024
    particleRaycastBudget: 512
    asyncUploadTimeSlice: 2
    asyncUploadBufferSize: 16
    asyncUploadPersistentBuffer: 1
    resolutionScalingFixedDPIFactor: 1
    customRenderPipeline: {{fileID: 11400000, guid: {ultraGuid}, type: 2}}
    terrainQualityOverrides: 0
    terrainPixelError: 1
    terrainDetailDensityScale: 1
    terrainBasemapDistance: 1000
    terrainDetailDistance: 80
    terrainTreeDistance: 5000
    terrainBillboardStart: 50
    terrainFadeLength: 5
    terrainMaxTrees: 50
    excludedTargetPlatforms: []
  - serializedVersion: 5
    name: High
    pixelLightCount: 4
    shadows: 2
    shadowResolution: 3
    shadowProjection: 1
    shadowCascades: 2
    shadowDistance: 50
    shadowNearPlaneOffset: 3
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.123, y: 0.293, z: 0.536}}
    shadowmaskMode: 1
    skinWeights: 4
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 2
    antiAliasing: 4
    softParticles: 1
    softVegetation: 1
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 1
    useLegacyDetailDistribution: 1
    adaptiveVsync: 0
    vSyncCount: 0
    realtimeGICPUUsage: 100
    adaptiveVsyncExtraA: 0
    adaptiveVsyncExtraB: 0
    lodBias: 2
    meshLodThreshold: 1
    maximumLODLevel: 0
    enableLODCrossFade: 1
    streamingMipmapsActive: 0
    streamingMipmapsAddAllCameras: 1
    streamingMipmapsMemoryBudget: 512
    streamingMipmapsRenderersPerFrame: 512
    streamingMipmapsMaxLevelReduction: 2
    streamingMipmapsMaxFileIORequests: 1024
    particleRaycastBudget: 512
    asyncUploadTimeSlice: 2
    asyncUploadBufferSize: 16
    asyncUploadPersistentBuffer: 1
    resolutionScalingFixedDPIFactor: 1
    customRenderPipeline: {{fileID: 11400000, guid: {highGuid}, type: 2}}
    terrainQualityOverrides: 0
    terrainPixelError: 1
    terrainDetailDensityScale: 1
    terrainBasemapDistance: 1000
    terrainDetailDistance: 80
    terrainTreeDistance: 5000
    terrainBillboardStart: 50
    terrainFadeLength: 5
    terrainMaxTrees: 50
    excludedTargetPlatforms: []
  - serializedVersion: 5
    name: Medium
    pixelLightCount: 2
    shadows: 2
    shadowResolution: 2
    shadowProjection: 1
    shadowCascades: 2
    shadowDistance: 40
    shadowNearPlaneOffset: 3
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.123, y: 0.293, z: 0.536}}
    shadowmaskMode: 1
    skinWeights: 4
    globalTextureMipmapLimit: 0
    textureMipmapLimitSettings: []
    anisotropicTextures: 1
    antiAliasing: 2
    softParticles: 0
    softVegetation: 1
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 1
    useLegacyDetailDistribution: 1
    adaptiveVsync: 0
    vSyncCount: 0
    realtimeGICPUUsage: 100
    adaptiveVsyncExtraA: 0
    adaptiveVsyncExtraB: 0
    lodBias: 1.5
    meshLodThreshold: 1
    maximumLODLevel: 0
    enableLODCrossFade: 1
    streamingMipmapsActive: 0
    streamingMipmapsAddAllCameras: 1
    streamingMipmapsMemoryBudget: 512
    streamingMipmapsRenderersPerFrame: 512
    streamingMipmapsMaxLevelReduction: 2
    streamingMipmapsMaxFileIORequests: 1024
    particleRaycastBudget: 384
    asyncUploadTimeSlice: 2
    asyncUploadBufferSize: 16
    asyncUploadPersistentBuffer: 1
    resolutionScalingFixedDPIFactor: 1
    customRenderPipeline: {{fileID: 11400000, guid: {mediumGuid}, type: 2}}
    terrainQualityOverrides: 0
    terrainPixelError: 1
    terrainDetailDensityScale: 1
    terrainBasemapDistance: 1000
    terrainDetailDistance: 80
    terrainTreeDistance: 5000
    terrainBillboardStart: 50
    terrainFadeLength: 5
    terrainMaxTrees: 50
    excludedTargetPlatforms: []
  - serializedVersion: 5
    name: Low
    pixelLightCount: 1
    shadows: 0
    shadowResolution: 1
    shadowProjection: 1
    shadowCascades: 1
    shadowDistance: 20
    shadowNearPlaneOffset: 3
    shadowCascade2Split: 0.33333334
    shadowCascade4Split: {{x: 0.067, y: 0.2, z: 0.467}}
    shadowmaskMode: 0
    skinWeights: 2
    globalTextureMipmapLimit: 1
    textureMipmapLimitSettings: []
    anisotropicTextures: 0
    antiAliasing: 0
    softParticles: 0
    softVegetation: 1
    realtimeReflectionProbes: 0
    billboardsFaceCameraPosition: 1
    useLegacyDetailDistribution: 1
    adaptiveVsync: 0
    vSyncCount: 0
    realtimeGICPUUsage: 100
    adaptiveVsyncExtraA: 0
    adaptiveVsyncExtraB: 0
    lodBias: 1
    meshLodThreshold: 1
    maximumLODLevel: 0
    enableLODCrossFade: 1
    streamingMipmapsActive: 0
    streamingMipmapsAddAllCameras: 1
    streamingMipmapsMemoryBudget: 512
    streamingMipmapsRenderersPerFrame: 512
    streamingMipmapsMaxLevelReduction: 2
    streamingMipmapsMaxFileIORequests: 1024
    particleRaycastBudget: 256
    asyncUploadTimeSlice: 2
    asyncUploadBufferSize: 16
    asyncUploadPersistentBuffer: 1
    resolutionScalingFixedDPIFactor: 1
    customRenderPipeline: {{fileID: 11400000, guid: {lowGuid}, type: 2}}
    terrainQualityOverrides: 0
    terrainPixelError: 1
    terrainDetailDensityScale: 1
    terrainBasemapDistance: 1000
    terrainDetailDistance: 80
    terrainTreeDistance: 5000
    terrainBillboardStart: 50
    terrainFadeLength: 5
    terrainMaxTrees: 50
    excludedTargetPlatforms: []
  m_TextureMipmapLimitGroupNames: []
  m_PerPlatformDefaultQuality:
    Android: 3
    AndroidXR: 3
    GameCoreScarlett: 1
    GameCoreXboxOne: 1
    Lumin: 3
    MetaQuest: 3
    Nintendo Switch: 2
    Nintendo Switch 2: 2
    PS4: 1
    PS5: 1
    Server: 3
    Stadia: 3
    Standalone: 1
    WebGL: 3
    Windows Store Apps: 3
    XboxOne: 2
    iPhone: 3
    tvOS: 3
";

        File.WriteAllText(qualitySettingsPath, yaml);
    }

    private static void ConfigureGameScene()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager missing in Game scene.");
            return;
        }

        var lightingRoot = GameObject.Find("Lighting");
        var directionalLightObject = GameObject.Find("Lighting/Directional Light") ?? GameObject.Find("Directional Light");
        if (directionalLightObject != null && directionalLightObject.TryGetComponent<Light>(out var directionalLight))
        {
            if (lightingRoot != null)
            {
                directionalLightObject.transform.SetParent(lightingRoot.transform, true);
            }

            directionalLight.type = LightType.Directional;
            directionalLight.color = new Color(1f, 0.97f, 0.92f, 1f);
            directionalLight.intensity = 1.15f;
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.transform.rotation = Quaternion.Euler(42f, 28f, 0f);
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.58f, 0.64f, 0.72f, 1f);
        RenderSettings.fog = false;

        var visualManager = gameManager.GetComponent<VisualManager>();
        if (visualManager == null)
        {
            visualManager = gameManager.gameObject.AddComponent<VisualManager>();
        }

        var serialized = new SerializedObject(visualManager);
        serialized.FindProperty("mainDirectionalLight").objectReferenceValue = directionalLightObject != null ? directionalLightObject.GetComponent<Light>() : null;
        serialized.FindProperty("defaultQualityTier").stringValue = "High";
        serialized.FindProperty("applyQualityOnAwake").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
    }

    private static void EnsureFolder(string folderPath)
    {
        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
