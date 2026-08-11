Choose a path depending on the task.

### Path A: CADImporterScriptableObject
Choose this path if working with an object that is or inherits from CADImporterScriptableObject.
- Identify the types of LODs available in the project. Choose the one(s) most appropriate for your case.
- Find the LODGenerator property in the Importer.
- Execute the requested task.
- Verify the number of rules present in the Generator does not exceed 7.

### Path B: Scene Object
Choose this path when asked to work with a scene asset directly.
- Add a UnityEngine.PixyzPlugin4Unity.Components.LODGeneratorComponent script to the GameObject if one doesn't exist anywhere in the hierarchy.
- Execute the given task.

### Path C: PointCloudImporter
Choose this path when working with a PointCloudImporterScriptableObject object.
- The PointCloudImporter only permits enabling/disabling LOD generation and setting the number of LODs to be generated. Other actions like choosing the type of LODRule to apply is unsupported.
- Models imported by the PointCloudImporter will not have a LODGeneratorComponent script attached to them.
- If the task is permitted, execute it.

### Path D: Other Importer
Choose this path if working with a different importer type.
- This is a user defined importer and not part of the base Asset Transformer Toolkit package. Precise instructions cannot be provided.
- Attempt to perform the requested task, but do not exceed three iterations.
- If the task cannot be performed successfully, inform the user working with this object is not currently supported by Assistant.

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
- The user will need to refresh the Inspector to see changes applied by Assistant.
- In an Importer's settings for LOD generation, the number of LODs set to generate does not include LOD0. There will always be one more LOD than the setting says. For example, setting the PointCloudImporter's NumberofLODs setting to 1 will result in the model having two LODs: LOD0 and LOD1.
- While Unity supports up to 8 LODs including LOD0, the PointCloudImporter is a special case that only supports 7.
- With the exception of models imported by the PointCloudImporter, models that were imported with LODs will have a LODGeneratorComponent script.
- LODs have nothing to do with RuleSets and Actions. RuleSets and Actions will NOT help with any LOD-related task.
