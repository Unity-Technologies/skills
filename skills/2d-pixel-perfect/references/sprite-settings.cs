// PixelPerfect Skill — Sprite Import Settings (Editor-only)
// ----------------------------------------------------------
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PixelPerfect.Editor
{
    public static class SpriteImportUtility
    {
        public static TextureImporter GetImporter(Sprite sprite)
        {
            if (sprite == null) return null;
            var path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetImporter.GetAtPath(path) as TextureImporter;
        }

        public static bool FixSpriteImportSettings(Sprite sprite, int targetPPU)
        {
            var importer = GetImporter(sprite);
            if (importer == null) { Debug.LogWarning($"No importer for: {sprite?.name}"); return false; }

            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = targetPPU;
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(sprite), ImportAssetOptions.ForceUpdate);
            return true;
        }

        // After batch-fixing multiple sprites, always flush with:
        //   AssetDatabase.SaveAssets();
        //   AssetDatabase.Refresh();
    }
}
#endif
