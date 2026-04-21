using System;

namespace WorldOfVictoria.Core
{
    public sealed class WorldGenerator
    {
        private const int SphereIterationsPerRadius = 1000;

        public void Generate(WorldData worldData, WorldConfig worldConfig, int seed)
        {
            if (worldData == null)
            {
                throw new ArgumentNullException(nameof(worldData));
            }

            if (worldConfig == null)
            {
                throw new ArgumentNullException(nameof(worldConfig));
            }

            var random = new Random(seed);
            worldData.Fill(VoxelBlockIds.Stone);

            for (var caveIndex = 0; caveIndex < worldConfig.CaveIterations; caveIndex++)
            {
                var caveSize = random.Next(worldConfig.CaveRadiusRange.x, worldConfig.CaveRadiusRange.y + 1);
                var caveX = random.Next(0, worldData.Width);
                var caveY = random.Next(0, worldData.Depth);
                var caveZ = random.Next(0, worldData.Height);

                for (var radius = 0; radius < caveSize; radius++)
                {
                    for (var sphere = 0; sphere < SphereIterationsPerRadius; sphere++)
                    {
                        var offsetX = (int)(random.NextDouble() * radius * 2d - radius);
                        var offsetY = (int)(random.NextDouble() * radius * 2d - radius);
                        var offsetZ = (int)(random.NextDouble() * radius * 2d - radius);

                        var distance = offsetX * offsetX + offsetY * offsetY + offsetZ * offsetZ;
                        if (distance > radius * radius)
                        {
                            continue;
                        }

                        var tileX = caveX + offsetX;
                        var tileY = caveY + offsetY;
                        var tileZ = caveZ + offsetZ;

                        if (tileX <= 0 || tileY <= 0 || tileZ <= 0)
                        {
                            continue;
                        }

                        if (tileX >= worldData.Width - 1 || tileY >= worldData.Depth || tileZ >= worldData.Height - 1)
                        {
                            continue;
                        }

                        worldData.SetBlock(tileX, tileY, tileZ, VoxelBlockIds.Air);
                    }
                }
            }

            worldData.CalculateLightDepths();
            ApplySurfacePalette(worldData);
            worldData.CalculateLightDepths();
        }

        public static void ApplySurfacePalette(WorldData worldData)
        {
            for (var x = 0; x < worldData.Width; x++)
            {
                for (var z = 0; z < worldData.Height; z++)
                {
                    var surfaceY = -1;
                    for (var y = worldData.Depth - 1; y >= 0; y--)
                    {
                        if (!worldData.IsSolidBlock(x, y, z))
                        {
                            continue;
                        }

                        surfaceY = y;
                        break;
                    }

                    if (surfaceY < 0)
                    {
                        continue;
                    }

                    var topExposed = surfaceY + 1 >= worldData.Depth || !worldData.IsSolidBlock(x, surfaceY + 1, z);
                    var isColumnSurface = surfaceY == worldData.GetLightDepth(x, z);
                    var hasMaximumSkyLight = worldData.GetSkyLightLevel(x, surfaceY + 1, z) == 15;

                    if (topExposed)
                    {
                        worldData.SetBlock(
                            x,
                            surfaceY,
                            z,
                            isColumnSurface && hasMaximumSkyLight
                                ? VoxelBlockIds.Grass
                                : VoxelBlockIds.Dirt);
                    }

                    for (var dirtDepth = 1; dirtDepth <= 3; dirtDepth++)
                    {
                        var dirtY = surfaceY - dirtDepth;
                        if (dirtY < 0 || !worldData.IsSolidBlock(x, dirtY, z))
                        {
                            continue;
                        }

                        worldData.SetBlock(x, dirtY, z, VoxelBlockIds.Dirt);
                    }
                }
            }
        }
    }
}
