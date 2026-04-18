using UnityEngine;

namespace WorldOfVictoria.Core
{
    [ExecuteAlways]
    public sealed class VolumetricLightingController : MonoBehaviour
    {
        private static readonly int EnabledId = Shader.PropertyToID("_WovVolumetricEnabled");
        private static readonly int FogColorId = Shader.PropertyToID("_WovVolumetricFogColor");
        private static readonly int DensityId = Shader.PropertyToID("_WovVolumetricDensity");
        private static readonly int HeightFalloffId = Shader.PropertyToID("_WovVolumetricHeightFalloff");
        private static readonly int SurfaceHeightId = Shader.PropertyToID("_WovVolumetricSurfaceHeight");
        private static readonly int UndergroundBoostId = Shader.PropertyToID("_WovVolumetricUndergroundBoost");
        private static readonly int MaxDistanceId = Shader.PropertyToID("_WovVolumetricMaxDistance");
        private static readonly int StepCountId = Shader.PropertyToID("_WovVolumetricStepCount");
        private static readonly int ScatteringId = Shader.PropertyToID("_WovVolumetricScattering");
        private static readonly int ExtinctionId = Shader.PropertyToID("_WovVolumetricExtinction");
        private static readonly int AnisotropyId = Shader.PropertyToID("_WovVolumetricAnisotropy");
        private static readonly int ShaftIntensityId = Shader.PropertyToID("_WovVolumetricShaftIntensity");
        private static readonly int AmbientBoostId = Shader.PropertyToID("_WovVolumetricAmbientBoost");

        [SerializeField] private GameManager gameManager;
        [SerializeField] private bool effectEnabled = true;
        [SerializeField] private Color fogColor = new(0.82f, 0.88f, 0.96f, 1f);
        [SerializeField, Min(0f)] private float density = 0.028f;
        [SerializeField, Min(0f)] private float heightFalloff = 0.028f;
        [SerializeField, Min(0f)] private float undergroundBoost = 1.9f;
        [SerializeField, Min(1f)] private float maxDistance = 72f;
        [SerializeField, Range(6, 32)] private int stepCount = 18;
        [SerializeField, Min(0f)] private float scattering = 1.1f;
        [SerializeField, Min(0f)] private float extinction = 1.7f;
        [SerializeField, Range(-0.95f, 0.95f)] private float anisotropy = 0.42f;
        [SerializeField, Range(0f, 2f)] private float shaftIntensity = 1.05f;
        [SerializeField, Range(0f, 1f)] private float ambientBoost = 0.18f;

        private float qualityDensityMultiplier = 1f;
        private float qualityDistanceMultiplier = 1f;
        private float qualityShaftMultiplier = 1f;
        private int qualityStepOverride = -1;

        public void ApplyQualityProfile(float densityMultiplier, float distanceMultiplier, int stepOverride, float shaftMultiplier)
        {
            qualityDensityMultiplier = Mathf.Max(0f, densityMultiplier);
            qualityDistanceMultiplier = Mathf.Max(0.25f, distanceMultiplier);
            qualityStepOverride = Mathf.Clamp(stepOverride, 4, 64);
            qualityShaftMultiplier = Mathf.Max(0f, shaftMultiplier);
            ApplyGlobals();
        }

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }

            ApplyGlobals();
        }

        private void OnEnable()
        {
            ApplyGlobals();
        }

        private void LateUpdate()
        {
            ApplyGlobals();
        }

        private void OnValidate()
        {
            ApplyGlobals();
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(EnabledId, 0f);
        }

        private void ApplyGlobals()
        {
            var config = gameManager != null ? gameManager.WorldConfig : null;
            var surfaceHeight = config != null ? config.Depth - 1f : 63f;
            var color = config != null ? Color.Lerp(config.FogColor, fogColor, 0.65f) : fogColor;

            Shader.SetGlobalFloat(EnabledId, effectEnabled ? 1f : 0f);
            Shader.SetGlobalColor(FogColorId, color);
            Shader.SetGlobalFloat(DensityId, density * qualityDensityMultiplier);
            Shader.SetGlobalFloat(HeightFalloffId, heightFalloff);
            Shader.SetGlobalFloat(SurfaceHeightId, surfaceHeight);
            Shader.SetGlobalFloat(UndergroundBoostId, undergroundBoost);
            Shader.SetGlobalFloat(MaxDistanceId, maxDistance * qualityDistanceMultiplier);
            Shader.SetGlobalFloat(StepCountId, qualityStepOverride > 0 ? qualityStepOverride : stepCount);
            Shader.SetGlobalFloat(ScatteringId, scattering);
            Shader.SetGlobalFloat(ExtinctionId, extinction);
            Shader.SetGlobalFloat(AnisotropyId, anisotropy);
            Shader.SetGlobalFloat(ShaftIntensityId, shaftIntensity * qualityShaftMultiplier);
            Shader.SetGlobalFloat(AmbientBoostId, ambientBoost);
        }
    }
}
