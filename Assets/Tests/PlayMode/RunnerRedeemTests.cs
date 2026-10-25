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
    public sealed class RunnerRedeemTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private RunnerRedeemPresentation gift;
        private readonly Dictionary<string, int> saved = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        private bool hadDino;
        private string previousDino;

        [UnitySetUp]
        public IEnumerator Load()
        {
            saved.Clear();
            existing.Clear();
            foreach (string key in RunnerMapPreferenceScope.ShopKeys)
            {
                if (PlayerPrefs.HasKey(key)) existing.Add(key);
                saved[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
            const string soundKey = "DinoRush.Muted";
            if (PlayerPrefs.HasKey(soundKey)) existing.Add(soundKey);
            saved[soundKey] = PlayerPrefs.GetInt(soundKey);
            hadDino = PlayerPrefs.HasKey(RunnerDinoRoster.SelectionKey);
            previousDino = PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "blue");
            yield return Reload();
        }

        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>();
            game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>(true);
            gift = menu.RedeemPresentation;
            foreach (BaseInputModule module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (var pair in saved)
                if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value);
                else PlayerPrefs.DeleteKey(pair.Key);
            if (hadDino) PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, previousDino);
            else PlayerPrefs.DeleteKey(RunnerDinoRoster.SelectionKey);
            PlayerPrefs.Save();
        }

        private IEnumerator Open()
        {
            Click("Options");
            yield return new WaitForSecondsRealtime(0.3f);
            Click("Redeem gift");
            yield return null;
            Assert.That(gift.IsShowing, Is.True);
            Assert.That(gift.CodeInput.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator OptionsOpensCodeEntryInvalidCodesDoNotPayAndTheGiftCanBeClaimedRepeatedly()
        {
            yield return Open();
            yield return RunnerDisplayCapture.Capture("redeem-code-entry", 1280, 720);
            yield return RunnerDisplayCapture.Capture("redeem-code-entry-portrait", 360, 800);
            gift.CodeInput.text = "not-a-gift";
            Click("Submit gift code");
            Assert.That(game.Shop.Balance, Is.Zero);
            Assert.That(GameObject.Find("Code status").GetComponent<Text>().text, Does.Contain("NOT RECOGNIZED"));
            gift.CodeInput.text = "jestergift";
            Click("Submit gift code");
            Assert.That(game.Shop.Balance, Is.EqualTo(10000), "Save the gift before starting its animation.");
            Assert.That(gift.ShowingGift, Is.True);
            Assert.That(GameObject.Find("Gift from production").GetComponent<Text>().text, Is.EqualTo("Gift from\nBibek Giri Productions"));
            // A duplicate submit event from the same click/Enter cannot pay twice during one celebration.
            gift.CodeInput.onSubmit.Invoke("jestergift");
            Assert.That(game.Shop.Balance, Is.EqualTo(10000));
            Advance(0.6f);
            Assert.That(gift.DisplayedEggs, Is.InRange(1, 9999));
            yield return RunnerDisplayCapture.Capture("redeem-gift-opening", 1280, 720);
            Advance(2);
            Assert.That(gift.DisplayedEggs, Is.EqualTo(10000));
            Assert.That(GameObject.Find("Gift egg amount").GetComponent<Text>().text, Is.EqualTo("+10,000 EGGS"));
            yield return RunnerDisplayCapture.Capture("redeem-gift-reward", 1280, 720);
            yield return RunnerDisplayCapture.Capture("redeem-gift-reward-portrait", 360, 800);
            Canvas.ForceUpdateCanvases();
            Assert.That(GameObject.Find("Opening present").GetComponent<RunnerGiftGraphic>().canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0));
            Click("Gift continue");
            yield return null;
            Assert.That(gift.CodeInput.text, Is.EqualTo("jestergift"));
            Click("Submit gift code");
            Assert.That(game.Shop.Balance, Is.EqualTo(20000));
            menu.CloseDialog(); // Skip.
            Assert.That(gift.DisplayedEggs, Is.EqualTo(10000));
            menu.CloseDialog(); // Return to entry, allowing another deliberate redemption.
            Assert.That(gift.ShowingGift, Is.False);
            gift.CodeInput.onSubmit.Invoke(gift.CodeInput.text);
            Assert.That(game.Shop.Balance, Is.EqualTo(30000));
            menu.CloseDialog();
            menu.CloseDialog();
            menu.CloseDialog(); // Dismiss entry, leaving Options open.
            Assert.That(menu.DialogOpen, Is.True);
            Assert.That(gift.IsShowing, Is.False);
            menu.CloseDialog();
            yield return null;
            Click("Shop");
            Assert.That(GameObject.Find("Shop egg balance").GetComponent<Text>().text, Is.EqualTo("EGGS: 30,000"));
            Assert.That(GameObject.Find("Buy ten mystery eggs").GetComponent<Button>().interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator ClosingOrInterruptingAGiftKeepsItsCreditAcrossReloadAndAnotherRedemption()
        {
            yield return Open();
            gift.CodeInput.text = "jestergift";
            gift.CodeInput.onSubmit.Invoke(gift.CodeInput.text);
            menu.Hide();
            Assert.That(gift.IsShowing, Is.False);
            yield return Reload();
            Assert.That(game.Shop.Balance, Is.EqualTo(10000));
            yield return Open();
            Assert.That(gift.CodeInput.text, Is.Empty);
            gift.CodeInput.text = "JESTERGIFT";
            Click("Submit gift code");
            Assert.That(game.Shop.Balance, Is.EqualTo(20000));
            Advance(4);
            Advance(4);
            Assert.That(game.Shop.Balance, Is.EqualTo(20000), "Presentation cannot credit again.");
            menu.Hide();
            yield return Reload();
            Assert.That(game.Shop.Balance, Is.EqualTo(20000));
        }

        [UnityTest]
        public IEnumerator GiftModalBlocksUnderlyingControlsReflowsAndAnimatesWithScaledTimePaused()
        {
            yield return Open();
            var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(null, GameObject.Find("Redeem gift").transform.position) };
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits[0].gameObject.name, Is.Not.EqualTo("Redeem gift"));
            gift.CodeInput.text = "jestergift";
            Click("Submit gift code");
            float scale = Time.timeScale;
            try
            {
                Time.timeScale = 0;
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.That(gift.DisplayedEggs, Is.GreaterThan(0));
                Advance(2);
                Assert.That(gift.DisplayedEggs, Is.EqualTo(10000));
                foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(1024, 768) })
                {
                    game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                    Canvas.ForceUpdateCanvases();
                    Transform panel = GameObject.Find("Redeem panel").transform;
                    Rect rect = ((RectTransform)panel).rect;
                    foreach (Text text in panel.GetComponentsInChildren<Text>())
                    {
                        Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                        Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
                    }
                    Bounds button = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, GameObject.Find("Gift continue").transform);
                    Assert.That(button.min.y, Is.GreaterThan(rect.yMin));
                    Assert.That(button.max.x, Is.LessThan(rect.xMax));
                    Assert.That(game.Shop.Balance, Is.EqualTo(10000));
                }
            }
            finally { Time.timeScale = scale; }
            menu.CloseDialog();
            menu.CloseDialog();
            menu.CloseDialog();
            Assert.That(gift.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator CodeCaretTracksLeftAlignedTextThroughEditingScrollingAndReflow()
        {
            yield return Open();
            InputField input = gift.CodeInput;
            input.caretBlinkRate = 0;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(360, 800), new Vector2Int(1280, 720) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                input.Select(); input.ActivateInputField(); yield return null;
                Assert.That(input.isFocused, Is.True);
                Text text = input.textComponent;
                var placeholder = (Text)input.placeholder;
                Assert.That(text.alignment, Is.EqualTo(TextAnchor.MiddleLeft));
                Assert.That(text.alignByGeometry, Is.False);
                Assert.That(text.lineSpacing, Is.EqualTo(1f));
                Assert.That(placeholder.alignment, Is.EqualTo(text.alignment));
                Assert.That(placeholder.fontSize, Is.EqualTo(text.fontSize));
                foreach (string value in new[] { "", "jestergift", new string('w', 64) })
                {
                    input.text = value;
                    foreach (int position in new[] { 0, value.Length / 2, value.Length })
                    {
                        input.caretPosition = position; input.ForceLabelUpdate();
                        yield return null;
                        Canvas.ForceUpdateCanvases(); input.Rebuild(CanvasUpdate.LatePreRender);
                        var renderer = (CanvasRenderer)typeof(InputField).GetField("m_CachedInputRenderer", Private).GetValue(input);
                        Assert.That(renderer, Is.Not.Null);
                        Mesh mesh = renderer.GetMesh();
                        Assert.That(mesh.vertexCount, Is.EqualTo(4), "Editing must display one visible caret, not a selection.");
                        var caretRect = (RectTransform)renderer.transform;
                        Assert.That(caretRect.anchoredPosition, Is.EqualTo(text.rectTransform.anchoredPosition));
                        Assert.That(caretRect.sizeDelta, Is.EqualTo(text.rectTransform.sizeDelta));
                        Assert.That(caretRect.localScale, Is.EqualTo(text.rectTransform.localScale));
                        int drawStart = (int)typeof(InputField).GetField("m_DrawStart", Private).GetValue(input);
                        int visiblePosition = position - drawStart;
                        float expectedX = value.Length == 0 ? text.rectTransform.rect.xMin :
                            text.cachedTextGenerator.characters[visiblePosition].cursorPos.x / text.pixelsPerUnit;
                        Assert.That(mesh.bounds.min.x, Is.EqualTo(expectedX).Within(2f / text.pixelsPerUnit), "Caret must follow the insertion point, including scrolled text.");
                        Assert.That(mesh.bounds.size.y, Is.LessThanOrEqualTo(text.fontSize * 1.5f), "Label line spacing must not make the caret oversized.");
                        Assert.That(mesh.bounds.min.x, Is.GreaterThanOrEqualTo(text.rectTransform.rect.xMin - 2));
                        Assert.That(mesh.bounds.max.x, Is.LessThanOrEqualTo(text.rectTransform.rect.xMax + 3));
                    }
                }
            }
            input.text = "jestergift"; input.caretPosition = 5; input.ForceLabelUpdate(); yield return null;
            yield return RunnerDisplayCapture.Capture("redeem-code-caret", 1280, 720);
            yield return RunnerDisplayCapture.Capture("redeem-code-caret-portrait", 360, 800);
            Assert.That(game.Shop.Balance, Is.Zero, "Editing alone must never redeem a gift.");
        }

        [UnityTest]
        public IEnumerator TypingGiftCodesDoesNotTriggerMutePauseOrPlayShortcuts()
        {
            yield return Open();
            Keyboard original = Keyboard.current;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            int sound = PlayerPrefs.GetInt("DinoRush.Muted");
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.M, Key.P, Key.W, Key.Space));
                InputSystem.Update();
                typeof(ForestRunnerGame).GetMethod("ReadInput", Private).Invoke(game, null);
                Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(sound));
                Assert.That(gift.IsShowing, Is.True);
                Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
                foreach (char letter in "jestergift")
                    gift.CodeInput.ProcessEvent(new Event { type = EventType.KeyDown, character = letter });
                Assert.That(gift.CodeInput.text, Is.EqualTo("jestergift"));
                gift.CodeInput.onSubmit.Invoke(gift.CodeInput.text);
                Assert.That(game.Shop.Balance, Is.EqualTo(10000));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (original != null) original.MakeCurrent();
            }
        }

        private void Advance(float dt) => typeof(RunnerRedeemPresentation).GetMethod("Advance", Private).Invoke(gift, new object[] { dt });

        private static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            Button button = GameObject.Find(name).GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
