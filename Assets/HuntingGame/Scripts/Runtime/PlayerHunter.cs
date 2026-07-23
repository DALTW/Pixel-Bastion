using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(SimpleSpriteAnimator))]
    public sealed class PlayerHunter : MonoBehaviour
    {
        [SerializeField] private float invulnerabilityDuration = 0.65f;

        private HuntingInput input;
        private Rigidbody2D body;
        private SimpleSpriteAnimator animator;
        private HuntingGameConfig config;
        private float health;
        private float invulnerableUntil;
        private float nextAttackTime;
        private Vector2 movement;
        private Vector2 facingDirection = Vector2.down;
        private bool attacking;

        public HuntingInput Input => input;
        public float Health => health;
        public float MaxHealth => config == null
            ? 100f
            : config.baseMaxHealth + GetUpgradeBonus(HunterUpgradeType.MaxHealth);
        public float MoveSpeed => config == null
            ? 4.5f
            : config.baseMoveSpeed * (1f + GetUpgradeBonus(HunterUpgradeType.MoveSpeed));
        public float SubduePower => config == null
            ? 20f
            : config.baseSubduePower * (1f + GetUpgradeBonus(HunterUpgradeType.SubduePower));
        public bool IsAlive => health > 0f;
        public bool IsAttacking => attacking;
        public Vector2 FacingDirection => facingDirection;

        public void Initialize(HuntingGameConfig gameConfig, HuntingInput gameInput)
        {
            config = gameConfig;
            input = gameInput;
            health = MaxHealth;
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<SimpleSpriteAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            animator.Play(config.playerIdleSprites, 7f);
        }

        private void Update()
        {
            if (input == null || HuntingGameController.Instance == null)
            {
                return;
            }

            movement = HuntingGameController.Instance.IsShopOpen || attacking
                ? Vector2.zero
                : input.Move.normalized;
            if (movement.sqrMagnitude > 0.01f)
            {
                facingDirection = movement;
            }

            UpdateAnimation();
            if (input.AttackPressed)
            {
                TryAttack();
            }
        }

        private void FixedUpdate()
        {
            body.linearVelocity = movement * MoveSpeed;
        }

        public bool TryAttack()
        {
            if (attacking || Time.time < nextAttackTime || !IsAlive ||
                HuntingGameController.Instance == null || HuntingGameController.Instance.IsShopOpen)
            {
                return false;
            }

            StartCoroutine(AttackRoutine());
            return true;
        }

        public int ApplySubdueHit()
        {
            if (config == null)
            {
                return 0;
            }

            var origin = (Vector2)transform.position;
            Physics2D.SyncTransforms();
            var hits = Physics2D.OverlapCircleAll(origin, config.attackRange);
            var affected = new HashSet<AnimalController>();
            foreach (var hit in hits)
            {
                var animal = hit != null ? hit.GetComponentInParent<AnimalController>() : null;
                if (animal == null || animal.IsSubdued || affected.Contains(animal))
                {
                    continue;
                }

                var delta = (Vector2)animal.transform.position - origin;
                if (delta.sqrMagnitude > 0.001f &&
                    Vector2.Angle(facingDirection, delta.normalized) > config.attackArc * 0.5f)
                {
                    continue;
                }

                affected.Add(animal);
                animal.TakeSubdueDamage(SubduePower, origin);
            }

            return affected.Count;
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
            health = MaxHealth;
            invulnerableUntil = Time.time + 1f;
            body.linearVelocity = Vector2.zero;
        }

        public void RefreshUpgrades()
        {
            health = MaxHealth;
        }

        private IEnumerator AttackRoutine()
        {
            attacking = true;
            nextAttackTime = Time.time + config.attackCooldown;
            body.linearVelocity = Vector2.zero;
            animator.Play(config.playerAttackSprites, config.playerAttackSprites.Length /
                                                      Mathf.Max(0.1f, config.attackDuration));
            yield return new WaitForSeconds(config.attackHitDelay);
            var hitCount = ApplySubdueHit();
            if (hitCount > 0)
            {
                HuntingGameController.Instance.Notify($"제압 공격 적중: {hitCount}마리");
            }

            yield return new WaitForSeconds(Mathf.Max(0f, config.attackDuration - config.attackHitDelay));
            attacking = false;
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (attacking)
            {
                animator.Renderer.flipX = facingDirection.x < -0.01f;
                return;
            }

            var moving = movement.sqrMagnitude > 0.01f;
            animator.Renderer.flipX = facingDirection.x < -0.01f;
            animator.Play(moving ? config.playerWalkSprites : config.playerIdleSprites, moving ? 10f : 7f);
        }

        private float GetUpgradeBonus(HunterUpgradeType type)
        {
            var game = HuntingGameController.Instance;
            if (game?.SaveData == null || config == null)
            {
                return 0f;
            }

            var definition = config.FindUpgrade(type);
            return definition == null
                ? 0f
                : game.SaveData.GetUpgradeLevel(type) * definition.bonusPerLevel;
        }
    }
}
