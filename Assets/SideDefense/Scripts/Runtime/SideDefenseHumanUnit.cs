using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseHumanUnit : MonoBehaviour
    {
        [SerializeField] private string displayName = "Human";
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;
        [SerializeField, Min(0f)] private float movementSpeed = 1.2f;
        [SerializeField, Min(0f)] private float attackDamage = 18f;
        [SerializeField, Min(0.1f)] private float attackInterval = 0.9f;
        [SerializeField, Min(0.1f)] private float attackRange = 0.75f;
        [SerializeField, Min(0.1f)] private float detectionRange = 2.2f;
        [SerializeField] private bool healsAllies;
        [SerializeField, Min(0f)] private float healAmount = 20f;
        [SerializeField, Min(0.1f)] private float healRange = 2.8f;
        [SerializeField] private SideDefenseAttackStyle attackStyle =
            SideDefenseAttackStyle.Melee;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 6f;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private bool isMarching;
        [SerializeField] private float stopWorldX;
        [SerializeField] private SideDefenseHealthBar healthBar;

        private static readonly List<SideDefenseHumanUnit> ActiveHumanUnits =
            new List<SideDefenseHumanUnit>();

        private float nextActionTime;
        private int appliedUpgradeLevel;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized =>
            maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsMarching => isMarching;
        public bool IsAlive => currentHealth > 0f;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float HealAmount => healsAllies ? healAmount : 0f;
        public int UpgradeLevel => appliedUpgradeLevel;
        public SideDefenseAttackStyle AttackStyle => attackStyle;
        public static IReadOnlyList<SideDefenseHumanUnit> ActiveUnits =>
            ActiveHumanUnits;

        public event Action<SideDefenseHumanUnit> HealthChanged;
        public event Action<SideDefenseHumanUnit> Died;

        public void Configure(string unitName, float health, float moveSpeed)
        {
            Configure(unitName, health, moveSpeed, 18f, 0.9f, 0.75f);
        }

        public void Configure(
            string unitName,
            float health,
            float moveSpeed,
            float damage,
            float secondsBetweenAttacks,
            float combatRange)
        {
            displayName = string.IsNullOrWhiteSpace(unitName) ? "Human" : unitName;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            movementSpeed = Mathf.Max(0f, moveSpeed);
            attackDamage = Mathf.Max(0f, damage);
            attackInterval = Mathf.Max(0.1f, secondsBetweenAttacks);
            attackRange = Mathf.Max(0.1f, combatRange);
            detectionRange = Mathf.Max(attackRange + 0.5f, 2.2f);
            isMarching = false;
            appliedUpgradeLevel = 0;
            RefreshHealthBar();
        }

        public void BindHealthBar(SideDefenseHealthBar bar)
        {
            healthBar = bar;
            RefreshHealthBar();
        }

        public void ConfigureAttackPresentation(
            SideDefenseAttackStyle style,
            GameObject rangedProjectilePrefab,
            float rangedProjectileSpeed,
            Transform rangedProjectileOrigin)
        {
            attackStyle = style;
            projectilePrefab = rangedProjectilePrefab;
            projectileSpeed = Mathf.Max(0.1f, rangedProjectileSpeed);
            projectileOrigin = rangedProjectileOrigin;
        }

        public void ConfigureHealing(float amount, float range)
        {
            healsAllies = amount > 0f;
            healAmount = Mathf.Max(0f, amount);
            healRange = Mathf.Max(0.1f, range);
        }

        public void ApplyUpgradeLevel(
            int level,
            float healthBonusPerLevel,
            float powerBonusPerLevel)
        {
            int targetLevel = Mathf.Max(0, level);
            if (!IsAlive || targetLevel <= appliedUpgradeLevel)
            {
                return;
            }

            float safeHealthBonus = Mathf.Max(0f, healthBonusPerLevel);
            float safePowerBonus = Mathf.Max(0f, powerBonusPerLevel);
            float oldHealthMultiplier =
                1f + appliedUpgradeLevel * safeHealthBonus;
            float newHealthMultiplier =
                1f + targetLevel * safeHealthBonus;
            float oldPowerMultiplier =
                1f + appliedUpgradeLevel * safePowerBonus;
            float newPowerMultiplier =
                1f + targetLevel * safePowerBonus;

            float previousMaxHealth = maxHealth;
            maxHealth = Mathf.Max(
                1f,
                maxHealth *
                newHealthMultiplier /
                Mathf.Max(0.01f, oldHealthMultiplier));
            currentHealth = Mathf.Min(
                maxHealth,
                currentHealth + maxHealth - previousMaxHealth);
            attackDamage = Mathf.Max(
                0f,
                attackDamage *
                newPowerMultiplier /
                Mathf.Max(0.01f, oldPowerMultiplier));
            if (healsAllies)
            {
                healAmount = Mathf.Max(
                    0f,
                    healAmount *
                    newPowerMultiplier /
                    Mathf.Max(0.01f, oldPowerMultiplier));
            }

            appliedUpgradeLevel = targetLevel;
            RefreshHealthBar();
            HealthChanged?.Invoke(this);
        }

        public void BeginMarch(float destinationWorldX)
        {
            stopWorldX = Mathf.Max(transform.position.x, destinationWorldX);
            isMarching = movementSpeed > 0f &&
                         transform.position.x < stopWorldX;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.flipX = false;
            }

            SideDefenseSpriteAnimator spriteAnimator =
                GetComponent<SideDefenseSpriteAnimator>();
            if (spriteAnimator != null)
            {
                spriteAnimator.SetWalking(isMarching);
            }
        }

        public void StopMarch()
        {
            isMarching = false;
            SideDefenseSpriteAnimator spriteAnimator =
                GetComponent<SideDefenseSpriteAnimator>();
            if (spriteAnimator != null)
            {
                spriteAnimator.Stop();
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            SideDefenseHumanUnit healingTarget = FindHealingTarget();
            if (healingTarget != null)
            {
                SetWalkingAnimation(false);
                TryHeal(healingTarget);
                return;
            }

            SideDefenseMonsterUnit target = FindNearestMonster();
            if (target != null &&
                Mathf.Abs(target.transform.position.x - transform.position.x) <=
                attackRange)
            {
                SetWalkingAnimation(false);
                TryAttack(target);
                return;
            }

            if (!isMarching)
            {
                SetWalkingAnimation(false);
                return;
            }

            SetWalkingAnimation(true);
            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(
                position.x,
                stopWorldX,
                movementSpeed * Time.deltaTime);
            transform.position = position;

            if (Mathf.Approximately(position.x, stopWorldX))
            {
                StopMarch();
            }
        }

        private SideDefenseHumanUnit FindHealingTarget()
        {
            if (!healsAllies || healAmount <= 0f)
            {
                return null;
            }

            SideDefenseHumanUnit bestTarget = null;
            float lowestHealthNormalized = 1f;
            float nearestDistance = float.MaxValue;

            for (int index = 0; index < ActiveHumanUnits.Count; index++)
            {
                SideDefenseHumanUnit ally = ActiveHumanUnits[index];
                if (ally == null ||
                    !ally.IsAlive ||
                    ally.CurrentHealth >= ally.MaxHealth)
                {
                    continue;
                }

                float distance = Mathf.Abs(
                    ally.transform.position.x - transform.position.x);
                if (distance > healRange)
                {
                    continue;
                }

                float healthNormalized = ally.HealthNormalized;
                if (healthNormalized < lowestHealthNormalized ||
                    Mathf.Approximately(
                        healthNormalized,
                        lowestHealthNormalized) &&
                    distance < nearestDistance)
                {
                    bestTarget = ally;
                    lowestHealthNormalized = healthNormalized;
                    nearestDistance = distance;
                }
            }

            return bestTarget;
        }

        private SideDefenseMonsterUnit FindNearestMonster()
        {
            SideDefenseMonsterUnit nearest = null;
            float nearestDistance = detectionRange;
            IReadOnlyList<SideDefenseMonsterUnit> monsters =
                SideDefenseMonsterUnit.ActiveUnits;

            for (int index = 0; index < monsters.Count; index++)
            {
                SideDefenseMonsterUnit monster = monsters[index];
                if (monster == null || !monster.IsAlive)
                {
                    continue;
                }

                float distance = Mathf.Abs(
                    monster.transform.position.x - transform.position.x);
                if (distance <= nearestDistance)
                {
                    nearest = monster;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void TryAttack(SideDefenseMonsterUnit target)
        {
            if (target == null ||
                !target.IsAlive ||
                attackDamage <= 0f ||
                Time.time < nextActionTime)
            {
                return;
            }

            nextActionTime = Time.time + attackInterval;
            PlayAttackMotion();

            if (attackStyle == SideDefenseAttackStyle.Ranged &&
                TryLaunchProjectile(target))
            {
                return;
            }

            target.TakeDamageFromHuman(attackDamage);
        }

        private void TryHeal(SideDefenseHumanUnit target)
        {
            if (target == null ||
                !target.IsAlive ||
                target.CurrentHealth >= target.MaxHealth ||
                healAmount <= 0f ||
                Time.time < nextActionTime)
            {
                return;
            }

            nextActionTime = Time.time + attackInterval;
            PlayAttackMotion();

            if (attackStyle == SideDefenseAttackStyle.Ranged &&
                TryLaunchHealingProjectile(target))
            {
                return;
            }

            target.Heal(healAmount);
        }

        private void SetWalkingAnimation(bool shouldAnimate)
        {
            SideDefenseSpriteAnimator spriteAnimator =
                GetComponent<SideDefenseSpriteAnimator>();
            if (spriteAnimator != null)
            {
                spriteAnimator.SetWalking(shouldAnimate);
            }
        }

        private void PlayAttackMotion()
        {
            SideDefenseSpriteAnimator spriteAnimator =
                GetComponent<SideDefenseSpriteAnimator>();
            spriteAnimator?.PlayAttack();
        }

        private bool TryLaunchProjectile(SideDefenseMonsterUnit target)
        {
            if (projectilePrefab == null || target == null)
            {
                return false;
            }

            Vector3 origin = projectileOrigin == null
                ? transform.position + Vector3.up * 0.18f
                : projectileOrigin.position;
            GameObject projectileObject = Instantiate(
                projectilePrefab,
                origin,
                Quaternion.identity);
            SideDefenseProjectile projectile =
                projectileObject.GetComponent<SideDefenseProjectile>();
            if (projectile == null)
            {
                Destroy(projectileObject);
                return false;
            }

            projectile.LaunchFromHuman(
                target,
                attackDamage,
                projectileSpeed);
            return true;
        }

        private bool TryLaunchHealingProjectile(SideDefenseHumanUnit target)
        {
            if (projectilePrefab == null || target == null)
            {
                return false;
            }

            Vector3 origin = projectileOrigin == null
                ? transform.position + Vector3.up * 0.18f
                : projectileOrigin.position;
            GameObject projectileObject = Instantiate(
                projectilePrefab,
                origin,
                Quaternion.identity);
            SideDefenseProjectile projectile =
                projectileObject.GetComponent<SideDefenseProjectile>();
            if (projectile == null)
            {
                Destroy(projectileObject);
                return false;
            }

            projectile.LaunchHealing(
                target,
                healAmount,
                projectileSpeed);
            return true;
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f || currentHealth <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            RefreshHealthBar();
            HealthChanged?.Invoke(this);

            if (currentHealth <= 0f)
            {
                StopMarch();
                Collider2D unitCollider = GetComponent<Collider2D>();
                if (unitCollider != null)
                {
                    unitCollider.enabled = false;
                }

                Died?.Invoke(this);
                if (Application.isPlaying)
                {
                    Destroy(gameObject, 0.35f);
                }
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            RefreshHealthBar();
            HealthChanged?.Invoke(this);
        }

        private void Awake()
        {
            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<SideDefenseHealthBar>(true);
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            RefreshHealthBar();
        }

        private void OnEnable()
        {
            if (!ActiveHumanUnits.Contains(this))
            {
                ActiveHumanUnits.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveHumanUnits.Remove(this);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackInterval = Mathf.Max(0.1f, attackInterval);
            attackRange = Mathf.Max(0.1f, attackRange);
            detectionRange = Mathf.Max(attackRange + 0.5f, detectionRange);
            healAmount = Mathf.Max(0f, healAmount);
            healRange = Mathf.Max(0.1f, healRange);
            projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
            RefreshHealthBar();
        }

        private void RefreshHealthBar()
        {
            if (healthBar != null)
            {
                healthBar.SetNormalized(HealthNormalized);
            }
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveUnits()
        {
            ActiveHumanUnits.Clear();
        }
    }
}
