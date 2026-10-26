using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Arrow-key adjustment alongside the game's scoped controller navigation.</summary>
    public sealed class RunnerMusicSlider : Slider
    {
        private int held;
        private float repeatAt;

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            // Navigation is disabled on the Selectable because controller focus is scoped by the game.
            if (IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left) Select();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsActive() || !IsInteractable() || EventSystem.current == null ||
                EventSystem.current.currentSelectedGameObject != gameObject || Keyboard.current == null)
            {
                held = 0;
                return;
            }
            int direction = (Keyboard.current.rightArrowKey.isPressed ? 1 : 0) -
                (Keyboard.current.leftArrowKey.isPressed ? 1 : 0);
            if (direction == 0) { held = 0; return; }
            if (direction == held && Time.unscaledTime < repeatAt) return;
            repeatAt = Time.unscaledTime + (direction == held ? 0.13f : 0.32f);
            held = direction;
            value += direction * 5f;
        }
    }
}
