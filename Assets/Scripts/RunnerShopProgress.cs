using UnityEngine;

namespace DinoRush
{
    /// <summary>Locally saved egg wallet and per-dino fragments. Uses no gameplay random state.</summary>
    public sealed class RunnerShopProgress
    {
        public const string WalletKey = "DinoRush.Shop.Eggs";
        public const int MysteryEggCost = 50;
        public const int RequiredFragments = 100;
        public const int MinFragments = 1;
        public const int MaxFragments = 20;

        private readonly RunnerDinoRoster roster;
        private readonly System.Random random;

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

        public void CollectEgg()
        {
            if (Balance == int.MaxValue) return;
            PlayerPrefs.SetInt(WalletKey, Balance + 1);
            PlayerPrefs.Save(); // Credit once at pickup, never again on results/retry/menu.
        }

        public bool TryBuy(out int dinoIndex, out int fragments, out bool unlocked)
        {
            dinoIndex = -1;
            fragments = 0;
            unlocked = false;
            if (!CanBuy) return false;

            int choice = random.Next(LockedCount);
            for (int i = 1; i < roster.Count; i++)
            {
                if (IsUnlocked(i) || choice-- != 0) continue;
                dinoIndex = i;
                break;
            }
            fragments = random.Next(MinFragments, MaxFragments + 1);
            int total = Fragments(dinoIndex) + fragments;
            PlayerPrefs.SetInt(WalletKey, Balance - MysteryEggCost);
            PlayerPrefs.SetInt(FragmentKey(roster[dinoIndex].Id), total);
            unlocked = total >= RequiredFragments;
            PlayerPrefs.Save();
            return true;
        }
    }
}
