using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Pooled UI copies carry collected eggs from the playfield into the live HUD icon.</summary>
    public sealed class RunnerEggFlights
    {
        public const int PoolSize = 24;
        public const float Duration = 0.62f;
        private sealed class Flight
        {
            public Image egg;
            public Image[] trail;
            public Vector3 origin;
            public Camera camera;
            public float time;
            public bool active;
        }

        private readonly Flight[] flights = new Flight[PoolSize];
        private readonly RectTransform layer, target;
        private readonly Canvas canvas;
        private readonly Action arrive;
        private int cursor;
        public int ActiveCount { get; private set; }

        public RunnerEggFlights(RectTransform parent, RectTransform target, Sprite sprite, Action arrive)
        {
            this.target = target;
            this.arrive = arrive;
            canvas = parent.GetComponentInParent<Canvas>();
            layer = new GameObject("Egg pickup flights", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = layer.offsetMax = Vector2.zero;
            CanvasGroup group = layer.GetComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            for (int i = 0; i < flights.Length; i++)
            {
                var flight = new Flight { trail = new Image[2] };
                for (int j = 0; j < flight.trail.Length; j++)
                    flight.trail[j] = Image("Egg flight trail " + i + "-" + j, sprite);
                flight.egg = Image("Flying egg " + i, sprite);
                flights[i] = flight;
            }
            SetVisible(false);
        }

        public void Play(Vector3 worldPosition, Camera camera)
        {
            if (camera == null || !layer.gameObject.activeInHierarchy) return;
            Flight flight = null;
            for (int i = 0; i < flights.Length; i++)
            {
                Flight candidate = flights[(cursor + i) % flights.Length];
                if (!candidate.active) { flight = candidate; cursor = (cursor + i + 1) % flights.Length; break; }
            }
            // Awards are already recorded. Under an extreme pickup burst, reuse the
            // oldest visual instead of allocating objects or losing any collected eggs.
            if (flight == null)
            {
                flight = flights[cursor];
                cursor = (cursor + 1) % flights.Length;
                Finish(flight, true);
            }
            flight.origin = worldPosition;
            flight.camera = camera;
            flight.time = 0f;
            flight.active = true;
            ActiveCount++;
            Render(flight);
        }

        public void Tick(float dt)
        {
            if (!layer.gameObject.activeInHierarchy) return;
            foreach (Flight flight in flights)
            {
                if (!flight.active) continue;
                flight.time += Mathf.Max(0f, dt);
                if (flight.time >= Duration) Finish(flight, true);
                else Render(flight);
            }
        }

        public void RefreshLayout()
        {
            foreach (Flight flight in flights) if (flight.active) Render(flight);
        }

        public void SetVisible(bool visible)
        {
            layer.gameObject.SetActive(visible);
            if (visible) RefreshLayout();
        }

        public void Clear()
        {
            foreach (Flight flight in flights) if (flight.active) Finish(flight, false);
            cursor = 0;
        }

        private void Render(Flight flight)
        {
            if (flight.camera == null) { Finish(flight, false); return; }
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer,
                flight.camera.WorldToScreenPoint(flight.origin), uiCamera, out Vector2 start)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer,
                flight.camera.WorldToScreenPoint(flight.origin + Vector3.up * 0.625f), uiCamera, out Vector2 top);
            Vector2 end = layer.InverseTransformPoint(target.TransformPoint(target.rect.center));
            float startScale = Mathf.Clamp(Vector2.Distance(start, top) / 40f, 0.45f, 1.5f);
            float progress = Mathf.Clamp01(flight.time / Duration);
            for (int i = 0; i < flight.trail.Length; i++)
            {
                float trailProgress = progress - (flight.trail.Length - i) * 0.065f;
                RenderImage(flight.trail[i], start, end, startScale, trailProgress, i == 0 ? 0.12f : 0.25f);
            }
            RenderImage(flight.egg, start, end, startScale, progress, 1f);
        }

        private static void RenderImage(Image image, Vector2 start, Vector2 end, float startScale, float progress, float opacity)
        {
            image.gameObject.SetActive(progress >= 0f);
            if (progress < 0f) return;
            float t = Mathf.SmoothStep(0f, 1f, progress), inverse = 1f - t;
            // Lift out of the playfield first, then bend into the HUD. Keep both
            // controls between the endpoints so eggs never loop forward past the dino.
            Vector2 controlA = new Vector2(start.x, Mathf.Lerp(start.y, end.y, 0.55f));
            Vector2 controlB = new Vector2(Mathf.Lerp(start.x, end.x, 0.8f), end.y);
            image.rectTransform.anchoredPosition = inverse * inverse * inverse * start +
                3f * inverse * inverse * t * controlA + 3f * inverse * t * t * controlB + t * t * t * end;
            image.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 0.4f, t);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, -18f * Mathf.Sin(t * Mathf.PI));
            image.color = new Color(1f, 1f, 1f, opacity * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, progress))));
        }

        private void Finish(Flight flight, bool pulse)
        {
            flight.active = false;
            flight.egg.gameObject.SetActive(false);
            foreach (Image trail in flight.trail) trail.gameObject.SetActive(false);
            ActiveCount--;
            if (pulse) arrive?.Invoke();
        }

        private Image Image(string name, Sprite sprite)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(layer, false);
            image.gameObject.layer = 5;
            image.rectTransform.sizeDelta = new Vector2(32, 40);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
