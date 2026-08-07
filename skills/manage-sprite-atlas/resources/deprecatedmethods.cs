// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;

/// <summary>
/// Documents deprecated SpriteAtlas methods and their replacements.
/// Always use the latest V2 API patterns.
/// </summary>
public static class DeprecatedMethodsDocumentation
{
    /// <summary>
    /// ⚠️ DEPRECATED: SetBuildSettings method.
    /// This method was used in early V1/V2 preview versions but is no longer available.
    ///
    /// REPLACEMENT: Use SpriteAtlasImporter for all configuration settings.
    /// </summary>
    [System.Obsolete("SetBuildSettings is deprecated. Use SpriteAtlasImporter instead.")]
    public static void Old_SetBuildSettings()
    {
        // OLD (deprecated):
        // SpriteAtlasAsset atlas = new SpriteAtlasAsset();
        // atlas.SetBuildSettings(settings);

        // NEW:
        // Create asset, save it, then configure via SpriteAtlasImporter
        // SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        // importer.packingSettings = settings;
        // importer.SaveAndReimport();
    }

    /// <summary>
    /// ⚠️ DEPRECATED: GetBuildSettings method.
    /// Use SpriteAtlasImporter to read configuration instead.
    /// </summary>
    [System.Obsolete("GetBuildSettings is deprecated. Read from SpriteAtlasImporter instead.")]
    public static void Old_GetBuildSettings()
    {
        // OLD (deprecated):
        // var settings = atlas.GetBuildSettings();

        // NEW:
        // SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        // var settings = importer.packingSettings;
    }

    /// <summary>
    /// ⚠️ DEPRECATED: Direct modification of SpriteAtlas after creation.
    /// In V2, SpriteAtlas is read-only at runtime and cannot be modified in editor.
    ///
    /// REPLACEMENT: Modify SpriteAtlasAsset before saving, then reimport.
    /// </summary>
    [System.Obsolete("Direct SpriteAtlas modification is deprecated. Use SpriteAtlasAsset for editing.")]
    public static void Old_DirectModification()
    {
        // OLD (deprecated):
        // atlas.Add(sprites); // Not allowed in editor!

        // NEW:
        // SpriteAtlasAsset asset = new SpriteAtlasAsset();
        // asset.Add(sprites);
        // SpriteAtlasAsset.Save(asset, path);
    }

    /// <summary>
    /// ⚠️ DEPRECATED: Using AssetDatabase.LoadAssetAtPath<SpriteAtlas>() in editor.
    /// SpriteAtlas is runtime-only and cannot be loaded in editor context.
    ///
    /// REPLACEMENT: Use SpriteAtlasAsset for editor operations.
    /// </summary>
    [System.Obsolete("Loading SpriteAtlas in editor is deprecated. Use SpriteAtlasAsset instead.")]
    public static void Old_LoadSpriteAtlasInEditor()
    {
        // OLD (deprecated):
        // SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

        // NEW:
        // SpriteAtlasAsset asset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(path);
    }

    /// <summary>
    /// ⚠️ DEPRECATED: Manual packing in editor using SpriteAtlasUtility.
    /// SpriteAtlasUtility.PackAtlases() is for preview only, not build-time packing.
    ///
    /// REPLACEMENT: Use the prebuild pipeline with IPreprocessBuildWithReport.
    /// </summary>
    [System.Obsolete("Manual packing is deprecated. Use IPreprocessBuildWithReport instead.")]
    public static void Old_ManualPacking()
    {
        // OLD (deprecated):
        // SpriteAtlasUtility.PackAtlases(atlases, BuildTarget.NoAPI);

        // NEW:
        // Implement IPreprocessBuildWithReport.OnPreprocessBuild() for automatic packing
    }
}

/// <summary>
/// Migration guide: Old code patterns vs new V2 patterns.
/// </summary>
public static class MigrationGuide
{
    /// <summary>
    /// Pattern 1: Creating an atlas
    /// </summary>
    public static void CreateAtlas_Old()
    {
        // ❌ OLD (deprecated):
        // var atlas = ScriptableObject.CreateInstance<SpriteAtlas>();
        // AssetDatabase.AddObjectToAsset(atlas, path);
        // EditorUtility.SetDirty(atlas);

        // ✅ NEW (V2):
        SpriteAtlasAsset asset = new SpriteAtlasAsset();
        // Add sprites...
        SpriteAtlasAsset.Save(asset, "Assets/MyAtlas.spriteatlasv2");
    }

    /// <summary>
    /// Pattern 2: Configuring an atlas
    /// </summary>
    public static void ConfigureAtlas_Old()
    {
        // ❌ OLD (deprecated):
        // var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
        // importer.SetBuildSettings(settings);

        // ✅ NEW (V2):
        SpriteAtlasImporter importer = null; // Get from AssetImporter
        // importer.packingSettings = settings;
        // importer.SaveAndReimport();
    }

    /// <summary>
    /// Pattern 3: Loading an atlas at runtime
    /// </summary>
    public static void LoadAtlas_Runtime()
    {
        // ✅ CORRECT (runtime only):
        // SpriteAtlasManager.atlasLoaded += OnAtlasLoaded;
        // Or use Addressables/AssetBundles for dynamic loading
    }
}
