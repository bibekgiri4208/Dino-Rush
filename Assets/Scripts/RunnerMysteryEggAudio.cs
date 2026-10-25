using UnityEngine;

namespace DinoRush
{
    /// <summary>Deterministic shell percussion and soft, musical hatch cues; generated once, not per pull.</summary>
    internal static class RunnerMysteryEggAudio
    {
        internal static AudioClip Create(RunnerAudio.EggCue cue, int note = 0)
        {
            const int rate = 22050;
            float duration = cue == RunnerAudio.EggCue.Pull ? 0.38f : cue == RunnerAudio.EggCue.Crack ? 0.2f :
                cue == RunnerAudio.EggCue.Charge ? 0.6f : cue == RunnerAudio.EggCue.Unlock ? 0.8f :
                cue == RunnerAudio.EggCue.Fragments ? 0.065f : 0.28f;
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            var noise = new System.Random(5127 + (int)cue);
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float seconds = i / (float)rate, t = seconds / duration;
                float value;
                if (cue == RunnerAudio.EggCue.Crack)
                {
                    float grit = (float)noise.NextDouble() * 2 - 1;
                    float first = Mathf.Exp(-seconds * 48);
                    float second = seconds < 0.065f ? 0 : Mathf.Exp(-(seconds - 0.065f) * 65);
                    value = grit * (first + second * 0.7f) * 0.55f + Mathf.Sin(seconds * 920) * first * 0.2f;
                }
                else if (cue == RunnerAudio.EggCue.Pull || cue == RunnerAudio.EggCue.Charge)
                {
                    bool pull = cue == RunnerAudio.EggCue.Pull;
                    phase += Mathf.Lerp(pull ? 240 : 350, pull ? 780 : 1100, t * t) * 2 * Mathf.PI / rate;
                    float envelope = Mathf.Sin(t * Mathf.PI) * (pull ? 1 - t * 0.5f : 1);
                    value = (Mathf.Sin(phase) * 0.27f + Mathf.Sin(phase * 1.5f) * 0.1f +
                        ((float)noise.NextDouble() * 2 - 1) * (pull ? 0.16f : 0.04f)) * envelope;
                }
                else
                {
                    float frequency = cue == RunnerAudio.EggCue.Fragments ? 1480 : 660 * Mathf.Pow(2, Mathf.Clamp(note, 0, 9) / 24f);
                    value = Bell(seconds, frequency) * 0.4f;
                    if (cue == RunnerAudio.EggCue.Unlock)
                        value = (Bell(seconds, 523.25f) + Bell(seconds - 0.08f, 659.25f) +
                            Bell(seconds - 0.16f, 783.99f) + Bell(seconds - 0.24f, 1046.5f)) * 0.22f;
                }
                // Small end ramps keep every cue click-free, even when interrupted by a skip.
                samples[i] = Mathf.Clamp(value, -0.9f, 0.9f) * Mathf.Clamp01(seconds / 0.003f) * Mathf.Clamp01((duration - seconds) / 0.02f);
            }
            AudioClip clip = AudioClip.Create("Mystery egg " + cue + (cue == RunnerAudio.EggCue.Reveal ? " " + note : ""), samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Bell(float seconds, float frequency)
        {
            if (seconds < 0) return 0;
            float phase = seconds * frequency * Mathf.PI * 2;
            return (Mathf.Sin(phase) + Mathf.Sin(phase * 2) * 0.25f) * Mathf.Exp(-seconds * 11) * Mathf.Clamp01(seconds / 0.008f);
        }
    }
}
