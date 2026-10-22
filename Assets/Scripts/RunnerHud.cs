using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>A fixed 1280x720 UI that fits the same letterboxed viewport as the world.</summary>
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
        private float toastTime;

        public bool IsBuilt => root != null;
        public bool MainMenuDialogOpen => mainMenu != null && mainMenu.DialogOpen;

        public void SetMainMenu(ForestMainMenu menu) => mainMenu = menu;

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
            Action<bool> duck)
        {
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
                int lit = Mathf.CeilToInt(Mathf.Clamp01(speed / RunnerRules.Speed(100)) * paceSegments.Length);
                for (int i = 0; i < paceSegments.Length; i++)
                    if (paceSegments[i] != null) paceSegments[i].color = i < lit ? PaceLit : PaceUnlit;
            }
            else stats.text = $"{cleared:D2} CLEARED  /  {speed / 6.8f:0.0}x PACE";
            if (pausedScore != null) pausedScore.text = $"SCORE {points:D6} / BEST {Mathf.Max(points, highScore):D6}";
        }

        public void SetSound(bool muted)
        {
            soundLabel.text = muted ? "OFF" : "SFX";
            if (soundIcon != null) soundIcon.sprite = muted ? soundOff : soundOn;
            if (mainMenu != null) mainMenu.SetSound(muted);
        }

        public void ShowReady(int highScore)
        {
            Menu(true);
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
            footer.text = "SPACE / UP / W : JUMP    DOWN / S : DUCK / FAST DROP    ESC / P : PAUSE";
        }

        public void ShowPaused()
        {
            Menu(true);
            if (pausePanel != null)
            {
                panel.SetActive(false);
                pausePanel.SetActive(true);
                footer.text = "RUN PAUSED  /  YOUR BEST SCORE IS SAVED";
                return;
            }
            eyebrow.text = "TAKE A BREATHER";
            title.text = "PAUSED";
            description.text = "The forest can wait.\nYour run is right where you left it.";
            result.text = "ESC / P  TO RESUME";
            actionLabel.text = "KEEP RUNNING  →";
        }

        public void ShowGameOver(int points, int highScore, bool record)
        {
            Menu(true);
            eyebrow.text = record ? "A NEW PERSONAL BEST!" : "EVERY RUN IS A FRESH START";
            title.text = "NICE RUN!";
            description.text = $"SCORE {points:D6} / BEST {highScore:D6}\nONE MORE JUMP. ONE MORE TRY.";
            result.text = record ? "YOUR FURTHEST ADVENTURE YET!" : "CAN YOU BEAT YOUR BEST?";
            actionLabel.text = "RUN AGAIN";
            footer.text = "SPACE / ENTER : RETRY    M : SOUND";
        }

        private void Menu(bool visible)
        {
            if (mainMenu != null) mainMenu.Hide();
            if (pausePanel != null) pausePanel.SetActive(false);
            shade.SetActive(visible);
            panel.SetActive(visible);
            jumpButton.SetActive(!visible);
            if (duckButton != null) duckButton.SetActive(!visible);
            pauseButton.SetActive(!visible);
            stats.gameObject.SetActive(!visible);
            if (statsFrame != null) statsFrame.SetActive(!visible);
            if (controlsGuide != null) controlsGuide.SetActive(!visible);
            if (gameplayBackdrop != null) gameplayBackdrop.SetActive(!visible);
            footer.gameObject.SetActive(visible || controlsGuide == null);
            toast.text = "";
            toastTime = 0f;
            footer.text = "SPACE / UP / W : JUMP    CLICK / TAP : JUMP";
        }

        public void Toast(string message)
        {
            toast.text = message;
            toastTime = 2.4f;
        }

        public void Tick(float dt)
        {
            toastTime = Mathf.Max(0f, toastTime - dt);
            Color color = Cream;
            color.a = Mathf.Clamp01(toastTime);
            toast.color = color;
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
