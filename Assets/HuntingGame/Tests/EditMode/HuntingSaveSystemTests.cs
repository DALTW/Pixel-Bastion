using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Game3.Hunting.Tests
{
    public sealed class HuntingSaveSystemTests
    {
        private string temporaryDirectory;
        private string savePath;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), "GAME3-HuntingTests", System.Guid.NewGuid().ToString("N"));
            savePath = Path.Combine(temporaryDirectory, "save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void SaveAndLoad_RoundTripsProgress()
        {
            var system = new HuntingSaveSystem(savePath);
            var data = HuntingSaveSystem.CreateDefault(50);
            data.money = 725;
            data.ownedDogIds.Add("scout");
            data.SetUpgradeLevel(HunterUpgradeType.SubduePower, 2);
            system.Save(data);

            var loaded = system.LoadOrCreate(50);

            Assert.That(loaded.money, Is.EqualTo(725));
            Assert.That(loaded.ownedDogIds, Does.Contain("scout"));
            Assert.That(loaded.GetUpgradeLevel(HunterUpgradeType.SubduePower), Is.EqualTo(2));
        }

        [Test]
        public void CorruptSave_FallsBackAndCreatesBackup()
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(savePath, "{ not valid json");
            var system = new HuntingSaveSystem(savePath);

            var loaded = system.LoadOrCreate(80);

            Assert.That(loaded.money, Is.EqualTo(80));
            Assert.That(Directory.GetFiles(temporaryDirectory, "*.bak"), Has.Length.EqualTo(1));
        }

        [Test]
        public void Normalize_AddsUpgradeStatesAndLimitsDogs()
        {
            var data = new HuntingSaveData
            {
                ownedDogIds = new System.Collections.Generic.List<string> { "a", "b", "c" },
                upgrades = new System.Collections.Generic.List<HunterUpgradeSave>()
            };

            data.Normalize();

            Assert.That(data.ownedDogIds, Has.Count.EqualTo(2));
            Assert.That(data.upgrades, Has.Count.EqualTo(3));
        }

        [Test]
        public void VersionOneSave_MigratesMoneyAndDogsButDropsWeapons()
        {
            Directory.CreateDirectory(temporaryDirectory);
            File.WriteAllText(savePath,
                "{\"version\":1,\"money\":930,\"equippedWeaponId\":\"ak47\"," +
                "\"ownedWeaponIds\":[\"glock\",\"ak47\"],\"ownedDogIds\":[\"guardian\"]}");
            var system = new HuntingSaveSystem(savePath);

            var loaded = system.LoadOrCreate(50);

            Assert.That(loaded.version, Is.EqualTo(HuntingSaveSystem.CurrentVersion));
            Assert.That(loaded.money, Is.EqualTo(930));
            Assert.That(loaded.ownedDogIds, Is.EquivalentTo(new[] { "guardian" }));
            Assert.That(loaded.upgrades.All(item => item.level == 0), Is.True);
        }
    }
}
