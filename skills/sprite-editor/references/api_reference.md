# Unity Sprite Editor API Reference

## Table of Contents
- [Core Interface: ISpriteEditorDataProvider](#core-interface-ispriteeditordataprovider)
  - [Properties](#properties)
  - [Core Methods](#core-methods)
  - [Callbacks](#callbacks)
- [Additional Data Providers](#additional-data-providers)
  - [ISpriteNameFileIdDataProvider](#ispritenamefileiddataprovider)
  - [ISpriteOutlineDataProvider](#ispriteoutlinedataprovider)
  - [ISpritePhysicsOutlineDataProvider](#ispritephysicsoutlinedataprovider)
  - [ISpriteBoneDataProvider](#ispritebonedataprovider)
  - [ISpriteMeshDataProvider](#ispritemeshdataprovider)
  - [ITextureDataProvider](#itexturedataprovider)
  - [ISecondaryTextureDataProvider](#isecondarytexturedataprovider)
  - [ISpriteFrameEditCapability](#ispriteframeeditcapability)
- [SpriteRect Properties](#spriterect-properties)
- [Common Patterns](#common-patterns)
  - [Modifying Sprite Properties](#modifying-sprite-properties)
  - [Working with Selection](#working-with-selection)
- [Version Considerations](#version-considerations)

## Core Interface: ISpriteEditorDataProvider

Main interface for editing sprite data. See [templates.md](templates.md) for the standard initialization and usage pattern.

### Properties
- `SpriteImportMode spriteImportMode` - How sprite data will be imported
- `float pixelsPerUnit` - Pixels per unit in world space
- `UnityObject targetObject` - Object providing the data

### Core Methods
- `SpriteRect[] GetSpriteRects()` - Returns array of SpriteRect
- `void SetSpriteRects(SpriteRect[] spriteRects)` - Updates sprite rectangles
- `void Apply()` - Applies changed data
- `void InitSpriteEditorDataProvider()` - Initializes the provider
- `T GetDataProvider<T>()` - Gets additional data providers
- `bool HasDataProvider(Type type)` - Checks if provider type is supported

### Callbacks
- `void RegisterDataChangeCallback(Action<ISpriteEditorDataProvider> action)`
- `void UnregisterDataChangeCallback(Action<ISpriteEditorDataProvider> action)`

## Additional Data Providers

### ISpriteNameFileIdDataProvider
Maps sprite names to file IDs (required for Unity 2021.2+ when adding/removing sprites).

```csharp
var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
IEnumerable<SpriteNameFileIdPair> pairs = nameFileIdProvider.GetNameFileIdPairs();
nameFileIdProvider.SetNameFileIdPairs(updatedPairs);
```

### ISpriteOutlineDataProvider
Manages outline data for sprite tessellation.

```csharp
var outlineProvider = dataProvider.GetDataProvider<ISpriteOutlineDataProvider>();
List<Vector2[]> outlines = outlineProvider.GetOutlines(spriteGuid);
outlineProvider.SetOutlines(spriteGuid, newOutlines);
float tessellation = outlineProvider.GetTessellationDetail(spriteGuid);
outlineProvider.SetTessellationDetail(spriteGuid, 0.5f); // 0-1 range
```

### ISpritePhysicsOutlineDataProvider
Manages physics outlines for Polygon Collider 2D.

```csharp
var physicsProvider = dataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
List<Vector2[]> physicsOutlines = physicsProvider.GetOutlines(spriteGuid);
physicsProvider.SetOutlines(spriteGuid, newPhysicsOutlines);
float tessellation = physicsProvider.GetTessellationDetail(spriteGuid);
physicsProvider.SetTessellationDetail(spriteGuid, 0.5f);
```

### ISpriteBoneDataProvider
Manages bone data for 2D animation.

```csharp
var boneProvider = dataProvider.GetDataProvider<ISpriteBoneDataProvider>();
List<SpriteBone> bones = boneProvider.GetBones(spriteGuid);
boneProvider.SetBones(spriteGuid, updatedBones);
```

### ISpriteMeshDataProvider
Manages custom sprite mesh data (vertices, indices, edges).

```csharp
var meshProvider = dataProvider.GetDataProvider<ISpriteMeshDataProvider>();
Vertex2DMetaData[] vertices = meshProvider.GetVertices(spriteGuid);
int[] indices = meshProvider.GetIndices(spriteGuid);
Vector2Int[] edges = meshProvider.GetEdges(spriteGuid);

meshProvider.SetVertices(spriteGuid, newVertices);
meshProvider.SetIndices(spriteGuid, newIndices);
meshProvider.SetEdges(spriteGuid, newEdges);
```

### ITextureDataProvider
Provides texture data for Sprite Editor.

```csharp
var textureProvider = dataProvider.GetDataProvider<ITextureDataProvider>();
Texture2D texture = textureProvider.texture;
Texture2D preview = textureProvider.previewTexture;
textureProvider.GetTextureActualWidthAndHeight(out int width, out int height);
Texture2D readable = textureProvider.GetReadableTexture2D();
```

### ISecondaryTextureDataProvider
Manages secondary textures.

```csharp
var secondaryProvider = dataProvider.GetDataProvider<ISecondaryTextureDataProvider>();
SecondarySpriteTexture[] textures = secondaryProvider.textures;
secondaryProvider.textures = newTextures;
```

### ISpriteFrameEditCapability
Controls sprite frame editing capabilities.

```csharp
var capabilityProvider = dataProvider.GetDataProvider<ISpriteFrameEditCapability>();
EditCapability capability = capabilityProvider.GetEditCapability();
capabilityProvider.SetEditCapability(newCapability);
```

## SpriteRect Properties

Key properties that can be modified on `SpriteRect`:

- `string name` - Sprite name
- `GUID spriteID` - Unique identifier (matches sprite asset's `GetSpriteID()`)
- `Rect rect` - Position and size in texture
- `Vector2 pivot` - Pivot point (0-1 range, relative to rect)
- `SpriteAlignment alignment` - Alignment preset (BottomLeft, Center, Custom, etc.)
- `Vector4 border` - 9-slice border (left, bottom, right, top)

**Note**: The `spriteID` property matches the GUID returned by calling `GetSpriteID()` on a sprite asset at runtime. This allows matching between editor-time configuration and runtime sprites.

## Common Patterns

See [templates.md](templates.md) for code patterns including:
- Safe Core Pattern with capability checks
- Modifying sprite properties
- Working with selection

## Version Considerations

See [background.md](background.md#version-specific-requirements) for Unity version-specific requirements (ISpriteNameFileIdDataProvider in 2021.2+).
