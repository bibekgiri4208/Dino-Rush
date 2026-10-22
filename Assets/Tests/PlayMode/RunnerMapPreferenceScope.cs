using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    /// <summary>Existing forest regressions must not depend on the player's last chosen map.</summary>
    [SetUpFixture]
    public sealed class RunnerMapPreferenceScope
    {
        private bool hadMap;
        private string previousMap;

        [OneTimeSetUp]
        public void UseForestForRegressions()
        {
            hadMap = PlayerPrefs.HasKey(RunnerMapCatalog.SelectionKey);
            previousMap = PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, "forest");
        }

        [OneTimeTearDown]
        public void RestorePlayerChoice()
        {
            if (hadMap) PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, previousMap);
            else PlayerPrefs.DeleteKey(RunnerMapCatalog.SelectionKey);
            PlayerPrefs.Save();
        }
    }
}
