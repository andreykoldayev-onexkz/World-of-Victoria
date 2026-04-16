using System.Collections.Generic;
using UnityEngine;

namespace WorldOfVictoria.Chunking
{
    public sealed class ChunkMeshData
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<int> BrightTriangles = new();
        public readonly List<int> ShadowTriangles = new();
        public readonly List<int> AllTriangles = new();
        public readonly List<Vector2> UVs = new();
        public readonly List<Vector4> Metadata = new();
        public readonly List<Vector4> LightCorners = new();
        public readonly List<Vector4> AoCorners = new();
        public readonly List<Vector4> FaceCenters = new();
        public readonly List<Color32> Colors = new();
        public readonly List<Vector3> Normals = new();
        public readonly List<Vector4> Tangents = new();

        public int FaceCount => (BrightTriangles.Count + ShadowTriangles.Count) / 6;

        public bool HasGeometry => BrightTriangles.Count > 0 || ShadowTriangles.Count > 0;

        public void Clear()
        {
            Vertices.Clear();
            BrightTriangles.Clear();
            ShadowTriangles.Clear();
            AllTriangles.Clear();
            UVs.Clear();
            Metadata.Clear();
            LightCorners.Clear();
            AoCorners.Clear();
            FaceCenters.Clear();
            Colors.Clear();
            Normals.Clear();
            Tangents.Clear();
        }
    }
}
