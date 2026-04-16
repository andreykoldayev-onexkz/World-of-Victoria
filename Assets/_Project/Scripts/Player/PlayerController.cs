using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;
using WorldOfVictoria.Utilities;

namespace WorldOfVictoria.Player
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MouseLook mouseLook;
        [SerializeField] private ChunkRuntimeController chunkRuntimeController;

        [Header("Input Actions")]
        [SerializeField] private string playerActionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string resetActionName = "Reset";

        private readonly List<VoxelAabb> collisionBuffer = new();

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction resetAction;
        private BoxCollider playerCollider;

        private Vector3 velocity;
        private Vector3 previousPosition;
        private bool onGround;
        private VoxelAabb boundingBox;

        public Vector3 Velocity => velocity;
        public bool IsGrounded => onGround;

        private void Awake()
        {
            playerCollider = GetComponent<BoxCollider>();
            ResolveReferences();
            ConfigureColliderFromPhysicsConfig();
            BindInput();
            ResetPosition();
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            resetAction?.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            resetAction?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (resetAction != null && resetAction.WasPressedThisFrame())
            {
                ResetPosition();
            }

            if (mouseLook != null && lookAction != null)
            {
                mouseLook.ApplyLook(lookAction.ReadValue<Vector2>());
            }
        }

        private void FixedUpdate()
        {
            if (gameManager == null || !gameManager.HasGeneratedWorld)
            {
                return;
            }

            if (transform.position.y < -8f)
            {
                TeleportTo(gameManager.GetDefaultSpawnPosition(), true);
                return;
            }

            previousPosition = transform.position;

            var moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            var strafe = moveInput.x;
            var forward = moveInput.y;

            if (jumpAction != null && jumpAction.IsPressed() && onGround)
            {
                velocity.y = gameManager.PhysicsConfig.JumpVelocity;
            }

            MoveRelative(strafe, forward, onGround
                ? gameManager.PhysicsConfig.GroundAcceleration
                : gameManager.PhysicsConfig.AirAcceleration);

            velocity.y -= gameManager.PhysicsConfig.GravityPerTick;
            Move(velocity);

            velocity.x *= gameManager.PhysicsConfig.HorizontalDrag;
            velocity.y *= gameManager.PhysicsConfig.VerticalDrag;
            velocity.z *= gameManager.PhysicsConfig.HorizontalDrag;

            if (onGround)
            {
                velocity.x *= gameManager.PhysicsConfig.GroundedHorizontalDrag;
                velocity.z *= gameManager.PhysicsConfig.GroundedHorizontalDrag;
            }
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            if (mouseLook == null)
            {
                mouseLook = GetComponentInChildren<MouseLook>(true);
            }

            if (chunkRuntimeController == null && gameManager != null)
            {
                chunkRuntimeController = gameManager.GetComponent<ChunkRuntimeController>();
            }
        }

        private void ConfigureColliderFromPhysicsConfig()
        {
            if (gameManager?.PhysicsConfig == null)
            {
                return;
            }

            var size = gameManager.PhysicsConfig.PlayerColliderSize;
            playerCollider.size = size;
            playerCollider.center = new Vector3(0f, size.y * 0.5f, 0f);
        }

        private void BindInput()
        {
            var actions = gameManager?.InputActions;
            if (actions == null)
            {
                return;
            }

            var map = actions.FindActionMap(playerActionMapName, true);
            moveAction = map.FindAction(moveActionName, true);
            lookAction = map.FindAction(lookActionName, true);
            jumpAction = map.FindAction(jumpActionName, true);
            resetAction = map.FindAction(resetActionName, true);
        }

        private void ResetPosition()
        {
            if (gameManager?.WorldConfig == null)
            {
                transform.position = Vector3.zero;
                velocity = Vector3.zero;
                return;
            }

            var randomX = Random.Range(0f, gameManager.WorldConfig.Width);
            var randomZ = Random.Range(0f, gameManager.WorldConfig.Height);
            var spawnHeight = gameManager.WorldConfig.Depth + 3f;
            SetPosition(new Vector3(randomX, spawnHeight, randomZ));
            velocity = Vector3.zero;
            chunkRuntimeController?.HandlePlayerTeleported();
        }

        public void TeleportTo(Vector3 position, bool rebuildChunks)
        {
            SetPosition(position);
            velocity = Vector3.zero;
            onGround = false;

            if (rebuildChunks)
            {
                chunkRuntimeController?.HandlePlayerTeleported();
            }
        }

        private void SetPosition(Vector3 position)
        {
            transform.position = position;

            var halfSize = playerCollider.size * 0.5f;
            boundingBox = new VoxelAabb(
                new Vector3(position.x - halfSize.x, position.y - gameManager.PhysicsConfig.EyeHeight, position.z - halfSize.z),
                new Vector3(position.x + halfSize.x, position.y - gameManager.PhysicsConfig.EyeHeight + playerCollider.size.y, position.z + halfSize.z));
        }

        private void MoveRelative(float strafe, float forward, float speed)
        {
            var input = new Vector2(strafe, forward);
            if (input.sqrMagnitude < 0.01f)
            {
                return;
            }

            input = input.normalized * speed;

            var planarForward = transform.forward;
            planarForward.y = 0f;
            planarForward.Normalize();

            var planarRight = transform.right;
            planarRight.y = 0f;
            planarRight.Normalize();

            var worldMove = planarRight * input.x + planarForward * input.y;
            velocity.x += worldMove.x;
            velocity.z += worldMove.z;
        }

        private void Move(Vector3 delta)
        {
            var originalX = delta.x;
            var originalY = delta.y;
            var originalZ = delta.z;

            CollectCollidingCubes(boundingBox.Expand(delta), collisionBuffer);

            for (var i = 0; i < collisionBuffer.Count; i++)
            {
                delta.y = collisionBuffer[i].ClipYCollide(boundingBox, delta.y);
            }
            boundingBox.Move(new Vector3(0f, delta.y, 0f));

            for (var i = 0; i < collisionBuffer.Count; i++)
            {
                delta.x = collisionBuffer[i].ClipXCollide(boundingBox, delta.x);
            }
            boundingBox.Move(new Vector3(delta.x, 0f, 0f));

            for (var i = 0; i < collisionBuffer.Count; i++)
            {
                delta.z = collisionBuffer[i].ClipZCollide(boundingBox, delta.z);
            }
            boundingBox.Move(new Vector3(0f, 0f, delta.z));

            onGround = !Mathf.Approximately(originalY, delta.y) && originalY < 0f;

            if (!Mathf.Approximately(originalX, delta.x)) velocity.x = 0f;
            if (!Mathf.Approximately(originalY, delta.y)) velocity.y = 0f;
            if (!Mathf.Approximately(originalZ, delta.z)) velocity.z = 0f;

            transform.position = new Vector3(
                (boundingBox.Min.x + boundingBox.Max.x) * 0.5f,
                boundingBox.Min.y + gameManager.PhysicsConfig.EyeHeight,
                (boundingBox.Min.z + boundingBox.Max.z) * 0.5f);
        }

        private void CollectCollidingCubes(VoxelAabb area, List<VoxelAabb> buffer)
        {
            buffer.Clear();

            var world = gameManager.RuntimeWorldData;
            var minX = Mathf.Max(0, Mathf.FloorToInt(area.Min.x) - 1);
            var maxX = Mathf.Min(world.Width, Mathf.CeilToInt(area.Max.x) + 1);
            var minY = Mathf.Max(0, Mathf.FloorToInt(area.Min.y) - 1);
            var maxY = Mathf.Min(world.Depth, Mathf.CeilToInt(area.Max.y) + 1);
            var minZ = Mathf.Max(0, Mathf.FloorToInt(area.Min.z) - 1);
            var maxZ = Mathf.Min(world.Height, Mathf.CeilToInt(area.Max.z) + 1);

            for (var x = minX; x < maxX; x++)
            {
                for (var y = minY; y < maxY; y++)
                {
                    for (var z = minZ; z < maxZ; z++)
                    {
                        if (!world.IsSolidBlock(x, y, z))
                        {
                            continue;
                        }

                        buffer.Add(new VoxelAabb(
                            new Vector3(x, y, z),
                            new Vector3(x + 1f, y + 1f, z + 1f)));
                    }
                }
            }
        }
    }
}
