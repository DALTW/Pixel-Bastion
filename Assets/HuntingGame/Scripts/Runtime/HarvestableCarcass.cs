using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class HarvestableCarcass : MonoBehaviour
    {
        private AnimalDefinition definition;
        private HuntingGameController game;
        private float expiresAt;
        private bool harvested;

        public AnimalDefinition Definition => definition;
        public bool IsAvailable => !harvested && Time.time < expiresAt;

        public void Initialize(AnimalDefinition animalDefinition, Sprite corpseSprite, HuntingGameController controller)
        {
            definition = animalDefinition;
            game = controller;
            expiresAt = Time.time + 30f;
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = corpseSprite;
            renderer.color = new Color(0.58f, 0.58f, 0.58f, 0.92f);
            renderer.sortingOrder = 4;
            transform.localScale = new Vector3(1f, 0.62f, 1f);
            var collider = GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            game.RegisterCarcass(this);
        }

        private void Update()
        {
            if (Time.time >= expiresAt)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.UnregisterCarcass(this);
            }
        }

        public bool Harvest()
        {
            if (!IsAvailable)
            {
                return false;
            }

            var hideAmount = definition.hideYield;
            if (definition.hideChance < 1f && Random.value > definition.hideChance)
            {
                hideAmount = 0;
            }

            var total = definition.meatYield + hideAmount;
            if (game.Inventory.Remaining < total)
            {
                game.Notify($"가방 공간이 부족합니다. {total}칸이 필요합니다.");
                return false;
            }

            game.Inventory.Add(definition.meatYield, hideAmount);
            harvested = true;
            game.Notify($"{definition.displayName} 채취: 고기 {definition.meatYield}, 가죽 {hideAmount}");
            Destroy(gameObject);
            return true;
        }
    }
}
