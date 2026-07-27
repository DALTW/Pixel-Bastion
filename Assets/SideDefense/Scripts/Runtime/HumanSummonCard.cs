using UnityEngine;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class HumanSummonCard : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField, Min(0)] private int coinCost;
        [SerializeField] private GameObject humanPrefab;
        [SerializeField] private Button button;
        [SerializeField] private Outline selectionOutline;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private Text costLabel;
        [SerializeField, Min(0f)] private float cooldownDuration = 0.75f;

        private HumanSummonController controller;
        private float cooldownRemaining;
        private bool isUnlocked = true;

        public string DisplayName => displayName;
        public int CoinCost => coinCost;
        public GameObject HumanPrefab => humanPrefab;
        public bool IsCoolingDown => cooldownRemaining > 0f;
        public bool IsUnlocked => isUnlocked;

        public void Configure(
            string unitName,
            int cost,
            GameObject prefab,
            Button cardButton,
            Outline outline,
            Image cooldownImage)
        {
            displayName = unitName;
            coinCost = Mathf.Max(0, cost);
            humanPrefab = prefab;
            button = cardButton;
            selectionOutline = outline;
            cooldownOverlay = cooldownImage;
            SetSelected(false);
            UpdateCooldownVisual();
        }

        public void Bind(HumanSummonController summonController)
        {
            controller = summonController;
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (costLabel == null)
            {
                Text[] labels = GetComponentsInChildren<Text>(true);
                foreach (Text label in labels)
                {
                    if (label.gameObject.name == "Coin Cost")
                    {
                        costLabel = label;
                        break;
                    }
                }
            }

            button.onClick.RemoveListener(SelectThisCard);
            button.onClick.AddListener(SelectThisCard);
            RefreshLockVisual();
        }

        public void SetDisplayedCoinCost(int cost)
        {
            if (costLabel != null)
            {
                costLabel.text = isUnlocked
                    ? Mathf.Max(0, cost).ToString()
                    : "LOCKED";
                costLabel.color = isUnlocked
                    ? new Color(1f, 0.84f, 0.25f, 1f)
                    : new Color(0.58f, 0.64f, 0.68f, 1f);
            }
        }

        public void Tick(float deltaTime, bool canSelect)
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
            if (button != null)
            {
                button.interactable = isUnlocked && canSelect;
            }

            UpdateCooldownVisual();
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            if (!isUnlocked)
            {
                cooldownRemaining = 0f;
                SetSelected(false);
            }

            RefreshLockVisual();
        }

        public void BeginCooldown()
        {
            cooldownRemaining = cooldownDuration;
            UpdateCooldownVisual();
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline == null)
            {
                return;
            }

            bool shouldShowSelection = selected && isUnlocked;
            selectionOutline.enabled = shouldShowSelection;
            selectionOutline.effectColor = new Color(1f, 0.82f, 0.2f, 1f);
            selectionOutline.effectDistance = shouldShowSelection
                ? new Vector2(4f, -4f)
                : Vector2.zero;
        }

        private void SelectThisCard()
        {
            if (isUnlocked)
            {
                controller?.SelectCard(this);
            }
        }

        private void RefreshLockVisual()
        {
            if (costLabel != null)
            {
                costLabel.text = isUnlocked
                    ? Mathf.Max(0, coinCost).ToString()
                    : "LOCKED";
                costLabel.color = isUnlocked
                    ? new Color(1f, 0.84f, 0.25f, 1f)
                    : new Color(0.58f, 0.64f, 0.68f, 1f);
            }
        }

        private void UpdateCooldownVisual()
        {
            if (cooldownOverlay == null)
            {
                return;
            }

            cooldownOverlay.fillAmount =
                cooldownDuration <= 0f
                    ? 0f
                    : Mathf.Clamp01(cooldownRemaining / cooldownDuration);
            cooldownOverlay.gameObject.SetActive(cooldownOverlay.fillAmount > 0f);
        }
    }
}
