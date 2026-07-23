using System;
using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SimpleSpriteAnimator))]
    public sealed class PlayerHunter : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityDuration = 0.65f;

        private HuntingInput input;
        private Rigidbody2D body;
        private SimpleSpriteAnimator animator;
        private HuntingGameConfig config;
        private Sprite[][] idleDirections;
        private Sprite[][] walkDirections;
        private float health;
        private float invulnerableUntil;
        private Vector2 movement;

        public HuntingInput Input => input;
        public WeaponSystem Weapon { get; private set; }
        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool IsAlive => health > 0f;

        public void Initialize(HuntingGameConfig gameConfig, HuntingInput gameInput)
        {
            config = gameConfig;
            input = gameInput;
            health = maxHealth;
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<SimpleSpriteAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BuildDirectionalFrames();

            var weaponObject = new GameObject("Weapon");
            weaponObject.transform.SetParent(transform, false);
            Weapon = weaponObject.AddComponent<WeaponSystem>();
            Weapon.Initialize(this);
        }

        private void Update()
        {
            if (input == null || HuntingGameController.Instance == null)
            {
                return;
            }

            movement = HuntingGameController.Instance.IsShopOpen ? Vector2.zero : input.Move.normalized;
            UpdateAnimation();

            if (input.ReloadPressed)
            {
                Weapon.TryReload();
            }

            for (var slot = 1; slot <= 4; slot++)
            {
                if (input.WeaponSlotPressed(slot))
                {
                    Weapon.TryEquipSlot(slot - 1);
                }
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = movement * moveSpeed;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || Time.time < invulnerableUntil || HuntingGameController.Instance.IsInsideCamp(transform.position))
            {
                return;
            }

            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
            invulnerableUntil = Time.time + invulnerabilityDuration;
            HuntingGameController.Instance.Hud.FlashDamage();
            if (health <= 0f)
            {
                HuntingGameController.Instance.HandlePlayerDeath();
            }
        }

        public void Respawn(Vector2 position)
        {
            transform.position = position;
            health = maxHealth;
            invulnerableUntil = Time.time + 1f;
            body.linearVelocity = Vector2.zero;
        }

        private void BuildDirectionalFrames()
        {
            idleDirections = SplitDirections(config.playerIdleSprites, 4);
            walkDirections = SplitDirections(config.playerWalkSprites, 6);
            animator.Play(idleDirections[0], 5f);
        }

        private static Sprite[][] SplitDirections(Sprite[] source, int framesPerDirection)
        {
            var result = new Sprite[3][];
            source ??= Array.Empty<Sprite>();
            for (var direction = 0; direction < result.Length; direction++)
            {
                var start = direction * framesPerDirection;
                var count = Mathf.Min(framesPerDirection, Mathf.Max(0, source.Length - start));
                if (count <= 0)
                {
                    result[direction] = source;
                    continue;
                }

                result[direction] = new Sprite[count];
                Array.Copy(source, start, result[direction], 0, count);
            }

            return result;
        }

        private void UpdateAnimation()
        {
            var moving = movement.sqrMagnitude > 0.01f;
            var direction = 0;
            if (Mathf.Abs(movement.y) > Mathf.Abs(movement.x))
            {
                direction = movement.y > 0f ? 1 : 0;
            }
            else if (Mathf.Abs(movement.x) > 0.01f)
            {
                direction = 2;
            }

            animator.Renderer.flipX = direction == 2 && movement.x < 0f;
            animator.Play(moving ? walkDirections[direction] : idleDirections[direction], moving ? 9f : 5f);
        }
    }
}
