using System;
using UnityEngine;

namespace Game3.Hunting
{
    [CreateAssetMenu(menuName = "GAME-3/Hunting/Dog Definition")]
    public sealed class DogDefinition : ScriptableObject
    {
        public string id = "dog";
        public string displayName = "동료 개";
        public int price = 350;
        public float detectionRadius = 8f;
        public float subduePower = 12f;
        public float attackCooldown = 0.8f;
        public float moveSpeed = 5f;
        public Sprite[] sprites = Array.Empty<Sprite>();
    }
}
