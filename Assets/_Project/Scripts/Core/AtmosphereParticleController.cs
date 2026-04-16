using System;
using UnityEngine;
using WorldOfVictoria.Player;

namespace WorldOfVictoria.Core
{
    public sealed class AtmosphereParticleController : MonoBehaviour
    {
        [Serializable]
        private struct DustQualityProfile
        {
            public string qualityName;
            public float rateOverTime;
            public int maxParticles;
            public Vector3 shapeScale;
            public Vector3 shapeOffset;
            public bool enabled;
        }

        [Serializable]
        private struct AdvancedQualityProfile
        {
            public string qualityName;
            public bool enableFootstepDust;
            public int footstepBurstMin;
            public int footstepBurstMax;
            public float footstepStrideDistance;
            public bool enableBreathParticles;
            public float breathRateOverTime;
            public int breathMaxParticles;
            public bool enableCaveFog;
            public float caveFogRateOverTime;
            public int caveFogMaxParticles;
            public Vector3 caveFogShapeScale;
            public Vector3 caveFogShapeOffset;
        }

        [SerializeField] private ParticleSystem dustMotes;
        [SerializeField] private Transform reactiveParticlesRoot;
        [SerializeField] private Transform footstepDustHook;
        [SerializeField] private Transform breathParticlesHook;
        [SerializeField] private ParticleSystem caveFog;
        [SerializeField] private ParticleSystem footstepDust;
        [SerializeField] private ParticleSystem breathParticles;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private DustQualityProfile[] dustProfiles =
        {
            new DustQualityProfile
            {
                qualityName = "Ultra",
                rateOverTime = 28f,
                maxParticles = 220,
                shapeScale = new Vector3(10f, 4.5f, 10f),
                shapeOffset = new Vector3(0f, 0f, 2f),
                enabled = true
            },
            new DustQualityProfile
            {
                qualityName = "High",
                rateOverTime = 22f,
                maxParticles = 180,
                shapeScale = new Vector3(9f, 4f, 9f),
                shapeOffset = new Vector3(0f, 0f, 2f),
                enabled = true
            },
            new DustQualityProfile
            {
                qualityName = "Medium",
                rateOverTime = 14f,
                maxParticles = 120,
                shapeScale = new Vector3(7f, 3.2f, 7f),
                shapeOffset = new Vector3(0f, 0f, 1.5f),
                enabled = true
            },
            new DustQualityProfile
            {
                qualityName = "Low",
                rateOverTime = 7f,
                maxParticles = 60,
                shapeScale = new Vector3(5f, 2.4f, 5f),
                shapeOffset = new Vector3(0f, 0f, 1f),
                enabled = true
            }
        };
        [SerializeField] private AdvancedQualityProfile[] advancedProfiles =
        {
            new AdvancedQualityProfile
            {
                qualityName = "Ultra",
                enableFootstepDust = true,
                footstepBurstMin = 6,
                footstepBurstMax = 9,
                footstepStrideDistance = 0.78f,
                enableBreathParticles = true,
                breathRateOverTime = 4.2f,
                breathMaxParticles = 24,
                enableCaveFog = true,
                caveFogRateOverTime = 6.5f,
                caveFogMaxParticles = 44,
                caveFogShapeScale = new Vector3(5.4f, 1.8f, 5.4f),
                caveFogShapeOffset = new Vector3(0f, -0.6f, 0f)
            },
            new AdvancedQualityProfile
            {
                qualityName = "High",
                enableFootstepDust = true,
                footstepBurstMin = 4,
                footstepBurstMax = 7,
                footstepStrideDistance = 0.82f,
                enableBreathParticles = true,
                breathRateOverTime = 3.2f,
                breathMaxParticles = 18,
                enableCaveFog = true,
                caveFogRateOverTime = 4.5f,
                caveFogMaxParticles = 34,
                caveFogShapeScale = new Vector3(5f, 1.7f, 5f),
                caveFogShapeOffset = new Vector3(0f, -0.55f, 0f)
            },
            new AdvancedQualityProfile
            {
                qualityName = "Medium",
                enableFootstepDust = true,
                footstepBurstMin = 4,
                footstepBurstMax = 7,
                footstepStrideDistance = 0.82f,
                enableBreathParticles = false,
                breathRateOverTime = 0f,
                breathMaxParticles = 0,
                enableCaveFog = false,
                caveFogRateOverTime = 0f,
                caveFogMaxParticles = 0,
                caveFogShapeScale = new Vector3(4.5f, 1.6f, 4.5f),
                caveFogShapeOffset = new Vector3(0f, -0.6f, 0f)
            },
            new AdvancedQualityProfile
            {
                qualityName = "Low",
                enableFootstepDust = false,
                footstepBurstMin = 0,
                footstepBurstMax = 0,
                footstepStrideDistance = 1f,
                enableBreathParticles = false,
                breathRateOverTime = 0f,
                breathMaxParticles = 0,
                enableCaveFog = false,
                caveFogRateOverTime = 0f,
                caveFogMaxParticles = 0,
                caveFogShapeScale = new Vector3(4f, 1.5f, 4f),
                caveFogShapeOffset = new Vector3(0f, -0.5f, 0f)
            }
        };

