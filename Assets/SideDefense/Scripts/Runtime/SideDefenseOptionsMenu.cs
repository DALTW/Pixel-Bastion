using UnityEngine;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseOptionsMenu : MonoBehaviour
    {
        private static readonly Color PanelColor =
            new Color(0.025f, 0.07f, 0.12f, 0.98f);
        private static readonly Color RowColor =
            new Color(0.045f, 0.12f, 0.19f, 0.98f);
        private static readonly Color CyanColor =
            new Color(0.2f, 0.88f, 1f, 1f);
        private static readonly Color GoldColor =
            new Color(1f, 0.72f, 0.22f, 1f);
        private static readonly Color IvoryColor =
            new Color(1f, 0.94f, 0.78f, 1f);

        private Slider soundSlider;
        private Slider speedSlider;
        private Text soundValueLabel;
        private Text speedValueLabel;
        private Text healthBarValueLabel;
        private Text damageNumbersValueLabel;

        public bool IsVisible => gameObject.activeSelf;

        public static SideDefenseOptionsMenu Create(Transform parent)
        {
            GameObject overlayObject = new GameObject(
                "Options Menu Overlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(SideDefenseOptionsMenu));
            overlayObject.transform.SetParent(parent, false);

            RectTransform overlayRect =
                overlayObject.GetComponent<RectTransform>();
            Stretch(overlayRect, Vector2.zero, Vector2.one);

            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0.005f, 0.015f, 0.03f, 0.92f);
            overlayImage.raycastTarget = true;

            SideDefenseOptionsMenu menu =
                overlayObject.GetComponent<SideDefenseOptionsMenu>();
            menu.Build();
            overlayObject.SetActive(false);
            return menu;
        }

        public void Show()
        {
            RefreshValues();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Build()
        {
            Font font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Image panel = CreateImage(
                "Options Panel",
                transform,
                PanelColor);
            SetRect(
                panel.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                Vector2.zero,
                new Vector2(660f, 580f));
            AddOutline(panel.gameObject, GoldColor, 4f);

            Text title = CreateText(
                "Options Title",
                panel.transform,
                font,
                42,
                TextAnchor.MiddleCenter,
                IvoryColor);
            title.text = "OPTIONS";
            SetRect(
                title.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(0f, 238f),
                new Vector2(560f, 62f));

            Image soundRow = CreateOptionRow(
                panel.transform,
                font,
                "Sound Row",
                "SOUND",
                145f);
            soundSlider = CreateSlider(
                soundRow.transform,
                new Vector2(70f, 0f),
                0f,
                1f,
                SideDefenseOptionsSettings.SoundVolume);
            soundValueLabel = CreateValueLabel(
                soundRow.transform,
                font,
                new Vector2(238f, 0f));
            soundSlider.onValueChanged.AddListener(HandleSoundChanged);

            Image speedRow = CreateOptionRow(
                panel.transform,
                font,
                "Speed Row",
                "GAME SPEED",
                55f);
            speedSlider = CreateSlider(
                speedRow.transform,
                new Vector2(70f, 0f),
                1f,
                2f,
                SideDefenseOptionsSettings.GameplaySpeed);
            speedValueLabel = CreateValueLabel(
                speedRow.transform,
                font,
                new Vector2(238f, 0f));
            speedSlider.onValueChanged.AddListener(HandleSpeedChanged);

            Image healthBarRow = CreateOptionRow(
                panel.transform,
                font,
                "Health Bar Row",
                "HEALTH BARS",
                -35f);
            Button healthBarButton = CreateChoiceButton(
                healthBarRow.transform,
                font,
                "Health Bar Choice",
                out healthBarValueLabel);
            healthBarButton.onClick.AddListener(CycleHealthBarMode);

            Image damageRow = CreateOptionRow(
                panel.transform,
                font,
                "Damage Number Row",
                "DAMAGE NUMBERS",
                -125f);
            Button damageButton = CreateChoiceButton(
                damageRow.transform,
                font,
                "Damage Number Choice",
                out damageNumbersValueLabel);
            damageButton.onClick.AddListener(ToggleDamageNumbers);

            Button closeButton = CreatePanelButton(
                panel.transform,
                font,
                "Options Back Button",
                "BACK",
                new Vector2(0f, -238f),
                new Vector2(220f, 58f));
            closeButton.onClick.AddListener(Hide);

            RefreshValues();
        }

        private void HandleSoundChanged(float value)
        {
            float rounded = Mathf.Round(Mathf.Clamp01(value) * 100f) / 100f;
            soundSlider.SetValueWithoutNotify(rounded);
            SideDefenseOptionsSettings.SoundVolume = rounded;
            soundValueLabel.text = $"{Mathf.RoundToInt(rounded * 100f)}%";
        }

        private void HandleSpeedChanged(float value)
        {
            float rounded = Mathf.Round(
                Mathf.Clamp(value, 1f, 2f) * 10f) / 10f;
            speedSlider.SetValueWithoutNotify(rounded);
            SideDefenseOptionsSettings.GameplaySpeed = rounded;
            speedValueLabel.text = $"{rounded:0.0}x";
        }

        private void CycleHealthBarMode()
        {
            int next =
                ((int)SideDefenseOptionsSettings.HealthBarMode + 1) % 3;
            SideDefenseOptionsSettings.HealthBarMode =
                (SideDefenseHealthBarMode)next;
            RefreshHealthBarLabel();
        }

        private void ToggleDamageNumbers()
        {
            SideDefenseOptionsSettings.DamageNumbersEnabled =
                !SideDefenseOptionsSettings.DamageNumbersEnabled;
            RefreshDamageNumberLabel();
        }

        private void RefreshValues()
        {
            if (soundSlider != null)
            {
                float volume = SideDefenseOptionsSettings.SoundVolume;
                soundSlider.SetValueWithoutNotify(volume);
                soundValueLabel.text =
                    $"{Mathf.RoundToInt(volume * 100f)}%";
            }

            if (speedSlider != null)
            {
                float speed = SideDefenseOptionsSettings.GameplaySpeed;
                speedSlider.SetValueWithoutNotify(speed);
                speedValueLabel.text = $"{speed:0.0}x";
            }

            RefreshHealthBarLabel();
            RefreshDamageNumberLabel();
        }

        private void RefreshHealthBarLabel()
        {
            if (healthBarValueLabel == null)
            {
                return;
            }

            switch (SideDefenseOptionsSettings.HealthBarMode)
            {
                case SideDefenseHealthBarMode.OnDamage:
                    healthBarValueLabel.text = "ON DAMAGE";
                    break;
                case SideDefenseHealthBarMode.Hidden:
                    healthBarValueLabel.text = "HIDDEN";
                    break;
                default:
                    healthBarValueLabel.text = "ALWAYS";
                    break;
            }
        }

        private void RefreshDamageNumberLabel()
        {
            if (damageNumbersValueLabel != null)
            {
                damageNumbersValueLabel.text =
                    SideDefenseOptionsSettings.DamageNumbersEnabled
                        ? "ON"
                        : "OFF";
            }
        }

        private static Image CreateOptionRow(
            Transform parent,
            Font font,
            string name,
            string labelText,
            float y)
        {
            Image row = CreateImage(name, parent, RowColor);
            SetRect(
                row.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(0f, y),
                new Vector2(580f, 76f));
            AddOutline(
                row.gameObject,
                new Color(0.08f, 0.45f, 0.58f, 0.8f),
                2f);

            Text label = CreateText(
                $"{name} Label",
                row.transform,
                font,
                21,
                TextAnchor.MiddleLeft,
                IvoryColor);
            label.text = labelText;
            SetRect(
                label.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(-170f, 0f),
                new Vector2(220f, 54f));
            return row;
        }

        private static Slider CreateSlider(
            Transform parent,
            Vector2 position,
            float minimum,
            float maximum,
            float value)
        {
            Image track = CreateImage(
                "Slider Track",
                parent,
                new Color(0.015f, 0.035f, 0.055f, 1f));
            SetRect(
                track.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                position,
                new Vector2(250f, 28f));

            GameObject fillAreaObject = new GameObject(
                "Fill Area",
                typeof(RectTransform));
            fillAreaObject.transform.SetParent(track.transform, false);
            RectTransform fillArea =
                fillAreaObject.GetComponent<RectTransform>();
            Stretch(
                fillArea,
                Vector2.zero,
                Vector2.one,
                new Vector2(6f, 6f),
                new Vector2(-6f, -6f));

            Image fill = CreateImage("Fill", fillArea, CyanColor);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one);

            GameObject handleAreaObject = new GameObject(
                "Handle Slide Area",
                typeof(RectTransform));
            handleAreaObject.transform.SetParent(track.transform, false);
            RectTransform handleArea =
                handleAreaObject.GetComponent<RectTransform>();
            Stretch(
                handleArea,
                Vector2.zero,
                Vector2.one,
                new Vector2(8f, 0f),
                new Vector2(-8f, 0f));

            Image handle = CreateImage(
                "Handle",
                handleArea,
                GoldColor);
            SetRect(
                handle.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                Vector2.zero,
                new Vector2(18f, 38f));

            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.wholeNumbers = false;
            slider.value = value;
            slider.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            return slider;
        }

        private static Text CreateValueLabel(
            Transform parent,
            Font font,
            Vector2 position)
        {
            Text label = CreateText(
                "Slider Value",
                parent,
                font,
                19,
                TextAnchor.MiddleCenter,
                GoldColor);
            SetRect(
                label.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                position,
                new Vector2(80f, 46f));
            return label;
        }

        private static Button CreateChoiceButton(
            Transform parent,
            Font font,
            string name,
            out Text valueLabel)
        {
            Image image = CreateImage(
                name,
                parent,
                new Color(0.08f, 0.26f, 0.34f, 1f));
            SetRect(
                image.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(120f, 0f),
                new Vector2(270f, 46f));
            AddOutline(image.gameObject, CyanColor, 2f);

            Button button = image.gameObject.AddComponent<Button>();
            ConfigureButton(button, image);

            valueLabel = CreateText(
                $"{name} Value",
                image.transform,
                font,
                20,
                TextAnchor.MiddleCenter,
                IvoryColor);
            Stretch(
                valueLabel.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(6f, 4f),
                new Vector2(-6f, -4f));
            return button;
        }

        private static Button CreatePanelButton(
            Transform parent,
            Font font,
            string name,
            string labelText,
            Vector2 position,
            Vector2 size)
        {
            Image image = CreateImage(
                name,
                parent,
                new Color(0.12f, 0.34f, 0.42f, 1f));
            SetRect(
                image.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                position,
                size);
            AddOutline(image.gameObject, GoldColor, 3f);

            Button button = image.gameObject.AddComponent<Button>();
            ConfigureButton(button, image);

            Text label = CreateText(
                $"{name} Label",
                image.transform,
                font,
                24,
                TextAnchor.MiddleCenter,
                IvoryColor);
            label.text = labelText;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void ConfigureButton(Button button, Image image)
        {
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(0.75f, 1f, 1f, 1f);
            colors.pressedColor =
                new Color(1f, 0.82f, 0.45f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new GameObject(
                name,
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
            text.resizeTextMinSize = Mathf.Max(11, fontSize - 6);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Outline AddOutline(
            GameObject target,
            Color color,
            float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Stretch(
                rect,
                anchorMin,
                anchorMax,
                Vector2.zero,
                Vector2.zero);
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
