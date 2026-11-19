# SpriteAtlas V2 API

Where to read each V2 type's Script Reference page, and the API rules a model is likely to get wrong.

Relative paths in this file resolve to the skill directory.

## Table of Contents

- [Script Reference pages](#script-reference-pages)
- [Deprecated SpriteAtlasAsset methods](#deprecated-spriteatlasasset-methods)
- [Key Requirements (V2-Specific)](#key-requirements-v2-specific)
- [Namespaces](#namespaces)

## Script Reference pages

Read the page for each type before you write code against it, because member names and overloads change between Unity versions. Each entry gives the local file under `<editor-install>/Editor/Data/Documentation/en/ScriptReference/` and the online page. Replace `<VERSION>` with the project's Unity version, as described in the Full topic map of the skill.

- **SpriteAtlasAsset** (editor authoring: add and remove packables, save, load, variants, custom packer) — local `U2D.SpriteAtlasAsset.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/u2d/spriteatlasasset.md`
- **SpriteAtlasImporter** (editor settings: texture, packing, platform, `includeInBuild`, `variantScale`) — local `U2D.SpriteAtlasImporter.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/u2d/spriteatlasimporter.md`
- **SpriteAtlasUtility** (editor preview packing) — local `U2D.SpriteAtlasUtility.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/u2d/spriteatlasutility.md`
- **SpriteAtlasExtensions** (editor extension methods on `SpriteAtlas`, including `GetPackables`) — local `U2D.SpriteAtlasExtensions.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/u2d/spriteatlasextensions.md`
- **SpriteAtlas** (runtime sprite queries) — local `U2D.SpriteAtlas.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/u2d/spriteatlas.md`
- **SpriteAtlasManager** (runtime late-binding events) — local `U2D.SpriteAtlasManager.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/u2d/spriteatlasmanager.md`
- **TextureImporterPlatformSettings** (per-platform overrides passed to `SpriteAtlasImporter.SetPlatformSettings`) — local `TextureImporterPlatformSettings.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/textureimporterplatformsettings.md`

For which compression formats each platform supports, read the **Choose a GPU texture format by platform** page in [docs-map.md](docs-map.md) rather than guessing.

## Deprecated SpriteAtlasAsset methods

These `SpriteAtlasAsset` setters are deprecated in V2. Use the `SpriteAtlasImporter` member instead, because the importer is what the build reads:

| Deprecated | Use instead |
|---|---|
| `SetVariantScale()` | `SpriteAtlasImporter.variantScale` |
| `SetIncludeInBuild()`, `IsIncludeInBuild()` | `SpriteAtlasImporter.includeInBuild` |
| `SetPlatformSettings()` | `SpriteAtlasImporter.SetPlatformSettings()` |
| `SetTextureSettings()` | `SpriteAtlasImporter.textureSettings` |
| `SetPackingSettings()` | `SpriteAtlasImporter.packingSettings` |

## Key Requirements (V2-Specific)

1. **🚨 ALWAYS check for existing scripts FIRST**: Before generating code, scan for scripts with `[UNITY-SKILL:SPRITEATLAS]` identifier and prompt user to update or create new
2. **🚨 ALWAYS tag generated scripts**: Include skill identifier at the top of every generated script for future detection
3. **🚨 ALWAYS ask for delivery mechanism**: Ask user "Built-in data or Addressables?" before generating code
4. **🚨 ALWAYS enable Sprite Packer mode**: Set `EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2` in prebuild script
5. **🚨 ALWAYS use prebuild generation**: Implement `IPreprocessBuildWithReport` as the DEFAULT approach for creating atlases
6. **🚨 ONLY add sprites from Assets/ folder**: Never add sprites from Packages, built-in assets, or external locations
7. **🚨 For Addressables delivery, ALWAYS generate THREE scripts**:
   - Prebuild atlas generator with Addressables setup
   - `IPostprocessBuildWithReport` to build Addressables content
   - Late-binding runtime loader script that subscribes to `SpriteAtlasManager.atlasRequested`
8. **Set includeInBuild correctly**: `true` for built-in data, `false` for Addressables
9. **Two-Step Editor Pattern**: Create with `SpriteAtlasAsset` → Save → Import → Configure via `SpriteAtlasImporter` → SaveAndReimport(), because importer changes don't apply until the asset reimports
10. **Never use `SpriteAtlas` in editor code** — except `SpriteAtlas.GetPackables()` instance method on a loaded runtime atlas
11. **Variants reference master**: Use `SetMasterAtlas(SpriteAtlas)` with runtime instance loaded via `AssetDatabase.LoadAssetAtPath<SpriteAtlas>()`
12. **Platform format assignment**: Use `format = TextureImporterFormat.ASTC_6x6` directly (no cast)
13. **File extension**: V2 atlases use `.spriteatlasv2` (not `.spriteatlas`)
14. **`SpriteAtlasUtility.PackAtlases()` is optional**: Only for editor preview and verification; build-time packing is automatic

For common errors and invalid patterns, see [common-errors.md](common-errors.md).

## Namespaces

**Editor Scripts:**
```csharp
using UnityEditor;            // AssetImporter, AssetDatabase, TextureImporterFormat
using UnityEditor.U2D;        // SpriteAtlasAsset, SpriteAtlasImporter, SpriteAtlasUtility
using UnityEngine;            // Core Unity types
using UnityEngine.U2D;        // SpriteAtlas
```

**Runtime Scripts:**
```csharp
using UnityEngine;            // Core Unity types
using UnityEngine.U2D;        // SpriteAtlas, SpriteAtlasManager
```
