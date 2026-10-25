using UnityEngine;

namespace DinoRush
{
    public enum RunnerSlashKind { None, Fire, Lightning, Water }

    /// <summary>Permanent styles and shared stored charges, independent of the three-use round limit.</summary>
    public static class RunnerSlashProgress
    {
        public const int UnlockCost = 5000, ChargeCost = 1000, MaxUsesPerRound = 3;
        public const string SelectionKey = "DinoRush.Slash.Equipped", ChargesKey = "DinoRush.Slash.Charges";
        public static bool Valid(RunnerSlashKind kind) => kind >= RunnerSlashKind.Fire && kind <= RunnerSlashKind.Water;
        public static string OwnershipKey(RunnerSlashKind kind) => "DinoRush.Slash.Owned." + kind.ToString().ToLowerInvariant();
        public static bool Owned(RunnerSlashKind kind) => Valid(kind) && PlayerPrefs.GetInt(OwnershipKey(kind)) == 1;
        public static RunnerSlashKind Equipped => Owned((RunnerSlashKind)PlayerPrefs.GetInt(SelectionKey)) ? (RunnerSlashKind)PlayerPrefs.GetInt(SelectionKey) : RunnerSlashKind.None;
        public static int Charges => Mathf.Max(0, PlayerPrefs.GetInt(ChargesKey));
        public static bool Buy(RunnerSlashKind kind)
        {
            int balance = Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            if (!Valid(kind) || Owned(kind) || balance < UnlockCost) return false;
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, balance - UnlockCost);
            PlayerPrefs.SetInt(OwnershipKey(kind), 1);
            PlayerPrefs.SetInt(SelectionKey, (int)kind);
            PlayerPrefs.Save();
            return true;
        }
        public static bool Equip(RunnerSlashKind kind)
        {
            if (!Owned(kind)) return false;
            PlayerPrefs.SetInt(SelectionKey, (int)kind); PlayerPrefs.Save(); return true;
        }
        public static bool BuyCharge()
        {
            int balance = Mathf.Max(0, PlayerPrefs.GetInt(RunnerShopProgress.WalletKey));
            // Guard the save format's integer boundary, not a gameplay inventory cap.
            if (Equipped == RunnerSlashKind.None || Charges == int.MaxValue || balance < ChargeCost) return false;
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, balance - ChargeCost);
            PlayerPrefs.SetInt(ChargesKey, Charges + 1); PlayerPrefs.Save(); return true;
        }
        public static bool Consume()
        {
            if (Equipped == RunnerSlashKind.None || Charges <= 0) return false;
            PlayerPrefs.SetInt(ChargesKey, Charges - 1); PlayerPrefs.Save(); return true;
        }
    }
}
