# Sprite Editor Background Information

## Table of Contents
- [Why Use ISpriteEditorDataProvider](#why-use-ispriteeditordataprovider)
- [Original vs Imported Image Sizes](#original-vs-imported-image-sizes)
  - [The Critical Distinction](#the-critical-distinction)
  - [Important Implications](#important-implications)
  - [Critical for Slicing Operations](#critical-for-slicing-operations)
- [Importer Compatibility](#importer-compatibility)
  - [TextureImporter Configuration](#textureimporter-configuration)
  - [Other Importers](#other-importers)
  - [Data Provider Initialization](#data-provider-initialization)
- [Version-Specific Requirements](#version-specific-requirements)
  - [Unity 2021.2+](#unity-20212)
  - [Unity 2021.1 and Earlier](#unity-20211-and-earlier)

## Why Use ISpriteEditorDataProvider

**ISpriteEditorDataProvider provides a unified interface** that works across all importer types (TextureImporter, PSBImporter, custom importers). This ensures:
- Scripts work consistently regardless of importer type
- Changes are properly communicated to the importer
- Sprite metadata is handled correctly

**Never access importer-specific properties directly.** Always use ISpriteEditorDataProvider for compatibility.

## Original vs Imported Image Sizes

### The Critical Distinction

**Sprite data is always based on the original image size**, not the imported Texture2D size.

**Original Image Size:**
- The dimensions of the source image file before import
- Example: 4096x4096 PNG file

**Imported Texture2D Size:**
- The actual texture size after import
- Can be **smaller** due to:
  - Platform-specific texture size limitations (e.g., mobile max 2048x2048)
  - Texture compression settings
  - Max texture size in import settings

### Important Implications

1. **All sprite data uses original coordinates:**
   - Sprite rectangles (rect)
   - Borders (for 9-slicing)
   - Pivots
   - Outline coordinates

2. **Example:**
   - Original image: 4096x4096 PNG
   - Imported texture: 2048x2048 (due to max size setting)
   - Sprite rect: `(0, 0, 4096, 4096)` ← Still uses original dimensions!

3. **Unity handles scaling internally** when rendering sprites

4. **Always work in original image coordinate space** when editing sprite data

### Critical for Slicing Operations

When performing slicing (automatic, grid, isometric), you **MUST ensure the texture being sliced matches the original source image size**.

If the imported Texture2D is smaller than original:
- Slicing coordinates will be incorrect
- Sprite rectangles won't align with intended regions
- The operation will fail

**Solution:** Use `GetTextureToSlice` utility (see scripts/README.md) to ensure correct texture dimensions for slicing.

## Importer Compatibility

### TextureImporter Configuration

For TextureImporter to support sprites:
- `textureType` must be `TextureImporterType.Sprite`
- `spriteImportMode` must be `SpriteImportMode.Multiple` for multiple sprites

The pre-flight check automatically configures these settings.

### Other Importers

PSBImporter and custom importers may have different configuration requirements. Always verify ISpriteEditorDataProvider support before attempting sprite operations.

### Data Provider Initialization

See [templates.md](templates.md) for the standard initialization pattern. If `dataProvider` is null, the importer does not support sprite editing.

## Version-Specific Requirements

### Unity 2021.2+

Adding or removing sprites requires additional steps:

```csharp
var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
if (nameFileIdProvider != null)
{
    // Get existing name-file ID pairs
    var nameFileIdPairs = nameFileIdProvider.GetNameFileIdPairs();

    // Update pairs when adding/removing sprites
    // Add new pair: nameFileIdPairs.Add(new SpriteNameFileIdPair(name, fileId));
    // Remove pair: nameFileIdPairs.RemoveAll(p => p.name == spriteName);

    nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);
}
```

### Unity 2021.1 and Earlier

ISpriteNameFileIdDataProvider does not exist. Simply use SetSpriteRects() without additional steps.
