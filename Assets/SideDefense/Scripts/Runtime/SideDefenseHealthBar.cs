using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseHealthBar : MonoBehaviour
    {
        private const float DamageVisibleDuration = 2.25f;

        [SerializeField] private Transform fillPivot;
        [SerializeField, Range(0f, 1f)] private float displayedNormalized = 1f;

        private Renderer[] barRenderers;
        private float visibleUntilUnscaledTime;
        private bool renderersVisible = true;

        public float DisplayedNormalized => displayedNormalized;

        public void Configure(Transform leftAnchoredFillPivot)
        {
            fillPivot = leftAnchoredFillPivot;
            SetNormalized(1f);
        }

        public void SetNormalized(float normalizedHealth)
        {
            float clamped = Mathf.Clamp01(normalizedHealth);
            if (clamped < displayedNormalized - 0.0001f)
            {
                visibleUntilUnscaledTime =
                    Time.unscaledTime + DamageVisibleDuration;
            }

            displayedNormalized = clamped;
            ApplyFillScale();
            ApplyVisibility();
        }

        private void Awake()
        {
            CacheRenderers();
            ApplyFillScale();
            ApplyVisibility();
        }

        private void OnEnable()
        {
            SideDefenseOptionsSettings.Changed -= HandleOptionsChanged;
            SideDefenseOptionsSettings.Changed += HandleOptionsChanged;
            CacheRenderers();
            ApplyVisibility();
        }

        private void OnDisable()
        {
            SideDefenseOptionsSettings.Changed -= HandleOptionsChanged;
        }

        private void Update()
        {
            if (SideDefenseOptionsSettings.HealthBarMode ==
                    SideDefenseHealthBarMode.OnDamage &&
                renderersVisible &&
                Time.unscaledTime >= visibleUntilUnscaledTime)
            {
                ApplyVisibility();
            }
        }

        private void OnValidate()
        {
            displayedNormalized = Mathf.Clamp01(displayedNormalized);
            CacheRenderers();
            ApplyFillScale();
            ApplyVisibility();
        }

        private void HandleOptionsChanged()
        {
            if (SideDefenseOptionsSettings.HealthBarMode ==
                    SideDefenseHealthBarMode.OnDamage &&
                displayedNormalized < 0.9999f)
            {
                visibleUntilUnscaledTime =
                    Time.unscaledTime + DamageVisibleDuration;
            }

            ApplyVisibility();
        }

        private void ApplyFillScale()
        {
            if (fillPivot == null)
            {
                return;
            }

            Vector3 scale = fillPivot.localScale;
            scale.x = displayedNormalized;
            scale.y = 1f;
            scale.z = 1f;
            fillPivot.localScale = scale;
        }

        private void CacheRenderers()
        {
            if (barRenderers == null || barRenderers.Length == 0)
            {
                barRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void ApplyVisibility()
        {
            CacheRenderers();
            bool shouldShow;
            switch (SideDefenseOptionsSettings.HealthBarMode)
            {
                case SideDefenseHealthBarMode.Hidden:
                    shouldShow = false;
                    break;
                case SideDefenseHealthBarMode.OnDamage:
                    shouldShow =
                        Time.unscaledTime < visibleUntilUnscaledTime;
                    break;
                default:
                    shouldShow = true;
                    break;
            }

            if (barRenderers == null)
            {
                return;
            }

            foreach (Renderer barRenderer in barRenderers)
            {
                if (barRenderer != null)
                {
                    barRenderer.enabled = shouldShow;
                }
            }

            renderersVisible = shouldShow;
        }
    }
}
