using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WorldOfVictoria.Core;

public static class UrpPhase8PostProcessSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string VolumeProfilePath = "Assets/_Project/Config/Rendering/VoxelRemake_DefaultVolumeProfile.asset";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        if (visualManager == null)
        {
            Debug.LogError("VisualManager missing in Game scene.");
            return;
        }

        var volume = ResolveOrCreateGlobalVolume(visualManager);
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            Debug.LogError("Default volume profile is missing.");
            return;
        }

        ConfigureProfile(profile);

        volume.isGlobal = true;
        volume.priority = 10f;
        volume.weight = 1f;
        volume.sharedProfile = profile;
        volume.profile = profile;

        visualManager.ConfigureGlobalVolume(volume);

        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(volume);
        EditorUtility.SetDirty(visualManager);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        if (visualManager == null)
        {
            EditorSceneManager.OpenScene(GameScenePath);
            visualManager = Object.FindAnyObjectByType<VisualManager>();
        }

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        var volume = visualManager != null ? visualManager.GlobalVolume : null;
        if (profile == null || volume == null)
        {
            return "Volume profile or Global Volume missing.";
        }

        profile.TryGet<Bloom>(out var bloom);
        profile.TryGet<ColorAdjustments>(out var colorAdjustments);
        profile.TryGet<Tonemapping>(out var tonemapping);

        return
            $"GlobalVolume={volume.name}; " +
            $"SharedProfile={(volume.sharedProfile != null ? volume.sharedProfile.name : "null")}; " +
            $"Bloom={(bloom != null ? bloom.active.ToString() : "missing")}; " +
            $"BloomIntensity={(bloom != null ? bloom.intensity.value.ToString("0.00") : "n/a")}; " +
            $"Exposure={(colorAdjustments != null ? colorAdjustments.postExposure.value.ToString("0.00") : "n/a")}; " +
            $"Contrast={(colorAdjustments != null ? colorAdjustments.contrast.value.ToString("0.0") : "n/a")}; " +
            $"Tonemapping={(tonemapping != null ? tonemapping.mode.value.ToString() : "missing")}";
    }

    private static Volume ResolveOrCreateGlobalVolume(VisualManager visualManager)
    {
        if (visualManager.GlobalVolume != null)
        {
            return visualManager.GlobalVolume;
        }

        var existing = Object.FindAnyObjectByType<Volume>();
        if (existing != null)
        {
            return existing;
        }

        var atmosphereRoot = GameObject.Find("Atmosphere");
        if (atmosphereRoot == null)
        {
            atmosphereRoot = new GameObject("Atmosphere");
        }

        var volumeObject = new GameObject("Global Volume");
        volumeObject.transform.SetParent(atmosphereRoot.transform, false);
        return volumeObject.AddComponent<Volume>();
    }

    private static void ConfigureProfile(VolumeProfile profile)
    {
        var bloom = EnsureOverride<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.32f);
        bloom.scatter.Override(0.68f);
        bloom.tint.Override(new Color(1f, 0.96f, 0.88f, 1f));
        bloom.clamp.Override(65472f);

        var colorAdjustments = EnsureOverride<ColorAdjustments>(profile);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.12f);
        colorAdjustments.contrast.Override(9f);
        colorAdjustments.saturation.Override(-6f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.985f, 0.955f, 1f));

        var tonemapping = EnsureOverride<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);
    }

    private static T EnsureOverride<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet<T>(out var component))
        {
            component = profile.Add<T>(true);
        }

        return component;
    }
}
