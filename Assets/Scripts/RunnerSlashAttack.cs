using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Fixed-step moving strike and bounded, texture-matched obstacle fragments. No gameplay RNG.</summary>
    public sealed class RunnerSlashAttack : MonoBehaviour
    {
        [SerializeField] private RunnerSlashCatalog catalog;
        private SpriteRenderer slash;
        private RunnerSlashKind kind;
        private float age;
        private bool active;
        private Vector3 previous;
        private readonly List<Piece> pieces = new List<Piece>();
        private readonly Dictionary<Sprite, Sprite[]> cuts = new Dictionary<Sprite, Sprite[]>();
        private sealed class Piece { public SpriteRenderer art; public Vector2 velocity; public float life, spin; }
        public bool Active => active;
        public int DebrisCount { get { int count = 0; foreach (var p in pieces) if (p.life > 0) count++; return count; } }
        public RunnerSlashCatalog Catalog => catalog;
        public Rect Hitbox => active ? Rect.MinMaxRect(previous.x - 0.85f, slash.transform.position.y - 1.2f,
            slash.transform.position.x + 0.85f, slash.transform.position.y + 1.2f) : Rect.zero;
        public void Configure(RunnerSlashCatalog source) => catalog = source;
        public bool Launch(RunnerSlashKind style, Vector3 feet)
        {
            if (active || catalog == null || catalog.Frame(style, 0) == null) return false;
            if (slash == null) slash = Renderer("Active slash", 35);
            slash.flipX = true; // Source artwork faces left; attacks travel to the right.
            kind = style; age = 0; active = true;
            slash.transform.position = feet + new Vector3(0.8f, 1.05f, 0);
            previous = slash.transform.position;
            slash.sprite = catalog.Frame(kind, 0);
            slash.transform.localScale = Vector3.one * (3.2f / slash.sprite.bounds.size.y);
            slash.gameObject.SetActive(true);
            return true;
        }
        public void Tick(float dt)
        {
            if (active)
            {
                previous = slash.transform.position;
                age += dt;
                slash.transform.position += Vector3.right * (22 * dt);
                slash.sprite = catalog.Frame(kind, age);
                if (age >= 0.6f) { active = false; slash.gameObject.SetActive(false); }
            }
            foreach (Piece p in pieces)
            {
                if (p.life <= 0) continue;
                p.life = Mathf.Max(0, p.life - dt);
                p.art.gameObject.SetActive(p.life > 0);
                p.velocity += Vector2.down * (12 * dt);
                p.art.transform.position += (Vector3)p.velocity * dt;
                p.art.transform.Rotate(0, 0, p.spin * dt);
                p.art.color = new Color(1, 1, 1, Mathf.Min(1, p.life * 3));
            }
        }
        public void Shatter(SpriteRenderer source)
        {
            Sprite sprite = source.sprite;
            if (!cuts.TryGetValue(sprite, out Sprite[] fragments))
            {
                fragments = new Sprite[9];
                Rect rect = sprite.rect;
                for (int i = 0; i < 9; i++) fragments[i] = Sprite.Create(sprite.texture,
                    new Rect(rect.x + (i % 3) * rect.width / 3, rect.y + (i / 3) * rect.height / 3, rect.width / 3, rect.height / 3),
                    Vector2.one * 0.5f, sprite.pixelsPerUnit);
                cuts.Add(sprite, fragments);
            }
            for (int i = 0; i < 9; i++)
            {
                Piece p = pieces.Find(item => item.life <= 0);
                if (p == null)
                {
                    if (pieces.Count >= 90) break;
                    p = new Piece { art = Renderer("Obstacle fragment", 32) }; pieces.Add(p);
                }
                p.art.sprite = fragments[i];
                Vector3 local = sprite.bounds.min + new Vector3((i % 3 + 0.5f) * sprite.bounds.size.x / 3,
                    (i / 3 + 0.5f) * sprite.bounds.size.y / 3, 0);
                p.art.transform.position = source.transform.TransformPoint(local);
                p.art.transform.rotation = source.transform.rotation;
                p.art.transform.localScale = source.transform.lossyScale;
                p.art.color = source.color;
                p.life = 0.85f + i * 0.025f;
                p.velocity = new Vector2((i % 3 - 1) * 2.5f + 1, 2.5f + i / 3);
                p.spin = (i % 2 == 0 ? 1 : -1) * (130 + i * 22);
                p.art.gameObject.SetActive(true);
            }
        }
        private SpriteRenderer Renderer(string title, int order)
        {
            var node = new GameObject(title); node.transform.SetParent(transform, false);
            var art = node.AddComponent<SpriteRenderer>(); art.sortingOrder = order; return art;
        }
        public void Clear()
        {
            active = false; if (slash != null) slash.gameObject.SetActive(false);
            foreach (var p in pieces) { p.life = 0; p.art.gameObject.SetActive(false); }
        }
        private void OnDestroy() { foreach (var batch in cuts.Values) foreach (var sprite in batch) Destroy(sprite); }
    }
}
