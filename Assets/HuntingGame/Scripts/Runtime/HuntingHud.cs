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
                GUI.Box(new Rect(Screen.width * 0.5f - 260f, 22f, 520f, 42f), notification, centeredStyle);
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
            var weapon = game.Player.Weapon;
            var weaponName = weapon?.Current != null ? weapon.Current.displayName : "-";
            var reload = weapon != null && weapon.IsReloading ? " (재장전)" : string.Empty;
            GUI.Box(new Rect(18f, 18f, 300f, 146f), string.Empty);
            GUI.Label(new Rect(34f, 30f, 270f, 28f), $"체력  {Mathf.CeilToInt(game.Player.Health)} / {Mathf.CeilToInt(game.Player.MaxHealth)}", titleStyle);
            GUI.Label(new Rect(34f, 60f, 270f, 24f), $"돈  {game.Money:N0}원", labelStyle);
            GUI.Label(new Rect(34f, 84f, 270f, 24f),
                $"전리품  고기 {game.Inventory.Meat}  가죽 {game.Inventory.Hide}  ({game.Inventory.Count}/{game.Inventory.Capacity})",
                labelStyle);
            GUI.Label(new Rect(34f, 108f, 270f, 24f), $"무기  {weaponName}{reload}", labelStyle);
            GUI.Label(new Rect(34f, 132f, 270f, 24f),
                $"탄약  {weapon?.MagazineAmmo ?? 0} / {weapon?.ReserveAmmo ?? 0}", labelStyle);
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
            if (interactionProgress > 0f)
            {
                GUI.Box(new Rect(x + 22f, y + 36f, width - 44f, 10f), string.Empty);
                var previous = GUI.color;
                GUI.color = new Color(0.92f, 0.68f, 0.22f);
                GUI.DrawTexture(new Rect(x + 24f, y + 38f, (width - 48f) * interactionProgress, 6f),
                    Texture2D.whiteTexture);
                GUI.color = previous;
            }
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
            GUI.Box(new Rect(screen.x - 48f, y - 18f, 96f, 28f),
                $"추적 {game.TrackedPreyDistance:0}m", centeredStyle);
        }

        private void DrawHelp()
        {
            var rect = new Rect(Screen.width - 330f, 18f, 312f, 192f);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 28f), "사냥 안내", titleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 44f, rect.width - 32f, 112f),
                "WASD  이동\n마우스  조준 / 좌클릭  사격\nR  재장전 / 1~4  무기 변경\nE 길게  채취 / 캠프 상점\nEsc  상점 닫기",
                labelStyle);
            if (GUI.Button(new Rect(rect.x + 190f, rect.y + 157f, 104f, 26f), "안내 닫기", buttonStyle))
            {
                showHelp = false;
            }
        }

        private void DrawShop()
        {
            var width = Mathf.Min(780f, Screen.width - 40f);
            var height = Mathf.Min(610f, Screen.height - 40f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(rect, string.Empty);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 18f, 300f, 34f), "캠프 상점", titleStyle);
            GUI.Label(new Rect(rect.x + width - 210f, rect.y + 22f, 180f, 28f), $"보유 {game.Money:N0}원", titleStyle);

            if (GUI.Button(new Rect(rect.x + 24f, rect.y + 62f, width - 48f, 38f),
                    $"전리품 모두 판매  (고기 {game.Inventory.Meat} × {game.Config.meatSellPrice}원, 가죽 {game.Inventory.Hide} × {game.Config.hideSellPrice}원)",
                    buttonStyle))
            {
                game.SellAllLoot();
            }

            GUI.Label(new Rect(rect.x + 24f, rect.y + 116f, 260f, 28f), "무기 및 탄약", titleStyle);
            var y = rect.y + 148f;
            foreach (var weapon in game.Config.weapons)
            {
                var owned = game.SaveData.ownedWeaponIds.Contains(weapon.id);
                var equipped = game.Player.Weapon.Current == weapon;
                var weaponText = equipped ? $"{weapon.displayName}  [장착 중]" :
                    owned ? $"{weapon.displayName}  장착" : $"{weapon.displayName}  구매 {weapon.price:N0}원";
                if (GUI.Button(new Rect(rect.x + 24f, y, width * 0.55f, 34f), weaponText, buttonStyle))
                {
                    game.BuyOrEquipWeapon(weapon);
                }

                var ammoText = $"탄약 +{weapon.ammoBundleSize}  {weapon.ammoBundlePrice:N0}원";
                if (GUI.Button(new Rect(rect.x + width * 0.61f, y, width * 0.32f, 34f), ammoText, buttonStyle))
                {
                    game.BuyAmmo(weapon);
                }

                y += 42f;
            }

            GUI.Label(new Rect(rect.x + 24f, y + 8f, 260f, 28f), "동료 개 (최대 2마리)", titleStyle);
            y += 42f;
            foreach (var dog in game.Config.dogs)
            {
                var owned = game.SaveData.ownedDogIds.Contains(dog.id);
                var dogText = owned
                    ? $"{dog.displayName}  [합류 완료]"
                    : $"{dog.displayName}  구매 {dog.price:N0}원  · 탐지 {dog.detectionRadius:0}m · 공격 {dog.damage:0}";
                GUI.enabled = !owned;
                if (GUI.Button(new Rect(rect.x + 24f, y, width - 48f, 36f), dogText, buttonStyle))
                {
                    game.BuyDog(dog);
                }

                GUI.enabled = true;
                y += 44f;
            }

            if (GUI.Button(new Rect(rect.x + width - 148f, rect.y + height - 48f, 124f, 30f), "상점 닫기 (Esc)", buttonStyle))
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
                normal = { textColor = new Color(1f, 0.9f, 0.62f) }
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
