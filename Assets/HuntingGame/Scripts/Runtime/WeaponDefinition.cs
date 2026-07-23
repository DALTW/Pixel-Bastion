using UnityEngine;

namespace Game3.Hunting
{
    [CreateAssetMenu(menuName = "GAME-3/Hunting/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string id = "weapon";
        public string displayName = "무기";
        public int price;
        public float damage = 20f;
        public float fireInterval = 0.3f;
        public int magazineSize = 8;
        public float reloadTime = 1.2f;
        public bool automatic;
        public int ammoBundleSize = 24;
        public int ammoBundlePrice = 30;
        public Sprite worldSprite;
    }
}
