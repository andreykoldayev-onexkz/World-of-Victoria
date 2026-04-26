using UnityEngine;
using UnityEngine.InputSystem;
using WorldOfVictoria.Chunking;
using WorldOfVictoria.Core;

namespace WorldOfVictoria.Player
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlayerController : Entity
    {
        [Header("References")]
        [SerializeField] private MouseLook mouseLook;
        [SerializeField] private ChunkRuntimeController chunkRuntimeController;
        [SerializeField] private BlockInteractionController blockInteractionController;

        [Header("Input Actions")]
        [SerializeField] private string playerActionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField] private string resetActionName = "Reset";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction resetAction;
        private BoxCollider playerCollider;

        protected override Vector3 EntityColliderSize =>
            gameManager?.PhysicsConfig != null ? gameManager.PhysicsConfig.PlayerColliderSize : playerCollider != null ? playerCollider.size : new Vector3(0.6f, 1.8f, 0.6f);

        protected override float EntityEyeHeight =>
            gameManager?.PhysicsConfig != null ? gameManager.PhysicsConfig.EyeHeight : 1.62f;

        protected override void Awake()
        {
            playerCollider = GetComponent<BoxCollider>();
            base.Awake();
            ConfigureColliderFromPhysicsConfig();
            SyncBoundingBoxFromTransform();
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
            MoveWithWorldCollision(velocity);

            velocity.x *= gameManager.PhysicsConfig.HorizontalDrag;
            velocity.y *= gameManager.PhysicsConfig.VerticalDrag;
            velocity.z *= gameManager.PhysicsConfig.HorizontalDrag;

            if (onGround)
            {
                velocity.x *= gameManager.PhysicsConfig.GroundedHorizontalDrag;
                velocity.z *= gameManager.PhysicsConfig.GroundedHorizontalDrag;
            }
        }

        protected override void ResolveEntityReferences()
        {
            base.ResolveEntityReferences();

            if (mouseLook == null)
            {
                mouseLook = GetComponentInChildren<MouseLook>(true);
            }

            if (chunkRuntimeController == null && gameManager != null)
            {
                chunkRuntimeController = gameManager.GetComponent<ChunkRuntimeController>();
            }

            if (blockInteractionController == null)
            {
                blockInteractionController = GetComponent<BlockInteractionController>();
            }

            if (blockInteractionController == null)
            {
                blockInteractionController = gameObject.AddComponent<BlockInteractionController>();
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
                ResetEntityKinematics();
                return;
            }

            var randomX = Random.Range(0f, gameManager.WorldConfig.Width);
            var randomZ = Random.Range(0f, gameManager.WorldConfig.Height);
            var spawnHeight = gameManager.WorldConfig.Depth + 3f;
            SetEntityPosition(new Vector3(randomX, spawnHeight, randomZ));
            ResetEntityKinematics();
            chunkRuntimeController?.HandlePlayerTeleported();
        }

        public void TeleportTo(Vector3 position, bool rebuildChunks)
        {
            SetEntityPosition(position);
            ResetEntityKinematics();

            if (rebuildChunks)
            {
                chunkRuntimeController?.HandlePlayerTeleported();
            }
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
    }
}
