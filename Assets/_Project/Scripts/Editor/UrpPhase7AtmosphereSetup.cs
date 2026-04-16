using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WorldOfVictoria.Core;

public static class UrpPhase7AtmosphereSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string VolumeProfilePath = "Assets/_Project/Config/Rendering/VoxelRemake_DefaultVolumeProfile.asset";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        if (gameManager == null || visualManager == null)
        {
            Debug.LogError("GameManager or VisualManager missing in Game scene.");
            return;
        }

        ConfigureFog(gameManager.WorldConfig);
        var volume = EnsureGlobalVolume(visualManager);
        var dustMotes = EnsureDustMotes(gameManager.PlayerRoot, visualManager);

        visualManager.ConfigureGlobalVolume(volume);
        visualManager.ConfigureDustMotes(dustMotes);
        EditorUtility.SetDirty(visualManager);

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        if (gameManager == null || visualManager == null)
        {
            EditorSceneManager.OpenScene(GameScenePath);
            gameManager = Object.FindAnyObjectByType<GameManager>();
            visualManager = Object.FindAnyObjectByType<VisualManager>();
        }

        if (gameManager == null || visualManager == null)
        {
            return "GameManager or VisualManager missing.";
        }

        return
            $"FogEnabled={RenderSettings.fog}; " +
            $"FogMode={RenderSettings.fogMode}; " +
            $"FogDensity={RenderSettings.fogDensity}; " +
            $"FogColor={RenderSettings.fogColor}; " +
            $"DustMotes={(visualManager.DustMotes != null ? visualManager.DustMotes.name : "null")}; " +
            $"GlobalVolume={(visualManager.GlobalVolume != null ? visualManager.GlobalVolume.name : "null")}";
    }

    private static void ConfigureFog(WorldConfig worldConfig)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = worldConfig.FogColor;
        RenderSettings.fogDensity = worldConfig.FogDensity;
        RenderSettings.fogStartDistance = worldConfig.FogStart;
        RenderSettings.fogEndDistance = worldConfig.FogEnd;
    }

    private static Volume EnsureGlobalVolume(VisualManager visualManager)
    {
        var volumeObject = GameObject.Find("Atmosphere/Global Volume");
        if (volumeObject == null)
        {
            var atmosphereRoot = GameObject.Find("Atmosphere");
            if (atmosphereRoot == null)
            {
                atmosphereRoot = new GameObject("Atmosphere");
            }

            volumeObject = new GameObject("Global Volume");
            volumeObject.transform.SetParent(atmosphereRoot.transform, false);
        }

        var volume = volumeObject.GetComponent<Volume>();
        if (volume == null)
        {
            volume = volumeObject.AddComponent<Volume>();
        }

        volume.isGlobal = true;
        volume.priority = 5f;
        volume.weight = 1f;
        volume.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        EditorUtility.SetDirty(volume);
        return volume;
    }

    private static ParticleSystem EnsureDustMotes(Transform playerRoot, VisualManager visualManager)
    {
        var dustTransform = GameObject.Find("Atmosphere/DustMotes");
        if (dustTransform == null)
        {
            var atmosphereRoot = GameObject.Find("Atmosphere");
            if (atmosphereRoot == null)
            {
                atmosphereRoot = new GameObject("Atmosphere");
            }

            var dustObject = new GameObject("DustMotes");
            dustObject.transform.SetParent(atmosphereRoot.transform, false);
            dustTransform = dustObject;
        }

        if (playerRoot != null)
        {
            dustTransform.transform.SetParent(playerRoot, false);
            dustTransform.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        }

        var particleSystem = dustTransform.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = dustTransform.AddComponent<ParticleSystem>();
        }

        var particleRenderer = dustTransform.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
        {
            particleRenderer = dustTransform.AddComponent<ParticleSystemRenderer>();
        }

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.83f, 0.79f, 0.71f, 0.12f));
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 22f;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(9f, 4f, 9f);
        shape.position = new Vector3(0f, 0f, 2f);

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

        var noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.15f, 1f, 1f));

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.84f, 0.81f, 0.74f), 0f),
                new GradientColorKey(new Color(0.73f, 0.70f, 0.66f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.12f, 0.2f),
                new GradientAlphaKey(0.1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        particleRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        particleRenderer.minParticleSize = 0.0001f;
        particleRenderer.maxParticleSize = 0.04f;

        if (!Application.isPlaying)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play();
        }

        EditorUtility.SetDirty(particleSystem);
        EditorUtility.SetDirty(particleRenderer);
        return particleSystem;
    }
}
