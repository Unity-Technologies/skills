// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;

/// <summary>
/// Demonstrates why you must call SaveAndReimport() after modifying SpriteAtlasImporter.
/// Changes to importer settings are not persisted without explicitly saving.
/// </summary>
public static class DontModifyWithoutSavingDocumentation
{
    /// <summary>
    /// ❌ WRONG: Modifying importer settings and expecting them to persist.
    /// Unity's asset database requires explicit SaveAndReimport() calls.
    /// </summary>
    public static void Wrong_ModifyAndForget()
    {
        string path = "Assets/MyAtlas.spriteatlasv2";

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            // Make changes
            importer.includeInBuild = true;
            var packingSettings = importer.packingSettings;
            packingSettings.enableTightPacking = true;
            importer.packingSettings = packingSettings;

            // ❌ NOT CALLING SaveAndReimport()!
            // Changes are lost when Unity reimports or restarts
        }
    }

    /// <summary>
    /// ❌ WRONG: Modifying multiple settings without saving.
    /// Each setting change needs to be followed by SaveAndReimport().
    /// </summary>
    public static void Wrong_MultipleChangesNoSave()
    {
        string path = "Assets/MyAtlas.spriteatlasv2";

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            var packingSettings = importer.packingSettings;
            var textureSettings = importer.GetPlatformSettings("DefaultTexturePlatform");

            // Change 1: Include in build
            importer.includeInBuild = true;

            // Change 2: Tight packing
            packingSettings.enableTightPacking = true;

            // Change 3: Max texture size
            textureSettings.maxTextureSize = 2048;

            // Change 4: Format
            textureSettings.format = TextureImporterFormat.ASTC_6x6;

            importer.SetPlatformSettings(textureSettings);
            importer.packingSettings = packingSettings;

            // ❌ NOT CALLING SaveAndReimport()!
            // All changes are lost
        }
    }

    /// <summary>
    /// ✅ CORRECT: Always call SaveAndReimport() after modifications.
    /// This ensures changes are saved and the asset is reimported.
    /// </summary>
    public static void Correct_SaveAfterModify()
    {
        string path = "Assets/MyAtlas.spriteatlasv2";

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            var packingSettings = importer.packingSettings;
            var textureSettings = importer.GetPlatformSettings("DefaultTexturePlatform");

            // Change 1: Include in build
            importer.includeInBuild = true;

            // Change 2: Tight packing
            packingSettings.enableTightPacking = true;

            // Change 3: Max texture size
            textureSettings.maxTextureSize = 2048;

            // Change 4: Format
            textureSettings.format = TextureImporterFormat.ASTC_6x6;

            importer.SetPlatformSettings(textureSettings);
            importer.packingSettings = packingSettings;

            // ✅ CALL SaveAndReimport()!
            importer.SaveAndReimport();
        }
    }
}

/// <summary>
/// Editor window that demonstrates the difference between saving and not saving.
/// </summary>
public class ModifyWithoutSavingDemo : EditorWindow
{
    private static void CreateDemoAtlas(string path)
    {
        // Clean up if exists
        if (System.IO.File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }

        // Create new atlas
        SpriteAtlasAsset asset = new SpriteAtlasAsset();
        SpriteAtlasAsset.Save(asset, path);
        AssetDatabase.ImportAsset(path);
    }
}
