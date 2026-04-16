using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldOfVictoria.Core;
using WorldOfVictoria.Utilities;

public static class Phase2ProjectSetup
{
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager was not found in the Game scene.");
            return;
        }

        var gizmos = gameManager.GetComponent<WorldDebugGizmos>();
        if (gizmos == null)
        {
            gizmos = gameManager.gameObject.AddComponent<WorldDebugGizmos>();
        }

        var serializedGizmos = new SerializedObject(gizmos);
        serializedGizmos.FindProperty("gameManager").objectReferenceValue = gameManager;
        serializedGizmos.ApplyModifiedPropertiesWithoutUndo();

        gameManager.GenerateRuntimeWorld();

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
    }

    public static string Report()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            return "GameManager missing.";
        }

        gameManager.GenerateRuntimeWorld();

        var world = gameManager.RuntimeWorldData;
        var chunkManager = gameManager.RuntimeChunkManager;
        if (world == null || chunkManager == null)
        {
            return "Runtime world generation failed.";
        }

        var solidBlocks = 0;
        for (var y = 0; y < world.Depth; y++)
        {
            for (var z = 0; z < world.Height; z++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    if (world.IsSolidBlock(x, y, z))
                    {
                        solidBlocks++;
                    }
                }
            }
        }

        return $"Scene={scene.path}; Seed={gameManager.LastGeneratedSeed}; World={world.Width}x{world.Height}x{world.Depth}; Blocks={world.BlockCount}; SolidBlocks={solidBlocks}; Chunks={chunkManager.ChunkCountX}x{chunkManager.ChunkCountY}x{chunkManager.ChunkCountZ} ({chunkManager.ChunkCount})";
    }
}
