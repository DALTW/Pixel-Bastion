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
        private const string GeneratedRoot = "Assets/HuntingGame/Generated";
        private const string DefinitionsRoot = GeneratedRoot + "/Definitions";
        private const string ResourcesRoot = "Assets/HuntingGame/Resources";
        private const string ConfigPath = ResourcesRoot + "/HuntingGameConfig.asset";
        private const string ScenePath = "Assets/Scenes/HuntingGame.unity";

        private const string CharacterIdle =
            "Assets/Asset/Farm RPG FREE 16x16 - Tiny Asset Pack/Character/Idle.png";
        private const string CharacterWalk =
            "Assets/Asset/Farm RPG FREE 16x16 - Tiny Asset Pack/Character/Walk.png";
        private const string RabbitIdle = "Assets/Asset/AllBunniesFree/BunnyBrown/Idle.png";
        private const string RabbitRun = "Assets/Asset/AllBunniesFree/BunnyBrown/Running.png";
        private const string Dogs = "Assets/Asset/DogMegaPackFree/Dogs.png";
        private const string DogItems = "Assets/Asset/DogMegaPackFree/DogItems.png";
        private const string Environment = "Assets/Asset/FreeEnvironment/FreePack.png";
        private const string GunRoot =
            "Assets/Asset/Guns_V1.01 - Commission - Copy/01 - Individual sprites/Guns";

        [MenuItem("GAME-3/Build Hunting Game")]
        public static void BuildFromMenu()
        {
            BuildAll();
            EditorUtility.DisplayDialog("GAME-3", "헌팅 게임 데이터와 씬을 생성했습니다.", "확인");
        }

        public static void BuildForBatchMode()
        {
            BuildAll();
            Debug.Log("HUNTING_GAME_BUILD_COMPLETE");
        }

        private static void BuildAll()
        {
            EnsureFolders();
            ValidateSourceAssets();
            NormalizeTextureImports();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var weapons = CreateWeapons();
            var animals = CreateAnimals();
            var dogs = CreateDogs();
            var config = CreateConfig(weapons, animals.rabbit, animals.wolf, dogs);
            CreateScene(config);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/HuntingGame");
            EnsureFolder("Assets/HuntingGame/Generated");
            EnsureFolder(DefinitionsRoot);
            EnsureFolder(ResourcesRoot);
        }

        private static void ValidateSourceAssets()
        {
            var required = new[]
            {
                CharacterIdle, CharacterWalk, RabbitIdle, RabbitRun, Dogs, DogItems, Environment,
                $"{GunRoot}/Glock - P80 [64x48].png",
                $"{GunRoot}/Revolver - Colt 45 [64x32].png",
                $"{GunRoot}/Submachine - MP5A3 [80x48].png",
                $"{GunRoot}/AK 47 [96x48].png"
            };

            var missing = required.Where(path => AssetDatabase.LoadMainAssetAtPath(path) == null).ToArray();
            if (missing.Length > 0)
            {
                throw new FileNotFoundException("필수 스프라이트를 찾을 수 없습니다:\n" + string.Join("\n", missing));
            }
        }

        private static void NormalizeTextureImports()
        {
            var textures = new[]
            {
                CharacterIdle, CharacterWalk, RabbitIdle, RabbitRun, Dogs, DogItems, Environment,
                $"{GunRoot}/Glock - P80 [64x48].png",
                $"{GunRoot}/Revolver - Colt 45 [64x32].png",
                $"{GunRoot}/Submachine - MP5A3 [80x48].png",
                $"{GunRoot}/AK 47 [96x48].png"
            };

            foreach (var path in textures)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                var changed = importer.filterMode != FilterMode.Point ||
                              importer.textureCompression != TextureImporterCompression.Uncompressed ||
                              importer.mipmapEnabled;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static WeaponDefinition[] CreateWeapons()
        {
            return new[]
            {
                CreateWeapon("glock", "Glock", 0, 25f, 0.28f, 8, 1.1f, false, 24, 30,
                    $"{GunRoot}/Glock - P80 [64x48].png"),
                CreateWeapon("revolver", "Revolver", 180, 45f, 0.5f, 6, 1.4f, false, 24, 30,
                    $"{GunRoot}/Revolver - Colt 45 [64x32].png"),
                CreateWeapon("mp5", "MP5", 500, 14f, 0.12f, 20, 1.6f, true, 40, 60,
                    $"{GunRoot}/Submachine - MP5A3 [80x48].png"),
                CreateWeapon("ak47", "AK-47", 1000, 28f, 0.18f, 24, 1.8f, true, 30, 75,
                    $"{GunRoot}/AK 47 [96x48].png")
            };
        }

        private static WeaponDefinition CreateWeapon(
            string id,
            string displayName,
            int price,
            float damage,
            float fireInterval,
            int magazineSize,
            float reloadTime,
            bool automatic,
            int ammoBundleSize,
            int ammoBundlePrice,
            string spritePath)
        {
            var definition = LoadOrCreate<WeaponDefinition>($"{DefinitionsRoot}/Weapon-{id}.asset");
            definition.id = id;
            definition.displayName = displayName;
            definition.price = price;
            definition.damage = damage;
            definition.fireInterval = fireInterval;
            definition.magazineSize = magazineSize;
            definition.reloadTime = reloadTime;
            definition.automatic = automatic;
            definition.ammoBundleSize = ammoBundleSize;
            definition.ammoBundlePrice = ammoBundlePrice;
            definition.worldSprite = LoadFirstSprite(spritePath);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static (AnimalDefinition rabbit, AnimalDefinition wolf) CreateAnimals()
        {
            var rabbit = LoadOrCreate<AnimalDefinition>($"{DefinitionsRoot}/Animal-rabbit.asset");
            rabbit.id = "rabbit";
            rabbit.displayName = "산토끼";
            rabbit.hostile = false;
            rabbit.maxHealth = 35f;
            rabbit.moveSpeed = 3.5f;
            rabbit.detectionRadius = 5.5f;
            rabbit.meatYield = 1;
            rabbit.hideYield = 1;
            rabbit.hideChance = 0.35f;
            rabbit.idleSprites = LoadSprites(RabbitIdle);
            rabbit.moveSprites = LoadSprites(RabbitRun);
            EditorUtility.SetDirty(rabbit);

            var dogSprites = LoadSprites(Dogs);
            var wolfFrames = SliceRange(dogSprites, 21, 7);
            var wolf = LoadOrCreate<AnimalDefinition>($"{DefinitionsRoot}/Animal-wolf.asset");
            wolf.id = "wolf";
            wolf.displayName = "야생 늑대";
            wolf.hostile = true;
            wolf.maxHealth = 90f;
            wolf.moveSpeed = 2.8f;
            wolf.detectionRadius = 7.5f;
            wolf.attackRange = 1.05f;
            wolf.attackDamage = 15f;
            wolf.attackCooldown = 1f;
            wolf.meatYield = 2;
            wolf.hideYield = 1;
            wolf.hideChance = 1f;
            wolf.idleSprites = wolfFrames;
            wolf.moveSprites = wolfFrames;
            EditorUtility.SetDirty(wolf);
            return (rabbit, wolf);
        }

        private static DogDefinition[] CreateDogs()
        {
            var dogSprites = LoadSprites(Dogs);
            var scout = LoadOrCreate<DogDefinition>($"{DefinitionsRoot}/Dog-scout.asset");
            scout.id = "scout";
            scout.displayName = "Scout";
            scout.price = 350;
            scout.detectionRadius = 8f;
            scout.damage = 12f;
            scout.attackCooldown = 0.8f;
            scout.moveSpeed = 5f;
            scout.sprites = SliceRange(dogSprites, 14, 7);
            EditorUtility.SetDirty(scout);

            var guardian = LoadOrCreate<DogDefinition>($"{DefinitionsRoot}/Dog-guardian.asset");
            guardian.id = "guardian";
            guardian.displayName = "Guardian";
            guardian.price = 800;
            guardian.detectionRadius = 12f;
            guardian.damage = 20f;
            guardian.attackCooldown = 0.7f;
            guardian.moveSpeed = 5.4f;
            guardian.sprites = SliceRange(dogSprites, 7, 7);
            EditorUtility.SetDirty(guardian);
            return new[] { scout, guardian };
        }

        private static HuntingGameConfig CreateConfig(
            WeaponDefinition[] weapons,
            AnimalDefinition rabbit,
            AnimalDefinition wolf,
            DogDefinition[] dogs)
        {
            var config = LoadOrCreate<HuntingGameConfig>(ConfigPath);
            config.weapons = weapons;
            config.rabbit = rabbit;
            config.wolf = wolf;
            config.dogs = dogs;
            config.playerIdleSprites = LoadSprites(CharacterIdle);
            config.playerWalkSprites = LoadSprites(CharacterWalk);

            var environment = LoadSprites(Environment);
            var environmentIndices = new[] { 2, 3, 4, 11, 12, 15, 16, 17, 18, 19, 20 };
            config.environmentSprites = environmentIndices
                .Where(index => index >= 0 && index < environment.Length)
                .Select(index => environment[index])
                .ToArray();

            var dogItems = LoadSprites(DogItems);
            config.meatIcon = dogItems.ElementAtOrDefault(2);
            config.hideIcon = dogItems.ElementAtOrDefault(6);
            config.startingMoney = 50;
            config.meatSellPrice = 12;
            config.hideSellPrice = 28;
            config.inventoryCapacity = 24;
            config.worldSize = new Vector2(60f, 40f);
            config.campPosition = new Vector2(-23f, 0f);
            config.campSafeRadius = 7f;
            config.rabbitPopulation = 14;
            config.wolfPopulation = 5;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void CreateScene(HuntingGameConfig config)
        {
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
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.08f);
            cameraObject.transform.position = new Vector3(config.campPosition.x, config.campPosition.y, -10f);

            CreateGlobalLight();
            EditorSceneManager.SaveScene(scene, ScenePath);
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

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
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
    }
}
