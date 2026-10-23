using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DinoRush
{
    /// <summary>The scene-authored title screen, map/dino selectors, and settings dialogs.</summary>
    public sealed class ForestMainMenu : MonoBehaviour
    {
        [SerializeField] private Button play;
        [SerializeField] private Button customize;
        [SerializeField] private Button options;
        [SerializeField] private Button quit;
        [SerializeField] private Button sound;
        [SerializeField] private Button back;
        [SerializeField] private Button confirmQuit;
        [SerializeField] private Button optionsSound;
        [SerializeField] private GameObject choices;
        [SerializeField] private GameObject logo;
        [SerializeField] private GameObject dialog;
        [SerializeField] private GameObject customizePage;
        [SerializeField] private GameObject optionsPage;
        [SerializeField] private GameObject quitPage;
        [SerializeField] private GameObject[] gameplayChrome;
        [SerializeField] private Image speaker;
        [SerializeField] private Sprite speakerOn;
        [SerializeField] private Sprite speakerOff;
        [SerializeField] private Text soundCaption;
        [SerializeField] private Text optionsSoundCaption;
        [SerializeField] private Text dialogTitle;
        [SerializeField] private Button[] dinoChoices = new Button[0];
        [SerializeField] private Image[] dinoPortraits = new Image[0];
        [SerializeField] private Text[] dinoCaptions = new Text[0];
        [SerializeField] private Text selectedDinoLabel;
        [SerializeField] private Sprite selectedDinoFrame;
        [SerializeField] private Sprite unselectedDinoFrame;
        [SerializeField] private GameObject mapPage;
        [SerializeField] private Button[] mapChoices = new Button[0];
        [SerializeField] private Image[] mapPreviews = new Image[0];
        [SerializeField] private Text[] mapCaptions = new Text[0];
        [SerializeField] private Text mapStatus;
        [SerializeField] private Text customizeMapLabel;
        [SerializeField] private Button startMap;
        [SerializeField] private Button mapBack;
        private RunnerMapCatalog mapCatalog;
        private bool mapsBound;
        private RunnerDinoRoster dinoRoster;
        private bool dinosBound;
        private bool bound;

        public bool DialogOpen => (dialog != null && dialog.activeInHierarchy) || (mapPage != null && mapPage.activeInHierarchy);
        public bool HasMapSelector => mapPage != null;

        public void OpenMapSelector()
        {
            if (mapPage == null) return;
            choices.SetActive(false);
            logo.SetActive(false);
            dialog.SetActive(false);
            mapPage.SetActive(true);
        }

        public void InitializeMaps(RunnerMapCatalog catalog, int selected, Action<int> select, Action start)
        {
            mapCatalog = catalog;
            if (catalog == null || catalog.Count == 0 || mapChoices.Length != catalog.Count) return;
            Text startLabel = startMap != null ? startMap.GetComponentInChildren<Text>(true) : null;
            if (startLabel != null) startLabel.text = "CONTINUE";
            if (!mapsBound)
            {
                mapsBound = true;
                for (int i = 0; i < mapChoices.Length; i++)
                {
                    int index = i;
                    Bind(mapChoices[i], () => select(index));
                }
                Bind(startMap, start);
                Bind(mapBack, CloseDialog);
            }
            SetMapSelection(selected);
        }

        public void SetMapSelection(int selected)
        {
            if (mapCatalog == null || selected < 0 || selected >= mapCatalog.Count || mapChoices.Length != mapCatalog.Count) return;
            for (int i = 0; i < mapChoices.Length; i++)
            {
                mapPreviews[i].sprite = mapCatalog[i].preview;
                mapCaptions[i].text = mapCatalog[i].displayName.ToUpperInvariant();
                mapChoices[i].GetComponent<Image>().sprite = i == selected ? selectedDinoFrame : unselectedDinoFrame;
            }
            string name = mapCatalog[selected].displayName.ToUpperInvariant();
            mapStatus.text = name + " SELECTED / READY TO RUN";
            if (customizeMapLabel != null) customizeMapLabel.text = "MAP: " + name;
        }

        public void Initialize(Action start, Action toggleSound)
        {
            if (bound) return;
            bound = true;
            Bind(play, start);
            Bind(customize, () => OpenDialog("CHOOSE YOUR DINO", customizePage));
            Bind(options, () => OpenDialog("OPTIONS", optionsPage));
            Bind(quit, () => OpenDialog("QUIT GAME?", quitPage));
            Bind(back, CloseDialog);
            Bind(sound, toggleSound);
            Bind(optionsSound, toggleSound);
            Bind(confirmQuit, QuitGame);
        }

        public void InitializeDinos(RunnerDinoRoster roster, int selected, Action<int> select)
        {
            dinoRoster = roster;
            if (roster == null || roster.Count == 0 || dinoChoices.Length != roster.Count) return;
            if (!dinosBound)
            {
                dinosBound = true;
                for (int i = 0; i < dinoChoices.Length; i++)
                {
                    int index = i;
                    Bind(dinoChoices[i], () => select(index));
                }
            }
            SetDinoSelection(selected);
        }

        public void SetDinoSelection(int selected)
        {
            if (dinoRoster == null || selected < 0 || selected >= dinoRoster.Count || dinoChoices.Length != dinoRoster.Count) return;
            for (int i = 0; i < dinoChoices.Length; i++)
            {
                dinoPortraits[i].sprite = dinoRoster[i].Portrait;
                dinoCaptions[i].text = dinoRoster[i].DisplayName.ToUpperInvariant();
                dinoChoices[i].GetComponent<Image>().sprite = i == selected ? selectedDinoFrame : unselectedDinoFrame;
            }
            selectedDinoLabel.text = dinoRoster[selected].DisplayName.ToUpperInvariant() + " DINO SELECTED";
        }

        private static void Bind(Button button, Action action)
        {
            button.onClick.AddListener(() =>
            {
                EventSystem.current?.SetSelectedGameObject(null);
                action();
            });
        }

        public void Show()
        {
            gameObject.SetActive(true);
            foreach (GameObject item in gameplayChrome) if (item != null) item.SetActive(false);
            CloseDialog();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            foreach (GameObject item in gameplayChrome) if (item != null) item.SetActive(true);
        }

        public void SetSound(bool muted)
        {
            speaker.sprite = muted ? speakerOff : speakerOn;
            soundCaption.text = muted ? "OFF" : "SFX";
            optionsSoundCaption.text = muted ? "SFX: OFF" : "SFX: ON";
        }

        private void OpenDialog(string heading, GameObject page)
        {
            if (mapPage != null) mapPage.SetActive(false);
            choices.SetActive(false);
            logo.SetActive(false);
            dialog.SetActive(true);
            customizePage.SetActive(page == customizePage);
            optionsPage.SetActive(page == optionsPage);
            quitPage.SetActive(page == quitPage);
            dialogTitle.text = heading;
        }

        public void CloseDialog()
        {
            if (mapPage != null) mapPage.SetActive(false);
            dialog.SetActive(false);
            choices.SetActive(true);
            logo.SetActive(true);
            customizePage.SetActive(false);
            optionsPage.SetActive(true);
            quitPage.SetActive(false);
            dialogTitle.text = "OPTIONS";
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
