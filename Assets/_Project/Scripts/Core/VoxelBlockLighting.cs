using UnityEngine;

namespace WorldOfVictoria.Core
{
    public enum VoxelLightDirection : byte
    {
        Down = 0,
        Up = 1,
        North = 2,
        South = 3,
        West = 4,
        East = 5
    }

    public static class VoxelBlockIds
    {
        public const byte Air = 0;
        public const byte Stone = 1;
        public const byte Dirt = 2;
        public const byte Grass = 3;
        public const byte FilterGlass = 4;
    }

    public readonly struct VoxelBlockLightingProperties
    {
        public VoxelBlockLightingProperties(
            bool isSolid,
            byte defaultOpacity,
            byte topOpacity,
            byte bottomOpacity,
            byte northOpacity,
            byte southOpacity,
            byte westOpacity,
            byte eastOpacity,
            bool receivesProbeGi)
        {
            IsSolid = isSolid;
            DefaultOpacity = defaultOpacity;
            TopOpacity = topOpacity;
            BottomOpacity = bottomOpacity;
            NorthOpacity = northOpacity;
            SouthOpacity = southOpacity;
            WestOpacity = westOpacity;
            EastOpacity = eastOpacity;
            ReceivesProbeGi = receivesProbeGi;
        }

        public bool IsSolid { get; }
        public byte DefaultOpacity { get; }
        public byte TopOpacity { get; }
        public byte BottomOpacity { get; }
        public byte NorthOpacity { get; }
        public byte SouthOpacity { get; }
        public byte WestOpacity { get; }
        public byte EastOpacity { get; }
        public bool ReceivesProbeGi { get; }

        public byte GetOpacity(VoxelLightDirection direction)
        {
            return direction switch
            {
                VoxelLightDirection.Down => BottomOpacity,
                VoxelLightDirection.Up => TopOpacity,
                VoxelLightDirection.North => NorthOpacity,
                VoxelLightDirection.South => SouthOpacity,
                VoxelLightDirection.West => WestOpacity,
                VoxelLightDirection.East => EastOpacity,
                _ => DefaultOpacity
            };
        }
    }

    public static class VoxelBlockLighting
    {
        private static readonly VoxelBlockLightingProperties Air =
            new(false, 0, 0, 0, 0, 0, 0, 0, true);

        private static readonly VoxelBlockLightingProperties Stone =
            new(true, 15, 15, 15, 15, 15, 15, 15, false);

        private static readonly VoxelBlockLightingProperties Dirt =
            new(true, 15, 15, 15, 15, 15, 15, 15, false);

        private static readonly VoxelBlockLightingProperties Grass =
            new(true, 15, 15, 15, 15, 15, 15, 15, false);

        // Prepared for future translucent blocks. Not yet used in world generation.
        private static readonly VoxelBlockLightingProperties FilterGlass =
            new(true, 3, 2, 4, 3, 3, 3, 3, true);

        public static VoxelBlockLightingProperties GetProperties(byte blockId)
        {
            return blockId switch
            {
                VoxelBlockIds.Air => Air,
                VoxelBlockIds.Stone => Stone,
                VoxelBlockIds.Dirt => Dirt,
                VoxelBlockIds.Grass => Grass,
                VoxelBlockIds.FilterGlass => FilterGlass,
                _ => Stone
            };
        }

        public static bool IsSolid(byte blockId)
        {
            return GetProperties(blockId).IsSolid;
        }

        public static byte GetDirectionalOpacity(byte blockId, VoxelLightDirection direction)
        {
            return GetProperties(blockId).GetOpacity(direction);
        }

        public static bool ReceivesProbeGi(byte blockId)
        {
            return GetProperties(blockId).ReceivesProbeGi;
        }
    }
}
