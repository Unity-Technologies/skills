// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Migration guide from SpriteAtlas V1 to V2.
/// This file shows the key differences and how to update existing code.
/// </summary>
public static class MigrateV1ToV2Documentation
{
    /// <summary>
    /// V1 Pattern: Direct SpriteAtlas scripting (NO LONGER WORKS)
    ///
    /// ❌ OLD V1 CODE:
    /// var atlas = ScriptableObject.CreateInstance<SpriteAtlas>();
    /// atlas.Add(sprites);
    /// AssetDatabase.AddObjectToAsset(atlas, path);
    /// EditorUtility.SetDirty(atlas);
    ///
    /// ✅ NEW V2 CODE:
    /// SpriteAtlasAsset asset = new SpriteAtlasAsset();
    /// asset.Add(sprites);
    /// SpriteAtlasAsset.Save(asset, path);
    /// AssetDatabase.ImportAsset(path);
    /// </summary>
    public static void Migration_1_CreateAtlas()
    {
        // ===== MIGRATION 1: Creating an Atlas =====

        // ❌ OLD (V1 - Broken):
        /*
        var atlas = ScriptableObject.CreateInstance<SpriteAtlas>();
        atlas.Add(sprites);
        AssetDatabase.AddObjectToAsset(atlas, path);
        EditorUtility.SetDirty(atlas);
        */

        // ✅ NEW (V2):
        SpriteAtlasAsset asset = new SpriteAtlasAsset();
        asset.Add(new Object[0]); // Add sprites
        SpriteAtlasAsset.Save(asset, "Assets/MyAtlas.spriteatlasv2");
        AssetDatabase.ImportAsset("Assets/MyAtlas.spriteatlasv2");
    }

    /// <summary>
    /// V1 Pattern: Direct configuration (NO LONGER WORKS)
    ///
    /// ❌ OLD V1 CODE:
    /// var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
    /// importer.SetBuildSettings(settings);
    ///
    /// ✅ NEW V2 CODE:
    /// var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
    /// importer.packingSettings = settings;
    /// importer.SaveAndReimport();
    /// </summary>
    public static void Migration_2_ConfigureAtlas()
    {
        // ===== MIGRATION 2: Configuring an Atlas =====

        // ❌ OLD (V1 - Deprecated):
        /*
        var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
        importer.SetBuildSettings(settings); // Method removed!
        */

        // ✅ NEW (V2):
        SpriteAtlasImporter importer = null; // Get from AssetImporter
        // importer.packingSettings = settings;
        // importer.SaveAndReimport();
    }

    /// <summary>
    /// V1 Pattern: Loading in editor (NO LONGER WORKS)
    ///
    /// ❌ OLD V1 CODE:
    /// var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
    ///
    /// ✅ NEW V2 CODE:
    /// var asset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(path);
    /// </summary>
    public static void Migration_3_LoadInEditor()
    {
        // ===== MIGRATION 3: Loading Atlases in Editor =====

        // ❌ OLD (V1 - Broken):
        /*
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path); // Returns null!
        */

        // ✅ NEW (V2):
        SpriteAtlasAsset asset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>("Assets/MyAtlas.spriteatlasv2");
    }

    /// <summary>
    /// V1 Pattern: Runtime loading (CHANGED SIGNIFICANTLY)
    ///
    /// ❌ OLD V1 CODE:
    /// var atlas = Resources.Load<SpriteAtlas>("Atlases/MyAtlas");
    ///
    /// ✅ NEW V2 CODE:
    /// SpriteAtlasManager.atlasRegistrationNeeded += OnRegistrationNeeded;
    /// // Or use Addressables for dynamic loading
    /// </summary>
    public static void Migration_4_RuntimeLoading()
    {
        // ===== MIGRATION 4: Runtime Loading =====

        // ❌ OLD (V1 - Not recommended):
        /*
        var atlas = Resources.Load<SpriteAtlas>("Atlases/MyAtlas");
        */

        // ✅ NEW (V2 - Recommended):
        // Use SpriteAtlasManager with late-binding
        /*
        public class MyLoader : MonoBehaviour
        {
            private void Awake()
            {
                SpriteAtlasManager.atlasRegistrationNeeded += OnRegistrationNeeded;
            }

            private void OnRegistrationNeeded(Sprite sprite)
            {
                // Load atlas via Addressables or AssetBundles
            }
        }
        */
    }

    /// <summary>
    /// V1 Pattern: Manual packing (CHANGED)
    ///
    /// ❌ OLD V1 CODE:
    /// SpriteAtlasUtility.PackAtlases(atlases, BuildTarget.StandaloneWindows64);
    ///
    /// ✅ NEW V2 CODE:
    /// Use IPreprocessBuildWithReport for automatic packing during build
    /// </summary>
    public static void Migration_5_Packing()
    {
        // ===== MIGRATION 5: Packing Atlases =====

        // ❌ OLD (V1 - Manual):
        /*
        SpriteAtlasUtility.PackAtlases(atlases, BuildTarget.StandaloneWindows64);
        */

        // ✅ NEW (V2 - Automatic via Prebuild):
        /*
        public class MyPreprocessor : IPreprocessBuildWithReport
        {
            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                // Atlases are automatically packed during build
                // Use this for generation/configuration, not manual packing
            }
        }
        */
    }

