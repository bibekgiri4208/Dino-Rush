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
        private readonly AudioClip pickup;
        public enum EggCue { Pull, Crack, Charge, Reveal, Unlock, Fragments }
        public event Action<EggCue> EggCuePlayed;
        private readonly AudioSource shopSource;
        private readonly AudioClip[] eggCues;
        private readonly AudioClip[] revealNotes;
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
            pickup = Tone("Egg pickup", 0.09f, 1100f, 1760f);
            shopSource = owner.AddComponent<AudioSource>();
            shopSource.playOnAwake = false;
            shopSource.spatialBlend = 0;
            shopSource.volume = 0.55f;
            shopSource.mute = Muted;
            eggCues = new AudioClip[6];
            for (int i = 0; i < eggCues.Length; i++) eggCues[i] = RunnerMysteryEggAudio.Create((EggCue)i);
            revealNotes = new AudioClip[RunnerShopProgress.TenPullCount];
            for (int i = 0; i < revealNotes.Length; i++) revealNotes[i] = RunnerMysteryEggAudio.Create(EggCue.Reveal, i);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            source.mute = Muted;
            shopSource.mute = Muted;
            if (Muted) { source.Stop(); shopSource.Stop(); }
            PlayerPrefs.SetInt("DinoRush.Muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Jump() => source.PlayOneShot(jump, 0.6f);
        public void Crash() => source.PlayOneShot(crash);
        public void Milestone() => source.PlayOneShot(milestone, 0.5f);
        public void Pickup() => source.PlayOneShot(pickup, 0.45f);
        public void EggPull() => PlayEgg(EggCue.Pull);
        public void EggCrack() => PlayEgg(EggCue.Crack);
        public void EggCharge() => PlayEgg(EggCue.Charge);
        public void EggHatch(bool unlocked = false, int sequence = 0) => PlayEgg(unlocked ? EggCue.Unlock : EggCue.Reveal, sequence);
        public void EggFragments() => PlayEgg(EggCue.Fragments);
        public void StopEggSounds() { if (shopSource != null) shopSource.Stop(); }

        private void PlayEgg(EggCue cue, int sequence = 0)
        {
            if (Muted || shopSource == null) return;
            shopSource.PlayOneShot(cue == EggCue.Reveal ? revealNotes[Mathf.Clamp(sequence, 0, revealNotes.Length - 1)] : eggCues[(int)cue],
                cue == EggCue.Fragments ? 0.3f : 1);
            EggCuePlayed?.Invoke(cue);
        }

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
            UnityEngine.Object.Destroy(pickup);
            if (source != null) source.Stop();
            StopEggSounds();
            foreach (AudioClip clip in eggCues) UnityEngine.Object.Destroy(clip);
            foreach (AudioClip clip in revealNotes) UnityEngine.Object.Destroy(clip);
            UnityEngine.Object.Destroy(source);
            UnityEngine.Object.Destroy(shopSource);
            EggCuePlayed = null;
        }
    }
}
