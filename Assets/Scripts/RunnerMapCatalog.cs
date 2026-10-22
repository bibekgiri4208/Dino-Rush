using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Authored map artwork and framing; gameplay rules are shared by every map.</summary>
    public sealed class RunnerMapCatalog : ScriptableObject
    {
        public const string SelectionKey = "DinoRush.Map";

        [Serializable]
        public sealed class MapLayer
        {
            public Texture2D source;
            public Sprite sprite;
            public float height = 12f;
            public float bottom = -5.625f;
            public float speed;
            public int order;
            public bool stationary;
            public float x;
        }

        [Serializable]
        public sealed class Map
        {
            public string id;
            public string displayName;
            public string description;
            public Sprite preview;
            public Color skyColor;
            public Color soilColor;
            public Color dustTint = Color.white;
            public Color moteColor = new Color(0.85f, 1f, 0.64f);
            public MapLayer[] layers;
        }

        [SerializeField] private Map[] maps = new Map[0];
        public int Count => maps.Length;
        public Map this[int index] => maps[index];

        public int Find(string id)
        {
            for (int i = 0; i < maps.Length; i++)
                if (string.Equals(maps[i].id, id, StringComparison.Ordinal)) return i;
            return 0;
        }
    }
}
