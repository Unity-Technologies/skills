# SpriteAtlas V2 Best Practices

Optimization tips, common pitfalls, and recommended patterns for SpriteAtlas V2.

## Table of Contents

- [V2 Architecture Rules (Critical)](#v2-architecture-rules-critical)
- [Editor Code Best Practices](#editor-code-best-practices)
  - [Always Use SpriteAtlasImporter for Configuration](#always-use-spriteatlasimporter-for-configuration)
  - [Save SpriteAtlasAsset Properly](#save-spriteatlasasset-properly)
  - [Clean Up After Batch Operations](#clean-up-after-batch-operations)
  - [Handle Null Cases](#handle-null-cases)
  - [Use Correct Platform Names](#use-correct-platform-names)
- [Runtime Code Best Practices](#runtime-code-best-practices)
  - [Register Callbacks Early](#register-callbacks-early)
  - [Cache Loaded Atlases](#cache-loaded-atlases)
  - [Handle Late Binding Properly](#handle-late-binding-properly)
  - [Validate Sprite Binding](#validate-sprite-binding)
  - [Query Sprites Efficiently](#query-sprites-efficiently)
- [Performance Best Practices](#performance-best-practices)
  - [Optimize Texture Settings](#optimize-texture-settings)
  - [Enable Tight Packing](#enable-tight-packing)
  - [Use Variants Wisely](#use-variants-wisely)
  - [Batch Pack Operations](#batch-pack-operations)
  - [Enable Alpha Dilation](#enable-alpha-dilation)
  - [Set includeInBuild Appropriately](#set-includeinbuild-appropriately)
- [Common Pitfalls](#common-pitfalls)
  - [Don't Script SpriteAtlas in Editor](#dont-script-spriteatlas-in-editor)
  - [Don't Use Deprecated SpriteAtlasAsset Methods](#dont-use-deprecated-spriteatlasasset-methods)
  - [Don't Forget to Reimport](#dont-forget-to-reimport)
  - [Don't Pack in Runtime Builds](#dont-pack-in-runtime-builds)
  - [Don't Modify Importer Without Saving](#dont-modify-importer-without-saving)
  - [Don't Confuse Authoring vs Runtime](#dont-confuse-authoring-vs-runtime)
- [Migration from V1 to V2](#migration-from-v1-to-v2)
  - [Replace Direct SpriteAtlas Scripting](#replace-direct-spriteatlas-scripting)
  - [Update Settings Configuration](#update-settings-configuration)
  - [Update Build Scripts](#update-build-scripts)
- [Checklist](#checklist)
  - [Before Creating an Atlas](#before-creating-an-atlas)
  - [When Creating an Atlas](#when-creating-an-atlas)
  - [When Packing Atlases](#when-packing-atlases)
  - [Runtime Loading](#runtime-loading)

## V2 Architecture Rules (Critical)

1. **NEVER script against SpriteAtlas in editor code** - SpriteAtlas is runtime-only in V2
2. **Use SpriteAtlasAsset for content authoring** - Add/remove sprites and folders
3. **Use SpriteAtlasImporter for all settings** - Texture, packing, platform, and variant configuration
4. **Follow the two-step pattern** - Create asset with SpriteAtlasAsset, configure with SpriteAtlasImporter
5. **Always call SaveAndReimport()** - Required after modifying importer settings

## Editor Code Best Practices

### Always Use SpriteAtlasImporter for Configuration

See [resources/editorconfiguration.cs](../resources/editorconfiguration.cs).

### Save SpriteAtlasAsset Properly

See [resources/savespriteatlasasset.cs](../resources/savespriteatlasasset.cs).

### Clean Up After Batch Operations

See [resources/cleanupbatchoperations.cs](../resources/cleanupbatchoperations.cs).

### Handle Null Cases

See [resources/handlenullcases.cs](../resources/handlenullcases.cs).

### Use Correct Platform Names

See [resources/platformnames.cs](../resources/platformnames.cs).

## Runtime Code Best Practices

### Register Callbacks Early

See [resources/registercallbacks.cs](../resources/registercallbacks.cs).

### Cache Loaded Atlases

See [resources/cacheloadedatlases.cs](../resources/cacheloadedatlases.cs).

### Handle Late Binding Properly

See [resources/handlelatebinding.cs](../resources/handlelatebinding.cs).

### Query Sprites Efficiently

See [resources/queryspritesefficiently.cs](../resources/queryspritesefficiently.cs).

## Performance Best Practices

### Optimize Texture Settings

See [resources/optimizetexturesettings.cs](../resources/optimizetexturesettings.cs).

### Enable Tight Packing

See [resources/enablespritepacking.cs](../resources/enablespritepacking.cs).

### Use Variants Wisely

See [resources/usevariantswisely.cs](../resources/usevariantswisely.cs).

### Set includeInBuild Appropriately

See [resources/setincludeinbuild.cs](../resources/setincludeinbuild.cs).

## Common Pitfalls

### Don't Script SpriteAtlas in Editor

See [resources/dontscriptspriteatlasineditor.cs](../resources/dontscriptspriteatlasineditor.cs).

### Don't Use Deprecated SpriteAtlasAsset Methods

See [resources/deprecatedmethods.cs](../resources/deprecatedmethods.cs).

### Don't Pack in Runtime Builds

See [resources/dontpackinruntimebuilds.cs](../resources/dontpackinruntimebuilds.cs).

### Don't Modify Importer Without Saving

See [resources/dontmodifywithoutsaving.cs](../resources/dontmodifywithoutsaving.cs).

### Don't Confuse Authoring vs Runtime

See [resources/authoringvsruntime.cs](../resources/authoringvsruntime.cs).

## Migration from V1 to V2

If migrating from V1 code:

### Replace Direct SpriteAtlas Scripting

See [resources/migratev1tov2.cs](../resources/migratev1tov2.cs).

### Update Settings Configuration

See [resources/updatesettingsconfiguration.cs](../resources/updatesettingsconfiguration.cs).

### Update Build Scripts

See [resources/updatebuildscripts.cs](../resources/updatebuildscripts.cs).

## Checklist

### Before Creating an Atlas

- [ ] Know what sprites to include
- [ ] Determine target platforms and formats
- [ ] Decide on texture quality settings
- [ ] Plan for variants if needed

### When Creating an Atlas

- [ ] Create SpriteAtlasAsset
- [ ] Add sprites or folders
- [ ] Save to disk with SpriteAtlasAsset.Save()
- [ ] Import with AssetDatabase.ImportAsset()
- [ ] Get SpriteAtlasImporter
- [ ] Configure texture settings
- [ ] Configure packing settings
- [ ] Configure platform settings
- [ ] Set includeInBuild
- [ ] Call SaveAndReimport()

### When Packing Atlases

- [ ] Use appropriate BuildTarget
- [ ] Wrap in try/finally block
- [ ] Call CleanupAtlasPacking() in finally
- [ ] Validate packing results

### Runtime Loading

- [ ] Register callbacks early (Awake/OnEnable)
- [ ] Unregister callbacks (OnDestroy/OnDisable)
- [ ] Cache loaded atlases
- [ ] Handle load failures gracefully
- [ ] Use async loading when possible
