# Sprite Editor Code Templates

## Safe Core Pattern (MANDATORY)

Types are fully qualified because this runs through `eval`, which rejects `using` directives.
If you save it as a `.cs` file instead, add `using UnityEditor.U2D.Sprites;` and shorten them.

Use this structure for all sprite modification tasks.

**CRITICAL:** If capability checks fail, the script MUST return immediately. NEVER bypass capability checks even if you suspect the API might work - this can cause data corruption and violates Unity's data provider contract.

```csharp
// 1. Get and Init Data Provider
var importer = UnityEditor.AssetImporter.GetAtPath(assetPath);
var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
factory.Init();
var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
dataProvider.InitSpriteEditorDataProvider();

// 2. MANDATORY: Check Capabilities - ABORT if not supported
var editCapability = dataProvider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (editCapability == null)
{
    throw new System.Exception("Edit capability not supported by importer. Operation aborted.");
}

var capability = editCapability.GetEditCapability();
// Check for: EditSpriteName, EditSpriteRect, EditBorder, EditPivot, CreateAndDeleteSprite
if (!capability.HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName))
{
    throw new System.Exception("Operation not supported by importer. User action aborted.");
}

// 3. Read and Modify
var spriteRects = dataProvider.GetSpriteRects();
// ... logic here ...

// 4. Apply and Reimport
dataProvider.SetSpriteRects(spriteRects);
dataProvider.Apply();
importer.SaveAndReimport();
```

## Capability Check Pattern

Before performing any modification operation, check if the importer supports it. **ABORT the user action if the capability is not supported.**

**DO NOT rationalize bypassing this check.** Even if you believe the API might accept the operation, capability checks are mandatory for data integrity. Return immediately on failure - no exceptions.

```csharp
var editCapability = dataProvider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if (editCapability == null)
{
    throw new System.Exception("Edit capability not supported by importer. User action aborted.");
    return;
}

var capability = editCapability.GetEditCapability();
if (!capability.HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteName)) // Adjust based on task
{
    throw new System.Exception("Importer does not support the requested operation. User action aborted.");
    return;
}
```

### Available Capabilities

- `EditSpriteName` - Modify sprite names
- `EditSpriteRect` - Modify sprite rectangles
- `EditBorder` - Modify 9-slice borders
- `EditPivot` - Modify pivot points
- `CreateAndDeleteSprite` - Add/remove sprites or perform slicing
