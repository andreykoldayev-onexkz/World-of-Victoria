using UnityEngine;
using WorldOfVictoria.Core;
using WorldOfVictoria.Chunking;

namespace WorldOfVictoria.Utilities
{
    [ExecuteAlways]
    public sealed class WorldDebugGizmos : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private bool showWorldBounds = true;
        [SerializeField] private bool showChunkBounds = true;
        [SerializeField] private int maxChunkGizmos = 1024;
        [SerializeField] private Color worldBoundsColor = new(0.35f, 0.8f, 1f, 1f);
        [SerializeField] private Color chunkBoundsColor = new(0.2f, 1f, 0.35f, 0.7f);

        private void OnDrawGizmos()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<GameManager>();
            }

            if (gameManager == null || !gameManager.HasGeneratedWorld)
            {
                return;
            }

            DrawWorldBounds(gameManager.RuntimeWorldData);
            DrawChunkBounds(gameManager.RuntimeChunkManager);
        }

        private void DrawWorldBounds(WorldData worldData)
        {
            if (!showWorldBounds)
            {
                return;
            }

            var center = new Vector3(worldData.Width * 0.5f, worldData.Depth * 0.5f, worldData.Height * 0.5f);
            var size = new Vector3(worldData.Width, worldData.Depth, worldData.Height);

            Gizmos.color = worldBoundsColor;
            Gizmos.DrawWireCube(center, size);
        }

        private void DrawChunkBounds(ChunkManager chunkManager)
        {
            if (!showChunkBounds)
            {
                return;
            }

            Gizmos.color = chunkBoundsColor;
            var chunks = chunkManager.GetAllChunks();
            var count = Mathf.Min(maxChunkGizmos, chunks.Count);

            for (var i = 0; i < count; i++)
            {
                var chunk = chunks[i];
                Gizmos.DrawWireCube(chunk.WorldBounds.center, chunk.WorldBounds.size);
            }
        }
    }
}
