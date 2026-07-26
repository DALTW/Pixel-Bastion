using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game3.SideDefense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game3.SideDefense.Editor
{
    public static class SideDefenseSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SideDefense.unity";
        private const string MapAssetPath =
            "Assets/Project_Asset/Generated/Maps/OrcFrontier_DefenseMap.png";
        private const string TowerAssetPath =
            "Assets/Project_Asset/Generated/Towers/AlliedGuardianTower.png";
        private const int MapSegmentCount = 4;
        private const float PixelsPerUnit = 100f;

        [InitializeOnLoadMethod]
        private static void QueueInitialSceneBuild()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += TryBuildMissingScene;
        }

        [MenuItem("Tools/Side Defense/Create or Rebuild Editable Scene")]
        private static void RebuildFromMenu()
        {
            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            if (sceneExists &&
                !EditorUtility.DisplayDialog(
                    "Rebuild Side Defense Scene?",
                    "This replaces only Assets/Scenes/SideDefense.unity. " +
                    "Other scenes and project assets are not changed.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            BuildScene(false);
        }

        public static void BuildForAutomation()
        {
            BuildScene(true);
        }

        private static void TryBuildMissingScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryBuildMissingScene;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            try
            {
                BuildScene(false);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void BuildScene(bool replaceCurrentScene)
        {
            EnsureFolder("Assets/SideDefense");
            EnsureFolder("Assets/SideDefense/Scripts");
            EnsureFolder("Assets/SideDefense/Scripts/Runtime");
            EnsureFolder("Assets/SideDefense/Scripts/Editor");
            EnsureFolder("Assets/Scenes");

            Sprite mapSprite = ConfigureSpriteImporter(MapAssetPath, false);
            Sprite towerSprite = ConfigureSpriteImporter(TowerAssetPath, true);
            SideDefenseHealthBarSpriteSet towerHealthBarSprites =
                SideDefenseHealthBarSceneFactory.LoadTowerSprites();

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene buildScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                replaceCurrentScene ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(buildScene);

            try
            {
                SideDefenseMapLayout mapLayout =
                    CreateEditableWorld(
                        mapSprite,
                        towerSprite,
                        towerHealthBarSprites);
                HumanSummonSceneBuilder.Build(mapLayout);
                MonsterWaveSceneBuilder.Build(mapLayout);
                EditorSceneManager.MarkSceneDirty(buildScene);
                if (!EditorSceneManager.SaveScene(buildScene, ScenePath, false))
                {
                    throw new InvalidOperationException(
                        $"Failed to save the side-defense scene at {ScenePath}.");
                }

                AddSceneToBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"Created editable side-defense scene: {ScenePath} " +
                    $"({MapSegmentCount} map segments).");
            }
            finally
            {
                if (!replaceCurrentScene &&
                    previousActiveScene.IsValid() &&
                    previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                if (!replaceCurrentScene &&
                    buildScene.IsValid() &&
                    buildScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(buildScene, true);
                }
            }
        }

        private static SideDefenseMapLayout CreateEditableWorld(
            Sprite mapSprite,
            Sprite towerSprite,
            SideDefenseHealthBarSpriteSet towerHealthBarSprites)
        {
            GameObject world = new GameObject("SideDefenseWorld");
            SideDefenseMapLayout mapLayout = world.AddComponent<SideDefenseMapLayout>();

            Transform environment = CreateGroup("Environment", world.transform);
            Transform mapSegmentsRoot = CreateGroup(
                "Map Segments (Editable)",
                environment);

            float segmentWidth = mapSprite.bounds.size.x;
            float segmentHeight = mapSprite.bounds.size.y;
            float totalMapWidth = segmentWidth * MapSegmentCount;
            List<SpriteRenderer> mapSegments = new List<SpriteRenderer>(MapSegmentCount);

            for (int index = 0; index < MapSegmentCount; index++)
            {
                GameObject segmentObject = new GameObject(
                    $"Background_{index + 1:00} (Editable)");
                segmentObject.transform.SetParent(mapSegmentsRoot, false);
                segmentObject.transform.position = new Vector3(
                    segmentWidth * (index + 0.5f),
                    0f,
                    0f);

                SpriteRenderer renderer = segmentObject.AddComponent<SpriteRenderer>();
                renderer.sprite = mapSprite;
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = -100;
                mapSegments.Add(renderer);
            }

            GameObject groundObject = new GameObject("Ground Collision (Editable)");
            groundObject.transform.SetParent(environment, false);
            groundObject.transform.position = new Vector3(
                totalMapWidth * 0.5f,
                -2.85f,
                0f);
            BoxCollider2D groundCollider = groundObject.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(totalMapWidth, 0.4f);

            Transform bases = CreateGroup("Bases", world.transform);
            GameObject towerObject = new GameObject("Allied Tower (Editable)");
            towerObject.transform.SetParent(bases, false);

            const float towerScale = 0.42f;
            const float towerX = 3.25f;
            const float towerBottomY = -3.82f;
            float towerCenterY =
                towerBottomY + towerSprite.bounds.extents.y * towerScale;
            towerObject.transform.position = new Vector3(
                towerX,
                towerCenterY,
                -0.1f);
            towerObject.transform.localScale = Vector3.one * towerScale;

            SpriteRenderer towerRenderer = towerObject.AddComponent<SpriteRenderer>();
            towerRenderer.sprite = towerSprite;
            towerRenderer.sortingLayerName = "Default";
            towerRenderer.sortingOrder = 10;

            GameObject humanSpawnObject = new GameObject(
                "Human Spawn Point (Inside Tower)");
            humanSpawnObject.transform.SetParent(towerObject.transform, false);
            humanSpawnObject.transform.localPosition = new Vector3(0.6f, -1.75f, 0f);
            SideDefenseSpawnPoint humanSpawn =
                humanSpawnObject.AddComponent<SideDefenseSpawnPoint>();
            humanSpawn.Configure(
                SideDefenseFaction.Human,
                20,
                new Color(0.25f, 1f, 0.55f, 1f));

            GameObject towerHpAnchor = new GameObject("Tower HP Bar Anchor");
            towerHpAnchor.transform.SetParent(towerObject.transform, false);
            towerHpAnchor.transform.localPosition = new Vector3(0f, 5.7f, 0f);
            SideDefenseHealthBar towerHealthBar =
                SideDefenseHealthBarSceneFactory.Create(
                    towerHpAnchor.transform,
                    "Tower HP Bar",
                    towerHealthBarSprites,
                    2.9f,
                    30);
            SideDefenseTower alliedTower =
                towerObject.AddComponent<SideDefenseTower>();
            alliedTower.Configure("Allied Tower", 1500f);
            alliedTower.BindHealthBar(towerHealthBar);

            Transform spawns = CreateGroup("Spawn Points", world.transform);
            GameObject monsterSpawnObject = new GameObject(
                "Monster Spawn Point (Right Edge)");
            monsterSpawnObject.transform.SetParent(spawns, false);
            monsterSpawnObject.transform.position = new Vector3(
                totalMapWidth - 1.25f,
                -2.55f,
                0f);
            SideDefenseSpawnPoint monsterSpawn =
                monsterSpawnObject.AddComponent<SideDefenseSpawnPoint>();
            monsterSpawn.Configure(
                SideDefenseFaction.Monster,
                20,
                new Color(1f, 0.25f, 0.2f, 1f));

            Transform runtimeContainers = CreateGroup(
                "Runtime Containers",
                world.transform);
            Transform unitsRoot = CreateGroup("Spawned Units", runtimeContainers);
            CreateGroup("Projectiles", runtimeContainers);

            GameObject cameraObject = new GameObject("Main Camera (Horizontal)");
            cameraObject.transform.SetParent(world.transform, false);
            cameraObject.transform.position = new Vector3(
                segmentWidth * 0.5f,
                0f,
                -10f);
            cameraObject.tag = "MainCamera";

            Camera sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = segmentHeight * 0.5f;
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.2f, 0.7f, 0.82f, 1f);
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();

            HorizontalCameraController cameraController =
                cameraObject.AddComponent<HorizontalCameraController>();
            cameraController.SetWorldBounds(0f, totalMapWidth);
            cameraController.MoveToLeftEdge();

            mapLayout.Configure(
                mapSegments.ToArray(),
                towerObject.transform,
                humanSpawn,
                monsterSpawn,
                unitsRoot,
                cameraController,
                groundCollider);
            return mapLayout;
        }

        private static Sprite ConfigureSpriteImporter(
            string assetPath,
            bool alphaTransparency)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    $"Could not find a texture importer for {assetPath}.",
                    assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alphaTransparency;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Unity did not import {assetPath} as a Sprite.");
            }

            return sprite;
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject groupObject = new GameObject(name);
            groupObject.transform.SetParent(parent, false);
            return groupObject.transform;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetPath)
                ?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrWhiteSpace(parent) ||
                string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException(
                    $"Invalid Unity folder path: {assetPath}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == ScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
