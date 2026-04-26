using UnityEngine;

namespace WorldOfVictoria.Rendering.Character
{
    public readonly struct ModelCubeUvLayout
    {
        public ModelCubeUvLayout(RectInt up, RectInt down, RectInt left, RectInt front, RectInt right, RectInt back)
        {
            Up = up;
            Down = down;
            Left = left;
            Front = front;
            Right = right;
            Back = back;
        }

        public RectInt Up { get; }
        public RectInt Down { get; }
        public RectInt Left { get; }
        public RectInt Front { get; }
        public RectInt Right { get; }
        public RectInt Back { get; }
    }

    public sealed class ModelCube
    {
        private const float TextureWidth = 64f;
        private const float TextureHeight = 32f;

        private readonly GameObject root;
        private readonly MeshRenderer meshRenderer;

        public ModelCube(string name, Transform parent, Vector3 localPosition, Vector3 sizeInPixels, Material material, ModelCubeUvLayout uvLayout)
        {
            root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var meshFilter = root.AddComponent<MeshFilter>();
            meshRenderer = root.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = CreateMesh(sizeInPixels, uvLayout);
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
            meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes;
        }

        public Transform Transform => root.transform;

        public void SetMaterial(Material material)
        {
            meshRenderer.sharedMaterial = material;
        }

        private static Mesh CreateMesh(Vector3 sizeInPixels, ModelCubeUvLayout uvLayout)
        {
            var mesh = new Mesh
            {
                name = "ModelCube"
            };

            var halfSize = sizeInPixels / 32f;

            var p0 = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            var p1 = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            var p2 = new Vector3(halfSize.x, halfSize.y, halfSize.z);
            var p3 = new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            var p4 = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            var p5 = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            var p6 = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            var p7 = new Vector3(halfSize.x, halfSize.y, -halfSize.z);

            var vertices = new[]
            {
                p0, p1, p2, p3, // front
                p4, p5, p6, p7, // back
                p5, p0, p3, p6, // left
                p1, p4, p7, p2, // right
                p3, p2, p7, p6, // up
                p5, p4, p1, p0  // down
            };

            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21, 20, 23, 22
            };

            var normals = new[]
            {
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down
            };

            var uvs = new Vector2[24];
            WriteFaceUvs(uvs, 0, uvLayout.Front);
            WriteFaceUvs(uvs, 4, uvLayout.Back);
            WriteFaceUvs(uvs, 8, uvLayout.Left);
            WriteFaceUvs(uvs, 12, uvLayout.Right);
            WriteFaceUvs(uvs, 16, uvLayout.Up);
            WriteFaceUvs(uvs, 20, uvLayout.Down);

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void WriteFaceUvs(Vector2[] target, int startIndex, RectInt pixelRect)
        {
            var uMin = pixelRect.x / TextureWidth;
            var uMax = (pixelRect.x + pixelRect.width) / TextureWidth;
            var vMin = 1f - ((pixelRect.y + pixelRect.height) / TextureHeight);
            var vMax = 1f - (pixelRect.y / TextureHeight);

            target[startIndex + 0] = new Vector2(uMin, vMin);
            target[startIndex + 1] = new Vector2(uMax, vMin);
            target[startIndex + 2] = new Vector2(uMax, vMax);
            target[startIndex + 3] = new Vector2(uMin, vMax);
        }
    }
}
