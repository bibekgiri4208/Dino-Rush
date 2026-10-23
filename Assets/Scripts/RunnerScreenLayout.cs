using UnityEngine;

namespace DinoRush
{
    /// <summary>One screen-space contract for the camera, safe UI, and pointer coordinates.</summary>
    public readonly struct RunnerScreenLayout
    {
        public readonly Vector2 ScreenSize;
        public readonly Rect SafeArea;
        public readonly Rect WorldPixels;
        public readonly Rect UiPixels;
        public readonly Vector2 UiSize;
        public readonly float UiScale;
        public readonly bool Portrait;
        public float OrthographicSize => 10f * WorldPixels.height / WorldPixels.width;

        public Rect CameraRect => new Rect(WorldPixels.x / ScreenSize.x, WorldPixels.y / ScreenSize.y,
            WorldPixels.width / ScreenSize.x, WorldPixels.height / ScreenSize.y);

        private RunnerScreenLayout(Vector2 screen, Rect safe, Rect world, bool portrait)
        {
            ScreenSize = screen;
            SafeArea = safe;
            WorldPixels = world;
            Portrait = portrait;
            UiPixels = portrait ? safe : world;
            UiScale = UiPixels.width / (portrait ? 720f : 1280f);
            UiSize = UiPixels.size / UiScale;
        }

        public static RunnerScreenLayout Calculate(int width, int height, Rect safeArea)
        {
            var screen = new Vector2(Mathf.Max(1, width), Mathf.Max(1, height));
            float left = Mathf.Clamp(safeArea.xMin, 0f, screen.x);
            float bottom = Mathf.Clamp(safeArea.yMin, 0f, screen.y);
            float right = Mathf.Clamp(safeArea.xMax, left, screen.x);
            float top = Mathf.Clamp(safeArea.yMax, bottom, screen.y);
            var safe = Rect.MinMaxRect(left, bottom, right, top);
            if (safe.width < 1f || safe.height < 1f) safe = new Rect(Vector2.zero, screen);

            // Taller landscape displays show extra sky/soil, rather than forcing 16:9 bars.
            bool portrait = safe.height > safe.width;
            if (!portrait && safe.width / safe.height <= 16f / 9f)
                return new RunnerScreenLayout(screen, safe, safe, false);

            // Keep the complete course on portrait and exceptionally wide displays.
            float scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
            Vector2 size = new Vector2(1280, 720) * scale;
            var world = new Rect(safe.center - size * 0.5f, size);
            return new RunnerScreenLayout(screen, safe, world, portrait);
        }
    }
}
