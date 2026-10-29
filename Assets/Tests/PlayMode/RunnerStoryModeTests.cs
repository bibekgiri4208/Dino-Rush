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

namespace DinoRush.Tests
{
    public sealed class RunnerStoryModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private RunnerStoryMode story;
        private RunnerPlayer player;
        private MethodInfo step;
        private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly HashSet<string> existed = new HashSet<string>();
        [UnitySetUp]
        public IEnumerator Load()
        {
            ints.Clear(); strings.Clear(); existed.Clear();
            var keys = new List<string>(RunnerAccountProgress.Keys(null));
            foreach (string id in new[] { "blue", "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" })
                keys.Add(RunnerShopProgress.FragmentKey(id));
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) existed.Add(key);
                if (key == RunnerStoryProgress.SaveKey || key == RunnerStoryProgress.TransactionKey || key == RunnerDailyMissions.SaveKey ||
                    key == RunnerMapCatalog.SelectionKey || key == RunnerDinoRoster.SelectionKey) strings[key] = PlayerPrefs.GetString(key);
                else ints[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "desert");
            PlayerPrefs.SetInt(RunnerAccountProgress.HighScoreKey, 1);
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            story = game.Story; player = (RunnerPlayer)Get("player");
            step = typeof(ForestRunnerGame).GetMethod("Step", Private);
            foreach (BaseInputModule module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
            yield return null;
            // Batch-mode synthetic input has no focused Game view; explicitly supply the focus event under test.
            typeof(RunnerStoryMode).GetMethod("OnApplicationFocus", Private).Invoke(story, new object[] { true });
        }
        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (var pair in ints) if (existed.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in strings) if (existed.Contains(pair.Key)) PlayerPrefs.SetString(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }
        private object Get(string name) => typeof(ForestRunnerGame).GetField(name, Private).GetValue(game);
        private void Set(string name, object value) => typeof(ForestRunnerGame).GetField(name, Private).SetValue(game, value);
        private void Step() => step.Invoke(game, new object[] { RunnerRules.SimulationStep });
        private void TickScene(float dt) => typeof(RunnerStoryMode).GetMethod("Tick", Private).Invoke(story, new object[] { dt });
        private RunnerStoryStage Stage
        {
            get
            {
                object view = typeof(RunnerStoryMode).GetField("cutscene", Private).GetValue(story);
                return (RunnerStoryStage)view.GetType().GetField("stage", Private).GetValue(view);
            }
        }
        private void UnlockThrough(int index)
        { for (int i = 0; i < index; i++) story.Progress.Complete(i, 3, 3); }
        private void GoToShot(string shot)
        {
            while (story.Conversation.Playing && story.Conversation.Line.Shot != shot)
            { if (!story.Conversation.Revealed) story.Conversation.Advance(); story.Conversation.Advance(); }
            Assert.That(story.Conversation.Playing, Is.True, "Missing visual beat: " + shot);
            if (!story.Conversation.Revealed) story.Conversation.Advance();
            TickScene(0);
        }

        [UnityTest]
        public IEnumerator StoryUiUsesTheActualForestFontFramesAndPrimarySecondaryButtons()
        {
            story.OpenChapterSelect();
            var root = story.NavigationRoot;
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            Transform original = menu.transform.Find("Menu dialog/Moss framed dialog");
            Font font = original.GetComponentInChildren<Text>(true).font;
            Assert.That(root.Find("Frame").GetComponent<Image>().sprite, Is.EqualTo(original.GetComponent<Image>().sprite));
            foreach (Text label in root.GetComponentsInChildren<Text>(true)) Assert.That(label.font, Is.EqualTo(font), label.name);
            Transform ui = game.transform.Find("Forest runner UI/16 by 9 safe content");
            Assert.That(root.Find("Continue story").GetComponent<Image>().sprite, Is.EqualTo(ui.Find("Forest Pause Menu/Resume run").GetComponent<Image>().sprite));
            Assert.That(root.Find("Back to modes").GetComponent<Image>().sprite, Is.EqualTo(ui.Find("Forest Pause Menu/Restart run").GetComponent<Image>().sprite));
            yield return RunnerDisplayCapture.Capture("story-themed-chapters", 1280, 720);
            yield return RunnerDisplayCapture.Capture("story-themed-chapters-portrait", 360, 800);
            story.StartLevel(0); TickScene(2);
            foreach (Text label in story.NavigationRoot.GetComponentsInChildren<Text>(true)) Assert.That(label.font, Is.EqualTo(font), label.name);
            yield return RunnerDisplayCapture.Capture("story-themed-dialogue", 1280, 720);
            yield return RunnerDisplayCapture.Capture("story-themed-dialogue-portrait", 360, 800);
        }

