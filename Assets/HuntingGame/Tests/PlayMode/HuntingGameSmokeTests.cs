using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game3.Hunting.Tests
{
    public sealed class HuntingGameSmokeTests
    {
        [UnityTest]
        public IEnumerator HuntingScene_CreatesPlayableWorld()
        {
            SceneManager.LoadScene("HuntingGame");
            yield return null;
            yield return null;
            yield return null;

            var game = HuntingGameController.Instance;
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Player, Is.Not.Null);
            Assert.That(game.Hud, Is.Not.Null);
            Assert.That(game.Config.animals, Has.Length.EqualTo(6));
            Assert.That(game.Config.upgrades, Has.Length.EqualTo(3));
            Assert.That(game.Animals.Count, Is.GreaterThanOrEqualTo(22));
            Assert.That(game.Inventory.Capacity, Is.EqualTo(24));
        }

        [UnityTest]
        public IEnumerator PlayerAttack_SubduesAnimalInMovementDirection()
        {
            SceneManager.LoadScene("HuntingGame");
            yield return null;
            yield return null;

            var game = HuntingGameController.Instance;
            var animal = game.Animals[0];
            game.Player.transform.position = Vector3.zero;
            animal.transform.position = Vector3.down * 0.8f;
            var before = animal.Resolve;

            var affected = game.Player.ApplySubdueHit();
            yield return null;

            Assert.That(affected, Is.GreaterThanOrEqualTo(1));
            Assert.That(animal.Resolve, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator SubduedAnimal_CanBeHarvestedIntoLoot()
        {
            SceneManager.LoadScene("HuntingGame");
            yield return null;
            yield return null;

            var game = HuntingGameController.Instance;
            var animal = game.Animals[0];
            animal.TakeSubdueDamage(9999f, game.Player.transform.position);
            yield return null;

            var harvestable = animal.GetComponent<HarvestableCatch>();
            Assert.That(harvestable, Is.Not.Null);
            Assert.That(animal.IsSubdued, Is.True);
            Assert.That(harvestable.Harvest(), Is.True);
            Assert.That(game.Inventory.Count, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator PlayerDeath_ClearsLootButKeepsMoney()
        {
            SceneManager.LoadScene("HuntingGame");
            yield return null;
            yield return null;

            var game = HuntingGameController.Instance;
            game.Inventory.Add(LootType.Meat, 3);
            game.Inventory.Add(LootType.Hide, 2);
            var money = game.Money;

            game.HandlePlayerDeath();
            yield return null;

            Assert.That(game.Inventory.Count, Is.Zero);
            Assert.That(game.Money, Is.EqualTo(money));
            Assert.That(game.IsInsideCamp(game.Player.transform.position), Is.True);
        }
    }
}
