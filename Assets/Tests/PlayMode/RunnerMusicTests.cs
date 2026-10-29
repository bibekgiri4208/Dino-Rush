using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DinoRush.Tests
{
    public sealed class RunnerMusicTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private Transform root;
        private AudioSource source;
        private bool hadVolume;
        private float oldVolume;
        private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly HashSet<string> existed = new HashSet<string>();

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadVolume = PlayerPrefs.HasKey(RunnerGameplayMusic.VolumeKey);
            oldVolume = PlayerPrefs.GetFloat(RunnerGameplayMusic.VolumeKey);
            PlayerPrefs.DeleteKey(RunnerGameplayMusic.VolumeKey);
            ints.Clear(); strings.Clear(); existed.Clear();
            var keys = new List<string>(RunnerAccountProgress.Keys(null)) { "DinoRush.Muted", RunnerGameplayMusic.EnabledKey };
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) existed.Add(key);
                if (key == RunnerDinoRoster.SelectionKey || key == RunnerMapCatalog.SelectionKey || key == RunnerDailyMissions.SaveKey ||
                    key == RunnerStoryProgress.SaveKey || key == RunnerStoryProgress.TransactionKey)
                    strings[key] = PlayerPrefs.GetString(key);
                else ints[key] = PlayerPrefs.GetInt(key);
            }
            PlayerPrefs.DeleteKey(RunnerGameplayMusic.EnabledKey);
            PlayerPrefs.SetInt("DinoRush.Muted", 1);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            root = game.transform.Find("Forest runner UI/16 by 9 safe content");
            source = game.transform.Find("Gameplay music").GetComponent<AudioSource>();
            foreach (BaseInputModule input in EventSystem.current.GetComponents<BaseInputModule>()) input.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (var pair in ints)
                if (existed.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in strings)
                if (existed.Contains(pair.Key)) PlayerPrefs.SetString(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            if (hadVolume) PlayerPrefs.SetFloat(RunnerGameplayMusic.VolumeKey, oldVolume);
            else PlayerPrefs.DeleteKey(RunnerGameplayMusic.VolumeKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator SuppliedTrackLoopsOnlyDuringGameplayAndResumesFromThePausedPosition()
        {
            Assert.That(source.clip, Is.Not.Null);
            Assert.That(source.clip.name, Is.EqualTo("Games Bg music"));
            Assert.That(source.loop, Is.True);
            Assert.That(source.playOnAwake, Is.False);
            Assert.That(source.spatialBlend, Is.Zero);
            Assert.That(game.MusicEnabled, Is.True, "Music defaults on independently of the saved SFX setting.");
            Assert.That(source.volume, Is.EqualTo(RunnerGameplayMusic.DefaultVolume * RunnerGameplayMusic.OutputGain).Within(0.001f));
            Assert.That(source.isPlaying, Is.False, "The title screen stays silent.");
            game.PrepareRun();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
            Assert.That(source.isPlaying, Is.False);
            game.StartRun();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(source.isPlaying, Is.True);
            Assert.That(source.mute, Is.False);
            Assert.That(source.time, Is.GreaterThan(0.1f));
            game.TogglePause();
            float pausedAt = source.time;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(source.isPlaying, Is.False);
            Assert.That(source.time, Is.EqualTo(pausedAt).Within(0.05f));
            game.TogglePause();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(source.isPlaying, Is.True);
            Assert.That(source.time, Is.GreaterThan(pausedAt));
            Invoke("EndRun");
            Assert.That(source.isPlaying, Is.False);
            game.StartRun();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(source.isPlaying, Is.True);
            Assert.That(source.time, Is.LessThan(pausedAt), "A new run restarts the track rather than overlapping another source.");
            game.ReturnToMenu();
            Assert.That(source.isPlaying, Is.False);
            Assert.That(game.GetComponentsInChildren<AudioSource>(true), Has.Exactly(1).Matches<AudioSource>(item => item.clip == source.clip));
        }

        [UnityTest]
        public IEnumerator OptionsAndPauseSlidersStayInSyncPersistAndDoNotChangeSfx()
        {
            var cues = (RunnerAudio)typeof(ForestRunnerGame).GetField("audioCues", Private).GetValue(game);
            Click("Forest Main Menu/Main menu buttons/Options");
            Slider options = root.Find("Forest Main Menu/Menu dialog/Moss framed dialog/Options page/Music volume").GetComponent<Slider>();
            Slider pause = root.Find("Forest Pause Menu/Music volume").GetComponent<Slider>();
            options.value = 70;
            Assert.That(game.MusicVolume, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(source.volume, Is.EqualTo(0.7f * RunnerGameplayMusic.OutputGain).Within(0.001f));
            Assert.That(pause.value, Is.EqualTo(70));
            options.value = 0;
            Assert.That(game.MusicEnabled, Is.False);
            Assert.That(PlayerPrefs.GetFloat(RunnerGameplayMusic.VolumeKey), Is.Zero);
            Assert.That(options.GetComponentInChildren<Text>().text, Is.EqualTo("MUSIC 0%"));
            Assert.That(pause.GetComponentInChildren<Text>().text, Is.EqualTo("MUSIC 0%"));
            Assert.That(cues.Muted, Is.True);
            game.StartRun();
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(source.isPlaying, Is.False);
            game.TogglePause(); pause.value = 60;
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(game.MusicEnabled, Is.True);
            Assert.That(source.isPlaying, Is.False, "Turning music on during pause must not start it until resume.");
            Assert.That(options.GetComponentInChildren<Text>().text, Is.EqualTo("MUSIC 60%"));
            game.TogglePause();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(source.isPlaying, Is.True);
            Invoke("ToggleMute");
            Assert.That(cues.Muted, Is.False);
            Assert.That(game.MusicEnabled, Is.True);
            Assert.That(source.isPlaying, Is.True);
            game.TogglePause(); pause.value = 25;
            Assert.That(cues.Muted, Is.False);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            Assert.That(game.MusicVolume, Is.EqualTo(0.25f).Within(0.001f), "The volume preference survives scene reloads.");
            Assert.That(source == null, Is.True, "The previous playback source must not leak across scenes.");
        }

        [UnityTest]
        public IEnumerator MusicSlidersFitBothMenusInLandscapeAndPortrait()
        {
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(360, 800), new Vector2Int(320, 568) })
            {
                game.ReturnToMenu(); Click("Forest Main Menu/Main menu buttons/Options");
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y)));
                yield return new WaitForSecondsRealtime(0.35f);
                AssertControls(root.Find("Forest Main Menu/Menu dialog/Moss framed dialog"));
                yield return RunnerDisplayCapture.Capture("music-options-" + size.x, size.x, size.y);
                game.StartRun(); game.TogglePause();
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 0, size.x, size.y)));
                yield return new WaitForSecondsRealtime(0.35f);
                AssertControls(root.Find("Forest Pause Menu"));
                yield return RunnerDisplayCapture.Capture("music-pause-" + size.x, size.x, size.y);
            }
        }

        private static void AssertControls(Transform panel)
        {
            var buttons = panel.GetComponentsInChildren<Selectable>();
            Slider slider = panel.GetComponentInChildren<Slider>();
            Assert.That(slider, Is.Not.Null);
            Assert.That(slider.handleRect.rect.height, Is.GreaterThan(0));
            Assert.That(slider.handleRect.rect.width, Is.GreaterThan(0));
            float original = slider.value;
            foreach (float volume in new[] { 0f, 0.35f, 1f })
            {
                RunnerMusicUi.SetVolume(slider, volume);
                Canvas.ForceUpdateCanvases();
                var rect = ((RectTransform)slider.transform).rect;
                float padding = rect.height >= 80 ? 20 : 12;
                var interior = Rect.MinMaxRect(rect.xMin + padding, rect.yMin + padding, rect.xMax - padding, rect.yMax - padding);
                Bounds labelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(slider.transform, slider.transform.Find("Label"));
                foreach (Transform part in new[] { slider.transform.Find("Label"), slider.transform.Find("Volume track"), slider.handleRect })
                {
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(slider.transform, part);
                    Assert.That(bounds.min.x >= interior.xMin - 0.01f && bounds.min.y >= interior.yMin - 0.01f &&
                        bounds.max.x <= interior.xMax + 0.01f && bounds.max.y <= interior.yMax + 0.01f, Is.True,
                        part.name + " must remain inside the button's border at " + slider.value + "%: " +
                        "bounds " + bounds + ", interior " + interior + ", slot " + rect);
                    if (part.name != "Label")
                        Assert.That(new Rect(labelBounds.min, labelBounds.size).Overlaps(new Rect(bounds.min, bounds.size)), Is.False,
                            "The label must not overlap " + part.name);
                }
                Text text = slider.transform.Find("Label").GetComponent<Text>();
                Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1));
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1));
            }
            RunnerMusicUi.SetVolume(slider, original / 100f);
            for (int i = 0; i < buttons.Length; i++)
            {
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, buttons[i].transform);
                Assert.That(((RectTransform)panel).rect.Contains(bounds.min), Is.True, buttons[i].name);
                Assert.That(((RectTransform)panel).rect.Contains(bounds.max), Is.True, buttons[i].name);
                for (int j = i + 1; j < buttons.Length; j++)
                {
                    var other = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, buttons[j].transform);
                    Assert.That(new Rect(bounds.min, bounds.size).Overlaps(new Rect(other.min, other.size)), Is.False,
                        buttons[i].name + " overlaps " + buttons[j].name);
                }
            }
        }

        [UnityTest]
        public IEnumerator PointerDraggingAndArrowKeysAdjustTheActualVolume()
        {
            Click("Forest Main Menu/Main menu buttons/Options");
            yield return new WaitForSecondsRealtime(0.35f);
            Slider slider = root.Find("Forest Main Menu/Menu dialog/Moss framed dialog/Options page/Music volume").GetComponent<Slider>();
            var track = (RectTransform)slider.transform.Find("Volume track");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = 0 };
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, track.TransformPoint(new Vector3(track.rect.width * 0.3f, 0, 0)));
            slider.OnPointerDown(pointer);
            Assert.That(game.MusicVolume, Is.EqualTo(0.8f).Within(0.02f));
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, track.TransformPoint(new Vector3(track.rect.xMin, 0, 0)));
            slider.OnDrag(pointer); slider.OnPointerUp(pointer);
            Assert.That(game.MusicVolume, Is.Zero);
            var original = Keyboard.current;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                InputSystem.EnableDevice(keyboard);
                keyboard.MakeCurrent();
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slider.gameObject), "Dragging also gives the slider keyboard focus.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.RightArrow)); InputSystem.Update();
                Assert.That(keyboard.rightArrowKey.isPressed, Is.True, "The simulated keyboard must be receiving input.");
                Assert.That(Keyboard.current, Is.EqualTo(keyboard));
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(slider.gameObject));
                Assert.That(slider.IsActive() && slider.IsInteractable(), Is.True);
                yield return new WaitForSecondsRealtime(0.04f);
                Assert.That(game.MusicVolume, Is.EqualTo(0.05f).Within(0.001f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (original != null && original.added) original.MakeCurrent();
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        [UnityTest]
        public IEnumerator VolumeIsClampedAndMigratesTheOldMutePreferenceWithoutOverwritingNewSettings()
        {
            game.SetMusicVolume(-2f); Assert.That(game.MusicVolume, Is.Zero);
            game.SetMusicVolume(2f); Assert.That(source.volume, Is.EqualTo(RunnerGameplayMusic.OutputGain));
            PlayerPrefs.DeleteKey(RunnerGameplayMusic.VolumeKey);
            PlayerPrefs.SetInt(RunnerGameplayMusic.EnabledKey, 0);
            var owner = new GameObject("Music migration owner");
            var legacy = new RunnerGameplayMusic(owner, null);
            Assert.That(legacy.Volume, Is.Zero, "Previously muted music should stay silent after upgrading.");
            legacy.SetVolume(0.6f);
            var reloaded = new RunnerGameplayMusic(owner, null);
            Assert.That(reloaded.Volume, Is.EqualTo(0.6f).Within(0.001f), "The new volume takes precedence over the old toggle.");
            foreach (AudioSource playback in owner.GetComponentsInChildren<AudioSource>())
                Assert.That(playback.volume, Is.EqualTo(0.6f * RunnerGameplayMusic.OutputGain).Within(0.001f),
                    "Saved volume levels must use the quieter mix too, without changing the saved percentage.");
            legacy.Dispose(); reloaded.Dispose(); Object.Destroy(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingTrackIsSafeAndDisposalDoesNotDestroyTheImportedClip()
        {
            var owner = new GameObject("Music test owner");
            var missing = new RunnerGameplayMusic(owner, null);
            missing.BeginRun(); missing.SetPaused(true); missing.SetVolume(0f); missing.SetPaused(false); missing.Dispose();
            var imported = source.clip;
            Object.Destroy(owner); Object.Destroy(game.gameObject);
            yield return null;
            Assert.That(source == null, Is.True);
            Assert.That(imported != null, Is.True);
        }

        private Button At(string path) => root.Find(path).GetComponent<Button>();
        private void Click(string path) => At(path).onClick.Invoke();
        private void Invoke(string name) => typeof(ForestRunnerGame).GetMethod(name, Private).Invoke(game, null);
    }
}
