using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class RunnerSlashTests
    {
        private readonly Dictionary<string, int> old = new Dictionary<string, int>();
        private readonly HashSet<string> existing = new HashSet<string>();
        [SetUp] public void Save()
        {
            var keys = new List<string> { RunnerShopProgress.WalletKey, RunnerSlashProgress.SelectionKey, RunnerSlashProgress.ChargesKey };
            for (int i = 1; i <= 3; i++) keys.Add(RunnerSlashProgress.OwnershipKey((RunnerSlashKind)i));
            foreach (string key in keys) { if (PlayerPrefs.HasKey(key)) existing.Add(key); old[key] = PlayerPrefs.GetInt(key); PlayerPrefs.DeleteKey(key); }
        }
        [TearDown] public void Restore()
        {
            foreach (var pair in old) if (existing.Contains(pair.Key)) PlayerPrefs.SetInt(pair.Key, pair.Value); else PlayerPrefs.DeleteKey(pair.Key);
            old.Clear(); existing.Clear(); PlayerPrefs.Save();
        }
        [TestCase(RunnerSlashKind.Fire)] [TestCase(RunnerSlashKind.Lightning)] [TestCase(RunnerSlashKind.Water)]
        public void StylesCostFiveThousandPermanentlyAndIncludeNoFreeCharges(RunnerSlashKind kind)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 4999);
            Assert.That(RunnerSlashProgress.Buy(kind), Is.False);
            Assert.That(RunnerSlashProgress.Equip(kind), Is.False);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 12000);
            Assert.That(RunnerSlashProgress.Buy(kind), Is.True);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(7000));
            Assert.That(RunnerSlashProgress.Equipped, Is.EqualTo(kind));
            Assert.That(RunnerSlashProgress.Charges, Is.Zero);
            Assert.That(RunnerSlashProgress.Buy(kind), Is.False);
            Assert.That(RunnerSlashProgress.Equip(kind), Is.True);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(7000));
        }
        [Test] public void ChargesCostOneThousandCanBeStockpiledAndAreNeverFreeOnConsume()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 30000);
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.False);
            RunnerSlashProgress.Buy(RunnerSlashKind.Fire);
            for (int i = 1; i <= 12; i++) { Assert.That(RunnerSlashProgress.BuyCharge(), Is.True); Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(i)); }
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(13000));
            RunnerSlashProgress.Buy(RunnerSlashKind.Water);
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(12), "Style swaps retain all shared stored charges.");
            for (int i = 0; i < 12; i++) Assert.That(RunnerSlashProgress.Consume(), Is.True);
            Assert.That(RunnerSlashProgress.Consume(), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(8000));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 999);
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.False);
        }
        [Test] public void StoredChargesRetainLargeValuesAndRejectOverflowWithoutTakingEggs()
        {
            PlayerPrefs.SetInt(RunnerSlashProgress.OwnershipKey(RunnerSlashKind.Fire), 1);
            RunnerSlashProgress.Equip(RunnerSlashKind.Fire);
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 2000);
            PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, int.MaxValue - 1);
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.True);
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(int.MaxValue));
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(1000));
            Assert.That(RunnerSlashProgress.Consume(), Is.True);
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(int.MaxValue - 1));
            PlayerPrefs.SetInt(RunnerSlashProgress.ChargesKey, -8);
            Assert.That(RunnerSlashProgress.Charges, Is.Zero);
            Assert.That(RunnerSlashProgress.Consume(), Is.False);
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(4)]
        public void InvalidStylesCannotSpendOrEquip(int id)
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 20000);
            Assert.That(RunnerSlashProgress.Buy((RunnerSlashKind)id), Is.False);
            Assert.That(RunnerSlashProgress.Equip((RunnerSlashKind)id), Is.False);
            PlayerPrefs.SetInt(RunnerSlashProgress.SelectionKey, id);
            Assert.That(RunnerSlashProgress.Equipped, Is.EqualTo(RunnerSlashKind.None));
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(20000));
        }
        [Test] public void AllThreeCatalogsReferenceTheTwelveSuppliedFrames()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RunnerSlashCatalog>("Assets/RunnerSlashCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            for (int i = 1; i <= 3; i++)
            {
                Sprite[] frames = catalog.Frames((RunnerSlashKind)i); Assert.That(frames.Length, Is.EqualTo(12));
                for (int j = 0; j < frames.Length; j++)
                {
                    Assert.That(frames[j], Is.Not.Null);
                    Assert.That(AssetDatabase.GetAssetPath(frames[j].texture), Does.Contain("Slash attack/").And.Contain("Frame_" + (j + 1).ToString("00")));
                    Assert.That(frames[j].rect.size, Is.EqualTo(new Vector2(frames[j].texture.width, frames[j].texture.height)));
                }
            }
        }

        [Test] public void AuthoredCountsSeparateStockFromTheThreeRoundSlots()
        {
            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ForestRun.unity");
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ForestRun.unity", OpenSceneMode.Additive);
            try
            {
                RunnerSlashHud hud = null; RunnerSlashShop shop = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (hud == null) hud = root.GetComponentInChildren<RunnerSlashHud>(true);
                    if (shop == null) shop = root.GetComponentInChildren<RunnerSlashShop>(true);
                }
                Assert.That(hud, Is.Not.Null); Assert.That(shop, Is.Not.Null);
                var data = new SerializedObject(hud);
                Assert.That(data.FindProperty("stock").objectReferenceValue, Is.Not.Null);
                Assert.That(data.FindProperty("hint").objectReferenceValue, Is.Not.Null);
                var pips = data.FindProperty("pips");
                Assert.That(pips.arraySize, Is.EqualTo(RunnerSlashProgress.MaxUsesPerRound));
                for (int i = 0; i < pips.arraySize; i++)
                {
                    var image = pips.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                    Assert.That(image, Is.Not.Null); Assert.That(image.raycastTarget, Is.False);
                }
                Assert.That(hud.transform.Find("Slash indicator/Slash charges").GetComponent<Text>().text, Does.Contain("ROUND 3/3"));
                Assert.That(shop.transform.Find("Slashes section/Slash loadout").GetComponent<Text>().text, Does.Contain("STOCK").And.Not.Contain("/3"));
                Assert.That(shop.transform.Find("Slashes section/Charge rules").GetComponent<Text>().text, Does.Contain("3 PER ROUND"));
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        [TestCase(RunnerSlashKind.Fire)] [TestCase(RunnerSlashKind.Lightning)] [TestCase(RunnerSlashKind.Water)]
        public void ElementalSoundsShipAsPreloadedMonoSamplesWithSourceCredits(RunnerSlashKind kind)
        {
            string path = "Assets/Resources/Audio/Slashes/" + kind + " slash.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Assert.That(clip, Is.Not.Null);
            Assert.That(Resources.Load<AudioClip>("Audio/Slashes/" + kind + " slash"), Is.SameAs(clip));
            Assert.That(clip.channels, Is.EqualTo(1));
            Assert.That(clip.frequency, Is.EqualTo(44100));
            Assert.That(clip.length, kind == RunnerSlashKind.Lightning ? Is.InRange(0.7f, 0.85f) : Is.InRange(0.25f, 0.8f));
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.True);
            Assert.That(importer.loadInBackground, Is.False);
            Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(AudioCompressionFormat.PCM));
            var credits = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Audio/Slashes/SOURCES.txt");
            Assert.That(credits.text, Does.Contain(kind + " slash.wav").And.Contain("CC0"));
        }
    }
}
