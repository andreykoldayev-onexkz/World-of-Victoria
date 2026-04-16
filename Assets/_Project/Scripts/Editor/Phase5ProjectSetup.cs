using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;

public static class Phase5ProjectSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager missing in Game scene.");
            return;
        }

        var runtimeController = gameManager.GetComponent<ChunkRuntimeController>();
        if (runtimeController == null)
        {
            runtimeController = gameManager.gameObject.AddComponent<ChunkRuntimeController>();
        }

        var presentationController = gameManager.GetComponent<ChunkPresentationController>();
        if (presentationController == null)
        {
            Debug.LogError("ChunkPresentationController missing in Game scene.");
            return;
        }

        var serializedPresentationController = new SerializedObject(presentationController);
        var settings = serializedPresentationController.FindProperty("settings").objectReferenceValue as ChunkPresentationSettings;
        if (settings == null)
        {
            Debug.LogError("ChunkPresentationSettings are not assigned.");
            return;
        }

        var serializedSettings = new SerializedObject(settings);
        serializedSettings.FindProperty("buildPreviewOnGenerate").boolValue = false;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();

        var serializedController = new SerializedObject(runtimeController);
        serializedController.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedController.FindProperty("presentationController").objectReferenceValue = presentationController;
        serializedController.FindProperty("maxRebuildsPerFrame").intValue = 2;
        serializedController.FindProperty("preloadPoolSize").intValue = 64;
        serializedController.FindProperty("activeChunkRadiusXZ").intValue = 3;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        gameManager.GenerateRuntimeWorld();
        runtimeController.ForceReinitialize();

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        if (!Application.isPlaying)
        {
            EditorSceneManager.OpenScene(GameScenePath);
        }

        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var runtimeController = Object.FindAnyObjectByType<ChunkRuntimeController>();
        if (gameManager == null || runtimeController == null)
        {
            return "GameManager or ChunkRuntimeController missing.";
        }

        return $"Scene={GameScenePath}; ActiveViews={runtimeController.ActiveChunkViews}; Pool={runtimeController.PooledChunkViews}; PendingRebuilds={runtimeController.PendingRebuilds}; WorldRootChildren={gameManager.WorldRoot.childCount}";
    }
}
