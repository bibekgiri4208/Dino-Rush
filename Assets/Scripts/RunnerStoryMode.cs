using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Campaign orchestration. The runner owns movement; this owns story screens and transitions.</summary>
    [DisallowMultipleComponent]
    public sealed class RunnerStoryMode : MonoBehaviour
    {
        private ForestRunnerGame game;
        private RunnerHud hud;
        private RunnerAudio audioCues;
        private RunnerStoryArt art;
        private RunnerStoryTrail trail;
        private RectTransform overlay, selector, results;
        private RunnerStoryCutsceneView cutscene;
        private Image preview, resultsPreview;
        private Image previewFrame, resultsPreviewFrame, journeyFill;
        private readonly Image[] resultStars = new Image[3];
        private Text chapterLabel, summary, objective, resultTitle, resultDetails, resultReward, resultBadge;
        private Button previousChapter, nextChapter, play, continueStory, replayOpening, replayEnding, claim, back;
        private Button nextLevel, retry, chapterSelect, resultClaim;
        private readonly Button[] levelButtons = new Button[3];
        private readonly List<Button> controls = new List<Button>();
        private Vector2 lastSize;
        private int chapter, selected, inputFrame = -1, lastActionFrame = -1, lastReward;
        private bool focused = true, appPaused, won;
        public RunnerStoryProgress Progress { get; private set; }
        public RunnerStoryConversation Conversation { get; } = new RunnerStoryConversation();
        public int ActiveLevelIndex { get; private set; } = -1;
        public RunnerStoryLevel ActiveLevel => IsStoryRun ? RunnerStoryCampaign.Levels[ActiveLevelIndex] : null;
        public bool IsStoryRun => ActiveLevelIndex >= 0 && ActiveLevelIndex < RunnerStoryCampaign.LevelCount;
        public bool IsModal => game != null && (game.State == ForestRunnerGame.RunState.StorySelect ||
            game.State == ForestRunnerGame.RunState.StoryScene || game.State == ForestRunnerGame.RunState.StoryResults);
        public int Tokens => trail != null ? trail.Collected : 0;
        public Transform NavigationRoot => game.State == ForestRunnerGame.RunState.StoryScene ? cutscene.Root.transform :
            game.State == ForestRunnerGame.RunState.StoryResults ? results : selector;
        public Button DefaultButton => game.State == ForestRunnerGame.RunState.StoryScene ? cutscene.AdvanceButton :
            game.State == ForestRunnerGame.RunState.StoryResults ? (won && nextLevel.interactable ? nextLevel : retry) : continueStory;

        internal void Initialize(ForestRunnerGame owner, RunnerHud runnerHud, RunnerAudio cues, Material material)
        {
            game = owner; hud = runnerHud; audioCues = cues; Progress = new RunnerStoryProgress();
            art = new RunnerStoryArt(); trail = new RunnerStoryTrail(transform, material, art);
            overlay = RunnerStoryUi.Rect("The Great Migration", hud.Root, 0, 0, 1280, 720);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<RunnerStoryTheme>().Initialize(game);
            Image blocker = overlay.gameObject.AddComponent<Image>(); blocker.color = new Color(0.015f, 0.06f, 0.065f, 0.78f); blocker.raycastTarget = true;
            BuildSelection(); BuildResults();
            cutscene = new RunnerStoryCutsceneView(overlay, game, art, audioCues, () => Invoke(AdvanceConversation), () => Invoke(SkipScene));
            RunnerUiEffects.Install(overlay, audioCues);
            overlay.gameObject.SetActive(false); hud.Story = this;
            game.GetComponentInChildren<ForestMainMenu>(true)?.InitializeStory(OpenChapterSelect);
            Layout();
        }
        private Button MakeButton(string name, Transform parent, string label, float x, float y, float w, float h, Action action) =>
            RunnerStoryUi.Button(name, parent, label, x, y, w, h, () => Invoke(action));
        private void Invoke(Action action)
        {
            if (!IsModal || !focused || appPaused || Time.frameCount <= inputFrame || lastActionFrame == Time.frameCount) return;
            lastActionFrame = Time.frameCount; action();
        }
        private void BuildSelection()
        {
            selector = RunnerStoryUi.Rect("Chapter select", overlay, 0, 0, 1120, 660);
            RunnerStoryUi.Panel("Frame", selector, 0, 0, 1120, 660);
            RunnerStoryUi.Label("Campaign title", selector, "THE GREAT MIGRATION", 0, 292, 1020, 48, 32).color = RunnerStoryUi.Gold;
            summary = RunnerStoryUi.Label("Campaign progress", selector, "", 0, 248, 1020, 30, 19);
            chapterLabel = RunnerStoryUi.Label("Chapter heading", selector, "", 0, 198, 850, 42, 25);
            previousChapter = MakeButton("Previous chapter", selector, "<", -492, 198, 72, 52, () => ChangeChapter(-1));
            nextChapter = MakeButton("Next chapter", selector, ">", 492, 198, 72, 52, () => ChangeChapter(1));
            preview = RunnerStoryUi.Box("Chapter landscape", selector, -295, 40, 430, 242, Color.white);
            preview.preserveAspect = true;
            previewFrame = RunnerStoryUi.Panel("Landscape frame", selector, -295, 40, 446, 258);
            previewFrame.transform.SetSiblingIndex(preview.transform.GetSiblingIndex());
            RunnerStoryUi.Box("Journey track", selector, 0, 224, 480, 4, new Color32(41, 66, 50, 255));
            journeyFill = RunnerStoryUi.Box("Journey progress", selector, -240, 224, 0, 4, RunnerStoryUi.Gold);
            journeyFill.rectTransform.pivot = new Vector2(0, 0.5f);
            for (int i = 0; i < 3; i++)
            {
                int local = i;
                levelButtons[i] = MakeButton("Level " + (i + 1), selector, "", 235, 112 - i * 82, 560, 72,
                    () => { selected = chapter * 3 + local; RefreshSelection(); });
            }
            objective = RunnerStoryUi.Label("Selected objective", selector, "", 0, -128, 1020, 76, 20);
            continueStory = MakeButton("Continue story", selector, "CONTINUE STORY", -350, -215, 330, 56, () => StartLevel(Progress.ContinueIndex));
            claim = MakeButton("Claim level reward", selector, "CLAIM REWARD", 0, -215, 280, 56, ClaimSelected);
            play = MakeButton("Play selected level", selector, "PLAY", 350, -215, 330, 56, () => StartLevel(selected));
            replayOpening = MakeButton("Replay opening", selector, "REPLAY OPENING", -350, -285, 330, 52, () => ReplayScene(selected, false));
            replayEnding = MakeButton("Replay ending", selector, "REPLAY ENDING", 0, -285, 280, 52, () => ReplayScene(selected, true));
            back = MakeButton("Back to modes", selector, "BACK TO MODES", 350, -285, 330, 52, BackToModes);
            RunnerStoryUi.Primary(continueStory); RunnerStoryUi.Primary(play);
        }
        private void BuildResults()
        {
            results = RunnerStoryUi.Rect("Story results", overlay, 0, 0, 900, 650);
            RunnerStoryUi.Panel("Frame", results, 0, 0, 900, 650);
            resultTitle = RunnerStoryUi.Label("Result title", results, "", 0, 270, 840, 56, 34); resultTitle.color = RunnerStoryUi.Gold;
            resultBadge = RunnerStoryUi.Label("Campaign badge", results, "", 0, 220, 840, 40, 23); resultBadge.color = RunnerStoryUi.Mint;
            resultsPreview = RunnerStoryUi.Box("Level landscape", results, 0, 95, 460, 180, Color.white); resultsPreview.preserveAspect = true;
            resultsPreviewFrame = RunnerStoryUi.Panel("Landscape frame", results, 0, 95, 476, 196);
            resultsPreviewFrame.transform.SetSiblingIndex(resultsPreview.transform.GetSiblingIndex());
            for (int i = 0; i < 3; i++)
            {
                resultStars[i] = RunnerStoryUi.Box("Result star " + i, results, (i - 1) * 62, -10, 42, 42, RunnerStoryUi.Gold);
                resultStars[i].sprite = art.Star; resultStars[i].preserveAspect = true;
            }
            resultDetails = RunnerStoryUi.Label("Earned stars", results, "", 0, -63, 800, 120, 23);
            resultReward = RunnerStoryUi.Label("First clear reward", results, "", 0, -152, 800, 40, 21); resultReward.color = RunnerStoryUi.Gold;
            nextLevel = MakeButton("Next story level", results, "NEXT LEVEL >", 225, -220, 390, 56, () => StartLevel(ActiveLevelIndex + 1));
            retry = MakeButton("Replay story level", results, "REPLAY", -225, -220, 390, 56, RetryLevel);
            chapterSelect = MakeButton("Return to chapters", results, "CHAPTER SELECT", -225, -287, 390, 52, OpenChapterSelect);
            resultClaim = MakeButton("Claim pending reward", results, "CLAIM REWARD", 225, -287, 390, 52, () =>
            { lastReward = Progress.ClaimReward(ActiveLevelIndex); RefreshResults(); });
            RunnerStoryUi.Primary(nextLevel);
            results.gameObject.SetActive(false);
        }

        public void OpenChapterSelect()
        {
            if (game == null) return;
            ShowSelection(Progress.ContinueIndex);
        }
        private void ShowSelection(int index)
        {
            Conversation.Cancel(); cutscene.Hide(); trail.Hide();
            ActiveLevelIndex = -1; hud.SetStoryLevel(null);
            game.SuspendForStory(ForestRunnerGame.RunState.StorySelect);
            selected = Mathf.Clamp(index, 0, RunnerStoryCampaign.LevelCount - 1); chapter = selected / 3;
            overlay.gameObject.SetActive(true); selector.gameObject.SetActive(true); results.gameObject.SetActive(false);
            inputFrame = Time.frameCount; RefreshSelection(); Layout(); SelectDefault();
        }
        private void ChangeChapter(int delta)
        {
            chapter = Mathf.Clamp(chapter + delta, 0, 4); selected = chapter * 3;
            RefreshSelection();
        }
        private void RefreshSelection()
        {
            int totalStars = 0; for (int i = 0; i < RunnerStoryCampaign.LevelCount; i++) totalStars += Progress.Stars(i);
            summary.text = $"{Progress.CompletedCount} / 15 LEVELS   •   {totalStars} / 45 STARS" +
                (Progress.CampaignComplete ? "   •   " + RunnerStoryProgress.CompletionTitle : "");
            journeyFill.rectTransform.sizeDelta = new Vector2(480 * Progress.CompletedCount / 15f, 4);
            chapterLabel.text = $"{chapter + 1}. {RunnerStoryCampaign.ChapterTitles[chapter].ToUpperInvariant()}";
            if (game.MapCatalog != null) preview.sprite = game.MapCatalog[game.MapCatalog.Find(RunnerStoryCampaign.MapIds[chapter])].preview;
            previousChapter.interactable = chapter > 0; nextChapter.interactable = chapter < 4;
            for (int i = 0; i < 3; i++)
            {
                int index = chapter * 3 + i; var level = RunnerStoryCampaign.Levels[index];
                string state = !Progress.Unlocked(index) ? "LOCKED" : Progress.Completed(index) ? $"{Progress.Stars(index)} / 3 STARS • COMPLETE" : "AVAILABLE";
                RunnerStoryUi.Caption(levelButtons[i], (selected == index ? "> " : "") + level.Number + "  " + level.Title + "\n" + state);
                levelButtons[i].interactable = Progress.Unlocked(index);
                RunnerStoryUi.Primary(levelButtons[i], selected == index);
            }
            RunnerStoryLevel chosen = RunnerStoryCampaign.Levels[selected];
            objective.text = chosen.Objective + "\n" + (Progress.Unlocked(selected) ?
                $"{chosen.Duration:0}s   •   3 optional cyan compasses   •   First clear: {chosen.Reward} eggs" : "Finish the previous level to open this trail.");
            play.interactable = Progress.Unlocked(selected); RunnerStoryUi.Caption(play, (Progress.Completed(selected) ? "REPLAY " : "PLAY ") + chosen.Number);
            replayOpening.interactable = Progress.Unlocked(selected); replayEnding.interactable = Progress.Completed(selected);
            claim.interactable = Progress.Completed(selected) && !Progress.RewardClaimed(selected);
            RunnerStoryUi.Caption(claim, Progress.RewardClaimed(selected) ? "REWARD CLAIMED" : "CLAIM REWARD");
            RunnerStoryUi.Caption(continueStory, Progress.CampaignComplete ? "REVISIT THE FINALE" : "CONTINUE STORY");
        }
        private void ClaimSelected()
        {
            int amount = Progress.ClaimReward(selected); RefreshSelection();
            if (amount == 0) objective.text = "Make room in your egg wallet, then claim again. Your reward is safe.";
        }
        public void StartLevel(int index)
        {
            if (game == null || !Progress.Unlocked(index)) return;
            Conversation.Cancel(); cutscene.Hide();
            ActiveLevelIndex = index; lastReward = 0; won = false;
            trail.Reset(ActiveLevel); hud.SetStoryLevel(ActiveLevel);
            game.PrepareStoryRun(ActiveLevel);
            PlayScene(ActiveLevel.Opening, false, () =>
            {
                overlay.gameObject.SetActive(false); cutscene.Hide();
                game.BeginPreparedStoryRun(); trail.Show();
            });
        }
        public void RetryLevel() { if (IsStoryRun) StartLevel(ActiveLevelIndex); }
        public void ReplayScene(int index, bool ending)
        {
            if (!Progress.Unlocked(index) || ending && !Progress.Completed(index)) return;
            ActiveLevelIndex = index; trail.Hide();
            var level = ActiveLevel;
            PlayScene(ending ? level.Ending : level.Opening, ending, () => ShowSelection(index));
        }
        private void PlayScene(RunnerStoryScene scene, bool ending, Action after)
        {
            int index = ActiveLevelIndex;
            game.SuspendForStory(ForestRunnerGame.RunState.StoryScene);
            overlay.gameObject.SetActive(true); selector.gameObject.SetActive(false); results.gameObject.SetActive(false);
            cutscene.Play(scene, index / 3); inputFrame = Time.frameCount;
            Conversation.Play(scene, () =>
            {
                cutscene.Hide(); Progress.MarkSceneSeen(index, ending); after();
            });
            Layout(); SelectDefault();
        }
        public void SkipScene() { if (game.State == ForestRunnerGame.RunState.StoryScene) Conversation.Skip(); }
        public void AdvanceConversation()
        {
            if (!Conversation.Playing) return;
            // Revealing text never skips the action; only the explicitly labeled Skip Scene does that.
            if (!Conversation.Revealed || cutscene.CanAdvance) Conversation.Advance();
        }
        internal bool AllowsEncounter(RunnerEncounter encounter, float distance, float spawnX, float speed) =>
            !IsStoryRun || trail.AllowsEncounter(encounter, distance, spawnX, speed);
        internal void StepRun(float dt, float elapsed, float distance, Rect playerBox)
        {
            if (!IsStoryRun || game.State != ForestRunnerGame.RunState.Running) return;
            if (trail.Step(dt, distance, playerBox)) { audioCues.Pickup(); hud.UpdateStoryTokens(Tokens); }
            if (elapsed < ActiveLevel.Duration) return;
            won = true; lastReward = Progress.Complete(ActiveLevelIndex, game.Lives, Tokens);
            trail.Hide();
            PlayScene(ActiveLevel.Ending, true, ShowResults);
        }
        internal void FailLevel() { if (!IsStoryRun) return; won = false; lastReward = 0; trail.Hide(); ShowResults(); }
        private void ShowResults()
        {
            Conversation.Cancel(); cutscene.Hide(); game.SuspendForStory(ForestRunnerGame.RunState.StoryResults);
            overlay.gameObject.SetActive(true); selector.gameObject.SetActive(false); results.gameObject.SetActive(true);
            inputFrame = Time.frameCount; RefreshResults(); Layout(); SelectDefault();
        }
        private void RefreshResults()
        {
            var level = ActiveLevel;
            if (level == null) return;
            resultTitle.text = won ? ActiveLevelIndex == 14 ? "A NEW BEGINNING" : "TRAIL COMPLETE!" : "TAKE A BREATHER";
            resultBadge.text = won && Progress.CampaignComplete ? "TITLE EARNED: " + RunnerStoryProgress.CompletionTitle : level.Number + " — " + level.Title;
            if (game.MapCatalog != null) resultsPreview.sprite = game.MapCatalog[game.MapCatalog.Find(level.MapId)].preview;
            resultDetails.text = won ?
                $"TRAIL FINISHED\nHEARTS {game.Lives} / 3  •  COMPASSES {Tokens} / 3\nBEST {Progress.Stars(ActiveLevelIndex)} / 3 STARS" :
                "The herd is counting on you, but the trail can wait.\nJump over debris. Duck under thorns.\nOptional compasses are never required to finish.";
            resultReward.text = lastReward > 0 ? $"FIRST-CLEAR REWARD: +{lastReward} EGGS" :
                won && !Progress.RewardClaimed(ActiveLevelIndex) ? "REWARD WAITING — MAKE ROOM IN YOUR EGG WALLET" :
                won ? "FIRST-CLEAR REWARD ALREADY CLAIMED" : "Your completed levels and best stars are safe.";
            nextLevel.interactable = won && ActiveLevelIndex + 1 < RunnerStoryCampaign.LevelCount;
            for (int i = 0; i < 3; i++) resultStars[i].color = Progress.Stars(ActiveLevelIndex) > i ? Color.white : new Color32(53, 76, 60, 255);
            RunnerStoryUi.Primary(retry, !nextLevel.interactable);
            RunnerStoryUi.Caption(retry, won ? "REPLAY LEVEL" : "RETRY LEVEL");
            resultClaim.interactable = Progress.Completed(ActiveLevelIndex) && !Progress.RewardClaimed(ActiveLevelIndex);
        }
        private void BackToModes() { game.ReturnToMenu(); hud.OpenModeSelector(); }
        internal void HandleBack()
        {
            Invoke(() =>
            {
                if (game.State == ForestRunnerGame.RunState.StoryScene) SkipScene();
                else if (game.State == ForestRunnerGame.RunState.StoryResults) OpenChapterSelect();
                else BackToModes();
            });
        }
        internal void ReadKeyboard()
        {
            if (!IsModal || !focused || appPaused || Time.frameCount <= inputFrame) return;
            Keyboard keyboard = Keyboard.current; if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { HandleBack(); return; }
            controls.Clear(); NavigationRoot.GetComponentsInChildren(false, controls);
            controls.RemoveAll(button => !button.IsActive() || !button.IsInteractable());
            Button current = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null ?
                EventSystem.current.currentSelectedGameObject.GetComponent<Button>() : null;
            if (!controls.Contains(current)) current = DefaultButton;
            int direction = keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame ? -1 :
                keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame ? 1 : 0;
            if (keyboard.tabKey.wasPressedThisFrame && keyboard.shiftKey.isPressed) direction = -1;
            if (direction != 0 && controls.Count > 0)
            {
                int index = controls.IndexOf(current); current = controls[(index + direction + controls.Count) % controls.Count];
                EventSystem.current?.SetSelectedGameObject(current.gameObject);
            }
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                if (current != null && current.IsInteractable()) current.onClick.Invoke();
        }
        internal void Tick(float dt)
        {
            if (hud.Root.rect.size != lastSize) Layout();
            if (!focused || appPaused || !IsModal) return;
            if (Conversation.Playing) { Conversation.Tick(dt); cutscene.Tick(dt, Conversation); }
        }
        private void SelectDefault() => EventSystem.current?.SetSelectedGameObject(DefaultButton.gameObject);
        private void Layout()
        {
            lastSize = hud.Root.rect.size;
            Vector2 available = lastSize.x > 0 ? lastSize : new Vector2(1280, 720);
            bool portrait = available.y > available.x;
            Vector2 design = portrait ? new Vector2(660, 1040) : new Vector2(1120, 660);
            RunnerStoryUi.Fit(selector, available, design);
            RunnerShopPresentation.Position(selector.Find("Frame"), 0, 0, design.x, design.y);
            RunnerShopPresentation.Position(selector.Find("Campaign title"), 0, portrait ? 475 : 292, design.x - 48, 48);
            RunnerShopPresentation.Position(summary.transform, 0, portrait ? 426 : 248, design.x - 40, portrait ? 48 : 30);
            RunnerShopPresentation.Position(selector.Find("Journey track"), 0, portrait ? 390 : 224, 480, 4);
            RunnerShopPresentation.Position(journeyFill.transform, -240, portrait ? 390 : 224, 480 * Progress.CompletedCount / 15f, 4);
            journeyFill.rectTransform.pivot = new Vector2(0, 0.5f);
            RunnerShopPresentation.Position(chapterLabel.transform, 0, portrait ? 352 : 198, design.x - 190, portrait ? 72 : 42);
            RunnerShopPresentation.Position(previousChapter.transform, portrait ? -280 : -492, portrait ? 352 : 198, 72, 56);
            RunnerShopPresentation.Position(nextChapter.transform, portrait ? 280 : 492, portrait ? 352 : 198, 72, 56);
            RunnerShopPresentation.Position(preview.transform, portrait ? 0 : -295, portrait ? 205 : 40, portrait ? 490 : 430, portrait ? 180 : 242);
            RunnerShopPresentation.Position(previewFrame.transform, portrait ? 0 : -295, portrait ? 205 : 40, portrait ? 506 : 446, portrait ? 196 : 258);
            for (int i = 0; i < 3; i++) RunnerShopPresentation.Position(levelButtons[i].transform, portrait ? 0 : 235,
                portrait ? 58 - i * 86 : 112 - i * 82, portrait ? 596 : 560, 76);
            RunnerShopPresentation.Position(objective.transform, 0, portrait ? -184 : -128, design.x - 60, portrait ? 106 : 76);
            RunnerShopPresentation.Position(continueStory.transform, portrait ? -154 : -350, portrait ? -286 : -215, portrait ? 294 : 330, 60);
            RunnerShopPresentation.Position(play.transform, portrait ? 154 : 350, portrait ? -286 : -215, portrait ? 294 : 330, 60);
            RunnerShopPresentation.Position(claim.transform, 0, portrait ? -356 : -215, portrait ? 596 : 280, 56);
            RunnerShopPresentation.Position(replayOpening.transform, portrait ? -154 : -350, portrait ? -426 : -285, portrait ? 294 : 330, 56);
            RunnerShopPresentation.Position(replayEnding.transform, portrait ? 154 : 0, portrait ? -426 : -285, portrait ? 294 : 280, 56);
            RunnerShopPresentation.Position(back.transform, portrait ? 0 : 350, portrait ? -493 : -285, portrait ? 596 : 330, 52);
            LayoutResults(available, portrait); cutscene?.Layout(available);
            foreach (Button button in overlay.GetComponentsInChildren<Button>(true))
            {
                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null) label.rectTransform.sizeDelta = ((RectTransform)button.transform).sizeDelta - new Vector2(32, 12);
                button.GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            }
        }
        internal void RefreshLayout() => Layout();
        private void LayoutResults(Vector2 available, bool portrait)
        {
            Vector2 design = portrait ? new Vector2(660, 1000) : new Vector2(900, 650);
            RunnerStoryUi.Fit(results, available, design);
            RunnerShopPresentation.Position(results.Find("Frame"), 0, 0, design.x, design.y);
            RunnerShopPresentation.Position(resultTitle.transform, 0, portrait ? 440 : 270, design.x - 48, 56);
            RunnerShopPresentation.Position(resultBadge.transform, 0, portrait ? 370 : 220, design.x - 48, portrait ? 70 : 40);
            RunnerShopPresentation.Position(resultsPreview.transform, 0, portrait ? 200 : 95, 460, portrait ? 240 : 180);
            RunnerShopPresentation.Position(resultsPreviewFrame.transform, 0, portrait ? 200 : 95, 476, portrait ? 256 : 196);
            for (int i = 0; i < 3; i++) RunnerShopPresentation.Position(resultStars[i].transform, (i - 1) * 64, portrait ? 47 : -22, 42, 42);
            RunnerShopPresentation.Position(resultDetails.transform, 0, portrait ? -42 : -95, design.x - 80, portrait ? 124 : 94);
            RunnerShopPresentation.Position(resultReward.transform, 0, portrait ? -140 : -157, design.x - 60, portrait ? 70 : 40);
            RunnerShopPresentation.Position(nextLevel.transform, portrait ? 0 : 225, portrait ? -235 : -220, portrait ? 596 : 390, 60);
            RunnerShopPresentation.Position(retry.transform, portrait ? 0 : -225, portrait ? -310 : -220, portrait ? 596 : 390, 60);
            RunnerShopPresentation.Position(chapterSelect.transform, portrait ? 0 : -225, portrait ? -385 : -287, portrait ? 596 : 390, 56);
            RunnerShopPresentation.Position(resultClaim.transform, portrait ? 0 : 225, portrait ? -455 : -287, portrait ? 596 : 390, 56);
        }
        internal void Leave()
        {
            Conversation.Cancel(); cutscene.Hide(); trail.Hide(); overlay.gameObject.SetActive(false);
            ActiveLevelIndex = -1; hud.SetStoryLevel(null);
        }
        internal void ReloadProgress() { Leave(); Progress.Reload(); }
        private void OnApplicationFocus(bool value) { focused = value; cutscene?.SetFocused(value && !appPaused); }
        private void OnApplicationPause(bool value) { appPaused = value; cutscene?.SetFocused(focused && !value); }
        private void OnDestroy()
        {
            Conversation.Cancel(); trail?.Dispose(); art?.Dispose();
            if (overlay != null) Destroy(overlay.gameObject);
        }
    }
}
