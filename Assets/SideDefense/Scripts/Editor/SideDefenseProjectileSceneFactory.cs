using System;
using System.IO;
using System.Linq;
using Game3.SideDefense;
using UnityEditor;
using UnityEngine;

namespace Game3.SideDefense.Editor
{
    internal static class SideDefenseProjectileSceneFactory
    {
        private const string PrefabFolder =
            "Assets/SideDefense/Prefabs/Projectiles";

        public static GameObject CreateOrReplace(
            string projectileName,
            string textureAssetPath,
            bool isAnimationSheet,
            float worldScale = 2.2f)
        {
            EnsureFolder("Assets/SideDefense");
            EnsureFolder("Assets/SideDefense/Prefabs");
            EnsureFolder(PrefabFolder);

            Sprite[] frames = LoadFrames(
                textureAssetPath,
                isAnimationSheet);
            string prefabPath =
                $"{PrefabFolder}/{SanitizeFileName(projectileName)}.prefab";

            GameObject root = new GameObject(projectileName);
            try
            {
                root.transform.localScale =
                    Vector3.one * Mathf.Max(0.1f, worldScale);

                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = frames[0];
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 35;

                SideDefenseSpriteAnimator animator =
                    root.AddComponent<SideDefenseSpriteAnimator>();
                animator.Configure(frames, 12f);

                root.AddComponent<SideDefenseProjectile>();

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to create projectile prefab: {prefabPath}");
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

        private static Sprite[] LoadFrames(
            string assetPath,
            bool isAnimationSheet)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    $"Missing projectile texture: {assetPath}",
                    assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = isAnimationSheet
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;
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
                    $"Projectile texture did not contain sprites: {assetPath}");
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
    }
}