        private AdvancedQualityProfile currentAdvancedProfile;
        private float distanceSinceLastFootstep;

        public ParticleSystem DustMotes => dustMotes;
        public Transform ReactiveParticlesRoot => reactiveParticlesRoot;
        public Transform FootstepDustHook => footstepDustHook;
        public Transform BreathParticlesHook => breathParticlesHook;
        public ParticleSystem CaveFog => caveFog;
        public ParticleSystem FootstepDust => footstepDust;
        public ParticleSystem BreathParticles => breathParticles;

        private void Awake()
        {
            ResolveReferences();
            ApplyQualityProfile(QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]);
        }

        private void Update()
        {
            if (playerController == null || gameManager == null || !gameManager.HasGeneratedWorld)
            {
                return;
            }

            UpdateAdvancedEffects();
        }

        public void ConfigureDustMotes(ParticleSystem particleSystem)
        {
            dustMotes = particleSystem;
        }

        public void ConfigureHooks(Transform reactiveRoot, Transform footstepHook, Transform breathHook)
        {
            reactiveParticlesRoot = reactiveRoot;
            footstepDustHook = footstepHook;
            breathParticlesHook = breathHook;
        }

        public void ConfigureAdvancedEffects(ParticleSystem caveFogSystem, ParticleSystem footstepDustSystem, ParticleSystem breathParticleSystem)
        {
            caveFog = caveFogSystem;
            footstepDust = footstepDustSystem;
            breathParticles = breathParticleSystem;
        }

        public void ConfigureRuntimeDependencies(PlayerController controller, GameManager manager)
        {
            playerController = controller;
            gameManager = manager;
        }

        public void ApplyQualityProfile(string qualityName)
        {
            if (string.IsNullOrWhiteSpace(qualityName))
            {
                return;
            }

            var profile = ResolveDustProfile(qualityName);
            currentAdvancedProfile = ResolveAdvancedProfile(qualityName);
            ApplyDustProfile(profile);
            ApplyAdvancedProfile(currentAdvancedProfile);

            if (!Application.isPlaying)
            {
                RestartParticlePreview(dustMotes, profile.enabled);
                RestartParticlePreview(caveFog, currentAdvancedProfile.enableCaveFog);
                RestartParticlePreview(footstepDust, false);
                RestartParticlePreview(breathParticles, currentAdvancedProfile.enableBreathParticles);
            }
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }

        private void ApplyDustProfile(DustQualityProfile profile)
        {
            if (dustMotes == null)
            {
                return;
            }

            var main = dustMotes.main;
            main.maxParticles = profile.maxParticles;

            var emission = dustMotes.emission;
            emission.enabled = profile.enabled;
            emission.rateOverTime = profile.rateOverTime;

            var shape = dustMotes.shape;
            shape.scale = profile.shapeScale;
            shape.position = profile.shapeOffset;
        }

        private void ApplyAdvancedProfile(AdvancedQualityProfile profile)
        {
            if (caveFog != null)
            {
                var main = caveFog.main;
                main.maxParticles = profile.caveFogMaxParticles;
                var shape = caveFog.shape;
                shape.scale = profile.caveFogShapeScale;
                shape.position = profile.caveFogShapeOffset;

                var emission = caveFog.emission;
                emission.enabled = profile.enableCaveFog;
                emission.rateOverTime = profile.caveFogRateOverTime;
            }

            if (breathParticles != null)
            {
                var main = breathParticles.main;
                main.maxParticles = profile.breathMaxParticles;
                var emission = breathParticles.emission;
                emission.enabled = profile.enableBreathParticles;
                emission.rateOverTime = profile.breathRateOverTime;
            }

            distanceSinceLastFootstep = 0f;
        }

