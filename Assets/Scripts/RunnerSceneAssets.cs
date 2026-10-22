using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Persistent sprite frames and tiny generated effect textures used by ForestRun.</summary>
    public sealed class RunnerSceneAssets : ScriptableObject
    {
        [SerializeField] private Object[] objects;

        public void SetAssets(IReadOnlyList<Object> assets)
        {
            objects = new Object[assets.Count];
            for (int i = 0; i < assets.Count; i++) objects[i] = assets[i];
        }
    }
}