    /// <summary>
    /// Complete migration checklist:
    ///
    /// [ ] Replace SpriteAtlas creation with SpriteAtlasAsset
    /// [ ] Use SpriteAtlasImporter for all configuration
    /// [ ] Call SaveAndReimport() after every modification
    /// [ ] Use SpriteAtlasManager for runtime loading
    /// [ ] Implement IPreprocessBuildWithReport for automatic generation
    /// [ ] Use .spriteatlasv2 file extension (not .spriteatlas)
    /// </summary>
    public static void Migration_Checklist()
    {
        /*
        MIGRATION CHECKLIST:
        ====================

        1. Asset Creation
           - OLD: ScriptableObject.CreateInstance<SpriteAtlas>()
           - NEW: new SpriteAtlasAsset()

        2. Saving Assets
           - OLD: AssetDatabase.AddObjectToAsset(), EditorUtility.SetDirty()
           - NEW: SpriteAtlasAsset.Save(), AssetDatabase.ImportAsset()

        3. Configuration
           - OLD: importer.SetBuildSettings()
           - NEW: importer.packingSettings, importer.platformSettings

        4. Saving Changes
           - OLD: EditorUtility.SetDirty()
           - NEW: importer.SaveAndReimport()

        5. Loading in Editor
           - OLD: AssetDatabase.LoadAssetAtPath<SpriteAtlas>()
           - NEW: AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>()

        6. Runtime Loading
           - OLD: Resources.Load<SpriteAtlas>()
           - NEW: SpriteAtlasManager + Addressables/AssetBundles

        7. File Extension
           - OLD: .spriteatlas
           - NEW: .spriteatlasv2
        */
    }
}

/// <summary>
/// Example: Complete V1 to V2 migration for a build script.
/// </summary>
public static class BuildScriptMigrationExample
{
    /// <summary>
    /// ❌ OLD V1 Build Script (BROKEN):
    /// This script will NOT work with SpriteAtlas V2.
    /// </summary>
    [System.Obsolete("This is the old V1 pattern - do not use")]
    public static void Old_V1_BuildScript()
    {
        // ❌ BROKEN: This is the old V1 way
        /*
        [MenuItem("Tools/Build Game")]
        public static void BuildGame()
        {
            // Create atlas (V1 - Broken)
            var atlas = ScriptableObject.CreateInstance<SpriteAtlas>();
            atlas.Add(AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites"));

            // Save atlas (V1 - Broken)
            AssetDatabase.AddObjectToAsset(atlas, "Assets/MyAtlas.spriteatlas");
            EditorUtility.SetDirty(atlas);

            // Configure (V1 - Broken)
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath("Assets/MyAtlas.spriteatlas");
            importer.SetBuildSettings(new SpriteAtlasPackingSettings()); // Method removed!

            // Build
            BuildPipeline.BuildPlayer(...);
        }
        */
    }

    /// <summary>
    /// ✅ NEW V2 Build Script (WORKING):
    /// This script works with SpriteAtlas V2.
    /// </summary>
    public static void New_V2_BuildScript()
    {
        // ✅ WORKING: This is the new V2 way

        /*
        [MenuItem("Tools/Build Game")]
        public static void BuildGame()
        {
            // Step 1: Generate/update atlases (V2)
            string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(path);

                // Add sprites if needed
                // asset.Add(sprites);

                // Save and import
                SpriteAtlasAsset.Save(asset, path);
                AssetDatabase.ImportAsset(path);

                // Configure via importer (V2)
                var importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
                importer.includeInBuild = true;
                importer.packingSettings.tightPacking = true;
                importer.SaveAndReimport();
            }

            // Step 2: Build the game
            BuildPipeline.BuildPlayer(...);
        }
        */

        Debug.Log("Use V2 pattern instead of V1");
    }

    /// <summary>
    /// ✅ NEW V2 Prebuild Processor (RECOMMENDED):
    /// This is the recommended approach - automatic atlas generation during build.
    /// </summary>
    public static void New_V2_PreprocessBuild()
    {
        // ✅ RECOMMENDED: Use prebuild processor for automatic generation

        /*
        public class AtlasPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                // Automatically generate or update atlases before build
                GenerateAtlases();
            }

            private void GenerateAtlases()
            {
                // Find sprites by folder, naming convention, or label
                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites" });

                // Create atlas asset
                var asset = new SpriteAtlasAsset();
                var sprites = new Object[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                }
                asset.Add(sprites);

                // Save and import
                string path = "Assets/Atlases/GameSprites.spriteatlasv2";
                SpriteAtlasAsset.Save(asset, path);
                AssetDatabase.ImportAsset(path);

                // Configure via importer
                var importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
                importer.includeInBuild = true;
                importer.packingSettings.tightPacking = true;
                importer.SaveAndReimport();
            }
        }
        */
    }
}
