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
        private float resolve;
        private Vector2 wanderDirection;
        private float chooseDirectionAt;
        private float fleeUntil;
        private Vector2 threatPosition;
        private float retaliateUntil;
        private float nextAttackTime;

        public AnimalDefinition Definition => definition;
        public bool IsHostile => definition != null && definition.canRetaliate;
        public bool IsAlive => !IsSubdued;
        public bool IsSubdued { get; private set; }
        public float Resolve => resolve;
        public float MaxResolve => definition?.maxResolve ?? 0f;

        public void Initialize(AnimalDefinition animalDefinition, HuntingGameController controller)
        {
            definition = animalDefinition;
            game = controller;
            resolve = definition.maxResolve;
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<SimpleSpriteAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            GetComponent<CircleCollider2D>().radius = Mathf.Lerp(0.25f, 0.52f, definition.visualScale * 0.45f);
            animator.Play(definition.idleSprites, 5f);
            PickWanderDirection();
        }

        private void FixedUpdate()
        {
            if (IsSubdued || game == null || game.Player == null)
            {
                return;
            }

            var position = (Vector2)transform.position;
            var playerPosition = (Vector2)game.Player.transform.position;
            Vector2 velocity;

            velocity = Time.time < retaliateUntil
                ? UpdateRetaliation(position, playerPosition)
                : UpdatePrey(position, playerPosition);

            position += velocity * Time.fixedDeltaTime;
            position = game.ClampToWorld(position, 1f);
            body.MovePosition(position);
            animator.Renderer.flipX = velocity.x < -0.01f;
            animator.Play(velocity.sqrMagnitude > 0.02f ? definition.moveSprites : definition.idleSprites,
                velocity.sqrMagnitude > 0.02f ? 10f : 5f);
        }

        public void TakeSubdueDamage(float amount, Vector2 sourcePosition)
        {
            if (IsSubdued)
            {
                return;
            }

            resolve = Mathf.Max(0f, resolve - Mathf.Max(0f, amount));
            threatPosition = sourcePosition;
            fleeUntil = Time.time + 3f;
            if (resolve <= 0f)
            {
                Subdue();
                return;
            }

            if (definition.canRetaliate && Random.value <= definition.retaliationChance)
            {
                retaliateUntil = Time.time + 3.2f;
            }
        }

        public void TakeDamage(float amount, Vector2 sourcePosition) => TakeSubdueDamage(amount, sourcePosition);

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

        private Vector2 UpdateRetaliation(Vector2 position, Vector2 playerPosition)
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

            if (!game.IsInsideCamp(playerPosition))
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

        private void Subdue()
        {
            IsSubdued = true;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            animator.Play(definition.idleSprites, 3f);
            animator.Renderer.color = new Color(0.62f, 0.7f, 0.72f, 0.95f);
            game.HandleAnimalSubdued(this);
        }
    }
}
