using System;
using UnityEngine;
using WorldOfVictoria.Core;

public static class Phase7SaveProbe
{
    public static string Report()
    {
        var gameManager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null || !gameManager.HasGeneratedWorld)
        {
            return "GameManager missing or world not initialized.";
        }

        var world = gameManager.RuntimeWorldData;
        return $"SavePath={gameManager.SaveFilePath}; Player={gameManager.PlayerRoot.position}; Blocks={world.BlockCount}; Seed={gameManager.LastGeneratedSeed}; LastSaveExists={SaveSystem.HasLastSavePath()}";
    }

    public static string SaveAndReload()
    {
        var gameManager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null || !gameManager.HasGeneratedWorld)
        {
            return "GameManager missing or world not initialized.";
        }

        var beforePlayer = gameManager.PlayerRoot.position;
        var beforeBytes = gameManager.RuntimeWorldData.RawBlocks.ToArray();

        gameManager.SaveCurrentGame();
        var saveData = SaveSystem.Load(gameManager.SaveFilePath);

        var samePlayer = Vector3.Distance(beforePlayer, saveData.PlayerPosition) < 0.0001f;
        var sameBytes = beforeBytes.Length == saveData.Blocks.Length;
        if (sameBytes)
        {
            for (var i = 0; i < beforeBytes.Length; i++)
            {
                if (beforeBytes[i] != saveData.Blocks[i])
                {
                    sameBytes = false;
                    break;
                }
            }
        }

        return $"SamePlayer={samePlayer}; SameBlocks={sameBytes}; SavePath={gameManager.SaveFilePath}; Player={beforePlayer}; Bytes={beforeBytes.Length}";
    }
}
