using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldOfVictoria.Core
{
    [Serializable]
    public sealed class WorldData
    {
        private const byte MaxSkyLight = 15;
        private const byte SkyLightFalloff = 1;
        private const int LocalSkyLightRadius = MaxSkyLight;

        private readonly byte[] blocks;
        private readonly int[] lightDepths;
        private readonly byte[] skyLight;
        private readonly int[] propagationQueue;
        private readonly List<ILevelListener> listeners = new();

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

            var index = GetIndexUnchecked(x, y, z);
            var oldBlock = blocks[index];
            if (oldBlock == blockType)
            {
                return;
            }

            blocks[index] = blockType;
            NotifyTileChanged(x, y, z);

            if (NeedsLightingUpdate(oldBlock, blockType))
            {
                RecalculateLightColumn(x, z);
            }
        }

        public bool IsTile(int x, int y, int z)
        {
            return GetBlock(x, y, z) != VoxelBlockIds.Air;
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
            return VoxelBlockLighting.IsSolid(GetBlock(x, y, z));
        }

        public bool IsLightBlocker(int x, int y, int z)
        {
            return GetDirectionalOpacity(x, y, z, VoxelLightDirection.Down) >= MaxSkyLight;
        }

        public byte GetDirectionalOpacity(int x, int y, int z, VoxelLightDirection direction)
        {
            return VoxelBlockLighting.GetDirectionalOpacity(GetBlock(x, y, z), direction);
        }

        public bool ReceivesProbeGi(int x, int y, int z)
        {
            return VoxelBlockLighting.ReceivesProbeGi(GetBlock(x, y, z));
        }

        public float GetBrightness(int x, int y, int z)
        {
            var level = GetSkyLightLevel(x, y, z);
            return Mathf.SmoothStep(0f, 1f, level / 15f);
        }

        public float GetNormalizedSkyLight(int x, int y, int z)
        {
            return GetSkyLightLevel(x, y, z) / 15f;
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

        public void AddListener(ILevelListener listener)
        {
            if (listener == null || listeners.Contains(listener))
            {
                return;
            }

            listeners.Add(listener);
        }

        public void RemoveListener(ILevelListener listener)
        {
            if (listener == null)
            {
                return;
            }

            listeners.Remove(listener);
        }

        public void NotifyAllChanged()
        {
            for (var i = 0; i < listeners.Count; i++)
            {
                listeners[i].OnAllChanged();
            }
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

            RebuildSkyLightFull();
        }

        public int GetLightDepth(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Height)
            {
                return Depth - 1;
            }

            return lightDepths[x + z * Width];
        }

        private bool NeedsLightingUpdate(byte oldBlock, byte newBlock)
        {
            if (oldBlock == newBlock)
            {
                return false;
            }

            if (VoxelBlockLighting.IsSolid(oldBlock) != VoxelBlockLighting.IsSolid(newBlock))
            {
                return true;
            }

            for (var i = 0; i < 6; i++)
            {
                var direction = (VoxelLightDirection)i;
                if (VoxelBlockLighting.GetDirectionalOpacity(oldBlock, direction) != VoxelBlockLighting.GetDirectionalOpacity(newBlock, direction))
                {
                    return true;
                }
            }

            return false;
        }

        private void RecalculateLightColumn(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Height)
            {
                return;
            }

            var oldDepth = lightDepths[x + z * Width];
            var newDepth = Depth - 1;
            while (newDepth >= 0 && !IsLightBlocker(x, newDepth, z))
            {
                newDepth--;
            }

            lightDepths[x + z * Width] = newDepth;
            RebuildSkyLightRegion(x, z, LocalSkyLightRadius);

            if (oldDepth != newDepth)
            {
                NotifyLightColumnChanged(x, z, oldDepth, newDepth);
            }
        }

        private void RebuildSkyLightFull()
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

                TryPropagateLight(x - 1, y, z, nextLight, VoxelLightDirection.West, ref queueTail);
                TryPropagateLight(x + 1, y, z, nextLight, VoxelLightDirection.East, ref queueTail);
                TryPropagateLight(x, y - 1, z, downLight, VoxelLightDirection.Down, ref queueTail);
                TryPropagateLight(x, y + 1, z, nextLight, VoxelLightDirection.Up, ref queueTail);
                TryPropagateLight(x, y, z - 1, nextLight, VoxelLightDirection.North, ref queueTail);
                TryPropagateLight(x, y, z + 1, nextLight, VoxelLightDirection.South, ref queueTail);
            }
        }

        private void RebuildSkyLightRegion(int centerX, int centerZ, int radius)
        {
            var minX = Mathf.Max(0, centerX - radius);
            var maxX = Mathf.Min(Width - 1, centerX + radius);
            var minZ = Mathf.Max(0, centerZ - radius);
            var maxZ = Mathf.Min(Height - 1, centerZ + radius);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
                {
                    for (var y = 0; y < Depth; y++)
                    {
                        skyLight[GetIndexUnchecked(x, y, z)] = 0;
                    }
                }
            }

            var queueHead = 0;
            var queueTail = 0;

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minZ; z <= maxZ; z++)
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

                TryPropagateLightRegion(x - 1, y, z, nextLight, VoxelLightDirection.West, minX, maxX, minZ, maxZ, ref queueTail);
                TryPropagateLightRegion(x + 1, y, z, nextLight, VoxelLightDirection.East, minX, maxX, minZ, maxZ, ref queueTail);
                TryPropagateLightRegion(x, y - 1, z, downLight, VoxelLightDirection.Down, minX, maxX, minZ, maxZ, ref queueTail);
                TryPropagateLightRegion(x, y + 1, z, nextLight, VoxelLightDirection.Up, minX, maxX, minZ, maxZ, ref queueTail);
                TryPropagateLightRegion(x, y, z - 1, nextLight, VoxelLightDirection.North, minX, maxX, minZ, maxZ, ref queueTail);
                TryPropagateLightRegion(x, y, z + 1, nextLight, VoxelLightDirection.South, minX, maxX, minZ, maxZ, ref queueTail);
            }
        }

        private void TryPropagateLight(int x, int y, int z, byte lightLevel, VoxelLightDirection direction, ref int queueTail)
        {
            if (lightLevel == 0 || !InBounds(x, y, z))
            {
                return;
            }

            var opacity = GetDirectionalOpacity(x, y, z, direction);
            if (opacity >= MaxSkyLight)
            {
                return;
            }

            var propagatedLight = (byte)Mathf.Max(0, lightLevel - opacity);
            if (propagatedLight == 0)
            {
                return;
            }

            var index = GetIndexUnchecked(x, y, z);
            if (skyLight[index] >= propagatedLight)
            {
                return;
            }

            skyLight[index] = propagatedLight;
            propagationQueue[queueTail++] = index;
        }

        private void TryPropagateLightRegion(
            int x,
            int y,
            int z,
            byte lightLevel,
            VoxelLightDirection direction,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            ref int queueTail)
        {
            if (x < minX || x > maxX || z < minZ || z > maxZ)
            {
                return;
            }

            TryPropagateLight(x, y, z, lightLevel, direction, ref queueTail);
        }

        private void DecodeIndex(int index, out int x, out int y, out int z)
        {
            x = index % Width;
            var yz = index / Width;
            y = yz / Height;
            z = yz % Height;
        }

        private void NotifyTileChanged(int x, int y, int z)
        {
            for (var i = 0; i < listeners.Count; i++)
            {
                listeners[i].OnTileChanged(x, y, z);
            }
        }

        private void NotifyLightColumnChanged(int x, int z, int oldDepth, int newDepth)
        {
            for (var i = 0; i < listeners.Count; i++)
            {
                listeners[i].OnLightColumnChanged(x, z, oldDepth, newDepth);
            }
        }
    }
}
