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

        [TestCase(1)]
        [TestCase(20)]
        public void TenPullCostsExactlyFiveHundredAndCommitsTenInclusiveRewardsBeforePresentation(int amount)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 750);
            var shop = new RunnerShopProgress(roster, new FixedRandom(3, amount));
            Assert.That(RunnerShopProgress.TenPullCost, Is.EqualTo(500));
            Assert.That(shop.CanBuyTen, Is.True);
            Assert.That(shop.TryBuyTen(out var rewards), Is.True);
            Assert.That(rewards.Length, Is.EqualTo(10));
            for (int i = 0; i < rewards.Length; i++)
            {
                Assert.That(rewards[i].DinoIndex, Is.EqualTo(4));
                Assert.That(rewards[i].Fragments, Is.EqualTo(amount));
                Assert.That(rewards[i].Total, Is.EqualTo((i + 1) * amount));
                Assert.That(rewards[i].NewlyUnlocked, Is.EqualTo(amount == 20 && i == 4));
            }
            var reloaded = new RunnerShopProgress(roster);
            Assert.That(reloaded.Balance, Is.EqualTo(250));
            Assert.That(reloaded.Fragments(4), Is.EqualTo(amount * 10));
            Assert.That(reloaded.Fragments(0), Is.Zero);
            Assert.That(shop.CanBuyTen, Is.False);
        }

        [Test]
        public void TenPullCannotPartiallyChargeWhenUnaffordableAllOwnedOrRosterMissing()
        {
            var shop = new RunnerShopProgress(roster);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 499);
            Assert.That(shop.CanBuy, Is.True);
            Assert.That(shop.CanBuyTen, Is.False);
            Assert.That(shop.TryBuyTen(out var rewards), Is.False);
            Assert.That(rewards, Is.Empty);
            Assert.That(shop.Balance, Is.EqualTo(499));
            for (int i = 1; i < roster.Count; i++) Assert.That(shop.Fragments(i), Is.Zero);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 500);
            Assert.That(new RunnerShopProgress(null).TryBuyTen(out _), Is.False);
            for (int i = 1; i < roster.Count; i++) PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(roster[i].Id), 100);
            Assert.That(shop.CanBuyTen, Is.False);
            Assert.That(shop.TryBuyTen(out rewards), Is.False);
            Assert.That(rewards, Is.Empty);
            Assert.That(shop.Balance, Is.EqualTo(500));
        }

        [Test]
        public void LastLockedDinoStillReceivesAllTenRewardsAndUnlocksOnlyOnce()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 1000);
            for (int i = 1; i < roster.Count; i++) PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey(roster[i].Id), 100);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("sena"), 99);
            var shop = new RunnerShopProgress(roster, new FixedRandom(0, 20));
            Assert.That(shop.TryBuyTen(out var rewards), Is.True);
            int unlocks = 0;
            foreach (var reward in rewards)
            {
                Assert.That(roster[reward.DinoIndex].Id, Is.EqualTo("sena"));
                if (reward.NewlyUnlocked) unlocks++;
            }
            Assert.That(unlocks, Is.EqualTo(1));
            Assert.That(shop.Fragments(roster.Find("sena")), Is.EqualTo(299), "Never discard a paid egg or excess fragments.");
            Assert.That(shop.Balance, Is.EqualTo(500));
            Assert.That(shop.TryBuyTen(out _), Is.False);
            Assert.That(shop.Balance, Is.EqualTo(500));
        }

        [Test]
        public void TenPullsVaryRecipientsAndAmountsExcludeOwnedDinosAndLeaveUnityRandomAlone()
        {
            var shop = new RunnerShopProgress(roster, new System.Random(745));
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("red"), 100);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 500);
            var state = Random.state;
            try
            {
                Random.InitState(127);
                float expected = Random.value;
                Random.InitState(127);
                Assert.That(shop.TryBuyTen(out var rewards), Is.True);
                var recipients = new HashSet<int>();
                var amounts = new HashSet<int>();
                foreach (var reward in rewards)
                {
                    Assert.That(reward.DinoIndex, Is.InRange(2, roster.Count - 1));
                    Assert.That(reward.Fragments, Is.InRange(1, 20));
                    recipients.Add(reward.DinoIndex);
                    amounts.Add(reward.Fragments);
                }
                Assert.That(recipients.Count, Is.GreaterThan(1));
                Assert.That(amounts.Count, Is.GreaterThan(1));
                Assert.That(Random.value, Is.EqualTo(expected));
            }
            finally { Random.state = state; }
        }

        [Test]
        public void IncompleteRewardPreparationNeverChargesOrWritesPartialFragments()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 500);
            var shop = new RunnerShopProgress(roster, new FailingRandom());
            Assert.Throws<System.InvalidOperationException>(() => shop.TryBuyTen(out _));
            Assert.That(shop.Balance, Is.EqualTo(500));
            for (int i = 1; i < roster.Count; i++) Assert.That(shop.Fragments(i), Is.Zero);
        }

        private sealed class FailingRandom : System.Random
        {
            private int draws;
            public override int Next(int maxValue) => 0;
            public override int Next(int minValue, int maxValue)
            {
                if (++draws == 4) throw new System.InvalidOperationException("Simulated incomplete reward preparation");
                return 20;
            }
        }

        [TestCase("jestergift")]
        [TestCase("JESTERGIFT")]
        [TestCase("  JesterGift  ")]
        public void GiftCodeAddsOneThousandEggsRepeatedlyAndPersistsWithoutChangingDinos(string code)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 25);
            PlayerPrefs.SetInt(RunnerShopProgress.FragmentKey("red"), 42);
            var shop = new RunnerShopProgress(roster);
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                Assert.That(shop.Redeem(code), Is.EqualTo(RunnerShopProgress.RedeemResult.Success));
                shop = new RunnerShopProgress(roster);
                Assert.That(shop.Balance, Is.EqualTo(25 + attempt * 1000));
            }
            Assert.That(shop.Fragments(1), Is.EqualTo(42));
            Assert.That(shop.IsUnlocked(1), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("jester gift")]
        [TestCase("jestergift1000")]
        public void InvalidGiftCodesNeverChangeTheWallet(string code)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 77);
            var shop = new RunnerShopProgress(roster);
            Assert.That(shop.Redeem(code), Is.EqualTo(RunnerShopProgress.RedeemResult.InvalidCode));
            Assert.That(shop.Balance, Is.EqualTo(77));
        }

        [Test]
        public void GiftsNeverOverflowOrPartiallyCreditTheWalletAndDoNotNeedARoster()
        {
            var shop = new RunnerShopProgress(null);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue - 1000);
            Assert.That(shop.Redeem("jestergift"), Is.EqualTo(RunnerShopProgress.RedeemResult.Success));
            Assert.That(shop.Balance, Is.EqualTo(int.MaxValue));
            Assert.That(shop.Redeem("jestergift"), Is.EqualTo(RunnerShopProgress.RedeemResult.WalletFull));
            Assert.That(shop.Balance, Is.EqualTo(int.MaxValue));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue - 999);
            Assert.That(shop.Redeem("jestergift"), Is.EqualTo(RunnerShopProgress.RedeemResult.WalletFull));
            Assert.That(shop.Balance, Is.EqualTo(int.MaxValue - 999));
        }
    }
}
