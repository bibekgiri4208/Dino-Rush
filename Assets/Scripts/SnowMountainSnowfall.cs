using UnityEngine;

namespace DinoRush
{
    /// <summary>Pooled, cosmetic snowfall for Snow Mountain, independent of gameplay randomness.</summary>
    [DisallowMultipleComponent]
    public sealed class SnowMountainSnowfall : MonoBehaviour
    {
        [SerializeField] private ForestRunnerGame game;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Material spriteMaterial;
        [Header("Mountain snowfall")]
        [SerializeField, Range(24, 120)] private int flakeCount = 72;
        [SerializeField, Range(0.1f, 3f)] private float driftSpeed = 0.65f;
        [SerializeField, Range(0.2f, 3f)] private float fallSpeed = 0.85f;
        [SerializeField, Range(0f, 2f)] private float gustStrength = 0.55f;
        [SerializeField, Range(0f, 1.5f)] private float runScrollInfluence = 1f;
        [SerializeField] private int seed = 4619;

        private sealed class Flake
        {
            public SpriteRenderer renderer;
            public Vector2 position;
            public float depth, size, phase, sway, spin;
            public Color color;
        }

        private Flake[] flakes;
        private Transform pool;
        private Texture2D texture;
        private Sprite sprite;
        private System.Random random;
        private float snowTime;

        private void Start() => Simulate(0f);
        private void LateUpdate() => Simulate(Time.deltaTime);

        private bool IsSnowMountain => game.State != ForestRunnerGame.RunState.Ready && (game.Story == null || !game.Story.IsModal) &&
            game.MapCatalog != null && game.MapCatalog.Count > 0 && game.MapCatalog[game.ActiveMapIndex].id == "snow";

        private Rect FlightArea()
        {
            Vector3 center = worldCamera.transform.position;
            float halfWidth = worldCamera.orthographicSize * worldCamera.aspect;
            return Rect.MinMaxRect(center.x - halfWidth - 0.4f, RunnerRules.GroundY + 0.04f,
                center.x + halfWidth + 0.4f, center.y + worldCamera.orthographicSize + 0.4f);
        }

        // Explicit simulation also supports deterministic tests and captures.
        public void Simulate(float dt)
        {
            if (game == null || worldCamera == null) return;
            if (!IsSnowMountain)
            {
                if (pool != null) pool.gameObject.SetActive(false);
                return;
            }
            Rect area = FlightArea();
            if (flakes == null) CreatePool(area);
            pool.gameObject.SetActive(true);
            if (game.State == ForestRunnerGame.RunState.Paused || game.State == ForestRunnerGame.RunState.GameOver) return;
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            snowTime += dt;
            float gust = (0.5f + 0.5f * Mathf.Sin(snowTime * 0.6f)) * gustStrength;
            float courseSpeed = game.State == ForestRunnerGame.RunState.Running ? game.CurrentSpeed * runScrollInfluence : 0f;
            foreach (Flake flake in flakes)
            {
                float wave = snowTime * flake.sway + flake.phase;
                float horizontal = (courseSpeed + driftSpeed + gust) * flake.depth + Mathf.Sin(wave) * 0.12f;
                float vertical = fallSpeed * flake.depth * (1f + Mathf.Sin(wave * 0.7f) * 0.15f);
                flake.position += new Vector2(-horizontal, -vertical) * dt;
                if (flake.position.x < area.xMin || flake.position.x > area.xMax ||
                    flake.position.y < area.yMin || flake.position.y > area.yMax)
                {
                    // Feed both incoming edges in proportion to their flow, avoiding an empty upper sky.
                    float topChance = area.width * vertical / (area.width * vertical + area.height * horizontal);
                    flake.position = Range(0f, 1f) < topChance
                        ? new Vector2(Range(area.xMin, area.xMax), area.yMax)
                        : new Vector2(area.xMax, Range(area.yMin + 0.3f, area.yMax));
                }
                Render(flake, area);
            }
        }

        private void CreatePool(Rect area)
        {
            random = new System.Random(seed);
            texture = new Texture2D(5, 5, TextureFormat.RGBA32, false)
            {
                name = "Mountain snowflake", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
            {
                int distance = Mathf.Abs(x - 2) + Mathf.Abs(y - 2);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, distance < 2 ? 1f : distance == 2 ? 0.65f : 0f));
            }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 5, 5), Vector2.one * 0.5f, 5f, 0, SpriteMeshType.FullRect);
            sprite.name = "Mountain snowflake";
            pool = new GameObject("Windblown snow").transform;
            Transform effects = game.transform.Find("Forest effects");
            pool.SetParent(effects != null ? effects : game.transform, false);
            flakes = new Flake[Mathf.Clamp(flakeCount, 24, 120)];
            for (int i = 0; i < flakes.Length; i++)
            {
                bool near = i % 3 == 0;
                var renderer = new GameObject("Drifting snowflake " + (i + 1), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(pool, false);
                renderer.sprite = sprite;
                if (spriteMaterial != null) renderer.sharedMaterial = spriteMaterial;
                // Like the forest leaves, snow never covers obstacles (15) or the dino (20).
                renderer.sortingOrder = near ? 10 : -8;
                var flake = new Flake
                {
                    renderer = renderer, position = new Vector2(Range(area.xMin, area.xMax), Range(area.yMin, area.yMax)),
                    depth = near ? Range(0.9f, 1.25f) : Range(0.4f, 0.75f),
                    size = near ? Range(0.09f, 0.16f) : Range(0.04f, 0.09f),
                    phase = Range(0f, Mathf.PI * 2f), sway = Range(0.8f, 1.8f), spin = Range(-28f, 28f),
                    color = near ? new Color(0.95f, 0.98f, 1f, Range(0.65f, 0.9f))
                        : new Color(0.8f, 0.9f, 1f, Range(0.3f, 0.55f))
                };
                flakes[i] = flake;
                Render(flake, area);
            }
        }

        private void Render(Flake flake, Rect area)
        {
            Transform item = flake.renderer.transform;
            item.position = new Vector3(flake.position.x, flake.position.y, 0f);
            item.rotation = Quaternion.Euler(0f, 0f, flake.phase * Mathf.Rad2Deg + snowTime * flake.spin);
            item.localScale = Vector3.one * flake.size;
            float edge = Mathf.Min(Mathf.Min(flake.position.x - area.xMin, area.xMax - flake.position.x),
                Mathf.Min(flake.position.y - area.yMin, area.yMax - flake.position.y));
            Color color = flake.color;
            color.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.3f));
            flake.renderer.color = color;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private void OnDisable()
        {
            if (pool != null) pool.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (pool != null) Destroy(pool.gameObject);
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
