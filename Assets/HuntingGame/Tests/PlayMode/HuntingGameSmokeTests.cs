using System.Collections;
using NUnit.Framework;
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
            Assert.That(game.Config.weapons, Has.Length.EqualTo(4));
            Assert.That(game.Animals.Count, Is.GreaterThanOrEqualTo(19));
            Assert.That(game.Inventory.Capacity, Is.EqualTo(24));
        }

        [UnityTest]
        public IEnumerator PlayerDeath_ClearsLootButKeepsMoney()
        {
            SceneManager.LoadScene("HuntingGame");
            yield return null;
            yield return null;

            var game = HuntingGameController.Instance;
            game.Inventory.Add(3, 2);
            var money = game.Money;

            game.HandlePlayerDeath();
            yield return null;

            Assert.That(game.Inventory.Count, Is.Zero);
            Assert.That(game.Money, Is.EqualTo(money));
            Assert.That(game.IsInsideCamp(game.Player.transform.position), Is.True);
        }
    }
}
