using System;
using UnityEngine;

namespace Game3.Hunting
{
    [CreateAssetMenu(menuName = "GAME-3/Hunting/Game Config")]
    public sealed class HuntingGameConfig : ScriptableObject
    {
        [Header("Definitions")]
        public WeaponDefinition[] weapons = Array.Empty<WeaponDefinition>();
        public AnimalDefinition rabbit;
        public AnimalDefinition wolf;
        public DogDefinition[] dogs = Array.Empty<DogDefinition>();

        [Header("Player Art")]
        public Sprite[] playerIdleSprites = Array.Empty<Sprite>();
        public Sprite[] playerWalkSprites = Array.Empty<Sprite>();

        [Header("World Art")]
        public Sprite[] environmentSprites = Array.Empty<Sprite>();
        public Sprite meatIcon;
        public Sprite hideIcon;

        [Header("Economy")]
        public int startingMoney = 50;
        public int meatSellPrice = 12;
        public int hideSellPrice = 28;
        public int inventoryCapacity = 24;

        [Header("World")]
        public Vector2 worldSize = new Vector2(60f, 40f);
        public Vector2 campPosition = new Vector2(-23f, 0f);
        public float campSafeRadius = 7f;
        public int rabbitPopulation = 14;
        public int wolfPopulation = 5;
    }
}
