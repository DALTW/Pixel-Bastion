using System;
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

        private HumanSummonCard selectedCard;
        private int currentCoins;
        private bool gameInputEnabled = true;

        public int CurrentCoins => currentCoins;
        public HumanSummonCard SelectedCard => selectedCard;
        public bool GameInputEnabled => gameInputEnabled;

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
            gameInputEnabled = true;

            if (summonButton != null)
            {
                summonButton.onClick.RemoveListener(SummonSelected);
                summonButton.onClick.AddListener(SummonSelected);
            }

            foreach (HumanSummonCard card in cards)
            {
                card?.Bind(this);
            }

            if (cards.Length > 0 && cards[0] != null)
            {
                SelectCard(cards[0]);
            }

            RefreshUi();
        }

        private void Update()
        {
            foreach (HumanSummonCard card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                card.Tick(
                    Time.deltaTime,
                    card.IsCoolingDown == false);
            }

            RefreshUi();
        }

        public void SelectCard(HumanSummonCard card)
        {
            if (card == null)
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

            currentCoins -= selectedCard.CoinCost;
            GameObject instance = mapLayout.SpawnHuman(selectedCard.HumanPrefab);
            instance.name = $"{selectedCard.DisplayName} (Human)";
            instance.SetActive(true);

            SideDefenseHumanUnit humanUnit =
                instance.GetComponent<SideDefenseHumanUnit>();
            if (humanUnit != null)
            {
                humanUnit.BeginMarch(mapLayout.WorldRight - 1.2f);
            }

            selectedCard.BeginCooldown();
            RefreshUi();
        }

        private bool CanSummonSelected()
        {
            return selectedCard != null &&
                   gameInputEnabled &&
                   selectedCard.HumanPrefab != null &&
                   mapLayout != null &&
                   !selectedCard.IsCoolingDown &&
                   currentCoins >= selectedCard.CoinCost;
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

        public void SetGameInputEnabled(bool isEnabled)
        {
            gameInputEnabled = isEnabled;
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
                        ? "SELECT A HUMAN"
                        : $"{selectedCard.DisplayName}  |  COST {selectedCard.CoinCost}";
            }

            if (summonButton != null)
            {
                summonButton.interactable = CanSummonSelected();
            }
        }
    }
}
