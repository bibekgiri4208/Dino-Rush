using UnityEngine;

namespace DinoRush
{
    /// <summary>Jungle-only rain streaks and ground splashes. Pooled and entirely cosmetic.</summary>
    [DisallowMultipleComponent]
    public sealed class JungleRain : MonoBehaviour
    {
        [SerializeField] private ForestRunnerGame game;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Material spriteMaterial;
        [Header("Jungle rain")]
        [SerializeField, Range(32, 160)] private int dropCount = 96;
        [SerializeField, Range(4f, 20f)] private float fallSpeed = 12f;
        [SerializeField, Range(0.2f, 4f)] private float windSpeed = 1.8f;
        [SerializeField, Range(0f, 2f)] private float gustStrength = 0.7f;
        [SerializeField, Range(0f, 1.5f)] private float runScrollInfluence = 1f;
        [SerializeField] private int seed = 7351;

        private sealed class Drop
        {
            public SpriteRenderer renderer;
            public Vector2 position;
            public float depth, length, width, phase;
            public Color color;
            public bool near;
        }

        private sealed class Splash
        {
            public SpriteRenderer renderer;
            public Vector2 position;
            public float age, size;
            public bool active;
        }

        private const float SplashDuration = 0.2f;
        private Drop[] drops;
        private Splash[] splashes;
        private Transform pool;
        private Texture2D rainTexture, splashTexture;
        private Sprite rainSprite, splashSprite;
        private System.Random random;
        private float rainTime;
        private int nextSplash;

        private void Start() => Simulate(0f);
        private void LateUpdate() => Simulate(Time.deltaTime);

        private bool IsJungle => game.State != ForestRunnerGame.RunState.Ready && (game.Story == null || !game.Story.IsModal) &&
            game.MapCatalog != null && game.MapCatalog.Count > 0 && game.MapCatalog[game.ActiveMapIndex].id == "jungle";

        private Rect FlightArea()
        {
            Vector3 center = worldCamera.transform.position;
            float halfWidth = worldCamera.orthographicSize * worldCamera.aspect;
            return Rect.MinMaxRect(center.x - halfWidth - 0.5f, RunnerRules.GroundY + 0.02f,
                center.x + halfWidth + 0.5f, center.y + worldCamera.orthographicSize + 0.5f);
        }

        // Explicit simulation supports deterministic tests without touching gameplay's random stream.
        public void Simulate(float dt)
        {
            if (game == null || worldCamera == null) return;
            if (!IsJungle)
            {
                if (pool != null) pool.gameObject.SetActive(false);
                return;
            }
            Rect area = FlightArea();
            if (drops == null) CreatePool(area);
            pool.gameObject.SetActive(true);
            if (game.State == ForestRunnerGame.RunState.Paused || game.State == ForestRunnerGame.RunState.GameOver) return;
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            rainTime += dt;
            float courseSpeed = game.State == ForestRunnerGame.RunState.Running ? game.CurrentSpeed * runScrollInfluence : 0f;
            UpdateSplashes(dt, courseSpeed, area);
            float wind = windSpeed + (0.5f + 0.5f * Mathf.Sin(rainTime * 0.8f)) * gustStrength;
            foreach (Drop drop in drops)
            {
                float horizontal = (courseSpeed + wind) * drop.depth;
                float vertical = fallSpeed * drop.depth * (1f + 0.08f * Mathf.Sin(drop.phase));
                Vector2 previous = drop.position;
                drop.position += new Vector2(-horizontal, -vertical) * dt;
                if (drop.near && previous.y >= area.yMin && drop.position.y < area.yMin)
                {
                    float fraction = (previous.y - area.yMin) / (previous.y - drop.position.y);
                    float impactX = Mathf.Lerp(previous.x, drop.position.x, fraction);
                    if (impactX > area.xMin && impactX < area.xMax) EmitSplash(impactX, area.yMin);
                }
                if (drop.position.x < area.xMin || drop.position.x > area.xMax ||
                    drop.position.y < area.yMin || drop.position.y > area.yMax)
                {
                    // Feed both incoming edges according to flow, keeping rain evenly spread at any run speed.
                    float topChance = area.width * vertical / (area.width * vertical + area.height * horizontal);
                    drop.position = Range(0f, 1f) < topChance
                        ? new Vector2(Range(area.xMin, area.xMax), area.yMax)
                        : new Vector2(area.xMax, Range(area.yMin, area.yMax));
                }
                RenderDrop(drop, area, horizontal, vertical);
            }
        }

