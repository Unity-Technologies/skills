// [UNITY-SKILL:SPRITEATLAS]
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

/// <summary>
/// Demonstrates what NOT to do when scripting SpriteAtlases in editor code.
/// This file shows common mistakes and their consequences.
/// </summary>
public static class DontScriptSpriteAtlasInEditor
{
    /// <summary>
    /// ❌ WRONG: Creating SpriteAtlas directly in editor code.
    /// SpriteAtlas is a runtime-only type. This will fail at compile time or runtime.
    /// </summary>
    [System.Obsolete("This is incorrect - do not use")]
    public static void Wrong_CreateSpriteAtlasDirectly()
    {
        // This is WRONG - SpriteAtlas cannot be constructed in editor code
        // SpriteAtlas atlas = new SpriteAtlas(); // Compile error!
    }

    /// <summary>
    /// ❌ WRONG: Using AssetDatabase.LoadAssetAtPath<SpriteAtlas>() in editor.
    /// Use SpriteAtlasAsset for editor authoring, not SpriteAtlas.
    /// </summary>
    [System.Obsolete("This is incorrect - do not use")]
    public static void Wrong_LoadSpriteAtlasInEditor()
    {
        // This is WRONG - SpriteAtlas is runtime-only
        // SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("path/to/atlas.spriteatlas"); // Runtime error!
    }

    /// <summary>
    /// ❌ WRONG: Modifying SpriteAtlas in editor scripts.
    /// SpriteAtlas is read-only at runtime and cannot be created in editor.
    /// </summary>
    [System.Obsolete("This is incorrect - do not use")]
    public static void Wrong_ModifySpriteAtlasInEditor()
    {
        // This is WRONG - cannot modify SpriteAtlas in editor
        // atlas.Add(sprites); // Compile error!
    }

    /// <summary>
    /// ❌ WRONG: Using deprecated SpriteAtlasAsset methods.
    /// Use the two-step pattern with SpriteAtlasImporter instead.
    /// </summary>
    [System.Obsolete("This is incorrect - do not use")]
    public static void Wrong_UseDeprecatedMethods()
    {
        // This is WRONG - deprecated methods should not be used
        // SpriteAtlasAsset atlas = new SpriteAtlasAsset();
        // atlas.SetBuildSettings(...); // Deprecated!
    }

    /// <summary>
    /// ❌ WRONG: Modifying importer without calling SaveAndReimport().
    /// Changes to SpriteAtlasImporter are not applied until you save and reimport.
    /// </summary>
    [System.Obsolete("This is incorrect - do not use")]
    public static void Wrong_ModifyWithoutSaving()
    {
        // This is WRONG - changes won't be saved
        // SpriteAtlasImporter importer = ...;
        // importer.includeInBuild = true; // Not applied!
        // No SaveAndReimport() called!
    }

    /// <summary>
    /// ❌ WRONG: Confusing authoring vs runtime contexts.
    /// SpriteAtlasAsset (editor) and SpriteAtlas (runtime) are different types.
    /// </summary>
    [System.Obsolete("This is incorrect - do not use")]
    public static void Wrong_ConfuseAuthoringVsRuntime()
    {
        // This is WRONG - mixing contexts
        // SpriteAtlasAsset asset = ...;
        // SpriteAtlas runtime = asset; // Type mismatch!
    }
}

/// <summary>
/// CORRECT: The proper way to work with SpriteAtlases in editor code.
/// </summary>
public static class CorrectEditorPattern
{
    /// <summary>
    /// Create and configure an atlas using the correct two-step V2 pattern.
    /// </summary>
    public static void CreateAndConfigureAtlas(string path, Object[] sprites)
    {
        // Step 1: Create with SpriteAtlasAsset (editor-only)
        SpriteAtlasAsset asset = new SpriteAtlasAsset();
        asset.Add(sprites);
        SpriteAtlasAsset.Save(asset, path);

        // Step 2: Import and configure via SpriteAtlasImporter
        AssetDatabase.ImportAsset(path);

        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            importer.includeInBuild = true;
            var packingSettings = importer.packingSettings;
            packingSettings.enableTightPacking = true;
            importer.packingSettings = packingSettings;

            // CRITICAL: Save and reimport to apply changes
            importer.SaveAndReimport();
        }
    }
}
