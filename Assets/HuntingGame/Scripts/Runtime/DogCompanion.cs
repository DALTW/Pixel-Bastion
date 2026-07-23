using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SimpleSpriteAnimator))]
    public sealed class DogCompanion : MonoBehaviour
    {
        private DogDefinition definition;
        private HuntingGameController game;
        private int slot;
        private Rigidbody2D body;
        private SimpleSpriteAnimator animator;
        private AnimalController targetWolf;
        private float nextScanTime;
        private float nextAttackTime;

        public DogDefinition Definition => definition;

        public void Initialize(DogDefinition dogDefinition, HuntingGameController controller, int companionSlot)
        {
            definition = dogDefinition;
            game = controller;
            slot = companionSlot;
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<SimpleSpriteAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            GetComponent<CircleCollider2D>().radius = 0.35f;
            animator.Play(definition.sprites, 8f);
        }

        private void FixedUpdate()
        {
            if (game == null || game.Player == null)
            {
                return;
            }

            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + 0.3f;
                ScanTargets();
            }

            var position = (Vector2)transform.position;
            var desiredPosition = (Vector2)game.Player.transform.position +
                                  new Vector2(-0.9f - slot * 0.65f, slot % 2 == 0 ? -0.75f : 0.75f);
            var destination = desiredPosition;

            if (targetWolf != null && targetWolf.IsAlive &&
                Vector2.Distance(position, targetWolf.transform.position) <= definition.detectionRadius)
            {
                destination = targetWolf.transform.position;
                var distance = Vector2.Distance(position, destination);
                if (distance <= 0.85f && Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + definition.attackCooldown;
                    targetWolf.TakeDamage(definition.damage, position);
                }
            }

            var delta = destination - position;
            var velocity = delta.magnitude > 0.35f ? delta.normalized * definition.moveSpeed : Vector2.zero;
            body.MovePosition(position + velocity * Time.fixedDeltaTime);
            animator.Renderer.flipX = velocity.x < -0.01f;
            animator.Play(definition.sprites, velocity.sqrMagnitude > 0.01f ? 9f : 5f);
        }

        private void ScanTargets()
        {
            targetWolf = null;
            AnimalController trackedRabbit = null;
            var wolfDistance = float.MaxValue;
            var rabbitDistance = float.MaxValue;
            var position = (Vector2)transform.position;

            foreach (var animal in game.Animals)
            {
                if (animal == null || !animal.IsAlive)
                {
                    continue;
                }

                var distance = Vector2.Distance(position, animal.transform.position);
                if (distance > definition.detectionRadius)
                {
                    continue;
                }

                if (animal.IsHostile && distance < wolfDistance)
                {
                    targetWolf = animal;
                    wolfDistance = distance;
                }
                else if (!animal.IsHostile && distance < rabbitDistance)
                {
                    trackedRabbit = animal;
                    rabbitDistance = distance;
                }
            }

            if (trackedRabbit != null)
            {
                game.ReportTrackedPrey(trackedRabbit, rabbitDistance);
            }
        }
    }
}
