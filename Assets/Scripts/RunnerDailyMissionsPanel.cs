using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static DinoRush.RunnerShopPresentation;

namespace DinoRush
{
    public sealed class RunnerDailyMissionsPanel : MonoBehaviour
    {
        private static readonly Color Cream = new Color32(240, 242, 207, 255);
        private static readonly Color Gold = new Color32(255, 210, 117, 255);
        [SerializeField] private Text countdown, wallet, hint;
        [SerializeField] private Button[] claims = new Button[3];
        [SerializeField] private Text[] counters = new Text[3];
        [SerializeField] private Image[] fills = new Image[3];
        private RunnerDailyMissions progress;
        private Action changed;
        private bool bound;
        private float refreshTime;
        private string message;
        private long displayedDay;
        public void Initialize(RunnerDailyMissions missions, Action refresh)
        {
            progress = missions; changed = refresh;
            if (!bound)
            {
                bound = true;
                for (int i = 0; i < claims.Length; i++)
                {
                    int index = i;
                    claims[i].onClick.AddListener(() => Claim(index));
                }
            }
            Refresh();
        }
        private void OnEnable() { refreshTime = 0; message = null; Refresh(); }
        private void Update()
        {
            refreshTime -= Time.unscaledDeltaTime;
            if (refreshTime <= 0) { refreshTime = 1; Refresh(); }
        }
        private void Claim(int index)
        {
            if (!isActiveAndEnabled || progress == null) return;
            var result = progress.Claim(index);
            if (result == RunnerDailyMissions.ClaimResult.Success)
            {
                message = "+" + RunnerDailyMissions.Reward(index) + " EGGS CLAIMED!";
                changed?.Invoke();
            }
            else if (result == RunnerDailyMissions.ClaimResult.WalletFull) message = "WALLET FULL / SPEND EGGS TO CLAIM";
            EventSystem.current?.SetSelectedGameObject(null);
            Refresh();
        }
        public void Refresh()
        {
            if (progress == null || countdown == null) return;
            progress.Refresh();
            if (displayedDay != progress.Day) { displayedDay = progress.Day; message = null; }
            TimeSpan remaining = progress.UntilReset;
            countdown.text = "RESETS IN " + (remaining.Days > 0 ? remaining.Days + "D " : "") +
                remaining.Hours.ToString("00") + ":" + remaining.Minutes.ToString("00") + ":" + remaining.Seconds.ToString("00");
            wallet.text = "EGGS: " + Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey)).ToString("N0");
            hint.text = message ?? "600 EGGS TO EARN / PROGRESS ACROSS MATCHES";
            for (int i = 0; i < claims.Length; i++)
            {
                bool claimed = progress.Claimed(i), complete = progress.Complete(i);
                counters[i].text = progress.Progress(i) + " / " + RunnerDailyMissions.Goal(i) + (i == 0 ? " M" : "");
                counters[i].color = claimed ? new Color32(183, 224, 151, 255) : Cream;
                fills[i].fillAmount = (float)progress.Progress(i) / RunnerDailyMissions.Goal(i);
                fills[i].rectTransform.anchorMax = new Vector2(fills[i].fillAmount, 1);
                fills[i].color = claimed ? new Color32(183, 224, 151, 255) : Gold;
                claims[i].interactable = complete && !claimed;
                claims[i].GetComponentInChildren<Text>(true).text = claimed ? "CLAIMED" : complete ? "CLAIM" : "IN PROGRESS";
            }
        }
        public void EnsureUi(Font font, Sprite panelSprite, Sprite buttonSprite)
        {
            Stretch(transform);
            countdown = Label("Daily reset countdown", transform, "RESETS IN --:--:--", font, 12, 0, 236, 688, 28, Cream);
            wallet = Label("Mission egg balance", transform, "EGGS: 0", font, 12, 0, 204, 688, 24, Gold);
            hint = Label("Mission feedback", transform, "600 EGGS TO EARN / PROGRESS ACROSS MATCHES", font, 10, 0, -230, 688, 24, Cream);
            for (int i = 0; i < 3; i++)
            {
                Transform card = Node("Daily mission " + (i + 1), transform); Art(card, panelSprite, Color.white, true);
                Label("Mission title", card, RunnerDailyMissions.Title(i), font, 15, -100, 34, 400, 28, Gold);
                Label("Mission detail", card, i == 0 ? "TOTAL DISTANCE / ANY MAP" : i == 1 ? "PICK UP EGGS DURING RUNS" : "PASS SAFELY OR SHATTER", font, 10, -100, 8, 400, 20, Cream);
                Transform track = Node("Mission progress track", card); Art(track, null, new Color32(8, 23, 31, 255), false);
                Stretch(track); // Repositioned by ApplyLayout; no layout groups or prefab replacements.
                fills[i] = Art(Node("Mission progress fill", track), null, Gold, false);
                Stretch(fills[i].transform); fills[i].rectTransform.anchorMax = Vector2.up; fills[i].fillAmount = 0;
                counters[i] = Label("Mission count", card, "0 / " + RunnerDailyMissions.Goal(i), font, 10, -100, -38, 400, 20, Cream);
                Label("Mission reward", card, "+" + RunnerDailyMissions.Reward(i) + " EGGS", font, 12, 238, 28, 164, 24, Gold);
                Transform action = Node("Claim daily mission " + (i + 1), card);
                Image image = Art(action, buttonSprite, Color.white, true); image.raycastTarget = true;
                claims[i] = action.GetComponent<Button>(); if (claims[i] == null) claims[i] = action.gameObject.AddComponent<Button>();
                claims[i].targetGraphic = image; claims[i].navigation = new Navigation { mode = Navigation.Mode.None };
                claims[i].interactable = false;
                Label("Label", action, "IN PROGRESS", font, 11, 0, 0, 144, 36, Cream);
            }
            ApplyLayout(false);
        }
        public void ApplyLayout(bool portrait)
        {
            if (countdown == null) return;
            Position(countdown.transform, 0, portrait ? 456 : 236, portrait ? 544 : 688, 28);
            Position(wallet.transform, 0, portrait ? 418 : 204, portrait ? 544 : 688, 24);
            Position(hint.transform, 0, portrait ? -392 : -230, portrait ? 544 : 688, portrait ? 44 : 24);
            hint.fontSize = portrait ? 11 : 10;
            for (int i = 0; i < 3; i++)
            {
                Transform card = claims[i].transform.parent;
                Position(card, 0, portrait ? 268 - i * 248 : 124 - i * 136, portrait ? 560 : 704, portrait ? 240 : 120);
                Position(card.Find("Mission title"), portrait ? 0 : -100, portrait ? 84 : 34, portrait ? 504 : 400, 28);
                card.Find("Mission title").GetComponent<Text>().fontSize = portrait ? 18 : 15;
                Position(card.Find("Mission detail"), portrait ? 0 : -100, portrait ? 54 : 8, portrait ? 504 : 400, 20);
                Position(card.Find("Mission progress track"), portrait ? 0 : -100, portrait ? 28 : -16, portrait ? 504 : 400, 8);
                Position(counters[i].transform, portrait ? -132 : -100, portrait ? 4 : -38, portrait ? 232 : 400, 24);
                Position(card.Find("Mission reward"), portrait ? 132 : 238, portrait ? 4 : 28, portrait ? 232 : 164, 24);
                Position(claims[i].transform, portrait ? 0 : 238, portrait ? -66 : -14, portrait ? 504 : 164, portrait ? 100 : 48);
                Transform label = claims[i].transform.Find("Label");
                Position(label, 0, 0, portrait ? 480 : 144, portrait ? 84 : 36);
                label.GetComponent<Text>().fontSize = portrait ? 18 : 11;
                claims[i].GetComponent<RunnerUiButtonFeedback>()?.RefreshContentLayout();
            }
            Refresh(); // Responsive snapshots restore anchors; reapply the live progress widths afterwards.
        }
    }
}
