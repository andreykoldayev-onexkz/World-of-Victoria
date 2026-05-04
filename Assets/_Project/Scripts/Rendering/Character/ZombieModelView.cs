using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace WorldOfVictoria.Rendering.Character
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ZombieModelView : MonoBehaviour
    {
        [Header("Rigged Model")]
        [SerializeField] private GameObject riggedModelPrefab;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip walkClip;
        [SerializeField] private AnimationClip runClip;
        [SerializeField] private Material overrideMaterial;
        [SerializeField] private float targetModelHeight = 1.72f;
        [SerializeField] private float entityEyeHeight = 1.62f;
        [SerializeField] private Vector3 modelEulerOffset = Vector3.zero;

        [Header("Preview")]
        [SerializeField] private bool autoPreviewAnimation = true;
        [SerializeField, Range(0f, 1f)] private float previewMoveAmount = 1f;
        [SerializeField] private float previewYaw;
        [SerializeField] private float previewHeadPitch;
        [SerializeField] private float walkCycleSpeed = 5f;

        [Header("Procedural Motion")]
        [SerializeField] private float bodyBobAmplitude = 0.05f;
        [SerializeField] private float bodyRollAmplitude = 4f;
        [SerializeField] private float bodyLeanAmplitude = 10f;
        [SerializeField] private float idleBreathAmplitude = 1.5f;
        [SerializeField] private float idleSwayAmplitude = 1.5f;
        [SerializeField] private float squashStretchAmplitude = 0.02f;
        [SerializeField] private float zombieArmForwardPitch = -72f;
        [SerializeField] private float zombieForearmBend = -12f;
        [SerializeField] private float zombieArmSwingAmplitude = 7f;

        private Transform visualRoot;
        private Transform motionRoot;
        private Transform modelRoot;
        private Transform headBone;
        private Transform leftArmBone;
        private Transform rightArmBone;
        private Transform leftForeArmBone;
        private Transform rightForeArmBone;
        private Animator rigAnimator;
        private PlayableGraph animationGraph;
        private AnimationMixerPlayable animationMixer;
        private AnimationClipPlayable idlePlayable;
        private AnimationClipPlayable walkPlayable;
        private AnimationClipPlayable runPlayable;
        private bool animationGraphValid;
        private bool modelDirty = true;
        private bool hasPoseState;
        private float poseWalkPhase;
        private float poseMoveAmount;
        private float poseYawDegrees;
        private float poseHeadPitchDegrees;
        private float poseIdleTime;
        private Quaternion headBaseRotation;
        private Quaternion leftArmBaseRotation;
        private Quaternion rightArmBaseRotation;
        private Quaternion leftForeArmBaseRotation;
        private Quaternion rightForeArmBaseRotation;

        public void Configure(GameObject modelPrefab, AnimationClip idle, AnimationClip walk, AnimationClip run, Material material)
        {
            riggedModelPrefab = modelPrefab;
            idleClip = idle;
            walkClip = walk;
            runClip = run;
            overrideMaterial = material;
            modelDirty = true;
        }

        private void OnEnable()
        {
            modelDirty = true;
        }

        private void OnDisable()
        {
            visualRoot = null;
            motionRoot = null;
            modelRoot = null;
            headBone = null;
            leftArmBone = null;
            rightArmBone = null;
            leftForeArmBone = null;
            rightForeArmBone = null;
            DestroyAnimationGraph();
        }

        private void OnValidate()
        {
            modelDirty = true;
        }

        private void Update()
        {
            RebuildModelIfNeeded();

            if (!Application.isPlaying && autoPreviewAnimation)
            {
                ApplyCurrentPose();
            }
        }

        private void LateUpdate()
        {
            RebuildModelIfNeeded();

            if (!hasPoseState)
            {
                return;
            }

            ApplyHighDetailPose(poseWalkPhase, poseMoveAmount, poseYawDegrees, poseHeadPitchDegrees, poseIdleTime);
        }

        public void SetAnimationPose(float walkPhase, float moveAmount, float yawDegrees, float headPitchDegrees, float idleTime)
        {
            RebuildModelIfNeeded();
            hasPoseState = true;
            poseWalkPhase = walkPhase;
            poseMoveAmount = moveAmount;
            poseYawDegrees = yawDegrees;
            poseHeadPitchDegrees = headPitchDegrees;
            poseIdleTime = idleTime;
        }

        private void RebuildModelIfNeeded()
        {
            if (!modelDirty && visualRoot != null)
            {
                return;
            }

            CleanupGeneratedVisuals();

            if (TryBuildHighDetailModel())
            {
                modelDirty = false;
                return;
            }
        }

        private bool TryBuildHighDetailModel()
        {
            if (riggedModelPrefab == null)
            {
                return false;
            }

            visualRoot = CreatePivot("ZombieVisualRoot", transform, Vector3.zero, Quaternion.identity);
            var yawRoot = CreatePivot("YawPivot", visualRoot, Vector3.zero, Quaternion.identity);
            motionRoot = CreatePivot("MotionPivot", yawRoot, Vector3.zero, Quaternion.identity);

            var instance = Instantiate(riggedModelPrefab, motionRoot, false);
            if (instance == null)
            {
                return false;
            }

            instance.name = "ZombieGeneratedVisual";
            instance.transform.SetParent(motionRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(modelEulerOffset);
            instance.transform.localScale = Vector3.one;
            modelRoot = instance.transform;

            ConfigureImportedRenderers(instance);
            FitModelToTargetHeight(instance.transform);
            CacheRigBones(instance.transform);
            InitializeAnimationGraph(instance, idleClip, walkClip, runClip);

            return true;
        }

        private void InitializeAnimationGraph(GameObject instance, AnimationClip idleClip, AnimationClip walkClip, AnimationClip runClip)
        {
            rigAnimator = instance.GetComponentInChildren<Animator>();
            if (rigAnimator == null)
            {
                rigAnimator = instance.AddComponent<Animator>();
            }

            rigAnimator.applyRootMotion = false;
            rigAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rigAnimator.updateMode = AnimatorUpdateMode.Normal;

            var resolvedIdleClip = idleClip != null ? idleClip : walkClip ?? runClip;
            var resolvedWalkClip = walkClip ?? idleClip ?? runClip;
            var resolvedRunClip = runClip ?? walkClip ?? idleClip;

            if (resolvedIdleClip == null || resolvedWalkClip == null || resolvedRunClip == null)
            {
                return;
            }

            DestroyAnimationGraph();

            animationGraph = PlayableGraph.Create($"{name}_ZombieRigGraph");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            var output = AnimationPlayableOutput.Create(animationGraph, "Animation", rigAnimator);
            animationMixer = AnimationMixerPlayable.Create(animationGraph, 3, true);
            output.SetSourcePlayable(animationMixer);

            idlePlayable = AnimationClipPlayable.Create(animationGraph, resolvedIdleClip);
            walkPlayable = AnimationClipPlayable.Create(animationGraph, resolvedWalkClip);
            runPlayable = AnimationClipPlayable.Create(animationGraph, resolvedRunClip);

            idlePlayable.SetApplyFootIK(false);
            walkPlayable.SetApplyFootIK(true);
            runPlayable.SetApplyFootIK(true);

            animationGraph.Connect(idlePlayable, 0, animationMixer, 0);
            animationGraph.Connect(walkPlayable, 0, animationMixer, 1);
            animationGraph.Connect(runPlayable, 0, animationMixer, 2);

            animationMixer.SetInputWeight(0, 1f);
            animationMixer.SetInputWeight(1, 0f);
            animationMixer.SetInputWeight(2, 0f);

            animationGraph.Play();
            animationGraphValid = true;
        }

        private void DestroyAnimationGraph()
        {
            if (!animationGraphValid)
            {
                return;
            }

            animationGraph.Destroy();
            animationGraphValid = false;
        }

        private void ConfigureImportedRenderers(GameObject instance)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

                if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    skinnedMeshRenderer.updateWhenOffscreen = true;
                }

                if (overrideMaterial != null)
                {
                    var replacement = new Material[renderer.sharedMaterials.Length];
                    for (var i = 0; i < replacement.Length; i++)
                    {
                        replacement[i] = overrideMaterial;
                    }
                    renderer.sharedMaterials = replacement;
                }
            }
        }

        private void FitModelToTargetHeight(Transform instanceRoot)
        {
            var renderers = instanceRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            var bounds = GetCombinedBounds(renderers);
            if (bounds.size.y <= 0.0001f)
            {
                return;
            }

            var scaleFactor = targetModelHeight / bounds.size.y;
            instanceRoot.localScale = Vector3.one * scaleFactor;

            renderers = instanceRoot.GetComponentsInChildren<Renderer>(true);
            bounds = GetCombinedBounds(renderers);

            var centerLocal = motionRoot.InverseTransformPoint(bounds.center);
            var feetWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var feetLocal = motionRoot.InverseTransformPoint(feetWorld);

            visualRoot.localPosition = new Vector3(0f, -entityEyeHeight, 0f);
            instanceRoot.localPosition = new Vector3(
                -centerLocal.x,
                -feetLocal.y,
                -centerLocal.z);
        }

        private static Bounds GetCombinedBounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void CleanupGeneratedVisuals()
        {
            visualRoot = null;
            motionRoot = null;
            modelRoot = null;
            headBone = null;
            leftArmBone = null;
            rightArmBone = null;
            leftForeArmBone = null;
            rightForeArmBone = null;
            rigAnimator = null;
            DestroyAnimationGraph();

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
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
            SetAnimationPose(time * walkCycleSpeed, previewMoveAmount, previewYaw, previewHeadPitch, time);
        }

        private void ApplyHighDetailPose(float walkPhase, float moveAmount, float yawDegrees, float headPitchDegrees, float idleTime)
        {
            if (visualRoot == null || motionRoot == null || modelRoot == null)
            {
                return;
            }

            var move = Mathf.Clamp01(moveAmount);
            var locomotionWeight = Mathf.SmoothStep(0f, 1f, move);
            var bob = Mathf.Sin(walkPhase) * bodyBobAmplitude * locomotionWeight + Mathf.Sin(idleTime * 1.65f) * 0.008f;
            var roll = Mathf.Sin(walkPhase) * bodyRollAmplitude * locomotionWeight + Mathf.Sin(idleTime * 0.8f) * idleSwayAmplitude * (1f - locomotionWeight * 0.65f);
            var lean = -bodyLeanAmplitude * locomotionWeight + Mathf.Sin(walkPhase * 2f) * (bodyLeanAmplitude * 0.12f) * locomotionWeight + Mathf.Sin(idleTime * 1.1f) * idleBreathAmplitude * (1f - locomotionWeight * 0.5f);
            var stretch = Mathf.Sin(walkPhase * 2f) * squashStretchAmplitude * locomotionWeight;

            visualRoot.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            motionRoot.localPosition = new Vector3(0f, bob, 0f);
            motionRoot.localRotation = Quaternion.Euler(lean, 0f, -roll);
            motionRoot.localScale = new Vector3(1f - stretch * 0.4f, 1f + stretch, 1f - stretch * 0.4f);

            modelRoot.localRotation = Quaternion.Euler(modelEulerOffset);
            UpdateAnimationBlend(locomotionWeight);
            ApplyZombieUpperBodyPose(walkPhase, locomotionWeight, headPitchDegrees);
        }

        private void UpdateAnimationBlend(float locomotionWeight)
        {
            if (!animationGraphValid)
            {
                return;
            }

            var runWeight = 0f;
            var walkWeight = locomotionWeight;
            var idleWeight = Mathf.Clamp01(1f - locomotionWeight);
            var total = idleWeight + walkWeight + runWeight;

            if (total > 0.0001f)
            {
                idleWeight /= total;
                walkWeight /= total;
                runWeight /= total;
            }

            animationMixer.SetInputWeight(0, idleWeight);
            animationMixer.SetInputWeight(1, walkWeight);
            animationMixer.SetInputWeight(2, runWeight);

            idlePlayable.SetSpeed(1f);
            walkPlayable.SetSpeed(Mathf.Lerp(0.62f, 0.82f, locomotionWeight));
            runPlayable.SetSpeed(0f);
        }

        private void CacheRigBones(Transform instanceRoot)
        {
            headBone = FindChildRecursive(instanceRoot, "Head");
            leftArmBone = FindChildRecursive(instanceRoot, "LeftArm");
            rightArmBone = FindChildRecursive(instanceRoot, "RightArm");
            leftForeArmBone = FindChildRecursive(instanceRoot, "LeftForeArm");
            rightForeArmBone = FindChildRecursive(instanceRoot, "RightForeArm");

            if (headBone != null) headBaseRotation = headBone.localRotation;
            if (leftArmBone != null) leftArmBaseRotation = leftArmBone.localRotation;
            if (rightArmBone != null) rightArmBaseRotation = rightArmBone.localRotation;
            if (leftForeArmBone != null) leftForeArmBaseRotation = leftForeArmBone.localRotation;
            if (rightForeArmBone != null) rightForeArmBaseRotation = rightForeArmBone.localRotation;
        }

        private void ApplyZombieUpperBodyPose(float walkPhase, float locomotionWeight, float headPitchDegrees)
        {
            if (headBone != null)
            {
                headBone.localRotation = headBaseRotation * Quaternion.Euler(headPitchDegrees * 0.55f, 0f, 0f);
            }

            var swing = Mathf.Sin(walkPhase) * zombieArmSwingAmplitude * Mathf.Lerp(0.35f, 1f, locomotionWeight);
            var armPitch = zombieArmForwardPitch;

            if (leftArmBone != null)
            {
                leftArmBone.localRotation = leftArmBaseRotation * Quaternion.Euler(armPitch + swing, 0f, 0f);
            }

            if (rightArmBone != null)
            {
                rightArmBone.localRotation = rightArmBaseRotation * Quaternion.Euler(armPitch - swing, 0f, 0f);
            }

            if (leftForeArmBone != null)
            {
                leftForeArmBone.localRotation = leftForeArmBaseRotation * Quaternion.Euler(zombieForearmBend, 0f, 0f);
            }

            if (rightForeArmBone != null)
            {
                rightForeArmBone.localRotation = rightForeArmBaseRotation * Quaternion.Euler(zombieForearmBend, 0f, 0f);
            }
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var match = FindChildRecursive(root.GetChild(i), childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform CreatePivot(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            pivot.localRotation = localRotation;
            pivot.localScale = Vector3.one;
            return pivot;
        }
    }
}
