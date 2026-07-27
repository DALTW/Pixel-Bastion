using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class HumanSummonController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private SideDefenseMapLayout mapLayout;
        [SerializeField] private Text coinLabel;
        [SerializeField] private Text selectedUnitLabel;
        [SerializeField] private Button summonButton;
        [SerializeField] private HumanSummonCard[] cards =
            Array.Empty<HumanSummonCard>();

        [Header("Gold Coins")]
        [SerializeField, Min(0)] private int startingCoins = 200;
        [SerializeField, Min(0)] private int passiveCoinsPerSecond = 1;

        [Header("Human Unlocks")]
        [SerializeField, Min(1)] private int initiallyUnlockedHumanTypes = 2;
        [SerializeField, Min(1)] private int maxActiveHumans = 24;

        [Header("Human Upgrades")]
        [SerializeField, Min(1)] private int maxUpgradeLevel = 10;
        [SerializeField, Min(1)] private int initiallyUnlockedUpgradeLevel = 5;
        [SerializeField, Min(1)] private int earlyUpgradeLevelCap = 5;
        [SerializeField, Min(0f)] private float healthBonusPerLevel = 0.15f;
        [SerializeField, Min(0f)] private float powerBonusPerLevel = 0.15f;
        [SerializeField, Min(0f)] private float lateHealthBonusPerLevel = 0.1f;
        [SerializeField, Min(0f)] private float latePowerBonusPerLevel = 0.1f;
        [SerializeField, Min(0f)] private float upgradeCostGrowthPerLevel = 0.5f;
        [SerializeField, Min(0)] private int summonCostReductionPerUpgrade = 4;
        [SerializeField, Min(0)] private int summonCostReductionLevelCap = 5;

        private HumanSummonCard selectedCard;
        private int currentCoins;
        private int[] upgradeLevels = Array.Empty<int>();
        private int[] humanUnlockOrder = Array.Empty<int>();
        private int unlockedHumanCount;
        private int unlockedUpgradeLevel;
        private float passiveCoinElapsedTime;
        private bool gameInputEnabled = true;
        private Button upgradeMenuButton;
        private Button confirmUpgradeButton;
        private Button closeUpgradeButton;
        private GameObject upgradePanel;
        private Text upgradeMenuLabel;
        private Text upgradeTitleLabel;
        private Text upgradeDetailsLabel;
        private Text upgradeCostLabel;
        private Text confirmUpgradeLabel;

        public int CurrentCoins => currentCoins;
        public HumanSummonCard SelectedCard => selectedCard;
        public bool GameInputEnabled => gameInputEnabled;
        public int MaxUpgradeLevel => maxUpgradeLevel;
        public int UnlockedUpgradeLevel => unlockedUpgradeLevel;
        public int UnlockedHumanCount => unlockedHumanCount;
        public int ActiveHumanCount => CountActiveHumans();
        public int MaxActiveHumans => maxActiveHumans;

        public void Configure(
            SideDefenseMapLayout layout,
            Text coinText,
            Text selectedText,
            Button deployButton,
            HumanSummonCard[] summonCards)
        {
            mapLayout = layout;
            coinLabel = coinText;
            selectedUnitLabel = selectedText;
            summonButton = deployButton;
            cards = summonCards ?? Array.Empty<HumanSummonCard>();
        }

        private void Awake()
        {
            currentCoins = Mathf.Max(0, startingCoins);
            upgradeLevels = new int[cards == null ? 0 : cards.Length];
            unlockedUpgradeLevel = Mathf.Clamp(
                initiallyUnlockedUpgradeLevel,
                1,
                maxUpgradeLevel);
            passiveCoinElapsedTime = 0f;
            gameInputEnabled = true;
            EnsureUpgradeUi();

            if (summonButton != null)
            {
                summonButton.onClick.RemoveListener(SummonSelected);
                summonButton.onClick.AddListener(SummonSelected);
            }

            if (upgradeMenuButton != null)
            {
                upgradeMenuButton.onClick.RemoveListener(ToggleUpgradePanel);
                upgradeMenuButton.onClick.AddListener(ToggleUpgradePanel);
            }

            if (confirmUpgradeButton != null)
            {
                confirmUpgradeButton.onClick.RemoveListener(UpgradeSelected);
                confirmUpgradeButton.onClick.AddListener(UpgradeSelected);
            }

            if (closeUpgradeButton != null)
            {
                closeUpgradeButton.onClick.RemoveListener(CloseUpgradePanel);
                closeUpgradeButton.onClick.AddListener(CloseUpgradePanel);
            }

            foreach (HumanSummonCard card in cards)
            {
                card?.Bind(this);
            }

            BuildHumanUnlockOrder();
            unlockedHumanCount = Mathf.Clamp(
                initiallyUnlockedHumanTypes,
                humanUnlockOrder.Length > 0 ? 1 : 0,
                humanUnlockOrder.Length);
            ApplyHumanUnlockStates();

            if (humanUnlockOrder.Length > 0)
            {
                SelectCard(cards[humanUnlockOrder[0]]);
            }

            RefreshUi();
        }

        private void Update()
        {
            GrantPassiveCoins(Time.deltaTime);

            foreach (HumanSummonCard card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                card.Tick(
                    Time.deltaTime,
                    gameInputEnabled &&
                    card.IsUnlocked &&
                    card.IsCoolingDown == false);
            }

            RefreshUi();
        }

        private void GrantPassiveCoins(float deltaTime)
        {
            if (!gameInputEnabled ||
                passiveCoinsPerSecond <= 0 ||
                deltaTime <= 0f)
            {
                return;
            }

            passiveCoinElapsedTime += deltaTime;
            int completedSeconds = Mathf.FloorToInt(passiveCoinElapsedTime);
            if (completedSeconds <= 0)
            {
                return;
            }

            passiveCoinElapsedTime -= completedSeconds;
            AddCoins(completedSeconds * passiveCoinsPerSecond);
        }

        public void SelectCard(HumanSummonCard card)
        {
            if (card == null || !card.IsUnlocked)
            {
                return;
            }

            selectedCard = card;
            foreach (HumanSummonCard summonCard in cards)
            {
                summonCard?.SetSelected(summonCard == selectedCard);
            }

            RefreshUi();
        }

        public void SummonSelected()
        {
            if (!CanSummonSelected())
            {
                return;
            }

            currentCoins -= GetSummonCost(selectedCard);
            GameObject instance = mapLayout.SpawnHuman(selectedCard.HumanPrefab);
            instance.name = $"{selectedCard.DisplayName} (Human)";
            instance.SetActive(true);

            SideDefenseHumanUnit humanUnit =
                instance.GetComponent<SideDefenseHumanUnit>();
            if (humanUnit != null)
            {
                humanUnit.ApplyUpgradeLevel(
                    GetUpgradeLevel(selectedCard),
                    GetHealthUpgradeMultiplier(
                        GetUpgradeLevel(selectedCard)),
                    GetPowerUpgradeMultiplier(
                        GetUpgradeLevel(selectedCard)));
                humanUnit.BeginMarch(mapLayout.WorldRight - 1.2f);
            }

            selectedCard.BeginCooldown();
            RefreshUi();
        }

        private bool CanSummonSelected()
        {
            return selectedCard != null &&
                   selectedCard.IsUnlocked &&
                   gameInputEnabled &&
                   CountActiveHumans() < maxActiveHumans &&
                   selectedCard.HumanPrefab != null &&
                   mapLayout != null &&
                   !selectedCard.IsCoolingDown &&
                   currentCoins >= GetSummonCost(selectedCard);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentCoins += amount;
            RefreshUi();
        }

        public bool UnlockNextHuman()
        {
            if (humanUnlockOrder == null ||
                humanUnlockOrder.Length == 0)
            {
                BuildHumanUnlockOrder();
            }

            if (unlockedHumanCount >= humanUnlockOrder.Length)
            {
                return false;
            }

            int unlockedCardIndex =
                humanUnlockOrder[unlockedHumanCount];
            unlockedHumanCount++;
            ApplyHumanUnlockStates();

            HumanSummonCard unlockedCard = cards[unlockedCardIndex];
            Debug.Log(
                $"Human unlocked after boss clear: " +
                $"{unlockedCard.DisplayName} " +
                $"(cost {unlockedCard.CoinCost}).",
                unlockedCard);
            RefreshUi();
            return true;
        }

        public bool UnlockNextUpgradeLevel()
        {
            if (unlockedUpgradeLevel >= maxUpgradeLevel)
            {
                return false;
            }

            unlockedUpgradeLevel++;
            Debug.Log(
                $"Human upgrade level {unlockedUpgradeLevel} unlocked " +
                "after boss clear.",
                this);
            RefreshUi();
            return true;
        }

        public int GetUpgradeLevel(HumanSummonCard card)
        {
            if (card == null)
            {
                return 0;
            }

            int cardIndex = Array.IndexOf(cards, card);
            return cardIndex < 0 || cardIndex >= upgradeLevels.Length
                ? 0
                : upgradeLevels[cardIndex];
        }

        public int GetUpgradeCost(HumanSummonCard card)
        {
            if (card == null)
            {
                return 0;
            }

            int currentLevel = GetUpgradeLevel(card);
            float costMultiplier =
                1f + currentLevel * upgradeCostGrowthPerLevel;
            return Mathf.Max(
                1,
                Mathf.CeilToInt(card.CoinCost * costMultiplier));
        }

        public int GetSummonCost(HumanSummonCard card)
        {
            if (card == null)
            {
                return 0;
            }

            return GetSummonCostAtLevel(
                card,
                GetUpgradeLevel(card));
        }

        public void UpgradeSelected()
        {
            if (!CanUpgradeSelected())
            {
                return;
            }

            int cardIndex = Array.IndexOf(cards, selectedCard);
            int upgradeCost = GetUpgradeCost(selectedCard);
            currentCoins -= upgradeCost;
            upgradeLevels[cardIndex]++;
            ApplyUpgradeToActiveUnits(
                selectedCard.DisplayName,
                upgradeLevels[cardIndex]);
            RefreshUi();
        }

        private bool CanUpgradeSelected()
        {
            if (!gameInputEnabled ||
                selectedCard == null ||
                !selectedCard.IsUnlocked)
            {
                return false;
            }

            int cardIndex = Array.IndexOf(cards, selectedCard);
            if (cardIndex < 0 || cardIndex >= upgradeLevels.Length)
            {
                return false;
            }

            return upgradeLevels[cardIndex] < maxUpgradeLevel &&
                   upgradeLevels[cardIndex] < unlockedUpgradeLevel &&
                   currentCoins >= GetUpgradeCost(selectedCard);
        }

        private int GetSummonCostAtLevel(
            HumanSummonCard card,
            int level)
        {
            if (card == null)
            {
                return 0;
            }

            int discountedLevels = Mathf.Min(
                Mathf.Max(0, level),
                summonCostReductionLevelCap);
            return Mathf.Max(
                0,
                card.CoinCost -
                discountedLevels * summonCostReductionPerUpgrade);
        }

        private float GetHealthUpgradeMultiplier(int level)
        {
            return CalculateUpgradeMultiplier(
                level,
                healthBonusPerLevel,
                lateHealthBonusPerLevel);
        }

        private float GetPowerUpgradeMultiplier(int level)
        {
            return CalculateUpgradeMultiplier(
                level,
                powerBonusPerLevel,
                latePowerBonusPerLevel);
        }

        private float CalculateUpgradeMultiplier(
            int level,
            float earlyBonusPerLevel,
            float lateBonusPerLevel)
        {
            int safeLevel = Mathf.Clamp(level, 0, maxUpgradeLevel);
            int earlyLevels = Mathf.Min(
                safeLevel,
                earlyUpgradeLevelCap);
            int lateLevels = Mathf.Max(
                0,
                safeLevel - earlyUpgradeLevelCap);
            return 1f +
                   earlyLevels * Mathf.Max(0f, earlyBonusPerLevel) +
                   lateLevels * Mathf.Max(0f, lateBonusPerLevel);
        }

        private void ApplyUpgradeToActiveUnits(
            string unitDisplayName,
            int level)
        {
            IReadOnlyList<SideDefenseHumanUnit> activeUnits =
                SideDefenseHumanUnit.ActiveUnits;
            for (int index = 0; index < activeUnits.Count; index++)
            {
                SideDefenseHumanUnit unit = activeUnits[index];
                if (unit == null ||
                    !unit.IsAlive ||
                    !string.Equals(
                        unit.DisplayName,
                        unitDisplayName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                unit.ApplyUpgradeLevel(
                    level,
                    GetHealthUpgradeMultiplier(level),
                    GetPowerUpgradeMultiplier(level));
            }
        }

        public void SetGameInputEnabled(bool isEnabled)
        {
            gameInputEnabled = isEnabled;
            if (!isEnabled)
            {
                CloseUpgradePanel();
            }

            RefreshUi();
        }

        private void RefreshUi()
        {
            if (coinLabel != null)
            {
                coinLabel.text = currentCoins.ToString();
            }

            if (selectedUnitLabel != null)
            {
                selectedUnitLabel.text =
                    selectedCard == null
                        ? $"SELECT A HUMAN  |  FIELD " +
                          $"{CountActiveHumans()}/{maxActiveHumans}"
                        : $"{selectedCard.DisplayName}  |  " +
                          $"COST {GetSummonCost(selectedCard)}  |  " +
                          $"FIELD {CountActiveHumans()}/{maxActiveHumans}";
            }

            foreach (HumanSummonCard card in cards)
            {
                card?.SetDisplayedCoinCost(GetSummonCost(card));
            }

            if (summonButton != null)
            {
                summonButton.interactable = CanSummonSelected();
            }

            RefreshUpgradeUi();
        }

        private void ToggleUpgradePanel()
        {
            if (!gameInputEnabled ||
                selectedCard == null ||
                !selectedCard.IsUnlocked ||
                upgradePanel == null)
            {
                return;
            }

            bool shouldOpen = !upgradePanel.activeSelf;
            upgradePanel.SetActive(shouldOpen);
            if (shouldOpen)
            {
                upgradePanel.transform.SetAsLastSibling();
            }

            RefreshUpgradeUi();
        }

        private void CloseUpgradePanel()
        {
            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }
        }

        private void RefreshUpgradeUi()
        {
            int currentLevel = GetUpgradeLevel(selectedCard);
            bool hasSelection =
                selectedCard != null && selectedCard.IsUnlocked;
            bool isMaxLevel = currentLevel >= maxUpgradeLevel;
            bool isAtUnlockedCap =
                !isMaxLevel &&
                currentLevel >= unlockedUpgradeLevel;

            if (upgradeMenuButton != null)
            {
                upgradeMenuButton.interactable =
                    gameInputEnabled && hasSelection;
            }

            if (upgradeMenuLabel != null)
            {
                upgradeMenuLabel.text =
                    hasSelection
                        ? $"UPGRADE\nLV {currentLevel}/{maxUpgradeLevel}"
                        : "UPGRADE";
            }

            if (!hasSelection)
            {
                if (confirmUpgradeButton != null)
                {
                    confirmUpgradeButton.interactable = false;
                }

                return;
            }

            if (upgradeTitleLabel != null)
            {
                upgradeTitleLabel.text =
                    $"{selectedCard.DisplayName.ToUpperInvariant()}  " +
                    $"LV {currentLevel}/{maxUpgradeLevel}  " +
                    $"CAP {unlockedUpgradeLevel}";
            }

            int currentHealthBonus =
                Mathf.RoundToInt(
                    (GetHealthUpgradeMultiplier(currentLevel) - 1f) *
                    100f);
            int currentPowerBonus =
                Mathf.RoundToInt(
                    (GetPowerUpgradeMultiplier(currentLevel) - 1f) *
                    100f);
            if (upgradeDetailsLabel != null)
            {
                int currentSummonCost = GetSummonCost(selectedCard);
                int nextLevel = Mathf.Min(
                    maxUpgradeLevel,
                    currentLevel + 1);
                int nextSummonCost = GetSummonCostAtLevel(
                    selectedCard,
                    nextLevel);
                int nextHealthBonus = Mathf.RoundToInt(
                    (GetHealthUpgradeMultiplier(nextLevel) - 1f) *
                    100f);
                int nextPowerBonus = Mathf.RoundToInt(
                    (GetPowerUpgradeMultiplier(nextLevel) - 1f) *
                    100f);
                string progressionText = isMaxLevel
                    ? "THIS HUMAN HAS REACHED MAX LEVEL"
                    : isAtUnlockedCap
                        ? "NEXT LEVEL LOCKED - DEFEAT A LATE BOSS"
                        : $"NEXT LEVEL  HP +{nextHealthBonus}%  |  " +
                          $"POWER +{nextPowerBonus}%";
                string summonCostText =
                    $"\nSUMMON COST  {currentSummonCost}";
                if (!isMaxLevel &&
                    nextSummonCost != currentSummonCost)
                {
                    summonCostText += $"  >  {nextSummonCost}";
                }
                else if (currentLevel >= summonCostReductionLevelCap)
                {
                    summonCostText += "  (MAX DISCOUNT)";
                }

                upgradeDetailsLabel.text =
                    $"CURRENT  HP +{currentHealthBonus}%  |  " +
                    $"POWER +{currentPowerBonus}%\n" +
                    progressionText +
                    summonCostText;
            }

            if (upgradeCostLabel != null)
            {
                upgradeCostLabel.text = isMaxLevel
                    ? "MAX LEVEL"
                    : isAtUnlockedCap
                        ? $"LEVEL CAP {unlockedUpgradeLevel}"
                        : $"COST  {GetUpgradeCost(selectedCard)} COINS";
            }

            if (confirmUpgradeButton != null)
            {
                confirmUpgradeButton.interactable = CanUpgradeSelected();
            }

            if (confirmUpgradeLabel != null)
            {
                confirmUpgradeLabel.text =
                    isMaxLevel
                        ? "MAX"
                        : isAtUnlockedCap
                            ? "LOCKED"
                            : "UPGRADE";
            }
        }

        private void EnsureUpgradeUi()
        {
            if (summonButton == null || upgradeMenuButton != null)
            {
                return;
            }

            RectTransform summonRect =
                summonButton.GetComponent<RectTransform>();
            if (summonRect == null)
            {
                return;
            }

            summonRect.anchoredPosition = new Vector2(-12f, 54f);
            summonRect.sizeDelta = new Vector2(132f, 84f);

            Font font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform buttonParent = summonButton.transform.parent;

            upgradeMenuButton = CreateRuntimeButton(
                "UPGRADE Button",
                buttonParent,
                new Color(0.08f, 0.62f, 0.72f, 1f),
                new Vector2(1f, 0f),
                new Vector2(-12f, 12f),
                new Vector2(132f, 34f),
                new Vector2(1f, 0f));
            upgradeMenuLabel = CreateRuntimeText(
                "UPGRADE Label",
                upgradeMenuButton.transform,
                font,
                15,
                TextAnchor.MiddleCenter,
                new Color(0.01f, 0.08f, 0.11f, 1f));
            StretchRuntimeRect(
                upgradeMenuLabel.rectTransform,
                new Vector2(4f, 2f),
                new Vector2(-4f, -2f));

            upgradePanel = new GameObject(
                "Human Upgrade Window",
                typeof(RectTransform),
                typeof(Image),
                typeof(Outline));
            upgradePanel.transform.SetParent(transform, false);
            RectTransform panelRect =
                upgradePanel.GetComponent<RectTransform>();
            SetRuntimeRect(
                panelRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(470f, 300f),
                new Vector2(0.5f, 0.5f));
            Image panelImage = upgradePanel.GetComponent<Image>();
            panelImage.color = new Color(0.015f, 0.11f, 0.16f, 0.98f);
            Outline panelOutline = upgradePanel.GetComponent<Outline>();
            panelOutline.effectColor = new Color(0.1f, 0.85f, 0.95f, 1f);
            panelOutline.effectDistance = new Vector2(3f, -3f);

            upgradeTitleLabel = CreateRuntimeText(
                "Upgrade Title",
                upgradePanel.transform,
                font,
                26,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.85f, 0.25f, 1f));
            SetRuntimeRect(
                upgradeTitleLabel.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(380f, 46f),
                new Vector2(0.5f, 1f));

            upgradeDetailsLabel = CreateRuntimeText(
                "Upgrade Details",
                upgradePanel.transform,
                font,
                18,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 0.96f, 1f, 1f));
            SetRuntimeRect(
                upgradeDetailsLabel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 25f),
                new Vector2(420f, 100f),
                new Vector2(0.5f, 0.5f));

            upgradeCostLabel = CreateRuntimeText(
                "Upgrade Cost",
                upgradePanel.transform,
                font,
                20,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.82f, 0.2f, 1f));
            SetRuntimeRect(
                upgradeCostLabel.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 86f),
                new Vector2(300f, 36f),
                new Vector2(0.5f, 0f));

            confirmUpgradeButton = CreateRuntimeButton(
                "Confirm Upgrade Button",
                upgradePanel.transform,
                new Color(0.95f, 0.61f, 0.08f, 1f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(230f, 52f),
                new Vector2(0.5f, 0f));
            confirmUpgradeLabel = CreateRuntimeText(
                "Confirm Upgrade Label",
                confirmUpgradeButton.transform,
                font,
                22,
                TextAnchor.MiddleCenter,
                new Color(0.12f, 0.08f, 0.01f, 1f));
            StretchRuntimeRect(
                confirmUpgradeLabel.rectTransform,
                new Vector2(4f, 4f),
                new Vector2(-4f, -4f));

            closeUpgradeButton = CreateRuntimeButton(
                "Close Upgrade Button",
                upgradePanel.transform,
                new Color(0.55f, 0.12f, 0.12f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-12f, -12f),
                new Vector2(40f, 36f),
                new Vector2(1f, 1f));
            Text closeLabel = CreateRuntimeText(
                "Close Label",
                closeUpgradeButton.transform,
                font,
                22,
                TextAnchor.MiddleCenter,
                Color.white);
            closeLabel.text = "X";
            StretchRuntimeRect(
                closeLabel.rectTransform,
                Vector2.zero,
                Vector2.zero);

            upgradePanel.SetActive(false);
        }

        private static Button CreateRuntimeButton(
            string objectName,
            Transform parent,
            Color color,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            GameObject buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect =
                buttonObject.GetComponent<RectTransform>();
            SetRuntimeRect(
                rect,
                anchor,
                anchoredPosition,
                size,
                pivot);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.65f, 0.82f, 0.85f, 1f);
            colors.disabledColor = new Color(0.3f, 0.34f, 0.36f, 0.75f);
            button.colors = colors;

            Outline outline = buttonObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.7f, 1f, 1f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return button;
        }

        private static Text CreateRuntimeText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 6);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static void SetRuntimeRect(
            RectTransform rect,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void StretchRuntimeRect(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void BuildHumanUnlockOrder()
        {
            List<int> orderedIndices = new List<int>();
            if (cards != null)
            {
                for (int index = 0; index < cards.Length; index++)
                {
                    if (cards[index] != null)
                    {
                        orderedIndices.Add(index);
                    }
                }
            }

            orderedIndices.Sort((leftIndex, rightIndex) =>
            {
                HumanSummonCard leftCard = cards[leftIndex];
                HumanSummonCard rightCard = cards[rightIndex];
                int costComparison =
                    leftCard.CoinCost.CompareTo(rightCard.CoinCost);
                return costComparison != 0
                    ? costComparison
                    : leftIndex.CompareTo(rightIndex);
            });
            humanUnlockOrder = orderedIndices.ToArray();
        }

        private void ApplyHumanUnlockStates()
        {
            if (cards == null)
            {
                return;
            }

            foreach (HumanSummonCard card in cards)
            {
                card?.SetUnlocked(false);
            }

            int appliedUnlockCount = Mathf.Min(
                unlockedHumanCount,
                humanUnlockOrder.Length);
            for (int index = 0; index < appliedUnlockCount; index++)
            {
                cards[humanUnlockOrder[index]]?.SetUnlocked(true);
            }
        }

        private static int CountActiveHumans()
        {
            int activeCount = 0;
            IReadOnlyList<SideDefenseHumanUnit> activeUnits =
                SideDefenseHumanUnit.ActiveUnits;
            for (int index = 0; index < activeUnits.Count; index++)
            {
                SideDefenseHumanUnit unit = activeUnits[index];
                if (unit != null && unit.IsAlive)
                {
                    activeCount++;
                }
            }

            return activeCount;
        }

        private void OnValidate()
        {
            startingCoins = Mathf.Max(0, startingCoins);
            passiveCoinsPerSecond = Mathf.Max(0, passiveCoinsPerSecond);
            initiallyUnlockedHumanTypes =
                Mathf.Max(1, initiallyUnlockedHumanTypes);
            maxActiveHumans = Mathf.Max(1, maxActiveHumans);
            maxUpgradeLevel = Mathf.Max(1, maxUpgradeLevel);
            initiallyUnlockedUpgradeLevel = Mathf.Clamp(
                initiallyUnlockedUpgradeLevel,
                1,
                maxUpgradeLevel);
            earlyUpgradeLevelCap = Mathf.Clamp(
                earlyUpgradeLevelCap,
                1,
                maxUpgradeLevel);
            healthBonusPerLevel = Mathf.Max(0f, healthBonusPerLevel);
            powerBonusPerLevel = Mathf.Max(0f, powerBonusPerLevel);
            lateHealthBonusPerLevel =
                Mathf.Max(0f, lateHealthBonusPerLevel);
            latePowerBonusPerLevel =
                Mathf.Max(0f, latePowerBonusPerLevel);
            upgradeCostGrowthPerLevel =
                Mathf.Max(0f, upgradeCostGrowthPerLevel);
            summonCostReductionPerUpgrade =
                Mathf.Max(0, summonCostReductionPerUpgrade);
            summonCostReductionLevelCap = Mathf.Clamp(
                summonCostReductionLevelCap,
                0,
                maxUpgradeLevel);
        }
    }
}
