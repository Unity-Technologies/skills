// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// Demonstrates updating build scripts for SpriteAtlas V2.
/// Shows how to migrate from old build patterns to the prebuild pipeline.
/// </summary>
public static class UpdateBuildScriptsDocumentation
{
    /// <summary>
    /// OLD Pattern: Manual atlas packing in build script (NO LONGER RECOMMENDED)
    ///
    /// ❌ OLD CODE:
    /// public static void BuildGame()
    /// {
    ///     // Manually pack atlases before build
    ///     SpriteAtlasUtility.PackAtlases(atlases, target);
    ///     BuildPipeline.BuildPlayer(...);
    /// }
    ///
    /// ✅ NEW V2 CODE:
    /// Implement IPreprocessBuildWithReport for automatic packing
    /// </summary>
    public static void Update_BuildScript_Packing()
    {
        // ===== UPDATING: Build Script with Manual Packing =====

        // ❌ OLD (V1 - Not recommended):
        /*
        [MenuItem("Tools/Build Game")]
        public static void BuildGame()
        {
            // Manually pack atlases
            var guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { "Assets" });
            var atlases = new SpriteAtlas[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                atlases[i] = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            // Manual packing before build
            SpriteAtlasUtility.PackAtlases(atlases, EditorUserBuildSettings.activeBuildTarget);

            // Then build
            BuildPipeline.BuildPlayer(...);
        }
        */

        // ✅ NEW (V2 - Recommended):
        /*
        // Implement this as a prebuild processor instead:
        public class AtlasPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                // Atlases are automatically packed during build
                // Use this for generation/configuration only
            }
        }

        [MenuItem("Tools/Build Game")]
        public static void BuildGame()
        {
            // Just build - atlases are handled automatically
            BuildPipeline.BuildPlayer(...);
        }
        */
    }

    /// <summary>
    /// OLD Pattern: Building with Addressables (REQUIRES UPDATES)
    ///
    /// ❌ OLD CODE:
    /// public static void BuildWithAddressables()
    /// {
    ///     // Manually build atlases
    ///     BuildPipeline.BuildAssetBundles(...);
    ///     AddressableAssetSettings.BuildPlayerContent();
    /// }
    ///
    /// ✅ NEW V2 CODE:
    /// Use IPreprocessBuildWithReport for atlas generation,
    /// IPostprocessBuildWithReport for Addressables content building
    /// </summary>
    public static void Update_BuildScript_Addressables()
    {
        // ===== UPDATING: Build Script with Addressables =====

        // ❌ OLD (V1 - Requires updates):
        /*
        [MenuItem("Tools/Build With Addressables")]
        public static void BuildWithAddressables()
        {
            // Generate atlases
            GenerateAtlases();

            // Build asset bundles
            BuildPipeline.BuildAssetBundles(...);

            // Build addressables content
            AddressableAssetSettings.BuildPlayerContent();
        }
        */

        // ✅ NEW (V2 - Recommended):
        /*
        // Step 1: Prebuild processor for atlas generation
        public class AtlasPreprocessor : IPreprocessBuildWithReport
        {
            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                GenerateAtlases(); // Generate atlases before build
            }
        }

        // Step 2: Postprocess processor for Addressables
        public class AddressablesPostprocessor : IPostprocessBuildWithReport
        {
            public void OnPostprocessBuild(BuildTarget target, string path)
            {
                AddressableAssetSettings.BuildPlayerContent();
            }
        }

        // Step 3: Simple build script
        [MenuItem("Tools/Build With Addressables")]
        public static void BuildWithAddressables()
        {
            // Atlases and addressables are handled automatically
            BuildPipeline.BuildPlayer(...);
        }
        */
    }

