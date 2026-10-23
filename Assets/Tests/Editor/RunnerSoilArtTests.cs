using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerSoilArtTests
    {
        [Test]
        public void SavedSoilArtIsOpaqueTexturedAndUsesUncompressedFullRectPixelSprites()
        {
            foreach (string map in new[] { "forest", "desert", "jungle", "rural" })
            {
                string path = $"Assets/Generated/TrailSoil/{map}_soil.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer, Is.Not.Null);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(RunnerSoilArt.PixelsPerUnit));
                Assert.That(sprite.pivot.y, Is.EqualTo(RunnerSoilArt.Height));
                var texture = new Texture2D(2, 2);
                try
                {
                    texture.LoadImage(System.IO.File.ReadAllBytes(path));
                    var colors = new HashSet<Color32>();
                    bool opaque = true;
                    foreach (Color32 pixel in texture.GetPixels32())
                    {
                        opaque &= pixel.a == 255;
                        colors.Add(pixel);
                    }
                    Assert.That(opaque, Is.True);
                    Assert.That(colors.Count, Is.GreaterThan(100), "Replace the solid rectangle with real soil detail.");
                }
                finally { Object.DestroyImmediate(texture); }
            }
        }

        [Test]
        public void GeneratingSoilIsDeterministicAndDoesNotConsumeGameplayRandomness()
        {
            Random.State before = Random.state;
            Color surface = new Color32(19, 48, 49, 255);
            Texture2D first = RunnerSoilArt.CreateTexture(surface, "forest");
            Texture2D second = RunnerSoilArt.CreateTexture(surface, "forest");
            try
            {
                Assert.That(Random.state, Is.EqualTo(before));
                Assert.That(first.GetPixels32(), Is.EqualTo(second.GetPixels32()));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
