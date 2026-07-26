using System;
using System.IO;
using System.Linq;
using Game3.SideDefense;
using UnityEditor;
using UnityEngine;

namespace Game3.SideDefense.Editor
{
    internal static class SideDefenseHealthBarSceneFactory
    {
        private const string AssetFolder =
            "Assets/Project_Asset/Generated/UI/HealthBars";
        private const float PixelsPerUnit = 100f;

        public static SideDefenseHealthBarSpriteSet LoadHumanSprites()
        {
            return LoadSpriteSet("HumanHp");
        }

        public static SideDefenseHealthBarSpriteSet LoadTowerSprites()
        {
            return LoadSpriteSet("TowerHp");
        }

        public static SideDefenseHealthBarSpriteSet LoadMonsterSprites()
        {
            return LoadSpriteSet("MonsterHp");
        }

        public static SideDefenseHealthBar Create(
            Transform parent,
            string objectName,
            SideDefenseHealthBarSpriteSet sprites,
            float targetWorldWidth,
            int sortingOrder)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            GameObject barObject = new GameObject(objectName);
            barObject.transform.SetParent(parent, false);

            float inheritedScale = Mathf.Max(
                0.0001f,
                Mathf.Abs(parent.lossyScale.x));
            float localScale =
                targetWorldWidth /
                (sprites.Frame.bounds.size.x * inheritedScale);
            barObject.transform.localScale = Vector3.one * localScale;

            CreateRenderer(
                "Background",
                barObject.transform,
                sprites.Background,
                sortingOrder);

            GameObject fillPivotObject = new GameObject("Fill (Left Anchored)");
            fillPivotObject.transform.SetParent(barObject.transform, false);
            fillPivotObject.transform.localPosition = new Vector3(
                -sprites.Fill.bounds.extents.x,
                0f,
                0f);

            SpriteRenderer fillRenderer = CreateRenderer(
                "Current Health",
                fillPivotObject.transform,
                sprites.Fill,
                sortingOrder + 1);
            fillRenderer.transform.localPosition = new Vector3(
                sprites.Fill.bounds.extents.x,
                0f,
                0f);

            CreateRenderer(
                "Frame",
                barObject.transform,
                sprites.Frame,
                sortingOrder + 2);

            SideDefenseHealthBar healthBar =
                barObject.AddComponent<SideDefenseHealthBar>();
            healthBar.Configure(fillPivotObject.transform);
            return healthBar;
        }

        private static SideDefenseHealthBarSpriteSet LoadSpriteSet(
            string assetPrefix)
        {
            return new SideDefenseHealthBarSpriteSet(
                ConfigureSingleSprite(
                    $"{AssetFolder}/{assetPrefix}Background.png"),
                ConfigureSingleSprite(
                    $"{AssetFolder}/{assetPrefix}Fill.png"),
                ConfigureSingleSprite(
                    $"{AssetFolder}/{assetPrefix}Frame.png"));
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
                    $"Missing health-bar texture: {assetPath}",
                    assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Health-bar texture did not import as a Sprite: {assetPath}");
            }

            return sprite;
        }

        private static SpriteRenderer CreateRenderer(
            string objectName,
            Transform parent,
            Sprite sprite,
            int sortingOrder)
        {
            GameObject rendererObject = new GameObject(objectName);
            rendererObject.transform.SetParent(parent, false);

            SpriteRenderer renderer =
                rendererObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }

    internal readonly struct SideDefenseHealthBarSpriteSet
    {
        public SideDefenseHealthBarSpriteSet(
            Sprite background,
            Sprite fill,
            Sprite frame)
        {
            Background = background;
            Fill = fill;
            Frame = frame;
        }

        public Sprite Background { get; }
        public Sprite Fill { get; }
        public Sprite Frame { get; }
    }
}