    /// <summary>
    /// Complete example: Updated build pipeline with prebuild.
    /// </summary>
    public static void Complete_UpdatedBuildPipeline()
    {
        // ===== COMPLETE: Updated Build Pipeline (V2) =====

        /*
        // ============================================
        // PREBUILD: Generate atlases automatically
        // ============================================

        public class AtlasPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                Debug.Log("Generating atlases before build...");

                // Find all sprite folders
                var folderConfigs = new[]
                {
                    new { Folder = "Assets/Art/UI", Output = "Assets/Atlases/UI.spriteatlasv2" },
                    new { Folder = "Assets/Art/Characters", Output = "Assets/Atlases/Characters.spriteatlasv2" }
                };

                foreach (var config in folderConfigs)
                {
                    GenerateAtlasByFolder(config.Folder, config.Output);
                }

                Debug.Log("Atlas generation complete.");
            }

            private void GenerateAtlasByFolder(string folderPath, string outputPath)
            {
                // Find sprites in folder
                var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

                // Load sprites
                var sprites = new Object[guids.Length];
                for (int i = 0; i < guids.Length; i++)
                {
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                }

                // Create atlas asset
                var asset = new SpriteAtlasAsset();
                asset.Add(sprites);

                // Save and import
                SpriteAtlasAsset.Save(asset, outputPath);
                AssetDatabase.ImportAsset(outputPath);

                // Configure via importer
                var importer = AssetImporter.GetAtPath(outputPath) as SpriteAtlasImporter;
                if (importer != null)
                {
                    importer.includeInBuild = true;
                    importer.packingSettings.tightPacking = true;
                    importer.SaveAndReimport();
                }
            }
        }

        // ============================================
        // BUILD: Simple build script
        // ============================================

        [MenuItem("Tools/Build Game")]
        public static void BuildGame()
        {
            Debug.Log("Starting build...");

            // Atlases are automatically generated by preprocessor
            // No manual packing needed!

            var buildResult = BuildPipeline.BuildPlayer(GetBuildOptions());
            if (buildResult.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {buildResult.outputPath}");
            }
        }

        private static BuildPlayerOptions GetBuildOptions()
        {
            return new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes,
                locationPathName = "Builds/Game.exe",
                target = EditorUserBuildSettings.activeBuildTarget,
                options = BuildOptions.None
            };
        }
        */
    }

    /// <summary>
    /// Migration checklist for build scripts:
    ///
    /// [ ] Remove manual SpriteAtlasUtility.PackAtlases() calls
    /// [ ] Implement IPreprocessBuildWithReport for atlas generation
    /// [ ] Use IPostprocessBuildWithReport for post-build steps (Addressables, etc.)
    /// [ ] Simplify build scripts - most work is now automated
    /// </summary>
    public static void Migration_Checklist()
    {
        /*
        MIGRATION CHECKLIST:
        ====================

        1. Remove Manual Packing
           - OLD: SpriteAtlasUtility.PackAtlases() in build script
           - NEW: Automatic during build (remove manual calls)

        2. Add Prebuild Processor
           - Implement IPreprocessBuildWithReport.OnPreprocessBuild()
           - Generate or update atlases here

        3. Add Postbuild Processor (if needed)
           - Implement IPostprocessBuildWithReport.OnPostprocessBuild()
           - Handle Addressables, asset bundles, etc.

        4. Simplify Build Script
           - Remove manual atlas generation/packing
           - Just call BuildPipeline.BuildPlayer()

        5. Test Build Pipeline
           - Verify atlases are generated before build
           - Check that atlases are included in build output
        */
    }
}

/// <summary>
/// Example: Complete prebuild processor for atlas generation.
/// </summary>
public class CompletePreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log($"[AtlasPreprocessor] Generating atlases...");

        // Generate atlases by folder
        GenerateAtlasByFolder("Assets/Art/UI", "Assets/Atlases/UI.spriteatlasv2");
        GenerateAtlasByFolder("Assets/Art/Characters", "Assets/Atlases/Characters.spriteatlasv2");

        Debug.Log("[AtlasPreprocessor] Atlas generation complete.");
    }

    private void GenerateAtlasByFolder(string folderPath, string outputPath)
    {
        // Find sprites in folder
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

        if (guids.Length == 0)
            return;

        Debug.Log($"[AtlasPreprocessor] Found {guids.Length} sprites in {folderPath}");

        // Load sprites
        var sprites = new Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        // Create or update atlas asset
        SpriteAtlasAsset asset;
        if (System.IO.File.Exists(outputPath))
        {
            asset = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(outputPath);
        }
        else
        {
            asset = new SpriteAtlasAsset();
        }

        asset.Add(sprites);

        // Save and import
        SpriteAtlasAsset.Save(asset, outputPath);
        AssetDatabase.ImportAsset(outputPath);

        // Configure via importer
        var importer = AssetImporter.GetAtPath(outputPath) as SpriteAtlasImporter;
        if (importer != null)
        {
            importer.includeInBuild = true;
            var packingSettings = importer.packingSettings;
            packingSettings.enableTightPacking = true;
            importer.packingSettings = packingSettings;

            // Platform-specific settings
            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                maxTextureSize = 1024,
                format = TextureImporterFormat.ETC2_RGB4
            });

            importer.SetPlatformSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone",
                maxTextureSize = 2048,
                format = TextureImporterFormat.DXT5
            });

            importer.SaveAndReimport();
        }
    }
}
