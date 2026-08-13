# Custom Sprite Packing with ScriptablePacker

Guide for implementing custom sprite packing algorithms in SpriteAtlas V2.

## Table of Contents

- [Overview](#overview)
- [Use Cases](#use-cases)
- [ScriptablePacker API](#scriptablepacker-api)
  - [Abstract Methods](#abstract-methods)
  - [PackerData Structure](#packerdata-structure)
  - [SpriteData Structure](#spritedata-structure)
  - [SpritePack Output Structure](#spritepack-output-structure)
  - [TextureData Structure](#texturedata-structure)
  - [PackTransform Options](#packtransform-options)
- [Example: Grid-Based Packer](#example-grid-based-packer)
- [Example: Multi-Page Packer](#example-multi-page-packer)
- [Example: Size-Optimized Packer](#example-size-optimized-packer)
- [Assigning Custom Packer to Atlas](#assigning-custom-packer-to-atlas)
- [Best Practices](#best-practices)
  - [Performance](#performance)
  - [Correctness](#correctness)
  - [Maintainability](#maintainability)
- [Common Pitfalls](#common-pitfalls)
- [Debugging Tips](#debugging-tips)

## Overview

`ScriptablePacker` is an abstract base class for creating custom packing algorithms. Extend it to implement specialized packing strategies beyond Unity's default algorithms.

**Namespace:** `UnityEditor.U2D`
**Location:** `SpriteAtlasAsset.bindings.cs`

## Use Cases

Custom packers are useful for:
- Non-standard sprite arrangements (circular, grid-based, specific patterns)
- Multi-page atlas generation with custom page selection
- Sprite preprocessing and optimization before packing
- Platform-specific packing strategies
- Domain-specific packing requirements (UI layouts, sprite sheets)

## ScriptablePacker API

### Abstract Methods

See [resources/scriptablepacker.cs](../resources/scriptablepacker.cs).

### PackerData Structure

Contains all input data for packing:

```csharp
struct PackerData
{
    NativeArray<Color32> colorData;   // Pixel data from source textures
    NativeArray<SpriteData> spriteData; // Sprite information
    NativeArray<TextureData> textureData; // Texture dimensions
    NativeArray<int> indexData;       // Sprite mesh indices
    NativeArray<Vector2> vertexData;  // Sprite mesh vertices
}
```

**Important:** Do NOT dispose these arrays - they're managed externally by Unity.

### SpriteData Structure

Information about each sprite:

See [resources/spritedata.cs](../resources/spritedata.cs).

### SpritePack Output Structure

Set this on each `SpriteData` to specify packing result:

See [resources/spritepack.cs](../resources/spritepack.cs).

### TextureData Structure

Source texture information:

See [resources/texturedata.cs](../resources/texturedata.cs).

### PackTransform Options

Sprite transformations during packing:

See [resources/packtransform.cs](../resources/packtransform.cs).

## Example: Grid-Based Packer

Simple example that arranges sprites in a grid pattern.

See [resources/gridspritepacker.cs](../resources/gridspritepacker.cs).

## Example: Multi-Page Packer

Distributes sprites across multiple atlas pages.

See [resources/multipagepacker.cs](../resources/multipagepacker.cs).

## Example: Size-Optimized Packer

Packs larger sprites first for better space utilization.

See [resources/sizeoptimizedpacker.cs](../resources/sizeoptimizedpacker.cs).

## Assigning Custom Packer to Atlas

Use `SpriteAtlasAsset.SetScriptablePacker()` to assign your custom packer.

See [resources/custompackersetup.cs](../resources/custompackersetup.cs).

## Best Practices

### Performance

1. **Minimize array access**: Cache NativeArray values when accessing multiple times
2. **Avoid allocations**: Don't create new arrays or collections in hot paths
3. **Use simple algorithms**: Complex packing algorithms can slow down import times

### Correctness

1. **Don't dispose input arrays**: Unity manages these - don't dispose them
2. **Validate output bounds**: Ensure sprites fit within atlas dimensions
3. **Handle edge cases**: Empty sprite lists, oversized sprites, etc.
4. **Return false on failure**: Let Unity know packing failed

### Maintainability

1. **Make packer configurable**: Add public properties for tweaking behavior
2. **Add debug logging**: Log packing statistics and warnings
3. **Document algorithm**: Explain packing strategy in comments
4. **Test with various inputs**: Different sprite sizes, counts, and arrangements

## Common Pitfalls

1. **Disposing input arrays**: Unity manages these - don't dispose them
2. **Forgetting to set `sprite.output`**: Each sprite needs output position
3. **Exceeding atlas bounds**: Validate that packed sprites fit within max texture size
4. **Not handling multi-page**: Set `sprite.output.page` correctly for multi-page atlases
5. **Modifying read-only fields**: Some fields like texture dimensions are read-only
6. **Forgetting to assign packer**: Use `SetScriptablePacker()` before saving atlas

## Debugging Tips

See [resources/packerdebugging.cs](../resources/packerdebugging.cs).
