using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldOfVictoria.Rendering.Character
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ZombieModelView : MonoBehaviour
    {
        private const string HighDetailModelAssetPath = "Assets/_Project/Prefabs/Characters/RealisticSteveRigged.glb";
        private const string WalkingAnimationAssetPath = "Assets/_Project/Prefabs/Characters/RealisticSteveRigged_walking.glb";
        private const string RunningAnimationAssetPath = "Assets/_Project/Prefabs/Characters/RealisticSteveRigged_running.glb";
        private const string HighDetailFallbackTexturePath = "Assets/_Project/Textures/Characters/RealisticSteve/texture_0.png";
        private static readonly Dictionary<int, Material> UpgradedMaterials = new();
        private static Material sharedHighDetailMaterial;

        [Header("Fallback Cube Model")]
        [SerializeField] private Material characterMaterial;

        [Header("High Detail Model")]
        [SerializeField] private bool useHighDetailModel = true;
        [SerializeField] private float targetModelHeight = 1.72f;
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

        private ZombieModel zombieModel;
        private Transform visualRoot;
        private Transform motionRoot;
        private Transform modelRoot;
        private Animator rigAnimator;
        private PlayableGraph animationGraph;
        private AnimationMixerPlayable animationMixer;
        private AnimationClipPlayable idlePlayable;
        private AnimationClipPlayable walkPlayable;
        private AnimationClipPlayable runPlayable;
        private bool animationGraphValid;
        private bool usingHighDetailModel;
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

#if UNITY_EDITOR
        public void ForceRefreshInEditor()
        {
            UpgradedMaterials.Clear();
            sharedHighDetailMaterial = null;
            modelDirty = true;
            RebuildModelIfNeeded();
        }
#endif

        private void OnEnable()
        {
            modelDirty = true;
        }

        private void OnDisable()
        {
            zombieModel = null;
            visualRoot = null;
            motionRoot = null;
            modelRoot = null;
            DestroyAnimationGraph();
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

            if (usingHighDetailModel)
            {
                ApplyHighDetailPose(walkPhase, moveAmount, yawDegrees, headPitchDegrees, idleTime);
                return;
            }

            zombieModel?.ApplyPose(walkPhase, moveAmount, idleTime, yawDegrees, headPitchDegrees);
        }

        private void RebuildModelIfNeeded()
        {
            if (!modelDirty && (usingHighDetailModel ? visualRoot != null : zombieModel != null))
            {
                return;
            }

            CleanupGeneratedVisuals();

            if (useHighDetailModel && TryBuildHighDetailModel())
            {
                modelDirty = false;
                return;
            }

            zombieModel = new ZombieModel(transform, characterMaterial);
            zombieModel.SetMaterial(characterMaterial);
            usingHighDetailModel = false;
            modelDirty = false;
        }

        private bool TryBuildHighDetailModel()
        {
#if UNITY_EDITOR
            if (!TryLoadRiggedAssets(out var modelAsset, out var idleClip, out var walkClip, out var runClip))
            {
                return false;
            }

            visualRoot = CreatePivot("ZombieVisualRoot", transform, Vector3.zero, Quaternion.identity);
            var yawRoot = CreatePivot("YawPivot", visualRoot, Vector3.zero, Quaternion.identity);
            motionRoot = CreatePivot("MotionPivot", yawRoot, Vector3.zero, Quaternion.identity);

            var instance = Instantiate(modelAsset, motionRoot, false);
            if (instance == null)
            {
                return false;
            }

            instance.name = "RealisticSteveVisual";
            instance.transform.SetParent(motionRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(modelEulerOffset);
            instance.transform.localScale = Vector3.one;
            modelRoot = instance.transform;

            ConfigureImportedRenderers(instance);
            FitModelToTargetHeight(instance.transform);
            InitializeAnimationGraph(instance, idleClip, walkClip, runClip);

            usingHighDetailModel = true;
            zombieModel = null;
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private static bool TryLoadRiggedAssets(out GameObject modelAsset, out AnimationClip idleClip, out AnimationClip walkClip, out AnimationClip runClip)
        {
            modelAsset = LoadModelAsset(HighDetailModelAssetPath);
            idleClip = LoadFirstAnimationClip(HighDetailModelAssetPath);
            walkClip = LoadFirstAnimationClip(WalkingAnimationAssetPath);
            runClip = LoadFirstAnimationClip(RunningAnimationAssetPath);

            return modelAsset != null && (idleClip != null || walkClip != null || runClip != null);
        }

        private static GameObject LoadModelAsset(string assetPath)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelAsset != null)
            {
                return modelAsset;
            }

            var nestedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < nestedAssets.Length; i++)
            {
                if (nestedAssets[i] is GameObject nestedGameObject)
                {
                    return nestedGameObject;
                }
            }

            return null;
        }

        private static AnimationClip LoadFirstAnimationClip(string assetPath)
        {
            var directClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (directClip != null)
            {
                return directClip;
            }

            var nestedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < nestedAssets.Length; i++)
            {
                if (nestedAssets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return null;
        }
#endif

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
            var resolvedHighDetailMaterial = GetOrCreateHighDetailMaterial();
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

                if (resolvedHighDetailMaterial != null)
                {
                    var replacement = new Material[renderer.sharedMaterials.Length];
                    for (var i = 0; i < replacement.Length; i++)
                    {
                        replacement[i] = resolvedHighDetailMaterial;
                    }
                    renderer.sharedMaterials = replacement;
                    continue;
                }

                var materials = renderer.sharedMaterials;
                var updated = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    var upgraded = UpgradeMaterialForUrp(materials[i]);
                    if (upgraded == null || upgraded == materials[i])
                    {
                        continue;
                    }

                    materials[i] = upgraded;
                    updated = true;
                }

                if (updated)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private Material GetOrCreateHighDetailMaterial()
        {
#if UNITY_EDITOR
            if (sharedHighDetailMaterial != null)
            {
                return sharedHighDetailMaterial;
            }

            AssetDatabase.ImportAsset(HighDetailFallbackTexturePath, ImportAssetOptions.ForceUpdate);
            var fallbackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HighDetailFallbackTexturePath);
            if (fallbackTexture == null)
            {
                return null;
            }

            sharedHighDetailMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = "RealisticSteve_RuntimeURP"
            };
            sharedHighDetailMaterial.SetTexture("_BaseMap", fallbackTexture);
            sharedHighDetailMaterial.SetColor("_BaseColor", Color.white);
            ConfigureUrpLitMaterial(sharedHighDetailMaterial);
            return sharedHighDetailMaterial;
