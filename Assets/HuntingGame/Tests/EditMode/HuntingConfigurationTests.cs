using NUnit.Framework;
using Game3.Hunting.Editor;

namespace Game3.Hunting.Tests
{
    public sealed class HuntingConfigurationTests
    {
        [Test]
        public void GeneratedConfig_HasAllRequiredSunnysideBindings()
        {
            var config = HuntingGameBuilder.GetOrCreateConfig();
            var errors = HuntingGameBuilder.ValidateConfiguration(config);

            Assert.That(errors, Is.Empty);
            Assert.That(config.playerAttackSprites, Has.Length.EqualTo(10));
            Assert.That(config.animals, Has.Length.EqualTo(6));
            Assert.That(config.populations, Has.Length.EqualTo(6));
            Assert.That(config.lootPrices, Has.Length.EqualTo(4));
        }

        [Test]
        public void UpgradeDefinitions_UseThreePurchaseLevels()
        {
            var config = HuntingGameBuilder.GetOrCreateConfig();

            foreach (var upgrade in config.upgrades)
            {
                Assert.That(upgrade.MaxLevel, Is.EqualTo(3));
                Assert.That(upgrade.GetCost(0), Is.GreaterThan(0));
                Assert.That(upgrade.GetCost(3), Is.EqualTo(-1));
            }
        }
    }
}
