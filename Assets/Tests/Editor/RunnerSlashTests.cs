using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
        [Test] public void ChargesCostOneThousandCapAtThreeAndAreNeverFreeOnConsume()
        {
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 20000);
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.False);
            RunnerSlashProgress.Buy(RunnerSlashKind.Fire);
            for (int i = 1; i <= 3; i++) { Assert.That(RunnerSlashProgress.BuyCharge(), Is.True); Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(i)); }
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(12000));
            RunnerSlashProgress.Buy(RunnerSlashKind.Water);
            Assert.That(RunnerSlashProgress.Charges, Is.EqualTo(3), "Free style swaps retain the shared magazine.");
            for (int i = 0; i < 3; i++) Assert.That(RunnerSlashProgress.Consume(), Is.True);
            Assert.That(RunnerSlashProgress.Consume(), Is.False);
            Assert.That(PlayerPrefs.GetInt(RunnerShopProgress.WalletKey), Is.EqualTo(7000));
            PlayerPrefs.SetInt(RunnerShopProgress.WalletKey, 999);
            Assert.That(RunnerSlashProgress.BuyCharge(), Is.False);
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
    }
}
