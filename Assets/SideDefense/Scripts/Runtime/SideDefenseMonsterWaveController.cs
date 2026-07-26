using System;
using UnityEngine;
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

        [Header("Time Difficulty")]
        [SerializeField, Min(5f)] private float secondsPerWave = 30f;
        [SerializeField, Min(0f)] private float initialSpawnDelay = 2f;
        [SerializeField, Min(0.25f)] private float baseSpawnInterval = 5.5f;
        [SerializeField, Min(0.25f)] private float minimumSpawnInterval = 1.5f;

        private float elapsedBattleTime;
        private float spawnCountdown;
        private int currentWave = 1;
        private bool spawningEnabled = true;

        public int CurrentWave => currentWave;
        public float ElapsedBattleTime => elapsedBattleTime;
        public bool SpawningEnabled => spawningEnabled;

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
            spawningEnabled = true;
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
            int calculatedWave =
                Mathf.FloorToInt(elapsedBattleTime / secondsPerWave) + 1;
            if (calculatedWave != currentWave)
            {
                currentWave = calculatedWave;
                RefreshWaveLabel();
            }

            spawnCountdown -= Time.deltaTime;
            if (spawnCountdown > 0f)
            {
                return;
            }

            SpawnMonster();
            float interval = Mathf.Max(
                minimumSpawnInterval,
                baseSpawnInterval * Mathf.Pow(0.9f, currentWave - 1));
            spawnCountdown = interval * UnityEngine.Random.Range(0.82f, 1.18f);
        }

        public void StopSpawning()
        {
            spawningEnabled = false;
        }

        private void SpawnMonster()
        {
            int unlockedCount = Mathf.Clamp(
                currentWave + 1,
                1,
                monsterPrefabs.Length);
            int strongestIndex = unlockedCount - 1;

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
                selectedIndex = UnityEngine.Random.Range(0, unlockedCount);
            }

            GameObject prefab = monsterPrefabs[selectedIndex];
            if (prefab == null)
            {
                return;
            }

            GameObject instance = mapLayout.SpawnMonster(prefab);
            instance.name = $"{prefab.name} (Monster Lv.{currentWave})";
            instance.SetActive(true);

            SideDefenseMonsterUnit monster =
                instance.GetComponent<SideDefenseMonsterUnit>();
            if (monster == null)
            {
                return;
            }

            float healthMultiplier = 1f + (currentWave - 1) * 0.18f;
            float damageMultiplier = 1f + (currentWave - 1) * 0.14f;
            float speedMultiplier =
                1f + Mathf.Min(0.28f, (currentWave - 1) * 0.025f);
            monster.ApplyDifficulty(
                healthMultiplier,
                damageMultiplier,
                speedMultiplier,
                currentWave);
            monster.BeginMarch(alliedTower);
            monster.Died += HandleMonsterDied;
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
        }

        private void RefreshWaveLabel()
        {
            if (waveLabel == null)
            {
                return;
            }

            int unlockedCount = Mathf.Clamp(
                currentWave + 1,
                1,
                Mathf.Max(1, monsterPrefabs.Length));
            float threatMultiplier = 1f + (currentWave - 1) * 0.18f;
            waveLabel.text =
                $"WAVE {currentWave}\n" +
                $"THREAT x{threatMultiplier:0.0}  |  " +
                $"{unlockedCount}/{Mathf.Max(1, monsterPrefabs.Length)} TYPES";
        }
    }
}
