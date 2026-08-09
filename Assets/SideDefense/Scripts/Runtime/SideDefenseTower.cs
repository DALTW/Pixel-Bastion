using System;
using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    public sealed class SideDefenseTower : MonoBehaviour
    {
        [SerializeField] private string displayName = "Allied Tower";
        [SerializeField, Min(1f)] private float maxHealth = 1500f;
        [SerializeField, Min(0f)] private float currentHealth = 1500f;
        [SerializeField] private SideDefenseHealthBar healthBar;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized =>
            maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public bool IsDestroyed => currentHealth <= 0f;

        public event Action<SideDefenseTower> HealthChanged;
        public event Action<SideDefenseTower> Destroyed;

        public void Configure(string towerName, float health)
        {
            displayName = string.IsNullOrWhiteSpace(towerName)
                ? "Allied Tower"
                : towerName;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            RefreshHealthBar();
        }

        public void BindHealthBar(SideDefenseHealthBar bar)
        {
            healthBar = bar;
            RefreshHealthBar();
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f || IsDestroyed)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            RefreshHealthBar();
            HealthChanged?.Invoke(this);

            if (IsDestroyed)
            {
                Destroyed?.Invoke(this);
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDestroyed)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            RefreshHealthBar();
            HealthChanged?.Invoke(this);
        }

        public void RestoreHealth(float savedHealth)
        {
            currentHealth = Mathf.Clamp(savedHealth, 1f, maxHealth);
            RefreshHealthBar();
            HealthChanged?.Invoke(this);
        }

        private void Awake()
        {
            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<SideDefenseHealthBar>(true);
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            RefreshHealthBar();
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            RefreshHealthBar();
        }

        private void RefreshHealthBar()
        {
            if (healthBar != null)
            {
                healthBar.SetNormalized(HealthNormalized);
            }
        }
    }
}
