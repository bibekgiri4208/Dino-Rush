using UnityEngine;

namespace DinoRush
{
    /// <summary>Deterministic double-jump, crouch and fast-fall movement without physics or an Animator.</summary>
    public sealed class RunnerPlayer
    {
        private readonly SpriteRenderer renderer;
        private Sprite[] frames;
        private int renderedFrame;
        private readonly Transform shadow;
        private float height;
        private float velocity;
        private float jumpBuffer;
        private int jumpsUsed;
        private float animationTime;
        private float squash;
        private float hurtTime;
        public const float HurtDuration = 0.64f;
        private bool duckHeld;
        private bool fastFalling;

        public bool Grounded => height <= 0f;
        public int JumpsRemaining => RunnerRules.MaxJumps - jumpsUsed;
        public bool IsDucking => duckHeld || fastFalling;
        public Rect Hitbox => RunnerRules.PlayerHitbox(height, IsDucking);
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
            height = velocity = jumpBuffer = animationTime = squash = hurtTime = 0f;
            jumpsUsed = 0;
            duckHeld = fastFalling = false;
            renderer.color = Color.white;
            renderer.transform.rotation = Quaternion.identity;
            Render(0f, false, false, 1f);
        }

        public void QueueJump()
        {
            if (IsDucking) return;
            jumpBuffer = 0.13f;
        }

        public void Hurt()
        {
            hurtTime = HurtDuration;
        }

        public void SetFrames(Sprite[] replacement)
        {
            if (replacement == null || replacement.Length != 24)
                throw new System.ArgumentException("A dino needs all 24 animation frames.", nameof(replacement));
            foreach (Sprite frame in replacement)
                if (frame == null) throw new System.ArgumentException("Dino frames cannot be missing.", nameof(replacement));
            frames = replacement;
            renderer.sprite = frames[renderedFrame]; // Preserve movement, pose, scale and animation timing.
        }

        public void SetDucking(bool held)
        {
            duckHeld = held;
            if (!held) return;
            jumpBuffer = 0f; // Duck wins over a queued jump; never bounce on a fast landing.
            if (!Grounded)
            {
                fastFalling = true; // A quick tap commits the drop even after release.
                velocity = Mathf.Min(velocity, -RunnerRules.FastFallSpeed);
            }
        }

        public void Step(float dt, out bool jumped, out bool landed)
        {
            jumped = landed = false;
            if (jumpBuffer > 0f && jumpsUsed < RunnerRules.MaxJumps && !IsDucking)
            {
                velocity = RunnerRules.JumpSpeed;
                jumpsUsed++;
                jumpBuffer = 0f;
                squash = -0.13f;
                jumped = true;
            }

            jumpBuffer = Mathf.Max(0f, jumpBuffer - dt);
            if (!Grounded || velocity > 0f)
            {
                // Exact integration of constant acceleration, independent of render rate.
                float gravity = fastFalling ? RunnerRules.FastFallGravity : RunnerRules.Gravity;
                height += velocity * dt - 0.5f * gravity * dt * dt;
                velocity -= gravity * dt;
                if (height <= 0f)
                {
                    height = velocity = 0f;
                    jumpsUsed = 0;
                    fastFalling = false;
                    squash = 0.2f;
                    landed = true;
                }
            }
        }

        public void Render(float dt, bool running, bool dead, float pace)
        {
            animationTime += dt * (running ? 12f * pace : 5f);
            int frame = dead ? 14 : IsDucking ? 18 + (int)animationTime % 6
                : !Grounded ? (velocity > 0f ? 11 : 12)
                : running ? 4 + (int)animationTime % 6 : (int)animationTime % 4;
            // The supplied sheets contain a crying pose (14) and its white hit silhouette (15).
            if (hurtTime > 0f)
                frame = (int)((HurtDuration - hurtTime) / 0.08f) % 2 == 0 ? 15 : 14;
            renderer.sprite = frames[frame];
            renderedFrame = frame;
            hurtTime = Mathf.Max(0f, hurtTime - dt);
            renderer.transform.position = Feet;
            squash = Mathf.MoveTowards(squash, 0f, dt * 1.8f);
            renderer.transform.localScale = new Vector3(2f * (1f + squash), 2f * (1f - squash), 1f);
            float shadowScale = Mathf.Lerp(1f, 0.5f, height / 2.5f);
            shadow.localScale = new Vector3(0.275f * shadowScale, 0.12f * shadowScale, 1f);
        }
    }
}
