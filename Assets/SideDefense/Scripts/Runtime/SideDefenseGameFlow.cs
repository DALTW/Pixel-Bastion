using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseGameFlow : MonoBehaviour
    {
        private const string TitleScreenResourcePath =
            "UI/PixelBastionTitleScreenClean";
        private const string StartButtonResourcePath =
            "UI/PixelBastionStartButton";
        private const string ContinueButtonResourcePath =
            "UI/PixelBastionContinueButton";
        private const string OptionsButtonResourcePath =
            "UI/PixelBastionOptionsButton";

        [SerializeField] private SideDefenseTower alliedTower;
        [SerializeField] private SideDefenseMonsterWaveController waveController;
        [SerializeField] private HumanSummonController humanSummonController;
        [SerializeField] private GameObject defeatOverlay;
        [SerializeField] private Button restartButton;

        private bool isDefeated;
        private bool isVictorious;
        private bool isWaitingToStart;
        private bool isPaused;
        private GameObject titleScreenOverlay;
        private Button startGameButton;
        private Button continueGameButton;
        private Button optionsMenuButton;
        private SideDefenseOptionsMenu optionsMenu;
        private SideDefensePauseMenu pauseMenu;

        public bool IsDefeated => isDefeated;
        public bool IsVictorious => isVictorious;
        public bool IsWaitingToStart => isWaitingToStart;
        public bool IsPaused => isPaused;

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
            SideDefenseOptionsSettings.Changed -= HandleOptionsChanged;
            SideDefenseOptionsSettings.Changed += HandleOptionsChanged;
            Time.timeScale = 0f;
            isDefeated = false;
            isVictorious = false;
            isWaitingToStart = true;
            isPaused = false;

            SideDefenseBackgroundMusic.EnsureFor(waveController);

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

        private void Update()
        {
            if (!WasEscapePressed())
            {
                return;
            }

            if (optionsMenu != null && optionsMenu.IsVisible)
            {
                optionsMenu.Hide();
                return;
            }

            if (isWaitingToStart || isDefeated || isVictorious)
            {
                return;
            }

            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
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

            if (continueGameButton != null)
            {
                continueGameButton.onClick.RemoveListener(
                    ContinueSavedGame);
            }

            if (optionsMenuButton != null)
            {
                optionsMenuButton.onClick.RemoveListener(OpenOptionsMenu);
            }

            SideDefenseOptionsSettings.Changed -= HandleOptionsChanged;
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
            isPaused = false;
            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(false);
            }

            optionsMenu?.Hide();
            pauseMenu?.Hide();
            humanSummonController?.SetGameInputEnabled(true);
            Time.timeScale = SideDefenseOptionsSettings.GameplaySpeed;
        }

        private void PauseGame()
        {
            if (isWaitingToStart || isDefeated || isVictorious || isPaused)
            {
                return;
            }

            isPaused = true;
            humanSummonController?.SetGameInputEnabled(false);
            Time.timeScale = 0f;
            pauseMenu?.Show();
        }

        private void ResumeGame()
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
            optionsMenu?.Hide();
            pauseMenu?.Hide();
            humanSummonController?.SetGameInputEnabled(true);
            Time.timeScale = SideDefenseOptionsSettings.GameplaySpeed;
        }

        private void SaveGameAndReturnToTitle()
        {
            if (!isPaused)
            {
                return;
            }

            if (!SideDefenseSaveSystem.Save(
                    alliedTower,
                    waveController,
                    humanSummonController))
            {
                Debug.LogWarning("Unable to save the current battle.", this);
                return;
            }

            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private void ContinueSavedGame()
        {
            if (!isWaitingToStart ||
                !SideDefenseSaveSystem.TryLoad(out SideDefenseSaveData data))
            {
                return;
            }

            Time.timeScale = 0f;
            alliedTower?.RestoreHealth(data.towerHealth);
            humanSummonController?.RestoreSaveData(data.human);
            waveController?.RestoreSaveData(data.wave);

            isWaitingToStart = false;
            isPaused = false;
            isDefeated = false;
            isVictorious = false;
            if (titleScreenOverlay != null)
            {
                titleScreenOverlay.SetActive(false);
            }

            optionsMenu?.Hide();
            pauseMenu?.Hide();
            humanSummonController?.SetGameInputEnabled(true);
            Time.timeScale = SideDefenseOptionsSettings.GameplaySpeed;
        }

        private void OpenOptionsMenu()
        {
            optionsMenu?.Show();
        }

        private void HandleOptionsChanged()
        {
            if (!isWaitingToStart &&
                !isDefeated &&
                !isVictorious &&
                Time.timeScale > 0f)
            {
                Time.timeScale = SideDefenseOptionsSettings.GameplaySpeed;
            }
        }

        public void TriggerDefeat()
        {
            if (isDefeated || isVictorious)
            {
                return;
            }

            isDefeated = true;
            isPaused = false;
            optionsMenu?.Hide();
            pauseMenu?.Hide();
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
            isPaused = false;
            optionsMenu?.Hide();
            pauseMenu?.Hide();
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
            Texture2D startButtonArtwork =
                Resources.Load<Texture2D>(StartButtonResourcePath);
            Texture2D continueButtonArtwork =
                Resources.Load<Texture2D>(ContinueButtonResourcePath);
            Texture2D optionsButtonArtwork =
                Resources.Load<Texture2D>(OptionsButtonResourcePath);
            if (canvas == null || artwork == null)
            {
                Debug.LogWarning(
                    canvas == null
                        ? "Cannot create the title screen because no Canvas exists."
                        : $"Missing title artwork at Resources/{TitleScreenResourcePath}.",
                    this);
                isWaitingToStart = false;
                Time.timeScale = SideDefenseOptionsSettings.GameplaySpeed;
                return;
            }

            artwork.filterMode = FilterMode.Point;
            artwork.wrapMode = TextureWrapMode.Clamp;
            ConfigureMenuTexture(startButtonArtwork);
            ConfigureMenuTexture(continueButtonArtwork);
            ConfigureMenuTexture(optionsButtonArtwork);

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

            Vector2 commonButtonAnchorMin = new Vector2(0.409f, 0f);
            Vector2 commonButtonAnchorMax = new Vector2(0.591f, 0f);

            Vector2 startAnchorMin = new Vector2(
                commonButtonAnchorMin.x,
                0.315f);
            Vector2 startAnchorMax = new Vector2(
                commonButtonAnchorMax.x,
                0.445f);
            CreateDecorativeButtonArtwork(
                artworkRect,
                startButtonArtwork,
                "START GAME Button Artwork",
                startAnchorMin,
                startAnchorMax);
            startGameButton = CreateMenuHitAreaButton(
                artworkRect,
                "START GAME Hit Area",
                startAnchorMin,
                startAnchorMax);
            startGameButton.onClick.RemoveListener(StartBattle);
            startGameButton.onClick.AddListener(StartBattle);

            Vector2 continueAnchorMin = new Vector2(
                commonButtonAnchorMin.x,
                0.175f);
            Vector2 continueAnchorMax = new Vector2(
                commonButtonAnchorMax.x,
                0.305f);
            RawImage continueArtworkImage =
                CreateDecorativeButtonArtwork(
                artworkRect,
                continueButtonArtwork,
                "CONTINUE Button Artwork",
                continueAnchorMin,
                continueAnchorMax);
            bool hasSavedGame = SideDefenseSaveSystem.HasSave;
            if (continueArtworkImage != null && !hasSavedGame)
            {
                continueArtworkImage.color =
                    new Color(0.45f, 0.5f, 0.55f, 1f);
            }

            continueGameButton = CreateMenuHitAreaButton(
                artworkRect,
                "CONTINUE Hit Area",
                continueAnchorMin,
                continueAnchorMax);
            continueGameButton.interactable = hasSavedGame;
            continueGameButton.onClick.RemoveListener(ContinueSavedGame);
            continueGameButton.onClick.AddListener(ContinueSavedGame);

            CreateDecorativeButtonArtwork(
                artworkRect,
                optionsButtonArtwork,
                "OPTIONS Button Artwork",
                new Vector2(commonButtonAnchorMin.x, 0.035f),
                new Vector2(commonButtonAnchorMax.x, 0.165f));
            optionsMenuButton = CreateMenuHitAreaButton(
                artworkRect,
                "OPTIONS Hit Area",
                new Vector2(commonButtonAnchorMin.x, 0.035f),
                new Vector2(commonButtonAnchorMax.x, 0.165f));
            optionsMenuButton.onClick.RemoveListener(OpenOptionsMenu);
            optionsMenuButton.onClick.AddListener(OpenOptionsMenu);

            optionsMenu = SideDefenseOptionsMenu.Create(
                canvas.transform);
            pauseMenu = SideDefensePauseMenu.Create(
                canvas.transform,
                OpenOptionsMenu,
                SaveGameAndReturnToTitle);

            titleScreenOverlay.transform.SetAsLastSibling();
        }

        private static void ConfigureMenuTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        private static RawImage CreateDecorativeButtonArtwork(
            RectTransform parent,
            Texture2D texture,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            if (texture == null)
            {
                Debug.LogWarning($"Missing menu artwork: {objectName}.");
                return null;
            }

            GameObject artworkObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(RawImage));
            artworkObject.transform.SetParent(parent, false);

            RectTransform rect =
                artworkObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = artworkObject.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private static bool WasEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null &&
                   Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static Button CreateMenuHitAreaButton(
            RectTransform parent,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
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
