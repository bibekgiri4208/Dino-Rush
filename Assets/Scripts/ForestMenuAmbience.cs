using UnityEngine;

namespace DinoRush
{
    /// <summary>Quiet title-only light shafts and distant flocks. No gameplay or UI interaction.</summary>
    [DisallowMultipleComponent]
    public sealed class ForestMenuAmbience : MonoBehaviour
    {
        [SerializeField] private ForestRunnerGame game;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Material spriteMaterial;
        [Header("Canopy sunlight")]
        [SerializeField, Range(0f, 0.25f)] private float sunbeamOpacity = 0.13f;
        [SerializeField, Range(0f, 0.8f)] private float sunbeamSway = 0.25f;
        [Header("Distant birds")]
        [SerializeField, Min(0f)] private float firstFlockDelay = 3f;
        [SerializeField] private Vector2 quietSeconds = new Vector2(14f, 24f);
        [SerializeField] private int seed = 19427;

        private ForestMainMenu menu;
        private Transform pool;
        private SpriteRenderer[] beams, birds;
        private Texture2D beamTexture, birdTexture;
        private Sprite beamSprite;
        private Sprite[] birdFrames;
        private System.Random random;
        private float sunlightTime, flockTime, wait, flightSeconds, flightHeight, direction;
        private int flockSize;
        private bool flying;

        public void Initialize(ForestRunnerGame runner, Camera camera, Material material)
        {
            game = runner;
            worldCamera = camera;
            spriteMaterial = material;
            menu = game.GetComponentInChildren<ForestMainMenu>(true);
            if (isActiveAndEnabled) Simulate(0f);
        }

        private void Start()
        {
            if (game != null && menu == null) menu = game.GetComponentInChildren<ForestMainMenu>(true);
            Simulate(0f);
        }

        private void LateUpdate() => Simulate(Time.deltaTime);

        public void Simulate(float dt)
        {
            bool visible = game != null && worldCamera != null && game.State == ForestRunnerGame.RunState.Ready &&
                menu != null && menu.gameObject.activeInHierarchy && !menu.DialogOpen;
            if (!visible)
            {
                if (pool != null) pool.gameObject.SetActive(false);
                return;
            }
            if (pool == null) CreatePool();
            pool.gameObject.SetActive(true);
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            sunlightTime += dt;
            Vector3 center = worldCamera.transform.position;
            float halfWidth = worldCamera.orthographicSize * worldCamera.aspect;
            Rect area = new Rect(center.x - halfWidth, center.y - worldCamera.orthographicSize,
                halfWidth * 2, worldCamera.orthographicSize * 2);
            RenderSunbeams(area);
            if (!flying)
            {
                wait -= dt;
                if (wait <= 0f) BeginFlock();
            }
            else
            {
                flockTime += dt;
                if (flockTime >= flightSeconds)
                {
                    flying = false;
                    wait = Range(Mathf.Max(0, quietSeconds.x), Mathf.Max(quietSeconds.x, quietSeconds.y));
                }
            }
            RenderBirds(area);
        }

        private void CreatePool()
        {
            random = new System.Random(seed);
            wait = firstFlockDelay;
            pool = new GameObject("Title sunbeams and distant birds").transform;
            Transform effects = game.transform.Find("Forest effects");
            pool.SetParent(effects != null ? effects : game.transform, false);
            CreateSunbeamArt();
            CreateBirdArt();
            beams = new SpriteRenderer[3];
            for (int i = 0; i < beams.Length; i++)
                beams[i] = Renderer("Soft canopy sunbeam " + (i + 1), beamSprite, -5);
            birds = new SpriteRenderer[4];
            for (int i = 0; i < birds.Length; i++)
                birds[i] = Renderer("Distant bird " + (i + 1), birdFrames[0], -85);
        }

        private SpriteRenderer Renderer(string name, Sprite sprite, int order)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(pool, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (spriteMaterial != null) renderer.sharedMaterial = spriteMaterial;
            return renderer;
        }

        private void RenderSunbeams(Rect area)
        {
            for (int i = 0; i < beams.Length; i++)
            {
                float phase = sunlightTime * 0.19f + i * 2.1f;
                // Feathered cones originate above the viewport and fade before the trail.
                float x = area.xMin + area.width * (0.3f + i * 0.37f) + Mathf.Sin(phase) * sunbeamSway;
                Transform beam = beams[i].transform;
                beam.position = new Vector3(x, area.yMax + 0.6f, 0);
                beam.rotation = Quaternion.Euler(0, 0, -22f + i * 3f + Mathf.Sin(phase * 0.7f) * 1.5f);
                float length = (area.yMax - RunnerRules.GroundY + 1.2f) / 0.92f;
                beam.localScale = new Vector3(i == 1 ? 1.1f : i == 0 ? 1.8f : 2.5f, length / 4, 1);
                beams[i].color = new Color(1f, 0.93f, 0.7f, sunbeamOpacity * (0.72f + Mathf.Sin(phase) * 0.18f));
            }
        }

