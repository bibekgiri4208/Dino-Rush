using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Generated gameplay/UI cues and sampled elemental slashes, with shared SFX controls.</summary>
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
        private readonly AudioSource uiSource, slashSource;
        private readonly AudioClip buttonClick;
        private readonly AudioClip[] slashClips;
        public event Action ButtonClickPlayed;
        public event Action<RunnerSlashKind> SlashPlayed;
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
            uiSource = owner.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0;
            uiSource.volume = 0.32f;
            uiSource.mute = Muted;
            slashSource = owner.AddComponent<AudioSource>();
            slashSource.playOnAwake = false;
            slashSource.spatialBlend = 0;
            slashSource.volume = 0.5f;
            slashSource.mute = Muted;
            buttonClick = RunnerActionAudio.CreateButtonClick();
            slashClips = new AudioClip[(int)RunnerSlashKind.Water];
            for (int i = 0; i < slashClips.Length; i++) slashClips[i] = RunnerActionAudio.LoadSlash((RunnerSlashKind)(i + 1));
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            source.mute = Muted;
            shopSource.mute = Muted;
            uiSource.mute = slashSource.mute = Muted;
            if (Muted) { source.Stop(); shopSource.Stop(); uiSource.Stop(); slashSource.Stop(); }
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

        public void ButtonClick()
        {
            if (Muted || uiSource == null) return;
            uiSource.PlayOneShot(buttonClick);
            ButtonClickPlayed?.Invoke();
        }

        public void Slash(RunnerSlashKind kind)
        {
            if (Muted || slashSource == null || !RunnerSlashProgress.Valid(kind)) return;
            AudioClip clip = slashClips[(int)kind - 1];
            if (clip == null) return;
            slashSource.Stop(); // Rapid deliberate style previews never pile up into a loud chorus.
            slashSource.PlayOneShot(clip);
            SlashPlayed?.Invoke(kind);
        }
        public void StopSlashSounds() { if (slashSource != null) slashSource.Stop(); }
        public void PauseSlashSounds(bool paused)
        {
            if (slashSource == null) return;
            if (paused) slashSource.Pause(); else slashSource.UnPause();
        }

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
            StopSlashSounds();
            if (uiSource != null) uiSource.Stop();
            foreach (AudioClip clip in eggCues) UnityEngine.Object.Destroy(clip);
            foreach (AudioClip clip in revealNotes) UnityEngine.Object.Destroy(clip);
            UnityEngine.Object.Destroy(buttonClick);
            // Slash clips are shared imported assets, not runtime-generated objects to destroy.
            UnityEngine.Object.Destroy(source);
            UnityEngine.Object.Destroy(shopSource);
            UnityEngine.Object.Destroy(uiSource);
            UnityEngine.Object.Destroy(slashSource);
            EggCuePlayed = null;
            ButtonClickPlayed = null;
            SlashPlayed = null;
        }
    }
}
