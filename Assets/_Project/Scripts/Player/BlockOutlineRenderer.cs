using System.Collections.Generic;
using UnityEngine;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.Player
{
    [DisallowMultipleComponent]
    public sealed class BlockOutlineRenderer : MonoBehaviour
    {
        [SerializeField] private Color outlineColor = new(1f, 1f, 1f, 0.7f);
        [SerializeField, Min(0.001f)] private float lineThickness = 0.035f;
        [SerializeField, Min(0f)] private float outlineExpansion = 0.01f;

        private GameObject outlineRoot;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh outlineMesh;
        private Material runtimeMaterial;

        public void Show(in HitResult hitResult)
        {
            EnsureInitialized();
            outlineRoot.transform.SetPositionAndRotation(hitResult.Position + Vector3.one * 0.5f, Quaternion.identity);
            outlineRoot.transform.localScale = Vector3.one;
            outlineRoot.SetActive(true);
        }

        public void Hide()
        {
            if (outlineRoot != null)
            {
                outlineRoot.SetActive(false);
            }
        }

        private void Awake()
        {
            EnsureInitialized();
            Hide();
        }

        private void OnValidate()
        {
            if (outlineRoot == null)
            {
                return;
            }

            RebuildMesh();
            ApplyMaterialProperties();
        }

        private void OnDestroy()
        {
            if (outlineMesh != null)
            {
                Destroy(outlineMesh);
            }

            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private void EnsureInitialized()
        {
            if (outlineRoot == null)
            {
                outlineRoot = new GameObject("BlockOutline");
                meshFilter = outlineRoot.AddComponent<MeshFilter>();
                meshRenderer = outlineRoot.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                outlineRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                outlineRoot.transform.localScale = Vector3.one;
            }

            if (outlineMesh == null)
            {
                outlineMesh = new Mesh
                {
                    name = "BlockOutlineMesh"
                };
                meshFilter.sharedMesh = outlineMesh;
                RebuildMesh();
            }

            if (runtimeMaterial == null)
            {
                var shader = Shader.Find("WorldOfVictoria/OutlineUnlit");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }

                runtimeMaterial = new Material(shader)
                {
                    name = "Runtime_BlockOutline"
                };
                meshRenderer.sharedMaterial = runtimeMaterial;
                ApplyMaterialProperties();
            }
        }

        private void ApplyMaterialProperties()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (runtimeMaterial.HasProperty("_BaseColor"))
            {
                runtimeMaterial.SetColor("_BaseColor", outlineColor);
            }

            if (runtimeMaterial.HasProperty("_Color"))
            {
                runtimeMaterial.SetColor("_Color", outlineColor);
            }
        }

        private void RebuildMesh()
        {
            if (outlineMesh == null)
            {
                return;
            }

            var vertices = new List<Vector3>(96);
            var triangles = new List<int>(432);

            var half = 0.5f + outlineExpansion;
            var halfThickness = lineThickness * 0.5f;

            AddEdgeBox(vertices, triangles, new Vector3(0f, half, half), new Vector3(1f, 0f, 0f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(0f, half, -half), new Vector3(1f, 0f, 0f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(0f, -half, half), new Vector3(1f, 0f, 0f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(0f, -half, -half), new Vector3(1f, 0f, 0f), half, halfThickness);

            AddEdgeBox(vertices, triangles, new Vector3(half, 0f, half), new Vector3(0f, 1f, 0f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(half, 0f, -half), new Vector3(0f, 1f, 0f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(-half, 0f, half), new Vector3(0f, 1f, 0f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(-half, 0f, -half), new Vector3(0f, 1f, 0f), half, halfThickness);

            AddEdgeBox(vertices, triangles, new Vector3(half, half, 0f), new Vector3(0f, 0f, 1f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(half, -half, 0f), new Vector3(0f, 0f, 1f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(-half, half, 0f), new Vector3(0f, 0f, 1f), half, halfThickness);
            AddEdgeBox(vertices, triangles, new Vector3(-half, -half, 0f), new Vector3(0f, 0f, 1f), half, halfThickness);

            outlineMesh.Clear();
            outlineMesh.SetVertices(vertices);
            outlineMesh.SetTriangles(triangles, 0);
            outlineMesh.RecalculateNormals();
            outlineMesh.RecalculateBounds();
        }

        private static void AddEdgeBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 axis, float halfLength, float halfThickness)
        {
            Vector3 extents;
            if (axis.x > 0.5f)
            {
                extents = new Vector3(halfLength, halfThickness, halfThickness);
            }
            else if (axis.y > 0.5f)
            {
                extents = new Vector3(halfThickness, halfLength, halfThickness);
            }
            else
            {
                extents = new Vector3(halfThickness, halfThickness, halfLength);
            }

            AddBox(vertices, triangles, center - extents, center + extents);
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 min, Vector3 max)
        {
            var start = vertices.Count;

            vertices.Add(new Vector3(min.x, min.y, min.z));
            vertices.Add(new Vector3(max.x, min.y, min.z));
            vertices.Add(new Vector3(max.x, max.y, min.z));
            vertices.Add(new Vector3(min.x, max.y, min.z));
            vertices.Add(new Vector3(min.x, min.y, max.z));
            vertices.Add(new Vector3(max.x, min.y, max.z));
            vertices.Add(new Vector3(max.x, max.y, max.z));
            vertices.Add(new Vector3(min.x, max.y, max.z));

            AddQuad(triangles, start + 0, start + 1, start + 2, start + 3);
            AddQuad(triangles, start + 5, start + 4, start + 7, start + 6);
            AddQuad(triangles, start + 4, start + 0, start + 3, start + 7);
            AddQuad(triangles, start + 1, start + 5, start + 6, start + 2);
            AddQuad(triangles, start + 3, start + 2, start + 6, start + 7);
            AddQuad(triangles, start + 4, start + 5, start + 1, start + 0);
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }
    }
}
