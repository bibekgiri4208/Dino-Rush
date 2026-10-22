using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DinoRush
{
    /// <summary>The scene-authored title screen and its small, single-character submenus.</summary>
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
        private bool bound;

        public bool DialogOpen => dialog != null && dialog.activeInHierarchy;

        public void Initialize(Action start, Action toggleSound)
        {
            if (bound) return;
            bound = true;
            Bind(play, start);
            Bind(customize, () => OpenDialog("YOUR ADVENTURE", customizePage));
            Bind(options, () => OpenDialog("OPTIONS", optionsPage));
            Bind(quit, () => OpenDialog("QUIT GAME?", quitPage));
            Bind(back, CloseDialog);
            Bind(sound, toggleSound);
            Bind(optionsSound, toggleSound);
            Bind(confirmQuit, QuitGame);
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
