using System;
using UnityEngine;

namespace Game3.Hunting
{
    [CreateAssetMenu(menuName = "GAME-3/Hunting/Game Config")]
    public sealed class HuntingGameConfig : ScriptableObject
    {
        [Header("Definitions")]
        public AnimalDefinition[] animals = Array.Empty<AnimalDefinition>();
        public AnimalPopulation[] populations = Array.Empty<AnimalPopulation>();
        public DogDefinition[] dogs = Array.Empty<DogDefinition>();
        public HunterUpgradeDefinition[] upgrades = Array.Empty<HunterUpgradeDefinition>();

        [Header("Player Art")]
        public Sprite[] playerIdleSprites = Array.Empty<Sprite>();
        public Sprite[] playerWalkSprites = Array.Empty<Sprite>();
        public Sprite[] playerAttackSprites = Array.Empty<Sprite>();

        [Header("World Art")]
        public Texture2D worldTileset;
        public Rect[] groundTileRects = Array.Empty<Rect>();
        public Sprite[] groundSprites = Array.Empty<Sprite>();
        public Sprite[] environmentSprites = Array.Empty<Sprite>();
        public Sprite meatIcon;
        public Sprite hideIcon;
        public Sprite woolIcon;
        public Sprite featherIcon;

        [Header("Economy")]
        public int startingMoney = 50;
        public LootPrice[] lootPrices =
        {
            new LootPrice { type = LootType.Meat, price = 12 },
            new LootPrice { type = LootType.Hide, price = 28 },
            new LootPrice { type = LootType.Wool, price = 20 },
            new LootPrice { type = LootType.Feather, price = 8 }
        };
        public int inventoryCapacity = 24;

        [Header("Hunter")]
        public float baseMoveSpeed = 4.5f;
        public float baseMaxHealth = 100f;
        public float baseSubduePower = 20f;
        public float attackRange = 1.25f;
        [Range(10f, 180f)] public float attackArc = 90f;
        public float attackCooldown = 0.7f;
        public float attackDuration = 0.5f;
        public float attackHitDelay = 0.2f;
        public float harvestDuration = 1.25f;

        [Header("World")]
        public Vector2 worldSize = new Vector2(60f, 40f);
        public Vector2 campPosition = new Vector2(-23f, 0f);
        public float campSafeRadius = 7f;

        public int GetLootPrice(LootType type)
        {
            foreach (var entry in lootPrices ?? Array.Empty<LootPrice>())
            {
                if (entry.type == type)
                {
                    return Mathf.Max(0, entry.price);
                }
            }

            return 0;
        }

        public HunterUpgradeDefinition FindUpgrade(HunterUpgradeType type)
        {
            foreach (var upgrade in upgrades ?? Array.Empty<HunterUpgradeDefinition>())
            {
                if (upgrade != null && upgrade.type == type)
                {
                    return upgrade;
                }
            }

            return null;
        }
    }
}
