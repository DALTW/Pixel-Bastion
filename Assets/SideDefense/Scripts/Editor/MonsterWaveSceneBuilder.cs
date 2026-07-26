using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game3.SideDefense;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game3.SideDefense.Editor
{
    internal static class MonsterWaveSceneBuilder
    {
        private const string MonsterRoot =
            "Assets/Project_Asset/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/" +
            "Characters(100x100)/Monster";
        private const string PrefabFolder =
            "Assets/SideDefense/Prefabs/Monster";
        private static readonly string SkeletonArrowPath =
            $"{MonsterRoot}/Skeleton Archer/Arrow(projectile)/" +
            "Arrow03(32x32).png";

        private static readonly MonsterSpec[] MonsterSpecs =
        {
            CreateSpec("Slime", 60f, 0.78f, 8f, 1.2f, 0.68f, 8),
            CreateSpec("Skeleton", 90f, 0.9f, 10f, 1.05f, 0.72f, 10),
            CreateSpec("Orc", 115f, 0.82f, 13f, 1.05f, 0.75f, 12),
            CreateSpec(
                "Skeleton Archer",
                80f,
                0.76f,
                12f,
                1.25f,
                3.25f,
                13),
            CreateSpec(
                "Armored Skeleton",
                155f,
                0.7f,
                17f,
                1.15f,
                0.76f,
                16),
            CreateSpec(
                "Armored Orc",
                185f,
                0.68f,
                20f,
                1.15f,
                0.8f,
                18),
            CreateSpec(
                "Greatsword Skeleton",
                215f,
                0.62f,
                25f,
                1.3f,
                0.9f,
                21),
            CreateSpec(
                "Werewolf",
                170f,
                1.08f,
                22f,
                0.82f,
                0.78f,
                22),
            CreateSpec(
                "Elite Orc",
                270f,
                0.64f,
                30f,
                1.18f,
                0.85f,
                26),
            CreateSpec(
                "Orc rider",
                245f,
                1.02f,
                28f,
                0.92f,
                0.9f,
                28),
            CreateSpec(
                "Werebear",
                380f,
                0.58f,
                38f,
                1.35f,
                1f,
                36)
        };

        public static void Build(SideDefenseMapLayout mapLayout)
        {
            if (mapLayout == null)
            {
                throw new ArgumentNullException(nameof(mapLayout));
            }

            EnsureFolder("Assets/SideDefense");
            EnsureFolder("Assets/SideDefense/Prefabs");
            EnsureFolder(PrefabFolder);

            SideDefenseHealthBarSpriteSet healthBarSprites =
                SideDefenseHealthBarSceneFactory.LoadMonsterSprites();
            GameObject skeletonArrowPrefab =
                SideDefenseProjectileSceneFactory.CreateOrReplace(
                    "Monster Arrow",
                    SkeletonArrowPath,
                    false,
                    2.2f);
            List<GameObject> monsterPrefabs =
                new List<GameObject>(MonsterSpecs.Length);

            foreach (MonsterSpec spec in MonsterSpecs)
            {
                Sprite[] walkFrames = LoadAnimationFrames(spec.WalkAssetPath);
                Sprite[] attackFrames =
                    LoadAnimationFrames(spec.AttackAssetPath);
                GameObject projectilePrefab =
                    spec.DisplayName == "Skeleton Archer"
                        ? skeletonArrowPrefab
                        : null;
                monsterPrefabs.Add(
                    CreateOrReplaceMonsterPrefab(
                        spec,
                        walkFrames,
                        attackFrames,
                        projectilePrefab,
                        healthBarSprites));
            }

            SideDefenseTower tower =
                mapLayout.AlliedTower.GetComponent<SideDefenseTower>();
            if (tower == null)
            {
                throw new InvalidOperationException(
                    "The allied tower is missing SideDefenseTower.");
            }

            HumanSummonController summonController =
                UnityEngine.Object.FindAnyObjectByType<HumanSummonController>();
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (summonController == null || canvas == null)
            {
                throw new InvalidOperationException(
                    "Human summon UI must be built before Monster waves.");
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text waveLabel = CreateWaveStatus(canvas.transform, font);
            GameObject defeatOverlay = CreateDefeatOverlay(
                canvas.transform,
                font,
                out Button restartButton);

            GameObject systemsObject =
                new GameObject("Monster Waves & Game Flow");
            SideDefenseMonsterWaveController waveController =
                systemsObject.AddComponent<SideDefenseMonsterWaveController>();
            waveController.Configure(
                mapLayout,
                tower,
                summonController,
                monsterPrefabs.ToArray(),
                waveLabel);

            SideDefenseGameFlow gameFlow =
                systemsObject.AddComponent<SideDefenseGameFlow>();
            gameFlow.Configure(
                tower,
                waveController,
                summonController,
                defeatOverlay,
                restartButton);
        }

        private static MonsterSpec CreateSpec(
            string displayName,
            float maxHealth,
            float movementSpeed,
            float attackDamage,
            float attackInterval,
            float attackRange,
            int coinReward)
        {
            string folder =
                $"{MonsterRoot}/{displayName}/{displayName} with shadows";
            string attackFile = displayName == "Skeleton Archer"
                ? $"{displayName}-Attack.png"
                : $"{displayName}-Attack01.png";
            return new MonsterSpec(
                displayName,
                $"{folder}/{displayName}-Walk.png",
                $"{folder}/{attackFile}",
                maxHealth,
                movementSpeed,
                attackDamage,
                attackInterval,
                attackRange,
                coinReward);
        }

        private static Sprite[] LoadAnimationFrames(string assetPath)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    $"Missing Monster walk texture: {assetPath}",
                    assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite[] frames = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => FrameIndex(sprite.name))
                .ToArray();
            if (frames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No sliced Monster sprites were found in {assetPath}.");
            }

            return frames;
        }

        private static int FrameIndex(string spriteName)
        {
            int separator = spriteName.LastIndexOf('_');
            if (separator >= 0 &&
                int.TryParse(
                    spriteName.Substring(separator + 1),
                    out int index))
            {
                return index;
            }

            return int.MaxValue;
        }

        private static GameObject CreateOrReplaceMonsterPrefab(
            MonsterSpec spec,
            Sprite[] walkFrames,
            Sprite[] attackFrames,
            GameObject projectilePrefab,
            SideDefenseHealthBarSpriteSet healthBarSprites)
        {
            string prefabPath =
                $"{PrefabFolder}/{SanitizeFileName(spec.DisplayName)}.prefab";

            GameObject root = new GameObject(spec.DisplayName);
            try
            {
                root.transform.localScale = Vector3.one * 5.5f;

                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = walkFrames[0];
                renderer.flipX = true;
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 20;

                SideDefenseSpriteAnimator animator =
                    root.AddComponent<SideDefenseSpriteAnimator>();
                float attackFramesPerSecond =
                    attackFrames.Length /
                    Mathf.Max(0.1f, spec.AttackInterval);
                animator.ConfigureCombatAnimations(
                    walkFrames,
                    10f,
                    attackFrames,
                    attackFramesPerSecond);

                SideDefenseMonsterUnit monster =
                    root.AddComponent<SideDefenseMonsterUnit>();
                monster.Configure(
                    spec.DisplayName,
                    spec.MaxHealth,
                    spec.MovementSpeed,
                    spec.AttackDamage,
                    spec.AttackInterval,
                    spec.AttackRange,
                    spec.CoinReward);

                CapsuleCollider2D collider =
                    root.AddComponent<CapsuleCollider2D>();
                collider.size = new Vector2(0.13f, 0.18f);
                collider.offset = new Vector2(0f, -0.01f);

                GameObject projectileOriginObject =
                    new GameObject("Projectile Origin");
                projectileOriginObject.transform.SetParent(
                    root.transform,
                    false);
                projectileOriginObject.transform.localPosition =
                    new Vector3(-0.08f, 0.04f, 0f);
                SideDefenseAttackStyle attackStyle =
                    projectilePrefab == null
                        ? SideDefenseAttackStyle.Melee
                        : SideDefenseAttackStyle.Ranged;
                monster.ConfigureAttackPresentation(
                    attackStyle,
                    projectilePrefab,
                    7.2f,
                    projectileOriginObject.transform);

                GameObject healthBarAnchor =
                    new GameObject("HP Bar Anchor");
                healthBarAnchor.transform.SetParent(root.transform, false);
                healthBarAnchor.transform.localPosition =
                    new Vector3(0f, 0.19f, 0f);
                SideDefenseHealthBar healthBar =
                    SideDefenseHealthBarSceneFactory.Create(
                        healthBarAnchor.transform,
                        "Monster HP Bar",
                        healthBarSprites,
                        1.3f,
                        30);
                monster.BindHealthBar(healthBar);

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create Monster prefab: {prefabPath}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(
                prefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static Text CreateWaveStatus(Transform parent, Font font)
        {
            Image badge = CreateImage(
                "Monster Wave Status",
                parent,
                new Color(0.18f, 0.035f, 0.045f, 0.94f));
            SetRect(
                badge.rectTransform,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-18f, -18f),
                new Vector2(255f, 62f),
                new Vector2(1f, 1f));
            AddOutline(
                badge.gameObject,
                new Color(0.95f, 0.24f, 0.18f, 0.95f),
                2f);

            Text label = CreateText(
                "Wave Label",
                badge.transform,
                font,
                18,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.72f, 1f));
            label.text = "WAVE 1\nTHREAT x1.0";
            SetStretch(
                label.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(8f, 4f),
                new Vector2(-8f, -4f));
            return label;
        }

        private static GameObject CreateDefeatOverlay(
            Transform parent,
            Font font,
            out Button restartButton)
        {
            Image overlay = CreateImage(
                "Defeat Overlay",
                parent,
                new Color(0.04f, 0.01f, 0.015f, 0.86f));
            SetStretch(
                overlay.rectTransform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            Image panel = CreateImage(
                "Defeat Panel",
                overlay.transform,
                new Color(0.22f, 0.035f, 0.04f, 0.98f));
            SetRect(
                panel.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(520f, 260f),
                new Vector2(0.5f, 0.5f));
            AddOutline(
                panel.gameObject,
                new Color(0.95f, 0.2f, 0.12f, 1f),
                4f);

            Text title = CreateText(
                "Defeat Title",
                panel.transform,
                font,
                52,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.28f, 0.18f, 1f));
            title.text = "DEFEAT";
            SetRect(
                title.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -22f),
                new Vector2(470f, 72f),
                new Vector2(0.5f, 1f));

            Text message = CreateText(
                "Defeat Message",
                panel.transform,
                font,
                23,
                TextAnchor.MiddleCenter,
                Color.white);
            message.text = "THE ALLIED TOWER HAS FALLEN";
            SetRect(
                message.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 12f),
                new Vector2(470f, 46f),
                new Vector2(0.5f, 0.5f));

            Image restartImage = CreateImage(
                "Restart Button",
                panel.transform,
                new Color(0.95f, 0.48f, 0.08f, 1f));
            SetRect(
                restartImage.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(220f, 62f),
                new Vector2(0.5f, 0f));
            restartButton = restartImage.gameObject.AddComponent<Button>();
            restartButton.targetGraphic = restartImage;

            Text restartLabel = CreateText(
                "Restart Label",
                restartImage.transform,
                font,
                26,
                TextAnchor.MiddleCenter,
                new Color(0.16f, 0.07f, 0.01f, 1f));
            restartLabel.text = "RESTART";
            SetStretch(
                restartLabel.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(5f, 5f),
                new Vector2(-5f, -5f));

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(10, fontSize - 8);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Outline AddOutline(
            GameObject target,
            Color color,
            float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetStretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            return value;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
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

        private readonly struct MonsterSpec
        {
            public MonsterSpec(
                string displayName,
                string walkAssetPath,
                string attackAssetPath,
                float maxHealth,
                float movementSpeed,
                float attackDamage,
                float attackInterval,
                float attackRange,
                int coinReward)
            {
                DisplayName = displayName;
                WalkAssetPath = walkAssetPath;
                AttackAssetPath = attackAssetPath;
                MaxHealth = maxHealth;
                MovementSpeed = movementSpeed;
                AttackDamage = attackDamage;
                AttackInterval = attackInterval;
                AttackRange = attackRange;
                CoinReward = coinReward;
            }

            public string DisplayName { get; }
            public string WalkAssetPath { get; }
            public string AttackAssetPath { get; }
            public float MaxHealth { get; }
            public float MovementSpeed { get; }
            public float AttackDamage { get; }
            public float AttackInterval { get; }
            public float AttackRange { get; }
            public int CoinReward { get; }
        }
    }
}
