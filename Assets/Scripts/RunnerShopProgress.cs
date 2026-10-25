using UnityEngine;

namespace DinoRush
{
    /// <summary>Locally saved egg wallet and per-dino fragments. Uses no gameplay random state.</summary>
    public sealed class RunnerShopProgress
    {
        public const string WalletKey = "DinoRush.Shop.Eggs";
        public const int MysteryEggCost = 50;
        public const int TenPullCount = 10;
        public const int TenPullCost = MysteryEggCost * TenPullCount;
        public const int RequiredFragments = 100;
        public const int MinFragments = 1;
        public const int MaxFragments = 20;
        public const string GiftCode = "jestergift";
        public const int GiftEggs = 10000;
        public enum RedeemResult { Success, InvalidCode, WalletFull }

        private readonly RunnerDinoRoster roster;
        private readonly System.Random random;

        public readonly struct Reward
        {
            public readonly int DinoIndex, Fragments, Total;
            public readonly bool NewlyUnlocked;
            public Reward(int dinoIndex, int fragments, int total, bool newlyUnlocked)
            {
                DinoIndex = dinoIndex;
                Fragments = fragments;
                Total = total;
                NewlyUnlocked = newlyUnlocked;
            }
        }

        public RunnerShopProgress(RunnerDinoRoster roster, System.Random random = null)
        {
            this.roster = roster;
            this.random = random ?? new System.Random();
        }

        public static string FragmentKey(string dinoId) => "DinoRush.Shop.Fragments." + dinoId;
        public int Balance => Mathf.Max(0, PlayerPrefs.GetInt(WalletKey, 0));
        public int Fragments(int index) => Valid(index)
            ? Mathf.Max(0, PlayerPrefs.GetInt(FragmentKey(roster[index].Id), 0)) : 0;
        public bool IsUnlocked(int index) => Valid(index) && (index == 0 || Fragments(index) >= RequiredFragments);
        private bool Valid(int index) => roster != null && index >= 0 && index < roster.Count;

        public int LockedCount
        {
            get
            {
                int count = 0;
                if (roster != null)
                    for (int i = 1; i < roster.Count; i++) if (!IsUnlocked(i)) count++;
                return count;
            }
        }

        public bool CanBuy => Balance >= MysteryEggCost && LockedCount > 0;
        public bool CanBuyTen => Balance >= TenPullCost && LockedCount > 0;

        public void CollectEgg()
        {
            if (Balance == int.MaxValue) return;
            PlayerPrefs.SetInt(WalletKey, Balance + 1);
            PlayerPrefs.Save(); // Credit once at pickup, never again on results/retry/menu.
        }

        public RedeemResult Redeem(string code)
        {
            if (!string.Equals(code?.Trim(), GiftCode, System.StringComparison.OrdinalIgnoreCase))
                return RedeemResult.InvalidCode;
            if (Balance > int.MaxValue - GiftEggs) return RedeemResult.WalletFull;
            PlayerPrefs.SetInt(WalletKey, Balance + GiftEggs);
            PlayerPrefs.Save(); // Repeatable by design; the gift is saved before any animation.
            return RedeemResult.Success;
        }

        public bool TryBuy(out int dinoIndex, out int fragments, out bool unlocked)
        {
            dinoIndex = -1;
            fragments = 0;
            unlocked = false;
            if (!TryBuyBatch(1, out Reward[] rewards)) return false;
            dinoIndex = rewards[0].DinoIndex;
            fragments = rewards[0].Fragments;
            unlocked = rewards[0].NewlyUnlocked;
            return true;
        }

        public bool TryBuyTen(out Reward[] rewards) => TryBuyBatch(TenPullCount, out rewards);

        private bool TryBuyBatch(int count, out Reward[] rewards)
        {
            rewards = System.Array.Empty<Reward>();
            int cost = count * MysteryEggCost;
            if (Balance < cost || LockedCount == 0) return false;
            // Eligibility is fixed before any rewards are applied: all ten paid eggs deliver,
            // even when the last locked dino crosses 100 partway through the batch.
            var eligible = new int[LockedCount];
            var totals = new int[roster.Count];
            int cursor = 0;
            for (int i = 1; i < roster.Count; i++)
            {
                totals[i] = Fragments(i);
                if (!IsUnlocked(i)) eligible[cursor++] = i;
            }
            var pending = new Reward[count];
            for (int i = 0; i < count; i++)
            {
                int index = eligible[random.Next(eligible.Length)];
                int amount = random.Next(MinFragments, MaxFragments + 1);
                int before = totals[index];
                totals[index] += amount;
                pending[i] = new Reward(index, amount, totals[index], before < RequiredFragments && totals[index] >= RequiredFragments);
            }
            // No preferences are touched until every reward is prepared. Animation is presentation only.
            PlayerPrefs.SetInt(WalletKey, Balance - cost);
            foreach (int index in eligible)
                if (totals[index] != Fragments(index)) PlayerPrefs.SetInt(FragmentKey(roster[index].Id), totals[index]);
            PlayerPrefs.Save();
            rewards = pending;
            return true;
        }
    }
}
