using System;
using UnityEngine;

namespace Game3.Hunting
{
    [CreateAssetMenu(menuName = "GAME-3/Hunting/Hunter Upgrade Definition")]
    public sealed class HunterUpgradeDefinition : ScriptableObject
    {
        public HunterUpgradeType type;
        public string displayName = "강화";
        [TextArea] public string description = string.Empty;
        public int[] costs = Array.Empty<int>();
        public float bonusPerLevel;

        public int MaxLevel => costs?.Length ?? 0;

        public int GetCost(int currentLevel)
        {
            return currentLevel >= 0 && currentLevel < MaxLevel
                ? Mathf.Max(0, costs[currentLevel])
                : -1;
        }
    }
}
