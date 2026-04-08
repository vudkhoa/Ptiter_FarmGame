using UnityEditor;
using UnityEngine;

namespace Importer
{
    public class TextureAutoImporter : AssetPostprocessor
    {
        // Before Import
        private void OnPreprocessTexture()
        {
            TextureImporter importer = (TextureImporter)assetImporter;
            string path = assetPath.ToLower();

            if (path.Contains("/ui/"))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;

                // Android
                SetPlatformSettings(importer, "Android", 2048, TextureImporterFormat.ASTC_4x4);

                // ios
                SetPlatformSettings(importer, "iPhone", 2048, TextureImporterFormat.ASTC_4x4);
                
                // Complete Debug
                Debug.Log($"[AutoImport] UI Texture: {assetPath}");
            }
        }

        void SetPlatformSettings(TextureImporter importer, string platform, int maxSize, TextureImporterFormat format)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);

            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = maxSize;
            settings.format = format;
            settings.compressionQuality = 50;
            settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

            importer.SetPlatformTextureSettings(settings);
        }
    }
}