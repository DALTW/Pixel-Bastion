using System;
using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SimpleSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private float framesPerSecond = 8f;
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames = Array.Empty<Sprite>();
        private float timer;
        private int frameIndex;

        public SpriteRenderer Renderer => spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            timer += Time.deltaTime;
            var frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                frameIndex = (frameIndex + 1) % frames.Length;
                spriteRenderer.sprite = frames[frameIndex];
            }
        }

        public void Play(Sprite[] newFrames, float fps = 8f)
        {
            newFrames ??= Array.Empty<Sprite>();
            if (ReferenceEquals(frames, newFrames))
            {
                return;
            }

            frames = newFrames;
            framesPerSecond = Mathf.Max(1f, fps);
            frameIndex = 0;
            timer = 0f;
            if (frames.Length > 0)
            {
                spriteRenderer.sprite = frames[0];
            }
        }
    }
}
