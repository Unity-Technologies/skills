# Sprite Editor Utility Examples

Reference implementations demonstrating sprite editing operations. Each file contains a single method for token efficiency.

## Basic Operations

### GetTextureSourceImageSize.cs
Gets the original source image dimensions (before import).

**Use when:** You need to know the true image dimensions, not the imported texture size.

**Key APIs:** ITextureDataProvider.GetTextureActualWidthAndHeight()

### SpriteToPng.cs
Exports a sprite to PNG byte array by rendering its mesh geometry.

**Use when:** Extracting individual sprites from sprite sheets.

**Key concepts:** Renders sprite vertices, UVs, and triangles. Handles tight packing and custom meshes.

### SetPivotExample.cs
Demonstrates setting sprite pivots using both predefined alignments and custom pivot positions.

**Use when:** Changing sprite pivot points.

**Key methods:**
- `SpriteEditorUtility.SetCustomPivot()` - Sets custom pivot position. Must set `alignment = SpriteAlignment.Custom` and `pivot = Vector2`
- `SpriteEditorUtility.SetPivot()` - Sets predefined alignment (Center, BottomLeft, TopRight, etc.)

**Important:** Always set `alignment` field when changing pivots. Custom pivots require `SpriteAlignment.Custom`.

## Slicing Operations

### GetTextureToSlice.cs
Prepares a readable texture for slicing, upscaling to original dimensions if needed.

**Use when:** Before any slicing operation to ensure correct coordinates.

**Key concept:** See [../references/background.md](../references/background.md#critical-for-slicing-operations) for why this is necessary.

### AutomaticSliceTexture.cs
Automatically detects sprite regions using Unity's built-in detection algorithm.

**Use when:** Slicing sprite sheets where sprites have transparent borders.

**Key APIs:**
- UnityEditorInternal.InternalSpriteUtility.GenerateAutomaticSpriteRectangles()
- GenerateNewSpriteRects() for sprite management

### GridSliceTexture.cs
Slices textures into regular grid patterns.

**Use when:** Sprite sheet has evenly-spaced sprites (e.g., animation frames, tile sets).

**Parameters:** offset, size, padding, keepEmptyRects

**Key APIs:** UnityEditorInternal.InternalSpriteUtility.GenerateGridSpriteRectangles()

### IsometricSliceTexture.cs
Slices textures into isometric diamond-pattern tiles.

**Use when:** Working with isometric tile sets (e.g., isometric RPG tiles).

**Key features:**
- Diamond-shaped outline generation
- Empty tile detection based on alpha pixels
- Alternating row offset support

**Key APIs:** ISpriteOutlineDataProvider for diamond outlines

### GenerateNewSpriteRects.cs
Core utility for managing sprite rectangles during slicing operations.

**Three modes:**
- **DeleteAll**: Replace all existing sprites with new ones
- **Smart**: Update overlapping sprites, add non-overlapping ones
- **Safe**: Only add sprites that don't overlap with existing sprites

**Key features:**
- Automatic sprite naming with conflict resolution
- Overlap detection (with tolerance and best-fit options)
- Preserves existing sprites in Safe/Smart modes

**Use when:** Implementing custom slicing logic or managing sprite updates.

## Usage Pattern

All slicing utilities follow this pattern:

```csharp
// 1. Get texture at original size
var texture = GetTextureToSlice(textureProvider);

// 2. Generate rectangles
var rects = [algorithm to generate Rect collection];

// 3. Convert to SpriteRects with management logic
var newRects = GenerateNewSpriteRects(
    spriteDataProvider,
    rects,
    addNewSpriteMethod,
    nameGenerator
);

// 4. Apply to data provider
spriteDataProvider.SetSpriteRects(newRects.ToArray());
```

## Name Generator Examples

The `nameGenerator` parameter is a function that takes an integer index and returns a sprite name string.

### Using Asset Filename from Data Provider
```csharp
string assetPath = AssetDatabase.GetAssetPath(spriteDataProvider.targetObject);
string filename = !string.IsNullOrEmpty(assetPath)
    ? System.IO.Path.GetFileNameWithoutExtension(assetPath)
    : "sprite";
Func<int, string> nameGenerator = (index) => $"{filename}_{index}";
// Produces: character_sheet_0, character_sheet_1, etc. (or sprite_0, sprite_1 if no path)
```

### Simple Numbered Names
```csharp
Func<int, string> nameGenerator = (index) => $"sprite_{index}";
// Produces: sprite_0, sprite_1, sprite_2, etc.
```

## Important Notes

- All coordinates are in **original image space** (see [../references/background.md](../references/background.md#original-vs-imported-image-sizes))
- Use GetTextureToSlice before any slicing operation
- GenerateNewSpriteRects handles name conflicts and overlap detection
- For Unity 2021.2+ requirements, see [../references/background.md](../references/background.md#version-specific-requirements)
