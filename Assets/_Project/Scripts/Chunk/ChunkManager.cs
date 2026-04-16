using System.Collections.Generic;
using UnityEngine;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.Chunking
{
    public sealed class ChunkManager
    {
        private readonly ChunkData[] chunks;

        public ChunkManager(WorldData worldData, int chunkSize)
        {
            WorldData = worldData;
            ChunkSize = chunkSize;

            ChunkCountX = Mathf.CeilToInt(worldData.Width / (float)chunkSize);
            ChunkCountY = Mathf.CeilToInt(worldData.Depth / (float)chunkSize);
            ChunkCountZ = Mathf.CeilToInt(worldData.Height / (float)chunkSize);

            chunks = new ChunkData[ChunkCountX * ChunkCountY * ChunkCountZ];

            for (var x = 0; x < ChunkCountX; x++)
            {
                for (var y = 0; y < ChunkCountY; y++)
                {
                    for (var z = 0; z < ChunkCountZ; z++)
                    {
                        var min = new Vector3Int(x * chunkSize, y * chunkSize, z * chunkSize);
                        var max = new Vector3Int(
                            Mathf.Min(worldData.Width, min.x + chunkSize),
                            Mathf.Min(worldData.Depth, min.y + chunkSize),
                            Mathf.Min(worldData.Height, min.z + chunkSize));

                        var bounds = new BoundsInt(min, max - min);
                        var index = (x + y * ChunkCountX) * ChunkCountZ + z;
                        chunks[index] = new ChunkData(index, new Vector3Int(x, y, z), bounds);
                    }
                }
            }
        }

        public WorldData WorldData { get; }
        public int ChunkSize { get; }
        public int ChunkCountX { get; }
        public int ChunkCountY { get; }
        public int ChunkCountZ { get; }
        public int ChunkCount => chunks.Length;

        public IReadOnlyList<ChunkData> GetAllChunks()
        {
            return chunks;
        }

        public ChunkData GetChunk(int chunkX, int chunkY, int chunkZ)
        {
            if (chunkX < 0 || chunkX >= ChunkCountX
                || chunkY < 0 || chunkY >= ChunkCountY
                || chunkZ < 0 || chunkZ >= ChunkCountZ)
            {
                return null;
            }

            var index = (chunkX + chunkY * ChunkCountX) * ChunkCountZ + chunkZ;
            return chunks[index];
        }

        public ChunkData GetChunkContainingBlock(int x, int y, int z)
        {
            return GetChunk(x / ChunkSize, y / ChunkSize, z / ChunkSize);
        }
    }
}
