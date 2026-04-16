using UnityEngine;
using UnityEngine.Rendering;

namespace WorldOfVictoria.Core
{
    public sealed class VisualManager : MonoBehaviour
    {
        [SerializeField] private Light mainDirectionalLight;
        [SerializeField] private Volume globalVolume;
        [SerializeField] private ParticleSystem dustMotes;
        [SerializeField] private AtmosphereParticleController atmosphereParticles;
        [SerializeField] private VisualScalabilityController scalabilityController;
        [SerializeField] private string defaultQualityTier = "High";
        [SerializeField] private bool applyQualityOnAwake = true;

        public Light MainDirectionalLight => mainDirectionalLight;
        public Volume GlobalVolume => globalVolume;
        public ParticleSystem DustMotes => dustMotes;
        public AtmosphereParticleController AtmosphereParticles => atmosphereParticles;
        public VisualScalabilityController ScalabilityController => scalabilityController;
        public string DefaultQualityTier => defaultQualityTier;

        private void Awake()
        {
            if (applyQualityOnAwake)
            {
                ApplyQuality(defaultQualityTier);
            }
        }

        public void ApplyQuality(string qualityName)
        {
            if (string.IsNullOrWhiteSpace(qualityName))
            {
                return;
            }

            var qualityNames = QualitySettings.names;
            for (var i = 0; i < qualityNames.Length; i++)
            {
                if (qualityNames[i] == qualityName)
                {
                    QualitySettings.SetQualityLevel(i, true);
                    scalabilityController?.ApplyQualityProfile(qualityName);
                    atmosphereParticles?.ApplyQualityProfile(qualityName);
                    return;
                }
            }

            Debug.LogWarning($"Unknown quality tier '{qualityName}'.", this);
        }

        public void ConfigureMainLight(Light directionalLight)
        {
            mainDirectionalLight = directionalLight;
        }

        public void ConfigureGlobalVolume(Volume volume)
        {
            globalVolume = volume;
        }

        public void ConfigureDustMotes(ParticleSystem particleSystem)
        {
            dustMotes = particleSystem;
            atmosphereParticles?.ConfigureDustMotes(particleSystem);
        }

        public void ConfigureAtmosphereParticles(AtmosphereParticleController controller)
        {
            atmosphereParticles = controller;
        }

        public void ConfigureScalabilityController(VisualScalabilityController controller)
        {
            scalabilityController = controller;
        }
    }
}
