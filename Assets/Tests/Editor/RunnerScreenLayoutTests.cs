using NUnit.Framework;
using UnityEngine;

namespace DinoRush.Tests
{
    public sealed class RunnerScreenLayoutTests
    {
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
        public void NotchesAndHomeIndicatorsStayOutsideBothLayouts(int width, int height, int x, int y, int w, int h)
        {
            var safe = new Rect(x, y, w, h);
            RunnerScreenLayout layout = RunnerScreenLayout.Calculate(width, height, safe);
            Check(layout);
            Assert.That(layout.SafeArea, Is.EqualTo(safe));
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
