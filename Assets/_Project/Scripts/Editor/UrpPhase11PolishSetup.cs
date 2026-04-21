using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;

public static class UrpPhase11PolishSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string VolumeProfilePath = "Assets/_Project/Config/Rendering/VoxelRemake_DefaultVolumeProfile.asset";
    private const string BrightMaterialPath = "Assets/_Project/Materials/PBR/ChunkPBR_Bright.mat";
    private const string ShadowMaterialPath = "Assets/_Project/Materials/PBR/ChunkPBR_Shadow.mat";
    private const string DustMaterialPath = "Assets/_Project/Materials/VFX/DustMotes_URP.mat";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var dustMotes = GameObject.Find("Player/DustMotes")?.GetComponent<ParticleSystem>();
        var presentationController = gameManager != null ? gameManager.GetComponent<ChunkPresentationController>() : null;
        if (visualManager == null || gameManager == null || dustMotes == null || presentationController == null || presentationController.Settings == null)
        {
            Debug.LogError("Phase 11 setup requires VisualManager, GameManager, ChunkPresentationController and Player/DustMotes.");
            return;
        }

        ConfigureDustMotes(dustMotes);
        ConfigurePostProcessing();
        ConfigureChunkMaterials(presentationController.Settings.BrightMaterial, presentationController.Settings.ShadowMaterial);
        ConfigureLighting();
        var scalabilityController = visualManager.GetComponent<VisualScalabilityController>();
        scalabilityController?.ResetProfilesToRecommendedDefaults();

        visualManager.ApplyQuality(visualManager.DefaultQualityTier);

        EditorUtility.SetDirty(visualManager);
        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(dustMotes);
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

        var dustMotes = GameObject.Find("Player/DustMotes")?.GetComponent<ParticleSystem>();
        var presentationController = Object.FindAnyObjectByType<GameManager>()?.GetComponent<ChunkPresentationController>();
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (visualManager == null || dustMotes == null || profile == null || presentationController == null || presentationController.Settings == null)
        {
            return "Phase 11 polish setup is incomplete.";
        }

        profile.TryGet<Bloom>(out var bloom);
        profile.TryGet<ColorAdjustments>(out var colorAdjustments);

        var renderer = dustMotes.GetComponent<ParticleSystemRenderer>();
        var brightMaterial = presentationController.Settings.BrightMaterial;
        var shadowMaterial = presentationController.Settings.ShadowMaterial;

        return
            $"DustMaterial={(renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null")}; " +
            $"DustLifetime={dustMotes.main.startLifetime.constant:0.##}; " +
            $"DustSpeed={dustMotes.main.startSpeed.constant:0.##}; " +
            $"BloomIntensity={(bloom != null ? bloom.intensity.value.ToString("0.00") : "n/a")}; " +
            $"Exposure={(colorAdjustments != null ? colorAdjustments.postExposure.value.ToString("0.00") : "n/a")}; " +
            $"BrightTint={(brightMaterial != null ? brightMaterial.GetColor("_BaseTint").ToString() : "n/a")}; " +
            $"ShadowTint={(shadowMaterial != null ? shadowMaterial.GetColor("_BaseTint").ToString() : "n/a")}";
    }

    private static void ConfigureDustMotes(ParticleSystem dustMotes)
    {
        var dustMaterial = EnsureDustMaterial();
        var renderer = dustMotes.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = dustMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.maxParticleSize = 0.022f;
            renderer.minParticleSize = 0.0001f;
            renderer.lengthScale = 1f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(renderer);
        }

        var main = dustMotes.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8.5f, 13.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startRotation3D = false;
        main.gravityModifier = 0f;
        main.maxParticles = Mathf.Max(main.maxParticles, 180);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.91f, 0.88f, 0.81f, 0.05f),
            new Color(0.83f, 0.80f, 0.74f, 0.11f));

        var emission = dustMotes.emission;
        emission.enabled = true;

        var shape = dustMotes.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;

        var velocityOverLifetime = dustMotes.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.005f, 0.02f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.025f, 0.025f);

        var noise = dustMotes.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = 0.08f;
        noise.strengthY = 0.03f;
        noise.strengthZ = 0.08f;
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.05f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var colorOverLifetime = dustMotes.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.92f, 0.89f, 0.84f), 0f),
                new GradientColorKey(new Color(0.84f, 0.81f, 0.77f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.12f, 0.18f),
                new GradientAlphaKey(0.08f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        if (!dustMotes.isPlaying)
        {
            dustMotes.Play();
        }
    }

    private static void ConfigurePostProcessing()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            Debug.LogError("Phase 11 volume profile is missing.");
            return;
        }

        var bloom = EnsureOverride<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.92f);
        bloom.intensity.Override(0.22f);
        bloom.scatter.Override(0.62f);
        bloom.tint.Override(new Color(1f, 0.965f, 0.89f, 1f));

        var colorAdjustments = EnsureOverride<ColorAdjustments>(profile);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.15f);
        colorAdjustments.contrast.Override(10f);
        colorAdjustments.saturation.Override(9f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.985f, 0.95f, 1f));

        var tonemapping = EnsureOverride<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        EditorUtility.SetDirty(profile);
    }

    private static void ConfigureChunkMaterials(Material brightMaterial, Material shadowMaterial)
    {
        if (brightMaterial == null || shadowMaterial == null)
        {
            brightMaterial = AssetDatabase.LoadAssetAtPath<Material>(BrightMaterialPath);
            shadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
        }

        if (brightMaterial == null || shadowMaterial == null)
        {
            Debug.LogError("Phase 11 PBR chunk materials are missing.");
            return;
        }

        brightMaterial.SetColor("_BaseTint", new Color(1.08f, 1.06f, 1.0f, 1f));
        brightMaterial.SetFloat("_AlbedoContrast", 1.18f);
        brightMaterial.SetFloat("_UseVertexBrightness", 1f);
        brightMaterial.SetFloat("_VertexLightBlend", 0.42f);
        brightMaterial.SetFloat("_AoStrength", 0.03f);
        brightMaterial.SetFloat("_LightVolumeStrength", 0f);
        brightMaterial.SetFloat("_BrightnessFloor", 0f);
        brightMaterial.SetFloat("_BrightnessBlackPoint", 0.055f);
        brightMaterial.SetFloat("_BrightnessWhitePoint", 0.98f);
        brightMaterial.SetFloat("_BrightnessGamma", 1.28f);
        brightMaterial.SetFloat("_ShadowBoost", 0.12f);
        brightMaterial.SetFloat("_RoughnessBias", -0.06f);

        shadowMaterial.SetColor("_BaseTint", new Color(1.08f, 1.06f, 1.0f, 1f));
        shadowMaterial.SetFloat("_AlbedoContrast", 1.18f);
        shadowMaterial.SetFloat("_UseVertexBrightness", 1f);
        shadowMaterial.SetFloat("_VertexLightBlend", 0.42f);
        shadowMaterial.SetFloat("_AoStrength", 0.03f);
        shadowMaterial.SetFloat("_LightVolumeStrength", 0f);
        shadowMaterial.SetFloat("_BrightnessFloor", 0f);
        shadowMaterial.SetFloat("_BrightnessBlackPoint", 0.055f);
        shadowMaterial.SetFloat("_BrightnessWhitePoint", 0.98f);
        shadowMaterial.SetFloat("_BrightnessGamma", 1.28f);
        shadowMaterial.SetFloat("_ShadowBoost", 0.12f);
        shadowMaterial.SetFloat("_RoughnessBias", -0.06f);

        EditorUtility.SetDirty(brightMaterial);
        EditorUtility.SetDirty(shadowMaterial);
    }

    private static void ConfigureLighting()
    {
        RenderSettings.ambientLight = new Color(0.58f, 0.62f, 0.66f, 1f);
        RenderSettings.subtractiveShadowColor = new Color(0.19f, 0.22f, 0.26f, 1f);
        RenderSettings.reflectionIntensity = 0.74f;

        var directionalLight = GameObject.Find("Lighting/Directional Light")?.GetComponent<Light>();
        if (directionalLight == null)
        {
            directionalLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
        }

        if (directionalLight != null)
        {
            directionalLight.intensity = 1.28f;
            directionalLight.shadowStrength = 0.78f;
            directionalLight.color = new Color(1f, 0.97f, 0.90f, 1f);
        }
    }

    private static Material EnsureDustMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (material != null)
        {
            return material;
        }

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            Debug.LogError("Could not find a URP dust particle shader.");
            return null;
        }

        var directory = System.IO.Path.GetDirectoryName(DustMaterialPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Materials", "VFX");
        }

        material = new Material(shader);
        material.name = "DustMotes_URP";
        material.SetColor("_BaseColor", new Color(0.86f, 0.82f, 0.76f, 0.12f));
        TrySetFloat(material, "_Surface", 1f);
        TrySetFloat(material, "_Blend", 0f);
        TrySetFloat(material, "_Cull", 0f);
        TrySetFloat(material, "_AlphaClip", 0f);
        TrySetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        TrySetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.renderQueue = (int)RenderQueue.Transparent;
        AssetDatabase.CreateAsset(material, DustMaterialPath);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void TrySetFloat(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
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
