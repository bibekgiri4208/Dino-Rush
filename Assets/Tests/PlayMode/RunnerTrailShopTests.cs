using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerTrailShopTests
    {
        private readonly Dictionary<string, int> old = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private RunnerPlayerTrail trail;
        private readonly Dictionary<string, string> oldStrings = new Dictionary<string, string>();
        private readonly HashSet<string> existingStrings = new HashSet<string>();
        [UnitySetUp]
        public IEnumerator Load()
        {
            old.Clear(); existing.Clear(); oldStrings.Clear(); existingStrings.Clear();
            foreach (string key in new[] { RunnerDinoRoster.SelectionKey, RunnerMapCatalog.SelectionKey })
            {
                if (PlayerPrefs.HasKey(key)) existingStrings.Add(key);
                oldStrings[key] = PlayerPrefs.GetString(key);
            }
            PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, "blue");
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "forest");
            var keys = new List<string>(RunnerMapPreferenceScope.ShopKeys) { RunnerTrailProgress.SelectionKey, "DinoRush.Forest.HighScore" };
            for (int i = 1; i <= RunnerTrailProgress.Count; i++) keys.Add(RunnerTrailProgress.OwnershipKey((RunnerTrailKind)i));
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) existing.Add(key);
                old[key] = PlayerPrefs.GetInt(key); PlayerPrefs.DeleteKey(key);
            }
            yield return Reload();
        }
        private IEnumerator Reload()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>(true);
            trail = game.GetComponentInChildren<RunnerPlayerTrail>();
            foreach (BaseInputModule module in EventSystem.current.GetComponents<BaseInputModule>()) module.enabled = false;
            yield return new WaitForSecondsRealtime(0.35f);
        }
        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            foreach (var pair in old)
                if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in oldStrings)
                if (existingStrings.Contains(pair.Key)) PlayerPrefs.SetString(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }
        private IEnumerator Open()
        {
            Click("Shop");
            yield return new WaitForSecondsRealtime(0.35f);
            Click("Trails category");
            yield return null;
            Assert.That(menu.TrailShop.IsTrails, Is.True);
            Assert.That(menu.TrailShop.transform.Find("Single adventure").gameObject.activeSelf, Is.False);
        }
        [UnityTest]
        public IEnumerator ShopPreviewsAreFreeBuyingEquipsOnceAndOwnedTrailsCanBeSwappedOrRemoved()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 6500);
            yield return Open();
            Assert.That(GameObject.Find("Buy mystery egg"), Is.Null);
            Assert.That(game.Shop.Balance, Is.EqualTo(6500));
            Assert.That(GameObject.Find("Equipped trail summary").GetComponent<Text>().text, Is.EqualTo("USING: NONE"));
            Assert.That(GameObject.Find("Trail page indicator").GetComponent<Text>().text, Is.EqualTo("PAGE 1 / 2"));
            Assert.That(GameObject.Find("Trail card 1/Trail ownership").GetComponent<Text>().text, Is.EqualTo("2,000 EGGS"));
            yield return new WaitForSecondsRealtime(0.2f);
            foreach (RunnerTrailPreview preview in menu.TrailShop.GetComponentsInChildren<RunnerTrailPreview>())
            {
                Assert.That(preview.Foreground, Is.False);
                Assert.That(preview.ActiveCount, Is.GreaterThan(2));
                Canvas.ForceUpdateCanvases();
                Assert.That(preview.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(8), "Previews must render separate moving particles, not one static trail quad.");
            }
            yield return RunnerDisplayCapture.Capture("trail-shop-landscape", 1280, 720);
            yield return RunnerDisplayCapture.Capture("trail-shop-portrait", 360, 800);
            // Restore native layout after virtual captures before pointer tests.
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            yield return null;
            for (int i = 1; i <= 3; i++)
            {
                Click("Buy or equip trail " + i);
                Assert.That(game.Shop.Balance, Is.EqualTo(6500 - i * 2000));
                Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo((RunnerTrailKind)i));
                Assert.That(RunnerTrailProgress.Owned((RunnerTrailKind)i), Is.True);
                Assert.That(GameObject.Find("Trail card " + i + "/Trail ownership").GetComponent<Text>().text, Is.EqualTo("IN USE"));
                var button = GameObject.Find("Buy or equip trail " + i).GetComponent<Button>();
                button.onClick.Invoke(); // A duplicate event cannot charge the owned cosmetic again.
                Assert.That(game.Shop.Balance, Is.EqualTo(6500 - i * 2000));
                yield return null;
            }
            yield return RunnerDisplayCapture.Capture("trail-shop-owned", 1280, 720);
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            yield return null;
            Click("Buy or equip trail 1");
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Smoke));
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
            Click("Remove trail");
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.None));
            Click("Buy or equip trail 2");
            Click("Dinos category");
            yield return null;
            Assert.That(GameObject.Find("Buy mystery egg"), Is.Not.Null);
            Assert.That(GameObject.Find("Shop egg balance").GetComponent<Text>().text, Is.EqualTo("EGGS: 500"));
            Assert.That(GameObject.Find("Trails section"), Is.Null);
            Assert.That(menu.TrailShop.transform.Find("Single adventure").gameObject.activeSelf, Is.False, "Switching back to Dinos must not revive background text.");
            yield return Reload();
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Fire));
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
            yield return Open();
            Assert.That(GameObject.Find("Buy or equip trail 1").GetComponent<Button>().interactable, Is.True);
        }
        [UnityTest]
        public IEnumerator NewTrailPageHasThreeDistinctLiveEffectsAndPurchasesPersistAcrossPagesAndReloads()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 6500);
            yield return Open();
            Click("Next trails page");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(menu.TrailShop.Page, Is.EqualTo(1));
            Assert.That(GameObject.Find("Trail page indicator").GetComponent<Text>().text, Is.EqualTo("PAGE 2 / 2"));
            Assert.That(GameObject.Find("Buy or equip trail 1"), Is.Null);
            yield return RunnerDisplayCapture.Capture("trail-shop-new-effects", 1280, 720);
            yield return RunnerDisplayCapture.Capture("trail-shop-new-effects-portrait", 360, 800);
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            for (int i = 4; i <= RunnerTrailProgress.Count; i++)
            {
                var preview = GameObject.Find("Trail card " + i).GetComponentInChildren<RunnerTrailPreview>();
                Assert.That(preview.Kind, Is.EqualTo((RunnerTrailKind)i));
                Assert.That(preview.ActiveCount, Is.GreaterThan(2));
                Click("Buy or equip trail " + i);
                Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo((RunnerTrailKind)i));
                Assert.That(game.Shop.Balance, Is.EqualTo(6500 - (i - 3) * 2000));
            }
            Click("Previous trails page");
            Assert.That(menu.TrailShop.Page, Is.Zero);
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Cosmic));
            Assert.That(GameObject.Find("Equipped trail summary").GetComponent<Text>().text, Is.EqualTo("USING: COSMIC"), "Equipped status stays visible even on the other page.");
            Click("Next trails page");
            Click("Buy or equip trail 4");
            Assert.That(game.Shop.Balance, Is.EqualTo(500));
            yield return Reload();
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Frost));
            for (int i = 4; i <= RunnerTrailProgress.Count; i++) Assert.That(RunnerTrailProgress.Owned((RunnerTrailKind)i), Is.True);
        }
        [UnityTest]
        public IEnumerator PoorWalletCannotBuyAndCategoriesAreBlockedDuringHatchReveal()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 1999);
            yield return Open();
            for (int i = 1; i <= 3; i++)
            {
                Button button = GameObject.Find("Buy or equip trail " + i).GetComponent<Button>();
                Assert.That(button.interactable, Is.False);
                button.onClick.Invoke();
                Assert.That(RunnerTrailProgress.Owned((RunnerTrailKind)i), Is.False);
            }
            Assert.That(game.Shop.Balance, Is.EqualTo(1999));
            Click("Dinos category");
            yield return null;
            Click("Buy mystery egg");
            Assert.That(menu.ShopPresentation.IsShowing, Is.True);
            menu.TrailShop.ShowTrails(true);
            Assert.That(menu.TrailShop.IsTrails, Is.False);
            Assert.That(game.Shop.Balance, Is.EqualTo(1949));
            menu.CloseDialog(); menu.CloseDialog();
        }
        [UnityTest]
        public IEnumerator WorldTrailsFollowAirborneDuckingPosesFreezeOnPauseAndClearOnRestartAndMenu()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, RunnerTrailProgress.Count * RunnerTrailProgress.Cost);
            foreach (RunnerTrailKind kind in new[] { RunnerTrailKind.Smoke, RunnerTrailKind.Fire, RunnerTrailKind.Electric, RunnerTrailKind.Frost, RunnerTrailKind.Blossom, RunnerTrailKind.Cosmic })
            {
                Assert.That(RunnerTrailProgress.Buy(kind), Is.True);
                RunnerTrailProgress.Equip(kind);
                game.StartRun();
                game.enabled = true;
                yield return new WaitForSecondsRealtime(0.3f);
                game.enabled = false;
                Assert.That(trail.Kind, Is.EqualTo(kind));
                Assert.That(trail.ActiveCount, Is.GreaterThan(0));
                Assert.That(trail.Renderer.enabled, Is.True);
                SpriteRenderer actor = game.transform.Find("Player/Blue dino player").GetComponent<SpriteRenderer>();
                Assert.That(trail.Renderer.sortingOrder, Is.LessThan(actor.sortingOrder));
                Assert.That(trail.System.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));
                Assert.That(trail.EmitterPosition.y, Is.GreaterThan(RunnerRules.GroundY + 0.5f));
                yield return RunnerDisplayCapture.Capture("trail-world-" + kind.ToString().ToLowerInvariant(), 1280, 720);
                game.Jump();
                game.enabled = true;
                yield return new WaitForSecondsRealtime(0.16f);
                game.enabled = false;
                Assert.That(trail.EmitterPosition.y, Is.GreaterThan(RunnerRules.GroundY + 1));
                yield return RunnerDisplayCapture.Capture("trail-air-" + kind.ToString().ToLowerInvariant(), 1280, 720);
                game.SetDuckHeld(true);
                game.enabled = true;
                yield return new WaitForSecondsRealtime(0.1f);
                game.enabled = false;
                game.TogglePause();
                Assert.That(trail.System.isPaused, Is.True, kind + " must not leave a native auto-playing system behind");
                float clock = trail.Clock;
                int count = trail.ActiveCount;
                game.enabled = true;
                yield return new WaitForSecondsRealtime(0.15f);
                game.enabled = false;
                Assert.That(trail.Clock, Is.EqualTo(clock));
                Assert.That(trail.ActiveCount, Is.EqualTo(count));
                game.RestartRun();
                Assert.That(trail.ActiveCount, Is.Zero);
                Assert.That(trail.Renderer.enabled, Is.False);
                Assert.That(trail.GetComponentsInChildren<MeshRenderer>().Length, Is.Zero, "No baked wake/aura meshes remain.");
                game.ReturnToMenu();
                Assert.That(trail.ActiveCount, Is.Zero);
                Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(kind));
            }
        }
        [UnityTest]
        public IEnumerator PortraitAndLandscapeTrailCardsAndLabelsStayInsideTheShopAndRemainTappable()
        {
            yield return Open();
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(360, 800), new Vector2Int(1024, 768) })
            for (int page = 0; page < menu.TrailShop.PageCount; page++)
            {
                // Pointer tests need the real display's viewport; virtual sizes below are for bounds checks.
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
                if (menu.TrailShop.Page != page) Click(page == 0 ? "Previous trails page" : "Next trails page");
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                Canvas.ForceUpdateCanvases();
                Transform panel = menu.transform.Find("Menu dialog/Moss framed dialog");
                Rect panelRect = ((RectTransform)panel).rect;
                foreach (Text text in panel.GetComponentsInChildren<Text>())
                {
                    Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
                }
                foreach (Button button in panel.GetComponentsInChildren<Button>())
                {
                    Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, button.transform);
                    Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(panelRect.xMin), button.name);
                    Assert.That(bounds.max.x, Is.LessThanOrEqualTo(panelRect.xMax), button.name);
                    Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(panelRect.yMin), button.name);
                    Assert.That(bounds.max.y, Is.LessThanOrEqualTo(panelRect.yMax), button.name);
                }
                Transform section = menu.TrailShop.transform.Find("Trails section");
                AssertSeparate(section, section.Find("Trail egg balance"), section.Find("Equipped trail summary"));
                AssertSeparate(section, section.Find("Previous trails page"), section.Find("Trail page indicator"));
                AssertSeparate(section, section.Find("Next trails page"), section.Find("Trail page indicator"));
                foreach (Transform card in section)
                {
                    if (!card.name.StartsWith("Trail card ") || !card.gameObject.activeSelf) continue;
                    AssertSeparate(section, card, section.Find("Next trails page"));
                    AssertSeparate(section, card, section.Find("Previous trails page"));
                    var blocks = new[] { card.Find("Trail name"), card.Find("Preview backdrop"), card.Find("Trail description"), card.Find("Trail ownership"), card.Find("Buy or equip trail " + card.name.Substring("Trail card ".Length)) };
                    for (int a = 0; a < blocks.Length; a++)
                    for (int b = a + 1; b < blocks.Length; b++) AssertSeparate(card, blocks[a], blocks[b]);
                    Assert.That(((RectTransform)card.Find("Live trail preview")).rect.height, Is.GreaterThanOrEqualTo(size.y > size.x ? 210 : 180));
                    if (size.y > size.x)
                    {
                        Assert.That(card.Find("Trail name").GetComponent<Text>().fontSize, Is.GreaterThanOrEqualTo(24));
                        Assert.That(blocks[4].Find("Label").GetComponent<Text>().fontSize, Is.GreaterThanOrEqualTo(18));
                    }
                }
            }
        }
        private static void AssertSeparate(Transform space, Transform a, Transform b)
        {
            Assert.That(RectIn(space, a).Overlaps(RectIn(space, b)), Is.False, a.name + " overlaps " + b.name);
        }
        private static Rect RectIn(Transform space, Transform node)
        {
            var corners = new Vector3[4];
            ((RectTransform)node).GetWorldCorners(corners);
            Vector2 min = space.InverseTransformPoint(corners[0]), max = min;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector2 point = space.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        [UnityTest]
        public IEnumerator TrailsWorkWithEveryMapAndDinoAndNoTrailProducesNoMesh()
        {
            foreach (string id in new[] { "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" })
                PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(id), RunnerShopProgress.RequiredFragments);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, RunnerTrailProgress.Count * RunnerTrailProgress.Cost);
            for (int i = 1; i <= RunnerTrailProgress.Count; i++) RunnerTrailProgress.Buy((RunnerTrailKind)i);
            for (int map = 0; map < 5; map++)
                for (int dino = 0; dino < game.DinoRoster.Count; dino++)
                {
                    game.SelectMap(map);
                    game.SelectDino(dino);
                    RunnerTrailKind kind = (RunnerTrailKind)(1 + (map + dino) % RunnerTrailProgress.Count);
                    RunnerTrailProgress.Equip(kind);
                    game.StartRun();
                    Assert.That(game.SelectedDinoIndex, Is.EqualTo(dino));
                    Assert.That(game.SelectedMapIndex, Is.EqualTo(map));
                    for (int step = 0; step < 30; step++) trail.Tick(1 / 60f, new Vector2(RunnerRules.PlayerX, RunnerRules.GroundY), false, 6.8f, true);
                    Assert.That(trail.Kind, Is.EqualTo(kind));
                    Assert.That(trail.ActiveCount, Is.GreaterThan(0));
                    game.ReturnToMenu();
                    Assert.That(trail.ActiveCount, Is.Zero);
                }
            RunnerTrailProgress.Equip(RunnerTrailKind.None);
            game.StartRun();
            trail.Tick(0.1f, Vector2.zero, false, 6.8f, true);
            Assert.That(trail.ActiveCount, Is.Zero);
            Assert.That(trail.Renderer.enabled, Is.False);
            yield return null;
        }
        private static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            Button button = GameObject.Find(name).GetComponent<Button>();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), name);
            Assert.That(hits[0].gameObject, Is.EqualTo(button.gameObject), name + " is blocked");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
