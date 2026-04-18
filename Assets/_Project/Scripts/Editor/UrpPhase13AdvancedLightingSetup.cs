using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;
using WorldOfVictoria.Rendering;

public static class UrpPhase13AdvancedLightingSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string BaseRendererPath = "Assets/_Project/Config/Rendering/VoxelRemake_Renderer.asset";
    private const string SsaoRendererPath = "Assets/_Project/Config/Rendering/VoxelRemake_Renderer_SSAO.asset";
    private const string VolumetricShaderPath = "Assets/_Project/Shaders/VoxelVolumetricFog.shader";
    private const string LightingSettingsPath = "Assets/_Project/Config/Rendering/VoxelRemake_LightingSettings.lighting";

    public static void Execute()
    {
        EnsureVolumetricFeature(BaseRendererPath);
        EnsureVolumetricFeature(SsaoRendererPath);

        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        if (gameManager == null || visualManager == null)
        {
            Debug.LogError("GameManager or VisualManager missing in Game scene.");
            return;
        }

        gameManager.GenerateRuntimeWorld();
        gameManager.BuildChunkPreviewMeshes();

        var volumetricController = EnsureVolumetricController(gameManager, visualManager);
        EnsureLightingSettingsAsset();
        EnsureProbeAndReflectionRig(gameManager);

        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        if (visualManager.ScalabilityController != null && presentationController != null && presentationController.Settings != null)
        {
            visualManager.ScalabilityController.Configure(
                visualManager.GlobalVolume,
                presentationController.Settings.BrightMaterial,
                presentationController.Settings.ShadowMaterial,
                visualManager.AtmosphereParticles,
                volumetricController,
                gameManager.WorldConfig);
            EditorUtility.SetDirty(visualManager.ScalabilityController);
        }

        visualManager.ApplyQuality(visualManager.DefaultQualityTier);

        EditorUtility.SetDirty(volumetricController);
        EditorUtility.SetDirty(visualManager);
        EditorUtility.SetDirty(gameManager);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("World of Victoria/Lighting/Bake Preview GI")]
    public static void BakePreviewGi()
    {
        Execute();
        Lightmapping.BakeAsync();
    }

    public static string Report()
    {
        var baseRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(BaseRendererPath);
        var ssaoRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SsaoRendererPath);
        var baseFeature = FindVolumetricFeature(baseRenderer);
        var ssaoFeature = FindVolumetricFeature(ssaoRenderer);
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
        var probeGroup = Object.FindAnyObjectByType<LightProbeGroup>();

        return
            $"BaseVolumetric={(baseFeature != null ? baseFeature.name : "missing")}; " +
            $"SsaoVolumetric={(ssaoFeature != null ? ssaoFeature.name : "missing")}; " +
            $"LightingSettings={(settings != null ? settings.name : "missing")}; " +
            $"ProbeCount={(probeGroup != null && probeGroup.probePositions != null ? probeGroup.probePositions.Length : 0)}";
    }

    private static void EnsureVolumetricFeature(string rendererPath)
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (renderer == null)
        {
            return;
        }

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(VolumetricShaderPath);
        var feature = FindVolumetricFeature(renderer);
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<VoxelVolumetricFogFeature>();
            feature.name = "Voxel Volumetric Fog";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var serializedRenderer = new SerializedObject(renderer);
            var features = serializedRenderer.FindProperty("m_RendererFeatures");
            var featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            var index = features.arraySize;
            features.InsertArrayElementAtIndex(index);
            features.GetArrayElementAtIndex(index).objectReferenceValue = feature;
            featureMap.InsertArrayElementAtIndex(index);
            featureMap.GetArrayElementAtIndex(index).longValue = localId;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        var serializedFeature = new SerializedObject(feature);
        var shaderProp = serializedFeature.FindProperty("shader");
        if (shaderProp != null)
        {
            shaderProp.objectReferenceValue = shader;
        }

        var eventProp = serializedFeature.FindProperty("injectionPoint");
        if (eventProp != null)
        {
            eventProp.intValue = (int)RenderPassEvent.BeforeRenderingPostProcessing;
        }

        serializedFeature.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(renderer);
    }

    private static VoxelVolumetricFogFeature FindVolumetricFeature(UniversalRendererData renderer)
    {
        if (renderer == null)
        {
            return null;
        }

        foreach (var feature in renderer.rendererFeatures)
        {
            if (feature is VoxelVolumetricFogFeature volumetricFeature)
            {
                return volumetricFeature;
            }
        }

        return null;
    }

    private static VolumetricLightingController EnsureVolumetricController(GameManager gameManager, VisualManager visualManager)
    {
        var lightingRoot = GameObject.Find("Lighting") ?? new GameObject("Lighting");
        var volumetricObject = GameObject.Find("Lighting/Volumetric Lighting");
        if (volumetricObject == null)
        {
            volumetricObject = new GameObject("Volumetric Lighting");
            volumetricObject.transform.SetParent(lightingRoot.transform, false);
        }

        var controller = volumetricObject.GetComponent<VolumetricLightingController>();
        if (controller == null)
        {
            controller = volumetricObject.AddComponent<VolumetricLightingController>();
        }

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("gameManager").objectReferenceValue = gameManager;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        visualManager.ConfigureVolumetricLighting(controller);
        return controller;
    }

    private static void EnsureLightingSettingsAsset()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
        if (settings == null)
        {
            settings = new LightingSettings();
            AssetDatabase.CreateAsset(settings, LightingSettingsPath);
        }

        settings.realtimeGI = false;
        settings.bakedGI = true;
        settings.maxBounces = 2;
        settings.environmentSampleCount = 256;
        settings.indirectSampleCount = 256;
        settings.directSampleCount = 64;
        Lightmapping.lightingSettings = settings;
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        EditorUtility.SetDirty(settings);
    }

    private static void EnsureProbeAndReflectionRig(GameManager gameManager)
    {
        var lightingRoot = GameObject.Find("Lighting") ?? new GameObject("Lighting");
        var giRoot = GameObject.Find("Lighting/GI") ?? new GameObject("GI");
        giRoot.transform.SetParent(lightingRoot.transform, false);

        var probeObject = GameObject.Find("Lighting/GI/World Probes") ?? new GameObject("World Probes");
        probeObject.transform.SetParent(giRoot.transform, false);
        var probeGroup = probeObject.GetComponent<LightProbeGroup>();
        if (probeGroup == null)
        {
            probeGroup = probeObject.AddComponent<LightProbeGroup>();
        }

        probeGroup.probePositions = GenerateProbePositions(gameManager);

        var reflectionObject = GameObject.Find("Lighting/GI/World Reflection Probe") ?? new GameObject("World Reflection Probe");
        reflectionObject.transform.SetParent(giRoot.transform, false);
        var reflectionProbe = reflectionObject.GetComponent<ReflectionProbe>();
        if (reflectionProbe == null)
        {
            reflectionProbe = reflectionObject.AddComponent<ReflectionProbe>();
        }

        var worldConfig = gameManager.WorldConfig;
        reflectionObject.transform.position = new Vector3(worldConfig.Width * 0.5f, worldConfig.Depth * 0.5f, worldConfig.Height * 0.5f);
        reflectionProbe.mode = ReflectionProbeMode.Baked;
        reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        reflectionProbe.boxProjection = true;
        reflectionProbe.size = new Vector3(worldConfig.Width + 8f, worldConfig.Depth + 8f, worldConfig.Height + 8f);
        reflectionProbe.intensity = 0.85f;
        reflectionProbe.clearFlags = ReflectionProbeClearFlags.Skybox;

        EditorUtility.SetDirty(probeGroup);
        EditorUtility.SetDirty(reflectionProbe);
    }

    private static Vector3[] GenerateProbePositions(GameManager gameManager)
    {
        var positions = new List<Vector3>(512);
        var config = gameManager.WorldConfig;
        var world = gameManager.RuntimeWorldData;
        var stepXZ = Mathf.Max(10, config.ChunkSize);
        var stepY = 8;

        for (var x = 4; x < config.Width - 4; x += stepXZ)
        {
            for (var z = 4; z < config.Height - 4; z += stepXZ)
            {
                for (var y = 3; y < config.Depth - 2; y += stepY)
                {
                    if (world != null && world.IsSolidBlock(x, y, z))
                    {
                        continue;
                    }

                    var skylit = world == null || world.GetSkyLightLevel(x, y, z) > 0;
                    var nearGeometry = world == null || HasSolidNeighbor(world, x, y, z);
                    if (!skylit && !nearGeometry)
                    {
                        continue;
                    }

                    positions.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                }

                var surfaceY = Mathf.Min(config.Depth - 2, config.Depth - 1);
                if (world == null || !world.IsSolidBlock(x, surfaceY, z))
                {
                    positions.Add(new Vector3(x + 0.5f, config.Depth - 1.5f, z + 0.5f));
                }
            }
        }

        return positions.ToArray();
    }

    private static bool HasSolidNeighbor(WorldData world, int x, int y, int z)
    {
        return world.IsSolidBlock(x + 1, y, z)
            || world.IsSolidBlock(x - 1, y, z)
            || world.IsSolidBlock(x, y + 1, z)
            || world.IsSolidBlock(x, y - 1, z)
            || world.IsSolidBlock(x, y, z + 1)
            || world.IsSolidBlock(x, y, z - 1);
    }
}