        private void BeginFlock()
        {
            flying = true;
            flockTime = 0f;
            flightSeconds = Range(22f, 29f);
            flightHeight = Range(0.79f, 0.87f);
            direction = random.Next(2) == 0 ? -1f : 1f;
            flockSize = random.Next(3, birds.Length + 1);
        }

        private void RenderBirds(Rect area)
        {
            for (int i = 0; i < birds.Length; i++)
            {
                SpriteRenderer bird = birds[i];
                bird.gameObject.SetActive(flying && i < flockSize);
                if (!flying || i >= flockSize) continue;
                float progress = flockTime / flightSeconds;
                float x = (direction > 0 ? -0.12f + progress * 1.35f : 1.12f - progress * 1.35f) - direction * i * 0.027f;
                float y = flightHeight + (i % 2 == 0 ? 1 : -1) * i * 0.009f + Mathf.Sin(flockTime * 0.9f + i) * 0.004f;
                bird.transform.position = new Vector3(area.xMin + x * area.width, area.yMin + y * area.height, 0);
                bird.transform.localScale = Vector3.one * (0.23f - i * 0.018f);
                int wing = (int)(flockTime * 4.5f + i * 0.7f) % 4;
                bird.sprite = birdFrames[wing == 3 ? 1 : wing];
                bird.flipX = direction < 0;
                float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Min(x, 1 - x) / 0.08f));
                bird.color = new Color(0.16f, 0.29f, 0.32f, 0.72f * fade);
            }
        }

        private void CreateSunbeamArt()
        {
            const int width = 64, height = 256;
            beamTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Soft canopy sunlight", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float v = y / (height - 1f);
                float halfWidth = Mathf.Lerp(0.49f, 0.1f, v);
                float side = Mathf.Clamp01(1 - Mathf.Abs((x + 0.5f) / width - 0.5f) / halfWidth);
                float end = Mathf.SmoothStep(0, 1, v / 0.25f) * Mathf.SmoothStep(0, 1, (1 - v) / 0.15f);
                pixels[y * width + x] = new Color(1, 1, 1, side * side * end);
            }
            beamTexture.SetPixels(pixels);
            beamTexture.Apply(false, true);
            beamSprite = Sprite.Create(beamTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 1), 64, 0, SpriteMeshType.FullRect);
            beamSprite.name = "Feathered canopy sunbeam";
        }

        private void CreateBirdArt()
        {
            const int width = 15, height = 9;
            birdTexture = new Texture2D(width * 3, height, TextureFormat.RGBA32, false)
            {
                name = "Distant bird wing cycle", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * 3 * height];
            for (int frame = 0; frame < 3; frame++)
            {
                int tip = frame == 0 ? 7 : frame == 1 ? 5 : 2;
                Line(pixels, frame * width, 1, tip, 4, frame == 1 ? 6 : tip);
                Line(pixels, frame * width, 4, frame == 1 ? 6 : tip, 7, 4);
                Line(pixels, frame * width, 7, 4, 10, frame == 1 ? 6 : tip);
                Line(pixels, frame * width, 10, frame == 1 ? 6 : tip, 13, tip);
                Line(pixels, frame * width, 7, 3, 8, 4);
            }
            birdTexture.SetPixels32(pixels);
            birdTexture.Apply(false, true);
            birdFrames = new Sprite[3];
            for (int i = 0; i < birdFrames.Length; i++)
            {
                birdFrames[i] = Sprite.Create(birdTexture, new Rect(i * width, 0, width, height), Vector2.one * 0.5f, width, 0, SpriteMeshType.FullRect);
                birdFrames[i].name = "Distant bird wing " + i;
            }
        }

        private static void Line(Color32[] pixels, int offset, int x0, int y0, int x1, int y1)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0 : i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                pixels[y * 45 + offset + x] = new Color32(255, 255, 255, 255);
            }
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private void OnDisable()
        {
            if (pool != null) pool.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (pool != null) Destroy(pool.gameObject);
            if (beamSprite != null) Destroy(beamSprite);
            if (birdFrames != null) foreach (Sprite frame in birdFrames) if (frame != null) Destroy(frame);
            if (beamTexture != null) Destroy(beamTexture);
            if (birdTexture != null) Destroy(birdTexture);
        }
    }
}
