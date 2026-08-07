# SpriteAtlas V2 API Reference

Complete API documentation for Unity's SpriteAtlas V2 system.

## Table of Contents

- [SpriteAtlasAsset (Editor Authoring)](#spriteatlasasset-editor-authoring)
- [SpriteAtlasImporter (Editor Settings)](#spriteatlasimporter-editor-settings)
- [SpriteAtlasUtility (Editor Packing)](#spriteatlasutility-editor-packing)
- [SpriteAtlas (Runtime)](#spriteatlas-runtime)
- [SpriteAtlasManager (Runtime Loading)](#spriteatlasmanager-runtime-loading)
- [Settings Structures](#settings-structures)

## SpriteAtlasAsset (Editor Authoring)

**Namespace:** `UnityEditor.U2D`
**Location:** `SpriteAtlasAsset.bindings.cs`

Primary API for adding/removing content from sprite atlases.

### Constructor

```csharp
SpriteAtlasAsset atlasAsset = new SpriteAtlasAsset();
```

### Content Management

```csharp
// Add sprites or folders
void Add(Object[] objects)

// Remove packables
void Remove(Object[] objects)

// Remove by index
void RemoveAt(int index)
```

### Persistence

```csharp
// Save to disk
static void Save(SpriteAtlasAsset asset, string path)

// Load from disk
static SpriteAtlasAsset Load(string path)
```

### Variant Configuration

```csharp
// Mark as variant atlas
void SetIsVariant(bool value)

// Check if variant
bool isVariant { get; }

// Set master atlas (requires SpriteAtlas reference)
void SetMasterAtlas(SpriteAtlas master)

// Get master atlas
SpriteAtlas GetMasterAtlas()
```

### Custom Packer

```csharp
// Set custom packing algorithm
void SetScriptablePacker(Object packer)

// Get current packer
Object GetPacker()
```

### Deprecated Methods

These methods are deprecated in V2. Use `SpriteAtlasImporter` instead:

- `SetVariantScale()` → Use `SpriteAtlasImporter.variantScale`
- `SetIncludeInBuild()` → Use `SpriteAtlasImporter.includeInBuild`
- `IsIncludeInBuild()` → Use `SpriteAtlasImporter.includeInBuild`
- `SetPlatformSettings()` → Use `SpriteAtlasImporter.SetPlatformSettings()`
- `SetTextureSettings()` → Use `SpriteAtlasImporter.textureSettings`
- `SetPackingSettings()` → Use `SpriteAtlasImporter.packingSettings`

## SpriteAtlasImporter (Editor Settings)

**Namespace:** `UnityEditor.U2D`
**Location:** `SpriteAtlasImporter.bindings.cs`

Primary API for configuring all sprite atlas settings.

### Getting the Importer

```csharp
string path = "Assets/MyAtlas.spriteatlas";
SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
```

### Basic Properties

```csharp
// Variant scale multiplier
float variantScale { get; set; }

// Include atlas in player build
bool includeInBuild { get; set; }
```

### Texture Settings

```csharp
// Get/set texture settings (struct)
SpriteAtlasTextureSettings textureSettings { get; set; }

// Example:
SpriteAtlasTextureSettings settings = importer.textureSettings;
settings.filterMode = FilterMode.Bilinear;
settings.generateMipMaps = false;
settings.readable = false;
settings.sRGB = true;
importer.textureSettings = settings;
```

### Packing Settings

```csharp
// Get/set packing settings (struct)
SpriteAtlasPackingSettings packingSettings { get; set; }

// Example:
SpriteAtlasPackingSettings settings = importer.packingSettings;
settings.padding = 4;
settings.enableRotation = false;
settings.enableTightPacking = false;
settings.enableAlphaDilation = true;
importer.packingSettings = settings;
```

### Platform Settings

```csharp
// Get platform-specific settings
TextureImporterPlatformSettings GetPlatformSettings(string platform)

// Set platform-specific settings
void SetPlatformSettings(TextureImporterPlatformSettings settings)

// Example:
TextureImporterPlatformSettings platformSettings = importer.GetPlatformSettings("Android");
platformSettings.overridden = true;
platformSettings.maxTextureSize = 2048;
platformSettings.format = TextureImporterFormat.ASTC_6x6;
platformSettings.compressionQuality = 50;
importer.SetPlatformSettings(platformSettings);
```

Platform names: "Standalone", "Android", "iOS", "WebGL", etc.

### Applying Changes

```csharp
// REQUIRED: Save and reimport after any changes
void SaveAndReimport()
```

**Critical:** Always call `SaveAndReimport()` after modifying importer properties.

## SpriteAtlasUtility (Editor Packing)

**Namespace:** `UnityEditor.U2D`
**Location:** `EditorSpritePacking.bindings.cs`

Utilities for packing sprite atlases.

### Packing Operations

```csharp
// Pack all atlases in project for target platform
static void PackAllAtlases(BuildTarget target, bool canCancel)

// Pack specific atlases
static void PackAtlases(SpriteAtlas[] atlases, BuildTarget target, bool canCancel)

// Clean up packing temporary data
static void CleanupAtlasPacking()
```

### Usage

```csharp
using UnityEditor;
using UnityEditor.U2D;

BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

// Pack all
SpriteAtlasUtility.PackAllAtlases(target, canCancel: true);
SpriteAtlasUtility.CleanupAtlasPacking();

// Pack specific
SpriteAtlas[] atlases = new[] { atlas1, atlas2 };
SpriteAtlasUtility.PackAtlases(atlases, target, canCancel: false);
SpriteAtlasUtility.CleanupAtlasPacking();
```

**Important:** Always call `CleanupAtlasPacking()` after batch packing operations.

## SpriteAtlas (Runtime)

**Namespace:** `UnityEngine.U2D`
**Location:** `Runtime/2D/SpriteAtlas/ScriptBindings/SpriteAtlas.bindings.cs`

**RUNTIME ONLY** - Do not script against SpriteAtlas in editor code. Provides read-only access to packed sprites.

### Properties

```csharp
// Total sprite count in atlas
int spriteCount { get; }

// Atlas tag identifier
string tag { get; }

// Check if atlas is variant
bool isVariant { get; }
```

### Sprite Queries

```csharp
// Get sprite by name
Sprite GetSprite(string name)

// Get all sprites
int GetSprites(Sprite[] sprites)

// Get sprites matching name prefix
int GetSprites(Sprite[] sprites, string namePrefix)

// Check if sprite belongs to atlas
bool CanBindTo(Sprite sprite)
```

### Usage

```csharp
using UnityEngine;
using UnityEngine.U2D;

SpriteAtlas atlas = Resources.Load<SpriteAtlas>("MyAtlas");

// Get specific sprite
Sprite sprite = atlas.GetSprite("SpriteName");

// Get all sprites
Sprite[] allSprites = new Sprite[atlas.spriteCount];
int count = atlas.GetSprites(allSprites);

// Get sprites with prefix
Sprite[] enemySprites = new Sprite[atlas.spriteCount];
int enemyCount = atlas.GetSprites(enemySprites, "Enemy_");

// Validate sprite binding
bool canBind = atlas.CanBindTo(sprite);
```

## SpriteAtlasManager (Runtime Loading)

**Namespace:** `UnityEngine.U2D`
**Location:** `Runtime/2D/SpriteAtlas/ScriptBindings/SpriteAtlas.bindings.cs`

Manages atlas loading callbacks and runtime creation.

### Events

```csharp
// Called when an atlas is requested (late binding)
static event Action<string, Action<SpriteAtlas>> atlasRequested

// Called when an atlas is registered
static event Action<SpriteAtlas> atlasRegistered
```

### Late Binding Pattern

```csharp
using UnityEngine;
using UnityEngine.U2D;

public class AtlasLoader : MonoBehaviour
{
    void OnEnable()
    {
        SpriteAtlasManager.atlasRequested += OnAtlasRequested;
        SpriteAtlasManager.atlasRegistered += OnAtlasRegistered;
    }

    void OnDisable()
    {
        SpriteAtlasManager.atlasRequested -= OnAtlasRequested;
        SpriteAtlasManager.atlasRegistered -= OnAtlasRegistered;
    }

    void OnAtlasRequested(string tag, System.Action<SpriteAtlas> callback)
    {
        // Load atlas from Resources or AssetBundle
        SpriteAtlas atlas = Resources.Load<SpriteAtlas>($"Atlases/{tag}");
        callback(atlas);
    }

    void OnAtlasRegistered(SpriteAtlas atlas)
    {
        Debug.Log($"Atlas registered: {atlas.tag}");
    }
}
```

### Runtime Atlas Creation

```csharp
// Create atlas at runtime
static SpriteAtlas CreateSpriteAtlas(string name,
                                     SpriteAtlasRuntimeConfig config,
                                     AtlasPage[] pages)
```

Advanced usage for dynamic atlas generation. See custom-packing.md for details.

## Settings Structures

### SpriteAtlasTextureSettings

```csharp
struct SpriteAtlasTextureSettings
{
    int maxTextureSize { get; }        // Read-only
    int anisoLevel { get; set; }       // Anisotropic filtering level
    FilterMode filterMode { get; set; } // Point, Bilinear, Trilinear
    bool generateMipMaps { get; set; }  // Enable mipmap generation
    bool readable { get; set; }         // CPU-readable texture
    bool sRGB { get; set; }            // Use sRGB color space
}
```

### SpriteAtlasPackingSettings

```csharp
struct SpriteAtlasPackingSettings
{
    int blockOffset { get; set; }        // Block offset for algorithm
    int padding { get; set; }            // Padding between sprites (pixels)
    bool enableRotation { get; set; }    // Allow sprite rotation
    bool enableTightPacking { get; set; } // Pack around sprite meshes
    bool enableAlphaDilation { get; set; } // Prevent bleeding artifacts
}
```

### TextureImporterPlatformSettings

Used for platform-specific configuration. See Unity's `TextureImporter` documentation for complete reference.

Common fields:
```csharp
class TextureImporterPlatformSettings
{
    string name { get; set; }              // Platform name
    bool overridden { get; set; }          // Override default settings
    int maxTextureSize { get; set; }       // Max size (32-8192)
    TextureImporterFormat format { get; set; } // Compression format
    int compressionQuality { get; set; }   // Quality (0-100)
}
```

Common platform names: "Standalone", "Android", "iOS", "WebGL"

Common formats:
- Android: `TextureImporterFormat.ASTC_6x6`, `TextureImporterFormat.ETC2_RGBA8`
- iOS: `TextureImporterFormat.ASTC_6x6`, `TextureImporterFormat.PVRTC_RGBA4`
- Desktop: `TextureImporterFormat.DXT5`, `TextureImporterFormat.BC7`
- WebGL: `TextureImporterFormat.DXT5`
