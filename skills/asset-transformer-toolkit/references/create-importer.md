## Contents
- [Workflow steps](#workflow) — Pre-Flight, Create Importer, Configure Importer, Import
- [Importer utility functions](#importer-utility-functions) — `EnsureImporterSaveFolder`, `GetImporterTypes`, `GetImporterProperties`, `CreateImporter`, `CanImportFile`, `ImportFile`

---

If a new Importer does not need to be created, skip to Step 3

### Step 1: Pre-Flight
- Check whether an importer asset already exists for this file. If it does, skip to Step 3.
- Verify the file to be imported exists.
- Verify the file type is supported by Pixyz/Asset Transformer Toolkit using `ATTAssistantUtilities.CanImportFile()`.
- Check what kind of file (eg. point cloud, CAD) is it. Find the Importer type that would best match it.

### Step 2: Create Importer
- Create the appropriate Importer asset with the path to the file to be imported. The path must be relative to the Application.dataPath.
- If there are issues, fix and revalidate. Do not exceed 3 iterations.
Do not continue if this step cannot be completed successfully.

### Step 3: Configure Importer
- If requested, change the importer's settings.
- If requested, assign the requested RuleSet to the Importer.
- If requested, make changes to the LOD generation.
Continue only if asked to also import the file. The import process can be very long, so NEVER assume you must import the file unless it was requested.

### Step 4: Import
- Start the asynchronous import using `ATTAssistantUtilities.ImportFile()`.
- Report whether the import process started successfully. Remind the user this is a background process.

## Importer utility functions

The following functions are from `Unity.Pixyz.Plugin4Unity.Editor.AI.ATTAssistantUtilities`.

### `EnsureImporterSaveFolder`

Returns the project-relative asset save folder configured in Asset Transformer Toolkit Project Settings, creating it if it does not already exist. Use this as the destination path when creating a new `ImporterScriptableObject` asset.

```csharp
public static string EnsureImporterSaveFolder()
```

Returns a `string` such as `"Assets/3DModels"`.

### `GetImporterTypes`

Returns the names of all `ImporterScriptableObject` types available in the project, including user-defined importers. Always call this before creating or referencing an importer type — never assume a type exists.

```csharp
public static string[] GetImporterTypes()
```

Returns a `string[]` of unqualified type names (e.g. `"CADImporterScriptableObject"`).

### `GetImporterProperties`

Returns the public serialized fields of an `ImporterScriptableObject` type by name, including fields from intermediate base classes. Use this to discover what settings are available on any importer type — concrete importer classes may be internal or user-defined.

```csharp
public static ImporterPropertyInfo[] GetImporterProperties(string typeName)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `typeName` | string | required | Unqualified type name as returned by `GetImporterTypes()`. |

Returns an `ImporterPropertyInfo[]`. Each entry has the following fields:

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Human-readable property name. |
| `Type` | string | Unqualified type name of the field. |
| `SerializedPropertyPath` | string | The exact string to pass to `SerializedObject.FindProperty`. For auto-properties declared with `[field: SerializeField]`, this differs from `Name` (e.g. `<MyProperty>k__BackingField`). Always use this field — never construct the path from `Name` yourself. |

### `CreateImporter`

Creates a new `ImporterScriptableObject` asset for the given file. Call `GetImporterTypes()` first to confirm the type name — never assume or invent one. Use `EnsureImporterSaveFolder()` to get the correct save path.

```csharp
public static string CreateImporter(string filePath, string typeName, string savePath)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `filePath` | string | required | Absolute system path to the 3D file the importer will reference. |
| `typeName` | string | required | Unqualified importer type name as returned by `GetImporterTypes()`. |
| `savePath` | string | required | Project-relative folder path where the importer asset will be saved. |

Returns a `string` with the project-relative path to the created asset (e.g. `"Assets/3DModels/MyModel.asset"`). Pass this path to `ImportFile` to trigger import.

### `CanImportFile`

Checks whether the Asset Transformer Toolkit supports a file format. Call this before `ImportFile` to avoid runtime errors. Throws if no file exists at `filePath`.

```csharp
public static bool CanImportFile(string filePath)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `filePath` | string | required | Absolute system path to the 3D file to check. |

Returns `true` if the file format is supported, `false` otherwise.

### `ImportFile`

Triggers import or re-import using an existing `ImporterScriptableObject` asset. Requires an `ImporterScriptableObject` to already exist at `importerPath`.

```csharp
public static void ImportFile(string importerPath)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `importerPath` | string | required | Project-relative path to the `ImporterScriptableObject` asset. |

### Technical Notes
- Because import is an asynchronous process, the imported files may not yet exist in the project by the time the prompt finishes execution.
- The imported model will always be saved to the same directory that the ImporterScriptableObject lives in.
- Importers are designed to be extended. Check which Importer types exist in the project using `ATTAssistantUtilities.GetImporterTypes()` instead of making assumptions like a point cloud file always maps to the PointCloudImporterScriptableObject.
- Only one import should be processing at a time.
- When creating a new ImporterScriptableObject, save it under the folder returned by `ATTAssistantUtilities.EnsureImporterSaveFolder()`. This reflects the user-configured save folder from Project Settings and defaults to `"Assets/3DModels"`.
