using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game3.Hunting.Editor
{
    public static class HuntingGameBuilder
    {
        public const string ConfigPath = "Assets/HuntingGame/Resources/HuntingGameConfig.asset";
        public const string ScenePath = "Assets/Scenes/HuntingGame.unity";

        private const string GeneratedRoot = "Assets/HuntingGame/Generated";
        private const string DefinitionsRoot = GeneratedRoot + "/Definitions";
        private const string ResourcesRoot = "Assets/HuntingGame/Resources";
        private const string SunnysideRoot =
            "Assets/Asset/Sunnyside_World_ASSET_PACK_V2.1/Sunnyside_World_Assets";
        private const string HumanRoot = SunnysideRoot + "/Characters/Human";
        private const string AnimalRoot = SunnysideRoot + "/Elements/Animals";
        private const string PlantRoot = SunnysideRoot + "/Elements/Plants";
        private const string UiRoot = SunnysideRoot + "/UI";
        private const string Tileset = SunnysideRoot + "/Tileset/spr_tileset_sunnysideworld_16px.png";
        private const string Dogs = "Assets/Asset/DogMegaPackFree/Dogs.png";

        private static readonly string PlayerIdle = HumanRoot + "/IDLE/base_idle_strip9.png";
        private static readonly string PlayerWalk = HumanRoot + "/WALKING/base_walk_strip8.png";
        private static readonly string PlayerAttack = HumanRoot + "/ATTACK/base_attack_strip10.png";

        private static readonly AnimalSource[] AnimalSources =
        {
            new AnimalSource("bird", "들새", AnimalRoot + "/spr_deco_bird_01_strip4.png",
                15f, 4.5f, 6f, false, 0f, 0f, 1f,
                Drop(LootType.Feather, 1, 2), Drop(LootType.Meat, 1, 1, 0.5f)),
            new AnimalSource("chicken", "야생 닭", AnimalRoot + "/spr_deco_chicken_01_strip4.png",
                25f, 3.8f, 5.5f, false, 0f, 0f, 0.75f,
                Drop(LootType.Meat, 1, 2), Drop(LootType.Feather, 1, 1)),
            new AnimalSource("duck", "들오리", AnimalRoot + "/spr_deco_duck_01_strip4.png",
                30f, 3.6f, 5.8f, false, 0f, 0f, 1.1f,
                Drop(LootType.Meat, 1, 2), Drop(LootType.Feather, 1, 2)),
            new AnimalSource("sheep", "야생 양", AnimalRoot + "/spr_deco_sheep_01_strip4.png",
                55f, 3.1f, 6.2f, false, 0f, 0f, 0.8f,
                Drop(LootType.Meat, 1, 1), Drop(LootType.Wool, 2, 3)),
            new AnimalSource("pig", "멧돼지", AnimalRoot + "/spr_deco_pig_01_strip4.png",
                80f, 3f, 6.5f, true, 0.35f, 10f, 0.85f,
                Drop(LootType.Meat, 2, 3), Drop(LootType.Hide, 1, 1)),
            new AnimalSource("cow", "야생 소", AnimalRoot + "/spr_deco_cow_strip4.png",
                120f, 2.6f, 7f, true, 0.55f, 18f, 0.9f,
                Drop(LootType.Meat, 3, 4), Drop(LootType.Hide, 1, 2))
        };

        private static readonly (string path, int frames)[] EnvironmentSources =
        {
            (PlantRoot + "/spr_deco_tree_01_strip4.png", 4),
            (PlantRoot + "/spr_deco_tree_02_strip4.png", 4),
            (PlantRoot + "/spr_deco_mushroom_blue_01_strip4.png", 4),
            (PlantRoot + "/spr_deco_mushroom_blue_02_strip4.png", 4),
            (PlantRoot + "/spr_deco_mushroom_blue_03_strip4.png", 4),
            (PlantRoot + "/spr_deco_mushroom_red_01_strip4.png", 4)
        };

        [MenuItem("GAME-3/Hunting Game Setup")]
        public static void OpenSetupWindow()
        {
            HuntingGameSetupWindow.Open();
        }

        [MenuItem("GAME-3/Load Sunnyside Defaults and Rebuild")]
        public static void LoadDefaultsFromMenu()
        {
            LoadSunnysideDefaultsAndBuild();
            EditorUtility.DisplayDialog("GAME-3", "Sunnyside 기본 설정과 헌팅 씬을 생성했습니다.", "확인");
        }

        public static void BuildForBatchMode()
        {
            EnsureFolders();
            var config = GetOrCreateConfig();
            if (ValidateConfiguration(config).Count > 0)
            {
                LoadSunnysideDefaults(config);
            }
            else
            {
                ApplyImportSettings();
            }

            RebuildScene(config);
            Debug.Log("HUNTING_GAME_BUILD_COMPLETE");
        }

        public static void LoadSunnysideDefaultsAndBuild()
        {
            EnsureFolders();
            var config = GetOrCreateConfig();
            LoadSunnysideDefaults(config);
            RebuildScene(config);
        }

        public static void RebuildFromCurrentConfig()
        {
            EnsureFolders();
            var config = GetOrCreateConfig();
            var errors = ValidateConfiguration(config);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("설정 오류:\n" + string.Join("\n", errors));
            }

            RebuildScene(config);
        }

        public static HuntingGameConfig GetOrCreateConfig()
        {
            EnsureFolders();
            return LoadOrCreate<HuntingGameConfig>(ConfigPath);
        }

        public static void ApplyImportSettings()
        {
            ValidateSourceAssets();
            ConfigureStrip(PlayerIdle, 9, 96, 64);
            ConfigureStrip(PlayerWalk, 8, 96, 64);
            ConfigureStrip(PlayerAttack, 10, 96, 64);
            foreach (var source in AnimalSources)
            {
                ConfigureStrip(source.Path, 4);
            }

            foreach (var (path, frames) in EnvironmentSources)
            {
                ConfigureStrip(path, frames);
            }

            ConfigurePointTexture(Tileset, 16f, false);
            ConfigurePointTexture(Dogs, 100f, true);
            foreach (var icon in new[] { "basket.png", "sword.png", "plant.png", "hand_closed_01.png" })
            {
                ConfigurePointTexture($"{UiRoot}/{icon}", 16f, false);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public static List<string> ValidateConfiguration(HuntingGameConfig config)
        {
            var errors = new List<string>();
            if (config == null)
            {
                errors.Add("HuntingGameConfig 에셋이 없습니다.");
                return errors;
            }

            if (config.playerIdleSprites == null || config.playerIdleSprites.Length != 9)
            {
                errors.Add("플레이어 Idle 프레임은 9개여야 합니다.");
            }

            if (config.playerWalkSprites == null || config.playerWalkSprites.Length != 8)
            {
                errors.Add("플레이어 Walk 프레임은 8개여야 합니다.");
            }

            if (config.playerAttackSprites == null || config.playerAttackSprites.Length != 10)
            {
                errors.Add("플레이어 Attack 프레임은 10개여야 합니다.");
            }

            if (config.animals == null || config.animals.Length != 6 || config.animals.Any(item => item == null))
            {
                errors.Add("Sunnyside 동물 정의 6종이 모두 필요합니다.");
            }

            if (config.populations == null || config.populations.Length != 6 ||
                config.populations.Any(item => item.animal == null || item.count < 0))
            {
                errors.Add("동물 6종의 개체 수 설정이 필요합니다.");
            }

            if (config.upgrades == null || config.upgrades.Length != 3 ||
                config.upgrades.Any(item => item == null))
            {
                errors.Add("제압력·이동속도·최대체력 강화 정의가 필요합니다.");
            }

            if (config.worldTileset == null)
            {
                errors.Add("Sunnyside 월드 타일셋이 필요합니다.");
            }

            if (config.environmentSprites == null || config.environmentSprites.Length == 0)
            {
                errors.Add("월드 장식 스프라이트가 한 개 이상 필요합니다.");
            }

            if (config.lootPrices == null || config.lootPrices.Length != 4)
            {
                errors.Add("고기·가죽·털·깃털 가격이 모두 필요합니다.");
            }

            return errors;
        }

        private static void LoadSunnysideDefaults(HuntingGameConfig config)
        {
            ApplyImportSettings();
            var animals = CreateAnimals();
            var dogs = CreateDogs();
            var upgrades = CreateUpgrades();

            config.animals = animals;
            config.populations = new[]
            {
                Population(animals, "bird", 5),
                Population(animals, "chicken", 4),
                Population(animals, "duck", 4),
                Population(animals, "sheep", 4),
                Population(animals, "pig", 3),
                Population(animals, "cow", 2)
            };
            config.dogs = dogs;
            config.upgrades = upgrades;
            config.playerIdleSprites = LoadSprites(PlayerIdle);
            config.playerWalkSprites = LoadSprites(PlayerWalk);
            config.playerAttackSprites = LoadSprites(PlayerAttack);
            config.worldTileset = AssetDatabase.LoadAssetAtPath<Texture2D>(Tileset);
            config.groundTileRects = new[]
            {
                new Rect(16f, 992f, 16f, 16f),
                new Rect(32f, 992f, 16f, 16f),
                new Rect(16f, 976f, 16f, 16f),
                new Rect(32f, 976f, 16f, 16f)
            };
            config.groundSprites = Array.Empty<Sprite>();
            config.environmentSprites = EnvironmentSources
                .Select(item => LoadSprites(item.path).FirstOrDefault())
                .Where(item => item != null)
                .ToArray();
            config.meatIcon = LoadFirstSprite($"{UiRoot}/basket.png");
            config.hideIcon = LoadFirstSprite($"{UiRoot}/sword.png");
            config.woolIcon = LoadFirstSprite($"{UiRoot}/plant.png");
            config.featherIcon = LoadFirstSprite($"{UiRoot}/hand_closed_01.png");
            config.startingMoney = 50;
            config.lootPrices = new[]
            {
                Price(LootType.Meat, 12),
                Price(LootType.Hide, 28),
                Price(LootType.Wool, 20),
                Price(LootType.Feather, 8)
            };
            config.inventoryCapacity = 24;
            config.baseMoveSpeed = 4.5f;
            config.baseMaxHealth = 100f;
            config.baseSubduePower = 20f;
            config.attackRange = 1.25f;
            config.attackArc = 90f;
            config.attackCooldown = 0.7f;
            config.attackDuration = 0.5f;
            config.attackHitDelay = 0.2f;
            config.harvestDuration = 1.25f;
            config.worldSize = new Vector2(60f, 40f);
            config.campPosition = new Vector2(-23f, 0f);
            config.campSafeRadius = 7f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static AnimalDefinition[] CreateAnimals()
        {
            var definitions = new List<AnimalDefinition>();
            foreach (var source in AnimalSources)
            {
                var definition = LoadOrCreate<AnimalDefinition>(
                    $"{DefinitionsRoot}/Animal-{source.Id}.asset");
                definition.id = source.Id;
                definition.displayName = source.DisplayName;
                definition.maxResolve = source.MaxResolve;
                definition.moveSpeed = source.MoveSpeed;
                definition.detectionRadius = source.DetectionRadius;
                definition.canRetaliate = source.CanRetaliate;
                definition.retaliationChance = source.RetaliationChance;
                definition.attackRange = 1.05f;
                definition.attackDamage = source.AttackDamage;
                definition.attackCooldown = 1f;
                definition.visualScale = source.VisualScale;
                definition.lootDrops = source.Drops;
                definition.idleSprites = LoadSprites(source.Path);
                definition.moveSprites = definition.idleSprites;
                EditorUtility.SetDirty(definition);
                definitions.Add(definition);
            }

            return definitions.ToArray();
        }

        private static DogDefinition[] CreateDogs()
        {
            var dogSprites = LoadSprites(Dogs);
            var scout = LoadOrCreate<DogDefinition>($"{DefinitionsRoot}/Dog-scout.asset");
            scout.id = "scout";
            scout.displayName = "Scout";
            scout.price = 350;
            scout.detectionRadius = 8f;
            scout.subduePower = 12f;
            scout.attackCooldown = 0.8f;
            scout.moveSpeed = 5f;
            scout.sprites = SliceRange(dogSprites, 14, 7);
            EditorUtility.SetDirty(scout);

            var guardian = LoadOrCreate<DogDefinition>($"{DefinitionsRoot}/Dog-guardian.asset");
            guardian.id = "guardian";
            guardian.displayName = "Guardian";
            guardian.price = 800;
            guardian.detectionRadius = 12f;
            guardian.subduePower = 20f;
            guardian.attackCooldown = 0.7f;
            guardian.moveSpeed = 5.4f;
            guardian.sprites = SliceRange(dogSprites, 7, 7);
            EditorUtility.SetDirty(guardian);
            return new[] { scout, guardian };
        }

        private static HunterUpgradeDefinition[] CreateUpgrades()
        {
            return new[]
            {
                CreateUpgrade(HunterUpgradeType.SubduePower, "제압력",
                    "단계당 제압 피해 +25%", 0.25f, 150, 400, 900),
                CreateUpgrade(HunterUpgradeType.MoveSpeed, "이동속도",
                    "단계당 이동속도 +8%", 0.08f, 120, 320, 700),
                CreateUpgrade(HunterUpgradeType.MaxHealth, "최대 체력",
                    "단계당 최대 체력 +20", 20f, 180, 450, 1000)
            };
        }

        private static HunterUpgradeDefinition CreateUpgrade(
            HunterUpgradeType type,
            string displayName,
            string description,
            float bonus,
            params int[] costs)
        {
            var id = type.ToString().ToLowerInvariant();
            var definition = LoadOrCreate<HunterUpgradeDefinition>(
                $"{DefinitionsRoot}/Upgrade-{id}.asset");
            definition.type = type;
            definition.displayName = displayName;
            definition.description = description;
            definition.bonusPerLevel = bonus;
            definition.costs = costs;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void RebuildScene(HuntingGameConfig config)
        {
            var errors = ValidateConfiguration(config);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("설정 오류:\n" + string.Join("\n", errors));
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HuntingGame";

            var root = new GameObject("Hunting Game");
            var controller = root.AddComponent<HuntingGameController>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("config").objectReferenceValue = config;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.19f, 0.55f, 0.76f);
            cameraObject.transform.position = new Vector3(config.campPosition.x, config.campPosition.y, -10f);

            CreateGlobalLight();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateGlobalLight()
        {
            var lightType = Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (lightType == null)
            {
                return;
            }

            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent(lightType);
            var serializedLight = new SerializedObject(light);
            var lightTypeProperty = serializedLight.FindProperty("m_LightType");
            if (lightTypeProperty != null)
            {
                lightTypeProperty.intValue = 4;
            }

            var intensity = serializedLight.FindProperty("m_Intensity");
            if (intensity != null)
            {
                intensity.floatValue = 1f;
            }

            serializedLight.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStrip(
            string path,
            int frameCount,
            int expectedWidth = 0,
            int expectedHeight = 0)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new FileNotFoundException($"스프라이트를 찾을 수 없습니다: {path}");
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var width = expectedWidth > 0 ? expectedWidth : texture.width / frameCount;
            var height = expectedHeight > 0 ? expectedHeight : texture.height;
            if (texture.width != width * frameCount || texture.height != height)
            {
                throw new InvalidDataException(
                    $"{path} 크기가 예상과 다릅니다. 실제 {texture.width}x{texture.height}, 예상 {width * frameCount}x{height}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var baseName = Path.GetFileNameWithoutExtension(path);
            var sprites = new SpriteMetaData[frameCount];
            for (var index = 0; index < frameCount; index++)
            {
                sprites[index] = new SpriteMetaData
                {
                    name = $"{baseName}_{index}",
                    rect = new Rect(index * width, 0f, width, height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.16f)
                };
            }

#pragma warning disable CS0618
            importer.spritesheet = sprites;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static void ConfigurePointTexture(string path, float pixelsPerUnit, bool multiple)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                throw new FileNotFoundException($"텍스처를 찾을 수 없습니다: {path}");
            }

            importer.textureType = TextureImporterType.Sprite;
            if (!multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }

            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void ValidateSourceAssets()
        {
            var required = new List<string> { PlayerIdle, PlayerWalk, PlayerAttack, Tileset, Dogs };
            required.AddRange(AnimalSources.Select(item => item.Path));
            required.AddRange(EnvironmentSources.Select(item => item.path));
            required.AddRange(new[]
            {
                $"{UiRoot}/basket.png",
                $"{UiRoot}/sword.png",
                $"{UiRoot}/plant.png",
                $"{UiRoot}/hand_closed_01.png"
            });

            var missing = required.Where(path => AssetDatabase.LoadMainAssetAtPath(path) == null).ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException("필수 Sunnyside 에셋을 찾을 수 없습니다:\n" +
                                                string.Join("\n", missing));
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && MonoScript.FromScriptableObject(asset) != null)
            {
                return asset;
            }

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Sprite[] LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => NumericSuffix(sprite.name))
                .ToArray();
        }

        private static Sprite LoadFirstSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? LoadSprites(path).FirstOrDefault();
        }

        private static Sprite[] SliceRange(Sprite[] sprites, int start, int count)
        {
            if (sprites == null || sprites.Length == 0)
            {
                return Array.Empty<Sprite>();
            }

            start = Mathf.Clamp(start, 0, sprites.Length - 1);
            count = Mathf.Clamp(count, 1, sprites.Length - start);
            var result = new Sprite[count];
            Array.Copy(sprites, start, result, 0, count);
            return result;
        }

        private static int NumericSuffix(string name)
        {
            var separator = name.LastIndexOf('_');
            return separator >= 0 && int.TryParse(name[(separator + 1)..], out var value) ? value : 0;
        }

        private static AnimalPopulation Population(AnimalDefinition[] definitions, string id, int count)
        {
            return new AnimalPopulation
            {
                animal = definitions.First(item => item.id == id),
                count = count
            };
        }

        private static LootDrop Drop(LootType type, int min, int max, float chance = 1f)
        {
            return new LootDrop { type = type, minAmount = min, maxAmount = max, chance = chance };
        }

        private static LootPrice Price(LootType type, int price)
        {
            return new LootPrice { type = type, price = price };
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/HuntingGame");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(DefinitionsRoot);
            EnsureFolder(ResourcesRoot);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", name);
        }

        private sealed class AnimalSource
        {
            public string Id { get; }
            public string DisplayName { get; }
            public string Path { get; }
            public float MaxResolve { get; }
            public float MoveSpeed { get; }
            public float DetectionRadius { get; }
            public bool CanRetaliate { get; }
            public float RetaliationChance { get; }
            public float AttackDamage { get; }
            public float VisualScale { get; }
            public LootDrop[] Drops { get; }

            public AnimalSource(
                string id,
                string displayName,
                string path,
                float maxResolve,
                float moveSpeed,
                float detectionRadius,
                bool canRetaliate,
                float retaliationChance,
                float attackDamage,
                float visualScale,
                params LootDrop[] drops)
            {
                Id = id;
                DisplayName = displayName;
                Path = path;
                MaxResolve = maxResolve;
                MoveSpeed = moveSpeed;
                DetectionRadius = detectionRadius;
                CanRetaliate = canRetaliate;
                RetaliationChance = retaliationChance;
                AttackDamage = attackDamage;
                VisualScale = visualScale;
                Drops = drops;
            }
        }
    }
}
