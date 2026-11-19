## Contents
- [Paths](#paths) — CADImporterScriptableObject, Scene Object, PointCloudImporter, Other Importer
- [LOD utility functions](#lod-utility-functions) — `GetLODRules`, `LODRuleDescription`, `LODParameterInfo`
- [Technical Notes](#technical-notes)

---

## Paths

Choose the path that matches the object you're working with.

### Path A: CADImporterScriptableObject
Choose this path if working with an object that is or inherits from CADImporterScriptableObject.
1. Call `GetLODRules` to list the LOD rule types in the project, and pick the ones that fit the request.
2. Find the LODGenerator property in the Importer.
3. Make the requested change.
4. Check the Generator has no more than 7 rules, because each rule adds one LOD after LOD0 and Unity supports at most 8 LODs including LOD0.

### Path B: Scene Object
Choose this path when the user asks you to work with a scene GameObject directly.
1. If no `UnityEngine.PixyzPlugin4Unity.Components.LODGeneratorComponent` exists anywhere in the GameObject's hierarchy, add one to the GameObject.
2. Call `GetLODRules` to list the LOD rule types in the project.
3. Make the requested change.

### Path C: PointCloudImporter
Choose this path when working with a PointCloudImporterScriptableObject object.
1. Check the request only enables or disables LOD generation or sets the number of LODs, because the PointCloudImporter supports nothing else, such as choosing a LODRule type. If it asks for anything else, tell the user the PointCloudImporter doesn't support it and stop.
2. Make the requested change on the Importer, not on a LODGeneratorComponent, because point cloud models don't get one.

### Path D: Other Importer
Choose this path if working with a different importer type.
This is a user-defined Importer, not part of Asset Transformer Toolkit, so this skill has no exact steps for it.
1. Try the requested change, up to three attempts, because without instructions for this Importer, repeated failures mean you're guessing.
2. If it still fails, tell the user you can't change LODs on this Importer type and they need to change them by hand.

## LOD utility functions

The following functions are from `Unity.Pixyz.Plugin4Unity.Editor.AI.ATTAssistantUtilities`.

### `GetLODRules`

Returns all available `LODRule` implementations and their parameters. Use this before constructing or modifying a LOD configuration to discover valid types.

```csharp
public static LODRuleDescription[] GetLODRules()
```

Returns a `LODRuleDescription[]`, each containing the type name, assembly-qualified name, and parameter list.

### `LODRuleDescription`

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Unqualified class name of the LODRule type. |
| `QualifiedName` | string | Assembly-qualified name, suitable for `Type.GetType`. |
| `Parameters` | `LODParameterInfo[]` | Configurable properties on this LODRule type. |

### `LODParameterInfo`

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Property name. |
| `Type` | string | Property type name. |

### Technical Notes
- The user needs to refresh the Inspector to see your changes.
- In an Importer's settings for LOD generation, the number of LODs set to generate does not include LOD0. There will always be one more LOD than the setting says. For example, setting the PointCloudImporter's NumberofLODs setting to 1 will result in the model having two LODs: LOD0 and LOD1.
- While Unity supports up to 8 LODs including LOD0, the PointCloudImporter is a special case that only supports 7.
- With the exception of models imported by the PointCloudImporter, models that were imported with LODs will have a LODGeneratorComponent script.
- LODs are separate from RuleSets and Actions. Don't use RuleSets or Actions for any LOD task, because they can't change LOD settings.
