using System.Collections;
using UnityEngine;

namespace Game3.Hunting
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WeaponSystem : MonoBehaviour
    {
        private PlayerHunter owner;
        private SpriteRenderer spriteRenderer;
        private WeaponDefinition current;
        private WeaponAmmoSave ammo;
        private float nextFireTime;
        private bool reloading;

        public WeaponDefinition Current => current;
        public int MagazineAmmo => ammo?.magazineAmmo ?? 0;
        public int ReserveAmmo => ammo?.reserveAmmo ?? 0;
        public bool IsReloading => reloading;

        public void Initialize(PlayerHunter player)
        {
            owner = player;
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 12;
            transform.localPosition = new Vector3(0.28f, -0.03f, 0f);
        }

        private void Update()
        {
            var game = HuntingGameController.Instance;
            if (game == null || owner == null || game.IsShopOpen)
            {
                return;
            }

            if (current == null)
            {
                Equip(game.SaveData.equippedWeaponId);
                return;
            }

            AimAtPointer();
            var wantsToFire = current.automatic ? owner.Input.FireHeld : owner.Input.FirePressed;
            if (wantsToFire)
            {
                TryFire();
            }
        }

        public bool TryEquipSlot(int slot)
        {
            var game = HuntingGameController.Instance;
            if (game == null || slot < 0 || slot >= game.Config.weapons.Length)
            {
                return false;
            }

            var definition = game.Config.weapons[slot];
            if (!game.SaveData.ownedWeaponIds.Contains(definition.id))
            {
                game.Notify($"{definition.displayName}은(는) 아직 구매하지 않았습니다.");
                return false;
            }

            Equip(definition.id);
            return true;
        }

        public void Equip(string weaponId)
        {
            var game = HuntingGameController.Instance;
            var definition = game.FindWeapon(weaponId);
            if (definition == null || !game.SaveData.ownedWeaponIds.Contains(definition.id))
            {
                return;
            }

            StopAllCoroutines();
            reloading = false;
            current = definition;
            ammo = game.GetOrCreateAmmo(definition);
            ammo.magazineAmmo = Mathf.Min(ammo.magazineAmmo, definition.magazineSize);
            spriteRenderer.sprite = definition.worldSprite;
            game.SaveData.equippedWeaponId = definition.id;
            game.SaveProgress();
            game.Notify($"{definition.displayName} 장착");
        }

        public void TryReload()
        {
            if (current == null || ammo == null || reloading || ammo.magazineAmmo >= current.magazineSize || ammo.reserveAmmo <= 0)
            {
                return;
            }

            StartCoroutine(ReloadRoutine());
        }

        private void TryFire()
        {
            if (current == null || ammo == null || reloading || Time.time < nextFireTime)
            {
                return;
            }

            if (ammo.magazineAmmo <= 0)
            {
                HuntingGameController.Instance.Notify("탄창이 비었습니다. R로 재장전하세요.");
                TryReload();
                nextFireTime = Time.time + 0.25f;
                return;
            }

            ammo.magazineAmmo--;
            nextFireTime = Time.time + current.fireInterval;
            var direction = GetAimDirection();
            FireRay(direction);
            StartCoroutine(MuzzleKick());
        }

        private void FireRay(Vector2 direction)
        {
            var origin = (Vector2)owner.transform.position + direction * 0.35f;
            var hits = Physics2D.RaycastAll(origin, direction, 14f);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            var end = origin + direction * 14f;

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(owner.transform))
                {
                    continue;
                }

                var animal = hit.collider.GetComponentInParent<AnimalController>();
                if (animal != null)
                {
                    animal.TakeDamage(current.damage, owner.transform.position);
                    end = hit.point;
                    break;
                }

                if (hit.collider.GetComponent<HuntingObstacle>() != null)
                {
                    end = hit.point;
                    break;
                }
            }

            CreateTracer(origin, end);
        }

        private static void CreateTracer(Vector2 start, Vector2 end)
        {
            var tracerObject = new GameObject("ShotTracer");
            var line = tracerObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.035f;
            line.endWidth = 0.01f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 0.85f, 0.25f, 0.95f);
            line.endColor = new Color(1f, 0.3f, 0.1f, 0.1f);
            line.sortingOrder = 20;
            Destroy(tracerObject, 0.055f);
        }

        private void AimAtPointer()
        {
            var direction = GetAimDirection();
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            spriteRenderer.flipY = direction.x < 0f;
        }

        private Vector2 GetAimDirection()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return Vector2.right;
            }

            var pointer = camera.ScreenToWorldPoint(owner.Input.PointerScreen);
            var direction = (Vector2)(pointer - owner.transform.position);
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        }

        private IEnumerator ReloadRoutine()
        {
            reloading = true;
            HuntingGameController.Instance.Notify("재장전 중...");
            yield return new WaitForSeconds(current.reloadTime);
            var required = current.magazineSize - ammo.magazineAmmo;
            var transferred = Mathf.Min(required, ammo.reserveAmmo);
            ammo.magazineAmmo += transferred;
            ammo.reserveAmmo -= transferred;
            reloading = false;
            HuntingGameController.Instance.SaveProgress();
        }

        private IEnumerator MuzzleKick()
        {
            var basePosition = new Vector3(0.28f, -0.03f, 0f);
            transform.localPosition = basePosition + Vector3.left * 0.06f;
            yield return new WaitForSeconds(0.045f);
            transform.localPosition = basePosition;
        }
    }

    public sealed class HuntingObstacle : MonoBehaviour
    {
    }
}
