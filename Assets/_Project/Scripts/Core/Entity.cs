using System.Collections.Generic;
using UnityEngine;
using WorldOfVictoria.Utilities;

namespace WorldOfVictoria.Core
{
    public abstract class Entity : MonoBehaviour
    {
        [Header("Entity References")]
        [SerializeField] protected GameManager gameManager;

        private readonly List<VoxelAabb> collisionBuffer = new();

        protected Vector3 velocity;
        protected Vector3 previousPosition;
        protected bool onGround;
        protected VoxelAabb boundingBox;

        public Vector3 Velocity => velocity;
        public bool IsGrounded => onGround;
        public Vector3 PreviousPosition => previousPosition;
        public VoxelAabb BoundingBox => boundingBox;
        protected GameManager GameManager => gameManager;

        protected abstract Vector3 EntityColliderSize { get; }
        protected abstract float EntityEyeHeight { get; }

        protected virtual void Awake()
        {
            ResolveEntityReferences();
            SyncBoundingBoxFromTransform();
        }

        protected virtual void ResolveEntityReferences()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }

        protected void ResetEntityKinematics()
        {
            velocity = Vector3.zero;
            onGround = false;
        }

        public void ApplyExternalDisplacement(Vector3 displacement)
        {
            if (displacement.sqrMagnitude <= 0f)
            {
                return;
            }

            SetEntityPosition(transform.position + displacement);
        }

        protected void SyncBoundingBoxFromTransform()
        {
            SetEntityPosition(transform.position);
        }

        protected void SetEntityPosition(Vector3 position)
        {
            transform.position = position;

            var halfSize = EntityColliderSize * 0.5f;
            boundingBox = new VoxelAabb(
                new Vector3(position.x - halfSize.x, position.y - EntityEyeHeight, position.z - halfSize.z),
                new Vector3(position.x + halfSize.x, position.y - EntityEyeHeight + EntityColliderSize.y, position.z + halfSize.z));
        }

        protected void MoveWithWorldCollision(Vector3 delta)
        {
            if (gameManager == null || !gameManager.HasGeneratedWorld)
            {
                transform.position += delta;
                SyncBoundingBoxFromTransform();
                return;
            }

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
                boundingBox.Min.y + EntityEyeHeight,
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
