using UnityEngine;

namespace WorldOfVictoria.Rendering.Character
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ZombieModelView : MonoBehaviour
    {
        [SerializeField] private Material characterMaterial;
        [SerializeField] private bool autoPreviewAnimation = true;
        [SerializeField, Range(0f, 1f)] private float previewMoveAmount = 1f;
        [SerializeField] private float previewYaw;
        [SerializeField] private float previewHeadPitch;
        [SerializeField] private float walkCycleSpeed = 5f;

        private ZombieModel zombieModel;
        private bool modelDirty = true;

        public Material CharacterMaterial
        {
            get => characterMaterial;
            set
            {
                characterMaterial = value;
                zombieModel?.SetMaterial(characterMaterial);
            }
        }

        private void OnEnable()
        {
            modelDirty = true;
        }

        private void OnDisable()
        {
            zombieModel = null;
        }

        private void OnValidate()
        {
            modelDirty = true;
        }

        private void Update()
        {
            RebuildModelIfNeeded();

            if (autoPreviewAnimation)
            {
                ApplyCurrentPose();
            }
        }

        public void SetAnimationPose(float walkPhase, float moveAmount, float yawDegrees, float headPitchDegrees, float idleTime)
        {
            RebuildModelIfNeeded();
            zombieModel?.ApplyPose(walkPhase, moveAmount, idleTime, yawDegrees, headPitchDegrees);
        }

        private void RebuildModelIfNeeded()
        {
            if (!modelDirty && zombieModel != null)
            {
                return;
            }

            CleanupVisualRoots();
            zombieModel = new ZombieModel(transform, characterMaterial);
            zombieModel.SetMaterial(characterMaterial);
            modelDirty = false;
        }

        private void CleanupVisualRoots()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != "ZombieVisualRoot")
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void ApplyCurrentPose()
        {
            var time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            zombieModel?.ApplyPose(time * walkCycleSpeed, previewMoveAmount, time, previewYaw, previewHeadPitch);
        }

    }
}
