using UnityEngine;
using WorldOfVictoria.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldOfVictoria.Chunking
{
    public sealed class ChunkPresentationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ChunkPresentationSettings settings;

        private readonly ChunkMeshBuilder chunkMeshBuilder = new();

        public ChunkPresentationSettings Settings => settings;
        public ChunkRenderer ChunkPrefab => settings != null ? settings.ChunkPrefab : null;
        public Material BrightMaterial => settings != null ? settings.BrightMaterial : null;
        public Material ShadowMaterial => settings != null ? settings.ShadowMaterial : null;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<GameManager>();
            }

            settings?.EnsureRuntimeMaterialsBound();
        }

        public bool HasRuntimePresentationResources()
        {
            return gameManager != null
                && gameManager.WorldRoot != null
                && ChunkPrefab != null
                && BrightMaterial != null
                && ShadowMaterial != null;
        }

        public bool ShouldBuildPreviewOnGenerate()
        {
            return settings != null && settings.BuildPreviewOnGenerate;
        }

        public void BuildChunkPreviewMeshes()
        {
            if (gameManager == null || !gameManager.HasGeneratedWorld || !HasRuntimePresentationResources())
            {
                return;
            }

            ClearWorldRoot();

            var runtimeChunkManager = gameManager.RuntimeChunkManager;
            var centerX = runtimeChunkManager.ChunkCountX / 2;
            var centerZ = runtimeChunkManager.ChunkCountZ / 2;
            var minY = settings.PreviewAllVerticalLayers ? 0 : runtimeChunkManager.ChunkCountY / 2;
            var maxY = settings.PreviewAllVerticalLayers ? runtimeChunkManager.ChunkCountY - 1 : minY;

            for (var chunkX = Mathf.Max(0, centerX - settings.PreviewChunkRadiusXZ); chunkX <= Mathf.Min(runtimeChunkManager.ChunkCountX - 1, centerX + settings.PreviewChunkRadiusXZ); chunkX++)
            {
                for (var chunkY = minY; chunkY <= maxY; chunkY++)
                {
                    for (var chunkZ = Mathf.Max(0, centerZ - settings.PreviewChunkRadiusXZ); chunkZ <= Mathf.Min(runtimeChunkManager.ChunkCountZ - 1, centerZ + settings.PreviewChunkRadiusXZ); chunkZ++)
                    {
                        var chunkData = runtimeChunkManager.GetChunk(chunkX, chunkY, chunkZ);
                        if (chunkData == null)
                        {
                            continue;
                        }

                        var chunkMesh = chunkMeshBuilder.Build(chunkData, gameManager.RuntimeWorldData);
                        if (!chunkMesh.HasGeometry)
                        {
                            continue;
                        }

                        var chunkView = Object.Instantiate(ChunkPrefab, gameManager.WorldRoot);
                        chunkView.Initialize(chunkData, BrightMaterial, ShadowMaterial);
                        chunkView.ApplyMesh(chunkMesh);
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            GameObjectUtility.SetStaticEditorFlags(
                                chunkView.gameObject,
                                StaticEditorFlags.ContributeGI |
                                StaticEditorFlags.OccluderStatic |
                                StaticEditorFlags.ReflectionProbeStatic);
                        }
#endif
                    }
                }
            }
        }

        private void ClearWorldRoot()
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
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
