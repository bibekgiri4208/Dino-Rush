using System.Collections.Generic;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Editor-only scenery layout. Changes reach gameplay only when explicitly saved to the catalog.</summary>
    public sealed class RunnerMapLayout : MonoBehaviour
    {
        [SerializeField] private RunnerMapCatalog catalog;
        [SerializeField] private string mapId;
        [SerializeField] private List<RunnerMapLayerLayout> layers = new List<RunnerMapLayerLayout>();

        public RunnerMapCatalog Catalog => catalog;
        public string MapId => mapId;
        public IReadOnlyList<RunnerMapLayerLayout> Layers => layers;
        public RunnerMapCatalog.Map Map => catalog[catalog.Find(mapId)];

        public void Initialize(RunnerMapCatalog source, int index)
        {
            catalog = source;
            mapId = source[index].id;
        }

        public void AddLayer(RunnerMapLayerLayout layer) => layers.Add(layer);
    }
}
