using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class MenuButtonArtTests
    {
        [TestCase("ButtonPlay")]
        [TestCase("ButtonForest")]
        public void ButtonFramesHaveMatchingSymmetricSilhouettes(string name)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes($"Assets/UI/ForestMenu/{name}.png")), Is.True);
                Assert.That(texture.width, Is.EqualTo(180));
                Assert.That(texture.height, Is.EqualTo(34));
                Color32[] pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                {
                    Color32 pixel = pixels[y * texture.width + x];
                    Assert.That(pixel.a, Is.EqualTo(pixels[y * texture.width + texture.width - 1 - x].a));
                    Assert.That(pixel.a, Is.EqualTo(pixels[(texture.height - 1 - y) * texture.width + x].a));
                    if (x < 24)
                        Assert.That(pixel, Is.EqualTo(pixels[y * texture.width + texture.width - 1 - x]), "Corner decorations must mirror.");
                }
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
