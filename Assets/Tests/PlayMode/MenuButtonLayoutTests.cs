using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    public sealed class MenuButtonLayoutTests
    {
        [UnityTest]
        public IEnumerator TitleLogoKeepsItsAspectAndUncroppedEdges()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            Image logo = menu.transform.Find("Dino Rush Logo/Mossy stone and amber logo").GetComponent<Image>();
            Assert.That(logo.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(logo.preserveAspect, Is.True);
            Assert.That(logo.sprite.rect.size, Is.EqualTo(new Vector2(340, 166)));
            Assert.That(logo.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(logo.rectTransform.sizeDelta, Is.EqualTo(new Vector2(578, 282)));
            yield return ForestRunTests.Capture("menu-refined-logo");
            yield return ForestRunTests.Capture("menu-refined-logo-tablet", 1024, 768);
            yield return ForestRunTests.Capture("menu-refined-logo-wide", 1920, 820);
        }

        [UnityTest]
        public IEnumerator CornerQuitMirrorsSfxAcrossScreenSizesAndStillOpensConfirmation()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            var game = Object.FindAnyObjectByType<ForestRunnerGame>(); game.enabled = false;
            var menu = game.GetComponentInChildren<ForestMainMenu>(true);
            yield return new WaitForSecondsRealtime(0.35f);
            var quit = (RectTransform)menu.transform.Find("Quit");
            var sound = (RectTransform)menu.transform.Find("SFX toggle");
            Assert.That(menu.transform.Find("Main menu buttons/Quit"), Is.Null);
            Assert.That(menu.transform.Find("Main menu buttons").GetComponentsInChildren<Button>().Length, Is.EqualTo(4));
            foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(320, 568),
                new Vector2Int(360, 800), new Vector2Int(1024, 768), new Vector2Int(1920, 820), new Vector2Int(1280, 720) })
            {
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(size.x, size.y, new Rect(0, 24, size.x, size.y - 48)));
                Canvas.ForceUpdateCanvases();
                Assert.That(quit.anchoredPosition, Is.EqualTo(new Vector2(-sound.anchoredPosition.x, sound.anchoredPosition.y)));
                Assert.That(quit.sizeDelta, Is.EqualTo(sound.sizeDelta));
                Assert.That(quit.GetComponent<Image>().sprite, Is.EqualTo(sound.GetComponent<Image>().sprite));
                Assert.That(quit.GetComponent<Image>().type, Is.EqualTo(sound.GetComponent<Image>().type));
                var caption = quit.Find("Label").GetComponent<Text>();
                var sfxCaption = sound.Find("SFX label").GetComponent<Text>();
                Assert.That(caption.text, Is.EqualTo("QUIT"));
                Assert.That(caption.fontSize, Is.EqualTo(sfxCaption.fontSize));
                Assert.That(caption.rectTransform.anchoredPosition, Is.EqualTo(sfxCaption.rectTransform.anchoredPosition));
                Assert.That(caption.rectTransform.sizeDelta, Is.EqualTo(sfxCaption.rectTransform.sizeDelta));
                Assert.That(((RectTransform)quit.Find("Icon")).anchoredPosition, Is.EqualTo(((RectTransform)sound.Find("Speaker icon")).anchoredPosition));
                Assert.That(((RectTransform)quit.Find("Icon")).sizeDelta, Is.EqualTo(((RectTransform)sound.Find("Speaker icon")).sizeDelta));
            }
            game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea));
            yield return ForestRunTests.Capture("corner-quit-landscape");
            yield return ForestRunTests.Capture("corner-quit-portrait", 720, 1280);
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, quit.position) };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits[0].gameObject, Is.EqualTo(quit.gameObject), "Quit must remain clickable in its new corner.");
            ExecuteEvents.Execute(quit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(menu.DialogOpen, Is.True);
            Assert.That(menu.transform.Find("Menu dialog/Moss framed dialog/Quit confirmation page").gameObject.activeInHierarchy, Is.True);
            yield return ForestRunTests.Capture("corner-quit-confirmation");
            menu.CloseDialog();
            Assert.That(menu.DialogOpen, Is.False);
            game.StartRun(); Assert.That(quit.gameObject.activeInHierarchy, Is.False);
            game.ReturnToMenu(); Assert.That(quit.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator MainButtonsShareShapeSpacingAndTypography()
        {
            yield return SceneManager.LoadSceneAsync("ForestRun");
            Canvas.ForceUpdateCanvases();
            ForestMainMenu menu = Object.FindAnyObjectByType<ForestMainMenu>();
            Transform cornerBranches = menu.transform.Find("Forest framing - canopy, log, rocks and flowers");
            Assert.That(cornerBranches == null || !cornerBranches.gameObject.activeSelf, Is.True,
                "Decorative branches must not cover the main menu's upper corners.");
            // Measure design-space pixel gaps at the native canvas scale. Dynamic
            // font hinting at a fractional Game view scale changes glyph bounds.
            Canvas canvas = menu.GetComponentInParent<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.scaleFactor = 1;
            foreach (Text label in menu.GetComponentsInChildren<Text>()) label.SetVerticesDirty();
            Canvas.ForceUpdateCanvases();
            string[] names = { "Play", "Shop", "Missions", "Options" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform button = menu.transform.Find("Main menu buttons/" + names[i]);
                var rect = (RectTransform)button;
                Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(360, 68)), names[i]);
                Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(0, -12 - i * 74)));
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
                if (names[i] == "Missions")
                {
                    Assert.That(text.rectTransform.anchoredPosition.x, Is.Zero);
                    continue;
                }
                Image icon = button.Find("Icon").GetComponent<Image>();
                var texture = new Texture2D(2, 2);
                try
                {
                    Assert.That(texture.LoadImage(System.IO.File.ReadAllBytes($"Assets/UI/ForestMenu/Icon{(names[i] == "Shop" ? "Dino" : names[i] == "Options" ? "Gear" : names[i])}.png")), Is.True);
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
