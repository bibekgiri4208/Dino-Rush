using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Pooled pop/count-up feedback, gold comet trails, and responsive flights into the live HUD.</summary>
    public sealed class RunnerEggFlights
    {
        public const int PoolSize = 24;
        public const float Duration = 0.62f;
        private sealed class Flight
        {
            public Image egg;
            public RunnerEggPickupGraphic[] trail;
            public RunnerEggPickupGraphic glow;
            public Text popup;
            public Vector3 origin;
            public Camera camera;
            public float time;
            public bool active;
        }

        private readonly Flight[] flights = new Flight[PoolSize];
        private readonly RectTransform layer, target;
        private readonly Canvas canvas;
        private readonly Action arrive;
        private readonly RunnerEggPickupGraphic impact;
        private float impactTime;
        public const float ImpactDuration = 0.34f;
        public const float PopupDuration = 0.42f;
        private int cursor;
        public int ActiveCount { get; private set; }

        public RunnerEggFlights(RectTransform parent, RectTransform target, Sprite sprite, Action arrive, Font font = null)
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
                var flight = new Flight { trail = new RunnerEggPickupGraphic[5] };
                for (int j = 0; j < flight.trail.Length; j++)
                    flight.trail[j] = Effect("Egg flight glint " + i + "-" + j, 12);
                flight.glow = Effect("Egg flight glow " + i, 64);
                flight.egg = Image("Flying egg " + i, sprite);
                if (font != null)
                {
                    flight.popup = new GameObject("Egg pickup +1 " + i, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    flight.popup.transform.SetParent(layer, false);
                    flight.popup.gameObject.layer = 5;
                    flight.popup.rectTransform.sizeDelta = new Vector2(80, 38);
                    flight.popup.font = font;
                    flight.popup.fontSize = 22;
                    flight.popup.text = "+1";
                    flight.popup.alignment = TextAnchor.MiddleCenter;
                    flight.popup.raycastTarget = false;
                    Shadow shadow = flight.popup.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color32(27, 37, 29, 220);
                    shadow.effectDistance = new Vector2(2, -2);
                    flight.popup.gameObject.SetActive(false);
                }
                flights[i] = flight;
            }
            impact = Effect("Egg counter arrival", 72);
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
            impactTime = Mathf.Max(0, impactTime - Mathf.Max(0, dt));
            foreach (Flight flight in flights)
            {
                if (!flight.active) continue;
                flight.time += Mathf.Max(0f, dt);
                if (flight.time >= Duration) Finish(flight, true);
                else Render(flight);
            }
            RenderImpact();
        }

        public void RefreshLayout()
        {
            foreach (Flight flight in flights) if (flight.active) Render(flight);
            RenderImpact();
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
            impactTime = 0;
            impact.gameObject.SetActive(false);
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
            float t = Travel(progress);
            Vector2 position = Path(start, end, t);
            for (int i = 0; i < flight.trail.Length; i++)
            {
                float trailProgress = progress - (flight.trail.Length - i) * 0.045f;
                RunnerEggPickupGraphic glint = flight.trail[i];
                glint.gameObject.SetActive(trailProgress >= 0 && progress < 0.98f);
                if (trailProgress < 0) continue;
                glint.rectTransform.anchoredPosition = Path(start, end, Travel(trailProgress));
                float size = Mathf.Lerp(0.25f, 0.8f, (i + 1f) / flight.trail.Length) * Mathf.Lerp(startScale, 0.6f, t);
                glint.rectTransform.localScale = Vector3.one * size;
                glint.rectTransform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 0 : 45);
                glint.SetEffect(RunnerEggPickupGraphic.Shape.Glint, 0, (0.2f + i * 0.11f) * (1 - t * 0.7f));
            }
            float pop = Mathf.Sin(Mathf.Clamp01(progress / 0.22f) * Mathf.PI) * 0.22f;
            float scale = Mathf.Lerp(startScale, 0.4f, t) + pop * startScale;
            flight.egg.gameObject.SetActive(true);
            flight.egg.rectTransform.anchoredPosition = position;
            flight.egg.rectTransform.localScale = Vector3.one * scale;
            flight.egg.rectTransform.localRotation = Quaternion.Euler(0, 0, -22f * Mathf.Sin(t * Mathf.PI));
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.9f, 1, progress));
            flight.egg.color = Color.Lerp(new Color(1, 0.9f, 0.62f, fade), new Color(1, 1, 1, fade), t);
            flight.glow.gameObject.SetActive(true);
            flight.glow.rectTransform.anchoredPosition = position;
            flight.glow.rectTransform.localScale = Vector3.one * scale;
            flight.glow.SetEffect(RunnerEggPickupGraphic.Shape.Glow, 0, fade * (0.45f - t * 0.22f));
            if (flight.popup != null)
            {
                float age = Mathf.Clamp01(flight.time / PopupDuration);
                flight.popup.gameObject.SetActive(age < 1);
                float popupScale = Mathf.Clamp(startScale, 0.85f, 1.15f) *
                    (1 + Mathf.Sin(Mathf.Clamp01(age * 3) * Mathf.PI) * 0.18f);
                Vector2 point = start + new Vector2(18, 24 + (1 - Mathf.Pow(1 - age, 2)) * 38);
                point.x = Mathf.Clamp(point.x, layer.rect.xMin + 42 * popupScale, layer.rect.xMax - 42 * popupScale);
                point.y = Mathf.Clamp(point.y, layer.rect.yMin + 21 * popupScale, layer.rect.yMax - 21 * popupScale);
                flight.popup.rectTransform.anchoredPosition = point;
                flight.popup.rectTransform.localScale = Vector3.one * popupScale;
                flight.popup.color = new Color(1, 0.9f, 0.57f, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.5f, 1, age)));
            }
        }

        private static float Travel(float progress)
        {
            // A tiny catch/pop, followed by an accelerating sweep and a soft landing.
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.08f, 1, progress));
        }

        private static Vector2 Path(Vector2 start, Vector2 end, float t)
        {
            float inverse = 1 - t;
            // Lift out of the playfield first, then bend into the HUD. Keep both
            // controls between the endpoints so eggs never loop forward past the dino.
            Vector2 controlA = new Vector2(start.x, Mathf.Lerp(start.y, end.y, 0.55f));
            Vector2 controlB = new Vector2(Mathf.Lerp(start.x, end.x, 0.8f), end.y);
            return inverse * inverse * inverse * start +
                3f * inverse * inverse * t * controlA + 3f * inverse * t * t * controlB + t * t * t * end;
        }

        private void RenderImpact()
        {
            impact.gameObject.SetActive(impactTime > 0);
            if (impactTime <= 0) return;
            impact.rectTransform.anchoredPosition = layer.InverseTransformPoint(target.TransformPoint(target.rect.center));
            float age = 1 - impactTime / ImpactDuration;
            impact.SetEffect(RunnerEggPickupGraphic.Shape.Arrival, age, (1 - age) * 0.8f);
        }

        private void Finish(Flight flight, bool pulse)
        {
            flight.active = false;
            flight.egg.gameObject.SetActive(false);
            flight.glow.gameObject.SetActive(false);
            if (flight.popup != null) flight.popup.gameObject.SetActive(false);
            foreach (RunnerEggPickupGraphic trail in flight.trail) trail.gameObject.SetActive(false);
            ActiveCount--;
            if (pulse) { impactTime = ImpactDuration; arrive?.Invoke(); }
        }

        private RunnerEggPickupGraphic Effect(string name, float size)
        {
            var effect = new GameObject(name, typeof(RectTransform), typeof(RunnerEggPickupGraphic)).GetComponent<RunnerEggPickupGraphic>();
            effect.transform.SetParent(layer, false);
            effect.gameObject.layer = 5;
            effect.rectTransform.sizeDelta = Vector2.one * size;
            effect.raycastTarget = false;
            effect.gameObject.SetActive(false);
            return effect;
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
