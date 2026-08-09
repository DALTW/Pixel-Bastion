using UnityEngine;
using UnityEngine.UI;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseDamageNumber : MonoBehaviour
    {
        private const float Lifetime = 0.9f;
        private const float RiseSpeed = 0.48f;

        private Text valueLabel;
        private float elapsedTime;
        private Color baseColor;

        public static void Show(
            Vector3 worldPosition,
            float damage,
            bool damagedHuman)
        {
            if (!Application.isPlaying ||
                damage <= 0f ||
                !SideDefenseOptionsSettings.DamageNumbersEnabled)
            {
                return;
            }

            GameObject root = new GameObject(
                "Damage Number",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(SideDefenseDamageNumber));
            root.transform.position =
                worldPosition +
                new Vector3(Random.Range(-0.08f, 0.08f), 0.82f, 0f);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(96f, 36f);
            rootRect.localScale = Vector3.one * 0.01f;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 220;

            GameObject labelObject = new GameObject(
                "Damage Value",
                typeof(RectTransform),
                typeof(Text),
                typeof(Outline));
            labelObject.transform.SetParent(root.transform, false);

            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = Mathf.CeilToInt(damage).ToString();
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 22;
            label.raycastTarget = false;
            label.color = damagedHuman
                ? new Color(1f, 0.34f, 0.24f, 1f)
                : new Color(1f, 0.86f, 0.34f, 1f);

            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            SideDefenseDamageNumber number =
                root.GetComponent<SideDefenseDamageNumber>();
            number.valueLabel = label;
            number.baseColor = label.color;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsedTime += deltaTime;
            transform.position += Vector3.up * (RiseSpeed * deltaTime);

            if (valueLabel != null)
            {
                float normalized = Mathf.Clamp01(elapsedTime / Lifetime);
                Color color = baseColor;
                color.a = 1f - Mathf.SmoothStep(0.45f, 1f, normalized);
                valueLabel.color = color;
            }

            if (elapsedTime >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
