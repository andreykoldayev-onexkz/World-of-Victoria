using UnityEngine;

namespace WorldOfVictoria.Chunking
{
    public sealed class ChunkData
    {
        public ChunkData(int index, Vector3Int coord, BoundsInt blockBounds)
        {
            Index = index;
            Coord = coord;
            BlockBounds = blockBounds;

            var center = new Vector3(
                blockBounds.xMin + blockBounds.size.x * 0.5f,
                blockBounds.yMin + blockBounds.size.y * 0.5f,
                blockBounds.zMin + blockBounds.size.z * 0.5f);

            WorldBounds = new Bounds(center, blockBounds.size);
        }

        public int Index { get; }
        public Vector3Int Coord { get; }
        public BoundsInt BlockBounds { get; }
        public Bounds WorldBounds { get; }
        public bool IsDirty { get; private set; } = true;

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void ClearDirty()
        {
            IsDirty = false;
        }
    }
}
