using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Themed conversation controls around a directed, animated scene. Presentation never advances gameplay.</summary>
    internal sealed class RunnerStoryCutsceneView
    {
        private readonly ForestRunnerGame game;
        private readonly RunnerAudio audio;
        private readonly RectTransform panel, theater, dialogue;
        private readonly Image frame, sceneFrame, dialogueFrame, portrait;
        private readonly Text title, speaker, words, hint;
        private readonly AudioSource music;
        private readonly RunnerStoryStage stage;
        public GameObject Root => panel.gameObject;
        public Button AdvanceButton { get; }
        public bool CanAdvance => stage.ActionFinished;
        private readonly Button skipButton;
        private RunnerStoryScene scene;
        private RunnerStoryLine currentLine;
        private int renderedCharacters = -1;

        internal RunnerStoryCutsceneView(Transform parent, ForestRunnerGame game, RunnerStoryArt art, RunnerAudio audio,
            Action advance, Action skip)
        {
            this.game = game; this.audio = audio;
            panel = RunnerStoryUi.Rect("Story cutscene", parent, 0, 0, 1180, 676);
            frame = RunnerStoryUi.Panel("Frame", panel, 0, 0, 1180, 676);
            title = RunnerStoryUi.Label("Scene title", panel, "", 0, 302, 1070, 36, 26); title.color = RunnerStoryUi.Gold;
            sceneFrame = RunnerStoryUi.Panel("Scene moss frame", panel, 0, 75, 1134, 410);
            theater = RunnerStoryUi.Rect("Sprite theater", panel, 0, 75, 1114, 390);
            theater.gameObject.AddComponent<RectMask2D>();
            stage = new RunnerStoryStage(theater, game, art);
            dialogue = RunnerStoryUi.Rect("Conversation", panel, 0, -205, 1124, 162);
            dialogueFrame = RunnerStoryUi.Panel("Forest dialogue box", dialogue, 0, 0, 1124, 162);
            portrait = RunnerStoryUi.Box("Speaking character portrait", dialogue, -490, 0, 90, 90, Color.white);
            portrait.preserveAspect = true;
            speaker = RunnerStoryUi.Label("Speaker", dialogue, "", 36, 48, 900, 26, 23, TextAnchor.MiddleLeft);
            speaker.color = RunnerStoryUi.Gold;
            words = RunnerStoryUi.Label("Dialogue text", dialogue, "", 36, -10, 900, 82, 26, TextAnchor.UpperLeft);
            words.resizeTextForBestFit = false; words.fontSize = 17; words.lineSpacing = 1.55f;
            hint = RunnerStoryUi.Label("Continue hint", panel, "", 0, -264, 1000, 20, 16);
            hint.color = RunnerStoryUi.Mint;
            AdvanceButton = RunnerStoryUi.Button("Continue conversation", panel, "CONTINUE", 374, -302, 340, 52, advance);
            RunnerStoryUi.Primary(AdvanceButton);
            skipButton = RunnerStoryUi.Button("Skip scene", panel, "SKIP SCENE", -390, -302, 300, 52, skip);
            var musicNode = new GameObject("Story cutscene music"); musicNode.transform.SetParent(game.transform, false);
            music = musicNode.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true; music.volume = 0;
            Root.SetActive(false);
        }
        internal void Play(RunnerStoryScene value, int chapter)
        {
            scene = value; currentLine = null; renderedCharacters = -1;
            title.text = value.Title.ToUpperInvariant(); words.text = ""; speaker.text = ""; hint.text = "";
            Root.SetActive(true); stage.Play(value, chapter);
            music.clip = game.StoryMusicClip;
            if (music.clip != null) { music.volume = 0; music.Play(); }
        }
        internal void Hide() { Root.SetActive(false); music.Stop(); music.clip = null; }
        internal void SetFocused(bool focused) { if (focused && Root.activeSelf) music.UnPause(); else music.Pause(); }
        internal void Tick(float dt, RunnerStoryConversation conversation)
        {
            if (!Root.activeSelf || !conversation.Playing) return;
            RunnerStoryLine line = conversation.Line;
            if (currentLine != line)
            {
                currentLine = line; renderedCharacters = -1;
                speaker.text = line.Speaker == "Narrator" ? "THE GREAT MIGRATION" : line.Speaker.ToUpperInvariant();
                stage.SetSpeaker(line.Speaker);
                if (!string.IsNullOrEmpty(line.Shot))
                {
                    bool changed = stage.Shot != line.Shot; stage.BeginShot(line.Shot);
                    if (changed && line.Shot == "quake") audio?.Crash();
                    if (changed && line.Shot == "gate") audio?.Milestone();
                    if (changed && line.Shot == "hatch") audio?.EggHatch();
                }
                portrait.gameObject.SetActive(line.Speaker != "Narrator");
                if (line.Speaker == "Hatchling") portrait.sprite = stage.Shot == "hatch" ? game.DinoRoster[0].Portrait : null;
                else portrait.sprite = game.DinoRoster[game.DinoRoster.Find(line.Speaker.ToLowerInvariant())].Portrait;
                if (line.Speaker == "Hatchling" && portrait.sprite == null) portrait.gameObject.SetActive(false);
            }
            stage.Tick(dt);
            int characters = Mathf.Clamp(Mathf.FloorToInt(conversation.Characters), 0, line.Text.Length);
            if (characters != renderedCharacters) { words.text = line.Text.Substring(0, characters); renderedCharacters = characters; }
            string status = conversation.Revealed ? stage.ActionFinished ? "CONFIRM TO CONTINUE" : "WATCH THE SCENE  •  SKIP IS ALWAYS AVAILABLE" : "CONFIRM TO REVEAL TEXT";
            hint.text = $"{conversation.Index + 1}/{scene.Lines.Length}  •  {status}";
            RunnerStoryUi.Caption(AdvanceButton, !conversation.Revealed ? "REVEAL TEXT" : stage.ActionFinished ? "CONTINUE >" : "WATCHING...");
            music.volume = Mathf.MoveTowards(music.volume, game.MusicVolume * 0.10f, dt * 0.1f);
        }
        internal void Layout(Vector2 available)
        {
            bool portraitLayout = available.y > available.x;
            Vector2 design = portraitLayout ? new Vector2(660, 1050) : new Vector2(1180, 676);
            RunnerStoryUi.Fit(panel, available, design);
            RunnerShopPresentation.Position(frame.transform, 0, 0, design.x, design.y);
            RunnerShopPresentation.Position(title.transform, 0, portraitLayout ? 473 : 302, design.x - 80, portraitLayout ? 60 : 36);
            float sceneY = portraitLayout ? 183 : 75, sceneH = portraitLayout ? 432 : 390;
            RunnerShopPresentation.Position(sceneFrame.transform, 0, sceneY, design.x - 46, sceneH + 20);
            RunnerShopPresentation.Position(theater, 0, sceneY, design.x - 66, sceneH);
            RunnerShopPresentation.Position(dialogue, 0, portraitLayout ? -200 : -183, design.x - 56, portraitLayout ? 262 : 172);
            RunnerShopPresentation.Position(dialogueFrame.transform, 0, 0, design.x - 56, portraitLayout ? 262 : 172);
            RunnerShopPresentation.Position(portrait.transform, portraitLayout ? -238 : -490, portraitLayout ? 76 : 8, portraitLayout ? 66 : 88, portraitLayout ? 66 : 88);
            RunnerShopPresentation.Position(speaker.transform, portraitLayout ? 42 : 44, portraitLayout ? 83 : 52, portraitLayout ? 398 : 890, 30);
            RunnerShopPresentation.Position(words.transform, portraitLayout ? 0 : 44, portraitLayout ? -25 : -14, design.x - (portraitLayout ? 100 : 250), portraitLayout ? 146 : 94);
            words.fontSize = portraitLayout ? 16 : 17;
            RunnerShopPresentation.Position(hint.transform, 0, portraitLayout ? -357 : -281, design.x - 100, portraitLayout ? 42 : 20);
            RunnerShopPresentation.Position(AdvanceButton.transform, portraitLayout ? 0 : 375, portraitLayout ? -421 : -310, portraitLayout ? 548 : 340, portraitLayout ? 64 : 48);
            RunnerShopPresentation.Position(skipButton.transform, portraitLayout ? 0 : -390, portraitLayout ? -493 : -310, portraitLayout ? 548 : 300, portraitLayout ? 56 : 48);
            stage.Layout();
        }
    }
}
