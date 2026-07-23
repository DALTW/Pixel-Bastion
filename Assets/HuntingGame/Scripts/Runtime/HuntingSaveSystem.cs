using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game3.Hunting
{
    [Serializable]
    public sealed class WeaponAmmoSave
    {
        public string weaponId;
        public int reserveAmmo;
        public int magazineAmmo;
    }

    [Serializable]
    public sealed class HuntingSaveData
    {
        public int version = 1;
        public int money = 50;
        public string equippedWeaponId = "glock";
        public List<string> ownedWeaponIds = new List<string> { "glock" };
        public List<string> ownedDogIds = new List<string>();
        public List<WeaponAmmoSave> weaponAmmo = new List<WeaponAmmoSave>();

        public void Normalize()
        {
            version = 1;
            money = Math.Max(0, money);
            ownedWeaponIds ??= new List<string>();
            ownedDogIds ??= new List<string>();
            weaponAmmo ??= new List<WeaponAmmoSave>();
            ownedWeaponIds = ownedWeaponIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            ownedDogIds = ownedDogIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(2).ToList();
            if (!ownedWeaponIds.Contains("glock"))
            {
                ownedWeaponIds.Insert(0, "glock");
            }

            if (string.IsNullOrWhiteSpace(equippedWeaponId) || !ownedWeaponIds.Contains(equippedWeaponId))
            {
                equippedWeaponId = ownedWeaponIds[0];
            }

            foreach (var ammo in weaponAmmo)
            {
                ammo.reserveAmmo = Math.Max(0, ammo.reserveAmmo);
                ammo.magazineAmmo = Math.Max(0, ammo.magazineAmmo);
            }
        }
    }

    public sealed class HuntingSaveSystem
    {
        public const int CurrentVersion = 1;
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
                var data = JsonUtility.FromJson<HuntingSaveData>(json);
                if (data == null || data.version != CurrentVersion)
                {
                    throw new InvalidDataException("지원하지 않는 저장 데이터입니다.");
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
                equippedWeaponId = "glock",
                ownedWeaponIds = new List<string> { "glock" },
                ownedDogIds = new List<string>(),
                weaponAmmo = new List<WeaponAmmoSave>
                {
                    new WeaponAmmoSave { weaponId = "glock", reserveAmmo = 32, magazineAmmo = 8 }
                }
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
    }
}
