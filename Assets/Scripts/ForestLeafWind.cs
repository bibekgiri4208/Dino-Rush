using UnityEngine;

namespace DinoRush
{
    /// <summary>A small, reusable leaf pool with shared gusts and individual flutter. Visual only.</summary>
    [DisallowMultipleComponent]
    public sealed class ForestLeafWind : MonoBehaviour
    {
        [SerializeField] private ForestRunnerGame game;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Texture2D leafTexture;
        [SerializeField] private Material spriteMaterial;
        [Header("Forest breeze")]
        [SerializeField, Range(8, 40)] private int leafCount = 24;
        [SerializeField, Range(0.2f, 3f)] private float driftSpeed = 1.25f;
        [SerializeField, Range(0f, 2f)] private float gustStrength = 0.9f;
        [SerializeField, Range(0.05f, 0.8f)] private float fallSpeed = 0.26f;
        [SerializeField, Range(0f, 1.5f)] private float runScrollInfluence = 1f;
        [SerializeField] private int seed = 8127;

        private sealed class Leaf
        {
            public SpriteRenderer renderer;
            public Vector2 position;
            public float depth, size, phase, flutter, spin, angle;
            public Color color;
        }

        private static readonly Color[] LeafColors =
        {
            new Color32(157, 192, 95, 255), new Color32(192, 207, 120, 255),
            new Color32(114, 160, 85, 255), new Color32(192, 174, 101, 255)
        };
        private Leaf[] leaves;
        private Transform pool;
        private Sprite sprite;
        private System.Random random;
        private float breezeTime;

        private void Start() => Simulate(0f);

        private void LateUpdate() => Simulate(Time.deltaTime);

        private bool IsForest => game.State == ForestRunnerGame.RunState.Ready ||
            game.MapCatalog == null || game.MapCatalog.Count == 0 || game.MapCatalog[game.SelectedMapIndex].id == "forest";

        private Rect FlightArea()
        {
            Vector3 center = worldCamera.transform.position;
            float halfWidth = worldCamera.orthographicSize * worldCamera.aspect;
            return Rect.MinMaxRect(center.x - halfWidth - 0.6f, RunnerRules.GroundY + 0.28f,
                center.x + halfWidth + 0.6f, center.y + worldCamera.orthographicSize + 0.6f);
        }

        // Explicit simulation keeps tests/captures deterministic and never uses gameplay's random stream.
        public void Simulate(float dt)
        {
            if (game == null || worldCamera == null || leafTexture == null) return;
            if (!IsForest)
            {
                if (pool != null) pool.gameObject.SetActive(false);
                return;
            }
            Rect area = FlightArea();
            if (leaves == null) CreatePool(area);
            pool.gameObject.SetActive(true);
            if (game.State == ForestRunnerGame.RunState.Paused || game.State == ForestRunnerGame.RunState.GameOver) return;
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            breezeTime += dt;
            float gust = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(breezeTime * 0.55f), 3f) * gustStrength;
            // The runner stays fixed on screen: world-space leaves must travel backward with the course.
            float courseSpeed = game.State == ForestRunnerGame.RunState.Running ? game.CurrentSpeed * runScrollInfluence : 0f;
            foreach (Leaf leaf in leaves)
            {
                float wave = breezeTime * leaf.flutter + leaf.phase;
                leaf.position.x -= ((courseSpeed + driftSpeed * (1f + gust)) * leaf.depth + Mathf.Sin(wave) * 0.12f) * dt;
                leaf.position.y += (-fallSpeed * leaf.depth + Mathf.Sin(wave * 0.73f) * 0.12f + gust * 0.04f) * dt;
                // New leaves enter from ahead, not from random positions above the player's head.
                if (leaf.position.x < area.xMin || leaf.position.y < area.yMin)
                    leaf.position = new Vector2(area.xMax, Range(area.yMin + 0.65f, area.yMax - 0.65f));
                if (leaf.position.y > area.yMax) leaf.position.y = area.yMax;
                Render(leaf, area, wave);
            }
        }

        private void CreatePool(Rect area)
        {
            random = new System.Random(seed);
            sprite = Sprite.Create(leafTexture, new Rect(0, 0, leafTexture.width, leafTexture.height),
                Vector2.one * 0.5f, leafTexture.height, 0, SpriteMeshType.FullRect);
            sprite.name = "Forest wind leaf";
            pool = new GameObject("Windblown leaves").transform;
            Transform effects = game.transform.Find("Forest effects");
            pool.SetParent(effects != null ? effects : game.transform, false);
            leaves = new Leaf[Mathf.Clamp(leafCount, 8, 40)];
            for (int i = 0; i < leaves.Length; i++)
            {
                bool near = i % 3 == 0;
                var renderer = new GameObject("Drifting leaf " + (i + 1), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(pool, false);
                renderer.sprite = sprite;
                if (spriteMaterial != null) renderer.sharedMaterial = spriteMaterial;
                // Both depths stay behind obstacles (15) and the dino (20), never hiding a hazard.
                renderer.sortingOrder = near ? 10 : -8;
                Color color = LeafColors[random.Next(LeafColors.Length)];
                color.a = near ? Range(0.65f, 0.88f) : Range(0.32f, 0.56f);
                var leaf = new Leaf { renderer = renderer, position = new Vector2(Range(area.xMin, area.xMax),
                    Range(area.yMin + 0.6f, area.yMax - 0.6f)), depth = near ? Range(1f, 1.35f) : Range(0.5f, 0.85f),
                    size = near ? Range(0.24f, 0.38f) : Range(0.12f, 0.23f), phase = Range(0f, Mathf.PI * 2f),
                    flutter = Range(1.7f, 3.5f), spin = Range(20f, 45f) * (i % 2 == 0 ? 1f : -1f),
                    angle = Range(0f, 360f), color = color };
                leaves[i] = leaf;
                Render(leaf, area, leaf.phase);
            }
        }

        private void Render(Leaf leaf, Rect area, float wave)
        {
            Transform item = leaf.renderer.transform;
            item.position = new Vector3(leaf.position.x, leaf.position.y, 0f);
            item.rotation = Quaternion.Euler(0, 0, leaf.angle + breezeTime * leaf.spin + Mathf.Sin(wave) * 12f);
            // A turning leaf briefly presents its thin edge, rather than looking like a rotating sticker.
            float flutterWidth = Mathf.Lerp(0.22f, 1f, Mathf.Abs(Mathf.Cos(wave)));
            item.localScale = new Vector3(leaf.size * flutterWidth, leaf.size * (0.9f + 0.1f * Mathf.Sin(wave)), 1f);
            float edge = Mathf.Min(Mathf.Min(leaf.position.x - area.xMin, area.xMax - leaf.position.x),
                Mathf.Min(leaf.position.y - area.yMin, area.yMax - leaf.position.y));
            Color color = leaf.color;
            color.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.65f));
            leaf.renderer.color = color;
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
        }
    }
}
