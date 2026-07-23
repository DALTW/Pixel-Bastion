using System.IO;
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
            data.ownedWeaponIds.Add("revolver");
            data.ownedDogIds.Add("scout");
            data.equippedWeaponId = "revolver";
            system.Save(data);

            var loaded = system.LoadOrCreate(50);

            Assert.That(loaded.money, Is.EqualTo(725));
            Assert.That(loaded.ownedWeaponIds, Does.Contain("revolver"));
            Assert.That(loaded.ownedDogIds, Does.Contain("scout"));
            Assert.That(loaded.equippedWeaponId, Is.EqualTo("revolver"));
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
        public void Normalize_AlwaysRestoresStarterWeaponAndLimitsDogs()
        {
            var data = new HuntingSaveData
            {
                ownedWeaponIds = new System.Collections.Generic.List<string>(),
                ownedDogIds = new System.Collections.Generic.List<string> { "a", "b", "c" },
                equippedWeaponId = "missing"
            };

            data.Normalize();

            Assert.That(data.ownedWeaponIds, Does.Contain("glock"));
            Assert.That(data.equippedWeaponId, Is.EqualTo("glock"));
            Assert.That(data.ownedDogIds, Has.Count.EqualTo(2));
        }
    }
}
