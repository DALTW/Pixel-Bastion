using System;
using UnityEngine;

namespace Game3.SideDefense
{
    [Serializable]
    public sealed class SideDefenseSaveData
    {
        public int version = 1;
        public float towerHealth;
        public SideDefenseHumanSaveData human = new SideDefenseHumanSaveData();
        public SideDefenseWaveSaveData wave = new SideDefenseWaveSaveData();
    }

    [Serializable]
    public sealed class SideDefenseHumanSaveData
    {
        public int currentCoins;
        public int unlockedHumanCount;
        public int unlockedUpgradeLevel;
        public int[] upgradeLevels = Array.Empty<int>();
        public float passiveCoinElapsedTime;
        public string selectedHumanName;
        public SideDefenseSavedHumanUnit[] activeHumans =
            Array.Empty<SideDefenseSavedHumanUnit>();
    }

    [Serializable]
    public sealed class SideDefenseSavedHumanUnit
    {
        public string displayName;
        public float currentHealth;
        public float positionX;
        public float positionY;
        public int upgradeLevel;
    }

    [Serializable]
    public sealed class SideDefenseWaveSaveData
    {
        public int currentWave = 1;
        public float elapsedBattleTime;
        public float spawnCountdown;
        public int unlockedMonsterCount;
        public int spawnedMonsterCount;
        public int defeatedMonsterCount;
        public bool bossSpawnedThisWave;
        public SideDefenseSavedMonsterUnit[] activeMonsters =
            Array.Empty<SideDefenseSavedMonsterUnit>();
    }

    [Serializable]
    public sealed class SideDefenseSavedMonsterUnit
    {
        public string displayName;
        public float currentHealth;
        public float positionX;
        public float positionY;
        public bool isBoss;
    }

    public static class SideDefenseSaveSystem
    {
        private const string SaveKey = "PixelBastion.SideDefense.Save.V1";
        private const int CurrentVersion = 1;

        public static bool HasSave => TryLoad(out _);

        public static bool Save(
            SideDefenseTower tower,
            SideDefenseMonsterWaveController waveController,
            HumanSummonController humanController)
        {
            if (tower == null ||
                waveController == null ||
                humanController == null ||
                tower.IsDestroyed ||
                waveController.AllWavesCleared)
            {
                return false;
            }

            SideDefenseSaveData data = new SideDefenseSaveData
            {
                version = CurrentVersion,
                towerHealth = tower.CurrentHealth,
                human = humanController.CaptureSaveData(),
                wave = waveController.CaptureSaveData()
            };

            try
            {
                string json = JsonUtility.ToJson(data);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save Pixel Bastion: {exception}");
                return false;
            }
        }

        public static bool TryLoad(out SideDefenseSaveData data)
        {
            data = null;
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return false;
            }

            try
            {
                string json = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                data = JsonUtility.FromJson<SideDefenseSaveData>(json);
                return data != null &&
                       data.version == CurrentVersion &&
                       data.human != null &&
                       data.wave != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Pixel Bastion save data could not be loaded: " +
                    exception.Message);
                data = null;
                return false;
            }
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
