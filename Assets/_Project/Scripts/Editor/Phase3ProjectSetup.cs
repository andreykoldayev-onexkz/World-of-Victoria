using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;

public static class Phase3ProjectSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        EditorSceneManager.OpenScene(GameScenePath);
        UrpPhase5ShaderSetup.Execute();

        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager was not found in the Game scene.");
            return;
        }

        gameManager.GenerateRuntimeWorld();
        gameManager.BuildChunkPreviewMeshes();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        EditorSceneManager.OpenScene(GameScenePath);

        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            return "GameManager missing.";
        }

        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        if (presentationController == null || presentationController.Settings == null)
        {
            return "Chunk presentation settings missing.";
        }

        gameManager.GenerateRuntimeWorld();
        gameManager.BuildChunkPreviewMeshes();

        var worldRoot = gameManager.WorldRoot;
        var renderedChunks = worldRoot != null ? worldRoot.childCount : 0;
        var brightMaterial = presentationController.Settings.BrightMaterial;
        var shadowMaterial = presentationController.Settings.ShadowMaterial;

        return
            $"RenderedChunkObjects={renderedChunks}; " +
            $"BrightMaterial={(brightMaterial != null ? brightMaterial.name : "null")}; " +
            $"BrightShader={(brightMaterial != null && brightMaterial.shader != null ? brightMaterial.shader.name : "null")}; " +
            $"ShadowMaterial={(shadowMaterial != null ? shadowMaterial.name : "null")}; " +
            $"ShadowShader={(shadowMaterial != null && shadowMaterial.shader != null ? shadowMaterial.shader.name : "null")}";
    }
}
