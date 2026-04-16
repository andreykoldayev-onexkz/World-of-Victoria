using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Core;

public static class UrpPhase9ParticleSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        var playerRoot = GameObject.Find("Player")?.transform;
        var atmosphereRoot = GameObject.Find("Atmosphere");
        if (visualManager == null || playerRoot == null || atmosphereRoot == null)
        {
            Debug.LogError("VisualManager, Player, or Atmosphere root missing in Game scene.");
            return;
        }

        var dustMotes = playerRoot.Find("DustMotes")?.GetComponent<ParticleSystem>();
        if (dustMotes == null)
        {
            Debug.LogError("DustMotes particle system missing under Player.");
            return;
        }

        var controller = visualManager.GetComponent<AtmosphereParticleController>();
        if (controller == null)
        {
            controller = visualManager.gameObject.AddComponent<AtmosphereParticleController>();
        }

        var reactiveRoot = EnsureChild(playerRoot, "ReactiveParticles");
        var footstepHook = EnsureChild(reactiveRoot, "FootstepDustHook");
        var breathHook = EnsureChild(reactiveRoot, "BreathParticlesHook");

        footstepHook.localPosition = new Vector3(0f, -0.9f, 0f);
        breathHook.localPosition = new Vector3(0f, 0.55f, 0.35f);

        controller.ConfigureDustMotes(dustMotes);
        controller.ConfigureHooks(reactiveRoot, footstepHook, breathHook);

        visualManager.ConfigureDustMotes(dustMotes);
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
        if (visualManager == null || controller == null || controller.DustMotes == null)
        {
            return "Atmosphere particle controller missing.";
        }

        var main = controller.DustMotes.main;
        var emission = controller.DustMotes.emission;
        return
            $"DustMotes={controller.DustMotes.name}; " +
            $"Quality={QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]}; " +
            $"MaxParticles={main.maxParticles}; " +
            $"EmissionRate={emission.rateOverTime.constant:0.##}; " +
            $"FootstepHook={(controller.FootstepDustHook != null ? controller.FootstepDustHook.name : "null")}; " +
            $"BreathHook={(controller.BreathParticlesHook != null ? controller.BreathParticlesHook.name : "null")}";
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
}
