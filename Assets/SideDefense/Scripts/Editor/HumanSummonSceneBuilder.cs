using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game3.SideDefense;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Game3.SideDefense.Editor
{
    internal static class HumanSummonSceneBuilder
    {
        private const string HumanRoot =
            "Assets/Project_Asset/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/" +
            "Characters(100x100)/Human";
        private const string PrefabFolder = "Assets/SideDefense/Prefabs/Human";
        private const string CoinAssetPath =
            "Assets/Project_Asset/Generated/UI/Currency/GoldCoin.png";
        private static readonly string ArcherProjectilePath =
            $"{HumanRoot}/Archer/Arrow(projectile)/Arrow02(32x32).png";
        private static readonly string PriestProjectilePath =
            $"{HumanRoot}/Priest/Magic(projectile)/Priest-Attack_Effect.png";
        private static readonly string WizardProjectilePath =
            $"{HumanRoot}/Wizard/Magic(projectile)/Wizard-Attack01_Effect.png";

        private static readonly CharacterSpec[] CharacterSpecs =
        {
            new CharacterSpec(
                "Soldier",
                $"{HumanRoot}/Soldier/Soldier with shadows/Soldier-Idle.png",
                $"{HumanRoot}/Soldier/Soldier with shadows/Soldier-Walk.png",
                60,
                100f,
                1.35f),
            new CharacterSpec(
                "Archer",
                $"{HumanRoot}/Archer/Archer with shadows/Archer-Idle.png",
                $"{HumanRoot}/Archer/Archer with shadows/Archer-Walk.png",
                75,
                85f,
                1.45f),
            new CharacterSpec(
                "Swordsman",
                $"{HumanRoot}/Swordsman/Swordsman with shadows/Swordsman-Idle.png",
                $"{HumanRoot}/Swordsman/Swordsman with shadows/Swordsman-Walk.png",
                95,
                115f,
                1.3f),
            new CharacterSpec(
                "Knight",
                $"{HumanRoot}/Knight/Knight with shadows/Knight-Idle.png",
                $"{HumanRoot}/Knight/Knight with shadows/Knight-Walk.png",
                100,
                155f,
                1.1f),
            new CharacterSpec(
                "Lancer",
                $"{HumanRoot}/Lancer/Lancer with shadows/Lancer-Idle.png",
                $"{HumanRoot}/Lancer/Lancer with shadows/Lancer-Walk01.png",
                115,
                160f,
                1.25f),
            new CharacterSpec(
                "Armored Axeman",
                $"{HumanRoot}/Armored Axeman/Armored Axeman with shadows/" +
                "Armored Axeman-Idle.png",
                $"{HumanRoot}/Armored Axeman/Armored Axeman with shadows/" +
                "Armored Axeman-Walk.png",
                130,
                220f,
                0.9f),
            new CharacterSpec(
                "Priest",
                $"{HumanRoot}/Priest/Priest with shadows/Priest-Idle.png",
                $"{HumanRoot}/Priest/Priest with shadows/Priest-Walk.png",
                145,
                130f,
                1.05f),
            new CharacterSpec(
                "Wizard",
                $"{HumanRoot}/Wizard/Wizard with shadows/Wizard-Idle.png",
                $"{HumanRoot}/Wizard/Wizard with shadows/Wizard-Walk.png",
                160,
                115f,
                1f),
            new CharacterSpec(
                "Knight Templar",
                $"{HumanRoot}/Knight Templar/Knight Templar with shadows/" +
                "Knight Templar-Idle.png",
                $"{HumanRoot}/Knight Templar/Knight Templar with shadows/" +
                "Knight Templar-Walk01.png",
                170,
                300f,
                0.85f)
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

            Sprite coinSprite = ConfigureSingleSprite(CoinAssetPath);
            SideDefenseHealthBarSpriteSet healthBarSprites =
                SideDefenseHealthBarSceneFactory.LoadHumanSprites();
            Dictionary<string, GameObject> rangedProjectiles =
                CreateRangedProjectilePrefabs();
            List<HumanUiEntry> entries = new List<HumanUiEntry>(CharacterSpecs.Length);

            foreach (CharacterSpec spec in CharacterSpecs)
            {
                Sprite[] idleFrames = LoadAnimationFrames(spec.IdleAssetPath);
                Sprite[] walkFrames = LoadAnimationFrames(spec.WalkAssetPath);
                Sprite[] attackFrames = LoadAnimationFrames(
                    GetAttackAssetPath(spec.DisplayName));
                rangedProjectiles.TryGetValue(
                    spec.DisplayName,
                    out GameObject projectilePrefab);
                GameObject prefab = CreateOrReplaceHumanPrefab(
                    spec,
                    walkFrames,
                    attackFrames,
                    projectilePrefab,
                    healthBarSprites);
                entries.Add(new HumanUiEntry(spec, prefab, idleFrames[0]));
            }

            RectTransform bottomPanel =
                CreateSummonUi(mapLayout, coinSprite, entries);

            SideDefenseCameraViewport cameraViewport =
                mapLayout.CameraController.gameObject
                    .AddComponent<SideDefenseCameraViewport>();
            cameraViewport.Configure(
                bottomPanel,
                178f / 720f,
                6f);

            CreateEventSystem();
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
                    $"Missing Human idle texture: {assetPath}",
                    assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite[] frames = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => FrameIndex(sprite.name))
                .ToArray();

            if (frames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No sliced sprites were found in {assetPath}.");
            }

            return frames;
        }

        private static int FrameIndex(string spriteName)
        {
            int separator = spriteName.LastIndexOf('_');
            if (separator >= 0 &&
                int.TryParse(spriteName.Substring(separator + 1), out int index))
            {
                return index;
            }

            return int.MaxValue;
        }

        private static GameObject CreateOrReplaceHumanPrefab(
            CharacterSpec spec,
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
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 20;

                GetHumanCombatStats(
                    spec.DisplayName,
                    out float attackDamage,
                    out float attackInterval,
                    out float attackRange);

                SideDefenseSpriteAnimator animator =
                    root.AddComponent<SideDefenseSpriteAnimator>();
                float attackFramesPerSecond =
                    attackFrames.Length / Mathf.Max(0.1f, attackInterval);
                animator.ConfigureCombatAnimations(
                    walkFrames,
                    10f,
                    attackFrames,
                    attackFramesPerSecond);

                SideDefenseHumanUnit unit =
                    root.AddComponent<SideDefenseHumanUnit>();
                unit.Configure(
                    spec.DisplayName,
                    spec.MaxHealth,
                    spec.MovementSpeed,
                    attackDamage,
                    attackInterval,
                    attackRange);

                CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
                collider.size = new Vector2(0.13f, 0.18f);
                collider.offset = new Vector2(0f, -0.01f);

                GameObject projectileOriginObject =
                    new GameObject("Projectile Origin");
                projectileOriginObject.transform.SetParent(
                    root.transform,
                    false);
                projectileOriginObject.transform.localPosition =
                    new Vector3(0.08f, 0.04f, 0f);
                SideDefenseAttackStyle attackStyle =
                    projectilePrefab == null
                        ? SideDefenseAttackStyle.Melee
                        : SideDefenseAttackStyle.Ranged;
                unit.ConfigureAttackPresentation(
                    attackStyle,
                    projectilePrefab,
                    GetProjectileSpeed(spec.DisplayName),
                    projectileOriginObject.transform);
                if (spec.DisplayName == "Priest")
                {
                    unit.ConfigureHealing(28f, attackRange);
                }

                GameObject healthBarAnchor = new GameObject("HP Bar Anchor");
                healthBarAnchor.transform.SetParent(root.transform, false);
                healthBarAnchor.transform.localPosition = new Vector3(0f, 0.19f, 0f);
                SideDefenseHealthBar healthBar =
                    SideDefenseHealthBarSceneFactory.Create(
                        healthBarAnchor.transform,
                        "Human HP Bar",
                        healthBarSprites,
                        1.25f,
                        30);
                unit.BindHealthBar(healthBar);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create Human prefab: {prefabPath}");
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

        private static Dictionary<string, GameObject>
            CreateRangedProjectilePrefabs()
        {
            return new Dictionary<string, GameObject>
            {
                {
                    "Archer",
                    SideDefenseProjectileSceneFactory.CreateOrReplace(
                        "Human Arrow",
                        ArcherProjectilePath,
                        false,
                        2.2f)
                },
                {
                    "Priest",
                    SideDefenseProjectileSceneFactory.CreateOrReplace(
                        "Holy Bolt",
                        PriestProjectilePath,
                        true,
                        2f)
                },
                {
                    "Wizard",
                    SideDefenseProjectileSceneFactory.CreateOrReplace(
                        "Wizard Magic",
                        WizardProjectilePath,
                        true,
                        2f)
                }
            };
        }

        private static string GetAttackAssetPath(string displayName)
        {
            string attackName = displayName == "Priest"
                ? $"{displayName}-Attack.png"
                : $"{displayName}-Attack01.png";
            return $"{HumanRoot}/{displayName}/{displayName} with shadows/" +
                   attackName;
        }

        private static float GetProjectileSpeed(string displayName)
        {
            switch (displayName)
            {
                case "Archer":
                    return 7.5f;
                case "Priest":
                    return 6.2f;
                case "Wizard":
                    return 5.6f;
                default:
                    return 6f;
            }
        }

        private static void GetHumanCombatStats(
            string displayName,
            out float damage,
            out float attackInterval,
            out float attackRange)
        {
            damage = 18f;
            attackInterval = 0.9f;
            attackRange = 0.75f;

            switch (displayName)
            {
                case "Archer":
                    damage = 17f;
                    attackInterval = 1.05f;
                    attackRange = 3.4f;
                    break;
                case "Swordsman":
                    damage = 24f;
                    attackInterval = 0.82f;
                    break;
                case "Knight":
                    damage = 21f;
                    attackInterval = 0.95f;
                    break;
                case "Lancer":
                    damage = 32f;
                    attackInterval = 1f;
                    attackRange = 1.05f;
                    break;
                case "Armored Axeman":
                    damage = 40f;
                    attackInterval = 1.1f;
                    attackRange = 0.82f;
                    break;
                case "Priest":
                    damage = 12f;
                    attackInterval = 1.1f;
                    attackRange = 2.8f;
                    break;
                case "Wizard":
                    damage = 52f;
                    attackInterval = 1.2f;
                    attackRange = 3.1f;
                    break;
                case "Knight Templar":
                    damage = 48f;
                    attackInterval = 1f;
                    attackRange = 0.85f;
                    break;
            }
        }

        private static RectTransform CreateSummonUi(
            SideDefenseMapLayout mapLayout,
            Sprite coinSprite,
            IReadOnlyList<HumanUiEntry> entries)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject uiRoot = new GameObject(
                "Game UI (Human Summon Bar)",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(HumanSummonController));

            Canvas canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = uiRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image coinBadge = CreateImage(
                "Gold Coin Counter",
                uiRoot.transform,
                new Color(0.025f, 0.15f, 0.2f, 0.96f));
            SetRect(
                coinBadge.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -18f),
                new Vector2(194f, 56f),
                new Vector2(0f, 1f));
            AddOutline(coinBadge.gameObject, new Color(0.15f, 0.8f, 0.9f, 0.9f), 2f);

            Image largeCoin = CreateImage(
                "Coin Icon",
                coinBadge.transform,
                Color.white,
                coinSprite);
            largeCoin.preserveAspect = true;
            largeCoin.raycastTarget = false;
            SetRect(
                largeCoin.rectTransform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(8f, 0f),
                new Vector2(48f, 48f),
                new Vector2(0f, 0.5f));

            Text coinLabel = CreateText(
                "Coin Amount",
                coinBadge.transform,
                font,
                28,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.85f, 0.2f, 1f));
            SetStretch(
                coinLabel.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(62f, 4f),
                new Vector2(-8f, -4f));

            Image bottomPanel = CreateImage(
                "Bottom Human Selection Panel",
                uiRoot.transform,
                new Color(0.02f, 0.12f, 0.17f, 0.97f));
            SetRect(
                bottomPanel.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 178f),
                new Vector2(0.5f, 0f));

            Image topBorder = CreateImage(
                "Top Cyan Border",
                bottomPanel.transform,
                new Color(0.05f, 0.72f, 0.82f, 1f));
            topBorder.raycastTarget = false;
            SetStretch(
                topBorder.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                new Vector2(0f, 6f));

            Text selectedLabel = CreateText(
                "Selected Human Label",
                bottomPanel.transform,
                font,
                18,
                TextAnchor.MiddleLeft,
                new Color(0.82f, 0.97f, 1f, 1f));
            SetRect(
                selectedLabel.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(16f, -8f),
                new Vector2(-174f, 28f),
                new Vector2(0f, 1f));

            ScrollRect scrollRect = CreateCardScrollView(bottomPanel.transform);
            RectTransform content = scrollRect.content;

            List<HumanSummonCard> cards =
                new List<HumanSummonCard>(entries.Count);
            foreach (HumanUiEntry entry in entries)
            {
                cards.Add(
                    CreateCharacterCard(
                        content,
                        font,
                        coinSprite,
                        entry));
            }

            Button summonButton = CreateSummonButton(bottomPanel.transform, font);

            HumanSummonController controller =
                uiRoot.GetComponent<HumanSummonController>();
            controller.Configure(
                mapLayout,
                coinLabel,
                selectedLabel,
                summonButton,
                cards.ToArray());
            return bottomPanel.rectTransform;
        }

        private static ScrollRect CreateCardScrollView(Transform parent)
        {
            GameObject scrollObject = new GameObject(
                "Human Card Scroll View",
                typeof(RectTransform),
                typeof(ScrollRect));
            scrollObject.transform.SetParent(parent, false);
            RectTransform scrollTransform =
                scrollObject.GetComponent<RectTransform>();
            SetStretch(
                scrollTransform,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(12f, 10f),
                new Vector2(-158f, -38f));

            GameObject viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewport =
                viewportObject.GetComponent<RectTransform>();
            SetStretch(
                viewport,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            GameObject contentObject = new GameObject(
                "Character Cards",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content =
                contentObject.GetComponent<RectTransform>();
            SetRect(
                content,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                Vector2.zero,
                new Vector2(0f, 126f),
                new Vector2(0f, 0.5f));

            HorizontalLayoutGroup layout =
                contentObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(3, 3, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.08f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.12f;
            scrollRect.scrollSensitivity = 28f;

            return scrollRect;
        }

        private static HumanSummonCard CreateCharacterCard(
            Transform parent,
            Font font,
            Sprite coinSprite,
            HumanUiEntry entry)
        {
            GameObject cardObject = new GameObject(
                $"{entry.Spec.DisplayName} Card",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(Outline),
                typeof(LayoutElement),
                typeof(HumanSummonCard));
            cardObject.transform.SetParent(parent, false);

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(112f, 126f);

            LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 112f;
            layoutElement.preferredHeight = 126f;

            Image background = cardObject.GetComponent<Image>();
            background.color = new Color(0.035f, 0.25f, 0.32f, 1f);

            Button button = cardObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.78f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.55f, 0.9f, 0.95f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.4f, 0.42f, 0.75f);
            button.colors = colors;

            Outline selectionOutline = cardObject.GetComponent<Outline>();
            selectionOutline.enabled = false;

            Image icon = CreateImage(
                "Character Icon",
                cardObject.transform,
                Color.white,
                entry.Icon);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetStretch(
                icon.rectTransform,
                new Vector2(0f, 0.34f),
                new Vector2(1f, 1f),
                new Vector2(7f, 2f),
                new Vector2(-7f, -4f));

            Text nameText = CreateText(
                "Character Name",
                cardObject.transform,
                font,
                14,
                TextAnchor.MiddleCenter,
                Color.white);
            nameText.text = entry.Spec.DisplayName.ToUpperInvariant();
            SetStretch(
                nameText.rectTransform,
                new Vector2(0f, 0.16f),
                new Vector2(1f, 0.34f),
                new Vector2(3f, 0f),
                new Vector2(-3f, 0f));

            Image miniCoin = CreateImage(
                "Cost Coin",
                cardObject.transform,
                Color.white,
                coinSprite);
            miniCoin.preserveAspect = true;
            miniCoin.raycastTarget = false;
            SetRect(
                miniCoin.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(22f, 3f),
                new Vector2(20f, 20f),
                new Vector2(0f, 0f));

            Text costText = CreateText(
                "Coin Cost",
                cardObject.transform,
                font,
                16,
                TextAnchor.MiddleLeft,
                new Color(1f, 0.83f, 0.15f, 1f));
            costText.text = entry.Spec.CoinCost.ToString();
            SetStretch(
                costText.rectTransform,
                new Vector2(0.4f, 0f),
                new Vector2(1f, 0.16f),
                Vector2.zero,
                new Vector2(-4f, 0f));

            Image cooldown = CreateImage(
                "Cooldown Overlay",
                cardObject.transform,
                new Color(0f, 0f, 0f, 0.62f));
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Vertical;
            cooldown.fillOrigin = (int)Image.OriginVertical.Bottom;
            cooldown.raycastTarget = false;
            SetStretch(
                cooldown.rectTransform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            HumanSummonCard card = cardObject.GetComponent<HumanSummonCard>();
            card.Configure(
                entry.Spec.DisplayName,
                entry.Spec.CoinCost,
                entry.Prefab,
                button,
                selectionOutline,
                cooldown);
            return card;
        }

        private static Button CreateSummonButton(Transform parent, Font font)
        {
            Image image = CreateImage(
                "SUMMON Button",
                parent,
                new Color(0.96f, 0.62f, 0.08f, 1f));
            SetRect(
                image.rectTransform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-12f, 54f),
                new Vector2(132f, 84f),
                new Vector2(1f, 0f));
            AddOutline(
                image.gameObject,
                new Color(1f, 0.9f, 0.35f, 1f),
                3f);

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.72f, 1f);
            colors.pressedColor = new Color(0.85f, 0.72f, 0.45f, 1f);
            colors.disabledColor = new Color(0.28f, 0.32f, 0.34f, 0.8f);
            button.colors = colors;

            Text label = CreateText(
                "SUMMON Label",
                image.transform,
                font,
                24,
                TextAnchor.MiddleCenter,
                new Color(0.12f, 0.1f, 0.02f, 1f));
            label.text = "SUMMON\n▶";
            SetStretch(
                label.rectTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(5f, 5f),
                new Vector2(-5f, -5f));
            return button;
        }

        private static Sprite ConfigureSingleSprite(string assetPath)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    $"Missing generated coin texture: {assetPath}",
                    assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Coin texture did not import as a Sprite: {assetPath}");
            }

            return sprite;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color,
            Sprite sprite = null)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
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
            text.resizeTextMinSize = Mathf.Max(9, fontSize - 5);
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

        private readonly struct CharacterSpec
        {
            public CharacterSpec(
                string displayName,
                string idleAssetPath,
                string walkAssetPath,
                int coinCost,
                float maxHealth,
                float movementSpeed)
            {
                DisplayName = displayName;
                IdleAssetPath = idleAssetPath;
                WalkAssetPath = walkAssetPath;
                CoinCost = coinCost;
                MaxHealth = maxHealth;
                MovementSpeed = movementSpeed;
            }

            public string DisplayName { get; }
            public string IdleAssetPath { get; }
            public string WalkAssetPath { get; }
            public int CoinCost { get; }
            public float MaxHealth { get; }
            public float MovementSpeed { get; }
        }

        private readonly struct HumanUiEntry
        {
            public HumanUiEntry(
                CharacterSpec spec,
                GameObject prefab,
                Sprite icon)
            {
                Spec = spec;
                Prefab = prefab;
                Icon = icon;
            }

            public CharacterSpec Spec { get; }
            public GameObject Prefab { get; }
            public Sprite Icon { get; }
        }
    }
}
