using UnityEngine;
using UnityEngine.InputSystem;
using WorldOfVictoria.Core;
using WorldOfVictoria.Utilities;

namespace WorldOfVictoria.Player
{
    [DisallowMultipleComponent]
    public sealed class BlockInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private BlockOutlineRenderer outlineRenderer;
        [SerializeField] private PlayerController playerController;

        [Header("Raycast")]
        [SerializeField, Min(0.5f)] private float maxReachDistance = 5f;
        [SerializeField] private byte selectedBlockType = VoxelBlockIds.Stone;

        [Header("Input Actions")]
        [SerializeField] private string playerActionMapName = "Player";
        [SerializeField] private string attackActionName = "Attack";
        [SerializeField] private string interactActionName = "Interact";

        [Header("Debug")]
        [SerializeField] private bool drawDebugRay = true;
        [SerializeField] private bool drawDebugHitCube = true;
        [SerializeField] private Color debugRayColor = new(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] private Color debugHitColor = new(0.2f, 1f, 0.7f, 0.9f);

        private HitResult? currentHitResult;
        private InputAction attackAction;
        private InputAction interactAction;

        public HitResult? CurrentHitResult => currentHitResult;
        public float MaxReachDistance => maxReachDistance;
        public byte SelectedBlockType => selectedBlockType;

        private void Awake()
        {
            ResolveReferences();
            BindInput();
        }

        private void OnEnable()
        {
            attackAction?.Enable();
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            attackAction?.Disable();
            interactAction?.Disable();
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (gameManager == null || playerCamera == null || !gameManager.HasGeneratedWorld)
            {
                currentHitResult = null;
                outlineRenderer?.Hide();
                return;
            }

            if (BlockRaycaster.RaycastDda(
                    playerCamera.transform.position,
                    playerCamera.transform.forward,
                    maxReachDistance,
                    gameManager.RuntimeWorldData,
                    out var hit))
            {
                currentHitResult = hit;
            }
            else
            {
                currentHitResult = null;
            }

            if (currentHitResult.HasValue)
            {
                outlineRenderer?.Show(currentHitResult.Value);
            }
            else
            {
                outlineRenderer?.Hide();
            }

            HandleBlockModification();

            if (drawDebugRay)
            {
                var end = currentHitResult.HasValue
                    ? (Vector3)currentHitResult.Value.Position + Vector3.one * 0.5f
                    : playerCamera.transform.position + playerCamera.transform.forward * maxReachDistance;
                Debug.DrawLine(playerCamera.transform.position, end, debugRayColor);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugHitCube || !currentHitResult.HasValue)
            {
                return;
            }

            Gizmos.color = debugHitColor;
            Gizmos.DrawWireCube((Vector3)currentHitResult.Value.Position + Vector3.one * 0.5f, Vector3.one * 1.02f);
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            if (playerCamera == null && gameManager != null)
            {
                playerCamera = gameManager.PlayerCamera;
            }

            if (outlineRenderer == null)
            {
                outlineRenderer = GetComponent<BlockOutlineRenderer>();
            }

            if (outlineRenderer == null)
            {
                outlineRenderer = gameObject.AddComponent<BlockOutlineRenderer>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        private void BindInput()
        {
            if (gameManager?.InputActions == null)
            {
                return;
            }

            var map = gameManager.InputActions.FindActionMap(playerActionMapName, true);
            attackAction = map.FindAction(attackActionName, false);
            interactAction = map.FindAction(interactActionName, false);
        }

        private void HandleBlockModification()
        {
            if (!currentHitResult.HasValue || gameManager?.RuntimeWorldData == null)
            {
                return;
            }

            var destroyPressed = attackAction != null && attackAction.WasPressedThisFrame();
            var placePressed = (interactAction != null && interactAction.WasPressedThisFrame())
                || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

            if (destroyPressed)
            {
                var hit = currentHitResult.Value;
                gameManager.RuntimeWorldData.SetBlock(hit.X, hit.Y, hit.Z, VoxelBlockIds.Air);
                return;
            }

            if (!placePressed)
            {
                return;
            }

            var placePosition = currentHitResult.Value.Position + currentHitResult.Value.FaceNormal;
            if (!CanPlaceBlockAt(placePosition))
            {
                return;
            }

            gameManager.RuntimeWorldData.SetBlock(placePosition.x, placePosition.y, placePosition.z, selectedBlockType);
        }

        private bool CanPlaceBlockAt(Vector3Int position)
        {
            var world = gameManager?.RuntimeWorldData;
            if (world == null || !world.InBounds(position.x, position.y, position.z))
            {
                return false;
            }

            if (world.GetBlock(position.x, position.y, position.z) != VoxelBlockIds.Air)
            {
                return false;
            }

            return !DoesBlockIntersectPlayer(position);
        }

        private bool DoesBlockIntersectPlayer(Vector3Int blockPosition)
        {
            if (gameManager?.PhysicsConfig == null)
            {
                return false;
            }

            var playerTransform = gameManager.PlayerRoot != null ? gameManager.PlayerRoot : transform;
            var colliderSize = gameManager.PhysicsConfig.PlayerColliderSize;
            var eyeHeight = gameManager.PhysicsConfig.EyeHeight;
            var halfSize = colliderSize * 0.5f;

            var playerBounds = new VoxelAabb(
                new Vector3(playerTransform.position.x - halfSize.x, playerTransform.position.y - eyeHeight, playerTransform.position.z - halfSize.z),
                new Vector3(playerTransform.position.x + halfSize.x, playerTransform.position.y - eyeHeight + colliderSize.y, playerTransform.position.z + halfSize.z));

            var blockBounds = new VoxelAabb(
                new Vector3(blockPosition.x, blockPosition.y, blockPosition.z),
                new Vector3(blockPosition.x + 1f, blockPosition.y + 1f, blockPosition.z + 1f));

            return Intersects(playerBounds, blockBounds);
        }

        private static bool Intersects(VoxelAabb a, VoxelAabb b)
        {
            return a.Max.x > b.Min.x && a.Min.x < b.Max.x
                && a.Max.y > b.Min.y && a.Min.y < b.Max.y
                && a.Max.z > b.Min.z && a.Min.z < b.Max.z;
        }
    }
}
