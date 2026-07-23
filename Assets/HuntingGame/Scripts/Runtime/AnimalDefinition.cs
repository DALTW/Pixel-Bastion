using System;
using UnityEngine;

namespace Game3.Hunting
{
    [CreateAssetMenu(menuName = "GAME-3/Hunting/Animal Definition")]
    public sealed class AnimalDefinition : ScriptableObject
    {
        public string id = "animal";
        public string displayName = "동물";
        public float maxResolve = 35f;
        public float moveSpeed = 2.5f;
        public float detectionRadius = 6f;
        public bool canRetaliate;
        [Range(0f, 1f)] public float retaliationChance;
        public float attackRange = 1.1f;
        public float attackDamage = 15f;
        public float attackCooldown = 1f;
        public float visualScale = 1f;
        public LootDrop[] lootDrops = Array.Empty<LootDrop>();
        public Sprite[] idleSprites = Array.Empty<Sprite>();
        public Sprite[] moveSprites = Array.Empty<Sprite>();
    }
}
