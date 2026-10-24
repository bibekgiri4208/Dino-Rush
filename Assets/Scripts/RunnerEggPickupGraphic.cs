using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Texture-free gold light, pixel glints and arrival rings for pooled egg feedback.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RunnerEggPickupGraphic : MaskableGraphic
    {
        public enum Shape { Glow, Glint, Arrival }
        private Shape shape;
        private float progress;

        public void SetEffect(Shape value, float phase, float opacity)
        {
            shape = value;
            progress = Mathf.Clamp01(phase);
            color = new Color(1, 0.84f, 0.4f, opacity);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Vector2 center = rectTransform.rect.center;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            if (shape == Shape.Glint)
            {
                Quad(mesh, center, new Vector2(radius * 0.22f, radius), color);
                Quad(mesh, center, new Vector2(radius, radius * 0.22f), color);
                return;
            }
            if (shape == Shape.Glow)
            {
                Color edge = color; edge.a = 0;
                for (int i = 0; i < 24; i++)
                {
                    int start = mesh.currentVertCount;
                    mesh.AddVert(center, color, Vector2.zero);
                    mesh.AddVert(center + Direction(i * Mathf.PI / 12) * radius, edge, Vector2.zero);
                    mesh.AddVert(center + Direction((i + 1) * Mathf.PI / 12) * radius, edge, Vector2.zero);
                    mesh.AddTriangle(start, start + 1, start + 2);
                }
                return;
            }
            float ring = Mathf.Lerp(radius * 0.26f, radius * 0.8f, 1 - Mathf.Pow(1 - progress, 3));
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12;
                int start = mesh.currentVertCount;
                mesh.AddVert(center + Direction(a) * ring, color, Vector2.zero);
                mesh.AddVert(center + Direction(b) * ring, color, Vector2.zero);
                mesh.AddVert(center + Direction(b) * (ring - 1.5f), color, Vector2.zero);
                mesh.AddVert(center + Direction(a) * (ring - 1.5f), color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 point = center + Direction(i * Mathf.PI / 2 + Mathf.PI / 4) * radius * Mathf.Lerp(0.4f, 0.95f, progress);
                float size = (1 - progress) * 3;
                Quad(mesh, point, new Vector2(size * 0.4f, size), color);
                Quad(mesh, point, new Vector2(size, size * 0.4f), color);
            }
        }

        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        private static void Quad(VertexHelper mesh, Vector2 center, Vector2 half, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center + new Vector2(-half.x, -half.y), color, Vector2.zero);
            mesh.AddVert(center + new Vector2(-half.x, half.y), color, Vector2.zero);
            mesh.AddVert(center + new Vector2(half.x, half.y), color, Vector2.zero);
            mesh.AddVert(center + new Vector2(half.x, -half.y), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
