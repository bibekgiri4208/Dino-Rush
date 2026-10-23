using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>A crisp pixel heart, including an empty outline, without font glyph dependencies.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RunnerHeartGraphic : MaskableGraphic
    {
        private static readonly string[] Pixels =
        {
            "..ooo.ooo..",
            ".orrrorrro.",
            "orhhrrrrrro",
            "orhrrrrrrro",
            "orrrrrrrrro",
            ".orrrrrrro.",
            "..orrrrro..",
            "...orrro...",
            "....oro....",
            ".....o....."
        };

        [SerializeField] private bool filled = true;
        public bool Filled
        {
            get => filled;
            set { if (filled == value) return; filled = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float unit = Mathf.Min(rect.width / 11f, rect.height / 10f);
            Vector2 origin = rect.center - new Vector2(11f, 10f) * (unit * 0.5f);
            for (int y = 0; y < Pixels.Length; y++)
            for (int x = 0; x < Pixels[y].Length; x++)
            {
                char pixel = Pixels[y][x];
                if (pixel == '.') continue;
                Color shade = pixel == 'o' ? new Color32(62, 33, 43, 255)
                    : !filled ? new Color32(64, 73, 67, 255)
                    : pixel == 'h' ? new Color32(255, 198, 174, 255) : new Color32(241, 92, 115, 255);
                shade *= color;
                float left = origin.x + x * unit;
                float bottom = origin.y + (9 - y) * unit;
                int start = mesh.currentVertCount;
                mesh.AddVert(new Vector3(left, bottom), shade, Vector2.zero);
                mesh.AddVert(new Vector3(left, bottom + unit), shade, Vector2.zero);
                mesh.AddVert(new Vector3(left + unit, bottom + unit), shade, Vector2.zero);
                mesh.AddVert(new Vector3(left + unit, bottom), shade, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
