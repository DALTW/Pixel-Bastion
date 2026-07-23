using System;
using System.Collections.Generic;

namespace Game3.Hunting
{
    [Serializable]
    public sealed class InventoryModel
    {
        private readonly int[] amounts = new int[Enum.GetValues(typeof(LootType)).Length];

        public int Capacity { get; }
        public int Meat => Get(LootType.Meat);
        public int Hide => Get(LootType.Hide);
        public int Wool => Get(LootType.Wool);
        public int Feather => Get(LootType.Feather);
        public int Count
        {
            get
            {
                var total = 0;
                foreach (var amount in amounts)
                {
                    total += amount;
                }

                return total;
            }
        }
        public int Remaining => Math.Max(0, Capacity - Count);

        public InventoryModel(int capacity)
        {
            Capacity = Math.Max(1, capacity);
        }

        public int Get(LootType type)
        {
            var index = (int)type;
            return index >= 0 && index < amounts.Length ? amounts[index] : 0;
        }

        public int Add(LootType type, int amount)
        {
            var index = (int)type;
            if (index < 0 || index >= amounts.Length)
            {
                return 0;
            }

            var accepted = Math.Min(Math.Max(0, amount), Remaining);
            amounts[index] += accepted;
            return accepted;
        }

        public int Add(IReadOnlyDictionary<LootType, int> loot)
        {
            var accepted = 0;
            foreach (LootType type in Enum.GetValues(typeof(LootType)))
            {
                if (loot != null && loot.TryGetValue(type, out var amount))
                {
                    accepted += Add(type, amount);
                }
            }

            return accepted;
        }

        public int SellAll(LootPrice[] prices)
        {
            var value = 0;
            foreach (var price in prices ?? Array.Empty<LootPrice>())
            {
                value += Get(price.type) * Math.Max(0, price.price);
            }

            Clear();
            return value;
        }

        public void Clear()
        {
            Array.Clear(amounts, 0, amounts.Length);
        }
    }
}
