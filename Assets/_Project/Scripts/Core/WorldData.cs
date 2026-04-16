using System;
using UnityEngine;

namespace WorldOfVictoria.Core
{
    [Serializable]
    public sealed class WorldData
    {
        private const byte MaxSkyLight = 15;
        private const byte SkyLightFalloff = 1;

        private readonly byte[] blocks;
        private readonly int[] lightDepths;
        private readonly byte[] skyLight;
        private readonly int[] propagationQueue;

        public WorldData(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;

            blocks = new byte[width * height * depth];
            lightDepths = new int[width * height];
            skyLight = new byte[width * height * depth];
            propagationQueue = new int[width * height * depth];
        }

        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }

        public int BlockCount => blocks.Length;
        public ReadOnlyMemory<byte> RawBlocks => blocks;

        public bool InBounds(int x, int y, int z)
        {
            return x >= 0 && x < Width
                && y >= 0 && y < Depth
                && z >= 0 && z < Height;
        }

        public int GetIndexUnchecked(int x, int y, int z)
        {
            return (y * Height + z) * Width + x;
        }

        public byte GetBlock(int x, int y, int z)
        {
            if (!InBounds(x, y, z))
            {
                return 0;
            }

            return blocks[GetIndexUnchecked(x, y, z)];
        }

        public void SetBlock(int x, int y, int z, byte blockType)
        {
            if (!InBounds(x, y, z))
            {
                return;
            }

            blocks[GetIndexUnchecked(x, y, z)] = blockType;
        }

        public bool IsTile(int x, int y, int z)
        {
            return GetBlock(x, y, z) != 0;
        }

        public void LoadBlocks(ReadOnlySpan<byte> sourceBlocks)
        {
            if (sourceBlocks.Length != blocks.Length)
            {
                throw new ArgumentException($"Block buffer length mismatch. Expected {blocks.Length}, got {sourceBlocks.Length}.", nameof(sourceBlocks));
            }

            sourceBlocks.CopyTo(blocks);
        }

        public bool IsSolidBlock(int x, int y, int z)
        {
            return IsTile(x, y, z);
        }

        public bool IsLightBlocker(int x, int y, int z)
        {
            return IsSolidBlock(x, y, z);
        }

        public float GetBrightness(int x, int y, int z)
        {
            var level = GetSkyLightLevel(x, y, z);
            return Mathf.SmoothStep(0f, 1f, level / 15f);
        }

        public byte GetSkyLightLevel(int x, int y, int z)
        {
            if (!InBounds(x, y, z))
            {
                return MaxSkyLight;
            }

            return skyLight[GetIndexUnchecked(x, y, z)];
        }

        public void Fill(byte blockType)
        {
            Array.Fill(blocks, blockType);
        }

        public void CalculateLightDepths()
        {
            CalculateLightDepths(0, 0, Width, Height);
        }

        public void CalculateLightDepths(int minX, int minZ, int sizeX, int sizeZ)
        {
            var maxX = Mathf.Min(Width, minX + sizeX);
            var maxZ = Mathf.Min(Height, minZ + sizeZ);

            for (var x = Mathf.Max(0, minX); x < maxX; x++)
            {
                for (var z = Mathf.Max(0, minZ); z < maxZ; z++)
                {
                    var depth = Depth - 1;
                    while (depth >= 0 && !IsLightBlocker(x, depth, z))
                    {
                        depth--;
                    }

                    lightDepths[x + z * Width] = depth;
                }
            }

            RebuildSkyLight();
        }

        public int GetLightDepth(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Height)
            {
                return Depth - 1;
            }

            return lightDepths[x + z * Width];
        }

        private void RebuildSkyLight()
        {
            Array.Clear(skyLight, 0, skyLight.Length);

            var queueHead = 0;
            var queueTail = 0;

            for (var x = 0; x < Width; x++)
            {
                for (var z = 0; z < Height; z++)
                {
                    var lightDepth = lightDepths[x + z * Width];
                    for (var y = Depth - 1; y > lightDepth; y--)
                    {
                        if (IsSolidBlock(x, y, z))
                        {
                            continue;
                        }

                        var index = GetIndexUnchecked(x, y, z);
                        skyLight[index] = MaxSkyLight;
                        propagationQueue[queueTail++] = index;
                    }
                }
            }

            while (queueHead < queueTail)
            {
                var index = propagationQueue[queueHead++];
                var currentLight = skyLight[index];
                if (currentLight == 0)
                {
                    continue;
                }

                DecodeIndex(index, out var x, out var y, out var z);
                var nextLight = (byte)Mathf.Max(0, currentLight - SkyLightFalloff);
                var downLight = currentLight == MaxSkyLight ? MaxSkyLight : nextLight;

                TryPropagateLight(x - 1, y, z, nextLight, ref queueTail);
                TryPropagateLight(x + 1, y, z, nextLight, ref queueTail);
                TryPropagateLight(x, y - 1, z, downLight, ref queueTail);
                TryPropagateLight(x, y + 1, z, nextLight, ref queueTail);
                TryPropagateLight(x, y, z - 1, nextLight, ref queueTail);
                TryPropagateLight(x, y, z + 1, nextLight, ref queueTail);
            }
        }

        private void TryPropagateLight(int x, int y, int z, byte lightLevel, ref int queueTail)
        {
            if (lightLevel == 0 || !InBounds(x, y, z) || IsSolidBlock(x, y, z))
            {
                return;
            }

            var index = GetIndexUnchecked(x, y, z);
            if (skyLight[index] >= lightLevel)
            {
                return;
            }

            skyLight[index] = lightLevel;
            propagationQueue[queueTail++] = index;
        }

        private void DecodeIndex(int index, out int x, out int y, out int z)
        {
            x = index % Width;
            var yz = index / Width;
            y = yz / Height;
            z = yz % Height;
        }
    }
}
