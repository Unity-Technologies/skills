# Localization API Reference

## LocalizationEditorSettings
The primary entry point for Editor-time localization settings.
- `GetStringTableCollections()` / `GetAssetTableCollections()`: Retrieves collections.
- `CreateStringTableCollection(name, path)`: Factory methods. Expects a **directory path** (e.g., `Assets/Localization`), not a full asset path.
- `EditorEvents.RaiseCollectionModified`: Must be called after any modification to refresh the editor.

```csharp
LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(sender, collection);
```

## Asset Table Entries
When adding entries to an Asset Table (fonts, textures, etc.), always use the asset's GUID:

```csharp
string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(myAsset));
var entry = table.GetEntry(sharedEntryId) ?? table.AddEntry(sharedEntryId, guid);
entry.Guid = guid;
EditorUtility.SetDirty(table);
// SetDirty only marks the table. Without a save the entry reads back correctly for the rest of
// the session and is gone when the Editor closes. See the save sequence in SKILL.md: dirty the
// collection and its SharedData too, then save once at the end.
AssetDatabase.SaveAssets();
```

## Common Namespace Conflicts (CS0118)
`UnityEngine.UI` is both a namespace and a class container — always fully qualify these types:
- `UnityEngine.UI.Image`
- `UnityEngine.UI.ScrollRect`
- `UnityEngine.UI.Mask`
- `UnityEngine.UI.CanvasScaler`
- `UnityEngine.UI.GraphicRaycaster`
- `UnityEngine.UI.VerticalLayoutGroup`
- `UnityEngine.UI.ContentSizeFitter`

## Addressables Requirement
Any asset referenced in an Asset Table must be marked as Addressable:

```csharp
var guid = AssetDatabase.AssetPathToGUID(assetPath);
var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
```

## Avoiding the Resources Folder
Do not reference assets inside a `Resources/` folder in Asset Tables — the Addressables system
cannot load sub-assets from Resources, causing `OperationException: Failed to load sub-asset`.
Copy the asset out first:

```csharp
string source = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
string target = "Assets/Fonts/LiberationSans SDF Localized.asset";
if (!File.Exists(target)) {
    AssetDatabase.CopyAsset(source, target);
    AssetDatabase.ImportAsset(target);
}
// Use target path for Addressables and Asset Tables
```

## Triggering an Addressables Build
After modifying Addressable entries or Asset Tables:

```csharp
using UnityEditor.AddressableAssets.Settings;
AddressableAssetSettings.BuildPlayerContent();
```

## UI Layout Refresh
Programmatic text changes require a layout rebuild to update dimensions:

```csharp
UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTransform);
```

## Robust TMP_FontAsset Repair
If fonts appear as "tofu" or throw `UnassignedReferenceException`:
- Check that the Material and Atlas Texture are nested under the Font Asset in the Project window.
- Re-assign `fontAsset.material.mainTexture = fontAsset.atlasTexture;` and re-save.
- If the font was deleted and recreated, update the GUID in both the Asset Table and the Addressables Group.
