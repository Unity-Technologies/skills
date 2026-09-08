# Common Errors and Invalid Patterns

This document covers invalid V2 patterns, API corrections, and common mistakes to avoid.

## Table of Contents

- [Invalid V2 Patterns (Do Not Use)](#invalid-v2-patterns-do-not-use)
- [Critical API Corrections](#critical-api-corrections)
  - [❌ Format Casting in TextureImporterPlatformSettings](#-format-casting-in-textureimporterplatformsettings)
  - [❌ SetMasterAtlasPath() - Method Does Not Exist](#-setmasteratlaspath---method-does-not-exist)
  - [⚠️ Platform-Specific Format Limitations](#️-platform-specific-format-limitations)
  - [⚠️ PackAtlases() Requires Runtime SpriteAtlas[]](#️-packatlases-requires-runtime-spriteatlas)
  - [⚠️ Always scope AssetDatabase.FindAssets to the folders you mean](#️-always-scope-assetdatabasefindassets-to-the-folders-you-mean)
  - [⚠️ AssetImporter.GetAtPath() Takes Single Argument](#️-assetimportergetatpath-takes-single-argument)

## Invalid V2 Patterns (Do Not Use)

```csharp
// ❌ WRONG - V1 pattern (scripting SpriteAtlas in editor)
SpriteAtlas atlas = new SpriteAtlas();
atlas.Add(sprites);  // Invalid in V2
atlas.SetTextureSettings(settings);  // Invalid in V2

// ❌ WRONG - Deprecated/invalid SpriteAtlasAsset methods
atlasAsset.SetTextureSettings(settings);  // Use importer instead
atlasAsset.SetPackingSettings(settings);  // Use importer instead
atlasAsset.SetIncludeInBuild(true);       // Use importer instead

// ❌ WRONG - Creating variants from runtime SpriteAtlas
SpriteAtlasAsset masterAsset = ...;
atlasVariant.Add(masterAsset.GetPackables()); // GetPackables() doesn't exist on SpriteAtlasAsset!

// ❌ WRONG - Incorrect packable handling in variants
SpriteAtlasAsset variant = new SpriteAtlasAsset();
variant.SetMasterAtlas(atlasPath); // WRONG: SetMasterAtlas expects a Runtime SpriteAtlas, not path or SpriteAtlasAsset!

// ❌ WRONG - Format cast in TextureImporterPlatformSettings
var settings = new TextureImporterPlatformSettings {
    format = (int)TextureImporterFormat.ASTC_6x6  // ❌ Explicit cast causes compile error
};

// ❌ WRONG - Using deprecated/invalid method
variant.SetMasterAtlasPath(path); // Method doesn't exist - use SetMasterAtlas() with runtime SpriteAtlas
```

## Critical API Corrections

### ❌ Format Casting in TextureImporterPlatformSettings

```csharp
// ❌ WRONG - Explicit cast causes compile error
var settings = new TextureImporterPlatformSettings {
    format = (int)TextureImporterFormat.ASTC_6x6
};

// ✅ CORRECT - Direct enum assignment
var settings = new TextureImporterPlatformSettings {
    format = TextureImporterFormat.ASTC_6x6
};
```

### ❌ SetMasterAtlasPath() - Method Does Not Exist

```csharp
// ❌ WRONG - Method doesn't exist
variant.SetMasterAtlasPath(path);

// ✅ CORRECT - Load runtime SpriteAtlas and pass to SetMasterAtlas()
SpriteAtlas masterRuntime = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(masterPath);
variant.SetMasterAtlas(masterRuntime);
```

### ⚠️ Platform-Specific Format Limitations

- **PVRTC is NOT supported** in SpriteAtlas V2
- Use **ASTC** for Android (`ASTC_6x6`, `ASTC_4x4`)
- Use **ASTC or ETC2** for iOS (`ASTC_4x4`, `ETC2_RGBA8`)
- Use **ETC2** for desktop (`ETC2_RGBA8`)

### ⚠️ PackAtlases() Requires Runtime SpriteAtlas[]

```csharp
// ❌ WRONG - Passing SpriteAtlasAsset[] causes compilation error
SpriteAtlasUtility.PackAtlases(atlasAssets, ...);

// ✅ CORRECT - Load runtime SpriteAtlases first
SpriteAtlas[] atlases = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" })
    .Select(g => AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(g)))
    .Where(a => a != null)
    .ToArray();
SpriteAtlasUtility.PackAtlases(atlases, EditorUserBuildSettings.activeBuildTarget, false);
```

### ⚠️ Always scope AssetDatabase.FindAssets to the folders you mean

```csharp
// ❌ WRONG - does not compile. There is no SearchMode overload.
string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", SearchMode.AllAssets);

// ❌ RISKY - compiles, but searches the WHOLE project including read-only packages
string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas");

// ✅ CORRECT - the second parameter is string[] searchInFolders
string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" });
```

`AssetDatabase.FindAssets` has exactly two overloads, `FindAssets(string filter)` and
`FindAssets(string filter, string[] searchInFolders)`. Verified on Unity 6000.5.8f1.

Scoping matters because an unscoped search reaches into `Packages/`, and those assets are usually
read-only. Measured on a real project: an unscoped `t:Scene` search returned 20 scenes where the
project itself has 1, and all 19 extras were package test fixtures. A batch operation that then
writes to what it found will fail, or worse, target assets it must not modify.

### ⚠️ AssetImporter.GetAtPath() Takes Single Argument

```csharp
// ❌ WRONG - No such overload
SpriteAtlasImporter importer = AssetImporter.GetAtPath(path, typeof(SpriteAtlasImporter));

// ✅ CORRECT - Single argument, cast result
SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
```
