using System.Collections.Generic;
using UnityEngine;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.Chunking
{
    public sealed class ChunkRuntimeController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ChunkPresentationController presentationController;
        [SerializeField, Min(1)] private int maxRebuildsPerFrame = 2;
        [SerializeField, Min(1)] private int preloadPoolSize = 64;
        [SerializeField, Min(0)] private int activeChunkRadiusXZ = 2;

        private readonly Queue<ChunkData> rebuildQueue = new();
        private readonly Dictionary<int, ChunkRenderer> activeRenderers = new();
        private readonly Stack<ChunkRenderer> pooledRenderers = new();
        private readonly HashSet<int> queuedChunkIndices = new();
        private readonly HashSet<int> desiredActiveIndices = new();
        private readonly List<int> releaseBuffer = new();
        private readonly ChunkMeshBuilder chunkMeshBuilder = new();

        private ChunkManager observedChunkManager;
        public int PendingRebuilds => rebuildQueue.Count;
        public int ActiveChunkViews => activeRenderers.Count;
        public int PooledChunkViews => pooledRenderers.Count;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<GameManager>();
            }

            if (presentationController == null)
            {
                presentationController = GetComponent<ChunkPresentationController>();
            }
        }

        private void Update()
        {
            if (gameManager == null || presentationController == null || !gameManager.HasGeneratedWorld || !presentationController.HasRuntimePresentationResources())
            {
                return;
            }

            EnsureInitializedForCurrentWorld();
            RefreshVisibleChunkSet();
            ProcessRebuildQueue();
        }

        public void ForceReinitialize()
        {
            observedChunkManager = null;
            rebuildQueue.Clear();
            queuedChunkIndices.Clear();
            ReclaimAllActiveRenderers();
            ClearWorldRootChildren();
            pooledRenderers.Clear();
        }

        private void EnsureInitializedForCurrentWorld()
        {
            if (ReferenceEquals(observedChunkManager, gameManager.RuntimeChunkManager))
            {
                return;
            }

            ForceReinitialize();
            observedChunkManager = gameManager.RuntimeChunkManager;
            WarmPool();

            foreach (var chunk in observedChunkManager.GetAllChunks())
            {
                chunk.MarkDirty();
            }

            BuildActiveRegionImmediately();
        }

        private void WarmPool()
        {
            while (pooledRenderers.Count < preloadPoolSize)
            {
                var renderer = Instantiate(presentationController.ChunkPrefab, gameManager.WorldRoot);
                renderer.Release();
                pooledRenderers.Push(renderer);
            }
        }

        private void RefreshVisibleChunkSet()
        {
            var playerChunk = GetPlayerChunk();

            if (playerChunk == null)
            {
                return;
            }

            desiredActiveIndices.Clear();
            for (var chunkX = Mathf.Max(0, playerChunk.Coord.x - activeChunkRadiusXZ); chunkX <= Mathf.Min(observedChunkManager.ChunkCountX - 1, playerChunk.Coord.x + activeChunkRadiusXZ); chunkX++)
            {
                for (var chunkY = 0; chunkY < observedChunkManager.ChunkCountY; chunkY++)
                {
                    for (var chunkZ = Mathf.Max(0, playerChunk.Coord.z - activeChunkRadiusXZ); chunkZ <= Mathf.Min(observedChunkManager.ChunkCountZ - 1, playerChunk.Coord.z + activeChunkRadiusXZ); chunkZ++)
                    {
                        var chunk = observedChunkManager.GetChunk(chunkX, chunkY, chunkZ);
                        if (chunk == null)
                        {
                            continue;
                        }

                        desiredActiveIndices.Add(chunk.Index);

                        if (!activeRenderers.ContainsKey(chunk.Index))
                        {
                            var renderer = AcquireRenderer();
                            renderer.Initialize(chunk, presentationController.BrightMaterial, presentationController.ShadowMaterial);
                            renderer.ClearMesh();
                            renderer.SetVisible(false);
                            activeRenderers[chunk.Index] = renderer;
                            chunk.MarkDirty();
                        }

                        activeRenderers[chunk.Index].SetVisible(ShouldChunkBeVisible(chunk));
                        EnqueueDirtyChunk(chunk);
                    }
                }
            }

            releaseBuffer.Clear();
            foreach (var pair in activeRenderers)
            {
                if (!desiredActiveIndices.Contains(pair.Key))
                {
                    pair.Value.Release();
                    pooledRenderers.Push(pair.Value);
                    releaseBuffer.Add(pair.Key);
                }
            }

            for (var i = 0; i < releaseBuffer.Count; i++)
            {
                activeRenderers.Remove(releaseBuffer[i]);
            }
        }

        private void ProcessRebuildQueue()
        {
            var buildsRemaining = maxRebuildsPerFrame;
            while (buildsRemaining > 0 && rebuildQueue.Count > 0)
            {
                var chunk = rebuildQueue.Dequeue();
                queuedChunkIndices.Remove(chunk.Index);

                if (!chunk.IsDirty)
                {
                    continue;
                }

                if (!activeRenderers.TryGetValue(chunk.Index, out var renderer))
                {
                    continue;
                }

                var meshData = chunkMeshBuilder.Build(chunk, gameManager.RuntimeWorldData);
                renderer.Initialize(chunk, presentationController.BrightMaterial, presentationController.ShadowMaterial);
                renderer.ApplyMesh(meshData);
                renderer.SetVisible(ShouldChunkBeVisible(chunk));
                chunk.ClearDirty();
                buildsRemaining--;
            }
        }

        public void HandlePlayerTeleported()
        {
            if (gameManager == null || !gameManager.HasGeneratedWorld)
            {
                return;
            }

            rebuildQueue.Clear();
            queuedChunkIndices.Clear();
            BuildActiveRegionImmediately();
            RefreshVisibleChunkSet();
        }

        private void EnqueueDirtyChunk(ChunkData chunk)
        {
            if (!chunk.IsDirty || queuedChunkIndices.Contains(chunk.Index))
            {
                return;
            }

            rebuildQueue.Enqueue(chunk);
            queuedChunkIndices.Add(chunk.Index);
        }

        private ChunkRenderer AcquireRenderer()
        {
            if (pooledRenderers.Count > 0)
            {
                var pooled = pooledRenderers.Pop();
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            return Instantiate(presentationController.ChunkPrefab, gameManager.WorldRoot);
        }

        private void ReclaimAllActiveRenderers()
        {
            foreach (var pair in activeRenderers)
            {
                pair.Value.Release();
                pooledRenderers.Push(pair.Value);
            }

            activeRenderers.Clear();
        }

        private void BuildActiveRegionImmediately()
        {
            var playerChunk = GetPlayerChunk();
            if (playerChunk == null)
            {
                return;
            }

            desiredActiveIndices.Clear();
            for (var chunkX = Mathf.Max(0, playerChunk.Coord.x - activeChunkRadiusXZ); chunkX <= Mathf.Min(observedChunkManager.ChunkCountX - 1, playerChunk.Coord.x + activeChunkRadiusXZ); chunkX++)
            {
                for (var chunkY = 0; chunkY < observedChunkManager.ChunkCountY; chunkY++)
                {
                    for (var chunkZ = Mathf.Max(0, playerChunk.Coord.z - activeChunkRadiusXZ); chunkZ <= Mathf.Min(observedChunkManager.ChunkCountZ - 1, playerChunk.Coord.z + activeChunkRadiusXZ); chunkZ++)
                    {
                        var chunk = observedChunkManager.GetChunk(chunkX, chunkY, chunkZ);
                        if (chunk == null)
                        {
                            continue;
                        }

                        desiredActiveIndices.Add(chunk.Index);

                        if (!activeRenderers.TryGetValue(chunk.Index, out var renderer))
                        {
                            renderer = AcquireRenderer();
                            activeRenderers[chunk.Index] = renderer;
                        }

                        var meshData = chunkMeshBuilder.Build(chunk, gameManager.RuntimeWorldData);
                        renderer.Initialize(chunk, presentationController.BrightMaterial, presentationController.ShadowMaterial);
                        renderer.ApplyMesh(meshData);
                        renderer.SetVisible(ShouldChunkBeVisible(chunk));
                        chunk.ClearDirty();
                    }
                }
            }

            releaseBuffer.Clear();
            foreach (var pair in activeRenderers)
            {
                if (!desiredActiveIndices.Contains(pair.Key))
                {
                    pair.Value.Release();
                    pooledRenderers.Push(pair.Value);
                    releaseBuffer.Add(pair.Key);
                }
            }

            for (var i = 0; i < releaseBuffer.Count; i++)
            {
                activeRenderers.Remove(releaseBuffer[i]);
            }
        }

        private ChunkData GetPlayerChunk()
        {
            if (gameManager?.PlayerRoot == null || observedChunkManager == null)
            {
                return null;
            }

            return observedChunkManager.GetChunkContainingBlock(
                Mathf.Clamp(Mathf.FloorToInt(gameManager.PlayerRoot.position.x), 0, gameManager.WorldConfig.Width - 1),
                Mathf.Clamp(Mathf.FloorToInt(gameManager.PlayerRoot.position.y), 0, gameManager.WorldConfig.Depth - 1),
                Mathf.Clamp(Mathf.FloorToInt(gameManager.PlayerRoot.position.z), 0, gameManager.WorldConfig.Height - 1));
        }

        private bool ShouldChunkBeVisible(ChunkData chunk)
        {
            // Unity already performs renderer frustum culling, and the extra manual pass
            // was racing against teleports/respawns and leaving valid chunks hidden.
            return true;
        }

        private void ClearWorldRootChildren()
        {
            if (gameManager?.WorldRoot == null)
            {
                return;
            }

            for (var i = gameManager.WorldRoot.childCount - 1; i >= 0; i--)
            {
                var child = gameManager.WorldRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
