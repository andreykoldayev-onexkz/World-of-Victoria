using System.Reflection;
using UnityEngine;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;
using WorldOfVictoria.Player;

public static class Phase5RespawnProbe
{
    private static readonly MethodInfo ResetPositionMethod = typeof(PlayerController).GetMethod(
        "ResetPosition",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static string TriggerRespawn()
    {
        if (!Application.isPlaying)
        {
            return "Play mode is required.";
        }

        var player = Object.FindAnyObjectByType<PlayerController>();
        if (player == null || ResetPositionMethod == null)
        {
            return "PlayerController or ResetPosition method missing.";
        }

        ResetPositionMethod.Invoke(player, null);
        return Report();
    }

    public static string Report()
    {
        var gameManager = Object.FindAnyObjectByType<GameManager>();
        var runtimeController = Object.FindAnyObjectByType<ChunkRuntimeController>();
        var player = Object.FindAnyObjectByType<PlayerController>();

        if (gameManager == null || runtimeController == null || player == null)
        {
            return "Missing runtime references.";
        }

        return $"Player={player.transform.position}; ActiveViews={runtimeController.ActiveChunkViews}; Pool={runtimeController.PooledChunkViews}; PendingRebuilds={runtimeController.PendingRebuilds}; WorldRootChildren={gameManager.WorldRoot.childCount}";
    }
}
