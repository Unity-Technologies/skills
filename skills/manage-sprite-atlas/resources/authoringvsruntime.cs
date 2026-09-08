// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// Explains the critical distinction between editor authoring and runtime access.
/// SpriteAtlasAsset (editor) vs SpriteAtlas (runtime) - never mix them!
/// </summary>
public static class AuthoringVsRuntimeDocumentation
{
    /// <summary>
    /// SPRITEATLASASSET - Editor-Only Type
    /// Used for creating, modifying, and configuring atlases in the editor.
    /// Cannot be used at runtime.
    /// </summary>
    public static void Use_SpriteAtlasAsset_Editor()
    {
        // ✅ CORRECT: Using SpriteAtlasAsset in editor code

        // Create new atlas asset
        SpriteAtlasAsset asset = new SpriteAtlasAsset();

        // Add sprites for authoring
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/mySprite.png");
        asset.Add(sprites);

        // Save to disk
        SpriteAtlasAsset.Save(asset, "Assets/MyAtlas.spriteatlasv2");

        // Configure via importer (editor-only)
        AssetDatabase.ImportAsset("Assets/MyAtlas.spriteatlasv2");
        SpriteAtlasImporter importer = AssetImporter.GetAtPath("Assets/MyAtlas.spriteatlasv2") as SpriteAtlasImporter;
        importer.includeInBuild = true;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// SPRITEATLAS - Runtime-Only Type
    /// Used for querying packed sprites at runtime.
    /// Cannot be created or modified in editor code.
    /// </summary>
    public static void Use_SpriteAtlas_Runtime()
    {
        // ✅ CORRECT: Using SpriteAtlas at runtime

        // Register for late-binding to load atlases on-demand
        SpriteAtlasManager.atlasRegistered += OnAtlasLoaded;
    }

    private static void OnAtlasLoaded(SpriteAtlas atlas)
    {
        // Query sprites from loaded atlas (runtime only)
        Object[] packables = atlas.GetPackables();
        // ... use sprites ...
    }

    /// <summary>
    /// ❌ WRONG: Using SpriteAtlas in editor code.
    /// SpriteAtlas is a runtime type and cannot be constructed in editor.
    /// </summary>
    public static void Wrong_UseSpriteAtlasInEditor()
    {
        // ❌ WRONG: Cannot construct SpriteAtlas in editor
        // SpriteAtlas atlas = new SpriteAtlas(); // Compile error!

        // ❌ WRONG: Cannot load SpriteAtlas in editor
        // SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("path"); // Runtime error!
    }

    /// <summary>
    /// ❌ WRONG: Using SpriteAtlasAsset at runtime.
    /// SpriteAtlasAsset is an asset file, not a runtime object.
    /// </summary>
    public static void Wrong_UseSpriteAtlasAssetRuntime()
    {
        // ❌ WRONG: Cannot use SpriteAtlasAsset in runtime code
        // SpriteAtlasAsset asset = someReference; // This is an editor type!
    }

    /// <summary>
    /// ✅ CORRECT: Two-step V2 pattern for atlas creation.
    /// Step 1: Create with SpriteAtlasAsset (editor)
    /// Step 2: Use at runtime via SpriteAtlas
    /// </summary>
    public static void Correct_TwoStepPattern()
    {
        // ===== STEP 1: EDITOR AUTHORING (SpriteAtlasAsset) =====

        // In editor script or prebuild processor:
        SpriteAtlasAsset asset = new SpriteAtlasAsset();
        asset.Add(AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/mySprite.png"));
        SpriteAtlasAsset.Save(asset, "Assets/MyAtlas.spriteatlasv2");
        AssetDatabase.ImportAsset("Assets/MyAtlas.spriteatlasv2");

        // Configure via importer
        SpriteAtlasImporter importer = AssetImporter.GetAtPath("Assets/MyAtlas.spriteatlasv2") as SpriteAtlasImporter;
        importer.includeInBuild = true; // Include in build for immediate loading
        importer.SaveAndReimport();

        // ===== STEP 2: RUNTIME ACCESS (SpriteAtlas) =====

        // In runtime script:
        /*
        public class MyRuntimeScript : MonoBehaviour
        {
            private void Awake()
            {
                // Register for late-binding if using Addressables
                SpriteAtlasManager.atlasRegistrationNeeded += OnAtlasRegistrationNeeded;
            }

            private void OnAtlasRegistrationNeeded(Sprite sprite)
            {
                // Load atlas via Addressables/AssetBundles
                // The loaded SpriteAtlas is what you query at runtime
            }
        }
        */
    }

    /// <summary>
    /// Summary table of when to use each type:
    ///
    /// | Context      | Type              | Purpose                              |
    /// |--------------|-------------------|--------------------------------------|
    /// | Editor       | SpriteAtlasAsset  | Create, modify, configure atlases    |
    /// | Editor       | SpriteAtlasImporter | Set texture, packing, platform settings |
    /// | Runtime      | SpriteAtlas       | Query packed sprites (read-only)     |
    /// | Runtime      | SpriteAtlasManager| Dynamic loading callbacks            |
    /// </summary>
    public static void Summary_Table()
    {
        /*
        +------------------+-------------------+--------------------------------------+
        | Context          | Type              | Purpose                              |
        +------------------+-------------------+--------------------------------------+
        | Editor           | SpriteAtlasAsset  | Create, modify, configure atlases    |
        | Editor           | SpriteAtlasImporter | Set texture, packing, platform settings |
        | Runtime          | SpriteAtlas       | Query packed sprites (read-only)     |
        | Runtime          | SpriteAtlasManager| Dynamic loading callbacks            |
        +------------------+-------------------+--------------------------------------+
        */
    }
}

/// <summary>
/// Example: Editor script that creates an atlas using the correct types.
/// </summary>
public static class CorrectEditorScript
{
    [MenuItem("Tools/Create Atlas")]
    public static void CreateAtlas()
    {
        // Use SpriteAtlasAsset for editor authoring
        SpriteAtlasAsset asset = new SpriteAtlasAsset();

        // Add sprites from a folder
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites" });
        Object[] sprites = new Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        asset.Add(sprites);

        // Save and import
        string path = "Assets/Atlases/MyAtlas.spriteatlasv2";
        SpriteAtlasAsset.Save(asset, path);
        AssetDatabase.ImportAsset(path);

        Debug.Log($"Created atlas at {path}");
    }
}
