using System;
using UnityEngine;

namespace Game3.Hunting
{
    public enum LootType
    {
        Meat,
        Hide,
        Wool,
        Feather
    }

    public enum HunterUpgradeType
    {
        SubduePower,
        MoveSpeed,
        MaxHealth
    }

    [Serializable]
    public struct LootDrop
    {
        public LootType type;
        [Min(0)] public int minAmount;
        [Min(0)] public int maxAmount;
        [Range(0f, 1f)] public float chance;

        public int Roll()
        {
            if (chance < 1f && UnityEngine.Random.value > chance)
            {
                return 0;
            }

            var minimum = Mathf.Max(0, minAmount);
            var maximum = Mathf.Max(minimum, maxAmount);
            return UnityEngine.Random.Range(minimum, maximum + 1);
        }
    }

    [Serializable]
    public struct LootPrice
    {
        public LootType type;
        [Min(0)] public int price;
    }

    [Serializable]
    public struct AnimalPopulation
    {
        public AnimalDefinition animal;
        [Min(0)] public int count;
    }
}
