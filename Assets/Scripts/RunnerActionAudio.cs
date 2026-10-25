using UnityEngine;

namespace DinoRush
{
    /// <summary>Generated UI taps and cached, sample-based elemental sounds. No gameplay randomness.</summary>
    internal static class RunnerActionAudio
    {
        private const int Rate = 22050;

        internal static AudioClip CreateButtonClick()
        {
            var samples = new float[Mathf.CeilToInt(Rate * 0.065f)];
            var noise = new System.Random(7139);
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float seconds = i / (float)Rate, t = i / (float)samples.Length;
                phase += Mathf.Lerp(920, 380, t) * 2 * Mathf.PI / Rate;
                float tap = (float)noise.NextDouble() * 2 - 1;
                samples[i] = Mathf.Sin(phase) * Mathf.Exp(-seconds * 72) * 0.65f + tap * Mathf.Exp(-seconds * 230) * 0.22f;
            }
            return Clip("UI button click", samples, 0.5f);
        }

        internal static AudioClip LoadSlash(RunnerSlashKind kind)
        {
            // Imported, preloaded PCM preserves each element's texture instead of approximating
            // all three with pitched oscillators. Resources also includes the clips in builds.
            AudioClip clip = Resources.Load<AudioClip>("Audio/Slashes/" + kind + " slash");
            if (clip == null) Debug.LogError("Missing elemental slash audio for " + kind + ".");
            return clip;
        }

        private static AudioClip Clip(string name, float[] samples, float peak)
        {
            float maximum = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float seconds = i / (float)Rate;
                samples[i] *= Mathf.Clamp01(seconds / 0.002f) * Mathf.Clamp01((samples.Length - 1 - i) / (Rate * 0.018f));
                maximum = Mathf.Max(maximum, Mathf.Abs(samples[i]));
            }
            float gain = maximum > 0 ? peak / maximum : 0;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
