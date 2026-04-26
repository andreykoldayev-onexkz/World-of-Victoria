using UnityEngine;

namespace WorldOfVictoria.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class InteractionAudioController : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float breakVolume = 0.24f;
        [SerializeField, Range(0f, 1f)] private float placeVolume = 0.18f;

        private AudioSource audioSource;
        private AudioClip breakClip;
        private AudioClip placeClip;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;

            breakClip ??= CreateClip("BlockBreak", 0.11f, 220f, 140f, 0.45f);
            placeClip ??= CreateClip("BlockPlace", 0.08f, 360f, 240f, 0.2f);
        }

        public void PlayBreak()
        {
            if (breakClip != null)
            {
                audioSource.PlayOneShot(breakClip, breakVolume);
            }
        }

        public void PlayPlace()
        {
            if (placeClip != null)
            {
                audioSource.PlayOneShot(placeClip, placeVolume);
            }
        }

        private static AudioClip CreateClip(string clipName, float lengthSeconds, float startFrequency, float endFrequency, float noiseAmount)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * lengthSeconds));
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)(sampleCount - 1);
                var frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                var envelope = Mathf.Pow(1f - t, 2.4f);
                var sine = Mathf.Sin(2f * Mathf.PI * frequency * (i / (float)sampleRate));
                var noise = (Random.value * 2f - 1f) * noiseAmount;
                samples[i] = Mathf.Clamp((sine * (1f - noiseAmount) + noise) * envelope * 0.35f, -1f, 1f);
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