        [UnityTest]
        public IEnumerator EarthquakeVisiblyBreaksTheGroundTopplesTheTreeAndScattersAnimatedDinos()
        {
            story.StartLevel(0); GoToShot("quake");
            float initialWidth = Stage.FissureWidth, initialAngle = Stage.FallingTreeAngle;
            Vector2 initialBlue = Stage.BluePosition;
            TickScene(1.7f);
            yield return RunnerDisplayCapture.Capture("story-quake-before-collapse", 1280, 720);
            TickScene(1.7f);
            Assert.That(Stage.FissureWidth, Is.GreaterThan(initialWidth * 5));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(initialAngle, Stage.FallingTreeAngle)), Is.GreaterThan(60));
            Assert.That(Vector2.Distance(Stage.BluePosition, initialBlue), Is.GreaterThan(80));
            Image actor = Stage.Root.Find("Animated cast/Blue animated actor").GetComponent<Image>();
            Assert.That(actor.rectTransform.pivot.y, Is.Zero, "Actors must be anchored by their feet, not half buried at the scene floor.");
            Sprite first = actor.sprite; TickScene(0.11f); Assert.That(actor.sprite, Is.Not.EqualTo(first), "Use real running frames, not a bobbing portrait.");
            Assert.That(game.Score, Is.Zero); Assert.That(game.Lives, Is.EqualTo(3));
            yield return RunnerDisplayCapture.Capture("story-quake-collapse", 1280, 720);
            yield return RunnerDisplayCapture.Capture("story-quake-collapse-portrait", 360, 800);
            TickScene(2.4f);
            yield return RunnerDisplayCapture.Capture("story-quake-aftermath", 1280, 720);
        }

        [UnityTest]
        public IEnumerator ConfirmRevealsDialogueButCannotAdvancePastAnUnfinishedActionAndSkipStillWorks()
        {
            story.StartLevel(0);
            story.AdvanceConversation(); Assert.That(story.Conversation.Revealed, Is.True);
            story.AdvanceConversation(); Assert.That(story.Conversation.Index, Is.Zero);
            TickScene(4.1f); story.AdvanceConversation(); Assert.That(story.Conversation.Index, Is.EqualTo(1));
            story.SkipScene(); Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RescueShowsTheGapTheOutwardJumpAndTheHatchlingReturningToSafety()
        {
            UnlockThrough(13); story.Progress.Complete(13, 3, 3); story.ReplayScene(13, true); TickScene(0.1f);
            Vector2 stranded = Stage.HatchlingPosition, blue = Stage.BluePosition;
            TickScene(1.4f);
            Assert.That(Stage.BluePosition.y, Is.GreaterThan(blue.y + 60), "Blue must visibly leap across the gap.");
            yield return RunnerDisplayCapture.Capture("story-rescue-jump", 1280, 720);
            TickScene(1.5f);
            yield return RunnerDisplayCapture.Capture("story-rescue-reached-hatchling", 1280, 720);
            TickScene(3.1f);
            Assert.That(Stage.HatchlingPosition.x, Is.LessThan(stranded.x - 200), "The rescued baby must move back across the gap.");
            Assert.That(Stage.BluePosition.x, Is.LessThan(0)); Assert.That(Stage.HatchlingPosition.x, Is.LessThan(0));
            yield return RunnerDisplayCapture.Capture("story-rescue-safe", 1280, 720);
        }

        [UnityTest]
        public IEnumerator GuardianBendsDownGateActuallyOpensAndTheHerdMigratesThroughIt()
        {
            UnlockThrough(14); story.Progress.Complete(14, 3, 3); story.ReplayScene(14, true); TickScene(0.1f);
            Vector2 childStart = Stage.HatchlingPosition;
            var neck = Stage.Root.Find("Physical story set/Articulated mountain guardian/Bending neck");
            Assert.That(((RectTransform)neck.Find("Neck scales")).anchorMin.y, Is.Zero, "The neck artwork must start at its articulated base.");
            TickScene(3.9f);
            Assert.That(Stage.HatchlingPosition.x, Is.GreaterThan(childStart.x + 200));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, neck.localEulerAngles.z)), Is.GreaterThan(50));
            yield return RunnerDisplayCapture.Capture("story-guardian-reunion", 1280, 720);
            GoToShot("gate"); TickScene(1.2f);
            Transform leftDoor = Stage.Root.Find("Physical story set/Ancient gate mechanism/Sliding left stone");
            float doorStart = ((RectTransform)leftDoor).anchoredPosition.x;
            yield return RunnerDisplayCapture.Capture("story-gate-closed", 1280, 720);
            TickScene(3.2f);
            Assert.That(((RectTransform)leftDoor).anchoredPosition.x, Is.LessThan(doorStart - 100));
            yield return RunnerDisplayCapture.Capture("story-gate-open", 1280, 720);
            GoToShot("valley"); TickScene(0.1f); Vector2 before = Stage.BluePosition; TickScene(4);
            Assert.That(Stage.BluePosition.x, Is.GreaterThan(before.x + 500));
            yield return RunnerDisplayCapture.Capture("story-herd-migration", 1280, 720);
            GoToShot("hatch"); TickScene(1.4f);
            yield return RunnerDisplayCapture.Capture("story-egg-cracking", 1280, 720);
            TickScene(1.4f);
            Assert.That(Stage.Root.Find("Animated cast/New Blue hatchling animated actor").gameObject.activeInHierarchy, Is.True);
            yield return RunnerDisplayCapture.Capture("story-baby-hatched", 1280, 720);
        }

        [UnityTest]
        public IEnumerator SandstormFootprintsRuinsAndSnowHaveTheirOwnVisibleSetsAndEffects()
        {
            UnlockThrough(12);
            foreach (int index in new[] { 1, 3, 4, 5, 8, 10, 11, 12 })
            {
                story.ReplayScene(index, false); TickScene(2.2f);
                Assert.That(Stage.Root.GetComponentsInChildren<Image>().Length, Is.GreaterThan(10));
                Image stone = Stage.Root.Find("Physical story set/Loose rock 0").GetComponent<Image>();
                Assert.That(new[] { "2", "5" }, Does.Contain(stone.sprite.texture.name), "Rock formations must not accidentally use stump/crate sprites.");
                yield return RunnerDisplayCapture.Capture("story-directed-scene-" + index, 1280, 720);
            }
            story.ReplayScene(10, true); TickScene(2.4f);
            Assert.That(Stage.Root.Find("Physical story set/Falling tree").GetComponent<Image>().preserveAspect, Is.False,
                "The improvised log bridge must span the whole broken trail.");
            yield return RunnerDisplayCapture.Capture("story-survivors-crossing", 1280, 720);
            story.ReplayScene(0, true); TickScene(2);
            Assert.That(Stage.Root.Find("Animated cast/Kira animated actor").gameObject.activeInHierarchy, Is.False,
                "The empty clearing must actually be empty of the missing herd.");
        }

        [UnityTest]
        public IEnumerator MenuEntryStartsACompleteConversationAndSuspendsTheRunnerUntilItEnds()
        {
            game.PrimaryAction();
            Button entry = game.GetComponentInChildren<ForestMainMenu>(true).transform
                .Find("Menu dialog/Moss framed dialog/Mode selection page/Story mode").GetComponent<Button>();
            Assert.That(entry.interactable, Is.True); entry.onClick.Invoke();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StorySelect));
            Assert.That(GameObject.Find("Play selected level").GetComponent<Button>().interactable, Is.True);
            Assert.That(GameObject.Find("Level 2").GetComponent<Button>().interactable, Is.False);
            yield return RunnerDisplayCapture.Capture("story-chapter-select", 1280, 720);
            yield return RunnerDisplayCapture.Capture("story-chapter-select-portrait", 360, 800);
            story.StartLevel(0); TickScene(0.8f);
            Assert.That(story.Conversation.Line.Speaker, Is.EqualTo("Blue"));
            for (int i = 0; i < 200; i++) { game.Jump(); game.Attack(); Step(); }
            Assert.That(game.Score, Is.Zero); Assert.That(game.Lives, Is.EqualTo(3));
            Assert.That(player.Grounded, Is.True); Assert.That(game.SlashesUsed, Is.Zero);
            yield return RunnerDisplayCapture.Capture("story-opening", 1280, 720);
            yield return RunnerDisplayCapture.Capture("story-opening-portrait", 360, 800);
            story.SkipScene();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(story.Progress.SceneSeen(0, false), Is.True);
            Assert.That(player.JumpsRemaining, Is.EqualTo(2));
            Assert.That(game.SelectedMapIndex, Is.EqualTo(game.MapCatalog.Find("desert")), "Story maps must not overwrite Endless selection.");
            Step(); Assert.That(game.Score, Is.GreaterThanOrEqualTo(0));
        }

        [UnityTest]
        public IEnumerator FailureAndPauseRestartReplayOnlyTheCurrentLevelWithFreshHeartsAndTokens()
        {
            story.StartLevel(0); story.SkipScene();
            game.TogglePause(); float before = (float)Get("elapsed");
            Step(); Assert.That((float)Get("elapsed"), Is.EqualTo(before));
            game.RestartRun();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StoryScene));
            Assert.That(game.Lives, Is.EqualTo(3)); Assert.That(story.Tokens, Is.Zero);
            story.SkipScene();
            var health = (RunnerHealth)Get("health");
            for (int i = 0; i < 3; i++)
            {
                typeof(ForestRunnerGame).GetMethod("TakeDamage", Private).Invoke(game, null); health.Step(1);
            }
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StoryResults));
            Assert.That(story.Progress.Completed(0), Is.False); Assert.That(story.Progress.Unlocked(1), Is.False);
            Assert.That(GameObject.Find("Next story level").GetComponent<Button>().interactable, Is.False);
            yield return RunnerDisplayCapture.Capture("story-failure", 1280, 720);
            story.RetryLevel(); story.SkipScene();
            Assert.That(game.Lives, Is.EqualTo(3)); Assert.That(story.Tokens, Is.Zero);
            Assert.That(game.SlashesUsed, Is.Zero); Assert.That(game.Score, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FinishingCommitsBeforeTheCutsceneAndReplaysCannotDuplicateRewardsOrEndlessRecords()
        {
            story.StartLevel(0); story.SkipScene();
            int balance = game.Shop.Balance;
            Set("elapsed", RunnerStoryCampaign.Levels[0].Duration - RunnerRules.SimulationStep * 0.5f);
            Set("spawnTimer", float.MaxValue); Set("distance", 800f); Step();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StoryScene));
            Assert.That(story.Progress.Completed(0), Is.True); Assert.That(game.Shop.Balance, Is.EqualTo(balance + 75));
            Assert.That(PlayerPrefs.GetInt(RunnerAccountProgress.HighScoreKey), Is.EqualTo(1));
            Assert.That(game.HighScore, Is.EqualTo(1));
            story.SkipScene(); story.SkipScene();
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StoryResults));
            yield return RunnerDisplayCapture.Capture("story-victory", 1280, 720);
            yield return RunnerDisplayCapture.Capture("story-victory-portrait", 360, 800);
            story.ReplayScene(0, true); story.SkipScene();
            Assert.That(game.Shop.Balance, Is.EqualTo(balance + 75));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StorySelect));
            story.StartLevel(0); story.SkipScene();
            Set("elapsed", 60f); Set("spawnTimer", float.MaxValue); Step(); story.SkipScene();
            Assert.That(game.Shop.Balance, Is.EqualTo(balance + 75));
            game.ReturnToMenu();
            Assert.That(game.HighScore, Is.EqualTo(1));
            game.StartRun(); Assert.That(game.IsStoryRun, Is.False);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(game.SelectedMapName, Is.EqualTo("Desert"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StoryScreensFitSafeAreasAndConversationBackSkipsWithoutOpeningGameplayMenus()
        {
            story.OpenChapterSelect();
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(360, 800), new Vector2Int(1024, 768), new Vector2Int(2400, 1080) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(20, 0, size.x - 40, size.y), true));
                Canvas.ForceUpdateCanvases();
                RectTransform root = (RectTransform)story.transform.Find("Forest runner UI/16 by 9 safe content");
                foreach (Button button in story.NavigationRoot.GetComponentsInChildren<Button>())
                {
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, button.transform);
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(root.rect.xMin - 2));
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(root.rect.xMax + 2));
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(root.rect.yMin - 2));
                    Assert.That(bounds.max.y, Is.LessThanOrEqualTo(root.rect.yMax + 2));
                }
            }
            story.StartLevel(0); yield return null;
            typeof(RunnerStoryMode).GetMethod("HandleBack", Private).Invoke(story, null);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(story.Conversation.Playing, Is.False);
        }

        [UnityTest]
        public IEnumerator AccountResetClearsLiveStoryStateAndCannotResurrectItAfterReload()
        {
            story.Progress.Complete(0, 3, 3); story.Progress.MarkSceneSeen(0, true);
            Assert.That(game.ResetAccount(), Is.True);
            Assert.That(story.Progress.CompletedCount, Is.Zero); Assert.That(story.ActiveLevelIndex, Is.EqualTo(-1));
            Assert.That(new RunnerStoryProgress().CompletedCount, Is.Zero);
            Assert.That(PlayerPrefs.HasKey(RunnerStoryProgress.TransactionKey), Is.False);
            story.OpenChapterSelect(); story.StartLevel(1);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StorySelect));
            yield return null;
        }

        [UnityTest]
        public IEnumerator LosingFocusFreezesConversationAnimationAndDoesNotAdvanceTheCourse()
        {
            story.StartLevel(0);
            typeof(RunnerStoryMode).GetMethod("OnApplicationFocus", Private).Invoke(story, new object[] { false });
            typeof(RunnerStoryMode).GetMethod("OnApplicationPause", Private).Invoke(story, new object[] { false });
            TickScene(2); Step();
            Assert.That(story.Conversation.Characters, Is.Zero); Assert.That(game.Score, Is.Zero);
            typeof(RunnerStoryMode).GetMethod("OnApplicationFocus", Private).Invoke(story, new object[] { true });
            typeof(RunnerStoryMode).GetMethod("OnApplicationPause", Private).Invoke(story, new object[] { true });
            TickScene(1); Assert.That(story.Conversation.Characters, Is.Zero);
            typeof(RunnerStoryMode).GetMethod("OnApplicationPause", Private).Invoke(story, new object[] { false });
            TickScene(0.5f);
            Assert.That(story.Conversation.Characters, Is.GreaterThan(0)); Assert.That(game.Score, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ControllerCanNavigateRevealAndSkipWithoutSpendingAJumpOrAnAttack()
        {
            Gamepad original = Gamepad.current;
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Gamepad pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent(); InputSystem.EnableDevice(pad);
            var ui = (RunnerGamepadUi)Get("gamepadUi");
            try
            {
                story.OpenChapterSelect(); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update(); ui.Read(pad);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StoryScene));
                Assert.That(ui.Scope, Is.EqualTo(story.NavigationRoot));
                InputSystem.QueueStateEvent(pad, default(GamepadState)); InputSystem.Update(); ui.Read(pad);
                yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update(); ui.Read(pad);
                Assert.That(story.Conversation.Revealed, Is.True); Assert.That(story.Conversation.Index, Is.Zero);
                InputSystem.QueueStateEvent(pad, default(GamepadState)); InputSystem.Update(); ui.Read(pad);
                yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East)); InputSystem.Update(); ui.Read(pad);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
                typeof(ForestRunnerGame).GetMethod("ReadInput", Private).Invoke(game, null);
                Assert.That(player.Grounded, Is.True); Assert.That(player.JumpsRemaining, Is.EqualTo(2));
                Assert.That(game.SlashesUsed, Is.Zero);
            }
            finally
            {
                if (pad.added) InputSystem.RemoveDevice(pad);
                if (original != null && original.added) original.MakeCurrent();
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator AllFifteenRealCoursesCanFinishWithEveryCompassAndNoPurchasedAttacks()
        {
            var sprites = (List<Sprite>)Get("obstacleSprites");
            var aspects = new float[sprites.Count];
            for (int i = 0; i < aspects.Length; i++) aspects[i] = sprites[i].bounds.size.x / sprites[i].bounds.size.y;
            var args = new object[] { RunnerRules.SimulationStep };
            for (int index = 0; index < 15; index++)
            {
                RunnerStoryLevel level = RunnerStoryCampaign.Levels[index];
                story.StartLevel(index); story.SkipScene();
                Assert.That(game.MapCatalog[game.ActiveMapIndex].id, Is.EqualTo(level.MapId));
                var oracle = new RunnerEncounterDirector(level.Seed, aspects);
                var trail = (RunnerStoryTrail)typeof(RunnerStoryMode).GetField("trail", Private).GetValue(story);
                var targets = new List<RunnerObstacleSpawn>();
                for (int i = 0; i < 3; i++) targets.Add(new RunnerObstacleSpawn(0, trail.TokenDistance(i), 0.6f, 0.6f));
                float lastGroundEnd = float.NegativeInfinity;
                for (int frame = 0; frame < Mathf.CeilToInt(level.Duration * 120) + 10; frame++)
                {
                    float distance = (float)Get("distance"), elapsed = (float)Get("elapsed");
                    float apex = RunnerRules.JumpSpeed / RunnerRules.Gravity;
                    float lead = (game.CurrentSpeed + RunnerRules.Speed(level.CourseTime(elapsed + apex))) * 0.5f * apex;
                    while (targets.Count > 0 && targets[0].Overhead && distance > targets[0].Offset + targets[0].Width * 0.39f + RunnerRules.PlayerWidth * 0.5f + 0.12f)
                        targets.RemoveAt(0);
                    bool duck = targets.Count > 0 && targets[0].Overhead && targets[0].Offset - distance <= game.CurrentSpeed * 0.6f && distance > lastGroundEnd;
                    game.SetDuckHeld(duck);
                    if (targets.Count > 0 && !targets[0].Overhead && player.Grounded && targets[0].Offset - distance <= lead)
                    {
                        lastGroundEnd = targets[0].Offset + targets[0].Width * 0.5f + RunnerRules.PlayerWidth * 0.5f + 0.12f;
                        game.Jump(); targets.RemoveAt(0);
                    }
                    float previousNext = (float)Get("nextEncounterDistance");
                    step.Invoke(game, args);
                    if (game.State == ForestRunnerGame.RunState.StoryScene && story.Progress.Completed(index)) break;
                    Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running), $"Failed {level.Number} at {elapsed:0.00}s.");
                    if ((float)Get("nextEncounterDistance") != previousNext)
                    {
                        elapsed = (float)Get("elapsed"); distance = (float)Get("distance");
                        RunnerEncounter encounter = oracle.Next(level.CourseTime(elapsed));
                        float spawnX = Mathf.Max(11.7f, (float)Get("viewportHalfWidth") + 1.7f);
                        if (trail.AllowsEncounter(encounter, distance, spawnX, game.CurrentSpeed))
                        {
                            float origin = distance + spawnX - RunnerRules.PlayerX - game.CurrentSpeed * RunnerRules.SimulationStep;
                            if (RunnerEncounterDirector.UsesIndividualActions(encounter.Pattern))
                                foreach (RunnerObstacleSpawn item in encounter.Obstacles)
                                    targets.Add(new RunnerObstacleSpawn(item.SpriteIndex, origin + item.Offset, item.Width, item.Height, item.Overhead));
                            else
                            {
                                RunnerObstacleSpawn first = encounter.Obstacles[0], last = encounter.Obstacles[encounter.Obstacles.Length - 1];
                                targets.Add(new RunnerObstacleSpawn(0, origin + (first.Offset - first.Width * 0.5f + last.Offset + last.Width * 0.5f) * 0.5f,
                                    last.Offset + (first.Width + last.Width) * 0.5f, 0));
                            }
                            targets.Sort((a, b) => a.Offset.CompareTo(b.Offset));
                        }
                    }
                    if ((index % 3 == 0 && frame == 1200) || (index == 0 && frame == 6840))
                    {
                        player.Render(0, true, false, game.CurrentSpeed / RunnerRules.Speed(0));
                        var hud = (RunnerHud)Get("hud"); hud.Tick(5); hud.UpdateScore(game.Score, game.HighScore, game.ObstaclesCleared, game.CurrentSpeed);
                        yield return RunnerDisplayCapture.Capture(index == 0 && frame == 6840 ? "story-final-stretch" : "story-gameplay-" + level.MapId, 1280, 720);
                    }
                    if (frame % 2400 == 0) yield return null;
                }
                Assert.That(story.Progress.Completed(index), Is.True, "Finite finish was not reached: " + level.Number);
                Assert.That(game.Lives, Is.EqualTo(3), "A clean route must exist: " + level.Number);
                Assert.That(story.Tokens, Is.EqualTo(3), "Every optional compass must be reachable: " + level.Number);
                Assert.That(game.SlashesUsed, Is.Zero);
                Assert.That(story.Progress.Stars(index), Is.EqualTo(3));
                if (index == 14)
                {
                    while (story.Conversation.Playing && story.Conversation.Line.Shot != "valley")
                    { story.Conversation.Advance(); story.Conversation.Advance(); }
                    TickScene(1);
                    yield return RunnerDisplayCapture.Capture("story-hidden-valley", 1280, 720);
                }
                story.SkipScene();
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.StoryResults));
            }
            Assert.That(story.Progress.CampaignComplete, Is.True);
            Assert.That(game.HighScore, Is.EqualTo(1)); Assert.That(PlayerPrefs.GetInt(RunnerAccountProgress.HighScoreKey), Is.EqualTo(1));
            for (int i = 1; i < game.DinoRoster.Count; i++) Assert.That(game.Shop.IsUnlocked(i), Is.False);
            yield return RunnerDisplayCapture.Capture("story-campaign-complete", 1280, 720);
        }
    }
}
