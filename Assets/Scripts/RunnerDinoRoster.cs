using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>References the supplied art without changing its existing sprite slices.</summary>
    public sealed class RunnerDinoRoster : ScriptableObject
    {
        public const string SelectionKey = "DinoRush.Dino";

        [Serializable]
        public sealed class Dino
        {
            [SerializeField] private string id;
            [SerializeField] private string displayName;
            [SerializeField] private Texture2D sheet;
            [SerializeField] private Sprite portrait;

            public string Id => id;
            public string DisplayName => displayName;
            public Texture2D Sheet => sheet;
            public Sprite Portrait => portrait;
        }

        [SerializeField] private Dino[] dinos = new Dino[0];
        public int Count => dinos.Length;
        public Dino this[int index] => dinos[index];

        public int Find(string id)
        {
            for (int i = 0; i < dinos.Length; i++)
                if (string.Equals(dinos[i].Id, id, StringComparison.Ordinal)) return i;
            return 0; // Missing or stale preferences safely use the default blue dino.
        }
    }
}
