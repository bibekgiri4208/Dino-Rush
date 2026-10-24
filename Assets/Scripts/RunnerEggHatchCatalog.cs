using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Original, unmodified 24-pixel move/crack/hatch strips, keyed to roster IDs.</summary>
    public sealed class RunnerEggHatchCatalog : ScriptableObject
    {
        public const int CellSize = 24;

        [Serializable]
        public sealed class Egg
        {
            public string dinoId;
            public Texture2D move;
            public Texture2D crack;
            public Texture2D hatch;
            public Color accent = Color.white;
        }

        [SerializeField] private Egg[] eggs = new Egg[0];
        public int Count => eggs.Length;
        public Egg this[int index] => eggs[index];

        public Egg Find(string id)
        {
            foreach (Egg egg in eggs) if (egg.dinoId == id) return egg;
            return null;
        }
    }
}
