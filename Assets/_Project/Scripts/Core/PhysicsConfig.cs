using UnityEngine;

namespace WorldOfVictoria.Core
{
    [CreateAssetMenu(fileName = "PhysicsConfig", menuName = "World of Victoria/Config/Physics Config")]
    public sealed class PhysicsConfig : ScriptableObject
    {
        [Header("Player Collider")]
        [SerializeField] private Vector3 playerColliderSize = new(0.6f, 1.8f, 0.6f);
        [SerializeField] private float eyeHeight = 1.62f;

        [Header("Movement")]
        [SerializeField] private float groundAcceleration = 0.02f;
        [SerializeField] private float airAcceleration = 0.005f;
        [SerializeField] private float gravityPerTick = 0.005f;
        [SerializeField] private float jumpVelocity = 0.12f;
        [SerializeField] private float horizontalDrag = 0.91f;
        [SerializeField] private float groundedHorizontalDrag = 0.8f;
        [SerializeField] private float verticalDrag = 0.98f;

        public Vector3 PlayerColliderSize => playerColliderSize;
        public float EyeHeight => eyeHeight;
        public float GroundAcceleration => groundAcceleration;
        public float AirAcceleration => airAcceleration;
        public float GravityPerTick => gravityPerTick;
        public float JumpVelocity => jumpVelocity;
        public float HorizontalDrag => horizontalDrag;
        public float GroundedHorizontalDrag => groundedHorizontalDrag;
        public float VerticalDrag => verticalDrag;

        private void OnValidate()
        {
            playerColliderSize.x = Mathf.Max(0.01f, playerColliderSize.x);
            playerColliderSize.y = Mathf.Max(0.01f, playerColliderSize.y);
            playerColliderSize.z = Mathf.Max(0.01f, playerColliderSize.z);
            eyeHeight = Mathf.Max(0f, eyeHeight);
        }
    }
}
