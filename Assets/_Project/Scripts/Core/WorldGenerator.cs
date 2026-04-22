namespace WorldOfVictoria.Core
{
    public sealed class WorldGenerator
    {
        public const string CurrentWorldProfileId = "rd132211-superflat-y43-v1";
        private const int GeneratedSurfaceY = 43;

        public void Generate(WorldData worldData, WorldConfig worldConfig, int seed)
        {
            _ = worldConfig;
            _ = seed;

            ApplySurfacePalette(worldData);
            worldData.CalculateLightDepths();
        }

        public static void ApplySurfacePalette(WorldData worldData)
        {
            var blocks = new byte[worldData.BlockCount];

            for (var x = 0; x < worldData.Width; x++)
            {
                for (var z = 0; z < worldData.Height; z++)
                {
                    for (var y = 0; y < worldData.Depth; y++)
                    {
                        if (y < GeneratedSurfaceY)
                        {
                            blocks[worldData.GetIndexUnchecked(x, y, z)] = VoxelBlockIds.Stone;
                        }
                        else if (y == GeneratedSurfaceY)
                        {
                            blocks[worldData.GetIndexUnchecked(x, y, z)] = VoxelBlockIds.Grass;
                        }
                    }
                }
            }

            worldData.LoadBlocks(blocks);
        }
    }
}
