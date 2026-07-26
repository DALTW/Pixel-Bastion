using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseGameFlow : MonoBehaviour
    {
        [SerializeField] private SideDefenseTower alliedTower;
        [SerializeField] private SideDefenseMonsterWaveController waveController;
        [SerializeField] private HumanSummonController humanSummonController;
        [SerializeField] private GameObject defeatOverlay;
        [SerializeField] private Button restartButton;

        private bool isDefeated;

        public bool IsDefeated => isDefeated;

        public void Configure(
            SideDefenseTower tower,
            SideDefenseMonsterWaveController monsters,
            HumanSummonController summonController,
            GameObject overlay,
            Button restart)
        {
            alliedTower = tower;
            waveController = monsters;
            humanSummonController = summonController;
            defeatOverlay = overlay;
            restartButton = restart;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            isDefeated = false;

            if (defeatOverlay != null)
            {
                defeatOverlay.SetActive(false);
            }

            if (alliedTower != null)
            {
                alliedTower.Destroyed += HandleTowerDestroyed;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
                restartButton.onClick.AddListener(RestartCurrentScene);
            }
        }

        private void OnDestroy()
        {
            if (alliedTower != null)
            {
                alliedTower.Destroyed -= HandleTowerDestroyed;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
            }
        }

        private void HandleTowerDestroyed(SideDefenseTower tower)
        {
            TriggerDefeat();
        }

        public void TriggerDefeat()
        {
            if (isDefeated)
            {
                return;
            }

            isDefeated = true;
            waveController?.StopSpawning();
            humanSummonController?.SetGameInputEnabled(false);

            if (defeatOverlay != null)
            {
                defeatOverlay.SetActive(true);
                defeatOverlay.transform.SetAsLastSibling();
            }

            Time.timeScale = 0f;
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }
    }
}
