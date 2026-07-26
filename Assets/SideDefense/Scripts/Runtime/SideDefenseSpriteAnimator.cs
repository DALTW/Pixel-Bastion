using System;
using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SideDefenseSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float framesPerSecond = 8f;
        [SerializeField] private Sprite[] attackFrames = Array.Empty<Sprite>();
        [SerializeField, Min(0.1f)] private float attackFramesPerSecond = 10f;
        [SerializeField] private PlaybackState playbackState =
            PlaybackState.Walking;

        private SpriteRenderer spriteRenderer;
        private float elapsed;

        public bool IsPlayingAttack =>
            playbackState == PlaybackState.Attacking;
        public int AttackFrameCount =>
            attackFrames == null ? 0 : attackFrames.Length;

        public void Configure(Sprite[] animationFrames, float animationSpeed)
        {
            frames = animationFrames ?? Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(0.1f, animationSpeed);
            attackFrames = Array.Empty<Sprite>();
            playbackState = PlaybackState.Walking;
            ApplyFrame(frames, 0);
        }

        public void ConfigureCombatAnimations(
            Sprite[] walkAnimationFrames,
            float walkAnimationSpeed,
            Sprite[] attackAnimationFrames,
            float attackAnimationSpeed)
        {
            frames = walkAnimationFrames ?? Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(0.1f, walkAnimationSpeed);
            attackFrames = attackAnimationFrames ?? Array.Empty<Sprite>();
            attackFramesPerSecond = Mathf.Max(0.1f, attackAnimationSpeed);
            playbackState = PlaybackState.Idle;
            elapsed = 0f;
            ApplyFrame(frames, 0);
        }

        public void SetWalking(bool shouldWalk)
        {
            if (playbackState == PlaybackState.Attacking)
            {
                return;
            }

            PlaybackState desiredState = shouldWalk
                ? PlaybackState.Walking
                : PlaybackState.Idle;
            if (playbackState == desiredState)
            {
                return;
            }

            playbackState = desiredState;
            elapsed = 0f;
            ApplyFrame(frames, 0);
        }

        public void PlayAttack()
        {
            if (attackFrames == null || attackFrames.Length == 0)
            {
                return;
            }

            playbackState = PlaybackState.Attacking;
            elapsed = 0f;
            ApplyFrame(attackFrames, 0);
        }

        public void Stop()
        {
            playbackState = PlaybackState.Idle;
            elapsed = 0f;
            ApplyFrame(frames, 0);
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            elapsed = 0f;
            if (playbackState == PlaybackState.Attacking)
            {
                playbackState = PlaybackState.Idle;
            }

            ApplyFrame(frames, 0);
        }

        private void Update()
        {
            if (playbackState == PlaybackState.Idle)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (playbackState == PlaybackState.Walking)
            {
                if (frames == null || frames.Length < 2)
                {
                    return;
                }

                int walkFrameIndex =
                    Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length;
                ApplyFrame(frames, walkFrameIndex);
                return;
            }

            if (attackFrames == null || attackFrames.Length == 0)
            {
                Stop();
                return;
            }

            int attackFrameIndex =
                Mathf.FloorToInt(elapsed * attackFramesPerSecond);
            if (attackFrameIndex >= attackFrames.Length)
            {
                Stop();
                return;
            }

            ApplyFrame(attackFrames, attackFrameIndex);
        }

        private void ApplyFrame(Sprite[] animationFrames, int frameIndex)
        {
            if (animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = animationFrames[
                Mathf.Clamp(frameIndex, 0, animationFrames.Length - 1)];
        }

        private enum PlaybackState
        {
            Idle,
            Walking,
            Attacking
        }
    }
}
