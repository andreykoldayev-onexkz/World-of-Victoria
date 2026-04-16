using UnityEngine;
using WorldOfVictoria.Rendering;

namespace WorldOfVictoria.Chunking
{
    [CreateAssetMenu(fileName = "ChunkPresentationSettings", menuName = "World of Victoria/Config/Chunk Presentation Settings")]
    public sealed class ChunkPresentationSettings : ScriptableObject
    {
        [SerializeField] private ChunkRenderer chunkPrefab;
        [SerializeField] private Material brightMaterial;
        [SerializeField] private Material shadowMaterial;
        [SerializeField] private VoxelPbrTextureLibrary textureLibrary;
        [SerializeField] private bool buildPreviewOnGenerate = true;
        [SerializeField, Min(0)] private int previewChunkRadiusXZ = 1;
        [SerializeField] private bool previewAllVerticalLayers = true;

        public ChunkRenderer ChunkPrefab => chunkPrefab;
        public Material BrightMaterial => brightMaterial;
        public Material ShadowMaterial => shadowMaterial;
        public VoxelPbrTextureLibrary TextureLibrary => textureLibrary;
        public bool BuildPreviewOnGenerate => buildPreviewOnGenerate;
        public int PreviewChunkRadiusXZ => previewChunkRadiusXZ;
        public bool PreviewAllVerticalLayers => previewAllVerticalLayers;

        public void EnsureRuntimeMaterialsBound()
        {
            if (textureLibrary == null)
            {
                return;
            }

            BindTextureArrays(brightMaterial);
            BindTextureArrays(shadowMaterial);
        }

        private void BindTextureArrays(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (textureLibrary.AlbedoArray != null)
            {
                material.SetTexture("_AlbedoArray", textureLibrary.AlbedoArray);
            }

            if (textureLibrary.NormalArray != null)
            {
                material.SetTexture("_NormalArray", textureLibrary.NormalArray);
            }

            if (textureLibrary.RoughnessArray != null)
            {
                material.SetTexture("_RoughnessArray", textureLibrary.RoughnessArray);
            }
        }
    }
}
