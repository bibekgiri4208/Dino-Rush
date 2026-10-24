using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>A tiny ribboned pixel present, drawn without additional textures or source-art edits.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RunnerGiftGraphic : MaskableGraphic
    {
        private float opening;
        public void Animate(float progress) { opening = Mathf.Clamp01(progress); SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector2 center = rectTransform.rect.center;
            float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 100;
            Color ink = new Color32(36, 64, 52, 255), green = new Color32(154, 193, 112, 255);
            Color gold = new Color32(255, 211, 116, 255), shade = new Color32(207, 154, 69, 255);
            Rect(mesh, center, scale, -34, -36, 68, 52, ink);
            Rect(mesh, center, scale, -30, -32, 60, 44, green);
            Rect(mesh, center, scale, 19, -32, 11, 44, new Color32(117, 156, 83, 255));
            Rect(mesh, center, scale, -7, -32, 14, 44, shade);
            Rect(mesh, center, scale, -5, -32, 8, 44, gold);
            float lift = (1 - Mathf.Pow(1 - opening, 3)) * 28;
            Rect(mesh, center, scale, -38, 10 + lift, 76, 14, ink);
            Rect(mesh, center, scale, -34, 14 + lift, 68, 6, green);
            Rect(mesh, center, scale, -7, 14 + lift, 14, 6, gold);
            Rect(mesh, center, scale, -25, 28 + lift, 18, 6, gold);
            Rect(mesh, center, scale, -25, 22 + lift, 6, 6, gold);
            Rect(mesh, center, scale, 7, 28 + lift, 18, 6, gold);
            Rect(mesh, center, scale, 19, 22 + lift, 6, 6, gold);
            Rect(mesh, center, scale, -7, 22 + lift, 14, 10, shade);
            if (opening > 0)
            {
                Color light = gold; light.a = opening * 0.85f;
                Rect(mesh, center, scale, -24, 17, 48, lift * 0.55f, light);
            }
        }

        private static void Rect(VertexHelper mesh, Vector2 center, float scale, float x, float y, float width, float height, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center + new Vector2(x, y) * scale, color, Vector2.zero);
            mesh.AddVert(center + new Vector2(x, y + height) * scale, color, Vector2.zero);
            mesh.AddVert(center + new Vector2(x + width, y + height) * scale, color, Vector2.zero);
            mesh.AddVert(center + new Vector2(x + width, y) * scale, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
