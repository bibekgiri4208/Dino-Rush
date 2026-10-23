using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Quiet, non-interactive thoughts from the title-screen dino, never during a run.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class RunnerMenuThought : MonoBehaviour
    {
        [SerializeField] private ForestRunnerGame game;
        [SerializeField] private SpriteRenderer dino;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Text caption;
        [SerializeField] private string[] thoughts =
        {
            "What a beautiful\nday!",
            "It's so peaceful\nhere...",
            "I should stretch\nmy legs.",
            "Just one little\nadventure?",
            "The breeze feels\nso nice.",
            "No rush...\nyet!"
        };
        [SerializeField, Min(0f)] private float firstDelay = 1.2f;
        [SerializeField, Min(1f)] private float visibleSeconds = 4.5f;
        [SerializeField] private Vector2 quietSeconds = new Vector2(6f, 9f);
        private const float FadeSeconds = 0.22f;
        private CanvasGroup group;
        private RectTransform rect;
        private ForestMainMenu menu;
        // Cosmetic randomness must not consume the encounter generator's Unity random stream.
        private readonly System.Random random = new System.Random();
        private float elapsed, wait;
        private int lastThought = -1;
        private bool speaking;

        public bool IsShowing => isActiveAndEnabled && group != null && group.alpha > 0f;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            menu = GetComponentInParent<ForestMainMenu>();
            group.interactable = group.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            elapsed = 0f;
            wait = firstDelay;
            speaking = false;
            GetComponent<CanvasGroup>().alpha = 0f;
        }

        private void OnDisable()
        {
            GetComponent<CanvasGroup>().alpha = 0f;
        }

        private void LateUpdate()
        {
            if (game == null || game.State != ForestRunnerGame.RunState.Ready ||
                menu == null || menu.DialogOpen || dino == null || worldCamera == null || caption == null ||
                thoughts == null || thoughts.Length == 0)
            {
                group.alpha = 0f;
                elapsed = 0f;
                speaking = false;
                wait = firstDelay;
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (!speaking)
            {
                if (elapsed < wait) return;
                int next = lastThought < 0 ? 0 : random.Next(Mathf.Max(1, thoughts.Length - 1));
                if (lastThought >= 0 && next >= lastThought && thoughts.Length > 1) next++;
                lastThought = next;
                caption.text = thoughts[next];
                speaking = true;
                elapsed = 0f;
            }
            if (elapsed >= visibleSeconds)
            {
                group.alpha = 0f;
                speaking = false;
                elapsed = 0f;
                wait = Mathf.Lerp(Mathf.Max(0f, quietSeconds.x), Mathf.Max(quietSeconds.x, quietSeconds.y), (float)random.NextDouble());
                return;
            }

            group.alpha = Mathf.Clamp01(Mathf.Min(elapsed, visibleSeconds - elapsed) / FadeSeconds);
            FollowDino();
        }

        private void FollowDino()
        {
            RectTransform parent = (RectTransform)rect.parent;
            Vector3 head = new Vector3(dino.bounds.center.x, dino.bounds.max.y, dino.transform.position.z);
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, worldCamera.WorldToScreenPoint(head), uiCamera, out Vector2 point)) return;
            float halfWidth = rect.rect.width * 0.5f;
            float halfHeight = rect.rect.height * 0.5f;
            RectTransform safe = canvas.transform.Find("16 by 9 safe content") as RectTransform;
            Vector3 min = safe != null ? parent.InverseTransformPoint(safe.TransformPoint(safe.rect.min)) : (Vector3)parent.rect.min;
            Vector3 max = safe != null ? parent.InverseTransformPoint(safe.TransformPoint(safe.rect.max)) : (Vector3)parent.rect.max;
            // Reserve the center column in landscape; portrait uses its whole narrow safe width.
            float right = safe != null && safe.rect.height > safe.rect.width ? max.x - halfWidth - 16f : -204f - halfWidth;
            point.x = Mathf.Clamp(point.x, min.x + halfWidth + 16f, right);
            point.y = Mathf.Clamp(point.y + halfHeight + 10f + Mathf.Sin(elapsed * 1.8f) * 2f,
                min.y + halfHeight + 16f, max.y - halfHeight - 16f);
            rect.anchoredPosition = point;
        }
    }
}
