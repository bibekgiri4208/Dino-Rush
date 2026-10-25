using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerSlashGameTests
    {
        private ForestRunnerGame game;
        private readonly Dictionary<string, int> old = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        private bool hadMap;
        private string oldMap;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [UnitySetUp] public IEnumerator Load()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey); oldMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            var keys = new List<string> { RunnerShopProgress.WalletKey, RunnerSlashProgress.SelectionKey, RunnerSlashProgress.ChargesKey, "DinoRush.Forest.HighScore", "DinoRush.Muted" };
            for (int i = 1; i <= 3; i++) keys.Add(RunnerSlashProgress.OwnershipKey((RunnerSlashKind)i));
            foreach (string key in keys) { if (PlayerPrefs.HasKey(key)) existing.Add(key); old[key] = PlayerPrefs.GetInt(key); PlayerPrefs.DeleteKey(key); }
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
        }
        [UnityTearDown] public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject); yield return null;
            foreach (var pair in old) if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            old.Clear(); existing.Clear(); PlayerPrefs.Save();
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, oldMap); else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.Save();
        }
        private void LoadCharges()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 10000);
            RunnerSlashProgress.Buy(RunnerSlashKind.Fire);
            for (int i = 0; i < 3; i++) RunnerSlashProgress.BuyCharge();
        }
        private void Step(int frames) { for (int i = 0; i < frames; i++) Invoke("Step", RunnerRules.SimulationStep); }
        private void Invoke(string method, params object[] args) => typeof(ForestRunnerGame).GetMethod(method, Private).Invoke(game, args);
        private void Set(string field, object value) => typeof(ForestRunnerGame).GetField(field, Private).SetValue(game, value);
        [UnityTest] public IEnumerator ThreePaidAttacksNoFreeRetryAndBlockedStatesNeverConsume()
        {
            LoadCharges(); Assert.That(game.Attack(), Is.False);
            game.StartRun(); Set("spawnTimer", float.MaxValue);
            Assert.That(game.Attack(), Is.True); Assert.That(game.Attack(), Is.False);
            game.TogglePause(); Assert.That(game.Attack(), Is.False);
            float x = game.SlashAttack.Hitbox.x; Step(20); Assert.That(game.SlashAttack.Hitbox.x, Is.EqualTo(x));
            yield return ForestRunTests.Capture("slash-paused");
            game.TogglePause(); Step(80);
            Assert.That(game.Attack(), Is.True); Step(80);
            Assert.That(game.Attack(), Is.True); Step(80);
            Assert.That(game.SlashesUsed, Is.EqualTo(3)); Assert.That(game.Attack(), Is.False);
            Assert.That(RunnerSlashProgress.Charges, Is.Zero);
            RunnerSlashProgress.BuyCharge(); Assert.That(game.Attack(), Is.False, "Even reloaded charges cannot exceed three attacks per match.");
            game.RestartRun(); Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(1));
            Assert.That(game.Attack(), Is.True); Assert.That(RunnerSlashProgress.Charges, Is.Zero);
            game.ReturnToMenu(); Assert.That(game.SlashAttack.Active, Is.False); Assert.That(game.SlashAttack.DebrisCount, Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator SlashDestroysGroundAndCanopyButNotYetUnstruckObstacles()
        {
            LoadCharges(); game.StartRun(); Set("spawnTimer", float.MaxValue);
            var pool = (IList)typeof(ForestRunnerGame).GetField("obstacles", Private).GetValue(game);
            var renderers = new List<SpriteRenderer>();
            for (int i = 0; i < 4; i++)
            {
                object item = pool[i]; var type = item.GetType();
                var art = (SpriteRenderer)type.GetField("renderer").GetValue(item); renderers.Add(art);
                type.GetField("active").SetValue(item, true);
                type.GetField("overhead").SetValue(item, i == 1);
                type.GetField("width").SetValue(item, 0.8f);
                type.GetField("height").SetValue(item, 1f);
                art.transform.position = new Vector3(i < 2 ? -3.6f + i * 1.4f : i == 2 ? 10 : -9, RunnerRules.GroundY, 0);
                art.gameObject.SetActive(true);
            }
            Assert.That(game.Attack(), Is.True); Step(12);
            Assert.That(renderers[0].gameObject.activeSelf, Is.False);
            Assert.That(renderers[1].gameObject.activeSelf, Is.False);
            Assert.That(renderers[2].gameObject.activeSelf, Is.True);
            Assert.That(renderers[3].gameObject.activeSelf, Is.True);
            Assert.That(game.SlashAttack.DebrisCount, Is.EqualTo(18));
            Assert.That(game.Lives, Is.EqualTo(RunnerHealth.MaximumLives));
            yield return ForestRunTests.Capture("slash-shattering-obstacles");
            Step(240); Assert.That(game.SlashAttack.DebrisCount, Is.Zero);
        }
        [UnityTest] public IEnumerator ShopHasAnimatedStylesAndPaidMagazineOnPhoneAndDesktop()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 18000);
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            GameObject.Find("Shop").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.35f);
            menu.TrailShop.ShowSlashes(); yield return null;
            Assert.That(menu.TrailShop.IsSlashes, Is.True);
            Assert.That(menu.TrailShop.transform.Find("Trails section").gameObject.activeSelf, Is.False);
            Assert.That(menu.TrailShop.GetComponentsInChildren<RunnerSlashPreview>().Length, Is.EqualTo(3));
            foreach (var preview in menu.TrailShop.GetComponentsInChildren<RunnerSlashPreview>())
                Assert.That(preview.transform.localScale.x, Is.LessThan(0), "Shop slashes must face forward like gameplay.");
            for (int i = 1; i <= 3; i++)
            {
                var button = menu.TrailShop.transform.Find("Slashes section/Slash card " + i + "/Unlock slash " + i).GetComponent<Button>();
                button.onClick.Invoke(); Assert.That(RunnerSlashProgress.Owned((RunnerSlashKind)i), Is.True);
            }
            var charge = menu.TrailShop.transform.Find("Slashes section/Buy slash charge").GetComponent<Button>();
            for (int i = 0; i < 3; i++) charge.onClick.Invoke();
            Assert.That(game.Shop.Balance, Is.Zero); Assert.That(charge.interactable, Is.False);
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(3));
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568), new Vector2Int(360, 800), new Vector2Int(1024, 768) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                Canvas.ForceUpdateCanvases();
                foreach (Text text in menu.TrailShop.GetComponentsInChildren<Text>())
                {
                    Assert.That(text.preferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width + 1), text.name);
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.name);
                }
            }
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            yield return ForestRunTests.Capture("slash-shop-landscape");
            yield return ForestRunTests.Capture("slash-shop-portrait", 720, 1280);
            menu.TrailShop.ShowTrails(true); Assert.That(menu.TrailShop.transform.Find("Slashes section").gameObject.activeSelf, Is.False);
            menu.TrailShop.ShowTrails(false); Assert.That(menu.TrailShop.IsEffects, Is.False);
        }
        [UnityTest] public IEnumerator EnterAttacksWithoutJumpingAndHudShowsRemainingCharges()
        {
            LoadCharges(); game.StartRun(); Set("spawnTimer", float.MaxValue);
            var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var original = Keyboard.current;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard); keyboard.MakeCurrent();
            _ = keyboard.enterKey.wasPressedThisFrame;
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update(); Invoke("ReadInput");
                Step(1);
                var player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
                Assert.That(player.Grounded, Is.True); Assert.That(game.SlashesUsed, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                yield return null;
                var label = game.GetComponentInChildren<RunnerSlashHud>().transform.Find("Slash indicator/Slash charges").GetComponent<Text>();
                Assert.That(label.text, Does.Contain("2/3").And.Contain("ENTER"));
                yield return ForestRunTests.Capture("slash-game-indicator");
                yield return ForestRunTests.Capture("slash-game-indicator-portrait", 720, 1280);
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(320, 568, new Rect(0, 24, 320, 520)));
                Assert.That(((RectTransform)label.transform.parent).rect.height * 320 / 720f, Is.GreaterThanOrEqualTo(44));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (original != null && original.added) original.MakeCurrent();
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }
        [UnityTest] public IEnumerator EveryStyleShattersActualCanopiesOnEveryMapAndKeepsVisualsBounded()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 20000);
            for (int k = 1; k <= 3; k++) RunnerSlashProgress.Buy((RunnerSlashKind)k);
            for (int map = 0; map < game.MapCatalog.Count; map++)
            for (int k = 1; k <= 3; k++)
            {
                game.ReturnToMenu(); game.SelectMap(map); RunnerSlashProgress.Equip((RunnerSlashKind)k);
                PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, 1);
                game.StartRun(); Set("spawnTimer", float.MaxValue);
                var pool = (IList)typeof(ForestRunnerGame).GetField("obstacles", Private).GetValue(game);
                object item = pool[0]; var type = item.GetType();
                var art = (SpriteRenderer)type.GetField("renderer").GetValue(item);
                art.sprite = (Sprite)typeof(ForestRunnerGame).GetField("canopySprite", Private).GetValue(game);
                art.transform.position = new Vector3(-3.5f, RunnerRules.GroundY + RunnerRules.CanopyBottom, 0);
                art.transform.localScale = new Vector3(0.8f / art.sprite.bounds.size.x, 1, 1);
                art.gameObject.SetActive(true);
                type.GetField("active").SetValue(item, true); type.GetField("overhead").SetValue(item, true);
                type.GetField("width").SetValue(item, 0.8f); type.GetField("height").SetValue(item, 0.8f);
                Assert.That(game.Attack(), Is.True);
                float startX = game.SlashAttack.Hitbox.xMax;
                Step(12);
                Assert.That(game.SlashAttack.transform.Find("Active slash").GetComponent<SpriteRenderer>().flipX, Is.True,
                    "Every style must face right on every map.");
                Assert.That(game.SlashAttack.Hitbox.xMax, Is.GreaterThan(startX), "Flipping art must not reverse attack travel.");
                Assert.That(art.gameObject.activeSelf, Is.False, "Canopy on map " + map + " / " + k);
                Assert.That(game.Lives, Is.EqualTo(RunnerHealth.MaximumLives));
                Assert.That(game.SlashAttack.DebrisCount, Is.EqualTo(9));
                if (map == 0) yield return ForestRunTests.Capture("slash-canopy-" + ((RunnerSlashKind)k).ToString().ToLowerInvariant());
                game.RestartRun(); Assert.That(game.SlashAttack.DebrisCount, Is.Zero);
            }
            Assert.That(game.SlashAttack.transform.childCount, Is.LessThanOrEqualTo(91));
        }
        [UnityTest] public IEnumerator AirborneSlashMissesGroundTargetsAndDoesNotConsumeGameplayRandomness()
        {
            LoadCharges(); game.StartRun(); game.Jump(); Set("spawnTimer", float.MaxValue); Step(40);
            var player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
            Assert.That(player.Feet.y, Is.GreaterThan(RunnerRules.GroundY + 1));
            Assert.That(game.Attack(), Is.True);
            Assert.That(game.SlashAttack.Hitbox.yMin, Is.GreaterThan(RunnerRules.GroundY + 0.9f));
            Random.State state = Random.state;
            float expected = Random.value; Random.state = state;
            game.SlashAttack.Tick(0.1f);
            Assert.That(Random.value, Is.EqualTo(expected));
            Random.state = state;
            yield return ForestRunTests.Capture("slash-airborne");
            Invoke("EndRun"); Assert.That(game.SlashAttack.Active, Is.False);
            Assert.That(game.Attack(), Is.False); Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator EachSuccessfulAttackPlaysItsElementOnceAndRejectedOrMutedAttacksStaySilent()
        {
            var audio = (RunnerAudio)typeof(ForestRunnerGame).GetField("audioCues", Private).GetValue(game);
            var sounds = new List<RunnerSlashKind>(); audio.SlashPlayed += sounds.Add;
            for (int i = 1; i <= 3; i++) PlayerPrefs.SetInt(RunnerSlashProgress.OwnershipKey((RunnerSlashKind)i), 1);
            RunnerSlashProgress.Equip(RunnerSlashKind.Fire);
            PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, 3);
            Assert.That(game.Attack(), Is.False); Assert.That(sounds, Is.Empty);
            game.StartRun(); Set("spawnTimer", float.MaxValue);
            for (int i = 1; i <= 3; i++)
            {
                RunnerSlashKind style = (RunnerSlashKind)i;
                RunnerSlashProgress.Equip(style);
                Assert.That(game.Attack(), Is.True);
                Assert.That(sounds.Count, Is.EqualTo(i)); Assert.That(sounds[i - 1], Is.EqualTo(style));
                Assert.That(game.Attack(), Is.False);
                game.TogglePause(); Assert.That(game.Attack(), Is.False);
                Assert.That(sounds.Count, Is.EqualTo(i));
                game.TogglePause(); Step(80);
            }
            Assert.That(game.Attack(), Is.False); Assert.That(sounds.Count, Is.EqualTo(3));
            game.ReturnToMenu(); PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, 1);
            game.StartRun(); Set("spawnTimer", float.MaxValue); audio.ToggleMute();
            Assert.That(game.Attack(), Is.True, "Muting must never block the attack or change charge costs.");
            Assert.That(RunnerSlashProgress.Charges, Is.Zero); Assert.That(sounds.Count, Is.EqualTo(3));
            foreach (AudioSource source in game.GetComponents<AudioSource>()) Assert.That(source.mute, Is.True);
            game.ReturnToMenu();
            audio.ToggleMute();
            yield return null;
            Assert.That(((AudioSource)typeof(RunnerAudio).GetField("slashSource", Private).GetValue(audio)).isPlaying, Is.False);
        }

        [UnityTest] public IEnumerator ShopStyleChangesPlayOnePreviewWhileLoopingArtFailedPurchasesAndRepeatedSelectionsAreSilent()
        {
            var audio = (RunnerAudio)typeof(ForestRunnerGame).GetField("audioCues", Private).GetValue(game);
            var sounds = new List<RunnerSlashKind>(); audio.SlashPlayed += sounds.Add;
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            GameObject.Find("Shop").GetComponent<Button>().onClick.Invoke();
            menu.TrailShop.ShowSlashes();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(sounds, Is.Empty, "Animated Shop previews do not trigger audio on every loop.");
            Button fire = menu.TrailShop.transform.Find("Slashes section/Slash card 1/Unlock slash 1").GetComponent<Button>();
            fire.onClick.Invoke(); Assert.That(sounds, Is.Empty);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 15000);
            menu.TrailShop.Refresh();
            for (int i = 1; i <= 3; i++)
            {
                var button = menu.TrailShop.transform.Find("Slashes section/Slash card " + i + "/Unlock slash " + i).GetComponent<Button>();
                button.onClick.Invoke(); Assert.That(sounds[sounds.Count - 1], Is.EqualTo((RunnerSlashKind)i));
                int count = sounds.Count; button.onClick.Invoke(); Assert.That(sounds.Count, Is.EqualTo(count));
            }
            Assert.That(sounds.Count, Is.EqualTo(3));
            fire.onClick.Invoke(); Assert.That(sounds.Count, Is.EqualTo(4));
            Assert.That(sounds[3], Is.EqualTo(RunnerSlashKind.Fire));
            audio.ToggleMute();
            menu.TrailShop.transform.Find("Slashes section/Slash card 2/Unlock slash 2").GetComponent<Button>().onClick.Invoke();
            Assert.That(sounds.Count, Is.EqualTo(4));
            Assert.That(RunnerSlashProgress.Equipped, Is.EqualTo(RunnerSlashKind.Lightning));
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(sounds.Count, Is.EqualTo(4));
        }
    }
}