        private void UpdateAdvancedEffects()
        {
            var position = playerController.transform.position;
            var brightness = SampleBrightness(position + Vector3.up * 0.4f);
            var horizontalSpeed = new Vector2(playerController.Velocity.x, playerController.Velocity.z).magnitude;
            var inDarkCave = brightness <= 0.10f;
            var inCaveAtmosphere = brightness <= 0.16f;

            UpdateCaveFog(inCaveAtmosphere, brightness);
            UpdateBreathParticles(inDarkCave);
            UpdateFootstepDust(horizontalSpeed, brightness);
        }

        private void UpdateCaveFog(bool inCaveAtmosphere, float brightness)
        {
            if (caveFog == null)
            {
                return;
            }

            var shouldEmit = currentAdvancedProfile.enableCaveFog && inCaveAtmosphere;
            var opacity = Mathf.InverseLerp(0.16f, 0.02f, brightness);
            var main = caveFog.main;
            var startColor = main.startColor;
            startColor.colorMax = new Color(0.58f, 0.63f, 0.70f, Mathf.Lerp(0.012f, 0.045f, opacity));
            startColor.colorMin = new Color(0.46f, 0.52f, 0.60f, Mathf.Lerp(0.004f, 0.018f, opacity));
            main.startColor = startColor;

            var emission = caveFog.emission;
            emission.enabled = shouldEmit;
            emission.rateOverTime = shouldEmit
                ? currentAdvancedProfile.caveFogRateOverTime * Mathf.Lerp(0.25f, 0.85f, opacity)
                : 0f;

            if (shouldEmit && !caveFog.isPlaying)
            {
                caveFog.Play();
            }
            else if (!shouldEmit && caveFog.isPlaying)
            {
                caveFog.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void UpdateBreathParticles(bool inDarkCave)
        {
            if (breathParticles == null)
            {
                return;
            }

            var shouldEmit = currentAdvancedProfile.enableBreathParticles && inDarkCave;
            var emission = breathParticles.emission;
            emission.enabled = shouldEmit;
            emission.rateOverTime = shouldEmit ? currentAdvancedProfile.breathRateOverTime : 0f;

            if (shouldEmit && !breathParticles.isPlaying)
            {
                breathParticles.Play();
            }
            else if (!shouldEmit && breathParticles.isPlaying)
            {
                breathParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void UpdateFootstepDust(float horizontalSpeed, float brightness)
        {
            if (footstepDust == null || !currentAdvancedProfile.enableFootstepDust)
            {
                return;
            }

            if (!playerController.IsGrounded || horizontalSpeed < 0.35f)
            {
                distanceSinceLastFootstep = 0f;
                return;
            }

            distanceSinceLastFootstep += horizontalSpeed * Time.deltaTime;
            if (distanceSinceLastFootstep < currentAdvancedProfile.footstepStrideDistance)
            {
                return;
            }

            distanceSinceLastFootstep = 0f;
            var emissionFactor = Mathf.Clamp01(Mathf.Lerp(0.55f, 1f, brightness));
            var burstCount = Mathf.RoundToInt(UnityEngine.Random.Range(
                currentAdvancedProfile.footstepBurstMin,
                currentAdvancedProfile.footstepBurstMax + 1) * emissionFactor);

            if (burstCount > 0)
            {
                footstepDust.Emit(burstCount);
            }
        }

        private float SampleBrightness(Vector3 worldPosition)
        {
            if (gameManager == null || gameManager.RuntimeWorldData == null)
            {
                return 1f;
            }

            var x = Mathf.FloorToInt(worldPosition.x);
            var y = Mathf.FloorToInt(worldPosition.y);
            var z = Mathf.FloorToInt(worldPosition.z);
            return gameManager.RuntimeWorldData.GetBrightness(x, y, z);
        }

        private void RestartParticlePreview(ParticleSystem particleSystem, bool shouldPlay)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (shouldPlay)
            {
                particleSystem.Play();
            }
        }

        private DustQualityProfile ResolveDustProfile(string qualityName)
        {
            for (var i = 0; i < dustProfiles.Length; i++)
            {
                if (string.Equals(dustProfiles[i].qualityName, qualityName, StringComparison.OrdinalIgnoreCase))
                {
                    return dustProfiles[i];
                }
            }

            return dustProfiles.Length > 0 ? dustProfiles[0] : default;
        }

        private AdvancedQualityProfile ResolveAdvancedProfile(string qualityName)
        {
            for (var i = 0; i < advancedProfiles.Length; i++)
            {
                if (string.Equals(advancedProfiles[i].qualityName, qualityName, StringComparison.OrdinalIgnoreCase))
                {
                    return advancedProfiles[i];
                }
            }

            return advancedProfiles.Length > 0 ? advancedProfiles[0] : default;
        }
    }
}
