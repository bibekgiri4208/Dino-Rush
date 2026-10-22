using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Shared, dependency-free motion styling for authored and fallback UI.</summary>
    public static class RunnerUiEffects
    {
        public static void Install(RectTransform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<RunnerUiButtonFeedback>() != null) continue;
                // Add the mesh animation before its outline so the glow follows the art.
                button.gameObject.AddComponent<RunnerUiButtonFeedback>();
                Outline glow = button.GetComponent<Outline>();
                if (glow == null) glow = button.gameObject.AddComponent<Outline>();
                glow.effectDistance = new Vector2(2, -2);
                glow.effectColor = new Color(0.78f, 0.88f, 0.48f, 0f);
                glow.useGraphicAlpha = true;
                glow.enabled = false;
            }
            Reveal(root.Find("Forest Main Menu/Dino Rush Logo"), 0.32f, new Vector2(0, -8), 0.985f, 2f);
            Reveal(root.Find("Forest Main Menu/Main menu buttons"), 0.28f, new Vector2(0, -12));
            Reveal(root.Find("Forest Main Menu/Menu dialog/Moss framed dialog"), 0.22f, new Vector2(0, -14), 0.975f);
            Reveal(root.Find("Forest Main Menu/Map selector/Map selection panel"), 0.22f, new Vector2(0, -14), 0.975f);
            Reveal(root.Find("Forest Pause Menu"), 0.22f, new Vector2(0, -14), 0.975f);
            Reveal(root.Find("Menu card"), 0.26f, new Vector2(0, -16), 0.975f);
            Reveal(root.Find("Top left badge"), 0.24f, new Vector2(-10, 0));
            Reveal(root.Find("Scoreboard"), 0.24f, new Vector2(10, 0));
            Reveal(root.Find("Trail stats frame"), 0.24f, new Vector2(0, -8));
            Reveal(root.Find("Keyboard controls"), 0.28f, new Vector2(0, -8));
            Reveal(root.Find("Milestone toast"), 0.24f, new Vector2(0, -10), 0.96f);
            foreach (string name in new[] { "Jump", "Duck", "Sound", "Pause" })
                Reveal(root.Find(name), 0.18f, Vector2.zero); // Only fade interactive HUD roots; don't move their hitboxes.
        }

        private static void Reveal(Transform target, float duration, Vector2 offset, float scale = 1f, float idleFloat = 0f)
        {
            if (target == null || target.GetComponent<RunnerUiReveal>() != null) return;
            RunnerUiReveal reveal = target.gameObject.AddComponent<RunnerUiReveal>();
            reveal.Configure(duration, offset, scale, idleFloat);
            if (Application.isPlaying) reveal.Replay();
        }
    }
}
