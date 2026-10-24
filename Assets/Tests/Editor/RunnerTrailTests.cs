using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerTrailTests
    {
        private readonly Dictionary<string, int> old = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        [SetUp]
        public void Save()
        {
            old.Clear(); existing.Clear();
            var keys = new List<string> { RunnerTrailProgress.SelectionKey, RunnerShopProgress.WalletKey };
            for (int i = 1; i <= RunnerTrailProgress.Count; i++) keys.Add(RunnerTrailProgress.OwnershipKey((RunnerTrailKind)i));
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) existing.Add(key);
                old[key] = PlayerPrefs.GetInt(key); PlayerPrefs.DeleteKey(key);
            }
        }
        [TearDown]
        public void Restore()
        {
            foreach (var pair in old)
                if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save();
        }
        [TestCase(RunnerTrailKind.Smoke)]
        [TestCase(RunnerTrailKind.Fire)]
        [TestCase(RunnerTrailKind.Electric)]
        [TestCase(RunnerTrailKind.Frost)]
        [TestCase(RunnerTrailKind.Blossom)]
        [TestCase(RunnerTrailKind.Cosmic)]
        public void EachTrailCostsExactlyTwoThousandAndCanOnlyBeBoughtOnce(RunnerTrailKind kind)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 1999);
            Assert.That(RunnerTrailProgress.Buy(kind), Is.False);
            Assert.That(RunnerTrailProgress.Equip(kind), Is.False);
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.None));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(1999));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 2025);
            Assert.That(RunnerTrailProgress.Buy(kind), Is.True);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(25));
            Assert.That(RunnerTrailProgress.Owned(kind), Is.True);
            Assert.That(RunnerTrailProgress.Equip(kind), Is.True);
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(kind));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 5000);
            Assert.That(RunnerTrailProgress.Buy(kind), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(5000));
            Assert.That(RunnerTrailProgress.Equip(RunnerTrailKind.None), Is.True);
            Assert.That(RunnerTrailProgress.Owned(kind), Is.True);
            Assert.That(RunnerTrailProgress.Equip(kind), Is.True);
        }
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(7)]
        [TestCase(999)]
        public void UnknownOrUnownedSavedTrailsSafelyFallBackToNone(int id)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, int.MaxValue);
            PlayerPrefs.SetInt(RunnerTrailProgress.SelectionKey, id);
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.None));
            Assert.That(RunnerTrailProgress.Buy((RunnerTrailKind)id), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(int.MaxValue));
        }
        [Test]
        public void WindRetainsExistingSmokeOwnershipAndSelectionWithoutChargingAgain()
        {
            PlayerPrefs.SetInt("DinoRush.Trails.Owned.smoke", 1);
            PlayerPrefs.SetInt(RunnerTrailProgress.SelectionKey, 1);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 345);
            Assert.That(RunnerTrailProgress.OwnershipKey(RunnerTrailKind.Smoke), Is.EqualTo("DinoRush.Trails.Owned.smoke"));
            Assert.That(RunnerTrailProgress.Owned(RunnerTrailKind.Smoke), Is.True);
            Assert.That(RunnerTrailProgress.Equipped, Is.EqualTo(RunnerTrailKind.Smoke));
            Assert.That(RunnerTrailProgress.Buy(RunnerTrailKind.Smoke), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(345));
        }
        [Test]
        public void SavedShopHasSeparateCategoriesAndAllThreeLivePreviews()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                RunnerTrailShop shop = null;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<RunnerTrailShop>(true) != null) shop = root.GetComponentInChildren<RunnerTrailShop>(true);
                Assert.That(shop, Is.Not.Null);
                Assert.That(shop.transform.Find("Single adventure").gameObject.activeSelf, Is.False, "The retired Shop slogan must remain hidden.");
                Assert.That(shop.GetComponentsInChildren<RunnerTrailPreview>(true).Length, Is.EqualTo(9));
                Assert.That(shop.transform.Find("Trails section").gameObject.activeSelf, Is.False);
                for (int i = 0; i < RunnerTrailProgress.Count; i++)
                {
                    var previews = shop.transform.Find("Trails section/Trail card " + (i + 1)).GetComponentsInChildren<RunnerTrailPreview>(true);
                    Assert.That(previews.Length, Is.EqualTo(i < 3 ? 2 : 1));
                    Assert.That(previews[0].Kind, Is.EqualTo((RunnerTrailKind)(i + 1)));
                    Assert.That(previews[0].Foreground, Is.False);
                    if (i < 3)
                    {
                        Assert.That(previews[1].Kind, Is.EqualTo((RunnerTrailKind)(i + 1)));
                        Assert.That(previews[1].Foreground, Is.True);
                        Assert.That(previews[1].gameObject.activeSelf, Is.False, "Legacy foreground shells must stay disabled.");
                    }
                    string name = shop.transform.Find("Trails section/Trail card " + (i + 1) + "/Trail name").GetComponent<Text>().text;
                    Assert.That(name, Is.EqualTo(new[] { "WIND", "INFERNO", "VOLTAGE", "FROST", "BLOSSOM", "COSMIC" }[i]));
                }
                Assert.That(shop.transform.Find("Dinos category").GetComponent<Button>(), Is.Not.Null);
                Assert.That(shop.transform.Find("Trails category").GetComponent<Button>(), Is.Not.Null);
                Assert.That(shop.PageCount, Is.EqualTo(2));
                Assert.That(shop.transform.Find("Trails section/Next trails page").GetComponent<Button>(), Is.Not.Null);
                Assert.That(shop.transform.Find("Trails section/Trail page indicator").GetComponent<Text>(), Is.Not.Null);
                Assert.That(shop.transform.Find("Trails section/Equipped trail summary").GetComponent<Text>(), Is.Not.Null);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
