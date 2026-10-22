using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class ForestLoopTextureTests
    {
        [TestCase("forest_sky")]
        [TestCase("forest_mountain")]
        [TestCase("forest_back")]
        [TestCase("forest_mid")]
        [TestCase("forest_long")]
        [TestCase("forest_short")]
        [TestCase("ground")]
        public void LoopTexturesHaveMatchingEdgesAndNoLossySampling(string name)
        {
            string path = $"Assets/Generated/ForestLoops/{name}_loop.png";
            var image = new Texture2D(2, 2);
            try
            {
                Assert.That(image.LoadImage(File.ReadAllBytes(path)), Is.True);
                Color32[] pixels = image.GetPixels32();
                int translucentEdgePixels = 0;
                for (int y = 0; y < image.height; y++)
                {
                    Assert.That(pixels[y * image.width], Is.EqualTo(pixels[y * image.width + image.width - 1]),
                        name + " has a discontinuity on row " + y);
                    byte alpha = pixels[y * image.width].a;
                    if (alpha > 0 && alpha < 255) translucentEdgePixels++;
                }
                Assert.That(translucentEdgePixels, Is.Zero, "Resampling must not introduce translucent seam pixels.");
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null);
                Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(image.width, image.height)));
                Assert.That(sprite.pivot.y, Is.Zero);
                Assert.That(sprite.vertices.Length, Is.EqualTo(4));
                Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
                Assert.That(sprite.texture.mipmapCount, Is.EqualTo(1));
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            }
            finally { Object.DestroyImmediate(image); }
        }
    }
}
