using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SideDefenseCameraViewport : MonoBehaviour
    {
        [SerializeField] private RectTransform bottomUiPanel;
        [SerializeField, Range(0f, 0.6f)] private float fallbackBottomRatio = 0.25f;
        [SerializeField, Min(0f)] private float viewportPaddingPixels = 4f;
        [SerializeField, Range(0.2f, 1f)] private float minimumMapViewportHeight = 0.4f;

        private Camera targetCamera;
        private readonly Vector3[] panelCorners = new Vector3[4];
        private int previousScreenWidth;
        private int previousScreenHeight;

        public void Configure(
            RectTransform panel,
            float defaultBottomRatio,
            float paddingPixels)
        {
            bottomUiPanel = panel;
            fallbackBottomRatio = Mathf.Clamp(defaultBottomRatio, 0f, 0.6f);
            viewportPaddingPixels = Mathf.Max(0f, paddingPixels);
            ApplyViewport(fallbackBottomRatio);
        }

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            ApplyViewport(fallbackBottomRatio);
            previousScreenWidth = -1;
            previousScreenHeight = -1;
        }

        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            RefreshViewportFromUi();
        }

        private void LateUpdate()
        {
            if (bottomUiPanel == null)
            {
                return;
            }

            if (Screen.width != previousScreenWidth ||
                Screen.height != previousScreenHeight ||
                bottomUiPanel.hasChanged)
            {
                RefreshViewportFromUi();
                bottomUiPanel.hasChanged = false;
            }
        }

        private void OnDisable()
        {
            if (targetCamera != null)
            {
                targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }
        }

        private void RefreshViewportFromUi()
        {
            previousScreenWidth = Screen.width;
            previousScreenHeight = Screen.height;

            if (bottomUiPanel == null || Screen.height <= 0)
            {
                ApplyViewport(fallbackBottomRatio);
                return;
            }

            bottomUiPanel.GetWorldCorners(panelCorners);
            float panelTop = Mathf.Max(panelCorners[1].y, panelCorners[2].y);
            float reservedRatio =
                (panelTop + viewportPaddingPixels) / Screen.height;

            if (float.IsNaN(reservedRatio) ||
                float.IsInfinity(reservedRatio) ||
                reservedRatio <= 0f)
            {
                reservedRatio = fallbackBottomRatio;
            }

            ApplyViewport(reservedRatio);
        }

        private void ApplyViewport(float reservedBottomRatio)
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            float maximumReserved = 1f - minimumMapViewportHeight;
            float bottom = Mathf.Clamp(reservedBottomRatio, 0f, maximumReserved);
            targetCamera.rect = new Rect(0f, bottom, 1f, 1f - bottom);
        }
    }
}
