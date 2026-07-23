using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game3.Hunting
{
    [Serializable]
    public sealed class HunterUpgradeSave
    {
        public HunterUpgradeType type;
        public int level;
    }

    [Serializable]
    public sealed class HuntingSaveData
    {
        public int version = HuntingSaveSystem.CurrentVersion;
        public int money = 50;
        public List<string> ownedDogIds = new List<string>();
        public List<HunterUpgradeSave> upgrades = new List<HunterUpgradeSave>();

        public void Normalize()
        {
            version = HuntingSaveSystem.CurrentVersion;
            money = Math.Max(0, money);
            ownedDogIds ??= new List<string>();
            upgrades ??= new List<HunterUpgradeSave>();
            ownedDogIds = ownedDogIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(2).ToList();
            upgrades = upgrades
                .Where(item => item != null)
                .GroupBy(item => item.type)
                .Select(group => new HunterUpgradeSave
                {
                    type = group.Key,
                    level = Math.Max(0, group.Max(item => item.level))
                })
                .ToList();

            foreach (HunterUpgradeType type in Enum.GetValues(typeof(HunterUpgradeType)))
            {
                if (upgrades.All(item => item.type != type))
                {
                    upgrades.Add(new HunterUpgradeSave { type = type, level = 0 });
                }
            }
        }

        public int GetUpgradeLevel(HunterUpgradeType type)
        {
            return upgrades?.FirstOrDefault(item => item != null && item.type == type)?.level ?? 0;
        }

        public void SetUpgradeLevel(HunterUpgradeType type, int level)
        {
            upgrades ??= new List<HunterUpgradeSave>();
            var state = upgrades.FirstOrDefault(item => item != null && item.type == type);
            if (state == null)
            {
                state = new HunterUpgradeSave { type = type };
                upgrades.Add(state);
            }

            state.level = Math.Max(0, level);
        }
    }

    public sealed class HuntingSaveSystem
    {
        public const int CurrentVersion = 2;
        public string SavePath { get; }

        public HuntingSaveSystem(string savePath = null)
        {
            SavePath = string.IsNullOrWhiteSpace(savePath)
                ? Path.Combine(Application.persistentDataPath, "hunting-save.json")
                : savePath;
        }

        public HuntingSaveData LoadOrCreate(int startingMoney)
        {
            if (!File.Exists(SavePath))
            {
                return CreateDefault(startingMoney);
            }

            try
            {
                var json = File.ReadAllText(SavePath);
                var envelope = JsonUtility.FromJson<SaveVersionEnvelope>(json);
                HuntingSaveData data;
                if (envelope != null && envelope.version == 1)
                {
                    var legacy = JsonUtility.FromJson<LegacyHuntingSaveDataV1>(json);
                    data = MigrateLegacy(legacy, startingMoney);
                }
                else if (envelope != null && envelope.version == CurrentVersion)
                {
                    data = JsonUtility.FromJson<HuntingSaveData>(json);
                }
                else
                {
                    throw new InvalidDataException("지원하지 않는 저장 데이터입니다.");
                }

                if (data == null)
                {
                    throw new InvalidDataException("저장 데이터가 비어 있습니다.");
                }

                data.Normalize();
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"저장 파일을 읽지 못해 새 게임으로 시작합니다: {exception.Message}");
                BackupCorruptSave();
                return CreateDefault(startingMoney);
            }
        }

        public void Save(HuntingSaveData data)
        {
            data ??= CreateDefault(50);
            data.Normalize();
            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var temporaryPath = SavePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            File.Move(temporaryPath, SavePath);
        }

        public static HuntingSaveData CreateDefault(int startingMoney)
        {
            return new HuntingSaveData
            {
                version = CurrentVersion,
                money = Math.Max(0, startingMoney),
                ownedDogIds = new List<string>(),
                upgrades = Enum.GetValues(typeof(HunterUpgradeType))
                    .Cast<HunterUpgradeType>()
                    .Select(type => new HunterUpgradeSave { type = type, level = 0 })
                    .ToList()
            };
        }

        private static HuntingSaveData MigrateLegacy(LegacyHuntingSaveDataV1 legacy, int startingMoney)
        {
            return new HuntingSaveData
            {
                version = CurrentVersion,
                money = Math.Max(0, legacy?.money ?? startingMoney),
                ownedDogIds = legacy?.ownedDogIds?
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .Take(2)
                    .ToList() ?? new List<string>(),
                upgrades = new List<HunterUpgradeSave>()
            };
        }

        private void BackupCorruptSave()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return;
                }

                var backupPath = SavePath + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
                File.Move(SavePath, backupPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"손상된 저장 파일 백업에 실패했습니다: {exception.Message}");
            }
        }

        [Serializable]
        private sealed class SaveVersionEnvelope
        {
            public int version;
        }

        [Serializable]
        private sealed class LegacyHuntingSaveDataV1
        {
            public int version = 1;
            public int money;
            public List<string> ownedDogIds = new List<string>();
        }
    }
}
