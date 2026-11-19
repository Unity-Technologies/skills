# Common Errors and Invalid Patterns

Errors that SpriteAtlas V2 scripts commonly hit, with their cause and fix.

Relative paths in this file resolve to the skill directory — the folder that holds the skill's main instructions.

## Table of Contents

- [Errors with a message](#errors-with-a-message)
- [Mistakes with no error message](#mistakes-with-no-error-message)

## Errors with a message

Match the message the Console or compiler shows against the first column. Compiler messages start with the code shown, followed by the member names from the user's code.

| Error message | Root cause | Fix |
|---|---|---|
| Inspector: `Sprite Atlas packing is disabled` | `EditorSettings.spritePackerMode` is `Disabled`, its default value | Set it to `SpritePackerMode.SpriteAtlasV2` with [scripts/enablespritepacking.cs](../scripts/enablespritepacking.cs), then read it back |
| `CS0266: Cannot implicitly convert type 'int' to 'UnityEditor.TextureImporterFormat'. An explicit conversion exists (are you missing a cast?)` | `format = (int)TextureImporterFormat.ASTC_6x6` casts the enum to `int` | Assign the enum directly: `format = TextureImporterFormat.ASTC_6x6` |
| `CS1061: 'SpriteAtlasAsset' does not contain a definition for 'SetMasterAtlasPath'` | `SetMasterAtlasPath` doesn't exist | Load the master with `AssetDatabase.LoadAssetAtPath<SpriteAtlas>(masterPath)` and pass it to `SetMasterAtlas` |
| `CS1503: Argument 1: cannot convert from 'string' to 'UnityEngine.U2D.SpriteAtlas'` on `SetMasterAtlas` | `SetMasterAtlas` takes the runtime `SpriteAtlas`, not a path or a `SpriteAtlasAsset` | Load the master as `SpriteAtlas` first, then pass that instance |
| `CS1929: 'SpriteAtlasAsset' does not contain a definition for 'GetPackables' and the best extension method overload 'SpriteAtlasExtensions.GetPackables(SpriteAtlas)' requires a receiver of type 'SpriteAtlas'` | `GetPackables` is an extension method on the runtime `SpriteAtlas`, not on `SpriteAtlasAsset` | Load the master as `SpriteAtlas` and call `GetPackables()` on it |
| `CS1503: Argument 1: cannot convert from 'UnityEditor.U2D.SpriteAtlasAsset[]' to 'UnityEngine.U2D.SpriteAtlas[]'` on `PackAtlases` | `SpriteAtlasUtility.PackAtlases` takes runtime `SpriteAtlas[]` | Load each atlas with `AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path)` and pass that array |
| `CS1501: No overload for method 'GetAtPath' takes 2 arguments` | `AssetImporter.GetAtPath` takes only a path | Call `AssetImporter.GetAtPath(path) as SpriteAtlasImporter` |
| `CS0103: The name 'SearchMode' does not exist in the current context` on `AssetDatabase.FindAssets` | `FindAssets` has no `SearchMode` overload; the second parameter is `string[] searchInFolders` | Pass the folders to search: `AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" })` |
| `CS0618: 'SpriteAtlasAsset.SetTextureSettings(SpriteAtlasTextureSettings)' is obsolete: 'SetTextureSettings is no longer supported and will be removed. Use SpriteAtlasImporter.SetTextureSettings instead.'` (same for `SetPackingSettings`, `SetIncludeInBuild`, `SetPlatformSettings`) | These `SpriteAtlasAsset` setters are deprecated in V2 | Set the matching `SpriteAtlasImporter` member, then call `SaveAndReimport()`. Despite the warning text, the importer exposes `textureSettings`, `packingSettings` and `includeInBuild` as properties, not `Set...` methods; only `SetPlatformSettings` is a method. The deprecated-methods table in the API reference file maps each one |

## Mistakes with no error message

These compile and import cleanly, so check for them in the code you write.

| Mistake | Why it matters | Fix |
|---|---|---|
| V1 pattern: `atlas.Add(...)` or other `SpriteAtlasExtensions` calls on a `SpriteAtlas` in editor code | These compile, because they resolve to the `SpriteAtlasExtensions` editor methods, so nothing warns you that you've bypassed V2 authoring | Author with `SpriteAtlasAsset` and configure with `SpriteAtlasImporter`. See [scripts/dontscriptspriteatlasineditor.cs](../scripts/dontscriptspriteatlasineditor.cs) |
| Unscoped `AssetDatabase.FindAssets("t:SpriteAtlas")` | It also searches `Packages/`, whose assets are read-only, so a batch write targets assets it must not modify | Always pass `new[] { "Assets" }` or the specific folders you mean |
| PVRTC formats in a V2 platform override | PVRTC is not supported in SpriteAtlas V2 | Use ASTC (`ASTC_4x4`, `ASTC_6x6`) for Android and iOS, or ETC2 (`ETC2_RGBA8`) for iOS |
| Importer properties changed without `SaveAndReimport()` | The change never reaches the imported atlas | Call `importer.SaveAndReimport()` after every change |
