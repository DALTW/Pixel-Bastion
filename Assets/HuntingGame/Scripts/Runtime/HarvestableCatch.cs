using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
    public sealed class HarvestableCatch : MonoBehaviour
    {
        private readonly Dictionary<LootType, int> rolledLoot = new Dictionary<LootType, int>();
        private AnimalDefinition definition;
        private HuntingGameController game;
        private float expiresAt;
        private bool harvested;

        public AnimalDefinition Definition => definition;
        public bool IsAvailable => !harvested && Time.time < expiresAt;
        public int RequiredCapacity => rolledLoot.Values.Sum();

        public void Initialize(AnimalDefinition animalDefinition, HuntingGameController controller)
        {
            definition = animalDefinition;
            game = controller;
            expiresAt = Time.time + 30f;
            foreach (var drop in definition.lootDrops ?? Array.Empty<LootDrop>())
            {
                var amount = drop.Roll();
                if (amount <= 0)
                {
                    continue;
                }

                rolledLoot[drop.type] = rolledLoot.TryGetValue(drop.type, out var current)
                    ? current + amount
                    : amount;
            }

            var collider = GetComponent<CircleCollider2D>();
            collider.isTrigger = true;
            game.RegisterCatch(this);
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
                game.UnregisterCatch(this);
            }
        }

        public bool Harvest()
        {
            if (!IsAvailable)
            {
                return false;
            }

            if (game.Inventory.Remaining < RequiredCapacity)
            {
                game.Notify($"가방 공간이 부족합니다. {RequiredCapacity}칸이 필요합니다.");
                return false;
            }

            game.Inventory.Add(rolledLoot);
            harvested = true;
            var summary = string.Join(", ", rolledLoot.Select(item => $"{DisplayName(item.Key)} {item.Value}"));
            game.Notify($"{definition.displayName} 채취: {summary}");
            Destroy(gameObject);
            return true;
        }

        private static string DisplayName(LootType type)
        {
            return type switch
            {
                LootType.Meat => "고기",
                LootType.Hide => "가죽",
                LootType.Wool => "털",
                LootType.Feather => "깃털",
                _ => type.ToString()
            };
        }
    }
}