#else
            return null;
#endif
        }

        private Material UpgradeMaterialForUrp(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            if (sourceMaterial.shader != null && sourceMaterial.shader.name == "Universal Render Pipeline/Lit")
            {
                ApplyFallbackBaseMapIfNeeded(sourceMaterial);
                ConfigureUrpLitMaterial(sourceMaterial);
                return sourceMaterial;
            }

            var instanceId = sourceMaterial.GetInstanceID();
            if (UpgradedMaterials.TryGetValue(instanceId, out var cachedMaterial) && cachedMaterial != null)
            {
                return cachedMaterial;
            }

            var upgradedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = $"{sourceMaterial.name}_URP"
            };

            if (sourceMaterial.HasProperty("_BaseMap"))
            {
                upgradedMaterial.SetTexture("_BaseMap", sourceMaterial.GetTexture("_BaseMap"));
            }
            else if (sourceMaterial.HasProperty("_MainTex"))
            {
                upgradedMaterial.SetTexture("_BaseMap", sourceMaterial.GetTexture("_MainTex"));
            }

            if (sourceMaterial.HasProperty("_BaseColor"))
            {
                upgradedMaterial.SetColor("_BaseColor", sourceMaterial.GetColor("_BaseColor"));
            }
            else if (sourceMaterial.HasProperty("_Color"))
            {
                upgradedMaterial.SetColor("_BaseColor", sourceMaterial.GetColor("_Color"));
            }
            else
            {
                upgradedMaterial.SetColor("_BaseColor", Color.white);
            }

            ApplyFallbackBaseMapIfNeeded(upgradedMaterial);

            if (sourceMaterial.HasProperty("_BumpMap"))
            {
                upgradedMaterial.SetTexture("_BumpMap", sourceMaterial.GetTexture("_BumpMap"));
                upgradedMaterial.EnableKeyword("_NORMALMAP");
            }

            ConfigureUrpLitMaterial(upgradedMaterial);
            UpgradedMaterials[instanceId] = upgradedMaterial;
            return upgradedMaterial;
        }

        private void ApplyFallbackBaseMapIfNeeded(Material material)
        {
#if UNITY_EDITOR
            if (material == null || material.GetTexture("_BaseMap") != null)
            {
                return;
            }

            AssetDatabase.ImportAsset(HighDetailFallbackTexturePath, ImportAssetOptions.ForceUpdate);
            var fallbackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HighDetailFallbackTexturePath);
            if (fallbackTexture != null)
            {
                material.SetTexture("_BaseMap", fallbackTexture);
            }
#endif
        }

        private void ConfigureUrpLitMaterial(Material material)
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_Cull", 2f);
            material.SetFloat("_Metallic", 0.02f);
            material.SetFloat("_Smoothness", 0.18f);
            material.SetFloat("_OcclusionStrength", 1f);
            material.enableInstancing = true;
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
            zombieModel = null;
            visualRoot = null;
            motionRoot = null;
            modelRoot = null;
            rigAnimator = null;
            usingHighDetailModel = false;
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

            modelRoot.localRotation = Quaternion.Euler(modelEulerOffset + new Vector3(headPitchDegrees * 0.15f, 0f, 0f));
            UpdateAnimationBlend(locomotionWeight);
        }

        private void UpdateAnimationBlend(float locomotionWeight)
        {
            if (!animationGraphValid)
            {
                return;
            }

            var runWeight = Mathf.Clamp01((locomotionWeight - 0.68f) / 0.32f);
            var walkWeight = locomotionWeight * (1f - runWeight);
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
            walkPlayable.SetSpeed(Mathf.Lerp(0.85f, 1.05f, locomotionWeight));
            runPlayable.SetSpeed(Mathf.Lerp(0.9f, 1.08f, locomotionWeight));
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
