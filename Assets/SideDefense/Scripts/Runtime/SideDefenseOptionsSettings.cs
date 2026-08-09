using System;
using UnityEngine;

namespace Game3.SideDefense
{
    public enum SideDefenseHealthBarMode
    {
        Always = 0,
        OnDamage = 1,
        Hidden = 2
    }

    public static class SideDefenseOptionsSettings
    {
        private const string SoundVolumeKey =
            "PixelBastion.Options.SoundVolume";
        private const string GameplaySpeedKey =
            "PixelBastion.Options.GameplaySpeed";
        private const string HealthBarModeKey =
            "PixelBastion.Options.HealthBarMode";
        private const string DamageNumbersKey =
            "PixelBastion.Options.DamageNumbers";

        private static float soundVolume;
        private static float gameplaySpeed;
        private static SideDefenseHealthBarMode healthBarMode;
        private static bool damageNumbersEnabled;

        public static event Action Changed;

        public static float SoundVolume
        {
            get => soundVolume;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(soundVolume, clamped))
                {
                    return;
                }

                soundVolume = clamped;
                PlayerPrefs.SetFloat(SoundVolumeKey, soundVolume);
                SaveAndNotify();
            }
        }

        public static float GameplaySpeed
        {
            get => gameplaySpeed;
            set
            {
                float rounded = Mathf.Round(
                    Mathf.Clamp(value, 1f, 2f) * 10f) / 10f;
                if (Mathf.Approximately(gameplaySpeed, rounded))
                {
                    return;
                }

                gameplaySpeed = rounded;
                PlayerPrefs.SetFloat(GameplaySpeedKey, gameplaySpeed);
                SaveAndNotify();
            }
        }

        public static SideDefenseHealthBarMode HealthBarMode
        {
            get => healthBarMode;
            set
            {
                SideDefenseHealthBarMode clamped =
                    (SideDefenseHealthBarMode)Mathf.Clamp(
                        (int)value,
                        (int)SideDefenseHealthBarMode.Always,
                        (int)SideDefenseHealthBarMode.Hidden);
                if (healthBarMode == clamped)
                {
                    return;
                }

                healthBarMode = clamped;
                PlayerPrefs.SetInt(HealthBarModeKey, (int)healthBarMode);
                SaveAndNotify();
            }
        }

        public static bool DamageNumbersEnabled
        {
            get => damageNumbersEnabled;
            set
            {
                if (damageNumbersEnabled == value)
                {
                    return;
                }

                damageNumbersEnabled = value;
                PlayerPrefs.SetInt(DamageNumbersKey, value ? 1 : 0);
                SaveAndNotify();
            }
        }

        static SideDefenseOptionsSettings()
        {
            Load();
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadBeforePlay()
        {
            Changed = null;
            Load();
        }

        private static void Load()
        {
            soundVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(SoundVolumeKey, 1f));
            gameplaySpeed = Mathf.Round(
                Mathf.Clamp(
                    PlayerPrefs.GetFloat(GameplaySpeedKey, 1f),
                    1f,
                    2f) * 10f) / 10f;
            healthBarMode = (SideDefenseHealthBarMode)Mathf.Clamp(
                PlayerPrefs.GetInt(
                    HealthBarModeKey,
                    (int)SideDefenseHealthBarMode.Always),
                (int)SideDefenseHealthBarMode.Always,
                (int)SideDefenseHealthBarMode.Hidden);
            damageNumbersEnabled =
                PlayerPrefs.GetInt(DamageNumbersKey, 1) != 0;
        }

        private static void SaveAndNotify()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
