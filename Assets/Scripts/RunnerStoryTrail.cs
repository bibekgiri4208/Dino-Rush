using System;
using UnityEngine;

namespace DinoRush
{
    /// <summary>Three optional cyan compass tokens and a non-colliding finish arch, on reserved safe routes.</summary>
    public sealed class RunnerStoryTrail : IDisposable
    {
        public const float TokenHeight = 2.15f;
        private readonly Transform root;
        private readonly SpriteRenderer[] tokens = new SpriteRenderer[3];
        private readonly bool[] collected = new bool[3];
        private readonly float[] tokenDistances = new float[3];
        private readonly SpriteRenderer finish;
        private float totalDistance, age;
        public int Collected { get; private set; }
        public float TotalDistance => totalDistance;
        public float TokenDistance(int index) => tokenDistances[index];
        public RunnerStoryTrail(Transform parent, Material material, RunnerStoryArt art)
        {
            root = new GameObject("Story trail markers").transform; root.SetParent(parent, false);
            for (int i = 0; i < 3; i++)
            {
                tokens[i] = Create("Optional compass " + (i + 1), art.Token, material);
                tokens[i].transform.localScale = Vector3.one * 0.7f;
            }
            finish = Create("Story finish - ancient gate", art.Gate, material);
            finish.transform.localScale = new Vector3(3, 4.5f, 1);
            Hide();
        }
        private SpriteRenderer Create(string name, Sprite sprite, Material material)
        {
            var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(root, false); renderer.sprite = sprite;
            renderer.sharedMaterial = material; renderer.sortingOrder = 14; return renderer;
        }
        public void Reset(RunnerStoryLevel level)
        {
            Collected = 0; age = 0; totalDistance = level.DistanceAt(level.Duration);
            float[] fractions = { 0.23f, 0.49f, 0.75f };
            for (int i = 0; i < 3; i++)
            {
                collected[i] = false; tokenDistances[i] = totalDistance * fractions[i];
                tokens[i].gameObject.SetActive(true);
            }
            Position(0); root.gameObject.SetActive(false);
        }
        public bool AllowsEncounter(RunnerEncounter encounter, float distance, float spawnX, float speed)
        {
            float origin = distance + spawnX - RunnerRules.PlayerX;
            foreach (RunnerObstacleSpawn obstacle in encounter.Obstacles)
                for (int i = 0; i < 3; i++)
                    if (Mathf.Abs(origin + obstacle.Offset - tokenDistances[i]) < speed * 2.2f + obstacle.Width)
                        return false;
            // No last-second hazards straddling the finish line.
            foreach (RunnerObstacleSpawn obstacle in encounter.Obstacles)
                if (origin + obstacle.Offset + obstacle.Width > totalDistance - speed * 2f) return false;
            return true;
        }
        public bool Step(float dt, float distance, Rect playerBox)
        {
            bool changed = false; age += dt; Position(distance);
            for (int i = 0; i < 3; i++)
            {
                if (collected[i]) continue;
                Vector3 point = tokens[i].transform.position;
                var hitbox = new Rect(point.x - 0.3f, point.y - 0.3f, 0.6f, 0.6f);
                if (!hitbox.Overlaps(playerBox)) continue;
                collected[i] = true; tokens[i].gameObject.SetActive(false); Collected++; changed = true;
            }
            return changed;
        }
        private void Position(float distance)
        {
            for (int i = 0; i < 3; i++)
                tokens[i].transform.position = new Vector3(RunnerRules.PlayerX + tokenDistances[i] - distance,
                    RunnerRules.GroundY + TokenHeight + Mathf.Sin(age * 3 + i) * 0.07f, 0);
            finish.transform.position = new Vector3(RunnerRules.PlayerX + totalDistance - distance, RunnerRules.GroundY + 2.1f, 0);
        }
        public void Show() => root.gameObject.SetActive(true);
        public void Hide() => root.gameObject.SetActive(false);
        public void Dispose() { if (root != null) UnityEngine.Object.Destroy(root.gameObject); }
    }
}
