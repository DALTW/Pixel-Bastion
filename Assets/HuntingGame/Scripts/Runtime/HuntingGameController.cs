using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game3.Hunting
{
    public sealed class HuntingGameController : MonoBehaviour
    {
        public static HuntingGameController Instance { get; private set; }

        [SerializeField] private HuntingGameConfig config;
        private readonly List<AnimalController> animals = new List<AnimalController>();
        private readonly List<HarvestableCarcass> carcasses = new List<HarvestableCarcass>();
        private readonly List<DogCompanion> companions = new List<DogCompanion>();
        private HuntingInput input;
        private HuntingSaveSystem saveSystem;
        private float nextPopulationCheck;
        private HarvestableCarcass activeCarcass;
        private float harvestProgress;
        private AnimalController trackedPrey;
        private float trackedPreyDistance;
        private float trackedPreyExpiresAt;
        private Sprite solidSprite;

        public HuntingGameConfig Config => config;
        public HuntingSaveData SaveData { get; private set; }
        public InventoryModel Inventory { get; private set; }
        public PlayerHunter Player { get; private set; }
        public HuntingHud Hud { get; private set; }
        public IReadOnlyList<AnimalController> Animals => animals;
        public int Money => SaveData?.money ?? 0;
        public bool IsShopOpen { get; private set; }
        public AnimalController TrackedPrey => Time.time <= trackedPreyExpiresAt ? trackedPrey : null;
        public float TrackedPreyDistance => trackedPreyDistance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            config ??= Resources.Load<HuntingGameConfig>("HuntingGameConfig");
            if (config == null)
            {
                Debug.LogError("HuntingGameConfig를 찾을 수 없습니다. GAME-3 > Build Hunting Game 메뉴를 실행하세요.");
                enabled = false;
                return;
            }

            input = gameObject.AddComponent<HuntingInput>();
            saveSystem = new HuntingSaveSystem();
            SaveData = saveSystem.LoadOrCreate(config.startingMoney);
            Inventory = new InventoryModel(config.inventoryCapacity);
        }

        private void Start()
        {
            BuildWorld();
            CreatePlayer();
            CreateHud();
            SpawnOwnedDogs();
            EnsurePopulation();
            Notify("숲에서 사냥하고 캠프로 돌아와 전리품을 판매하세요.");
        }

        private void Update()
        {
            if (Player == null || input == null)
            {
                return;
            }

            if (input.CancelPressed && IsShopOpen)
            {
                SetShopOpen(false);
            }

            HandleInteraction();
            if (Time.time >= nextPopulationCheck)
            {
                nextPopulationCheck = Time.time + 2f;
                EnsurePopulation();
            }

            animals.RemoveAll(item => item == null);
            carcasses.RemoveAll(item => item == null);
            companions.RemoveAll(item => item == null);
        }

        private void OnApplicationQuit() => SaveProgress();

        public WeaponDefinition FindWeapon(string id)
        {
            return config.weapons.FirstOrDefault(item => item != null && item.id == id);
        }

        public DogDefinition FindDog(string id)
        {
            return config.dogs.FirstOrDefault(item => item != null && item.id == id);
        }

        public WeaponAmmoSave GetOrCreateAmmo(WeaponDefinition definition)
        {
            var state = SaveData.weaponAmmo.FirstOrDefault(item => item.weaponId == definition.id);
            if (state != null)
            {
                return state;
            }

            state = new WeaponAmmoSave
            {
                weaponId = definition.id,
                magazineAmmo = definition.magazineSize,
                reserveAmmo = 0
            };
            SaveData.weaponAmmo.Add(state);
            return state;
        }

        public bool BuyOrEquipWeapon(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (SaveData.ownedWeaponIds.Contains(definition.id))
            {
                Player.Weapon.Equip(definition.id);
                return true;
            }

            if (!TrySpend(definition.price))
            {
                Notify($"돈이 부족합니다. {definition.price}원이 필요합니다.");
                return false;
            }

            SaveData.ownedWeaponIds.Add(definition.id);
            var ammo = GetOrCreateAmmo(definition);
            ammo.magazineAmmo = definition.magazineSize;
            ammo.reserveAmmo = definition.ammoBundleSize;
            Player.Weapon.Equip(definition.id);
            Notify($"{definition.displayName} 구매 완료");
            SaveProgress();
            return true;
        }

        public bool BuyAmmo(WeaponDefinition definition)
        {
            if (definition == null || !SaveData.ownedWeaponIds.Contains(definition.id))
            {
                Notify("먼저 해당 무기를 구매해야 합니다.");
                return false;
            }

            if (!TrySpend(definition.ammoBundlePrice))
            {
                Notify($"돈이 부족합니다. {definition.ammoBundlePrice}원이 필요합니다.");
                return false;
            }

            GetOrCreateAmmo(definition).reserveAmmo += definition.ammoBundleSize;
            Notify($"{definition.displayName} 탄약 {definition.ammoBundleSize}발 구매");
            SaveProgress();
            return true;
        }

        public bool BuyDog(DogDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            if (SaveData.ownedDogIds.Contains(definition.id))
            {
                Notify("이미 함께하고 있는 동료입니다.");
                return false;
            }

            if (SaveData.ownedDogIds.Count >= 2)
            {
                Notify("동료 개는 최대 2마리까지 데려갈 수 있습니다.");
                return false;
            }

            if (!TrySpend(definition.price))
            {
                Notify($"돈이 부족합니다. {definition.price}원이 필요합니다.");
                return false;
            }

            SaveData.ownedDogIds.Add(definition.id);
            SpawnDog(definition, companions.Count);
            Notify($"{definition.displayName}이(가) 사냥에 합류했습니다.");
            SaveProgress();
            return true;
        }

        public int SellAllLoot()
        {
            if (Inventory.Count == 0)
            {
                Notify("판매할 전리품이 없습니다.");
                return 0;
            }

            var value = Inventory.SellAll(config.meatSellPrice, config.hideSellPrice);
            SaveData.money += value;
            Notify($"전리품을 판매해 {value}원을 벌었습니다.");
            SaveProgress();
            return value;
        }

        public void HandlePlayerDeath()
        {
            Inventory.Clear();
            SetShopOpen(false);
            Player.Respawn(config.campPosition);
            Notify("쓰러졌습니다. 운반하던 전리품을 잃고 캠프에서 구조되었습니다.");
            SaveProgress();
        }

        public void HandleAnimalDeath(AnimalController animal)
        {
            if (animal == null)
            {
                return;
            }

            animals.Remove(animal);
            var corpseObject = new GameObject($"{animal.Definition.displayName} 사체");
            corpseObject.transform.position = animal.transform.position;
            corpseObject.transform.localScale = animal.transform.localScale;
            var renderer = corpseObject.AddComponent<SpriteRenderer>();
            var sourceRenderer = animal.GetComponent<SpriteRenderer>();
            var corpse = corpseObject.AddComponent<HarvestableCarcass>();
            corpse.Initialize(animal.Definition, sourceRenderer != null ? sourceRenderer.sprite : null, this);
            Notify($"{animal.Definition.displayName}을(를) 처치했습니다. 가까이에서 E를 길게 누르세요.");
        }

        public void RegisterCarcass(HarvestableCarcass carcass)
        {
            if (carcass != null && !carcasses.Contains(carcass))
            {
                carcasses.Add(carcass);
            }
        }

        public void UnregisterCarcass(HarvestableCarcass carcass)
        {
            carcasses.Remove(carcass);
        }

        public void ReportTrackedPrey(AnimalController prey, float distance)
        {
            if (prey == null)
            {
                return;
            }

            if (Time.time > trackedPreyExpiresAt || trackedPrey == null || distance < trackedPreyDistance)
            {
                trackedPrey = prey;
                trackedPreyDistance = distance;
            }

            trackedPreyExpiresAt = Time.time + 0.5f;
        }

        public bool IsInsideCamp(Vector2 position)
        {
            return Vector2.Distance(position, config.campPosition) <= config.campSafeRadius;
        }

        public Vector2 ClampToWorld(Vector2 position, float padding)
        {
            var half = config.worldSize * 0.5f;
            return new Vector2(
                Mathf.Clamp(position.x, -half.x + padding, half.x - padding),
                Mathf.Clamp(position.y, -half.y + padding, half.y - padding));
        }

        public void SetShopOpen(bool open)
        {
            IsShopOpen = open;
            harvestProgress = 0f;
            if (open)
            {
                Notify("상점이 열렸습니다. Esc로 닫을 수 있습니다.");
            }
        }

        public void SaveProgress()
        {
            if (saveSystem == null || SaveData == null)
            {
                return;
            }

            try
            {
                saveSystem.Save(SaveData);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"진행 저장 실패: {exception.Message}");
            }
        }

        public void Notify(string message)
        {
            if (Hud != null)
            {
                Hud.ShowNotification(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        private bool TrySpend(int amount)
        {
            amount = Mathf.Max(0, amount);
            if (SaveData.money < amount)
            {
                return false;
            }

            SaveData.money -= amount;
            return true;
        }

        private void HandleInteraction()
        {
            var playerPosition = (Vector2)Player.transform.position;
            var atShop = Vector2.Distance(playerPosition, config.campPosition) <= 2.4f;
            if (!IsShopOpen && atShop && input.InteractPressed)
            {
                SetShopOpen(true);
                return;
            }

            if (IsShopOpen)
            {
                Hud.SetInteraction(string.Empty, 0f);
                return;
            }

            activeCarcass = carcasses
                .Where(item => item != null && item.IsAvailable)
                .OrderBy(item => Vector2.Distance(playerPosition, item.transform.position))
                .FirstOrDefault(item => Vector2.Distance(playerPosition, item.transform.position) <= 1.45f);

            if (activeCarcass != null)
            {
                if (input.InteractHeld)
                {
                    harvestProgress += Time.deltaTime / 1.25f;
                    if (harvestProgress >= 1f)
                    {
                        activeCarcass.Harvest();
                        harvestProgress = 0f;
                    }
                }
                else
                {
                    harvestProgress = 0f;
                }

                Hud.SetInteraction($"E 길게: {activeCarcass.Definition.displayName} 채취", harvestProgress);
            }
            else
            {
                harvestProgress = 0f;
                Hud.SetInteraction(atShop ? "E: 캠프 상점 열기" : string.Empty, 0f);
            }
        }

        private void BuildWorld()
        {
            solidSprite = CreateSolidSprite();
            CreateColoredArea("Ground", Vector2.zero, config.worldSize, new Color(0.19f, 0.31f, 0.17f), -50);
            CreateColoredArea("Camp", config.campPosition, new Vector2(11f, 10f), new Color(0.48f, 0.37f, 0.22f), -40);
            CreateColoredArea("Camp Path", new Vector2(-14f, 0f), new Vector2(10f, 3f), new Color(0.39f, 0.31f, 0.2f), -39);
            CreateBoundaries();
            CreateEnvironment();
        }

        private void CreatePlayer()
        {
            var playerObject = new GameObject("Hunter");
            playerObject.transform.position = config.campPosition;
            playerObject.transform.localScale = Vector3.one * 4.2f;
            var renderer = playerObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 10;
            playerObject.AddComponent<Rigidbody2D>();
            var collider = playerObject.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.15f, 0.22f);
            collider.offset = new Vector2(0f, -0.02f);
            playerObject.AddComponent<SimpleSpriteAnimator>();
            Player = playerObject.AddComponent<PlayerHunter>();
            Player.Initialize(config, input);

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.08f);
            camera.transform.position = new Vector3(config.campPosition.x, config.campPosition.y, -10f);
            var follow = camera.gameObject.GetComponent<SmoothCameraFollow>() ??
                         camera.gameObject.AddComponent<SmoothCameraFollow>();
            follow.Initialize(Player.transform, config.worldSize);
        }

        private void CreateHud()
        {
            Hud = gameObject.AddComponent<HuntingHud>();
            Hud.Initialize(this);
        }

        private void EnsurePopulation()
        {
            var rabbits = animals.Count(item => item != null && item.Definition == config.rabbit);
            var wolves = animals.Count(item => item != null && item.Definition == config.wolf);
            while (rabbits++ < config.rabbitPopulation)
            {
                SpawnAnimal(config.rabbit);
            }

            while (wolves++ < config.wolfPopulation)
            {
                SpawnAnimal(config.wolf);
            }
        }

        private void SpawnAnimal(AnimalDefinition definition)
        {
            var spawnPosition = RandomWorldPositionOutsideCamp(config.campSafeRadius + 3f);
            var animalObject = new GameObject(definition.displayName);
            animalObject.transform.position = spawnPosition;
            animalObject.transform.localScale = Vector3.one * (definition.hostile ? 1.45f : 2.45f);
            var renderer = animalObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 5;
            animalObject.AddComponent<Rigidbody2D>();
            animalObject.AddComponent<CircleCollider2D>();
            animalObject.AddComponent<SimpleSpriteAnimator>();
            animalObject.AddComponent<YSortSprite>();
            var animal = animalObject.AddComponent<AnimalController>();
            animal.Initialize(definition, this);
            animals.Add(animal);
        }

        private void SpawnOwnedDogs()
        {
            for (var index = 0; index < SaveData.ownedDogIds.Count; index++)
            {
                var definition = FindDog(SaveData.ownedDogIds[index]);
                if (definition != null)
                {
                    SpawnDog(definition, index);
                }
            }
        }

        private void SpawnDog(DogDefinition definition, int slot)
        {
            var dogObject = new GameObject(definition.displayName);
            dogObject.transform.position = (Vector2)Player.transform.position + Vector2.left * (1f + slot);
            dogObject.transform.localScale = Vector3.one * 1.35f;
            var renderer = dogObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 9;
            dogObject.AddComponent<Rigidbody2D>();
            dogObject.AddComponent<CircleCollider2D>();
            dogObject.AddComponent<SimpleSpriteAnimator>();
            dogObject.AddComponent<YSortSprite>();
            var companion = dogObject.AddComponent<DogCompanion>();
            companion.Initialize(definition, this, slot);
            companions.Add(companion);
        }

        private void CreateEnvironment()
        {
            if (config.environmentSprites == null || config.environmentSprites.Length == 0)
            {
                return;
            }

            var random = new System.Random(7303);
            for (var index = 0; index < 42; index++)
            {
                var position = new Vector2(
                    Mathf.Lerp(-config.worldSize.x * 0.46f, config.worldSize.x * 0.46f, (float)random.NextDouble()),
                    Mathf.Lerp(-config.worldSize.y * 0.45f, config.worldSize.y * 0.45f, (float)random.NextDouble()));
                if (Vector2.Distance(position, config.campPosition) < config.campSafeRadius + 2f)
                {
                    index--;
                    continue;
                }

                var sprite = config.environmentSprites[random.Next(config.environmentSprites.Length)];
                if (sprite == null)
                {
                    continue;
                }

                var environmentObject = new GameObject($"Forest Prop {index + 1}");
                environmentObject.transform.position = position;
                environmentObject.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 2f, (float)random.NextDouble());
                var renderer = environmentObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1;
                environmentObject.AddComponent<YSortSprite>();
                if (sprite.bounds.size.magnitude > 0.55f)
                {
                    var collider = environmentObject.AddComponent<CircleCollider2D>();
                    collider.radius = Mathf.Clamp(sprite.bounds.extents.x * 0.45f, 0.15f, 0.65f);
                    collider.offset = new Vector2(0f, -sprite.bounds.extents.y * 0.35f);
                    environmentObject.AddComponent<HuntingObstacle>();
                }
            }
        }

        private void CreateBoundaries()
        {
            var half = config.worldSize * 0.5f;
            CreateBoundary("North", new Vector2(0f, half.y + 0.5f), new Vector2(config.worldSize.x + 2f, 1f));
            CreateBoundary("South", new Vector2(0f, -half.y - 0.5f), new Vector2(config.worldSize.x + 2f, 1f));
            CreateBoundary("East", new Vector2(half.x + 0.5f, 0f), new Vector2(1f, config.worldSize.y + 2f));
            CreateBoundary("West", new Vector2(-half.x - 0.5f, 0f), new Vector2(1f, config.worldSize.y + 2f));
        }

        private static void CreateBoundary(string name, Vector2 position, Vector2 size)
        {
            var boundary = new GameObject($"Boundary {name}");
            boundary.transform.position = position;
            var collider = boundary.AddComponent<BoxCollider2D>();
            collider.size = size;
            boundary.AddComponent<HuntingObstacle>();
        }

        private void CreateColoredArea(string areaName, Vector2 position, Vector2 size, Color color, int order)
        {
            var area = new GameObject(areaName);
            area.transform.position = position;
            area.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = area.AddComponent<SpriteRenderer>();
            renderer.sprite = solidSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        private Vector2 RandomWorldPositionOutsideCamp(float minimumCampDistance)
        {
            for (var attempt = 0; attempt < 40; attempt++)
            {
                var half = config.worldSize * 0.5f - Vector2.one * 2f;
                var position = new Vector2(
                    UnityEngine.Random.Range(-half.x, half.x),
                    UnityEngine.Random.Range(-half.y, half.y));
                if (Vector2.Distance(position, config.campPosition) >= minimumCampDistance)
                {
                    return position;
                }
            }

            return Vector2.zero;
        }

        private static Sprite CreateSolidSprite()
        {
            var texture = new Texture2D(1, 1)
            {
                name = "Runtime Solid",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class YSortSprite : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void LateUpdate()
        {
            spriteRenderer.sortingOrder = Mathf.Clamp(5 - Mathf.RoundToInt(transform.position.y * 10f), -30, 30);
        }
    }
}
