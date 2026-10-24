using UnityEngine;

namespace DinoRush
{
    // Keep Smoke's ID/key for existing owners; its displayed name is now WIND.
    public enum RunnerTrailKind { None, Smoke, Fire, Electric, Frost, Blossom, Cosmic }

    /// <summary>Permanent cosmetic ownership, using the same egg wallet as mystery eggs.</summary>
    public static class RunnerTrailProgress
    {
        public const int Cost = 2000;
        public const int Count = 6;
        public const string SelectionKey = "DinoRush.Trails.Equipped";
        public static bool Valid(RunnerTrailKind kind) => kind >= RunnerTrailKind.Smoke && kind <= RunnerTrailKind.Cosmic;
        public static string OwnershipKey(RunnerTrailKind kind) => "DinoRush.Trails.Owned." + kind.ToString().ToLowerInvariant();
        public static bool Owned(RunnerTrailKind kind) => Valid(kind) && PlayerPrefs.GetInt(OwnershipKey(kind)) == 1;
        public static RunnerTrailKind Equipped
        {
            get
            {
                var kind = (RunnerTrailKind)PlayerPrefs.GetInt(SelectionKey);
                return Owned(kind) ? kind : RunnerTrailKind.None;
            }
        }
        public static bool Buy(RunnerTrailKind kind)
        {
            int balance = Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            if (!Valid(kind) || Owned(kind) || balance < Cost) return false;
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, balance - Cost);
            PlayerPrefs.SetInt(OwnershipKey(kind), 1);
            PlayerPrefs.Save();
            return true;
        }
        public static bool Equip(RunnerTrailKind kind)
        {
            if (kind != RunnerTrailKind.None && !Owned(kind)) return false;
            PlayerPrefs.SetInt(SelectionKey, (int)kind);
            PlayerPrefs.Save();
            return true;
        }
    }
}
