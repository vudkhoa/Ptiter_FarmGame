using UnityEditor;
using UnityEngine;

namespace TechArt.AssetsAutoImport.Editor
{
    /// <summary>
    /// Tự động cấu hình <see cref="TextureImporter"/> theo đường dẫn asset (UI, Environment, Normal map).
    /// Unity gọi khi import/reimport texture — không cần đăng ký thêm.
    /// </summary>
    public sealed class TextureAutoImporter : AssetPostprocessor
    {
        private const string LogPrefix = "[AssetsAutoImport][Texture]";

        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            string path = assetPath.ToLowerInvariant();

            if (path.Contains("/ui/"))
            {
                ApplyUiRules(importer);
                Debug.Log($"{LogPrefix} UI: {assetPath}");
            }
            else if (path.Contains("/environment/") || path.Contains("/env/"))
            {
                ApplyEnvironmentRules(importer);
                Debug.Log($"{LogPrefix} Environment: {assetPath}");
            }
            else if (path.Contains("_n.") || path.Contains("_normal."))
            {
                ApplyNormalMapRules(importer);
                Debug.Log($"{LogPrefix} NormalMap: {assetPath}");
            }
        }

        private static void ApplyUiRules(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;

            SetPlatformSettings(importer, "Android", 2048, TextureImporterFormat.ASTC_4x4);
            SetPlatformSettings(importer, "iPhone", 2048, TextureImporterFormat.ASTC_4x4);
        }

        private static void ApplyEnvironmentRules(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;

            SetPlatformSettings(importer, "Android", 1024, TextureImporterFormat.ASTC_6x6);
            SetPlatformSettings(importer, "iPhone", 1024, TextureImporterFormat.ASTC_6x6);
        }

        private static void ApplyNormalMapRules(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.mipmapEnabled = true;

            SetPlatformSettings(importer, "Android", 1024, TextureImporterFormat.ASTC_5x5);
            SetPlatformSettings(importer, "iPhone", 1024, TextureImporterFormat.ASTC_5x5);
        }

        private static void SetPlatformSettings(
            TextureImporter importer,
            string platform,
            int maxSize,
            TextureImporterFormat format)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = format;
            settings.compressionQuality = 50;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
