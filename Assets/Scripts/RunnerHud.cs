using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Safe-area UI with the authored landscape layout and a reflowed portrait layout.</summary>
    [Serializable]
    public sealed class RunnerHud
    {
        private static readonly Color Ink = new Color32(15, 35, 39, 245);
        private static readonly Color Cream = new Color32(245, 246, 222, 255);
        private static readonly Color Mint = new Color32(173, 233, 154, 255);
        private static readonly Color Muted = new Color32(174, 199, 187, 255);
        private static readonly Color PaceLit = new Color32(184, 217, 87, 255);
        private static readonly Color PaceUnlit = new Color32(26, 53, 46, 255);
        [SerializeField] private Font font;
        [SerializeField] private RectTransform root;
        [SerializeField] private List<Button> buttons = new List<Button>();
        [SerializeField] private Text score;
        [SerializeField] private Text best;
        [SerializeField] private Text stats;
        [SerializeField] private Text toast;
        [SerializeField] private Text soundLabel;
        [SerializeField] private GameObject shade;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject jumpButton;
        [SerializeField] private GameObject duckButton;
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private Text eyebrow;
        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text result;
        [SerializeField] private Text actionLabel;
        [SerializeField] private Text footer;
        [SerializeField] private Button primaryControl;
        [SerializeField] private Button soundControl;
        [SerializeField] private ForestMainMenu mainMenu;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeControl;
        [SerializeField] private Button restartControl;
        [SerializeField] private Button quitRunControl;
        [SerializeField] private Button resultsMenuControl;
        [SerializeField] private Text pausedScore;
        [SerializeField] private GameObject statsFrame;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOn;
        [SerializeField] private Sprite soundOff;
        [SerializeField] private Image[] paceSegments = new Image[0];
        [SerializeField] private GameObject controlsGuide;
        [SerializeField] private GameObject gameplayBackdrop;
        [SerializeField] private bool minimalPresentation;
        [SerializeField] private Text pausedBest;
        [SerializeField] private Text resultScore;
        [SerializeField] private Text resultBest;
        private float toastTime;
        private Text levelLabel;
        private string activeMapName = "Forest";
        private GameObject lifeBar;
        private RunnerHeartGraphic[] hearts;
        private RectTransform damagePopup;
        private CanvasGroup damageOpacity;
        private Vector3 damageOrigin;
        private float damageTime;
        private const float DamagePopupDuration = 0.95f;
        private RunnerResponsiveUi responsiveUi;
        private RunnerAudio audioCues;
        private GameObject stageDisplay;
        private Text stageTitle, stageCountdown;
        private RectTransform stageFill;
        private float progressElapsed;
        private int progressLevel = -1, progressSeconds = -1, progressLap = -1;
        private const float StageProgressWidth = 416f;
        private GameObject startPrompt;
        private Text startMapName;
        private GameObject eggDisplay;
        private Text eggCount, resultEggs;
        private float eggPulse;
        private RectTransform eggIcon;
        private RunnerEggFlights eggFlights;
        private int movementLevel;
        private Slider musicControl;
        private RunnerStoryLevel storyLevel;
        private int storyTokens;
        internal RunnerStoryMode Story { get; set; }

        public void InitializeMusic(float volume, Action<float> change)
        {
            if (musicControl == null)
            {
                musicControl = RunnerMusicUi.EnsureSlider(pausePanel != null ? pausePanel.transform : panel.transform,
                    resumeControl != null ? resumeControl : primaryControl);
                musicControl.onValueChanged.AddListener(value => change(value / 100f));
                if (pausePanel == null)
                {
                    RunnerShopPresentation.Position(musicControl.transform, 112, -111, 208, 58);
                    RunnerMusicUi.Layout(musicControl, 208, 58, 10);
                }
            }
            mainMenu?.InitializeMusic(volume, change);
            SetMusicVolume(volume);
            musicControl.gameObject.SetActive(false);
        }

        public void SetMusicVolume(float volume)
        {
            RunnerMusicUi.SetVolume(musicControl, volume);
            mainMenu?.SetMusicVolume(volume);
        }

        public bool IsBuilt => root != null;
        public bool MainMenuDialogOpen => mainMenu != null && mainMenu.DialogOpen;
        public bool RedeemDialogOpen => mainMenu != null && mainMenu.RedeemPresentation != null && mainMenu.RedeemPresentation.IsShowing;
        public bool HasMapSelector => mainMenu != null && mainMenu.HasMapSelector;
        public bool HasModeSelector => mainMenu != null && mainMenu.HasModeSelector;
        internal RectTransform Root => root;
        internal bool PreferSafeNavigation => mainMenu != null && mainMenu.PreferSafeNavigation;
        public Transform NavigationRoot(ForestRunnerGame.RunState state) => Story != null && Story.IsModal ? Story.NavigationRoot : state == ForestRunnerGame.RunState.Ready ?
            (mainMenu != null ? mainMenu.NavigationRoot : panel.transform) :
            state == ForestRunnerGame.RunState.Paused ? (pausePanel != null ? pausePanel.transform : panel.transform) :
            state == ForestRunnerGame.RunState.GameOver ? panel.transform : null;
        public Selectable NavigationDefault(ForestRunnerGame.RunState state) => Story != null && Story.IsModal ? Story.DefaultButton : state == ForestRunnerGame.RunState.Ready ?
            (mainMenu != null ? mainMenu.NavigationDefault : primaryControl) :
            state == ForestRunnerGame.RunState.Paused && resumeControl != null ? resumeControl : primaryControl;
        public void GamepadBack() => mainMenu?.GamepadBack();
        internal void SetGamepadPresentation(bool active)
        {
            Text startTitle = startPrompt != null ? startPrompt.transform.Find("Start title")?.GetComponent<Text>() : null;
            Text startHint = startPrompt != null ? startPrompt.transform.Find("Start hint")?.GetComponent<Text>() : null;
            Text pauseHint = pausePanel != null ? pausePanel.transform.Find("Pause shortcut")?.GetComponent<Text>() : null;
            if (startTitle != null) startTitle.text = active ? "PRESS A TO START" : "TAP TO START";
            if (startHint != null) startHint.text = active ? "B TO RETURN TO THE MENU" : "TAP ANYWHERE / OR PRESS SPACE";
            if (pauseHint != null) pauseHint.text = active ? "START / B TO RESUME" : "ESC / P TO RESUME";
        }

        public void SetMainMenu(ForestMainMenu menu) => mainMenu = menu;

        public void InitializeShop(RunnerShopProgress shop, RunnerAudio audio = null)
        {
            audioCues = audio;
            if (mainMenu != null) mainMenu.InitializeShop(shop, audio);
        }

        public void InitializeDinos(RunnerDinoRoster roster, int selected, Action<int> select)
        {
            if (mainMenu != null) mainMenu.InitializeDinos(roster, selected, select);
        }

        public void SetDinoSelection(int selected)
        {
            if (mainMenu != null) mainMenu.SetDinoSelection(selected);
        }

        public void FeedbackJump() => jumpButton.GetComponent<RunnerUiButtonFeedback>()?.Pulse();

        public void InitializeMaps(RunnerMapCatalog catalog, int selected, Action<int> select, Action start)
        {
            if (mainMenu != null) mainMenu.InitializeMaps(catalog, selected, select, start);
        }

        public void OpenMapSelector()
        {
            if (mainMenu != null) mainMenu.OpenMapSelector();
        }

        public void OpenModeSelector()
        {
            if (mainMenu != null) mainMenu.OpenModeSelector();
        }

        public void SetMapSelection(int selected)
        {
            if (mainMenu != null) mainMenu.SetMapSelection(selected);
        }

        public void SetMapName(string name)
        {
            activeMapName = name;
            Text brand = root.Find("Top left badge/Brand")?.GetComponent<Text>();
            if (brand != null) brand.text = name.ToUpperInvariant() + " RUN";
            Text pauseMap = pausePanel != null ? pausePanel.transform.Find("Pause eyebrow")?.GetComponent<Text>() : null;
            if (pauseMap != null) pauseMap.text = minimalPresentation ? name.ToUpperInvariant() : name.ToUpperInvariant() + " / ENDLESS RUNNER";
        }

        public void FeedbackDuck(bool held)
        {
            if (duckButton != null) duckButton.GetComponent<RunnerUiButtonFeedback>()?.SetHeld(held);
        }

        public void CloseMainMenuDialog()
        {
            if (mainMenu != null) mainMenu.CloseDialog();
        }

        public RunnerHud() { }

        public RunnerHud(Transform owner)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Forest runner UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(owner, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root = Rect("16 by 9 safe content", canvasObject.transform, Vector2.zero, new Vector2(1280, 720));

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("UI input", typeof(EventSystem));
                events.transform.SetParent(owner, false);
            }

            Image header = Box("Top left badge", root, new Vector2(-437, 293), new Vector2(338, 82), Ink);
            Box("Badge accent", header.transform, new Vector2(-166, 0), new Vector2(4, 82), Mint);
            Label("Brand", header.transform, "DINO RUSH", new Vector2(-4, 10), new Vector2(282, 36), 29,
                Cream, TextAnchor.MiddleLeft, true);
            Label("Map", header.transform, "FOREST  /  ENDLESS RUNNER", new Vector2(-4, -21), new Vector2(282, 22),
                12, Mint, TextAnchor.MiddleLeft);

            Image scores = Box("Scoreboard", root, new Vector2(264, 293), new Vector2(346, 82), Ink);
            Label("Score caption", scores.transform, "SCORE", new Vector2(-84, 23), new Vector2(140, 20), 12, Mint);
            Label("Best caption", scores.transform, "PERSONAL BEST", new Vector2(83, 23), new Vector2(150, 20), 12, Muted);
            score = Label("Score", scores.transform, "000000", new Vector2(-84, -9), new Vector2(150, 44), 31, Cream, bold: true);
            best = Label("Best", scores.transform, "000000", new Vector2(83, -9), new Vector2(150, 44), 31, Cream, bold: true);
            Box("Divider", scores.transform, Vector2.zero, new Vector2(1, 48), new Color(1, 1, 1, 0.12f));
            soundControl = Button("Sound", root, new Vector2(483, 293), new Vector2(66, 64), "SFX", false);
            soundLabel = soundControl.GetComponentInChildren<Text>();
            pauseButton = Button("Pause", root, new Vector2(565, 293), new Vector2(66, 64), "II", false).gameObject;

            stats = Label("Run stats", root, "", new Vector2(-395, -270), new Vector2(410, 28), 15, Mint,
                TextAnchor.MiddleLeft);
            footer = Label("Controls", root, "SPACE / UP / W  to jump     •     CLICK OR TAP anywhere",
                new Vector2(-78, -323), new Vector2(900, 24), 14, Muted);
            jumpButton = Button("Jump", root, new Vector2(517, -275), new Vector2(174, 68), "JUMP  ↑", true).gameObject;
            duckButton = Button("Duck", root, new Vector2(320, -275), new Vector2(174, 68), "DUCK  ↓", true).gameObject;
            duckButton.AddComponent<RunnerDuckControl>();
            toast = Label("Milestone toast", root, "", new Vector2(0, 181), new Vector2(800, 44), 22, Cream, bold: true);
            toast.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            shade = Box("Menu scrim", root, Vector2.zero, new Vector2(1280, 720), new Color(0.02f, 0.08f, 0.09f, 0.24f)).gameObject;
            panel = Box("Menu card", root, new Vector2(100, 35), new Vector2(560, 356), Ink).gameObject;
            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(Mint.r, Mint.g, Mint.b, 0.28f);
            outline.effectDistance = new Vector2(1, -1);
            Box("Card accent", panel.transform, new Vector2(0, 177), new Vector2(560, 3), Mint);
            eyebrow = Label("Eyebrow", panel.transform, "THE FOREST IS CALLING", new Vector2(0, 133), new Vector2(510, 25), 13, Mint, bold: true);
            title = Label("Title", panel.transform, "DINO RUSH", new Vector2(0, 79), new Vector2(520, 72), 57, Cream, bold: true);
            description = Label("Description", panel.transform, "One little dino. One endless forest.\nLeap over trouble. Go a little further.",
                new Vector2(0, 10), new Vector2(510, 61), 19, Muted);
            result = Label("Result", panel.transform, "", new Vector2(0, -49), new Vector2(510, 31), 16, Mint, bold: true);
            primaryControl = Button("Primary action", panel.transform, new Vector2(0, -111), new Vector2(320, 58), "LET'S RUN  →", true);
            actionLabel = primaryControl.GetComponentInChildren<Text>();
            Label("Shortcut", panel.transform, "or press SPACE / tap the forest", new Vector2(0, -155), new Vector2(510, 20), 12, Muted);
        }

        public void Initialize(Action primary, Action pause, Action jump, Action mute, Action restart, Action quitRun,
            Action<bool> duck, Sprite eggSprite = null)
        {
            CreateLifeDisplay();
            CreateStageDisplay();
            CreateStartPrompt();
            CreateEggDisplay(eggSprite);
            RunnerUiEffects.Install(root, audioCues);
            responsiveUi = new RunnerResponsiveUi(root);
            levelLabel = root.Find("Top left badge/Map")?.GetComponent<Text>();
            Bind(primaryControl, primary);
            Bind(pauseButton.GetComponent<Button>(), pause);
            Bind(jumpButton.GetComponent<Button>(), jump);
            if (duckButton != null) duckButton.GetComponent<RunnerDuckControl>().Initialize(duck);
            Bind(soundControl, mute);
            if (resumeControl != null) Bind(resumeControl, pause);
            if (restartControl != null) Bind(restartControl, restart);
            if (quitRunControl != null) Bind(quitRunControl, quitRun);
            if (resultsMenuControl != null) Bind(resultsMenuControl, quitRun);
            if (mainMenu != null) mainMenu.Initialize(primary, mute);

            EventSystem events = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (events == null) events = new GameObject("UI input", typeof(EventSystem)).GetComponent<EventSystem>();
            InputSystemUIInputModule input = events.GetComponent<InputSystemUIInputModule>();
            if (input == null) input = events.gameObject.AddComponent<InputSystemUIInputModule>();
            if (input.actionsAsset == null) input.AssignDefaultActions();
            // Keyboard shortcuts belong to the game, not a previously selected button.
            input.submit = null;
            input.cancel = null;
            input.move = null; // Controller focus is scoped to the visible page/modal by RunnerGamepadUi.
        }

        private static void Bind(Button button, Action action)
        {
            button.onClick.AddListener(() =>
            {
                EventSystem.current?.SetSelectedGameObject(null);
                action();
            });
        }

        public void UpdateScore(int points, int highScore, int cleared, float speed)
        {
            score.text = points.ToString("D6");
            best.text = Mathf.Max(points, highScore).ToString("D6");
            if (paceSegments != null && paceSegments.Length > 0)
            {
                stats.text = $"{speed / RunnerRules.Speed(0):0.0}x";
                int lit = Mathf.CeilToInt(Mathf.Clamp01(speed / RunnerRules.MaximumSpeed) * paceSegments.Length);
                for (int i = 0; i < paceSegments.Length; i++)
                    if (paceSegments[i] != null) paceSegments[i].color = i < lit ? PaceLit : PaceUnlit;
            }
            else stats.text = $"{cleared:D2} CLEARED  /  {speed / 6.8f:0.0}x PACE";
            if (pausedScore != null) pausedScore.text = minimalPresentation ? points.ToString("N0")
                : $"SCORE {points:D6} / BEST {Mathf.Max(points, highScore):D6}";
            if (pausedBest != null) pausedBest.text = Mathf.Max(points, highScore).ToString("N0");
            if (storyLevel != null)
            {
                if (pausedScore != null) pausedScore.text = Mathf.RoundToInt(Mathf.Clamp01(progressElapsed / storyLevel.Duration) * 100) + "%";
                if (pausedBest != null) pausedBest.text = storyTokens + "/3";
            }
        }

        public void UpdateLevel(int level)
        {
            movementLevel = level;
            if (duckButton != null) duckButton.SetActive(false);
            if (levelLabel != null) levelLabel.text = level > 0 ? $"LEVEL {level:00}" : "ENDLESS";
        }

        public void UpdateProgress(float elapsed)
        {
            progressElapsed = elapsed;
            if (storyLevel != null)
            {
                int phase = elapsed >= storyLevel.Duration - 6 ? 1 : 0;
                if (progressLevel != phase)
                {
                    stageTitle.text = phase == 1 ? "FINAL STRETCH!" : storyLevel.Number + " / " + storyLevel.Title.ToUpperInvariant();
                    progressLevel = phase;
                }
                if (progressSeconds != storyTokens) { stageCountdown.text = storyTokens + "/3"; progressSeconds = storyTokens; }
                stageFill.sizeDelta = new Vector2(StageProgressWidth * Mathf.Clamp01(elapsed / storyLevel.Duration), 6f);
                return;
            }
            int level = RunnerRules.Level(elapsed);
            int seconds = Mathf.CeilToInt(RunnerRules.SecondsToNextStage(elapsed));
            int lap = RunnerRules.EndlessLap(elapsed);
            if (level != progressLevel || seconds != progressSeconds || lap != progressLap)
            {
                stageTitle.text = level < 6 ? $"{level:00} / {RunnerRules.LevelHint(level)}" : $"WILD RUN / LAP {lap:00}";
                stageCountdown.text = $"{seconds:00}s";
                progressLevel = level;
                progressSeconds = seconds;
                progressLap = lap;
            }
            stageFill.sizeDelta = new Vector2(StageProgressWidth * RunnerRules.LevelProgress(elapsed), 6f);
        }

        internal void SetStoryLevel(RunnerStoryLevel level)
        {
            storyLevel = level; storyTokens = 0;
            if (stageTitle != null)
            {
                stageTitle.resizeTextForBestFit = level != null;
                stageTitle.resizeTextMinSize = 10; stageTitle.resizeTextMaxSize = 16;
                stageTitle.horizontalOverflow = level != null ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                stageTitle.verticalOverflow = level != null ? VerticalWrapMode.Truncate : VerticalWrapMode.Overflow;
            }
            if (pausePanel != null)
            {
                Text scoreCaption = pausePanel.transform.Find("Score caption")?.GetComponent<Text>();
                Text bestCaption = pausePanel.transform.Find("Best caption")?.GetComponent<Text>();
                if (scoreCaption != null) scoreCaption.text = level != null ? "PROGRESS" : "SCORE";
                if (bestCaption != null) bestCaption.text = level != null ? "TOKENS" : "BEST";
            }
            progressLevel = progressSeconds = progressLap = -1;
            if (stageTitle != null) UpdateProgress(0);
        }
        internal void UpdateStoryTokens(int count) { storyTokens = count; if (storyLevel != null) UpdateProgress(progressElapsed); }

        internal void ShowStoryScreen()
        {
            eggFlights?.Clear(); Menu(true);
            panel.SetActive(false); shade.SetActive(false); footer.gameObject.SetActive(false);
            ClearDamage();
            root.Find("Scoreboard")?.gameObject.SetActive(false);
            root.Find("Sound")?.gameObject.SetActive(false);
        }

        public void SetSound(bool muted)
        {
            soundLabel.text = muted ? "OFF" : "SFX";
            if (soundIcon != null) soundIcon.sprite = muted ? soundOff : soundOn;
            if (mainMenu != null) mainMenu.SetSound(muted);
        }

        public void ShowReady(int highScore)
        {
            eggFlights?.Clear();
            Menu(true);
            ClearDamage();
            eyebrow.text = "THE FOREST IS CALLING";
            title.text = "DINO RUSH";
            description.text = "One little dino. One endless forest.\nLeap over trouble. Go a little further.";
            result.text = $"YOUR PERSONAL BEST   {highScore:D6}";
            actionLabel.text = "LET'S RUN  →";
            if (mainMenu != null) mainMenu.Show();
        }

        public void ShowRunning()
        {
            Menu(false);
            root.Find("Scoreboard")?.gameObject.SetActive(true);
            if (storyLevel != null) root.Find("Scoreboard")?.gameObject.SetActive(false);
            root.Find("Sound")?.gameObject.SetActive(true);
            if (damagePopup != null) damagePopup.gameObject.SetActive(damageTime > 0f);
            footer.text = "SPACE / UP / W : JUMP    DOWN / S : DUCK / FAST DROP    ESC / P : PAUSE";
        }

        public void ShowAwaitingStart(string mapName)
        {
            eggFlights?.Clear();
            Menu(true);
            panel.SetActive(false);
            shade.SetActive(false);
            footer.gameObject.SetActive(false);
            root.Find("Scoreboard")?.gameObject.SetActive(false);
            root.Find("Sound")?.gameObject.SetActive(false);
            if (damagePopup != null) damagePopup.gameObject.SetActive(false);
            startMapName.text = mapName.ToUpperInvariant();
            startPrompt.SetActive(true);
        }

        public void ShowPaused()
        {
            Menu(true);
            if (musicControl != null) musicControl.gameObject.SetActive(true);
            if (damagePopup != null) damagePopup.gameObject.SetActive(false);
            if (pausePanel != null)
            {
                panel.SetActive(false);
                pausePanel.SetActive(true);
                footer.text = storyLevel != null ? "STORY PAUSED / RESTART REPLAYS THIS LEVEL" : "RUN PAUSED  /  YOUR BEST SCORE IS SAVED";
                return;
            }
            eyebrow.text = "TAKE A BREATHER";
            title.text = "PAUSED";
            description.text = "The forest can wait.\nYour run is right where you left it.";
            result.text = "ESC / P  TO RESUME";
            if (musicControl != null) RunnerShopPresentation.Position(primaryControl.transform, -112, -111, 208, 58);
            actionLabel.text = "KEEP RUNNING  →";
        }

        public void ShowGameOver(int points, int highScore, bool record)
        {
            eggFlights?.Clear();
            Menu(true);
            if (minimalPresentation)
            {
                eyebrow.text = record ? "NEW BEST" : activeMapName.ToUpperInvariant();
                eyebrow.color = record ? new Color32(255, 210, 117, 255) : new Color32(174, 203, 152, 255);
                title.text = "GAME OVER";
                resultScore.text = points.ToString("N0");
                resultBest.text = Mathf.Max(points, highScore).ToString("N0");
                actionLabel.text = "PLAY AGAIN";
                return;
            }
            eyebrow.text = record ? "A NEW PERSONAL BEST!" : "EVERY RUN IS A FRESH START";
            title.text = "NICE RUN!";
            description.text = $"SCORE {points:D6} / BEST {highScore:D6}\nONE MORE JUMP. ONE MORE TRY.";
            result.text = record ? "YOUR FURTHEST ADVENTURE YET!" : "CAN YOU BEAT YOUR BEST?";
            actionLabel.text = "RUN AGAIN";
            footer.text = "SPACE / ENTER : RETRY    M : SOUND";
        }

        private void Menu(bool visible)
        {
            if (musicControl != null)
            {
                musicControl.gameObject.SetActive(false);
                if (pausePanel == null) RunnerShopPresentation.Position(primaryControl.transform, 0, -111, 320, 58);
            }
            if (startPrompt != null) startPrompt.SetActive(false);
            if (mainMenu != null) mainMenu.Hide();
            if (pausePanel != null) pausePanel.SetActive(false);
            shade.SetActive(visible);
            panel.SetActive(visible);
            // Gameplay stays button-free: tap to jump, swipe down and hold to duck on touch.
            jumpButton.SetActive(false);
            if (duckButton != null) duckButton.SetActive(false);
            pauseButton.SetActive(!visible);
            if (lifeBar != null) lifeBar.SetActive(!visible);
            if (stageDisplay != null) stageDisplay.SetActive(!visible);
            if (eggDisplay != null) eggDisplay.SetActive(!visible);
            eggFlights?.SetVisible(!visible);
            stats.gameObject.SetActive(!visible);
            if (statsFrame != null) statsFrame.SetActive(!visible);
            if (controlsGuide != null) controlsGuide.SetActive(!visible && !minimalPresentation);
            if (gameplayBackdrop != null) gameplayBackdrop.SetActive(!visible);
            footer.gameObject.SetActive(!minimalPresentation && (visible || controlsGuide == null));
            if (minimalPresentation)
            {
                foreach (string name in new[] { "Scoreboard", "Sound" })
                    root.Find(name)?.gameObject.SetActive(!visible);
            }
            // Keep legacy scene bindings, but never restore the retired map/level panel on resume.
            root.Find("Top left badge")?.gameObject.SetActive(false);
            toast.text = "";
            toastTime = 0f;
            footer.text = "SPACE / UP / W : JUMP    CLICK / TAP : JUMP";
        }

        public void Toast(string message)
        {
            toast.text = message;
            toastTime = 2.4f;
            toast.GetComponent<RunnerUiReveal>()?.Replay();
        }

        public void Tick(float dt)
        {
            toastTime = Mathf.Max(0f, toastTime - dt);
            Color color = Cream;
            color.a = Mathf.Clamp01(toastTime);
            toast.color = color;
            if (eggCount != null && eggDisplay.activeInHierarchy)
            {
                eggFlights?.Tick(dt);
                eggPulse = Mathf.Max(0f, eggPulse - dt);
                RenderEggPulse();
            }
        }

        private void RenderEggPulse()
        {
            if (eggCount == null) return;
            float bounce = Mathf.Sin(eggPulse / 0.22f * Mathf.PI);
            eggCount.transform.localScale = Vector3.one * (1 + bounce * 0.08f);
            eggCount.color = Color.Lerp(Cream, new Color32(255, 218, 119, 255), eggPulse / 0.22f);
            if (eggIcon == null) return;
            eggIcon.localScale = new Vector3(1 + bounce * 0.12f, 1 - bounce * 0.08f, 1);
            eggIcon.localRotation = Quaternion.Euler(0, 0, bounce * -10f);
        }

        private void CreateEggDisplay(Sprite sprite)
        {
            if (eggDisplay != null) return;
            Image frame = Box("Egg counter", root, new Vector2(-492, 250), new Vector2(224, 42), Ink);
            Image theme = lifeBar.GetComponent<Image>();
            if (theme.sprite != null)
            {
                frame.sprite = theme.sprite;
                frame.type = theme.type;
                frame.color = theme.color;
            }
            eggDisplay = frame.gameObject;
            Image icon = Box("Egg icon", frame.transform, new Vector2(-86, 0), new Vector2(28, 34), Color.white);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            eggIcon = icon.rectTransform;
            Label("Egg caption", frame.transform, "EGGS", new Vector2(-32, 0), new Vector2(64, 26), 12, Cream);
            eggCount = Label("Egg count", frame.transform, "000", Vector2.zero, new Vector2(84, 26), 14,
                Cream, TextAnchor.MiddleRight);
            // Keep the right edge inset even at the peak of the pickup pulse:
            // a right-side pivot makes the number grow inward, not through the border.
            eggCount.rectTransform.anchorMin = eggCount.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            eggCount.rectTransform.pivot = new Vector2(1f, 0.5f);
            eggCount.rectTransform.anchoredPosition = new Vector2(-20f, 0f);
            eggCount.horizontalOverflow = HorizontalWrapMode.Wrap;
            eggCount.verticalOverflow = VerticalWrapMode.Truncate;
            eggCount.resizeTextForBestFit = true;
            eggCount.resizeTextMinSize = 12;
            eggCount.resizeTextMaxSize = 14;
            if (minimalPresentation)
                resultEggs = Label("Collected eggs", panel.transform, "EGGS COLLECTED: 000", new Vector2(0, -12),
                    new Vector2(384, 20), 12, new Color32(255, 218, 119, 255));
            if (sprite != null) eggFlights = new RunnerEggFlights(root, icon.rectTransform, sprite, () => eggPulse = 0.22f, font);
            eggDisplay.SetActive(false);
        }

        public void CollectEgg(int count, Vector3 worldPosition, Camera camera)
        {
            // Record awards immediately; the flight and arrival pulse are cosmetic,
            // so pausing or ending the run can never lose an already collected egg.
            UpdateEggs(count, false);
            if (eggFlights != null && eggDisplay.activeInHierarchy && camera != null)
                eggFlights.Play(worldPosition, camera);
        }

        public void UpdateEggs(int count, bool pulse = true)
        {
            if (eggCount == null) return;
            string caption = count > 99999 ? "99999+" : count.ToString("D3");
            if (count > 0)
            {
                if (pulse) eggPulse = 0.22f;
            }
            else
            {
                eggFlights?.Clear();
                eggPulse = 0f;
                eggCount.transform.localScale = Vector3.one;
                eggCount.color = Cream;
                if (eggIcon != null)
                {
                    eggIcon.localScale = Vector3.one;
                    eggIcon.localRotation = Quaternion.identity;
                }
            }
            eggCount.text = caption;
            if (resultEggs != null) resultEggs.text = "EGGS COLLECTED: " + caption;
        }

        private void CreateLifeDisplay()
        {
            if (lifeBar != null) return;
            Image frame = Box("Life hearts", root, new Vector2(-492, 309), new Vector2(224, 46), Ink);
            Image themedFrame = root.Find("Trail stats frame")?.GetComponent<Image>();
            if (themedFrame != null && themedFrame.sprite != null)
            {
                frame.sprite = themedFrame.sprite;
                frame.type = themedFrame.type;
                frame.color = themedFrame.color;
            }
            lifeBar = frame.gameObject;
            Label("Life caption", frame.transform, "LIFE", new Vector2(-76, 0), new Vector2(48, 26), 12, Cream);
            hearts = new RunnerHeartGraphic[RunnerHealth.MaximumLives];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = Rect("Heart " + (i + 1), frame.transform,
                    new Vector2(Mathf.Lerp(-24, 84, hearts.Length == 1 ? 0.5f : (float)i / (hearts.Length - 1)), 0),
                    new Vector2(28, 28)).gameObject.AddComponent<RunnerHeartGraphic>();
                hearts[i].raycastTarget = false;
            }
            lifeBar.SetActive(false);

            damagePopup = Rect("Damage popup", root, Vector2.zero, new Vector2(104, 44));
            damageOpacity = damagePopup.gameObject.AddComponent<CanvasGroup>();
            damageOpacity.interactable = damageOpacity.blocksRaycasts = false;
            Text amount = Label("Lost life", damagePopup, "-1", new Vector2(-16, 0), new Vector2(60, 36), 24, Cream);
            Shadow shadow = amount.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(42, 24, 35, 255);
            shadow.effectDistance = new Vector2(2, -2);
            RunnerHeartGraphic heart = Rect("Lost heart", damagePopup, new Vector2(32, 0), new Vector2(30, 30))
                .gameObject.AddComponent<RunnerHeartGraphic>();
            heart.raycastTarget = false;
            damagePopup.gameObject.SetActive(false);
        }

        private void CreateStageDisplay()
        {
            if (stageDisplay != null) return;
            Image frame = Box("Stage progress", root, new Vector2(-64, 309), new Vector2(448, 52), Ink);
            Image theme = lifeBar.GetComponent<Image>();
            if (theme.sprite != null)
            {
                frame.sprite = theme.sprite;
                frame.type = theme.type;
                frame.color = theme.color;
            }
            stageDisplay = frame.gameObject;
            stageTitle = Label("Stage name", frame.transform, "", new Vector2(-24, 7), new Vector2(368, 22), 16,
                Cream, TextAnchor.MiddleLeft);
            stageCountdown = Label("Next stage", frame.transform, "", new Vector2(184, 7), new Vector2(48, 22), 16,
                Mint, TextAnchor.MiddleRight);
            Box("Progress track", frame.transform, new Vector2(0, -13), new Vector2(StageProgressWidth, 6), PaceUnlit);
            stageFill = Box("Progress fill", frame.transform, new Vector2(-StageProgressWidth * 0.5f, -13), new Vector2(0, 6), PaceLit).rectTransform;
            stageFill.pivot = new Vector2(0, 0.5f);
            UpdateProgress(0f);
            stageDisplay.SetActive(false);
        }

        private void CreateStartPrompt()
        {
            if (startPrompt != null) return;
            Image frame = Box("Tap to start", root, new Vector2(0, 70), new Vector2(600, 144), Ink);
            Image theme = lifeBar.GetComponent<Image>();
            if (theme.sprite != null)
            {
                frame.sprite = theme.sprite;
                frame.type = theme.type;
                frame.color = theme.color;
            }
            startPrompt = frame.gameObject;
            startMapName = Label("Selected map", frame.transform, "", new Vector2(0, 42), new Vector2(560, 24), 12, Mint);
            Label("Start title", frame.transform, "TAP TO START", Vector2.zero, new Vector2(560, 48), 32, Cream);
            Label("Start hint", frame.transform, "TAP ANYWHERE / OR PRESS SPACE", new Vector2(0, -42), new Vector2(560, 24), 12, Muted);
            startPrompt.SetActive(false);
        }

        public void UpdateLives(int lives)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++) hearts[i].Filled = i < lives;
        }

        public void ApplyScreenLayout(RunnerScreenLayout layout)
        {
            responsiveUi?.Apply(layout);
            Story?.RefreshLayout();
            root.GetComponent<RunnerSlashHud>()?.ApplyLayout(layout.Portrait);
            if (stageFill != null) UpdateProgress(progressElapsed);
            if (eggFlights != null)
            {
                Canvas.ForceUpdateCanvases();
                eggFlights.RefreshLayout();
                RenderEggPulse();
            }
        }

        public void ShowDamage(Vector3 worldPosition, Camera camera)
        {
            damageOrigin = worldPosition;
            damageTime = DamagePopupDuration;
            damageOpacity.alpha = 1f;
            damagePopup.localScale = Vector3.one * 1.18f;
            damagePopup.gameObject.SetActive(true);
            PositionDamage(camera, 0f);
        }

        public void TickDamage(float dt, Camera camera)
        {
            if (damageTime <= 0f) return;
            damageTime = Mathf.Max(0f, damageTime - dt);
            if (damageTime == 0f)
            {
                damagePopup.gameObject.SetActive(false);
                return;
            }
            float age = 1f - damageTime / DamagePopupDuration;
            PositionDamage(camera, age);
            damageOpacity.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, age));
            damagePopup.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, Mathf.Clamp01(age * 6f));
        }

        private void PositionDamage(Camera camera, float age)
        {
            Canvas canvas = root.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root,
                camera.WorldToScreenPoint(damageOrigin), uiCamera, out Vector2 point)) return;
            point.y += 54f * age;
            point.x = Mathf.Clamp(point.x, root.rect.xMin + 60f, root.rect.xMax - 60f);
            point.y = Mathf.Clamp(point.y, root.rect.yMin + 26f, root.rect.yMax - 26f);
            damagePopup.anchoredPosition = point;
        }

        public void ClearDamage()
        {
            damageTime = 0f;
            if (damagePopup != null) damagePopup.gameObject.SetActive(false);
        }

        public void RegisterButton(Button button)
        {
            if (button != null && !buttons.Contains(button)) buttons.Add(button);
        }

        public bool IsOverButton(Vector2 screenPoint)
        {
            foreach (Button button in buttons)
            {
                if (button.gameObject.activeInHierarchy &&
                    RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, screenPoint))
                    return true;
            }
            return false;
        }

        private Button Button(string name, Transform parent, Vector2 position, Vector2 size, string text,
            bool primary)
        {
            Image image = Box(name, parent, position, size, primary ? Mint : Ink);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.85f, 1f, 0.82f);
            colors.pressedColor = new Color(0.65f, 0.8f, 0.6f);
            button.colors = colors;
            Label(name + " label", image.transform, text, Vector2.zero, size - new Vector2(10, 4),
                primary ? 20 : 17, primary ? Ink : Cream, bold: true);
            buttons.Add(button);
            return button;
        }

        private Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            RectTransform rect = Rect(name, parent, position, size);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            Image image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
