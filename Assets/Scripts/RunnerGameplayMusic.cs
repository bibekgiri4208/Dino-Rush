using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Independent, saved music preference and a single looping gameplay-only source.</summary>
    public sealed class RunnerGameplayMusic : IDisposable
    {
        public const string EnabledKey = "DinoRush.MusicEnabled";
        public const string VolumeKey = "DinoRush.MusicVolume";
        public const float DefaultVolume = 0.35f;
        // Keep the soundtrack beneath gameplay cues, including for existing saved slider values.
        public const float OutputGain = 0.25f;
        private readonly AudioSource source;
        private bool running, paused, started;
        public float Volume { get; private set; }
        public bool Enabled => Volume > 0f;

        public RunnerGameplayMusic(GameObject owner, AudioClip track)
        {
            var node = new GameObject("Gameplay music");
            node.transform.SetParent(owner.transform, false);
            source = node.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            // Retain the previous on/off preference until a volume has been saved.
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,
                PlayerPrefs.GetInt(EnabledKey, 1) != 0 ? DefaultVolume : 0f));
            source.volume = Volume * OutputGain;
            source.priority = 192;
            source.clip = track;
        }

        public void BeginRun()
        {
            Stop();
            running = true;
            Apply();
        }

        public void SetPaused(bool value)
        {
            paused = value;
            Apply();
        }

        public void SetVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(Volume, value)) return;
            Volume = value;
            source.volume = Volume * OutputGain;
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.Save();
            Apply();
        }

        private void Apply()
        {
            if (!running || source.clip == null) return;
            if (!Enabled || paused) { source.Pause(); return; }
            if (started) source.UnPause();
            else { source.Play(); started = true; }
        }

        public void Stop()
        {
            running = paused = started = false;
            source.Stop();
        }

        public void Dispose()
        {
            Stop();
            // The imported clip belongs to the project, not to this playback owner.
            UnityEngine.Object.Destroy(source.gameObject);
        }
    }
}
