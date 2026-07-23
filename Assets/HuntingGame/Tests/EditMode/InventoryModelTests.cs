using NUnit.Framework;

namespace Game3.Hunting.Tests
{
    public sealed class InventoryModelTests
    {
        [Test]
        public void Add_StopsAtCapacity()
        {
            var inventory = new InventoryModel(4);

            var acceptedMeat = inventory.Add(LootType.Meat, 3);
            var acceptedHide = inventory.Add(LootType.Hide, 3);

            Assert.That(acceptedMeat + acceptedHide, Is.EqualTo(4));
            Assert.That(inventory.Meat, Is.EqualTo(3));
            Assert.That(inventory.Hide, Is.EqualTo(1));
            Assert.That(inventory.Remaining, Is.Zero);
        }

        [Test]
        public void SellAll_UsesConfiguredPricesAndClearsInventory()
        {
            var inventory = new InventoryModel(10);
            inventory.Add(LootType.Meat, 2);
            inventory.Add(LootType.Hide, 3);
            inventory.Add(LootType.Wool, 1);
            inventory.Add(LootType.Feather, 2);

            var value = inventory.SellAll(new[]
            {
                new LootPrice { type = LootType.Meat, price = 12 },
                new LootPrice { type = LootType.Hide, price = 28 },
                new LootPrice { type = LootType.Wool, price = 20 },
                new LootPrice { type = LootType.Feather, price = 8 }
            });

            Assert.That(value, Is.EqualTo(144));
            Assert.That(inventory.Count, Is.Zero);
        }

        [Test]
        public void Clear_RemovesAllCarriedLoot()
        {
            var inventory = new InventoryModel(24);
            inventory.Add(LootType.Meat, 4);
            inventory.Add(LootType.Hide, 2);
            inventory.Add(LootType.Wool, 3);
            inventory.Add(LootType.Feather, 1);

            inventory.Clear();

            Assert.That(inventory.Meat, Is.Zero);
            Assert.That(inventory.Hide, Is.Zero);
            Assert.That(inventory.Wool, Is.Zero);
            Assert.That(inventory.Feather, Is.Zero);
        }
    }
}
