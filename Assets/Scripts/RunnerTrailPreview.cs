using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Displays native particles in UI. A periodic small hop demonstrates the retained airborne wake.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RunnerTrailPreview : MaskableGraphic
    {
        [SerializeField] private RunnerTrailKind kind;
        [SerializeField] private bool foreground; // Retained old scene binding; no foreground shell is rendered.
        private RunnerTrailParticles particles;
        private RectTransform dino;
        private Vector2 dinoRest;
        private float elapsed;
        public RunnerTrailKind Kind { get => kind; set { kind = value; particles?.SetKind(value); SetMaterialDirty(); SetVerticesDirty(); } }
        public bool Foreground { get => foreground; set { foreground = value; SetVerticesDirty(); } }
        public int ActiveCount => particles != null ? particles.ActiveCount : 0;
        public override Texture mainTexture => particles != null ? particles.Texture : Texture2D.whiteTexture;
        public static float WorldScale(Vector2 size) => Mathf.Min(size.x / 4.8f, size.y / 3.2f);
        public static Vector2 FeetOffset(Vector2 size) => new Vector2(size.x * 0.23f, -size.y * 0.39f);
        public void SetDinoLayout(RectTransform image, Vector2 rest)
        {
            dino = image; dinoRest = rest;
            if (dino != null) dino.anchoredPosition = rest;
        }
        private void Update()
        {
            if (!Application.isPlaying || foreground) return;
            if (particles == null)
            {
                particles = new RunnerTrailParticles(transform);
                particles.SetKind(kind);
                for (int i = 0; i < 30; i++) particles.Advance(1 / 60f, Vector2.zero, false, 4.2f, true);
                SetMaterialDirty();
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            elapsed += dt;
            float phase = Mathf.Repeat(elapsed, 3.4f);
            float hop = phase > 1.3f && phase < 2.15f ? Mathf.Sin((phase - 1.3f) / 0.85f * Mathf.PI) * 0.7f : 0;
            particles.Advance(dt, new Vector2(0, hop), false, 4.2f, true);
            if (dino != null) dino.anchoredPosition = dinoRest + Vector2.up * hop * WorldScale(rectTransform.rect.size);
            SetVerticesDirty();
        }
        protected override void OnDisable()
        {
            particles?.Dispose(); particles = null; elapsed = 0;
            if (dino != null) dino.anchoredPosition = dinoRest;
            base.OnDisable();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            if (foreground || particles == null) { mesh.Clear(); return; }
            Vector2 size = rectTransform.rect.size;
            particles.DrawPreview(mesh, WorldScale(size), rectTransform.rect.center + FeetOffset(size));
        }
    }
}
