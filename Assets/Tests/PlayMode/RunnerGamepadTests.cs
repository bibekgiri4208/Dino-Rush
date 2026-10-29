using System;
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
using Object = UnityEngine.Object;

namespace DinoRush.Tests
{
    public sealed class RunnerGamepadTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private ForestRunnerGame game;
        private ForestMainMenu menu;
        private RunnerGamepadUi ui;
        private RunnerPlayer player;
        private Gamepad pad, originalPad;
        private InputSettings.BackgroundBehavior background;
        private InputSettings.EditorInputBehaviorInPlayMode editor;
        private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly HashSet<string> existed = new HashSet<string>();
        private bool hadVolume;
        private float oldVolume;

        [UnitySetUp]
        public IEnumerator Load()
        {
            hadVolume = PlayerPrefs.HasKey(RunnerGameplayMusic.VolumeKey);
            oldVolume = PlayerPrefs.GetFloat(RunnerGameplayMusic.VolumeKey);
            PlayerPrefs.DeleteKey(RunnerGameplayMusic.VolumeKey);
            var keys = new List<string>(RunnerAccountProgress.Keys(null)) { "DinoRush.Muted", RunnerGameplayMusic.EnabledKey };
            foreach (string id in new[] { "blue", "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" })
                keys.Add(RunnerShopProgress.FragmentKey(id));
            ints.Clear(); strings.Clear(); existed.Clear();
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) existed.Add(key);
                if (key == RunnerDinoRoster.SelectionKey || key == RunnerMapCatalog.SelectionKey || key == RunnerDailyMissions.SaveKey ||
                    key == RunnerStoryProgress.SaveKey || key == RunnerStoryProgress.TransactionKey)
                    strings[key] = PlayerPrefs.GetString(key);
                else ints[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 100000);
            PlayerPrefs.SetInt(RunnerSlashProgress.OwnershipKey(RunnerSlashKind.Fire), 1);
            PlayerPrefs.SetInt(RunnerSlashProgress.SelectionKey, (int)RunnerSlashKind.Fire);
            PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, 10);
            PlayerPrefs.SetInt("DinoRush.Muted", 1);
            background = InputSystem.settings.backgroundBehavior;
            editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            originalPad = Gamepad.current;
            pad = InputSystem.AddDevice<Gamepad>();
            InputSystem.EnableDevice(pad);
            pad.MakeCurrent();
            _ = pad.buttonSouth.wasPressedThisFrame;
            yield return SceneManager.LoadSceneAsync("ForestRun");
            game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            menu = game.GetComponentInChildren<ForestMainMenu>(true);
            ui = (RunnerGamepadUi)typeof(ForestRunnerGame).GetField("gamepadUi", Private).GetValue(game);
            player = (RunnerPlayer)typeof(ForestRunnerGame).GetField("player", Private).GetValue(game);
            Set(default);
            Press(GamepadButton.North); // Claim controller focus without activating a title button.
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (game != null) Object.Destroy(game.gameObject);
            yield return null;
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            if (originalPad != null && originalPad.added) originalPad.MakeCurrent();
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            foreach (var pair in ints)
                if (existed.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in strings)
                if (existed.Contains(pair.Key)) PlayerPrefs.SetString(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            if (hadVolume) PlayerPrefs.SetFloat(RunnerGameplayMusic.VolumeKey, oldVolume);
            else PlayerPrefs.DeleteKey(RunnerGameplayMusic.VolumeKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator ControllerCanAdjustMusicInOptionsAndPauseWithoutMovingFocusOrResumingTheRun()
        {
            float original = game.MusicVolume;
            Click("Options"); Focus("Music volume");
            Press(GamepadButton.DpadRight);
            Assert.That(ui.Focused.name, Is.EqualTo("Music volume"));
            Assert.That(game.MusicVolume, Is.EqualTo(original + 0.05f).Within(0.001f));
            Set(new GamepadState { leftStick = Vector2.right });
            Assert.That(game.MusicVolume, Is.EqualTo(original + 0.1f).Within(0.001f));
            Set(default);
            Press(GamepadButton.DpadDown);
            Assert.That(ui.Focused.name, Is.Not.EqualTo("Music volume"), "Up/down still leave the slider.");
            Press(GamepadButton.East);
            game.StartRun(); Press(GamepadButton.Start);
            Focus("Music volume");
            for (int i = 0; i < 30; i++) Press(GamepadButton.DpadLeft);
            Assert.That(game.MusicVolume, Is.Zero);
            Press(GamepadButton.DpadRight);
            Assert.That(game.MusicVolume, Is.EqualTo(0.05f).Within(0.001f));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            yield return RunnerDisplayCapture.Capture("music-slider-gamepad", 1280, 720);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ControllerNavigatesMapsStartsWithoutJumpingAndControlsTheWholeRun()
        {
            Assert.That(ui.Focused.name, Is.EqualTo("Play"));
            Press(GamepadButton.South);
            Assert.That(ui.Focused.name, Is.EqualTo("Endless mode"));
            Press(GamepadButton.South);
            for (int i = 0; i < game.MapCatalog.Count; i++)
            {
                Click("Choose " + game.MapCatalog[i].displayName + " map");
                Assert.That(game.SelectedMapIndex, Is.EqualTo(i));
            }
            Click("Start selected map");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.AwaitingStart));
            yield return null; // The map confirmation cannot also dismiss Tap to Start.
            Press(GamepadButton.South);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(player.Grounded, Is.True);
            typeof(ForestRunnerGame).GetField("spawnTimer", Private).SetValue(game, float.MaxValue);
            Set(new GamepadState().WithButton(GamepadButton.South)); Step(1);
            Assert.That(player.JumpsRemaining, Is.EqualTo(1));
            Step(12); Assert.That(player.JumpsRemaining, Is.EqualTo(1), "Holding A cannot spend the second jump.");
            Set(default); Press(GamepadButton.South); Step(1);
            Assert.That(player.JumpsRemaining, Is.Zero);
            Press(GamepadButton.West);
            Assert.That(game.SlashesUsed, Is.EqualTo(1));
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(9));
            Press(GamepadButton.Start);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(ui.Focused.name, Is.EqualTo("Resume run"));
            float pausedHeight = player.Feet.y;
            Press(GamepadButton.West); Press(GamepadButton.South);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(player.Feet.y, Is.EqualTo(pausedHeight));
            Assert.That(game.SlashesUsed, Is.EqualTo(1));
            Press(GamepadButton.Start);
            Set(new GamepadState().WithButton(GamepadButton.East)); // Back resumes; its held B must not fast-drop.
            Set(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Assert.That(player.IsDucking, Is.False);
            Set(default); Set(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(player.IsDucking, Is.True);
            Set(default); Step(90);
            Assert.That(player.Grounded, Is.True);
            Set(new GamepadState { leftStick = Vector2.down }); Assert.That(player.IsDucking, Is.True);
            Set(default); Assert.That(player.IsDucking, Is.False);
            Press(GamepadButton.Start); Click("Restart run");
            Assert.That(game.SlashesUsed, Is.Zero);
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(9), "Restart must not replenish paid stock.");
            Press(GamepadButton.Start); Click("Quit run");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            game.StartRun(); Invoke("EndRun");
            Set(default); ui.RefreshFocus();
            Click("Results main menu");
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
        }

        [UnityTest]
        public IEnumerator ControllerNavigatesEveryShopCategoryPagesAndHatchModalWithoutLeakingFocus()
        {
            Click("Shop");
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(game.DinoRoster[1].Id), RunnerShopProgress.RequiredFragments);
            menu.SetDinoSelection(game.SelectedDinoIndex);
            var dinos = (Button[])typeof(ForestMainMenu).GetField("dinoChoices", Private).GetValue(menu);
            Click(dinos[1].name);
            Assert.That(game.SelectedDinoIndex, Is.EqualTo(1), "Owned dinos can be equipped without pointer input.");
            Click("Next dinos"); Assert.That(menu.ShopCollection.Page, Is.EqualTo(1));
            Click("Previous dinos"); Assert.That(menu.ShopCollection.Page, Is.Zero);
            Click("Trails category");
            int balance = game.Shop.Balance;
            Click("Buy or equip trail 1");
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Smoke));
            Assert.That(game.Shop.Balance, Is.EqualTo(balance - RunnerTrailProgress.Cost));
            Click("Next trails page"); Assert.That(menu.TrailShop.Page, Is.EqualTo(1));
            Click("Previous trails page"); Assert.That(menu.TrailShop.Page, Is.Zero);
            Click("Remove trail"); Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.None));
            Click("Slashes category");
            int charges = RunnerSlashProgress.Charges;
            Click("Buy slash charge"); Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(charges + 1));
            Click("Unlock slash 2"); Assert.That(RunnerSlashProgress.Equipped, Is.EqualTo(RunnerSlashKind.Lightning));
            Click("Dinos category");
            // Locate the authored hatch button by its binding rather than its art label.
            var buy = (Button)typeof(ForestMainMenu).GetField("buyMysteryEgg", Private).GetValue(menu);
            Click(buy.name);
            Assert.That(menu.ShopPresentation.IsShowing, Is.True);
            Assert.That(ui.Focused.transform.IsChildOf(menu.ShopPresentation.NavigationRoot), Is.True);
            int paidBalance = game.Shop.Balance;
            for (int i = 0; i < 12; i++) Press(GamepadButton.RightShoulder);
            Assert.That(ui.Focused.transform.IsChildOf(menu.ShopPresentation.NavigationRoot), Is.True);
            Assert.That(game.Shop.Balance, Is.EqualTo(paidBalance));
            Press(GamepadButton.East); Press(GamepadButton.East);
            Assert.That(menu.ShopPresentation.IsShowing, Is.False);
            var buyTen = (Button)typeof(ForestMainMenu).GetField("buyTenMysteryEggs", Private).GetValue(menu);
            Click(buyTen.name); Assert.That(menu.ShopPresentation.Batch.IsShowing, Is.True);
            Press(GamepadButton.South); Press(GamepadButton.South);
            Assert.That(menu.ShopPresentation.Batch.IsShowing, Is.False);
            foreach (Vector2 size in new[] { new Vector2(1280, 720), new Vector2(360, 800) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate((int)size.x, (int)size.y, new Rect(Vector2.zero, size)));
                Canvas.ForceUpdateCanvases();
                var marker = (RectTransform)game.GetComponentInChildren<Canvas>().transform.Find("16 by 9 safe content/Gamepad focus");
                var target = (RectTransform)ui.Focused.transform;
                Assert.That(Vector3.Distance(marker.TransformPoint(marker.rect.center), target.TransformPoint(target.rect.center)),
                    Is.LessThan(0.01f), "Focus must remain aligned after rotating or resizing the UI.");
            }
            yield return RunnerDisplayCapture.Capture("gamepad-shop-focus", 1280, 720);
            yield return RunnerDisplayCapture.Capture("gamepad-shop-focus-phone", 360, 800);
        }

        [UnityTest]
        public IEnumerator ControllerCanTypeGiftCodesCancelDestructiveDialogsAndReachMissions()
        {
            Click("Options");
            Click("Reset account");
            Assert.That(ui.Focused.name, Is.EqualTo("Cancel account reset"), "Destructive confirmation must never be the default.");
            Focus("Confirm account reset");
            Press(GamepadButton.East);
            Assert.That(menu.AccountReset.IsShowing, Is.False);
            Click("Reset account");
            Assert.That(ui.Focused.name, Is.EqualTo("Cancel account reset"), "Reopening must not remember a dangerous confirmation.");
            Press(GamepadButton.East);
            int originalBalance = game.Shop.Balance;
            var redeem = (Button)typeof(ForestMainMenu).GetField("redeemGift", Private).GetValue(menu);
            Click(redeem.name);
            Assert.That(ui.Focused, Is.EqualTo(menu.RedeemPresentation.CodeInput));
            yield return null;
            Assert.That(menu.RedeemPresentation.CodeInput.isFocused, Is.False, "Gamepad focus must not open the OS touch keyboard.");
            Press(GamepadButton.South);
            Assert.That(ui.Scope.name, Is.EqualTo("Gamepad code keyboard"));
            foreach (char letter in "JESTERGIFT") Click("Key " + letter);
            Press(GamepadButton.North); Press(GamepadButton.West); // Space/delete without submitting.
            Assert.That(menu.RedeemPresentation.CodeInput.text, Is.EqualTo("JESTERGIFT"));
            Assert.That(game.Shop.Balance, Is.EqualTo(originalBalance));
            yield return RunnerDisplayCapture.Capture("gamepad-gift-keyboard", 1280, 720);
            yield return RunnerDisplayCapture.Capture("gamepad-gift-keyboard-phone", 360, 800);
            Click("Keyboard done"); Click("Submit gift code");
            Assert.That(menu.RedeemPresentation.ShowingGift, Is.True);
            Assert.That(game.Shop.Balance, Is.EqualTo(originalBalance + RunnerShopProgress.GiftEggs));
            Press(GamepadButton.East); Press(GamepadButton.East); Press(GamepadButton.East);
            Assert.That(menu.RedeemPresentation.IsShowing, Is.False);
            Press(GamepadButton.East);
            Click("Missions");
            Assert.That(menu.MissionsPage.gameObject.activeInHierarchy, Is.True);
            game.DailyMissions.RecordDistance(500);
            for (int i = 0; i < 30; i++) game.DailyMissions.RecordEgg();
            for (int i = 0; i < 25; i++) game.DailyMissions.RecordClear();
            menu.MissionsPage.Refresh();
            int beforeClaims = game.Shop.Balance;
            for (int i = 1; i <= 3; i++) Click("Claim daily mission " + i);
            Assert.That(game.Shop.Balance, Is.EqualTo(beforeClaims + 600));
            Press(GamepadButton.East);
            Click("Quit");
            Assert.That(ui.Focused.name, Is.EqualTo("Back to main menu"));
            Press(GamepadButton.East);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
        }

        [UnityTest]
        public IEnumerator ResetRequiresExplicitControllerConfirmationAndResultsCanRetry()
        {
            game.StartRun(); Invoke("EndRun");
            Set(default); ui.RefreshFocus();
            Press(GamepadButton.South);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.GameOver), "Controller retry respects the post-hit input guard.");
            typeof(ForestRunnerGame).GetField("timeSinceCrash", Private).SetValue(game, 0.5f);
            Set(default); ui.RefreshFocus();
            Assert.That(ui.Focused, Is.Not.Null);
            Press(GamepadButton.South);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Running));
            Press(GamepadButton.Start);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Click("Quit run");
            Click("Options"); Click("Reset account");
            Assert.That(ui.Focused.name, Is.EqualTo("Cancel account reset"));
            Assert.That(game.Shop.Balance, Is.EqualTo(100000));
            Click("Confirm account reset");
            Assert.That(game.Shop.Balance, Is.Zero);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Ready));
            Assert.That(RunnerSlashProgress.Charges, Is.Zero);
            Assert.That(ui.Focused, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StickDeadzoneFocusRecoveryAndDisconnectDoNotLeaveDuckOrUiStuck()
        {
            Selectable first = ui.Focused;
            Set(new GamepadState { leftStick = new Vector2(0.1f, -0.1f) });
            Assert.That(ui.Focused, Is.EqualTo(first));
            Set(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(ui.Focused.name, Is.EqualTo("Shop"));
            Set(new GamepadState().WithButton(GamepadButton.DpadDown));
            Assert.That(ui.Focused.name, Is.EqualTo("Shop"), "A held direction must not advance on every input update.");
            yield return new WaitForSecondsRealtime(0.34f);
            Invoke("ReadInput");
            Assert.That(ui.Focused.name, Is.EqualTo("Missions"), "Held directions repeat after the initial delay.");
            Set(default); Focus("Shop");
            Set(default); Press(GamepadButton.South); Press(GamepadButton.East);
            Assert.That(ui.Focused.name, Is.EqualTo("Shop"), "Back restores the opener rather than jumping to Play.");
            game.StartRun();
            Set(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(player.IsDucking, Is.True);
            InputSystem.RemoveDevice(pad);
            if (originalPad != null && originalPad.added) originalPad.MakeCurrent();
            Invoke("ReadInput");
            Assert.That(player.IsDucking, Is.False);
            Assert.That(game.UsingGamepad, Is.False);
            pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent();
            Press(GamepadButton.Start);
            Assert.That(game.State, Is.EqualTo(ForestRunnerGame.RunState.Paused));
            Assert.That(ui.Focused.name, Is.EqualTo("Resume run"));
            yield return RunnerDisplayCapture.Capture("gamepad-pause-focus", 1280, 720);
        }

        private void Focus(string name)
        {
            Canvas.ForceUpdateCanvases(); ui.RefreshFocus();
            for (int i = 0; i < 100 && (ui.Focused == null || ui.Focused.name != name); i++) Press(GamepadButton.RightShoulder);
            Assert.That(ui.Focused != null ? ui.Focused.name : "<none>", Is.EqualTo(name), "Unreachable control in " + ui.Scope?.name);
            Assert.That(ui.Focused.IsInteractable(), Is.True);
        }
        private void Click(string name) { Focus(name); Press(GamepadButton.South); }
        private void Press(GamepadButton button) { Set(default); Set(new GamepadState().WithButton(button)); Set(default); }
        private void Set(GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state); InputSystem.Update();
            Invoke("ReadInput");
        }
        private void Step(int count) { for (int i = 0; i < count; i++) Invoke("Step", RunnerRules.SimulationStep); }
        private void Invoke(string name, params object[] args) => typeof(ForestRunnerGame).GetMethod(name, Private).Invoke(game, args);
    }
}
