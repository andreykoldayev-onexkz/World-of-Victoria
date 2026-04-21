using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WorldOfVictoria.Core
{
    public sealed class VisualScalabilityController : MonoBehaviour
    {
        [Serializable]
        private struct QualityVisualProfile
        {
            public string qualityName;
            public bool postProcessingEnabled;
            public float bloomIntensity;
            public float postExposure;
            public float contrast;
            public float saturation;
            public float fogDensityMultiplier;
            public float brightVertexLighting;
            public float shadowVertexLighting;
            public float brightShadowBoost;
            public float shadowShadowBoost;
            public float brightRoughnessBias;
            public float shadowRoughnessBias;
        }

        [SerializeField] private Volume globalVolume;
        [SerializeField] private Material brightChunkMaterial;
        [SerializeField] private Material shadowChunkMaterial;
        [SerializeField] private AtmosphereParticleController atmosphereParticles;
        [SerializeField] private WorldConfig worldConfig;
        [SerializeField] private QualityVisualProfile[] profiles =
        {
            new QualityVisualProfile
            {
                qualityName = "Ultra",
                postProcessingEnabled = true,
                bloomIntensity = 0.22f,
                postExposure = 0.16f,
                contrast = 10f,
                saturation = 10f,
                fogDensityMultiplier = 1.0f,
                brightVertexLighting = 1.0f,
                shadowVertexLighting = 1.0f,
                brightShadowBoost = 0.14f,
                shadowShadowBoost = 0.18f,
                brightRoughnessBias = -0.06f,
                shadowRoughnessBias = 0.01f
            },
            new QualityVisualProfile
            {
                qualityName = "High",
                postProcessingEnabled = true,
                bloomIntensity = 0.20f,
                postExposure = 0.14f,
                contrast = 9f,
                saturation = 8f,
                fogDensityMultiplier = 1.0f,
                brightVertexLighting = 1.0f,
                shadowVertexLighting = 1.0f,
                brightShadowBoost = 0.12f,
                shadowShadowBoost = 0.16f,
                brightRoughnessBias = -0.04f,
                shadowRoughnessBias = 0.02f
            },
            new QualityVisualProfile
            {
                qualityName = "Medium",
                postProcessingEnabled = true,
                bloomIntensity = 0.14f,
                postExposure = 0.09f,
                contrast = 6f,
                saturation = 4f,
                fogDensityMultiplier = 0.82f,
                brightVertexLighting = 1.0f,
                shadowVertexLighting = 1.0f,
                brightShadowBoost = 0.08f,
                shadowShadowBoost = 0.14f,
                brightRoughnessBias = -0.02f,
                shadowRoughnessBias = 0.03f
            },
            new QualityVisualProfile
            {
                qualityName = "Low",
                postProcessingEnabled = false,
                bloomIntensity = 0f,
                postExposure = 0f,
                contrast = 2f,
                saturation = 0f,
                fogDensityMultiplier = 0.68f,
                brightVertexLighting = 1.0f,
                shadowVertexLighting = 1.0f,
                brightShadowBoost = 0.03f,
                shadowShadowBoost = 0.05f,
                brightRoughnessBias = 0f,
                shadowRoughnessBias = 0.02f
            }
        };

        public void Configure(Volume volume, Material brightMaterial, Material shadowMaterial, AtmosphereParticleController particleController, WorldConfig config)
        {
            globalVolume = volume;
            brightChunkMaterial = brightMaterial;
            shadowChunkMaterial = shadowMaterial;
            atmosphereParticles = particleController;
            worldConfig = config;
        }

        public void ResetProfilesToRecommendedDefaults()
        {
            profiles = new[]
            {
                new QualityVisualProfile
                {
                    qualityName = "Ultra",
                    postProcessingEnabled = true,
                    bloomIntensity = 0.22f,
                    postExposure = 0.16f,
                    contrast = 10f,
                    saturation = 10f,
                    fogDensityMultiplier = 1.0f,
                    brightVertexLighting = 1.0f,
                    shadowVertexLighting = 1.0f,
                    brightShadowBoost = 0.14f,
                    shadowShadowBoost = 0.18f,
                    brightRoughnessBias = -0.06f,
                    shadowRoughnessBias = 0.01f
                },
                new QualityVisualProfile
                {
                    qualityName = "High",
                    postProcessingEnabled = true,
                    bloomIntensity = 0.20f,
                    postExposure = 0.14f,
                    contrast = 9f,
                    saturation = 8f,
                    fogDensityMultiplier = 1.0f,
                    brightVertexLighting = 1.0f,
                    shadowVertexLighting = 1.0f,
                    brightShadowBoost = 0.12f,
                    shadowShadowBoost = 0.16f,
                    brightRoughnessBias = -0.04f,
                    shadowRoughnessBias = 0.02f
                },
                new QualityVisualProfile
                {
                    qualityName = "Medium",
                    postProcessingEnabled = true,
                    bloomIntensity = 0.14f,
                    postExposure = 0.09f,
                    contrast = 6f,
                    saturation = 4f,
                    fogDensityMultiplier = 0.82f,
                    brightVertexLighting = 1.0f,
                    shadowVertexLighting = 1.0f,
                    brightShadowBoost = 0.08f,
                    shadowShadowBoost = 0.14f,
                    brightRoughnessBias = -0.02f,
                    shadowRoughnessBias = 0.03f
                },
                new QualityVisualProfile
                {
                    qualityName = "Low",
                    postProcessingEnabled = false,
                    bloomIntensity = 0f,
                    postExposure = 0f,
                    contrast = 2f,
                    saturation = 0f,
                    fogDensityMultiplier = 0.68f,
                    brightVertexLighting = 1.0f,
                    shadowVertexLighting = 1.0f,
                    brightShadowBoost = 0.03f,
                    shadowShadowBoost = 0.05f,
                    brightRoughnessBias = 0f,
                    shadowRoughnessBias = 0.02f
                }
            };
        }

        public void ApplyQualityProfile(string qualityName)
        {
            if (string.IsNullOrWhiteSpace(qualityName))
            {
                return;
            }

            var profile = ResolveProfile(qualityName);
            ApplyVolume(profile);
            ApplyChunkMaterials(profile);
            ApplyFog(profile);
            atmosphereParticles?.ApplyQualityProfile(qualityName);
        }

        private void ApplyVolume(QualityVisualProfile profile)
        {
            if (globalVolume == null || globalVolume.sharedProfile == null)
            {
                return;
            }

            globalVolume.weight = profile.postProcessingEnabled ? 1f : 0f;
            if (globalVolume.sharedProfile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = profile.postProcessingEnabled;
                bloom.intensity.Override(profile.bloomIntensity);
            }

            if (globalVolume.sharedProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments.active = profile.postProcessingEnabled;
                colorAdjustments.postExposure.Override(profile.postExposure);
                colorAdjustments.contrast.Override(profile.contrast);
                colorAdjustments.saturation.Override(profile.saturation);
            }

            if (globalVolume.sharedProfile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping.active = profile.postProcessingEnabled;
            }
        }

        private void ApplyChunkMaterials(QualityVisualProfile profile)
        {
            if (brightChunkMaterial != null)
            {
                brightChunkMaterial.SetFloat("_UseVertexBrightness", profile.brightVertexLighting);
                brightChunkMaterial.SetFloat("_ShadowBoost", profile.brightShadowBoost);
                brightChunkMaterial.SetFloat("_RoughnessBias", profile.brightRoughnessBias);
            }

            if (shadowChunkMaterial != null)
            {
                shadowChunkMaterial.SetFloat("_UseVertexBrightness", profile.shadowVertexLighting);
                shadowChunkMaterial.SetFloat("_ShadowBoost", profile.shadowShadowBoost);
                shadowChunkMaterial.SetFloat("_RoughnessBias", profile.shadowRoughnessBias);
            }
        }

        private void ApplyFog(QualityVisualProfile profile)
        {
            if (worldConfig == null)
            {
                return;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = worldConfig.FogColor;
            RenderSettings.fogDensity = worldConfig.FogDensity * profile.fogDensityMultiplier;
        }

        private QualityVisualProfile ResolveProfile(string qualityName)
        {
            for (var i = 0; i < profiles.Length; i++)
            {
                if (string.Equals(profiles[i].qualityName, qualityName, StringComparison.OrdinalIgnoreCase))
                {
                    return profiles[i];
                }
            }

            return profiles.Length > 0 ? profiles[0] : default;
        }
    }
}
