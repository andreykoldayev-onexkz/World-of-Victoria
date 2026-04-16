using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Player;
using WorldOfVictoria.UI;

namespace WorldOfVictoria.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Configs")]
        [SerializeField] private WorldConfig worldConfig;
        [SerializeField] private PhysicsConfig physicsConfig;

        [Header("Scene References")]
        [SerializeField] private Transform worldRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LoadingScreenUI loadingScreen;

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("World Bootstrap")]
        [SerializeField] private bool generateOnPlay = true;
        [SerializeField] private bool generateEditorPreview = true;
        [SerializeField] private bool loadLastSaveOnPlay = true;
        [SerializeField] private bool saveOnApplicationQuit = true;
        [SerializeField] private int generationSeed;
        [SerializeField] private string saveFilePath = string.Empty;

        [Header("Presentation")]
        [SerializeField] private ChunkPresentationController chunkPresentationController;

        private WorldData runtimeWorldData;
        private ChunkManager runtimeChunkManager;
        private Texture3D runtimeSkyLightVolume;
        private int lastGeneratedSeed;
        private bool isBootstrapping;

        public WorldConfig WorldConfig => worldConfig;
        public PhysicsConfig PhysicsConfig => physicsConfig;
        public Transform WorldRoot => worldRoot;
        public Transform PlayerRoot => playerRoot;
        public Camera PlayerCamera => playerCamera;
        public InputActionAsset InputActions => inputActions;
        public WorldData RuntimeWorldData => runtimeWorldData;
        public ChunkManager RuntimeChunkManager => runtimeChunkManager;
        public int LastGeneratedSeed => lastGeneratedSeed;
        public bool HasGeneratedWorld => runtimeWorldData != null && runtimeChunkManager != null;
        public ChunkPresentationController ChunkPresentationController => chunkPresentationController;
        public string SaveFilePath => string.IsNullOrWhiteSpace(saveFilePath) ? SaveSystem.DefaultSavePath : saveFilePath;

        public Vector3 GetDefaultSpawnPosition()
        {
            if (worldConfig == null)
            {
                return Vector3.zero;
            }

            return new Vector3(worldConfig.Width * 0.5f, worldConfig.Depth + 3f, worldConfig.Height * 0.5f);
        }

        private void OnEnable()
        {
            if (chunkPresentationController == null)
            {
                chunkPresentationController = GetComponent<ChunkPresentationController>();
            }

            if (!Application.isPlaying && generateEditorPreview)
            {
                GenerateRuntimeWorld();
            }
        }

        private IEnumerator Start()
        {
            if (Application.isPlaying && generateOnPlay)
            {
                yield return StartCoroutine(BootstrapRuntimeWorld());
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || isBootstrapping)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                SaveCurrentGame();
            }

            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                StartCoroutine(ReloadLatestSave());
            }
        }

        private void OnApplicationQuit()
        {
            if (!Application.isPlaying || !saveOnApplicationQuit)
            {
                return;
            }

            SaveCurrentGame();
        }

        private IEnumerator BootstrapRuntimeWorld()
        {
            isBootstrapping = true;
            SetLoadingState(true, 0f, "Preparing world...", "Initializing runtime state");
            yield return null;

            GameSaveData? saveData = null;
            if (loadLastSaveOnPlay && SaveSystem.HasLastSavePath())
            {
                SetLoadingState(true, 0.2f, "Loading save...", "Reading compressed world data");
                yield return null;

                try
                {
                    saveData = SaveSystem.Load(SaveSystem.GetLastSavePath());
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning($"Failed to load last save. Falling back to world generation.\n{exception}", this);
                }
            }

            if (saveData.HasValue)
            {
                SetLoadingState(true, 0.45f, "Restoring world...", "Applying world blocks");
                ApplySaveData(saveData.Value);
                yield return null;
                SetLoadingState(true, 0.75f, "Restoring player...", "Rebuilding active chunks");
                TeleportPlayer(saveData.Value.PlayerPosition);
            }
            else
            {
                SetLoadingState(true, 0.35f, "Generating world...", "Carving caves and calculating light");
                GenerateRuntimeWorld();
                yield return null;
                SetLoadingState(true, 0.75f, "Spawning player...", "Preparing initial chunk region");
                TeleportPlayer(GetDefaultSpawnPosition());
            }

            SetLoadingState(true, 1f, "Ready", SaveFilePath);
            yield return null;
            SetLoadingState(false, 1f, string.Empty, string.Empty);
            isBootstrapping = false;
        }

        [ContextMenu("Generate Runtime World")]
        public void GenerateRuntimeWorld()
        {
            if (worldConfig == null)
            {
                Debug.LogWarning("GameManager is missing WorldConfig.", this);
                return;
            }

            var seedToUse = generationSeed != 0 ? generationSeed : System.Environment.TickCount;

            runtimeWorldData = new WorldData(worldConfig.Width, worldConfig.Height, worldConfig.Depth);

            var worldGenerator = new WorldGenerator();
            worldGenerator.Generate(runtimeWorldData, worldConfig, seedToUse);

            runtimeChunkManager = new ChunkManager(runtimeWorldData, worldConfig.ChunkSize);
            lastGeneratedSeed = seedToUse;
            UpdateWorldLightingVolume();

            if (!Application.isPlaying && chunkPresentationController != null && chunkPresentationController.ShouldBuildPreviewOnGenerate())
            {
                BuildChunkPreviewMeshes();
            }
        }

        public void SaveCurrentGame()
        {
            if (!HasGeneratedWorld)
            {
                return;
            }

            var saveData = new GameSaveData(
                runtimeWorldData.Width,
                runtimeWorldData.Height,
                runtimeWorldData.Depth,
                lastGeneratedSeed,
                playerRoot != null ? playerRoot.position : GetDefaultSpawnPosition(),
                runtimeWorldData.RawBlocks.ToArray());

            SaveSystem.Save(SaveFilePath, saveData);
        }

        public IEnumerator ReloadLatestSave()
        {
            if (!SaveSystem.HasLastSavePath())
            {
                yield break;
            }

            isBootstrapping = true;
            SetLoadingState(true, 0.15f, "Loading save...", "Reading last saved world");
            yield return null;

            var saveData = SaveSystem.Load(SaveSystem.GetLastSavePath());
            SetLoadingState(true, 0.55f, "Restoring world...", "Applying blocks and rebuilding chunks");
            ApplySaveData(saveData);
            yield return null;
            TeleportPlayer(saveData.PlayerPosition);
            SetLoadingState(true, 1f, "Loaded", SaveSystem.GetLastSavePath());
            yield return null;
            SetLoadingState(false, 1f, string.Empty, string.Empty);
            isBootstrapping = false;
        }

        [ContextMenu("Build Chunk Preview Meshes")]
        public void BuildChunkPreviewMeshes()
        {
            chunkPresentationController?.BuildChunkPreviewMeshes();
        }

        private void ApplySaveData(GameSaveData saveData)
        {
            runtimeWorldData = new WorldData(saveData.Width, saveData.Height, saveData.Depth);
            runtimeWorldData.LoadBlocks(saveData.Blocks);
            runtimeWorldData.CalculateLightDepths();
            runtimeChunkManager = new ChunkManager(runtimeWorldData, worldConfig.ChunkSize);
            lastGeneratedSeed = saveData.GenerationSeed;
            UpdateWorldLightingVolume();

            if (!Application.isPlaying && chunkPresentationController != null && chunkPresentationController.ShouldBuildPreviewOnGenerate())
            {
                BuildChunkPreviewMeshes();
            }

            if (TryGetComponent<ChunkRuntimeController>(out var runtimeController))
            {
                runtimeController.ForceReinitialize();
            }
        }

        private void TeleportPlayer(Vector3 targetPosition)
        {
            if (playerRoot == null)
            {
                return;
            }

            targetPosition = ResolveSafePlayerPosition(targetPosition);

            var controller = playerRoot.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.TeleportTo(targetPosition, true);
                return;
            }

            playerRoot.position = targetPosition;
            if (TryGetComponent<ChunkRuntimeController>(out var runtimeController))
            {
                runtimeController.HandlePlayerTeleported();
            }
        }

        private void UpdateWorldLightingVolume()
        {
            if (runtimeWorldData == null || chunkPresentationController?.Settings == null)
            {
                return;
            }

            ReleaseWorldLightingVolume();

            var width = runtimeWorldData.Width;
            var depth = runtimeWorldData.Depth;
            var height = runtimeWorldData.Height;
            var lightData = new Color[width * depth * height];

            for (var z = 0; z < height; z++)
            {
                for (var y = 0; y < depth; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var light = SampleSmoothedSkyLight(x, y, z);
                        var index = x + (y * width) + (z * width * depth);
                        lightData[index] = new Color(light, light, light, 1f);
                    }
                }
            }

            runtimeSkyLightVolume = new Texture3D(width, depth, height, TextureFormat.RGBA32, false)
            {
                name = "RuntimeSkyLightVolume",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            runtimeSkyLightVolume.SetPixels(lightData);
            runtimeSkyLightVolume.Apply(false, false);

            ApplyWorldLightingToMaterial(chunkPresentationController.Settings.BrightMaterial);
            ApplyWorldLightingToMaterial(chunkPresentationController.Settings.ShadowMaterial);
        }

        private void ApplyWorldLightingToMaterial(Material material)
        {
            if (material == null || runtimeSkyLightVolume == null || runtimeWorldData == null)
            {
                return;
            }

            material.SetTexture("_SkyLightVolume", runtimeSkyLightVolume);
            material.SetFloat("_LightVolumeStrength", 0.55f);
            material.SetVector("_WorldLightVolumeSize", new Vector4(runtimeWorldData.Width, runtimeWorldData.Depth, runtimeWorldData.Height, 0f));
        }

        private float SampleSmoothedSkyLight(int x, int y, int z)
        {
            var center = runtimeWorldData.GetSkyLightLevel(x, y, z) / 15f;
            var neighborSum = center * 4f;

            neighborSum += runtimeWorldData.GetSkyLightLevel(x - 1, y, z) / 15f;
            neighborSum += runtimeWorldData.GetSkyLightLevel(x + 1, y, z) / 15f;
            neighborSum += runtimeWorldData.GetSkyLightLevel(x, y - 1, z) / 15f;
            neighborSum += runtimeWorldData.GetSkyLightLevel(x, y + 1, z) / 15f;
            neighborSum += runtimeWorldData.GetSkyLightLevel(x, y, z - 1) / 15f;
            neighborSum += runtimeWorldData.GetSkyLightLevel(x, y, z + 1) / 15f;

            var smoothed = neighborSum / 10f;
            return Mathf.Lerp(center, smoothed, 0.55f);
        }

        private void ReleaseWorldLightingVolume()
        {
            if (runtimeSkyLightVolume == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeSkyLightVolume);
            }
            else
            {
                DestroyImmediate(runtimeSkyLightVolume);
            }

            runtimeSkyLightVolume = null;
        }

        private Vector3 ResolveSafePlayerPosition(Vector3 candidatePosition)
        {
            if (worldConfig == null || !HasGeneratedWorld)
            {
                return candidatePosition;
            }

            if (float.IsNaN(candidatePosition.x) || float.IsNaN(candidatePosition.y) || float.IsNaN(candidatePosition.z))
            {
                return GetDefaultSpawnPosition();
            }

            if (candidatePosition.y < -8f)
            {
                return GetDefaultSpawnPosition();
            }

            if (candidatePosition.x < 0f || candidatePosition.x >= worldConfig.Width
                || candidatePosition.z < 0f || candidatePosition.z >= worldConfig.Height)
            {
                return GetDefaultSpawnPosition();
            }

            return candidatePosition;
        }

        private void SetLoadingState(bool visible, float progress, string status, string detail)
        {
            if (loadingScreen == null)
            {
                return;
            }

            loadingScreen.SetVisible(visible);
            loadingScreen.SetProgress(progress, status, detail);
        }
    }
}
