using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Animates button artwork without moving its hit area, and sounds accepted activations.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class RunnerUiButtonFeedback : BaseMeshEffect, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.025f;
        [SerializeField] private float pressedScale = 0.97f;
        [SerializeField] private Color glowColor = new Color(0.78f, 0.88f, 0.48f, 1f);
        private readonly HashSet<int> hovering = new HashSet<int>();
        private readonly HashSet<int> pressing = new HashSet<int>();
        private Button button;
        private Outline glow;
        private RectTransform[] content;
        private Vector2[] positions;
        private Vector3[] scales;
        private float visualScale = 1f;
        private float lift, hover, clickTime;
        private bool initialized;
        private bool held;
        private bool focused;
        private RunnerAudio audioCues;

        public void SetAudio(RunnerAudio audio) => audioCues = audio;

        public bool IsPressed => held || pressing.Count > 0;
        public bool IsHovered => hovering.Count > 0 || focused;
        public void SetFocused(bool value) => focused = value;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Application.isPlaying) return;
            Initialize();
            Restore();
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            button = GetComponent<Button>();
            glow = GetComponent<Outline>();
            button.onClick.AddListener(OnClicked);
            var children = new List<RectTransform>();
            foreach (Transform child in transform)
                if (child is RectTransform rect) children.Add(rect);
            content = children.ToArray();
            positions = new Vector2[content.Length];
            scales = new Vector3[content.Length];
            for (int i = 0; i < content.Length; i++)
            {
                positions[i] = content[i].anchoredPosition;
                scales[i] = content[i].localScale;
            }
        }

        private bool CanInteract => isActiveAndEnabled && button != null && button.IsActive() && button.IsInteractable();

        private static bool HasHover(PointerEventData data) => data is ExtendedPointerEventData extended
            ? extended.pointerType == UIPointerType.MouseOrPen : data.pointerId < 0;

        public void OnPointerEnter(PointerEventData data)
        {
            if (CanInteract && HasHover(data)) hovering.Add(data.pointerId);
        }

        public void OnPointerExit(PointerEventData data) => hovering.Remove(data.pointerId);

        public void OnPointerDown(PointerEventData data)
        {
            if (CanInteract && data.button == PointerEventData.InputButton.Left) pressing.Add(data.pointerId);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) pressing.Remove(data.pointerId);
        }

        public void Pulse()
        {
            if (CanInteract) clickTime = 0.24f;
        }

        private void OnClicked()
        {
            // Button.onClick means an accepted activation. Its action may already have hidden
            // or disabled the widget; the sound must still finish on the game-owned source.
            audioCues?.ButtonClick();
            Pulse();
        }

        public void SetHeld(bool value) => held = value && CanInteract;

        /// <summary>Adopt a new resting child layout after a dialog reflow, without restoring old offsets.</summary>
        public void RefreshContentLayout()
        {
            if (!initialized) return;
            hovering.Clear();
            pressing.Clear();
            held = false;
            clickTime = hover = lift = 0;
            visualScale = 1;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == null) continue;
                positions[i] = content[i].anchoredPosition;
                scales[i] = content[i].localScale;
            }
            SetGlow(0);
            if (graphic != null) graphic.SetVerticesDirty();
        }

        private void Update()
        {
            if (!initialized) return;
            if (!CanInteract)
            {
                if (IsPressed || IsHovered || visualScale != 1f || lift != 0f || clickTime > 0f) Restore();
                return;
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float response = 1f - Mathf.Exp(-24f * dt);
            clickTime = Mathf.Max(0f, clickTime - dt);
            float pulse = Mathf.Sin(Mathf.PI * clickTime / 0.24f);
            hover = Mathf.Lerp(hover, IsHovered ? 1f : 0f, response);
            float targetScale = IsPressed ? pressedScale : Mathf.Lerp(1f, hoverScale, hover) + pulse * 0.035f;
            float targetLift = IsPressed ? -2f : hover * 2f + pulse;
            float nextScale = Mathf.Lerp(visualScale, targetScale, response);
            float nextLift = Mathf.Lerp(lift, targetLift, response);
            if (!IsHovered && !IsPressed && clickTime == 0f && Mathf.Abs(nextScale - 1f) < 0.0002f && Mathf.Abs(nextLift) < 0.01f)
            {
                nextScale = 1f;
                nextLift = hover = 0f;
            }
            if (visualScale != nextScale || lift != nextLift)
            {
                visualScale = nextScale;
                lift = nextLift;
                ApplyContent();
                graphic.SetVerticesDirty();
            }
            SetGlow(Mathf.Clamp01(hover * 0.32f + pulse * 0.65f + (IsPressed ? 0.22f : 0f)));
        }

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive() || !Application.isPlaying || (visualScale == 1f && lift == 0f)) return;
            var vertex = new UIVertex();
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                Vector3 position = vertex.position;
                vertex.position = new Vector3(position.x * visualScale, position.y * visualScale + lift, position.z);
                vertices.SetUIVertex(vertex, i);
            }
        }

        private void ApplyContent()
        {
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == null) continue;
                content[i].anchoredPosition = positions[i] * visualScale + Vector2.up * lift;
                content[i].localScale = scales[i] * visualScale;
            }
        }

        private void SetGlow(float alpha)
        {
            if (glow == null) glow = GetComponent<Outline>(); // Runtime fallback may add the outline after this component.
            if (glow == null) return;
            Color color = glowColor;
            color.a *= alpha;
            glow.effectColor = color;
            glow.enabled = alpha > 0.001f;
        }

        private void Restore()
        {
            focused = false;
            hovering.Clear();
            pressing.Clear();
            held = false;
            clickTime = hover = lift = 0f;
            visualScale = 1f;
            if (!initialized) return;
            ApplyContent();
            SetGlow(0f);
            if (graphic != null) graphic.SetVerticesDirty();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) Restore(); }

        protected override void OnDisable()
        {
            if (Application.isPlaying) Restore();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
            audioCues = null;
            base.OnDestroy();
        }
    }
}
