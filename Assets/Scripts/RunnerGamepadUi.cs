using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DinoRush
{
    /// <summary>Controller input and visible, modal-scoped focus without changing pointer/keyboard bindings.</summary>
    public sealed class RunnerGamepadUi : IDisposable
    {
        private readonly ForestRunnerGame game;
        private readonly RunnerHud hud;
        private readonly List<Selectable> controls = new List<Selectable>();
        private readonly Dictionary<Transform, Selectable> remembered = new Dictionary<Transform, Selectable>();
        private readonly RectTransform marker;
        private readonly Text hint;
        private readonly RunnerGamepadKeyboard keyboard;
        private readonly Vector3[] corners = new Vector3[4];
        private Transform scope;
        private Selectable focused;
        private Vector2 heldDirection;
        private float repeatAt;
        public bool Active { get; private set; }
        public Selectable Focused => focused;
        public Transform Scope => scope;

        public RunnerGamepadUi(ForestRunnerGame game, RunnerHud hud, RunnerAudio audio)
        {
            this.game = game;
            this.hud = hud;
            marker = (RectTransform)RunnerShopPresentation.Node("Gamepad focus", hud.Root);
            for (int i = 0; i < 4; i++)
            {
                var line = RunnerShopPresentation.Art(RunnerShopPresentation.Node("Focus edge " + i, marker), null,
                    new Color32(255, 224, 133, 255), false).rectTransform;
                bool horizontal = i < 2;
                line.anchorMin = horizontal ? new Vector2(0, i) : new Vector2(i - 2, 0);
                line.anchorMax = horizontal ? new Vector2(1, i) : new Vector2(i - 2, 1);
                line.sizeDelta = horizontal ? new Vector2(0, 3) : new Vector2(3, 0);
                line.anchoredPosition = Vector2.zero;
            }
            hint = RunnerShopPresentation.Label("Gamepad controls", hud.Root, "", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                13, 0, 0, 1000, 26, new Color32(255, 240, 195, 255));
            hint.rectTransform.anchorMin = new Vector2(0, 0);
            hint.rectTransform.anchorMax = new Vector2(1, 0);
            hint.rectTransform.pivot = new Vector2(0.5f, 0);
            hint.rectTransform.sizeDelta = new Vector2(-24, 26);
            hint.rectTransform.anchoredPosition = new Vector2(0, 2);
            hint.resizeTextForBestFit = true;
            hint.resizeTextMinSize = 9;
            hint.resizeTextMaxSize = 13;
            hint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);
            keyboard = new RunnerGamepadKeyboard(hud.Root, audio);
            marker.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            Canvas.preWillRenderCanvases += RefreshFocus; // Update before UI meshes rebuild, including after resolution changes.
        }

        public bool Read(Gamepad pad)
        {
            bool pointer = Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.delta.ReadValue().sqrMagnitude > 4);
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            if (pad == null || pointer || touch || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)) SetActive(false);
            if (pad != null && HasActivity(pad)) SetActive(true);
            if (!Active) return false;
            RefreshFocus();
            if (pad.startButton.wasPressedThisFrame &&
                (game.State == ForestRunnerGame.RunState.Running || game.State == ForestRunnerGame.RunState.Paused))
            {
                game.TogglePause(); RefreshFocus(); return true;
            }
            if (game.State == ForestRunnerGame.RunState.Running)
            {
                if (pad.buttonSouth.wasPressedThisFrame) game.Jump();
                if (pad.buttonWest.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame) game.Attack();
                if (pad.buttonNorth.wasPressedThisFrame) game.ToggleGamepadSound();
                return false;
            }
            if (game.State == ForestRunnerGame.RunState.AwaitingStart)
            {
                if (pad.buttonEast.wasPressedThisFrame) { game.ReturnToMenu(); RefreshFocus(); return true; }
                if (pad.buttonSouth.wasPressedThisFrame) { game.PrimaryAction(); RefreshFocus(); return true; }
                return false;
            }
            if (keyboard.IsOpen && (pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame))
            {
                if (pad.buttonWest.wasPressedThisFrame) keyboard.Backspace(); else keyboard.Append(" ");
                return true;
            }
            if (pad.buttonEast.wasPressedThisFrame)
            {
                if (keyboard.IsOpen) keyboard.Close();
                else if (game.State == ForestRunnerGame.RunState.Paused) game.TogglePause();
                else if (game.State == ForestRunnerGame.RunState.GameOver) game.ReturnToMenu();
                else hud.GamepadBack();
                RefreshFocus(); return true;
            }
            Vector2 direction = Direction(pad);
            if (direction == Vector2.zero) heldDirection = Vector2.zero;
            else if (direction != heldDirection || Time.unscaledTime >= repeatAt)
            {
                bool first = direction != heldDirection;
                heldDirection = direction;
                repeatAt = Time.unscaledTime + (first ? 0.32f : 0.13f);
                Move(direction);
            }
            // Shoulder cycling also reaches isolated corner controls and irregular card layouts.
            if (pad.leftShoulder.wasPressedThisFrame) Cycle(-1);
            if (pad.rightShoulder.wasPressedThisFrame) Cycle(1);
            if (!pad.buttonSouth.wasPressedThisFrame) return false;
            if (focused is InputField input) keyboard.Open(input);
            else if (focused is Button button && Usable(button))
                ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            RefreshFocus();
            return true;
        }

        private static bool HasActivity(Gamepad pad) => Direction(pad) != Vector2.zero ||
            pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
            pad.buttonNorth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame ||
            pad.rightShoulder.wasPressedThisFrame;

        private static Vector2 Direction(Gamepad pad)
        {
            Vector2 value = pad.dpad.ReadValue();
            if (value.sqrMagnitude < 0.25f) value = pad.leftStick.ReadValue();
            if (value.magnitude < 0.55f) return Vector2.zero;
            return Mathf.Abs(value.x) > Mathf.Abs(value.y) ? new Vector2(Mathf.Sign(value.x), 0) : new Vector2(0, Mathf.Sign(value.y));
        }

        private void SetActive(bool value)
        {
            if (Active == value) return;
            Active = value;
            hud.SetGamepadPresentation(value);
            heldDirection = Vector2.zero;
            if (value) return;
            InputField editing = keyboard.IsOpen ? keyboard.Target : focused as InputField;
            keyboard.Close();
            focused?.GetComponent<RunnerUiButtonFeedback>()?.SetFocused(false);
            if (editing != null && editing.IsActive())
            {
                editing.Select();
                editing.ActivateInputField();
            }
            else if (EventSystem.current != null && focused != null && EventSystem.current.currentSelectedGameObject == focused.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            focused = null;
            scope = null;
            marker.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
        }

        public void RefreshFocus()
        {
            if (!Active) return;
            keyboard.Validate();
            Transform nextScope = keyboard.IsOpen ? keyboard.Root : hud.NavigationRoot(game.State);
            bool changed = scope != nextScope;
            if (changed)
            {
                scope = nextScope;
                SetFocus(null);
                heldDirection = Vector2.zero;
            }
            controls.Clear();
            if (scope != null) scope.GetComponentsInChildren(false, controls);
            controls.RemoveAll(item => !Usable(item));
            if (!controls.Contains(focused))
            {
                Selectable preferred = null;
                if (scope != null && !(changed && hud.PreferSafeNavigation)) remembered.TryGetValue(scope, out preferred);
                if (!controls.Contains(preferred)) preferred = hud.NavigationDefault(game.State);
                SetFocus(controls.Contains(preferred) ? preferred : controls.Count > 0 ? controls[0] : null);
            }
            else SetFocus(focused); // Existing button listeners may clear EventSystem selection after activation.
            marker.gameObject.SetActive(focused != null);
            if (focused != null)
            {
                ((RectTransform)focused.transform).GetWorldCorners(corners);
                Vector2 min = hud.Root.InverseTransformPoint(corners[0]);
                Vector2 max = min;
                foreach (Vector3 corner in corners)
                {
                    Vector2 point = hud.Root.InverseTransformPoint(corner);
                    min = Vector2.Min(min, point); max = Vector2.Max(max, point);
                }
                RunnerShopPresentation.Position(marker, (min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.x - min.x + 10, max.y - min.y + 10);
                if (marker.GetSiblingIndex() < hud.Root.childCount - 2) marker.SetAsLastSibling();
            }
            bool portrait = hud.Root.rect.height > hud.Root.rect.width;
            hint.rectTransform.sizeDelta = new Vector2(-24, portrait ? 54 : 26);
            hint.resizeTextMinSize = portrait ? 16 : 9;
            hint.resizeTextMaxSize = portrait ? 20 : 13;
            hint.text = keyboard.IsOpen ? (portrait ? "A: TYPE   X: DELETE\nY: SPACE   B: CLOSE" : "A: TYPE   X: DELETE   Y: SPACE   B: CLOSE") :
                game.State == ForestRunnerGame.RunState.Running ? (portrait ? "A: JUMP   B / DOWN: DUCK\nX / RB: SLASH   START: PAUSE   Y: SFX" : "A: JUMP / DOUBLE JUMP   B / DOWN: DUCK   X / RB: SLASH   START: PAUSE   Y: SFX") :
                game.State == ForestRunnerGame.RunState.AwaitingStart ? "A: START RUN   B: BACK" :
                portrait ? "D-PAD / STICK: MOVE   A: CONFIRM\nB: BACK   LB / RB: CYCLE" : "D-PAD / STICK: MOVE   A: CONFIRM   B: BACK   LB / RB: CYCLE";
            hud.SetGamepadPresentation(true);
            hint.gameObject.SetActive(true);
            if (hint.transform.GetSiblingIndex() != hud.Root.childCount - 1) hint.transform.SetAsLastSibling();
        }

        private static bool Usable(Selectable item) => item != null && item.IsActive() && item.IsInteractable() &&
            item.transform is RectTransform rect && rect.rect.width > 1 && rect.rect.height > 1;

        private void SetFocus(Selectable item)
        {
            if (focused != item) focused?.GetComponent<RunnerUiButtonFeedback>()?.SetFocused(false);
            focused = item;
            if (scope != null && item != null) remembered[scope] = item;
            focused?.GetComponent<RunnerUiButtonFeedback>()?.SetFocused(true);
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != (item != null ? item.gameObject : null))
            {
                // Highlighting a text field must not summon an OS keyboard before A opens ours.
                InputField field = item as InputField;
                bool activate = field != null && field.shouldActivateOnSelect;
                if (field != null) field.shouldActivateOnSelect = false;
                EventSystem.current.SetSelectedGameObject(item != null ? item.gameObject : null);
                if (field != null) field.shouldActivateOnSelect = activate;
            }
        }

        private void Cycle(int step)
        {
            if (controls.Count == 0) return;
            int index = controls.IndexOf(focused);
            SetFocus(controls[(index + step + controls.Count) % controls.Count]);
            RefreshFocus();
        }

        private void Move(Vector2 direction)
        {
            if (focused == null) return;
            Vector2 origin = Center(focused);
            Selectable best = null;
            float bestScore = float.MaxValue;
            foreach (Selectable item in controls)
            {
                if (item == focused) continue;
                Vector2 delta = Center(item) - origin;
                float forward = Vector2.Dot(delta, direction);
                if (forward < 1f) continue;
                float sideways = Mathf.Abs(delta.x * direction.y - delta.y * direction.x);
                float score = delta.magnitude * (1f + sideways / forward * 2f);
                if (score < bestScore) { bestScore = score; best = item; }
            }
            if (best != null) { SetFocus(best); RefreshFocus(); }
        }

        private static Vector2 Center(Selectable item)
        {
            var rect = (RectTransform)item.transform;
            return rect.TransformPoint(rect.rect.center);
        }

        public void Dispose()
        {
            Canvas.preWillRenderCanvases -= RefreshFocus;
            SetActive(false);
            keyboard.Dispose();
            Object.Destroy(marker.gameObject);
            Object.Destroy(hint.gameObject);
        }
    }
}
