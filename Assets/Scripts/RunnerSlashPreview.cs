using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    [RequireComponent(typeof(Image))]
    public sealed class RunnerSlashPreview : MonoBehaviour
    {
        [SerializeField] private RunnerSlashCatalog catalog;
        [SerializeField] private RunnerSlashKind kind;
        private Image art;
        private float age;
        public void Configure(RunnerSlashCatalog source, RunnerSlashKind style) { catalog = source; kind = style; FaceForward(); }
        private void OnEnable() { age = 0.18f; art = GetComponent<Image>(); FaceForward(); }
        private void FaceForward()
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x); // Match the forward-facing gameplay slash without mirroring other UI.
            transform.localScale = scale;
        }
        private void Update()
        {
            if (catalog == null) return;
            age = Mathf.Repeat(age + Time.unscaledDeltaTime, 1.25f);
            art.sprite = catalog.Frame(kind, age < 0.6f ? age : 0.2f);
            art.color = Color.white;
        }
    }
}
