using UnityEngine;

namespace Game3.SideDefense
{
    public enum SideDefenseAttackStyle
    {
        Melee,
        Ranged
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SideDefenseProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float maximumLifetime = 5f;
        [SerializeField, Min(0.01f)] private float hitDistance = 0.08f;

        private SideDefenseMonsterUnit monsterTarget;
        private SideDefenseHumanUnit humanTarget;
        private SideDefenseTower towerTarget;
        private float damage;
        private float movementSpeed;
        private float remainingLifetime;
        private bool damageComesFromHuman;
        private bool healsHumanTarget;

        public bool HasTarget =>
            monsterTarget != null ||
            humanTarget != null ||
            towerTarget != null;

        public void Launch(
            SideDefenseMonsterUnit target,
            float attackDamage,
            float speed)
        {
            monsterTarget = target;
            damageComesFromHuman = false;
            ConfigureLaunch(attackDamage, speed);
        }

        public void LaunchFromHuman(
            SideDefenseMonsterUnit target,
            float attackDamage,
            float speed)
        {
            monsterTarget = target;
            damageComesFromHuman = true;
            ConfigureLaunch(attackDamage, speed);
        }

        public void Launch(
            SideDefenseHumanUnit target,
            float attackDamage,
            float speed)
        {
            humanTarget = target;
            ConfigureLaunch(attackDamage, speed);
        }

        public void LaunchHealing(
            SideDefenseHumanUnit target,
            float healingAmount,
            float speed)
        {
            humanTarget = target;
            healsHumanTarget = true;
            ConfigureLaunch(healingAmount, speed);
        }

        public void Launch(
            SideDefenseTower target,
            float attackDamage,
            float speed)
        {
            towerTarget = target;
            ConfigureLaunch(attackDamage, speed);
        }

        private void ConfigureLaunch(float attackDamage, float speed)
        {
            damage = Mathf.Max(0f, attackDamage);
            movementSpeed = Mathf.Max(0.1f, speed);
            remainingLifetime = maximumLifetime;

            Vector3 targetPosition;
            if (TryGetTargetPosition(out targetPosition))
            {
                SpriteRenderer renderer = GetComponent<SpriteRenderer>();
                renderer.flipX = targetPosition.x < transform.position.x;
            }

            SideDefenseSpriteAnimator animator =
                GetComponent<SideDefenseSpriteAnimator>();
            animator?.SetWalking(true);
        }

        private void Update()
        {
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPosition;
            if (!TryGetTargetPosition(out targetPosition))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                movementSpeed * Time.deltaTime);
            if ((transform.position - targetPosition).sqrMagnitude <=
                hitDistance * hitDistance)
            {
                ApplyDamage();
                Destroy(gameObject);
            }
        }

        private bool TryGetTargetPosition(out Vector3 targetPosition)
        {
            if (monsterTarget != null && monsterTarget.IsAlive)
            {
                targetPosition =
                    monsterTarget.transform.position + Vector3.up * 0.18f;
                return true;
            }

            if (humanTarget != null && humanTarget.IsAlive)
            {
                targetPosition =
                    humanTarget.transform.position + Vector3.up * 0.18f;
                return true;
            }

            if (towerTarget != null && !towerTarget.IsDestroyed)
            {
                targetPosition =
                    towerTarget.transform.position + Vector3.up * 0.35f;
                return true;
            }

            targetPosition = transform.position;
            return false;
        }

        private void ApplyDamage()
        {
            if (monsterTarget != null && monsterTarget.IsAlive)
            {
                if (damageComesFromHuman)
                {
                    monsterTarget.TakeDamageFromHuman(damage);
                }
                else
                {
                    monsterTarget.TakeDamage(damage);
                }

                return;
            }

            if (humanTarget != null && humanTarget.IsAlive)
            {
                if (healsHumanTarget)
                {
                    humanTarget.Heal(damage);
                }
                else
                {
                    humanTarget.TakeDamage(damage);
                }

                return;
            }

            if (towerTarget != null && !towerTarget.IsDestroyed)
            {
                towerTarget.TakeDamage(damage);
            }
        }
    }
}
