using UnityEditor;
using UnityEngine;

namespace TechArt.AssetsAutoImport.Editor
{
    /// <summary>
    /// Áp rule nhẹ lên <see cref="Material"/> sau khi import (GPU Instancing theo folder).
    /// Dùng <see cref="AssetPostprocessor.OnPostprocessAllAssets"/> vì Unity không có OnPreprocessMaterial.
    /// </summary>
    public sealed class MaterialAutoImporter : AssetPostprocessor
    {
        private const string LogPrefix = "[AssetsAutoImport][Material]";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".mat", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    continue;
                }

                string p = path.Replace('\\', '/').ToLowerInvariant();
                bool changed = false;

                // GPU Instancing: cùng một mesh + cùng material (và keyword/shader hợp lệ) được gộp batch,
                // vẽ nhiều bản copy mà không tăng số draw call như mỗi object một material riêng.
                // Môi trường: thường nhiều instance cùng material (đá, cây, prop lặp) → bật instancing có lợi.
                if (p.Contains("/environment/") || p.Contains("/env/"))
                {
                    if (!mat.enableInstancing)
                    {
                        mat.enableInstancing = true;
                        changed = true;
                    }
                }
                // UI: thường ít cùng một material cho hàng loạt mesh giống hệt; instancing ít hữu ích hơn
                // và một số shader/UI workflow dễ gây nhầm batch — tắt để tránh bật nhầm khi không cần.
                else if (p.Contains("/ui/"))
                {
                    if (mat.enableInstancing)
                    {
                        mat.enableInstancing = false;
                        changed = true;
                    }
                }

                if (changed)
                {
                    // Đánh dấu asset đã bị sửa trong bộ nhớ so với file .mat trên đĩa — Unity mới biết cần ghi lại
                    // (serializing) khi Save/AssetDatabase lưu; không SetDirty thì đổi instancing có thể mất khi reload domain/mở lại project.
                    EditorUtility.SetDirty(mat);
                    Debug.Log($"{LogPrefix} Updated: {path}");
                }
            }
        }
    }
}
