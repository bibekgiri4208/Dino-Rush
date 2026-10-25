using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Explicit account keys only: never deletes sound settings or unrelated preferences.</summary>
    public static class RunnerAccountProgress
    {
        public const string HighScoreKey = "DinoRush.Forest.HighScore";

        public static IEnumerable<string> Keys(RunnerDinoRoster roster)
        {
            yield return RunnerShopProgress.WalletKey;
            yield return RunnerDinoRoster.SelectionKey;
            yield return RunnerMapCatalog.SelectionKey;
            yield return HighScoreKey;
            yield return RunnerDailyMissions.SaveKey;
            if (roster != null)
                for (int i = 0; i < roster.Count; i++) yield return RunnerShopProgress.FragmentKey(roster[i].Id);
            yield return RunnerTrailProgress.SelectionKey;
            for (int i = 1; i <= RunnerTrailProgress.Count; i++)
                yield return RunnerTrailProgress.OwnershipKey((RunnerTrailKind)i);
            yield return RunnerSlashProgress.SelectionKey;
            yield return RunnerSlashProgress.ChargesKey;
            for (int i = 1; i <= (int)RunnerSlashKind.Water; i++)
                yield return RunnerSlashProgress.OwnershipKey((RunnerSlashKind)i);
        }

        public static void Reset(RunnerDinoRoster roster)
        {
            foreach (string key in Keys(roster)) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
