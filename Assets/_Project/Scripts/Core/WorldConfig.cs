using UnityEngine;

namespace WorldOfVictoria.Core
{
    [CreateAssetMenu(fileName = "WorldConfig", menuName = "World of Victoria/Config/World Config")]
    public sealed class WorldConfig : ScriptableObject
    {
        public const int DefaultWidth = 256;
        public const int DefaultHeight = 256;
        public const int DefaultDepth = 64;
        public const int DefaultChunkSize = 16;
        public const int DefaultCaveIterations = 10000;

        [Header("World Dimensions")]
        [Min(1)] [SerializeField] private int width = DefaultWidth;
        [Min(1)] [SerializeField] private int height = DefaultHeight;
        [Min(1)] [SerializeField] private int depth = DefaultDepth;

        [Header("Chunking")]
        [Min(1)] [SerializeField] private int chunkSize = DefaultChunkSize;

        [Header("Generation")]
        [Min(1)] [SerializeField] private int caveIterations = DefaultCaveIterations;
        [SerializeField] private Vector2Int caveRadiusRange = new(1, 7);

        [Header("Rendering")]
        [SerializeField] private Color fogColor = new(14f / 255f, 11f / 255f, 10f / 255f, 1f);
        [SerializeField] private float fogStart = -10f;
        [SerializeField] private float fogEnd = 20f;
        [SerializeField, Min(0f)] private float fogDensity = 0.03f;

        public int Width => width;
        public int Height => height;
        public int Depth => depth;
        public int ChunkSize => chunkSize;
        public int CaveIterations => caveIterations;
        public Vector2Int CaveRadiusRange => caveRadiusRange;
        public Color FogColor => fogColor;
        public float FogStart => fogStart;
        public float FogEnd => fogEnd;
        public float FogDensity => fogDensity;

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            depth = Mathf.Max(1, depth);
            chunkSize = Mathf.Max(1, chunkSize);
            caveIterations = Mathf.Max(1, caveIterations);
            caveRadiusRange.x = Mathf.Max(1, caveRadiusRange.x);
            caveRadiusRange.y = Mathf.Max(caveRadiusRange.x, caveRadiusRange.y);
            fogDensity = Mathf.Max(0f, fogDensity);
        }
    }
}
