using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;

public static class UrpPhase10ScalabilitySetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (visualManager == null || gameManager == null)
        {
            Debug.LogError("VisualManager or GameManager missing in Game scene.");
            return;
        }

        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        var atmosphereController = visualManager.AtmosphereParticles;
        if (presentationController == null || presentationController.Settings == null)
        {
            Debug.LogError("Chunk presentation settings missing.");
            return;
        }

        var scalabilityController = visualManager.GetComponent<VisualScalabilityController>();
        if (scalabilityController == null)
        {
            scalabilityController = visualManager.gameObject.AddComponent<VisualScalabilityController>();
        }

        scalabilityController.Configure(
            visualManager.GlobalVolume,
            presentationController.Settings.BrightMaterial,
            presentationController.Settings.ShadowMaterial,
            atmosphereController,
            visualManager.VolumetricLighting,
            gameManager.WorldConfig);

        visualManager.ConfigureScalabilityController(scalabilityController);
        visualManager.ApplyQuality(visualManager.DefaultQualityTier);

        EditorUtility.SetDirty(scalabilityController);
        EditorUtility.SetDirty(visualManager);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        var visualManager = Object.FindAnyObjectByType<VisualManager>();
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (visualManager == null || gameManager == null)
        {
            EditorSceneManager.OpenScene(GameScenePath);
            visualManager = Object.FindAnyObjectByType<VisualManager>();
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }

        if (visualManager == null || gameManager == null || visualManager.ScalabilityController == null)
        {
            return "Scalability controller missing.";
        }

        var qualityName = QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)];
        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        var brightMaterial = presentationController != null && presentationController.Settings != null ? presentationController.Settings.BrightMaterial : null;
        var shadowMaterial = presentationController != null && presentationController.Settings != null ? presentationController.Settings.ShadowMaterial : null;
        var atmosphere = visualManager.AtmosphereParticles;
        var emissionRate = atmosphere != null && atmosphere.DustMotes != null ? atmosphere.DustMotes.emission.rateOverTime.constant : 0f;

        return
            $"Quality={qualityName}; " +
            $"FogDensity={RenderSettings.fogDensity:0.000}; " +
            $"VolumeWeight={(visualManager.GlobalVolume != null ? visualManager.GlobalVolume.weight.ToString("0.00") : "0")}; " +
            $"BrightVertex={(brightMaterial != null ? brightMaterial.GetFloat("_UseVertexBrightness").ToString("0.00") : "n/a")}; " +
            $"ShadowVertex={(shadowMaterial != null ? shadowMaterial.GetFloat("_UseVertexBrightness").ToString("0.00") : "n/a")}; " +
            $"DustRate={emissionRate:0.##}";
    }
}
