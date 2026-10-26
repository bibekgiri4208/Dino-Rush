using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DinoRush.Tests
{
    internal static class RunnerDisplayCapture
    {
        internal static IEnumerator Capture(string name, int width, int height, Rect? safeArea = null, bool fullBleedLandscape = false)
        {
            string directory = System.Environment.GetEnvironmentVariable("DINO_RUSH_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory)) yield break;
            yield return null;
            ForestRunnerGame game = Object.FindAnyObjectByType<ForestRunnerGame>();
            var hud = (RunnerHud)typeof(ForestRunnerGame).GetField("hud", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
            Camera camera = Camera.main;
            Canvas canvas = game.GetComponentInChildren<Canvas>();
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            RenderMode previousMode = canvas.renderMode;
            Camera previousUiCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            Rect previousRect = camera.rect;
            float previousSize = camera.orthographicSize;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var worldTarget = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var uiCamera = new GameObject("Resolution capture UI camera", typeof(Camera)).GetComponent<Camera>();
            uiCamera.enabled = false;
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = 5.625f;
            uiCamera.transform.position = new Vector3(0, 0, -10);
            uiCamera.cullingMask = 1 << 5;
            uiCamera.clearFlags = CameraClearFlags.SolidColor;
            uiCamera.backgroundColor = new Color32(12, 35, 39, 255);
            uiCamera.allowMSAA = false;
            uiCamera.allowHDR = false;
            uiCamera.targetTexture = target;
            var background = new GameObject("Captured world", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            background.transform.SetParent(canvas.transform, false);
            background.transform.SetSiblingIndex(0);
            background.gameObject.layer = 5;
            background.raycastTarget = false;
            background.texture = worldTarget;
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
            bool scalerEnabled = scaler.enabled;
            try
            {
                RunnerScreenLayout layout = RunnerScreenLayout.Calculate(width, height, safeArea ?? new Rect(0, 0, width, height), fullBleedLandscape);
                camera.targetTexture = worldTarget;
                game.ApplyScreenLayout(layout);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
                canvas.planeDistance = 1f;
                scaler.enabled = false;
                hud.ApplyScreenLayout(layout);
                foreach (Text text in canvas.GetComponentsInChildren<Text>())
                    text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = worldTarget;
                GL.Clear(true, true, Color.black);
                // Full-screen UI is rendered separately so portrait controls can sit outside the world viewport.
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = worldTarget });
                RenderPipeline.SubmitRenderRequest(uiCamera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Color32 first = image.GetPixel(0, 0);
                bool varied = false;
                for (int y = 1; y < 8; y++)
                for (int x = 1; x < 8; x++)
                    if (!((Color32)image.GetPixel(width * x / 8, height * y / 8)).Equals(first)) varied = true;
                Assert.That(varied, Is.True, "A screenshot must contain the rendered game, not just a cleared target.");
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousUiCamera;
                canvas.planeDistance = previousDistance;
                scaler.enabled = scalerEnabled;
                game.ApplyScreenLayout(RunnerScreenLayout.Calculate(Screen.width, Screen.height, Screen.safeArea, Application.isMobilePlatform));
                camera.targetTexture = previousTarget;
                camera.rect = previousRect;
                camera.orthographicSize = previousSize;
                uiCamera.targetTexture = null;
                background.texture = null;
                Object.Destroy(background.gameObject);
                Object.Destroy(uiCamera.gameObject);
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                RenderTexture.ReleaseTemporary(worldTarget);
                Object.Destroy(image);
                Canvas.ForceUpdateCanvases();
            }
        }
    }
}
