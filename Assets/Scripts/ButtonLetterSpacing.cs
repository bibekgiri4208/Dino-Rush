using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Adds tracking to a single-line button caption without shifting its center.</summary>
    [RequireComponent(typeof(Text))]
    [DisallowMultipleComponent]
    public sealed class ButtonLetterSpacing : BaseMeshEffect
    {
        [SerializeField, Min(0)] private float spacing = 2f;
        private readonly List<UIVertex> vertices = new List<UIVertex>();

        public float Spacing => spacing;
        public float PreferredWidth => GetComponent<Text>().preferredWidth
            + Mathf.Max(0, GetComponent<Text>().text.Length - 1) * spacing;

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            Text label = GetComponent<Text>();
            if (label.text.Contains("\n")) return;
            int glyphCount = label.cachedTextGenerator.verts.Count / 4;
            if (glyphCount < 2) return;

            vertices.Clear();
            mesh.GetUIVertexStream(vertices);
            // Shadow effects duplicate the triangle stream. Apply identical tracking
            // to every copy, regardless of the component order in the Inspector.
            for (int i = 0; i < vertices.Count; i++)
            {
                UIVertex vertex = vertices[i];
                int glyph = (i / 6) % glyphCount;
                vertex.position.x += (glyph - (glyphCount - 1) * 0.5f) * spacing;
                vertices[i] = vertex;
            }
            mesh.Clear();
            mesh.AddUIVertexTriangleStream(vertices);
        }
    }
}
