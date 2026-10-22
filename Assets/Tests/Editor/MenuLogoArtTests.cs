using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class MenuLogoArtTests
    {
        private const string Path = "Assets/UI/ForestMenu/DinoRushLogo.png";
        private Texture2D texture;

        [SetUp]
        public void LoadLogo()
        {
            texture = new Texture2D(2, 2);
            Assert.That(texture.LoadImage(File.ReadAllBytes(Path)), Is.True);
            Assert.That(texture.width, Is.EqualTo(340));
            Assert.That(texture.height, Is.EqualTo(166));
        }

        [TearDown]
        public void ReleaseLogo() => Object.DestroyImmediate(texture);

        [Test]
        public void LeafyEdgesAreMirroredAndHaveTransparentPadding()
        {
            int foliagePixels = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < 56; x++)
            {
                Color32 left = texture.GetPixel(x, y);
                Color32 right = texture.GetPixel(texture.width - 1 - x, y);
                bool Leaf(Color32 pixel) => pixel.a > 0 && pixel.g > pixel.r && pixel.g > pixel.b;
                if (!Leaf(left) && !Leaf(right)) continue;
                foliagePixels++;
                Assert.That(left, Is.EqualTo(right), $"Unbalanced foliage at {x}, {y}");
                Assert.That(x, Is.GreaterThanOrEqualTo(24), "Leaves must not protrude into the outer padding.");
                Assert.That(y, Is.InRange(18, 141), "Trim must not crowd the word's top or bottom.");
            }
            Assert.That(foliagePixels, Is.GreaterThan(200));
            for (int y = 0; y < texture.height; y++)
            {
                Assert.That(texture.GetPixel(0, y).a, Is.Zero);
                Assert.That(texture.GetPixel(texture.width - 1, y).a, Is.Zero);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SideTrimHasNoDetachedFragments(bool right)
        {
            const int width = 56;
            Color32[] pixels = texture.GetPixels32();
            bool Opaque(int x, int y) => pixels[y * texture.width + (right ? texture.width - 1 - x : x)].a > 0;
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            int components = 0;
            Vector2Int[] offsets = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < width; x++)
            {
                var pixel = new Vector2Int(x, y);
                if (!Opaque(x, y) || !visited.Add(pixel)) continue;
                components++;
                queue.Enqueue(pixel);
                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    foreach (Vector2Int offset in offsets)
                    {
                        Vector2Int next = current + offset;
                        if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= texture.height) continue;
                        if (Opaque(next.x, next.y) && visited.Add(next)) queue.Enqueue(next);
                    }
                }
            }
            Assert.That(components, Is.EqualTo(1), "Every leaf must connect to the trim, not float beside the title.");
        }

        [Test]
        public void LogoRetainsUncompressedPixelArtImportSettings()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        }
    }
}
