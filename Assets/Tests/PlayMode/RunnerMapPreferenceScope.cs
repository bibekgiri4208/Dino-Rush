using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace DinoRush.Tests
{
    /// <summary>Existing forest regressions must not depend on the player's last chosen map.</summary>
    [SetUpFixture]
    public sealed class RunnerMapPreferenceScope
    {
        private bool hadMap;
        private string previousMap;
        private readonly Dictionary<string, int> shopPrefs = new Dictionary<string, int>();
        private readonly HashSet<string> existingShopPrefs = new HashSet<string>();
        internal static IEnumerable<string> ShopKeys
        {
            get
            {
                yield return RunnerShopProgress.WalletKey;
                foreach (string id in new[] { "red", "yellow", "green", "cole", "kira", "kuro", "loki", "mono", "sena" })
                    yield return RunnerShopProgress.FragmentKey(id);
            }
        }

        [OneTimeSetUp]
        public void UseForestForRegressions()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "forest");
            // Animation/map regressions exercise every dino. Shop-specific tests use a fresh locked profile.
            foreach (string key in ShopKeys)
            {
                if (PlayerPrefs.HasKey(key)) existingShopPrefs.Add(key);
                shopPrefs[key] = PlayerPrefs.GetInt(key);
                PlayerPrefs.SetInt(key, key == RunnerShopProgress.WalletKey ? 0 : RunnerShopProgress.RequiredFragments);
            }
        }

        [OneTimeTearDown]
        public void RestorePlayerChoice()
        {
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, previousMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            foreach (var preference in shopPrefs)
                if (existingShopPrefs.Contains(preference.Key)) PlayerPrefs.SetInt(preference.Key, preference.Value);
                else PlayerPrefs.DeleteKey(preference.Key);
            PlayerPrefs.Save();
        }
    }
}
