using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerStoryProgressTests
    {
        private readonly Dictionary<string, int> ints = new Dictionary<string, int>();
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly HashSet<string> existed = new HashSet<string>();
        private RunnerDinoRoster roster;
        [SetUp]
        public void IsolateProfile()
        {
            roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
            ints.Clear(); strings.Clear(); existed.Clear();
            foreach (string key in RunnerAccountProgress.Keys(roster).Concat(new[] { "DinoRush.Muted" }))
            {
                if (PlayerPrefs.HasKey(key)) existed.Add(key);
                if (key == RunnerStoryProgress.SaveKey || key == RunnerStoryProgress.TransactionKey ||
                    key == RunnerDailyMissions.SaveKey || key == RunnerDinoRoster.SelectionKey || key == RunnerMapCatalog.SelectionKey)
                    strings[key] = PlayerPrefs.GetString(key);
                else ints[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.DeleteKey(key);
            }
        }
        [TearDown]
        public void RestoreProfile()
        {
            foreach (var pair in ints) if (existed.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            foreach (var pair in strings) if (existed.Contains(pair.Key)) PlayerPrefs.SetString(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }
        [Test]
        public void FreshCampaignUnlocksOnlyTheFirstLevelAndRejectsInvalidCompletions()
        {
            var progress = new RunnerStoryProgress();
            Assert.That(progress.Unlocked(0), Is.True);
            for (int i = 1; i < 15; i++) Assert.That(progress.Unlocked(i), Is.False);
            Assert.That(progress.Unlocked(-1), Is.False); Assert.That(progress.Unlocked(15), Is.False);
            Assert.That(progress.Complete(3, 3, 3), Is.Zero);
            Assert.That(progress.Complete(0, 0, 3), Is.Zero);
            Assert.That(progress.CompletedCount, Is.Zero); Assert.That(progress.ContinueIndex, Is.Zero);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.Zero);
        }
        [Test]
        public void StarsAreIndependentAndMergeAcrossReplaysWithoutLosingOldAchievements()
        {
            var progress = new RunnerStoryProgress();
            progress.Complete(0, 1, 3); Assert.That(progress.StarMask(0), Is.EqualTo(5));
            progress.Complete(0, 3, 0); Assert.That(progress.StarMask(0), Is.EqualTo(7));
            progress.Complete(0, 1, 0); Assert.That(progress.Stars(0), Is.EqualTo(3));
            var reloaded = new RunnerStoryProgress();
            Assert.That(reloaded.StarMask(0), Is.EqualTo(7)); Assert.That(reloaded.Unlocked(1), Is.True);
        }
        [Test]
        public void FirstClearIsCreditedExactlyOnceAcrossRetriesClaimsAndReloads()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 100);
            var progress = new RunnerStoryProgress();
            Assert.That(progress.Complete(0, 3, 3), Is.EqualTo(75));
            for (int i = 0; i < 5; i++)
            {
                progress = new RunnerStoryProgress();
                Assert.That(progress.Complete(0, 3, 3), Is.Zero);
                Assert.That(progress.ClaimReward(0), Is.Zero);
            }
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(175));
            Assert.That(PlayerPrefs.HasKey(RunnerStoryProgress.TransactionKey), Is.False);
        }
        [Test]
        public void AFullWalletDoesNotBlockProgressOrDestroyThePendingReward()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue);
            var progress = new RunnerStoryProgress();
            Assert.That(progress.Complete(0, 3, 3), Is.Zero);
            Assert.That(progress.Completed(0), Is.True); Assert.That(progress.Unlocked(1), Is.True);
            Assert.That(progress.RewardClaimed(0), Is.False);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 40);
            Assert.That(new RunnerStoryProgress().ClaimReward(0), Is.EqualTo(75));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(115));
        }
        [Test]
        public void InterruptedRewardIntentIsRecoveredBeforeTheNextOperation()
        {
            var progress = new RunnerStoryProgress(); progress.Complete(0, 3, 0);
            string committed = PlayerPrefs.GetString(RunnerStoryProgress.SaveKey);
            PlayerPrefs.DeleteKey(RunnerStoryProgress.SaveKey);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 0);
            PlayerPrefs.SetString(RunnerStoryProgress.TransactionKey,
                "{\"version\":1,\"walletAfter\":75,\"saveAfter\":\"" + committed.Replace("\"", "\\\"") + "\"}");
            progress = new RunnerStoryProgress();
            Assert.That(progress.Completed(0), Is.True); Assert.That(progress.RewardClaimed(0), Is.True);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(75));
            Assert.That(progress.Complete(0, 3, 3), Is.Zero);
        }
        [Test]
        public void FinishingAllFifteenLevelsUnlocksTheTitleWithoutTouchingTheCollectionOrEndlessRecord()
        {
            PlayerPrefs.SetInt(RunnerAccountProgress.HighScoreKey, 7654);
            var progress = new RunnerStoryProgress(); int reward = 0;
            for (int i = 0; i < 15; i++)
            {
                reward += progress.Complete(i, 3, 3);
                Assert.That(progress.ContinueIndex, Is.EqualTo(Mathf.Min(14, i + 1)));
            }
            Assert.That(progress.CompletedCount, Is.EqualTo(15)); Assert.That(progress.CampaignComplete, Is.True);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(reward));
            Assert.That(PlayerPrefs.GetInt(RunnerAccountProgress.HighScoreKey), Is.EqualTo(7654));
            for (int i = 1; i < roster.Count; i++) Assert.That(new RunnerShopProgress(roster).IsUnlocked(i), Is.False);
        }
        [Test]
        public void ResetClearsStoryAndTheRecoveryJournalButRetainsSoundPreferences()
        {
            var progress = new RunnerStoryProgress(); progress.Complete(0, 3, 3); progress.MarkSceneSeen(0, true);
            PlayerPrefs.SetString(RunnerStoryProgress.TransactionKey, "unfinished");
            PlayerPrefs.SetInt("DinoRush.Muted", 1);
            RunnerAccountProgress.Reset(roster); progress.Reload();
            Assert.That(progress.CompletedCount, Is.Zero); Assert.That(progress.SceneSeen(0, true), Is.False);
            Assert.That(PlayerPrefs.HasKey(RunnerStoryProgress.TransactionKey), Is.False);
            Assert.That(PlayerPrefs.GetInt("DinoRush.Muted"), Is.EqualTo(1));
        }
        [Test]
        public void MalformedOrOldSavesFailSafely()
        {
            foreach (string save in new[] { "not json", "null", "{\"version\":99}", "{\"version\":1,\"stars\":null}", "{\"version\":1,\"stars\":[0]}" })
            {
                PlayerPrefs.SetString(RunnerStoryProgress.SaveKey, save);
                var progress = new RunnerStoryProgress();
                Assert.That(progress.CompletedCount, Is.Zero); Assert.That(progress.Unlocked(0), Is.True);
            }
        }
        [Test]
        public void EveryLevelHasCompleteReadableConversationsAndAReproducibleFiniteCourse()
        {
            Assert.That(RunnerStoryCampaign.Levels.Length, Is.EqualTo(15));
            var seeds = new HashSet<int>();
            foreach (RunnerStoryLevel level in RunnerStoryCampaign.Levels)
            {
                Assert.That(level.Duration, Is.InRange(60, 120)); Assert.That(seeds.Add(level.Seed), Is.True);
                Assert.That(level.DistanceAt(level.Duration), Is.GreaterThan(300));
                foreach (RunnerStoryScene scene in new[] { level.Opening, level.Ending })
                {
                    Assert.That(scene.Lines.Length, Is.GreaterThanOrEqualTo(4));
                    foreach (RunnerStoryLine line in scene.Lines)
                    {
                        Assert.That(line.Speaker, Is.Not.Empty); Assert.That(line.Text, Is.Not.Empty);
                        Assert.That(line.Text, Does.Not.Contain("TODO")); Assert.That(line.Text.Length, Is.LessThan(180));
                    }
                }
            }
            Assert.That(RunnerStoryCampaign.Levels[14].Ending.Lines.Last().Text, Does.Contain("A NEW BEGINNING"));
        }
        [Test]
        public void ConversationConfirmRevealsThenAdvancesAndCompletesOnlyOnce()
        {
            int finishes = 0; var conversation = new RunnerStoryConversation();
            var scene = new RunnerStoryScene("Test", "trail", "Kira", new RunnerStoryLine("Blue", "Hello."), new RunnerStoryLine("Kira", "Together."));
            conversation.Play(scene, () => finishes++);
            conversation.Advance(); Assert.That(conversation.Revealed, Is.True); Assert.That(conversation.Index, Is.Zero);
            conversation.Advance(); Assert.That(conversation.Index, Is.EqualTo(1)); Assert.That(conversation.Characters, Is.Zero);
            conversation.Tick(10); conversation.Advance(); conversation.Advance(); conversation.Skip();
            Assert.That(conversation.Playing, Is.False); Assert.That(finishes, Is.EqualTo(1));
        }
        [Test]
        public void SkipHasTheSameCompletionCallbackAndCancellationNeverAwardsAnything()
        {
            int finishes = 0; var conversation = new RunnerStoryConversation();
            conversation.Play(RunnerStoryCampaign.Levels[0].Opening, () => finishes++);
            conversation.Skip(); conversation.Skip(); Assert.That(finishes, Is.EqualTo(1));
            conversation.Play(RunnerStoryCampaign.Levels[0].Ending, () => finishes++);
            conversation.Cancel(); conversation.Skip(); Assert.That(finishes, Is.EqualTo(1));
        }
    }
}
