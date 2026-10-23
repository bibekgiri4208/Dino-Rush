using UnityEngine;

namespace DinoRush
{
    /// <summary>Selecting any of a layer's tiles selects the entire editable layer.</summary>
    [SelectionBase]
    public sealed class RunnerMapLayerLayout : MonoBehaviour
    {
        [SerializeField] private RunnerMapLayout layout;
        [SerializeField] private int layerIndex;

        public RunnerMapLayout Layout => layout;
        public RunnerMapCatalog.MapLayer Spec => layout.Map.layers[layerIndex];

        public void Initialize(RunnerMapLayout owner, int index)
        {
            layout = owner;
            layerIndex = index;
            ResetFraming();
        }

        public void ResetFraming()
        {
            transform.localPosition = new Vector3(Spec.stationary ? Spec.x : 0f, Spec.bottom, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = new Vector3(Spec.height, Spec.height, 1f);
        }
    }

}
