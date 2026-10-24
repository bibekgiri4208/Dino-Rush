using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerShopProgressTests
    {
        private RunnerDinoRoster roster;
        private readonly Dictionary<string, int> saved = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();

        [SetUp]
        public void FreshProfile()
        {
            roster = AssetDatabase.LoadAssetAtPath<RunnerDinoRoster>("Assets/Generated/ForestDinoRoster.asset");
            saved.Clear();
            existing.Clear();
            Remember(RunnerShopProgress.WalletKey);
            for (int i = 0; i < roster.Count; i++) Remember(RunnerShopProgress.FragmentKey(roster[i].Id));
        }

        private void Remember(string key)
        {
            if (PlayerPrefs.HasKey(key)) existing.Add(key);
            saved[key] = PlayerPrefs.GetInt(key);
            PlayerPrefs.DeleteKey(key);
        }

        [TearDown]
        public void RestoreProfile()
        {
            foreach (var pair in saved)
                if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value);
                else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }

        [Test]
        public void NewPlayersOnlyOwnBlueAndCannotSpendWithoutEnoughEggs()
        {
            var shop = new RunnerShopProgress(roster);
            Assert.That(shop.IsUnlocked(0), Is.True);
            Assert.That(shop.IsUnlocked(-1), Is.False);
            Assert.That(shop.IsUnlocked(roster.Count), Is.False);
            Assert.That(shop.LockedCount, Is.EqualTo(roster.Count - 1));
            Assert.That(shop.TryBuy(out int index, out int amount, out bool unlocked), Is.False);
            Assert.That(index, Is.EqualTo(-1));
            Assert.That(amount, Is.Zero);
            Assert.That(unlocked, Is.False);
            Assert.That(shop.Balance, Is.Zero);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, RunnerShopProgress.MysteryEggCost - 1);
            Assert.That(shop.TryBuy(out _, out _, out _), Is.False);
            Assert.That(shop.Balance, Is.EqualTo(RunnerShopProgress.MysteryEggCost - 1));
        }

        [TestCase(1)]
        [TestCase(20)]
        public void InclusiveRewardEndpointsAreCreditedAndPersistWithExactlyOneCharge(int amount)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 100);
            var shop = new RunnerShopProgress(roster, new FixedRandom(1, amount));
            Assert.That(shop.TryBuy(out int index, out int reward, out bool unlocked), Is.True);
            Assert.That(index, Is.EqualTo(2));
            Assert.That(reward, Is.EqualTo(amount));
            Assert.That(unlocked, Is.False);
            var reloaded = new RunnerShopProgress(roster);
            Assert.That(reloaded.Balance, Is.EqualTo(100 - RunnerShopProgress.MysteryEggCost));
            Assert.That(reloaded.Fragments(2), Is.EqualTo(amount));
            Assert.That(reloaded.Fragments(1), Is.Zero);
            Assert.That(reloaded.Fragments(3), Is.Zero);
        }

        [TestCase(1, 100)]
        [TestCase(20, 119)]
        public void CrossingOneHundredUnlocksPermanentlyAndNeverAwardsAlreadyOwnedDinos(int amount, int expectedTotal)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 200);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("red"), 99);
            var shop = new RunnerShopProgress(roster, new FixedRandom(0, amount));
            Assert.That(shop.IsUnlocked(1), Is.False);
            Assert.That(shop.TryBuy(out int index, out _, out bool unlocked), Is.True);
            Assert.That(index, Is.EqualTo(1));
            Assert.That(unlocked, Is.True);
            Assert.That(new RunnerShopProgress(roster).IsUnlocked(1), Is.True);
            Assert.That(shop.Fragments(1), Is.EqualTo(expectedTotal), "The full fragment reward is retained.");
            Assert.That(shop.TryBuy(out index, out _, out _), Is.True);
            Assert.That(index, Is.EqualTo(2), "Skip the starter and the newly unlocked red dino.");
            for (int i = 1; i < roster.Count; i++) PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(roster[i].Id), 100);
            int before = shop.Balance;
            Assert.That(shop.LockedCount, Is.Zero);
            Assert.That(shop.CanBuy, Is.False);
            Assert.That(shop.TryBuy(out _, out _, out _), Is.False);
            Assert.That(shop.Balance, Is.EqualTo(before));
        }

        [Test]
        public void SeededPurchasesVaryDinoAndQuantityWithoutConsumingUnityRandom()
        {
            var shop = new RunnerShopProgress(roster, new System.Random(741));
            var dinos = new HashSet<int>();
            var amounts = new HashSet<int>();
            var state = Random.state;
            try
            {
                Random.InitState(97);
                float expected = Random.value;
                Random.InitState(97);
                for (int i = 0; i < 100; i++)
                {
                    PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, RunnerShopProgress.MysteryEggCost);
                    for (int d = 1; d < roster.Count; d++) PlayerPrefs.DeleteKey(RunnerShopProgress.FragmentKey(roster[d].Id));
                    Assert.That(shop.TryBuy(out int index, out int amount, out _), Is.True);
                    Assert.That(index, Is.InRange(1, roster.Count - 1));
                    Assert.That(amount, Is.InRange(1, 20));
                    dinos.Add(index);
                    amounts.Add(amount);
                }
                Assert.That(dinos.Count, Is.EqualTo(roster.Count - 1));
                Assert.That(amounts.Count, Is.GreaterThan(1));
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally { Random.state = state; }
        }

        [Test]
        public void PickupsPersistAndInvalidOrMissingRosterCannotSpend()
        {
            var shop = new RunnerShopProgress(roster);
            shop.CollectEgg();
            shop.CollectEgg();
            Assert.That(new RunnerShopProgress(roster).Balance, Is.EqualTo(2));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, -8);
            Assert.That(shop.Balance, Is.Zero);
            shop.CollectEgg();
            Assert.That(shop.Balance, Is.EqualTo(1));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue);
            shop.CollectEgg();
            Assert.That(shop.Balance, Is.EqualTo(int.MaxValue));
            Assert.That(new RunnerShopProgress(null).TryBuy(out _, out _, out _), Is.False);
        }

        private sealed class FixedRandom : System.Random
        {
            private readonly int choice, amount;
            public FixedRandom(int choice, int amount) { this.choice = choice; this.amount = amount; }
            public override int Next(int maxValue) => choice;
            public override int Next(int minValue, int maxValue)
            {
                Assert.That(minValue, Is.EqualTo(1));
                Assert.That(maxValue, Is.EqualTo(21));
                return amount;
            }
        }

        [Test]
        public void ExistingWalletAndColorProgressSurviveTheExpandedRosterAndNewDinosReceiveRewards()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 500);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("red"), 45);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("yellow"), 100);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("green"), 8);
            var shop = new RunnerShopProgress(roster);
            Assert.That(shop.Balance, Is.EqualTo(500));
            Assert.That(shop.Fragments(1), Is.EqualTo(45));
            Assert.That(shop.IsUnlocked(2), Is.True);
            Assert.That(shop.Fragments(3), Is.EqualTo(8));
            PlayerPrefs.DeleteKey(RunnerShopProgress.FragmentKey("yellow"));
            for (int i = 4; i < roster.Count; i++)
            {
                Assert.That(shop.IsUnlocked(i), Is.False);
                var purchase = new RunnerShopProgress(roster, new FixedRandom(i - 1, 20));
                Assert.That(purchase.TryBuy(out int target, out int reward, out _), Is.True);
                Assert.That(target, Is.EqualTo(i));
                Assert.That(reward, Is.EqualTo(20));
                Assert.That(new RunnerShopProgress(roster).Fragments(i), Is.EqualTo(20));
            }
        }
    }
}
