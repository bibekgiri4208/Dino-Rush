using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class MenuButtonLayoutTests
    {
        [UnityTest]
        public IEnumerator MainButtonsShareShapeSpacingAndTypography()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            Canvas.ForceUpdateCanvases();
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            // Measure design-space pixel gaps at the native canvas scale. Dynamic
            // font hinting at a fractional Game view scale changes glyph bounds.
            Canvas canvas = menu.GetComponentInParent<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.scaleFactor = 1;
            foreach (Text label in menu.GetComponentsInChildren<Text>()) label.SetVerticesDirty();
            Canvas.ForceUpdateCanvases();
            string[] names = { "Play", "Customize", "Options", "Quit" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform button = menu.transform.Find("Main menu buttons/" + names[i]);
                var rect = (RectTransform)button;
                Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(360, 68)), names[i]);
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0, -12 - i * 78)));
                Image background = button.GetComponent<Image>();
                Assert.That(background.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(background.sprite.rect.size, Is.EqualTo(new Vector2(180, 34)));
                Assert.That(background.sprite.border, Is.EqualTo(new Vector4(24, 10, 24, 10)));
                Text text = button.Find("Label").GetComponent<Text>();
                Assert.That(text.fontSize, Is.EqualTo(24));
                Assert.That(text.rectTransform.anchoredPosition.y, Is.Zero);
                ButtonLetterSpacing tracking = text.GetComponent<ButtonLetterSpacing>();
                Assert.That(tracking, Is.Not.Null);
                Assert.That(tracking.Spacing, Is.EqualTo(2));
                Assert.That(tracking.PreferredWidth, Is.LessThanOrEqualTo(text.rectTransform.rect.width));

                // Verify the actual generated glyph mesh, not just the component settings.
                var source = text.cachedTextGenerator.verts;
                Assert.That(source.Count / 4, Is.EqualTo(text.text.Length));
                float textLeft = float.PositiveInfinity, textRight = float.NegativeInfinity;
                for (int v = 0; v < source.Count; v++)
                {
                    float x = source[v].position.x / text.pixelsPerUnit
                        + (v / 4 - (text.text.Length - 1) * 0.5f) * tracking.Spacing
                        + text.rectTransform.anchoredPosition.x;
                    textLeft = Mathf.Min(textLeft, x);
                    textRight = Mathf.Max(textRight, x);
                }
                Image icon = button.Find("Icon").GetComponent<Image>();
                var texture = new Texture2D(2, 2);
                try
                {
                    Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes($"Assets/UI/ForestMenu/Icon{(names[i] == "Customize" ? "Dino" : names[i] == "Options" ? "Gear" : names[i])}.png")), Is.True);
                    int min = texture.width, max = 0;
                    for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (texture.GetPixel(x, y).a > 0)
                        {
                            min = Mathf.Min(min, x);
                            max = Mathf.Max(max, x + 1);
                        }
                    float width = icon.rectTransform.rect.width;
                    float iconLeft = icon.rectTransform.anchoredPosition.x + (float)min / texture.width * width - width * 0.5f;
                    float iconRight = icon.rectTransform.anchoredPosition.x + (float)max / texture.width * width - width * 0.5f;
                    Assert.That(textLeft - iconRight, Is.EqualTo(12).Within(1), names[i] + " has an inconsistent visible icon/text gap");
                    Assert.That((iconLeft + textRight) * 0.5f, Is.EqualTo(0).Within(1), names[i] + " content is not centered as a group");
                }
                finally { Object.DestroyImmediate(texture); }
                using (var mesh = new VertexHelper())
                {
                    for (int glyph = 0; glyph < source.Count / 4; glyph++)
                    {
                        var quad = new UIVertex[4];
                        for (int v = 0; v < 4; v++) quad[v] = source[glyph * 4 + v];
                        mesh.AddUIVertexQuad(quad);
                    }
                    tracking.ModifyMesh(mesh);
                    var vertex = new UIVertex();
                    for (int glyph = 0; glyph < text.text.Length; glyph++)
                    {
                        mesh.PopulateUIVertex(ref vertex, glyph * 6);
                        float expected = (glyph - (text.text.Length - 1) * 0.5f) * 2;
                        Assert.That(vertex.position.x - source[glyph * 4].position.x,
                            Is.EqualTo(expected).Within(0.01f));
                    }
                }
            }
        }
    }
}
