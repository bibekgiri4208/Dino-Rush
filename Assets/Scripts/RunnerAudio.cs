using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Small synthesized cues, since the supplied art pack has no audio.</summary>
    public sealed class RunnerAudio : IDisposable
    {
        private readonly AudioSource source;
        private readonly AudioClip jump;
        private readonly AudioClip crash;
        private readonly AudioClip milestone;
        public bool Muted { get; private set; }

        public RunnerAudio(GameObject owner)
        {
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.2f;
            Muted = PlayerPrefs.GetInt("DinoRush.Muted", 0) == 1;
            source.mute = Muted;
            jump = Tone("Jump", 0.13f, 420f, 880f);
            crash = Tone("Bump", 0.24f, 180f, 45f);
            milestone = Tone("New milestone", 0.3f, 660f, 1320f);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            source.mute = Muted;
            PlayerPrefs.SetInt("DinoRush.Muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Jump() => source.PlayOneShot(jump, 0.6f);
        public void Crash() => source.PlayOneShot(crash);
        public void Milestone() => source.PlayOneShot(milestone, 0.5f);

        private static AudioClip Tone(string name, float duration, float start, float end)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * sampleRate)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.Lerp(start, end, t) * 2f * Mathf.PI / sampleRate;
                float envelope = Mathf.Min(t * 30f, 1f) * Mathf.Pow(1f - t, 2f);
                samples[i] = Mathf.Sin(phase) * envelope * 0.45f;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(jump);
            UnityEngine.Object.Destroy(crash);
            UnityEngine.Object.Destroy(milestone);
        }
    }
}
