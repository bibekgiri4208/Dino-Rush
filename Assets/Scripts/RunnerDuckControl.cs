using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Hold input for mouse/touch, released even when the HUD is hidden mid-press.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class RunnerDuckControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private readonly HashSet<int> pointers = new HashSet<int>();
        private Action<bool> setHeld;

        public void Initialize(Action<bool> callback)
        {
            Release();
            setHeld = callback;
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !GetComponent<Button>().IsInteractable()) return;
            EventSystem.current?.SetSelectedGameObject(null);
            if (pointers.Add(data.pointerId) && pointers.Count == 1) setHeld?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (pointers.Remove(data.pointerId) && pointers.Count == 0) setHeld?.Invoke(false);
        }

        private void OnDisable() => Release();

        private void Release()
        {
            if (pointers.Count == 0) return;
            pointers.Clear();
            setHeld?.Invoke(false);
        }
    }
}
