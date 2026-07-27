using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseMonsterUnit : MonoBehaviour
    {
        [SerializeField] private string displayName = "Monster";
        [SerializeField, Min(1f)] private float maxHealth = 80f;
        [SerializeField, Min(0f)] private float currentHealth = 80f;
        [SerializeField, Min(0f)] private float movementSpeed = 0.8f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0.1f)] private float attackInterval = 1.1f;
        [SerializeField, Min(0.1f)] private float attackRange = 0.75f;
        [SerializeField, Min(0.1f)] private float detectionRange = 2.4f;
        [SerializeField, Min(0.1f)] private float towerAttackRange = 1.85f;
        [SerializeField] private SideDefenseAttackStyle attackStyle =
            SideDefenseAttackStyle.Melee;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 6f;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField, Min(1)] private int monsterLevel = 1;
        [SerializeField, Range(0.1f, 1f)]
        private float towerDamageMultiplier = 1f;
        [SerializeField, Min(0)] private int coinReward = 10;
        [SerializeField, Min(0f)]
        private float coinRewardIncreasePerLevel = 0.1f;
        [SerializeField] private SideDefenseHealthBar healthBar;
        [SerializeField] private SideDefenseTower targetTower;

        private static readonly List<SideDefenseMonsterUnit> ActiveMonsterUnits =
            new List<SideDefenseMonsterUnit>();

        private float nextAttackTime;
        private bool wasDefeatedByHuman;

        public string DisplayName => displayName;
        public int Level => monsterLevel;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized =>
            maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsAlive => currentHealth > 0f;
        public float AttackRange => attackRange;
        public float TowerAttackRange => towerAttackRange;
        public SideDefenseAttackStyle AttackStyle => attackStyle;
        public int CoinReward => CalculateCoinReward(
            coinReward,
            monsterLevel,
            coinRewardIncreasePerLevel);
        public bool WasDefeatedByHuman => wasDefeatedByHuman;
        public static IReadOnlyList<SideDefenseMonsterUnit> ActiveUnits =>
            ActiveMonsterUnits;

        public event Action<SideDefenseMonsterUnit> HealthChanged;
        public event Action<SideDefenseMonsterUnit> Died;

        public void Configure(
            string unitName,
            float health,
            float moveSpeed,
            float damage,
            float secondsBetweenAttacks,
            float combatRange,
            int reward)
        {
            displayName = string.IsNullOrWhiteSpace(unitName)
                ? "Monster"
                : unitName;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            movementSpeed = Mathf.Max(0f, moveSpeed);
            attackDamage = Mathf.Max(0f, damage);
            attackInterval = Mathf.Max(0.1f, secondsBetweenAttacks);
            attackRange = Mathf.Max(0.1f, combatRange);
            detectionRange = Mathf.Max(attackRange + 0.5f, 2.4f);
            monsterLevel = 1;
            towerDamageMultiplier = 1f;
            coinReward = Mathf.Max(0, reward);
            wasDefeatedByHuman = false;
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
            if (attackStyle == SideDefenseAttackStyle.Ranged)
            {
                towerAttackRange = Mathf.Max(towerAttackRange, attackRange);
            }
        }

        public void ApplyDifficulty(
            float healthMultiplier,
            float damageMultiplier,
            float speedMultiplier,
            int level,
            float rewardIncreasePerLevel,
            float damageAgainstTowerMultiplier)
        {
            monsterLevel = Mathf.Max(1, level);
            towerDamageMultiplier =
                Mathf.Clamp(damageAgainstTowerMultiplier, 0.1f, 1f);
            coinRewardIncreasePerLevel =
                Mathf.Max(0f, rewardIncreasePerLevel);
            maxHealth = Mathf.Max(
                1f,
                maxHealth * Mathf.Max(1f, healthMultiplier));
            currentHealth = maxHealth;
            attackDamage = Mathf.Max(
                0f,
                attackDamage * Mathf.Max(1f, damageMultiplier));
            movementSpeed = Mathf.Max(
                0f,
                movementSpeed * Mathf.Max(1f, speedMultiplier));
            RefreshHealthBar();
        }

        public void BeginMarch(SideDefenseTower tower)
        {
            targetTower = tower;
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.flipX = true;
            }

            SetWalkingAnimation(IsAlive);
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            SideDefenseHumanUnit humanTarget = FindNearestHuman();
            if (humanTarget != null &&
                Mathf.Abs(
                    humanTarget.transform.position.x - transform.position.x) <=
                attackRange)
            {
                SetWalkingAnimation(false);
                TryAttack(humanTarget);
                return;
            }

            if (targetTower != null && !targetTower.IsDestroyed)
            {
                float distanceToTower =
                    transform.position.x - targetTower.transform.position.x;
                if (distanceToTower <= towerAttackRange)
                {
                    SetWalkingAnimation(false);
                    TryAttack(targetTower);
                    return;
                }
            }

            SetWalkingAnimation(true);
            MoveLeft();
        }

        private SideDefenseHumanUnit FindNearestHuman()
        {
            SideDefenseHumanUnit nearest = null;
            float nearestDistance = detectionRange;
            IReadOnlyList<SideDefenseHumanUnit> humans =
                SideDefenseHumanUnit.ActiveUnits;

            for (int index = 0; index < humans.Count; index++)
            {
                SideDefenseHumanUnit human = humans[index];
                if (human == null || !human.IsAlive)
                {
                    continue;
                }

                float distance = Mathf.Abs(
                    human.transform.position.x - transform.position.x);
                if (distance <= nearestDistance)
                {
                    nearest = human;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void MoveLeft()
        {
            float destinationX = targetTower == null
                ? transform.position.x - 100f
                : targetTower.transform.position.x + towerAttackRange;

            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(
                position.x,
                destinationX,
                movementSpeed * Time.deltaTime);
            transform.position = position;
        }

        private void TryAttack(SideDefenseHumanUnit target)
        {
            if (target == null ||
                !target.IsAlive ||
                attackDamage <= 0f ||
                Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackInterval;
            PlayAttackMotion();

            if (attackStyle == SideDefenseAttackStyle.Ranged &&
                TryLaunchProjectile(target))
            {
                return;
            }

            target.TakeDamage(attackDamage);
        }

        private void TryAttack(SideDefenseTower tower)
        {
            if (tower == null ||
                tower.IsDestroyed ||
                attackDamage <= 0f ||
                Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + attackInterval;
            PlayAttackMotion();

            if (attackStyle == SideDefenseAttackStyle.Ranged &&
                TryLaunchProjectile(tower))
            {
                return;
            }

            tower.TakeDamage(attackDamage * towerDamageMultiplier);
        }

        public void TakeDamage(float damage)
        {
            ApplyDamage(damage, false);
        }

        public void TakeDamageFromHuman(float damage)
        {
            ApplyDamage(damage, true);
        }

        private void ApplyDamage(float damage, bool causedByHuman)
        {
            if (damage <= 0f || !IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            RefreshHealthBar();
            HealthChanged?.Invoke(this);

            if (!IsAlive)
            {
                wasDefeatedByHuman = causedByHuman;
                SideDefenseSpriteAnimator spriteAnimator =
                    GetComponent<SideDefenseSpriteAnimator>();
                spriteAnimator?.Stop();
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
            if (!ActiveMonsterUnits.Contains(this))
            {
                ActiveMonsterUnits.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveMonsterUnits.Remove(this);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            movementSpeed = Mathf.Max(0f, movementSpeed);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackInterval = Mathf.Max(0.1f, attackInterval);
            attackRange = Mathf.Max(0.1f, attackRange);
            detectionRange = Mathf.Max(attackRange + 0.5f, detectionRange);
            towerAttackRange = Mathf.Max(0.1f, towerAttackRange);
            projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
            monsterLevel = Mathf.Max(1, monsterLevel);
            towerDamageMultiplier =
                Mathf.Clamp(towerDamageMultiplier, 0.1f, 1f);
            coinReward = Mathf.Max(0, coinReward);
            coinRewardIncreasePerLevel =
                Mathf.Max(0f, coinRewardIncreasePerLevel);
            RefreshHealthBar();
        }

        public static int CalculateCoinReward(int baseReward, int level)
        {
            return CalculateCoinReward(baseReward, level, 0.1f);
        }

        public static int CalculateCoinReward(
            int baseReward,
            int level,
            float rewardIncreasePerLevel)
        {
            int safeReward = Mathf.Max(0, baseReward);
            int safeLevel = Mathf.Max(1, level);
            float levelMultiplier =
                1f +
                (safeLevel - 1) *
                Mathf.Max(0f, rewardIncreasePerLevel);
            return Mathf.Max(
                safeReward,
                Mathf.CeilToInt(safeReward * levelMultiplier));
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

        private bool TryLaunchProjectile(SideDefenseHumanUnit target)
        {
            if (projectilePrefab == null || target == null)
            {
                return false;
            }

            SideDefenseProjectile projectile = CreateProjectile();
            if (projectile == null)
            {
                return false;
            }

            projectile.Launch(target, attackDamage, projectileSpeed);
            return true;
        }

        private bool TryLaunchProjectile(SideDefenseTower target)
        {
            if (projectilePrefab == null || target == null)
            {
                return false;
            }

            SideDefenseProjectile projectile = CreateProjectile();
            if (projectile == null)
            {
                return false;
            }

            projectile.Launch(
                target,
                attackDamage * towerDamageMultiplier,
                projectileSpeed);
            return true;
        }

        private SideDefenseProjectile CreateProjectile()
        {
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
            }

            return projectile;
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
            ActiveMonsterUnits.Clear();
        }
    }
}
