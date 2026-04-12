using UnityEditor;
using UnityEngine;

namespace TechArt.AssetsAutoImport.Editor
{
    /// <summary>
    /// Tự động cấu hình <see cref="AudioImporter"/> theo đường dẫn (music vs SFX/UI).
    /// </summary>
    public sealed class AudioAutoImporter : AssetPostprocessor
    {
        private const string LogPrefix = "[AssetsAutoImport][Audio]";

        private void OnPreprocessAudio()
        {
            if (!assetPath.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError(
                    $"{LogPrefix} Audio không phải định dạng .ogg — pipeline auto-import đang dùng Vorbis; nên xuất/nén về .ogg cho đồng nhất: {assetPath}");
            }

            var importer = (AudioImporter)assetImporter;
            string path = assetPath.ToLowerInvariant();

            if (path.Contains("/music/") || path.Contains("/bgm/"))
            {
                ApplyMusicRules(importer);
                Debug.Log($"{LogPrefix} Music: {assetPath}");
            }
            else if (path.Contains("/sfx/") || path.Contains("/ui/"))
            {
                ApplySfxOrUiRules(importer);
                Debug.Log($"{LogPrefix} SFX/UI: {assetPath}");
            }
        }

        private static void ApplyMusicRules(AudioImporter importer)
        {
            importer.forceToMono = false;
            importer.loadInBackground = true;

            var def = importer.defaultSampleSettings;
            def.loadType = AudioClipLoadType.Streaming;
            def.compressionFormat = AudioCompressionFormat.Vorbis;
            def.quality = 0.7f;
            def.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            def.preloadAudioData = false;
            importer.defaultSampleSettings = def;

            ApplyMobileOverrides(importer, streaming: true, quality: 0.65f, preloadAudioData: false);
        }

        private static void ApplySfxOrUiRules(AudioImporter importer)
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;

            var def = importer.defaultSampleSettings;
            def.loadType = AudioClipLoadType.CompressedInMemory;
            def.compressionFormat = AudioCompressionFormat.Vorbis;
            def.quality = 0.85f;
            def.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            def.preloadAudioData = true;
            importer.defaultSampleSettings = def;

            ApplyMobileOverrides(importer, streaming: false, quality: 0.8f, preloadAudioData: true);
        }

        private static void ApplyMobileOverrides(AudioImporter importer, bool streaming, float quality, bool preloadAudioData)
        {
            void Apply(string platform)
            {
                var s = importer.GetOverrideSampleSettings(platform);
                s.loadType = streaming ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = quality;
                s.preloadAudioData = preloadAudioData;
                importer.SetOverrideSampleSettings(platform, s);
            }

            Apply("Android");
            Apply("iPhone");
        }
    }
}
