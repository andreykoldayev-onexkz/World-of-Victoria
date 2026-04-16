using UnityEngine;

namespace WorldOfVictoria.Player
{
    public sealed class MouseLook : MonoBehaviour
    {
        [SerializeField] private Transform yawRoot;
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float pitchClamp = 90f;

        private float pitch;

        public void Initialize(Transform yawTransform, float lookSensitivity, float lookPitchClamp)
        {
            yawRoot = yawTransform;
            sensitivity = lookSensitivity;
            pitchClamp = lookPitchClamp;
        }

        public void ApplyLook(Vector2 lookDelta)
        {
            if (yawRoot == null)
            {
                return;
            }

            yawRoot.Rotate(Vector3.up, lookDelta.x * sensitivity, Space.Self);

            pitch -= lookDelta.y * sensitivity;
            pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
