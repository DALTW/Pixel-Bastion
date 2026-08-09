using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefensePauseMenu : MonoBehaviour
    {
        public bool IsVisible => gameObject.activeSelf;

        public static SideDefensePauseMenu Create(
            Transform parent,
            Action openOptions,
            Action saveGame)
        {
            GameObject overlayObject = new GameObject(
                "Pause Menu Overlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(SideDefensePauseMenu));
            overlayObject.transform.SetParent(parent, false);

            RectTransform overlayRect =
                overlayObject.GetComponent<RectTransform>();
            Stretch(overlayRect, Vector2.zero, Vector2.one);
            Image overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(0.005f, 0.012f, 0.025f, 0.76f);
            overlayImage.raycastTarget = true;

            SideDefensePauseMenu menu =
                overlayObject.GetComponent<SideDefensePauseMenu>();
            menu.Build(openOptions, saveGame);
            overlayObject.SetActive(false);
            return menu;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Build(Action openOptions, Action saveGame)
        {
            Font font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Color ivory = new Color(1f, 0.94f, 0.78f, 1f);
            Color gold = new Color(1f, 0.72f, 0.22f, 1f);
            Color cyan = new Color(0.2f, 0.88f, 1f, 1f);

            Image panel = CreateImage(
                "Pause Panel",
                transform,
                new Color(0.025f, 0.07f, 0.12f, 0.98f));
            SetRect(
                panel.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                Vector2.zero,
                new Vector2(500f, 390f));
            AddOutline(panel.gameObject, gold, 4f);

            Text title = CreateText(
                "Pause Title",
                panel.transform,
                font,
                46,
                ivory);
            title.text = "PAUSED";
            SetRect(
                title.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(0f, 135f),
                new Vector2(430f, 68f));

            Button optionsButton = CreateButton(
                panel.transform,
                font,
                "Pause Options Button",
                "OPTIONS",
                new Vector2(0f, 48f),
                cyan,
                ivory);
            optionsButton.onClick.AddListener(
                () => openOptions?.Invoke());

            Button saveButton = CreateButton(
                panel.transform,
                font,
                "Pause Save Button",
                "SAVE GAME",
                new Vector2(0f, -38f),
                gold,
                ivory);
            saveButton.onClick.AddListener(
                () => saveGame?.Invoke());

            Text hint = CreateText(
                "Pause Resume Hint",
                panel.transform,
                font,
                18,
                new Color(0.72f, 0.82f, 0.88f, 1f));
            hint.text = "PRESS ESC TO RESUME";
            SetRect(
                hint.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                new Vector2(0f, -135f),
                new Vector2(430f, 46f));
        }

        private static Button CreateButton(
            Transform parent,
            Font font,
            string name,
            string labelText,
            Vector2 position,
            Color outlineColor,
            Color textColor)
        {
            Image image = CreateImage(
                name,
                parent,
                new Color(0.065f, 0.2f, 0.28f, 1f));
            SetRect(
                image.rectTransform,
                Vector2.one * 0.5f,
                Vector2.one * 0.5f,
                position,
                new Vector2(330f, 64f));
            AddOutline(image.gameObject, outlineColor, 3f);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(0.74f, 1f, 1f, 1f);
            colors.pressedColor =
                new Color(1f, 0.8f, 0.42f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };

            Text label = CreateText(
                $"{name} Label",
                image.transform,
                font,
                25,
                textColor);
            label.text = labelText;
            Stretch(label.rectTransform, Vector2.zero, Vector2.one);
            return button;
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
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(11, fontSize - 7);
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
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
