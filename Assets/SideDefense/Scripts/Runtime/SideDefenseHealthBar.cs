using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseHealthBar : MonoBehaviour
    {
        [SerializeField] private Transform fillPivot;
        [SerializeField, Range(0f, 1f)] private float displayedNormalized = 1f;

        public float DisplayedNormalized => displayedNormalized;

        public void Configure(Transform leftAnchoredFillPivot)
        {
            fillPivot = leftAnchoredFillPivot;
            SetNormalized(1f);
        }

        public void SetNormalized(float normalizedHealth)
        {
            displayedNormalized = Mathf.Clamp01(normalizedHealth);
            ApplyFillScale();
        }

        private void Awake()
        {
            ApplyFillScale();
        }

        private void OnValidate()
        {
            displayedNormalized = Mathf.Clamp01(displayedNormalized);
            ApplyFillScale();
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
    }
}