        private static void RenderDrop(Drop drop, Rect area, float horizontal, float vertical)
        {
            Transform item = drop.renderer.transform;
            item.position = new Vector3(drop.position.x, drop.position.y, 0f);
            // The streak points along its actual velocity, including the backward-moving course.
            item.rotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(horizontal, vertical) * Mathf.Rad2Deg);
            item.localScale = new Vector3(drop.width * 8f, drop.length, 1f);
            float edge = Mathf.Min(Mathf.Min(drop.position.x - area.xMin, area.xMax - drop.position.x), area.yMax - drop.position.y);
            Color color = drop.color;
            color.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.35f));
            drop.renderer.color = color;
        }

        private void CreatePool(Rect area)
        {
            random = new System.Random(seed);
            rainTexture = new Texture2D(1, 8, TextureFormat.RGBA32, false)
            {
                name = "Jungle rain streak", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < 8; y++) rainTexture.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.15f, y / 7f)));
            rainTexture.Apply();
            rainSprite = Sprite.Create(rainTexture, new Rect(0, 0, 1, 8), new Vector2(0.5f, 0f), 8f, 0, SpriteMeshType.FullRect);
            rainSprite.name = "Jungle rain streak";
            splashTexture = new Texture2D(7, 3, TextureFormat.RGBA32, false)
            {
                name = "Jungle rain splash", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 7; x++)
            {
                bool filled = y == 0 ? x >= 2 && x <= 4 : y == 1 ? x == 1 || x == 5 : x == 0 || x == 6;
                splashTexture.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
            splashTexture.Apply();
            splashSprite = Sprite.Create(splashTexture, new Rect(0, 0, 7, 3), new Vector2(0.5f, 0f), 7f, 0, SpriteMeshType.FullRect);
            splashSprite.name = "Jungle rain splash";
            pool = new GameObject("Jungle rain").transform;
            Transform effects = game.transform.Find("Forest effects");
            pool.SetParent(effects != null ? effects : game.transform, false);
            drops = new Drop[Mathf.Clamp(dropCount, 32, 160)];
            for (int i = 0; i < drops.Length; i++)
            {
                bool near = i % 3 == 0;
                drops[i] = new Drop
                {
                    renderer = CreateRenderer("Rain streak " + (i + 1), rainSprite, near ? 10 : -8), near = near,
                    position = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax)),
                    depth = near ? Range(0.95f, 1.2f) : Range(0.55f, 0.85f),
                    length = near ? Range(0.3f, 0.5f) : Range(0.16f, 0.28f),
                    width = near ? Range(0.016f, 0.025f) : Range(0.01f, 0.016f), phase = Range(0f, Mathf.PI * 2f),
                    color = near ? new Color(0.76f, 0.91f, 0.94f, Range(0.38f, 0.58f))
                        : new Color(0.59f, 0.78f, 0.83f, Range(0.18f, 0.32f))
                };
                // Initialize even if the first simulation occurs during pause/results.
                RenderDrop(drops[i], area, windSpeed * drops[i].depth, fallSpeed * drops[i].depth);
            }
            splashes = new Splash[20];
            for (int i = 0; i < splashes.Length; i++)
            {
                var splash = new Splash { renderer = CreateRenderer("Rain splash " + (i + 1), splashSprite, 10) };
                splash.renderer.gameObject.SetActive(false);
                splashes[i] = splash;
            }
        }

        private SpriteRenderer CreateRenderer(string itemName, Sprite art, int order)
        {
            var renderer = new GameObject(itemName, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(pool, false);
            renderer.sprite = art;
            if (spriteMaterial != null) renderer.sharedMaterial = spriteMaterial;
            // All rain and splashes stay behind obstacles (15) and the dino (20).
            renderer.sortingOrder = order;
            return renderer;
        }

        private void EmitSplash(float x, float y)
        {
            Splash splash = splashes[nextSplash];
            nextSplash = (nextSplash + 1) % splashes.Length;
            splash.position = new Vector2(x, y);
            splash.age = 0f;
            splash.size = Range(0.18f, 0.3f);
            splash.active = true;
            splash.renderer.gameObject.SetActive(true);
            RenderSplash(splash);
        }

        private void UpdateSplashes(float dt, float courseSpeed, Rect area)
        {
            foreach (Splash splash in splashes)
            {
                if (!splash.active) continue;
                splash.age += dt;
                splash.position.x -= courseSpeed * dt;
                if (splash.age >= SplashDuration || splash.position.x < area.xMin)
                {
                    splash.active = false;
                    splash.renderer.gameObject.SetActive(false);
                    continue;
                }
                RenderSplash(splash);
            }
        }

        private static void RenderSplash(Splash splash)
        {
            float t = splash.age / SplashDuration;
            splash.renderer.transform.position = new Vector3(splash.position.x, splash.position.y, 0f);
            splash.renderer.transform.localScale = new Vector3(splash.size * (0.6f + t), splash.size * (1f - t * 0.7f), 1f);
            splash.renderer.color = new Color(0.73f, 0.9f, 0.92f, 0.45f * (1f - t));
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private void OnDisable()
        {
            if (pool != null) pool.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (pool != null) Destroy(pool.gameObject);
            if (rainSprite != null) Destroy(rainSprite);
            if (splashSprite != null) Destroy(splashSprite);
            if (rainTexture != null) Destroy(rainTexture);
            if (splashTexture != null) Destroy(splashTexture);
        }
    }
}
