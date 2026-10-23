using UnityEngine;

namespace DinoRush
{
    /// <summary>Short, interruptible entrances driven by unscaled time, with exact resting layout.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class RunnerUiReveal : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration = 0.24f;
        [SerializeField] private Vector2 enterOffset = new Vector2(0, -12);
        [SerializeField] private float enterScale = 0.975f;
        [SerializeField, Min(0f)] private float idleFloat;
        private RectTransform rect;
        private CanvasGroup group;
        private Vector2 position;
        private Vector3 scale;
        private float alpha, elapsed;
        private bool captured, revealing;

        public bool IsRevealing => revealing;
        public Vector2 RestPosition => captured ? position : ((RectTransform)transform).anchoredPosition;
        public Vector3 RestScale => captured ? scale : transform.localScale;

        public void SetRestLayout(Vector2 restPosition, Vector3 restScale)
        {
            Capture();
            position = restPosition;
            scale = restScale;
            if (revealing)
                Apply(1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)), 3f));
            else Restore();
        }

        public void Configure(float seconds, Vector2 offset, float initialScale = 1f, float floatPixels = 0f)
        {
            duration = seconds;
            enterOffset = offset;
            enterScale = initialScale;
            idleFloat = floatPixels;
        }

        private void Capture()
        {
            if (captured) return;
            rect = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            position = rect.anchoredPosition;
            scale = rect.localScale;
            alpha = group.alpha;
            captured = true;
        }

        private void OnEnable()
        {
            if (Application.isPlaying) Replay();
        }

        public void Replay()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            Capture();
            elapsed = 0f;
            revealing = true;
            Apply(0f);
        }

        private void Update()
        {
            if (!captured || (!revealing && idleFloat <= 0f)) return;
            elapsed += Time.unscaledDeltaTime;
            if (revealing)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                Apply(1f - Mathf.Pow(1f - t, 3f));
                if (t >= 1f)
                {
                    Restore();
                    revealing = false;
                }
            }
            else
            {
                // Whole design pixels keep the idle title motion crisp rather than shimmering.
                rect.anchoredPosition = position + Vector2.up * Mathf.Round(Mathf.Sin((elapsed - duration) * 1.7f) * idleFloat);
            }
        }

        private void Apply(float progress)
        {
            rect.anchoredPosition = position + enterOffset * (1f - progress);
            rect.localScale = scale * Mathf.Lerp(enterScale, 1f, progress);
            group.alpha = alpha * Mathf.Lerp(0.15f, 1f, progress);
            // Raycast blocking and interactability never change: an opening modal still
            // shields the game immediately, and button actions never wait on animation.
        }

        private void Restore()
        {
            if (!captured) return;
            rect.anchoredPosition = position;
            rect.localScale = scale;
            group.alpha = alpha;
        }

        private void OnDisable()
        {
            Restore();
            revealing = false;
        }
    }
}
