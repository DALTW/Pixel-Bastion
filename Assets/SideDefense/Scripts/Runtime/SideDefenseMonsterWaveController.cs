using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseMonsterWaveController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SideDefenseMapLayout mapLayout;
        [SerializeField] private SideDefenseTower alliedTower;
        [SerializeField] private HumanSummonController humanSummonController;
        [SerializeField] private GameObject[] monsterPrefabs =
            Array.Empty<GameObject>();
        [SerializeField] private Text waveLabel;

        [Header("Wave Rules")]
        [SerializeField, Min(1)] private int maximumWave = 100;
        [SerializeField, Min(1)] private int bossWaveInterval = 10;
        [SerializeField, Min(1)] private int initiallyUnlockedMonsterTypes = 2;
        [FormerlySerializedAs("monstersToDefeatPerWave")]
        [SerializeField, Min(1)] private int baseMonstersToDefeat = 10;
        [SerializeField, Min(1)] private int wavesPerMonsterIncrease = 5;
        [SerializeField, Min(1)] private int additionalMonstersPerIncrease = 1;

        [Header("Wave Difficulty")]
        [SerializeField, Min(0f)] private float healthGrowthPerWave = 0.12f;
        [SerializeField, Min(0f)] private float damageGrowthPerWave = 0.08f;

        [Header("Spawn Timing")]
        [SerializeField, Min(0f)] private float initialSpawnDelay = 2f;
        [SerializeField, Min(0.25f)] private float baseSpawnInterval = 5.5f;
        [SerializeField, Min(0.25f)] private float minimumSpawnInterval = 1.5f;

        [Header("Boss Difficulty")]
        [SerializeField, Min(1f)] private float bossHealthMultiplier = 5f;
        [SerializeField, Min(1f)] private float bossDamageMultiplier = 1.6f;
        [SerializeField, Min(1f)] private float bossScaleMultiplier = 1.5f;
        [SerializeField, Range(0.1f, 1f)]
        private float bossTowerDamageMultiplier = 0.68f;

        [Header("Coin Rewards")]
        [SerializeField, Min(0f)] private float coinRewardIncreasePerWave = 0.1f;

        private float elapsedBattleTime;
        private float spawnCountdown;
        private int currentWave = 1;
        private int unlockedMonsterCount;
        private int currentWaveSpawnedMonsterCount;
        private int currentWaveDefeatedMonsterCount;
        private bool spawningEnabled = true;
        private bool bossSpawnedThisWave;
        private bool allWavesCleared;
        private SideDefenseMonsterUnit activeBoss;

        public int CurrentWave => currentWave;
        public int MaximumWave => maximumWave;
        public int UnlockedMonsterCount => unlockedMonsterCount;
        public int MonstersToDefeatThisWave =>
            CalculateMonstersToDefeat(currentWave);
        public int CurrentWaveDefeatedMonsterCount =>
            currentWaveDefeatedMonsterCount;
        public float ElapsedBattleTime => elapsedBattleTime;
        public bool SpawningEnabled => spawningEnabled;
        public bool AllWavesCleared => allWavesCleared;

        public event Action AllWavesClearedEvent;

        public void Configure(
            SideDefenseMapLayout layout,
            SideDefenseTower tower,
            HumanSummonController summonController,
            GameObject[] orderedMonsterPrefabs,
            Text statusLabel)
        {
            mapLayout = layout;
            alliedTower = tower;
            humanSummonController = summonController;
            monsterPrefabs =
                orderedMonsterPrefabs ?? Array.Empty<GameObject>();
            waveLabel = statusLabel;
        }

        private void Awake()
        {
            currentWave = 1;
            elapsedBattleTime = 0f;
            spawnCountdown = initialSpawnDelay;
            unlockedMonsterCount = Mathf.Clamp(
                initiallyUnlockedMonsterTypes,
                1,
                Mathf.Max(1, monsterPrefabs.Length));
            currentWaveSpawnedMonsterCount = 0;
            currentWaveDefeatedMonsterCount = 0;
            spawningEnabled = true;
            bossSpawnedThisWave = false;
            allWavesCleared = false;
            activeBoss = null;
            RefreshWaveLabel();
        }

        private void Update()
        {
            if (!spawningEnabled ||
                mapLayout == null ||
                alliedTower == null ||
                alliedTower.IsDestroyed ||
                monsterPrefabs.Length == 0)
            {
                return;
            }

            elapsedBattleTime += Time.deltaTime;
            if (IsBossWave(currentWave))
            {
                UpdateBossWave();
                return;
            }

            UpdateNormalWave();
        }

        private void UpdateNormalWave()
        {
            if (currentWaveSpawnedMonsterCount >=
                MonstersToDefeatThisWave)
            {
                return;
            }

            spawnCountdown -= Time.deltaTime;
            if (spawnCountdown > 0f)
            {
                return;
            }

            if (SpawnNormalMonster() != null)
            {
                currentWaveSpawnedMonsterCount++;
            }

            ScheduleNextMonsterSpawn();
        }

        private void UpdateBossWave()
        {
            if (bossSpawnedThisWave)
            {
                return;
            }

            spawnCountdown -= Time.deltaTime;
            if (spawnCountdown <= 0f)
            {
                if (!SpawnBoss())
                {
                    spawnCountdown = minimumSpawnInterval;
                }
            }
        }

        private void ScheduleNextMonsterSpawn()
        {
            float interval = Mathf.Max(
                minimumSpawnInterval,
                baseSpawnInterval * Mathf.Pow(0.9f, currentWave - 1));
            spawnCountdown =
                interval * UnityEngine.Random.Range(0.82f, 1.18f);
        }

        public void StopSpawning()
        {
            spawningEnabled = false;
        }

        private SideDefenseMonsterUnit SpawnNormalMonster()
        {
            int availableMonsterCount = Mathf.Clamp(
                unlockedMonsterCount,
                1,
                monsterPrefabs.Length);
            int strongestIndex = availableMonsterCount - 1;

            int roll = UnityEngine.Random.Range(0, 100);
            int selectedIndex;
            if (roll < 50)
            {
                selectedIndex = strongestIndex;
            }
            else if (roll < 80)
            {
                selectedIndex = Mathf.Max(0, strongestIndex - 1);
            }
            else
            {
                selectedIndex =
                    UnityEngine.Random.Range(0, availableMonsterCount);
            }

            return SpawnMonster(selectedIndex, false);
        }

        private bool SpawnBoss()
        {
            int bossIndex = GetBossMonsterIndex(currentWave);
            activeBoss = SpawnMonster(bossIndex, true);
            bossSpawnedThisWave = activeBoss != null;
            RefreshWaveLabel();
            return bossSpawnedThisWave;
        }

        private SideDefenseMonsterUnit SpawnMonster(
            int monsterIndex,
            bool isBoss)
        {
            if (monsterIndex < 0 || monsterIndex >= monsterPrefabs.Length)
            {
                return null;
            }

            GameObject prefab = monsterPrefabs[monsterIndex];
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = mapLayout.SpawnMonster(prefab);
            instance.name = isBoss
                ? $"BOSS {prefab.name} (Monster Lv.{currentWave})"
                : $"{prefab.name} (Monster Lv.{currentWave})";
            instance.SetActive(true);

            SideDefenseMonsterUnit monster =
                instance.GetComponent<SideDefenseMonsterUnit>();
            if (monster == null)
            {
                return null;
            }

            float healthMultiplier =
                1f + (currentWave - 1) * healthGrowthPerWave;
            float damageMultiplier =
                1f + (currentWave - 1) * damageGrowthPerWave;
            float speedMultiplier =
                1f + Mathf.Min(0.28f, (currentWave - 1) * 0.025f);
            if (isBoss)
            {
                healthMultiplier *= bossHealthMultiplier;
                damageMultiplier *= bossDamageMultiplier;
                instance.transform.localScale *= bossScaleMultiplier;
            }

            monster.ApplyDifficulty(
                healthMultiplier,
                damageMultiplier,
                speedMultiplier,
                currentWave,
                coinRewardIncreasePerWave,
                isBoss ? bossTowerDamageMultiplier : 1f);
            monster.BeginMarch(alliedTower);
            monster.Died += HandleMonsterDied;
            return monster;
        }

        private void HandleMonsterDied(SideDefenseMonsterUnit monster)
        {
            if (monster == null)
            {
                return;
            }

            monster.Died -= HandleMonsterDied;
            if (humanSummonController != null &&
                monster.WasDefeatedByHuman)
            {
                humanSummonController.AddCoins(monster.CoinReward);
            }

            if (monster == activeBoss)
            {
                HandleBossDefeated();
                return;
            }

            if (IsBossWave(currentWave))
            {
                return;
            }

            currentWaveDefeatedMonsterCount = Mathf.Min(
                MonstersToDefeatThisWave,
                currentWaveDefeatedMonsterCount + 1);
            RefreshWaveLabel();
            if (currentWaveDefeatedMonsterCount >=
                MonstersToDefeatThisWave)
            {
                AdvanceToNextWave();
            }
        }

        private void HandleBossDefeated()
        {
            int bossIndex = GetBossMonsterIndex(currentWave);
            unlockedMonsterCount = Mathf.Max(
                unlockedMonsterCount,
                Mathf.Min(monsterPrefabs.Length, bossIndex + 1));
            activeBoss = null;
            humanSummonController?.UnlockNextHuman();

            if (currentWave >= maximumWave)
            {
                CompleteAllWaves();
                return;
            }

            AdvanceToNextWave();
        }

        private void AdvanceToNextWave()
        {
            if (currentWave >= maximumWave)
            {
                CompleteAllWaves();
                return;
            }

            currentWave++;
            spawnCountdown = initialSpawnDelay;
            currentWaveSpawnedMonsterCount = 0;
            currentWaveDefeatedMonsterCount = 0;
            bossSpawnedThisWave = false;
            activeBoss = null;
            RefreshWaveLabel();
        }

        private void CompleteAllWaves()
        {
            if (allWavesCleared)
            {
                return;
            }

            allWavesCleared = true;
            spawningEnabled = false;
            RefreshWaveLabel();
            AllWavesClearedEvent?.Invoke();
        }

        private bool IsBossWave(int wave)
        {
            return wave > 0 && wave % bossWaveInterval == 0;
        }

        private int GetBossMonsterIndex(int wave)
        {
            int defeatedBossNumber = Mathf.Max(1, wave / bossWaveInterval);
            return Mathf.Clamp(
                initiallyUnlockedMonsterTypes + defeatedBossNumber - 1,
                0,
                Mathf.Max(0, monsterPrefabs.Length - 1));
        }

        private int CalculateMonstersToDefeat(int wave)
        {
            int safeWave = Mathf.Max(1, wave);
            int increaseCount =
                safeWave / Mathf.Max(1, wavesPerMonsterIncrease);
            return Mathf.Max(
                1,
                baseMonstersToDefeat +
                increaseCount * additionalMonstersPerIncrease);
        }

        private void RefreshWaveLabel()
        {
            if (waveLabel == null)
            {
                return;
            }

            if (allWavesCleared)
            {
                waveLabel.text =
                    $"WAVE {maximumWave}/{maximumWave}\n" +
                    "ALL WAVES CLEARED";
                return;
            }

            int safeMonsterCount = Mathf.Max(1, monsterPrefabs.Length);
            int visibleUnlockedCount = Mathf.Clamp(
                unlockedMonsterCount,
                1,
                safeMonsterCount);

            if (IsBossWave(currentWave))
            {
                int bossIndex = GetBossMonsterIndex(currentWave);
                string bossName =
                    bossIndex >= 0 &&
                    bossIndex < monsterPrefabs.Length &&
                    monsterPrefabs[bossIndex] != null
                        ? monsterPrefabs[bossIndex].name.ToUpperInvariant()
                        : "UNKNOWN";
                waveLabel.text =
                    $"WAVE {currentWave}/{maximumWave}\n" +
                    $"BOSS {bossName}  |  " +
                    $"{visibleUnlockedCount}/{safeMonsterCount} TYPES";
                return;
            }

            waveLabel.text =
                $"WAVE {currentWave}/{maximumWave}\n" +
                $"DEFEATED {currentWaveDefeatedMonsterCount}/" +
                $"{MonstersToDefeatThisWave}  |  " +
                $"{visibleUnlockedCount}/{safeMonsterCount} TYPES";
        }

        private void OnValidate()
        {
            maximumWave = Mathf.Max(1, maximumWave);
            bossWaveInterval = Mathf.Max(1, bossWaveInterval);
            initiallyUnlockedMonsterTypes =
                Mathf.Max(1, initiallyUnlockedMonsterTypes);
            baseMonstersToDefeat =
                Mathf.Max(1, baseMonstersToDefeat);
            wavesPerMonsterIncrease =
                Mathf.Max(1, wavesPerMonsterIncrease);
            additionalMonstersPerIncrease =
                Mathf.Max(1, additionalMonstersPerIncrease);
            healthGrowthPerWave = Mathf.Max(0f, healthGrowthPerWave);
            damageGrowthPerWave = Mathf.Max(0f, damageGrowthPerWave);
            initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
            baseSpawnInterval = Mathf.Max(0.25f, baseSpawnInterval);
            minimumSpawnInterval =
                Mathf.Clamp(
                    minimumSpawnInterval,
                    0.25f,
                    baseSpawnInterval);
            bossHealthMultiplier = Mathf.Max(1f, bossHealthMultiplier);
            bossDamageMultiplier = Mathf.Max(1f, bossDamageMultiplier);
            bossScaleMultiplier = Mathf.Max(1f, bossScaleMultiplier);
            bossTowerDamageMultiplier =
                Mathf.Clamp(bossTowerDamageMultiplier, 0.1f, 1f);
            coinRewardIncreasePerWave =
                Mathf.Max(0f, coinRewardIncreasePerWave);
        }
    }
}
