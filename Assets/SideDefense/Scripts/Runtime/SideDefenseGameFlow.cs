using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseGameFlow : MonoBehaviour
    {
        private const string TitleScreenResourcePath =
            "UI/PixelBastionTitleScreen";

        [SerializeField] private SideDefenseTower alliedTower;
        [SerializeField] private SideDefenseMonsterWaveController waveController;
        [SerializeField] private HumanSummonController humanSummonController;
        [SerializeField] private GameObject defeatOverlay;
        [SerializeField] private Button restartButton;

        private bool isDefeated;
        private bool isVictorious;
        private bool isWaitingToStart;
        private GameObject titleScreenOverlay;
        private Button startGameButton;

        public bool IsDefeated => isDefeated;
        public bool IsVictorious => isVictorious;
        public bool IsWaitingToStart => isWaitingToStart;

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
            Time.timeScale = 0f;
            isDefeated = false;
            isVictorious = false;
            isWaitingToStart = true;

            if (defeatOverlay != null)
            {
                defeatOverlay.SetActive(false);
            }

            if (alliedTower != null)
            {
                alliedTower.Destroyed += HandleTowerDestroyed;
            }

            if (waveController != null)
            {
                waveController.AllWavesClearedEvent += HandleAllWavesCleared;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
                restartButton.onClick.AddListener(RestartCurrentScene);
            }

            CreateTitleScreen();
        }

        private void Start()
        {
            if (isWaitingToStart)
            {
                humanSummonController?.SetGameInputEnabled(false);
            }
        }

        private void OnDestroy()
        {
            if (alliedTower != null)
            {
                alliedTower.Destroyed -= HandleTowerDestroyed;
            }

            if (waveController != null)
            {
                waveController.AllWavesClearedEvent -= HandleAllWavesCleared;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartCurrentScene);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(StartBattle);
            }
        }

        private void HandleTowerDestroyed(SideDefenseTower tower)
        {
            TriggerDefeat();
        }

        private void HandleAllWavesCleared()
        {
            TriggerVictory();
        }

        public void StartBattle()
        {
            if (!isWaitingToStart || isDefeated || isVictorious)
            {
                return;
            }

            isWaitingToStart = false;
            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(false);
            }

            humanSummonController?.SetGameInputEnabled(true);
            Time.timeScale = 1f;
        }

        public void TriggerDefeat()
        {
            if (isDefeated || isVictorious)
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

        public void TriggerVictory()
        {
            if (isDefeated || isVictorious)
            {
                return;
            }

            isVictorious = true;
            waveController?.StopSpawning();
            humanSummonController?.SetGameInputEnabled(false);
            ConfigureVictoryOverlay();

            if (defeatOverlay != null)
            {
                defeatOverlay.SetActive(true);
                defeatOverlay.transform.SetAsLastSibling();
            }

            Time.timeScale = 0f;
        }

        private void ConfigureVictoryOverlay()
        {
            if (defeatOverlay == null)
            {
                return;
            }

            Text[] texts = defeatOverlay.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text.gameObject.name == "Defeat Title")
                {
                    text.text = "VICTORY";
                    text.color = new Color(1f, 0.86f, 0.3f, 1f);
                }
                else if (text.gameObject.name == "Defeat Message")
                {
                    int clearedWaveCount =
                        waveController == null
                            ? 100
                            : waveController.MaximumWave;
                    text.text =
                        $"ALL {clearedWaveCount} WAVES CLEARED\n" +
                        "THE PIXEL BASTION STANDS";
                    text.color = new Color(0.65f, 1f, 0.75f, 1f);
                }
            }
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private void CreateTitleScreen()
        {
            Canvas canvas = defeatOverlay == null
                ? FindAnyObjectByType<Canvas>()
                : defeatOverlay.GetComponentInParent<Canvas>();
            Texture2D artwork =
                Resources.Load<Texture2D>(TitleScreenResourcePath);
            if (canvas == null || artwork == null)
            {
                Debug.LogWarning(
                    canvas == null
                        ? "Cannot create the title screen because no Canvas exists."
                        : $"Missing title artwork at Resources/{TitleScreenResourcePath}.",
                    this);
                isWaitingToStart = false;
                Time.timeScale = 1f;
                return;
            }

            artwork.filterMode = FilterMode.Point;
            artwork.wrapMode = TextureWrapMode.Clamp;

            titleScreenOverlay = new GameObject(
                "Pixel Bastion Title Screen",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            titleScreenOverlay.transform.SetParent(canvas.transform, false);
            RectTransform overlayRect =
                titleScreenOverlay.GetComponent<RectTransform>();
            StretchRect(overlayRect);
            Image overlayBackground = titleScreenOverlay.GetComponent<Image>();
            overlayBackground.color = new Color(0.005f, 0.01f, 0.02f, 1f);

            CanvasGroup canvasGroup =
                titleScreenOverlay.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            GameObject artworkObject = new GameObject(
                "Title Artwork",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter));
            artworkObject.transform.SetParent(titleScreenOverlay.transform, false);
            RectTransform artworkRect =
                artworkObject.GetComponent<RectTransform>();
            StretchRect(artworkRect);

            RawImage artworkImage = artworkObject.GetComponent<RawImage>();
            artworkImage.texture = artwork;
            artworkImage.raycastTarget = false;

            AspectRatioFitter aspectFitter =
                artworkObject.GetComponent<AspectRatioFitter>();
            aspectFitter.aspectMode =
                AspectRatioFitter.AspectMode.FitInParent;
            aspectFitter.aspectRatio =
                (float)artwork.width / Mathf.Max(1, artwork.height);

            startGameButton = CreateStartButton(artworkRect);
            startGameButton.onClick.RemoveListener(StartBattle);
            startGameButton.onClick.AddListener(StartBattle);

            titleScreenOverlay.transform.SetAsLastSibling();
        }

        private static Button CreateStartButton(RectTransform parent)
        {
            GameObject buttonObject = new GameObject(
                "START GAME Hit Area",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.355f, 0.285f);
            rect.anchorMax = new Vector2(0.655f, 0.47f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.005f);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.005f);
            colors.highlightedColor =
                new Color(0.25f, 0.9f, 1f, 0.17f);
            colors.pressedColor =
                new Color(1f, 0.78f, 0.22f, 0.3f);
            colors.selectedColor =
                new Color(0.25f, 0.9f, 1f, 0.17f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
