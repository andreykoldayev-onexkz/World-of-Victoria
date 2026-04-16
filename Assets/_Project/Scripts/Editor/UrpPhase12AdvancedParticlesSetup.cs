using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WorldOfVictoria.Core;
using WorldOfVictoria.Player;

public static class UrpPhase12AdvancedParticlesSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string VfxFolder = "Assets/_Project/Materials/VFX";
    private const string FootstepMaterialPath = VfxFolder + "/FootstepDust_URP.mat";
    private const string BreathMaterialPath = VfxFolder + "/BreathParticles_URP.mat";
    private const string CaveFogMaterialPath = VfxFolder + "/CaveFog_URP.mat";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var player = Object.FindAnyObjectByType<PlayerController>();
        var playerRoot = GameObject.Find("Player")?.transform;
        if (visualManager == null || gameManager == null || player == null || playerRoot == null)
        {
            Debug.LogError("Phase 12 advanced particles requires VisualManager, GameManager and Player.");
            return;
        }

        var controller = visualManager.AtmosphereParticles ?? visualManager.GetComponent<AtmosphereParticleController>();
        if (controller == null)
        {
            controller = visualManager.gameObject.AddComponent<AtmosphereParticleController>();
        }

        var reactiveRoot = EnsureChild(playerRoot, "ReactiveParticles");
        var footstepHook = EnsureChild(reactiveRoot, "FootstepDustHook");
        var breathHook = EnsureChild(reactiveRoot, "BreathParticlesHook");
        footstepHook.localPosition = new Vector3(0f, -0.9f, 0f);
        breathHook.localPosition = new Vector3(0f, 0.56f, 0.32f);

        var footstepDust = EnsureParticleSystem(footstepHook, "FootstepDust");
        var breathParticles = EnsureParticleSystem(breathHook, "BreathParticles");
        var caveFog = EnsureParticleSystem(playerRoot, "CaveFog");
        caveFog.transform.localPosition = new Vector3(0f, -0.65f, 0f);

        ConfigureFootstepDust(footstepDust);
        ConfigureBreathParticles(breathParticles);
        ConfigureCaveFog(caveFog);

        controller.ConfigureHooks(reactiveRoot, footstepHook, breathHook);
        controller.ConfigureAdvancedEffects(caveFog, footstepDust, breathParticles);
        controller.ConfigureRuntimeDependencies(player, gameManager);

        visualManager.ConfigureAtmosphereParticles(controller);
        visualManager.ApplyQuality(visualManager.DefaultQualityTier);

        EditorUtility.SetDirty(controller);
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

        var controller = visualManager != null ? visualManager.AtmosphereParticles : null;
        if (controller == null)
        {
            return "Advanced particle controller missing.";
        }

        return
            $"FootstepDust={(controller.FootstepDust != null ? controller.FootstepDust.name : "null")}; " +
            $"BreathParticles={(controller.BreathParticles != null ? controller.BreathParticles.name : "null")}; " +
            $"CaveFog={(controller.CaveFog != null ? controller.CaveFog.name : "null")}; " +
            $"Quality={QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]}";
    }

    private static ParticleSystem EnsureParticleSystem(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent<ParticleSystem>(out var particleSystem))
        {
            return particleSystem;
        }

        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        return root.AddComponent<ParticleSystem>();
    }

    private static Transform EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            return existing;
        }

        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void ConfigureFootstepDust(ParticleSystem particleSystem)
    {
        ConfigureRenderer(
            particleSystem,
            EnsureParticleMaterial(FootstepMaterialPath, new Color(0.55f, 0.49f, 0.39f, 0.34f), "_Surface"));

        var main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.duration = 0.7f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.24f, 0.72f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
        main.gravityModifier = 0f;
        main.maxParticles = 24;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.64f, 0.58f, 0.47f, 0.10f),
            new Color(0.46f, 0.42f, 0.35f, 0.18f));

        var emission = particleSystem.emission;
        emission.enabled = false;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.16f;
        shape.position = new Vector3(0f, 0.02f, 0f);

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.72f, 0.65f, 0.53f), 0f),
                new GradientColorKey(new Color(0.55f, 0.50f, 0.43f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.14f, 0.18f),
                new GradientAlphaKey(0.06f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
    }

    private static void ConfigureBreathParticles(ParticleSystem particleSystem)
    {
        ConfigureRenderer(
            particleSystem,
            EnsureParticleMaterial(BreathMaterialPath, new Color(0.83f, 0.90f, 1f, 0.10f), "_Breath"));

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.gravityModifier = 0f;
        main.maxParticles = 24;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.82f, 0.90f, 1f, 0.035f),
            new Color(0.72f, 0.82f, 0.95f, 0.08f));

        var emission = particleSystem.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.02f;
        shape.position = Vector3.zero;
        shape.rotation = new Vector3(-8f, 0f, 0f);

        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.015f, 0.015f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.83f, 0.90f, 1f), 0f),
                new GradientColorKey(new Color(0.74f, 0.82f, 0.95f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.09f, 0.14f),
                new GradientAlphaKey(0.05f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void ConfigureCaveFog(ParticleSystem particleSystem)
    {
        ConfigureRenderer(
            particleSystem,
            EnsureParticleMaterial(CaveFogMaterialPath, new Color(0.44f, 0.50f, 0.58f, 0.05f), "_CaveFog"));

        var main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4.2f, 6.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.008f, 0.03f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.0f, 1.9f);
        main.gravityModifier = 0f;
        main.maxParticles = 48;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.61f, 0.68f, 0.006f),
            new Color(0.43f, 0.49f, 0.56f, 0.028f));

        var emission = particleSystem.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 1.7f, 5f);
        shape.position = new Vector3(0f, -0.55f, 0f);

        var noise = particleSystem.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = 0.05f;
        noise.strengthY = 0.015f;
        noise.strengthZ = 0.05f;
        noise.frequency = 0.045f;
        noise.damping = true;
    }

    private static void ConfigureRenderer(ParticleSystem particleSystem, Material material)
    {
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.maxParticleSize = 0.08f;
        renderer.minParticleSize = 0.0001f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material EnsureParticleMaterial(string assetPath, Color baseColor, string suffix)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material != null)
        {
            return material;
        }

        if (!AssetDatabase.IsValidFolder(VfxFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Materials", "VFX");
        }

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        material = new Material(shader)
        {
            name = "Advanced" + suffix
        };
        material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }
        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }
}
