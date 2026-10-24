using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Soft radial light, gold rays and drifting pixel glints without textures or particles.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RunnerHatchGlowGraphic : MaskableGraphic
    {
        private float phase;
        private float intensity = 1f;
        public void Animate(float time, float strength)
        {
            phase = time;
            intensity = strength;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.48f;
            Vector2 center = rectTransform.rect.center;
            Color clear = color; clear.a = 0;
            Color core = color; core.a *= 0.24f * intensity;
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                int start = mesh.currentVertCount;
                mesh.AddVert(center, core, Vector2.zero);
                mesh.AddVert(center + Direction(a) * radius, clear, Vector2.zero);
                mesh.AddVert(center + Direction(b) * radius, clear, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
            }
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6 + phase * 0.1f;
                float pulse = 0.6f + 0.4f * Mathf.Sin(phase * 1.8f + i * 2.3f);
                Color glint = color; glint.a *= pulse * 0.65f * intensity;
                Vector2 point = center + Direction(angle) * radius * (0.65f + 0.16f * Mathf.Sin(i * 7.1f + phase));
                float size = 2f + 2f * pulse;
                Quad(mesh, point, new Vector2(size, size * 3), glint);
                Quad(mesh, point, new Vector2(size * 3, size), glint);
                if (i % 2 != 0) continue;
                int start = mesh.currentVertCount;
                Color ray = color; ray.a *= 0.12f * intensity;
                mesh.AddVert(center + Direction(angle - 0.035f) * radius * 0.32f, ray, Vector2.zero);
                mesh.AddVert(center + Direction(angle + 0.035f) * radius * 0.32f, ray, Vector2.zero);
                mesh.AddVert(center + Direction(angle + 0.09f) * radius, clear, Vector2.zero);
                mesh.AddVert(center + Direction(angle - 0.09f) * radius, clear, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        private static void Quad(VertexHelper mesh, Vector2 center, Vector2 halfSize, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(center + new Vector2(-halfSize.x, -halfSize.y), color, Vector2.zero);
            mesh.AddVert(center + new Vector2(-halfSize.x, halfSize.y), color, Vector2.zero);
            mesh.AddVert(center + new Vector2(halfSize.x, halfSize.y), color, Vector2.zero);
            mesh.AddVert(center + new Vector2(halfSize.x, -halfSize.y), color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
