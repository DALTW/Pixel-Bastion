using UnityEngine;

namespace Game3.Hunting
{
    public sealed class HuntingHud : MonoBehaviour
    {
        private HuntingGameController game;
        private Font font;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;
        private string notification = string.Empty;
        private float notificationUntil;
        private string interactionText = string.Empty;
        private float interactionProgress;
        private float damageFlashUntil;
        private bool showHelp = true;

        public void Initialize(HuntingGameController controller)
        {
            game = controller;
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 18);
        }

        public void ShowNotification(string message)
        {
            notification = message ?? string.Empty;
            notificationUntil = Time.time + 3.2f;
        }

        public void SetInteraction(string text, float progress)
        {
            interactionText = text ?? string.Empty;
            interactionProgress = Mathf.Clamp01(progress);
        }

        public void FlashDamage() => damageFlashUntil = Time.time + 0.18f;

        private void OnGUI()
        {
            if (game == null || game.Player == null)
            {
                return;
            }

            EnsureStyles();
            DrawHud();
            DrawInteraction();
            DrawTrackedPrey();

            if (game.IsShopOpen)
            {
                DrawShop();
            }
            else if (showHelp)
            {
                DrawHelp();
            }

            if (Time.time < notificationUntil)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 270f, 22f, 540f, 42f), notification, centeredStyle);
            }

            if (Time.time < damageFlashUntil)
            {
                var previous = GUI.color;
                GUI.color = new Color(0.75f, 0.05f, 0.05f, 0.2f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(18f, 18f, 390f, 176f), string.Empty);
            GUI.Label(new Rect(34f, 28f, 350f, 28f),
                $"체력  {Mathf.CeilToInt(game.Player.Health)} / {Mathf.CeilToInt(game.Player.MaxHealth)}",
                titleStyle);
            GUI.Label(new Rect(34f, 58f, 350f, 24f), $"돈  {game.Money:N0}원", labelStyle);
            GUI.Label(new Rect(34f, 82f, 350f, 24f),
                $"고기 {game.Inventory.Meat}  가죽 {game.Inventory.Hide}  털 {game.Inventory.Wool}  깃털 {game.Inventory.Feather}",
                labelStyle);
            GUI.Label(new Rect(34f, 106f, 350f, 24f),
                $"가방  {game.Inventory.Count} / {game.Inventory.Capacity}", labelStyle);
            GUI.Label(new Rect(34f, 130f, 350f, 24f),
                $"제압력  {game.Player.SubduePower:0.#}  이동속도  {game.Player.MoveSpeed:0.0}", labelStyle);
            GUI.Label(new Rect(34f, 154f, 350f, 24f),
                game.Player.IsAttacking ? "행동  제압 공격 중" : "행동  준비", labelStyle);
        }

        private void DrawInteraction()
        {
            if (string.IsNullOrWhiteSpace(interactionText))
            {
                return;
            }

            var width = 430f;
            var x = Screen.width * 0.5f - width * 0.5f;
            var y = Screen.height - 92f;
            GUI.Box(new Rect(x, y, width, 58f), interactionText, centeredStyle);
            if (interactionProgress <= 0f)
            {
                return;
            }

            GUI.Box(new Rect(x + 22f, y + 36f, width - 44f, 10f), string.Empty);
            var previous = GUI.color;
            GUI.color = new Color(0.36f, 0.84f, 0.32f);
            GUI.DrawTexture(new Rect(x + 24f, y + 38f, (width - 48f) * interactionProgress, 6f),
                Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawTrackedPrey()
        {
            var prey = game.TrackedPrey;
            var camera = Camera.main;
            if (prey == null || camera == null)
            {
                return;
            }

            var screen = camera.WorldToScreenPoint(prey.transform.position + Vector3.up * 0.8f);
            if (screen.z <= 0f)
            {
                return;
            }

            var y = Screen.height - screen.y;
            GUI.Box(new Rect(screen.x - 58f, y - 18f, 116f, 28f),
                $"{prey.Definition.displayName} {game.TrackedPreyDistance:0}m", centeredStyle);
        }

        private void DrawHelp()
        {
            var rect = new Rect(Screen.width - 350f, 18f, 332f, 190f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 28f), "포획 안내", titleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 44f, rect.width - 32f, 108f),
                "WASD / 방향키  이동\nSpace  이동 방향으로 제압 공격\nE 길게  기절 동물 채취\n캠프에서 E  판매·강화\nEsc  상점 닫기",
                labelStyle);
            if (GUI.Button(new Rect(rect.x + 208f, rect.y + 154f, 106f, 26f), "안내 닫기", buttonStyle))
            {
                showHelp = false;
            }
        }

        private void DrawShop()
        {
            var width = Mathf.Min(820f, Screen.width - 40f);
            var height = Mathf.Min(650f, Screen.height - 40f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, 320f, 34f), "Sunnyside 캠프 상점", titleStyle);
            GUI.Label(new Rect(rect.x + width - 220f, rect.y + 22f, 190f, 28f),
                $"보유 {game.Money:N0}원", titleStyle);

            var saleText =
                $"전리품 모두 판매  · 고기 {game.Inventory.Meat}×{game.Config.GetLootPrice(LootType.Meat)}  " +
                $"가죽 {game.Inventory.Hide}×{game.Config.GetLootPrice(LootType.Hide)}  " +
                $"털 {game.Inventory.Wool}×{game.Config.GetLootPrice(LootType.Wool)}  " +
                $"깃털 {game.Inventory.Feather}×{game.Config.GetLootPrice(LootType.Feather)}";
            if (GUI.Button(new Rect(rect.x + 24f, rect.y + 62f, width - 48f, 42f), saleText, buttonStyle))
            {
                game.SellAllLoot();
            }

            GUI.Label(new Rect(rect.x + 24f, rect.y + 120f, 320f, 28f), "사냥꾼 능력 강화", titleStyle);
            var y = rect.y + 154f;
            foreach (var upgrade in game.Config.upgrades)
            {
                if (upgrade == null)
                {
                    continue;
                }

                var level = game.SaveData.GetUpgradeLevel(upgrade.type);
                var cost = upgrade.GetCost(level);
                var text = cost < 0
                    ? $"{upgrade.displayName}  Lv.{level} / MAX  · {upgrade.description}"
                    : $"{upgrade.displayName}  Lv.{level} → {level + 1}  · {cost:N0}원  · {upgrade.description}";
                GUI.enabled = cost >= 0;
                if (GUI.Button(new Rect(rect.x + 24f, y, width - 48f, 38f), text, buttonStyle))
                {
                    game.BuyUpgrade(upgrade);
                }

                GUI.enabled = true;
                y += 46f;
            }

            GUI.Label(new Rect(rect.x + 24f, y + 8f, 300f, 28f), "동료 개 (최대 2마리)", titleStyle);
            y += 42f;
            foreach (var dog in game.Config.dogs)
            {
                if (dog == null)
                {
                    continue;
                }

                var owned = game.SaveData.ownedDogIds.Contains(dog.id);
                var dogText = owned
                    ? $"{dog.displayName}  [합류 완료]"
                    : $"{dog.displayName}  구매 {dog.price:N0}원  · 탐지 {dog.detectionRadius:0}m · 제압 {dog.subduePower:0}";
                GUI.enabled = !owned;
                if (GUI.Button(new Rect(rect.x + 24f, y, width - 48f, 36f), dogText, buttonStyle))
                {
                    game.BuyDog(dog);
                }

                GUI.enabled = true;
                y += 44f;
            }

            if (GUI.Button(new Rect(rect.x + width - 158f, rect.y + height - 48f, 134f, 30f),
                    "상점 닫기 (Esc)", buttonStyle))
            {
                game.SetShopOpen(false);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.91f, 0.56f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                normal = { textColor = Color.white },
                wordWrap = true
            };
            centeredStyle = new GUIStyle(GUI.skin.box)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
                wordWrap = true
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
    }
}
