using NUnit.Framework;

namespace Game3.Hunting.Tests
{
    public sealed class InventoryModelTests
    {
        [Test]
        public void Add_StopsAtCapacity()
        {
            var inventory = new InventoryModel(4);

            var accepted = inventory.Add(3, 3);

            Assert.That(accepted, Is.EqualTo(4));
            Assert.That(inventory.Meat, Is.EqualTo(3));
            Assert.That(inventory.Hide, Is.EqualTo(1));
            Assert.That(inventory.Remaining, Is.Zero);
        }

        [Test]
        public void SellAll_UsesConfiguredPricesAndClearsInventory()
        {
            var inventory = new InventoryModel(10);
            inventory.Add(2, 3);

            var value = inventory.SellAll(12, 28);

            Assert.That(value, Is.EqualTo(108));
            Assert.That(inventory.Count, Is.Zero);
        }

        [Test]
        public void Clear_RemovesAllCarriedLoot()
        {
            var inventory = new InventoryModel(24);
            inventory.Add(4, 2);

            inventory.Clear();

            Assert.That(inventory.Meat, Is.Zero);
            Assert.That(inventory.Hide, Is.Zero);
        }
    }
}
