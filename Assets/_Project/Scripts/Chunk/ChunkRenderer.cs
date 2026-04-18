using UnityEngine;
using UnityEngine.Rendering;

namespace WorldOfVictoria.Chunking
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ChunkRenderer : MonoBehaviour
    {
        [SerializeField] private Vector3Int chunkCoord;
        [SerializeField] private Bounds localBounds;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;

        public Vector3Int ChunkCoord => chunkCoord;

        public void Initialize(ChunkData chunkData, Material brightMaterial, Material shadowMaterial)
        {
            EnsureComponents();

            chunkCoord = chunkData.Coord;
            localBounds = chunkData.WorldBounds;
            name = $"Chunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}";

            transform.localPosition = Vector3.zero;
            meshRenderer.sharedMaterial = brightMaterial != null ? brightMaterial : shadowMaterial;
            meshRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            meshRenderer.receiveShadows = true;
            gameObject.SetActive(true);
        }

        public void ApplyMesh(ChunkMeshData meshData)
        {
            EnsureComponents();

            mesh.Clear();
            mesh.SetVertices(meshData.Vertices);
            mesh.SetUVs(0, meshData.UVs);
            mesh.SetUVs(1, meshData.Metadata);
            mesh.SetUVs(2, meshData.LightCorners);
            mesh.SetUVs(3, meshData.AoCorners);
            mesh.SetUVs(4, meshData.FaceCenters);
            mesh.SetColors(meshData.Colors);
            mesh.SetNormals(meshData.Normals);
            mesh.SetTangents(meshData.Tangents);
            mesh.subMeshCount = 1;
            mesh.SetTriangles(meshData.AllTriangles, 0, true);
            mesh.RecalculateBounds();
        }

        public void ClearMesh()
        {
            EnsureComponents();
            mesh.Clear();
        }

        private void EnsureComponents()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            if (mesh == null)
            {
                mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    mesh = new Mesh
                    {
                        name = "ChunkMesh",
                        indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                    };
                    mesh.MarkDynamic();
                    meshFilter.sharedMesh = mesh;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = visible;
            }
        }

        public void Release()
        {
            gameObject.SetActive(false);
        }
    }
}
