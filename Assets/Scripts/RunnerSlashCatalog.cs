using UnityEngine;

namespace DinoRush
{
    public sealed class RunnerSlashCatalog : ScriptableObject
    {
        [SerializeField] private Sprite[] fire, lightning, water;
        public Sprite[] Frames(RunnerSlashKind kind) => kind == RunnerSlashKind.Fire ? fire : kind == RunnerSlashKind.Lightning ? lightning : kind == RunnerSlashKind.Water ? water : null;
        public Sprite Frame(RunnerSlashKind kind, float age)
        {
            Sprite[] frames = Frames(kind);
            return frames == null || frames.Length == 0 ? null : frames[Mathf.Clamp((int)(age * 20), 0, frames.Length - 1)];
        }
    }
}
