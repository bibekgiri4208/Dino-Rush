using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerScreenLayoutTests
    {
        [Test]
        public void MobileLaunchAllowsOnlyLandscapeInEitherDirection()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
        }

        [TestCase(568, 320)]
        [TestCase(640, 360)]
        [TestCase(800, 480)]
        [TestCase(960, 540)]
        [TestCase(2160, 1080)]
        [TestCase(2340, 1080)]
        [TestCase(2400, 1080)]
        [TestCase(2208, 1840)]
        [TestCase(2048, 1536)]
        [TestCase(320, 568)]
        [TestCase(360, 800)]
        [TestCase(1080, 2400)]
        [TestCase(800, 600)]
        [TestCase(1024, 768)]
        [TestCase(1280, 720)]
        [TestCase(1280, 800)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1080)]
        [TestCase(3840, 2160)]
        [TestCase(5120, 1440)]
        public void WholeCourseAndUiFitWithoutStretchingAtAnyAspect(int width, int height)
        {
            Check(RunnerScreenLayout.Calculate(width, height, new Rect(0, 0, width, height)));
        }

        [TestCase(2400, 1080, 100, 40, 2250, 1040)]
        [TestCase(1080, 2400, 0, 100, 1080, 2180)]
        [TestCase(2400, 1080, 0, 40, 2250, 1040)]
        [TestCase(568, 320, 24, 12, 520, 300)]
        [TestCase(2400, 1080, 120, 24, 2280, 1032)]
        [TestCase(2400, 1080, 0, 24, 2280, 1032)]
        [TestCase(2208, 1840, 0, 48, 2208, 1768)]
        public void NotchesAndHomeIndicatorsStayOutsideBothLayouts(int width, int height, int x, int y, int w, int h)
        {
            var safe = new Rect(x, y, w, h);
            RunnerScreenLayout layout = RunnerScreenLayout.Calculate(width, height, safe);
            Check(layout);
            Assert.That(layout.SafeArea, Is.EqualTo(safe));
        }

        [Test]
        public void MobileFullBleedFillsTheDisplayButKeepsUiInTheSafeArea()
        {
            foreach (Vector2Int size in new[] { new Vector2Int(568, 320), new Vector2Int(1024, 768),
                new Vector2Int(2208, 1840), new Vector2Int(2400, 1080), new Vector2Int(5120, 1440) })
            foreach (bool rightNotch in new[] { false, true })
            {
                Rect safe = new Rect(rightNotch ? 0 : 24, 12, size.x - 24, size.y - 24);
                RunnerScreenLayout layout = RunnerScreenLayout.Calculate(size.x, size.y, safe, true);
                Assert.That(layout.FullBleedLandscape, Is.True);
                Assert.That(layout.CameraRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
                Assert.That(layout.UiPixels, Is.EqualTo(safe));
                Assert.That(layout.UiSize.x, Is.GreaterThanOrEqualTo(1280f - 0.01f));
                Assert.That(layout.UiSize.y, Is.GreaterThanOrEqualTo(720f - 0.01f));
                Assert.That(layout.OrthographicSize, Is.GreaterThanOrEqualTo(5.625f));
                Assert.That(layout.OrthographicSize * 2f * size.x / size.y, Is.GreaterThanOrEqualTo(20f - 0.001f));
            }
            Assert.That(RunnerScreenLayout.Calculate(360, 800, new Rect(0, 0, 360, 800), true).FullBleedLandscape,
                Is.False, "Portrait Editor previews retain their supported fallback.");
        }

        [TestCase(0, 60)]
        [TestCase(-1, 60)]
        [TestCase(double.NaN, 60)]
        [TestCase(double.PositiveInfinity, 60)]
        [TestCase(24, 24)]
        [TestCase(30, 30)]
        [TestCase(48, 48)]
        [TestCase(59.94, 60)]
        [TestCase(60, 60)]
        [TestCase(75, 75)]
        [TestCase(90, 90)]
        [TestCase(119.88, 120)]
        [TestCase(120, 120)]
        [TestCase(144, 144)]
        [TestCase(165, 165)]
        [TestCase(240, 240)]
        [TestCase(360, 360)]
        public void MobileFrameRateMatchesTheFullDeviceRefreshWithoutAnArtificialCap(double refresh, int expected)
        {
            Assert.That(ForestRunnerGame.MobileFrameRate(refresh), Is.EqualTo(expected));
        }

        [Test]
        public void MinimizedWindowsAndInvalidSafeAreasHaveFiniteFallbacks()
        {
            foreach (Rect safe in new[] { Rect.zero, new Rect(-100, -50, 5000, 3000) })
            {
                Check(RunnerScreenLayout.Calculate(0, 0, safe));
                Check(RunnerScreenLayout.Calculate(1280, 720, safe));
            }
        }

        private static void Check(RunnerScreenLayout layout)
        {
            Assert.That(2f * layout.OrthographicSize * layout.WorldPixels.width / layout.WorldPixels.height,
                Is.EqualTo(20f).Within(0.0001f), "The entire original course width must remain visible.");
            Assert.That(layout.OrthographicSize, Is.GreaterThanOrEqualTo(5.625f - 0.0001f));
            Assert.That(layout.UiSize.x * layout.UiScale, Is.EqualTo(layout.UiPixels.width).Within(0.001f));
            Assert.That(layout.UiSize.y * layout.UiScale, Is.EqualTo(layout.UiPixels.height).Within(0.001f));
            Assert.That(layout.UiScale, Is.GreaterThan(0f));
            foreach (Rect rect in new[] { layout.WorldPixels, layout.UiPixels })
            {
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(layout.SafeArea.xMin - 0.001f));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(layout.SafeArea.yMin - 0.001f));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(layout.SafeArea.xMax + 0.001f));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(layout.SafeArea.yMax + 0.001f));
            }
        }

        [TestCase(1280, 800)]
        [TestCase(1600, 1000)]
        [TestCase(1920, 1200)]
        [TestCase(2560, 1600)]
        public void SixteenByTenUsesTheWholeScreenWithoutLetterboxing(int width, int height)
        {
            RunnerScreenLayout layout = RunnerScreenLayout.Calculate(width, height, new Rect(0, 0, width, height));
            Assert.That(layout.CameraRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(layout.OrthographicSize, Is.EqualTo(6.25f).Within(0.0001f));
            Assert.That(layout.UiSize, Is.EqualTo(new Vector2(1280, 800)));
        }
    }
}
