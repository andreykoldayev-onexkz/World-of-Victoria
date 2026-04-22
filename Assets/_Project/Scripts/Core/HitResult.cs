using UnityEngine;

namespace WorldOfVictoria.Core
{
    public readonly struct HitResult
    {
        public HitResult(int x, int y, int z, int face)
        {
            X = x;
            Y = y;
            Z = z;
            Face = face;
            FaceNormal = FaceToNormal(face);
        }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int Face { get; }
        public Vector3Int FaceNormal { get; }
        public Vector3Int Position => new(X, Y, Z);

        public static Vector3Int FaceToNormal(int face)
        {
            return face switch
            {
                0 => Vector3Int.left,
                1 => Vector3Int.right,
                2 => Vector3Int.down,
                3 => Vector3Int.up,
                4 => Vector3Int.back,
                5 => Vector3Int.forward,
                _ => Vector3Int.zero
            };
        }
    }
}
