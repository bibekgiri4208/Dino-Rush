using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerDailyMissionTests
    {
        private string oldSave;
        private int oldWallet;
        private bool hadSave, hadWallet;
        private DateTime now;
        private RunnerDailyMissions progress;
        [SetUp] public void Save()
        {
            hadSave = PlayerPrefs.HasKey(RunnerDailyMissions.SaveKey); oldSave = PlayerPrefs.GetString(RunnerDailyMissions.SaveKey);
            hadWallet = PlayerPrefs.HasKey(RunnerShopProgress.WalletKey); oldWallet = PlayerPrefs.GetInt(RunnerShopProgress.WalletKey);
            PlayerPrefs.DeleteKey(RunnerDailyMissions.SaveKey); PlayerPrefs.DeleteKey(RunnerShopProgress.WalletKey);
            now = new DateTime(2030, 10, 24, 12, 0, 0);
            progress = New();
        }
        [TearDown] public void Restore()
        {
            if (hadSave) PlayerPrefs.SetString(RunnerDailyMissions.SaveKey, oldSave); else PlayerPrefs.DeleteKey(RunnerDailyMissions.SaveKey);
            if (hadWallet) PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, oldWallet); else PlayerPrefs.DeleteKey(RunnerShopProgress.WalletKey);
            PlayerPrefs.Save();
        }
        private RunnerDailyMissions New() => new RunnerDailyMissions(() => now);
        private void CompleteAll()
        {
            progress.RecordDistance(500);
            for (int i = 0; i < 30; i++) progress.RecordEgg();
            for (int i = 0; i < 25; i++) progress.RecordClear();
        }
        [TestCase(0, 500, 100)] [TestCase(1, 30, 200)] [TestCase(2, 25, 300)]
        public void MissionsHaveTheAgreedGoalsAndManualRewards(int index, int goal, int reward)
        {
            Assert.That(RunnerDailyMissions.Goal(index), Is.EqualTo(goal));
            Assert.That(RunnerDailyMissions.Reward(index), Is.EqualTo(reward));
            Assert.That(progress.Claim(index), Is.EqualTo(RunnerDailyMissions.ClaimResult.Incomplete));
            CompleteAll();
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.Zero, "Completing missions does not auto-credit.");
            Assert.That(progress.Claim(index), Is.EqualTo(RunnerDailyMissions.ClaimResult.Success));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(reward));
            Assert.That(progress.Claimed(index), Is.True);
            Assert.That(progress.Claim(index), Is.EqualTo(RunnerDailyMissions.ClaimResult.AlreadyClaimed));
            Assert.That(New().Claim(index), Is.EqualTo(RunnerDailyMissions.ClaimResult.AlreadyClaimed));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(reward));
        }
        [Test] public void PartialProgressPersistsAndAccumulatesAcrossSessions()
        {
            progress.RecordDistance(123.5f); progress.RecordEgg(); progress.RecordClear(); progress.Flush();
            progress = New(); progress.RecordDistance(376.5f); progress.RecordEgg(); progress.Flush();
            var reloaded = New();
            Assert.That(reloaded.Progress(0), Is.EqualTo(500));
            Assert.That(reloaded.Progress(1), Is.EqualTo(2)); Assert.That(reloaded.Progress(2), Is.EqualTo(1));
        }
        [Test] public void ClaimsTotalSixHundredAndDoNotCountAsEggPickups()
        {
            CompleteAll();
            for (int i = 0; i < 3; i++) Assert.That(progress.Claim(i), Is.EqualTo(RunnerDailyMissions.ClaimResult.Success));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(600));
            Assert.That(progress.Progress(1), Is.EqualTo(30));
        }
        [Test] public void MidnightResetsProgressAndUnclaimedRewardsWithoutRetroactivePayout()
        {
            now = now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            CompleteAll(); progress.Claim(0); progress.Flush();
            Assert.That(progress.UntilReset.TotalSeconds, Is.EqualTo(1));
            now = now.AddSeconds(2); progress.RecordDistance(3); progress.Refresh();
            Assert.That(progress.Progress(0), Is.EqualTo(3));
            Assert.That(progress.Progress(1), Is.Zero); Assert.That(progress.Progress(2), Is.Zero);
            for (int i = 0; i < 3; i++) Assert.That(progress.Claimed(i), Is.False);
            Assert.That(progress.Claim(1), Is.EqualTo(RunnerDailyMissions.ClaimResult.Incomplete));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(100));
            Assert.That(progress.UntilReset.TotalSeconds, Is.EqualTo(86399));
        }
        [Test] public void ClockRollbackAndStaleInstancesCannotUnclaimOrDoublePay()
        {
            progress.RecordDistance(500); progress.Flush();
            var stale = New();
            Assert.That(progress.Claim(0), Is.EqualTo(RunnerDailyMissions.ClaimResult.Success));
            stale.RecordEgg(); stale.Flush();
            Assert.That(stale.Claim(0), Is.EqualTo(RunnerDailyMissions.ClaimResult.AlreadyClaimed));
            now = now.AddDays(-2); progress = New();
            Assert.That(progress.Claim(0), Is.EqualTo(RunnerDailyMissions.ClaimResult.AlreadyClaimed));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(100));
        }
        [Test] public void MissedDaysResetOnceWithoutBankingRewards()
        {
            CompleteAll(); progress.Flush(); now = now.AddDays(7);
            progress = New();
            Assert.That(progress.Progress(0), Is.Zero); Assert.That(progress.Claimed(0), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.Zero);
            progress.RecordDistance(7); progress.Flush();
            Assert.That(New().Progress(0), Is.EqualTo(7));
        }
        [Test] public void WalletOverflowDoesNotBurnClaimAndExactCapacityIsAllowed()
        {
            CompleteAll(); PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue - 99);
            Assert.That(progress.Claim(0), Is.EqualTo(RunnerDailyMissions.ClaimResult.WalletFull));
            Assert.That(progress.Claimed(0), Is.False);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue - 100);
            Assert.That(progress.Claim(0), Is.EqualTo(RunnerDailyMissions.ClaimResult.Success));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(int.MaxValue));
        }
        [TestCase(-1)] [TestCase(3)]
        public void InvalidClaimsNeverSpendOrCredit(int index)
        {
            CompleteAll(); Assert.That(progress.Claim(index), Is.EqualTo(RunnerDailyMissions.ClaimResult.InvalidMission));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.Zero);
        }
        [Test] public void InvalidDistanceAndExcessProgressCannotCorruptTheGoals()
        {
            foreach (float value in new[] { -1f, 0f, float.NaN, float.NegativeInfinity, float.PositiveInfinity }) progress.RecordDistance(value);
            Assert.That(progress.Progress(0), Is.Zero);
            progress.RecordDistance(float.MaxValue);
            for (int i = 0; i < 100; i++) { progress.RecordEgg(); progress.RecordClear(); }
            progress.Flush(); progress = New();
            for (int i = 0; i < 3; i++) Assert.That(progress.Progress(i), Is.EqualTo(RunnerDailyMissions.Goal(i)));
        }
        [TestCase("not json")] [TestCase("{}")] [TestCase("{\"day\":-1,\"metres\":-10,\"claimed\":7}")]
        public void BadSavedDataRecoversWithoutCreditingEggs(string json)
        {
            PlayerPrefs.SetString(RunnerDailyMissions.SaveKey, json); progress = New();
            Assert.That(progress.Progress(0), Is.Zero); Assert.That(progress.Claimed(0), Is.False);
            Assert.That(progress.Claim(0), Is.EqualTo(RunnerDailyMissions.ClaimResult.Incomplete));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.Zero);
        }
        [Test] public void SavedSceneHasOneBoundButtonThreeCardsAndAnInitiallyHiddenMissionsPage()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                var menu = Array.Find(scene.GetRootGameObjects(), root => root.GetComponentInChildren<ForestMainMenu>(true) != null).GetComponentInChildren<ForestMainMenu>(true);
                Assert.That(menu.MissionsPage, Is.Not.Null);
                Assert.That(menu.MissionsPage.gameObject.activeSelf, Is.False);
                Assert.That(menu.transform.Find("Main menu buttons/Missions").GetComponent<Button>(), Is.Not.Null);
                Assert.That(menu.MissionsPage.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(3));
                int count = menu.GetComponentsInChildren<Transform>(true).Length;
                menu.EnsureDailyMissionsUi(); menu.EnsureDailyMissionsUi();
                Assert.That(menu.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
