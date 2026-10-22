using UnityEngine;

namespace DinoRush
{
    /// <summary>Deterministic jump movement; no physics layers or Animator setup required.</summary>
    public sealed class RunnerPlayer
    {
        private readonly SpriteRenderer renderer;
        private readonly Sprite[] frames;
        private readonly Transform shadow;
        private float height;
        private float velocity;
        private float jumpBuffer;
        private float animationTime;
        private float squash;

        public bool Grounded => height <= 0f;
        public Rect Hitbox => RunnerRules.PlayerHitbox(height);
        public Vector3 Feet => new Vector3(RunnerRules.PlayerX, RunnerRules.GroundY + height, 0f);

        public RunnerPlayer(SpriteRenderer renderer, Sprite[] frames, Transform shadow)
        {
            this.renderer = renderer;
            this.frames = frames;
            this.shadow = shadow;
            Reset();
        }

        public void Reset()
        {
            height = velocity = jumpBuffer = animationTime = squash = 0f;
            renderer.color = Color.white;
            renderer.transform.rotation = Quaternion.identity;
            Render(0f, false, false, 1f);
        }

        public void QueueJump()
        {
            jumpBuffer = 0.13f;
        }

        public void Step(float dt, out bool jumped, out bool landed)
        {
            jumped = landed = false;
            if (jumpBuffer > 0f && Grounded)
            {
                velocity = RunnerRules.JumpSpeed;
                jumpBuffer = 0f;
                squash = -0.13f;
                jumped = true;
            }

            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
            if (!Grounded || velocity > 0f)
            {
                // Exact integration of constant acceleration, independent of render rate.
                height += velocity * dt - 0.5f * RunnerRules.Gravity * dt * dt;
                velocity -= RunnerRules.Gravity * dt;
                if (height <= 0f)
                {
                    height = velocity = 0f;
                    squash = 0.2f;
                    landed = true;
                }
            }
        }

        public void Render(float dt, bool running, bool dead, float pace)
        {
            animationTime += dt * (running ? 12f * pace : 5f);
            int frame = dead ? 14 : !Grounded ? (velocity > 0f ? 11 : 12)
                : running ? 4 + (int)animationTime % 6 : (int)animationTime % 4;
            renderer.sprite = frames[frame];
            renderer.transform.position = Feet;
            squash = Mathf.MoveTowards(squash, 0f, dt * 1.8f);
            renderer.transform.localScale = new Vector3(2f * (1f + squash), 2f * (1f - squash), 1f);
            float shadowScale = Mathf.Lerp(1f, 0.5f, height / 2.5f);
            shadow.localScale = new Vector3(0.275f * shadowScale, 0.12f * shadowScale, 1f);
        }
    }
}
