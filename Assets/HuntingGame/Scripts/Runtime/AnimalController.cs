using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SimpleSpriteAnimator))]
    public sealed class AnimalController : MonoBehaviour
    {
        private AnimalDefinition definition;
        private HuntingGameController game;
        private Rigidbody2D body;
        private SimpleSpriteAnimator animator;
        private float health;
        private Vector2 wanderDirection;
        private float chooseDirectionAt;
        private float fleeUntil;
        private Vector2 threatPosition;
        private float nextAttackTime;

        public AnimalDefinition Definition => definition;
        public bool IsHostile => definition != null && definition.hostile;
        public bool IsAlive => health > 0f;

        public void Initialize(AnimalDefinition animalDefinition, HuntingGameController controller)
        {
            definition = animalDefinition;
            game = controller;
            health = definition.maxHealth;
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<SimpleSpriteAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            GetComponent<CircleCollider2D>().radius = definition.hostile ? 0.42f : 0.32f;
            animator.Play(definition.idleSprites, 5f);
            PickWanderDirection();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || game == null || game.Player == null)
            {
                return;
            }

            var position = (Vector2)transform.position;
            var playerPosition = (Vector2)game.Player.transform.position;
            Vector2 velocity;

            if (definition.hostile)
            {
                velocity = UpdateHostile(position, playerPosition);
            }
            else
            {
                velocity = UpdatePrey(position, playerPosition);
            }

            position += velocity * Time.fixedDeltaTime;
            position = game.ClampToWorld(position, 1f);
            body.MovePosition(position);
            animator.Renderer.flipX = velocity.x < -0.01f;
            animator.Play(velocity.sqrMagnitude > 0.02f ? definition.moveSprites : definition.idleSprites,
                velocity.sqrMagnitude > 0.02f ? 10f : 5f);
        }

        public void TakeDamage(float amount, Vector2 sourcePosition)
        {
            if (!IsAlive)
            {
                return;
            }

            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
            threatPosition = sourcePosition;
            fleeUntil = Time.time + 3f;
            if (health <= 0f)
            {
                Die();
            }
        }

        private Vector2 UpdatePrey(Vector2 position, Vector2 playerPosition)
        {
            var playerDistance = Vector2.Distance(position, playerPosition);
            if (playerDistance <= definition.detectionRadius)
            {
                threatPosition = playerPosition;
                fleeUntil = Time.time + 1.2f;
            }

            if (Time.time < fleeUntil)
            {
                var away = position - threatPosition;
                if (away.sqrMagnitude < 0.01f)
                {
                    away = Random.insideUnitCircle;
                }

                return away.normalized * definition.moveSpeed;
            }

            return WanderVelocity();
        }

        private Vector2 UpdateHostile(Vector2 position, Vector2 playerPosition)
        {
            if (game.IsInsideCamp(position))
            {
                var retreat = position - game.Config.campPosition;
                return retreat.normalized * definition.moveSpeed * 1.25f;
            }

            var distance = Vector2.Distance(position, playerPosition);
            if (distance <= definition.attackRange && !game.IsInsideCamp(playerPosition))
            {
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + definition.attackCooldown;
                    game.Player.TakeDamage(definition.attackDamage);
                }

                return Vector2.zero;
            }

            if (distance <= definition.detectionRadius && !game.IsInsideCamp(playerPosition))
            {
                return (playerPosition - position).normalized * definition.moveSpeed;
            }

            return WanderVelocity();
        }

        private Vector2 WanderVelocity()
        {
            if (Time.time >= chooseDirectionAt)
            {
                PickWanderDirection();
            }

            return wanderDirection * definition.moveSpeed * 0.42f;
        }

        private void PickWanderDirection()
        {
            wanderDirection = Random.insideUnitCircle.normalized;
            chooseDirectionAt = Time.time + Random.Range(1.5f, 4f);
        }

        private void Die()
        {
            body.linearVelocity = Vector2.zero;
            game.HandleAnimalDeath(this);
            Destroy(gameObject);
        }
    }
}
