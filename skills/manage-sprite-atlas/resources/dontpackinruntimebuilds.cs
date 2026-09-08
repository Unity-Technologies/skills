// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.U2D;

/// <summary>
/// Demonstrates why you should NOT pack atlases during runtime builds.
/// Packed atlases during build time are already optimized for the target platform.
/// </summary>
public static class DontPackInRuntimeBuildsDocumentation
{
    /// <summary>
    /// ❌ WRONG: Attempting to pack atlases during a runtime build.
    /// This is unnecessary and can cause issues because:
    /// 1. Atlases should be packed at editor time (prebuild)
    /// 2. Runtime builds don't have access to all source assets
    /// 3. It slows down the build process significantly
    /// </summary>
    public static void Wrong_PackDuringRuntimeBuild()
    {
        // This is WRONG - do not pack during runtime build

        // ❌ BAD: Calling PackAtlases in a build callback
        // public class MyBuildProcessor : IPostprocessBuildWithReport
        // {
        //     public void OnPostprocessBuild(BuildTarget target, string path)
        //     {
        //         // Don't do this!
        //         SpriteAtlasUtility.PackAtlases(atlases, BuildTarget.NoAPI);
        //     }
        // }

        // ❌ BAD: Using SpriteAtlasUtility.PackAtlases in a runtime build
        // This method is for editor preview only, not for build-time packing
    }

    /// <summary>
    /// ❌ WRONG: Calling SaveAndReimport() during a build.
    /// Modifying assets during build can cause unpredictable behavior.
    /// </summary>
    public static void Wrong_ModifyDuringBuild()
    {
        // This is WRONG - do not modify assets during build

        // ❌ BAD: Saving asset database during build
        // AssetDatabase.SaveAssets();
        // AssetDatabase.ImportAsset(path);
        // importer.SaveAndReimport(); // Don't do this in build!
    }

    /// <summary>
    /// ✅ CORRECT: Pack atlases during prebuild, not runtime.
    /// Use IPreprocessBuildWithReport to pack atlases before the build starts.
    /// </summary>
    public static void Correct_PackDuringPrebuild()
    {
        // ✅ CORRECT: Implement IPreprocessBuildWithReport for packing

        /*
        public class AtlasPacker : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                // Find and pack all atlases before build
                string[] guids = AssetDatabase.FindAssets("t:SpriteAtlasAsset", new[] { "Assets" });
                var atlases = new SpriteAtlasAsset[guids.Length];

                for (int i = 0; i < guids.Length; i++)
                {
                    atlases[i] = AssetDatabase.LoadAssetAtPath<SpriteAtlasAsset>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                }

                // Pack is optional here - build-time packing happens automatically
                // SpriteAtlasUtility.PackAtlases(atlases, BuildTarget.NoAPI);
            }
        }
        */
    }

    /// <summary>
    /// ✅ CORRECT: Use prebuild pipeline for atlas generation.
    /// This is the recommended approach for all atlas operations.
    /// </summary>
    public static void Correct_UsePrebuildPipeline()
    {
        // ✅ Implement IPreprocessBuildWithReport for:
        // - Atlas generation
        // - Atlas configuration
        // - Batch packing

        /*
        [InitializeOnLoad]
        public class MyAtlasPreprocessor
        {
            static MyAtlasPreprocessor()
            {
                EditorApplication.wantsToQuit += () =>
                {
                    // Clean up any temporary assets before quit
                    return true;
                };
            }
        }

        public class MyBuildProcessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildTarget target, string path)
            {
                // Generate or update atlases here
                // This runs BEFORE the build starts
            }
        }
        */
    }

    /// <summary>
    /// ✅ CORRECT: For runtime loading, use SpriteAtlasManager with late-binding.
    /// Load atlases on-demand at runtime using Addressables or AssetBundles.
    /// </summary>
    public static void Correct_RuntimeLateBinding()
    {
        // ✅ Use SpriteAtlasManager for runtime atlas loading

        /*
        public class RuntimeAtlasLoader : MonoBehaviour
        {
            private void Awake()
            {
                // Register for late-binding
                SpriteAtlasManager.atlasRegistrationNeeded += OnAtlasRegistrationNeeded;
            }

            private void OnAtlasRegistrationNeeded(Sprite sprite)
            {
                // Load the appropriate atlas using Addressables or AssetBundles
                // This runs at runtime, not during build
            }
        }
        */
    }
}

/// <summary>
/// Summary of when to pack atlases:
///
/// Editor Time (✅ DO):
/// - IPreprocessBuildWithReport.OnPreprocessBuild()
/// - Manual packing via SpriteAtlasUtility.PackAtlases() for preview
///
/// Build Time (❌ DON'T):
/// - IPostprocessBuildWithReport.OnPostprocessBuild()
/// - Any time during actual build process
///
/// Runtime (✅ DO):
/// - Load atlases on-demand using SpriteAtlasManager
/// - Use Addressables or AssetBundles for dynamic loading
/// </summary>
public static class PackingTimeline
{
    /// <summary>
    /// Editor: Prebuild - Pack atlases here ✅
    /// </summary>
    public static void Pack_Editor_Prebuild()
    {
        // IPreprocessBuildWithReport.OnPreprocessBuild()
        // This is the correct time to pack atlases
    }

    /// <summary>
    /// Build: Build process - Do NOT pack here ❌
    /// </summary>
    public static void Pack_Build_Process()
    {
        // IPostprocessBuildWithReport.OnPostprocessBuild()
        // Atlases are already packed at this point
    }

    /// <summary>
    /// Runtime: Late-binding - Load atlases on-demand ✅
    /// </summary>
    public static void Pack_Runtime_LateBinding()
    {
        // SpriteAtlasManager.atlasRegistrationNeeded
        // Load atlases as needed during gameplay
    }
}
