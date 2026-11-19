## Contents
- [Workflow steps](#workflow) — Pre-Flight, Create Importer, Configure Importer, Import
- [Importer utility functions](#importer-utility-functions) — `EnsureImporterSaveFolder`, `GetImporterTypes`, `GetImporterProperties`, `CreateImporter`, `CanImportFile`, `ImportFile`

---

## Workflow

### Step 1: Pre-Flight
1. Check whether an Importer asset already exists for this file. If it does, skip to Step 3, because creating a second Importer for the same file duplicates the imported model.
2. Check the file to import exists.
3. Call `ATTAssistantUtilities.CanImportFile()` to check Asset Transformer Toolkit supports the file type. If it returns `false`, tell the user the format isn't supported and stop.
4. Identify the kind of file, such as point cloud or CAD, and pick the best-matching type from `ATTAssistantUtilities.GetImporterTypes()`.

### Step 2: Create Importer
1. Call `ATTAssistantUtilities.CreateImporter()` with the absolute system path to the file, the type from Step 1, and the folder from `ATTAssistantUtilities.EnsureImporterSaveFolder()`.
2. If it fails, fix the problem and try again. Stop after three attempts, because repeated failures usually mean the approach is wrong, and more attempts waste the user's time.
3. If you can't create the Importer, tell the user why and stop, because every later step needs it.

### Step 3: Configure Importer
1. If the user asked to change Importer settings, change them through `SerializedObject`, using the paths from `ATTAssistantUtilities.GetImporterProperties()`.
2. If the user asked for a RuleSet on the Importer, assign it.
3. If the user asked for LOD changes, follow the Levels of Detail workflow.
4. Stop here unless the user asked you to import the file, because an import can take a very long time.

### Step 4: Import
1. Start the asynchronous import with `ATTAssistantUtilities.ImportFile()`.
2. Report whether the import started, and remind the user it runs in the background.

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
- The toolkit always saves the imported model in the same folder as its ImporterScriptableObject.
- Users can add their own Importer types. Check which Importer types exist in the project with `ATTAssistantUtilities.GetImporterTypes()` instead of assuming, for example, that a point cloud file always maps to `PointCloudImporterScriptableObject`.
- Run only one import at a time, because imports run in the background and you can't tell which result belongs to which import while several are running.
- When creating a new ImporterScriptableObject, save it under the folder returned by `ATTAssistantUtilities.EnsureImporterSaveFolder()`. This reflects the user-configured save folder from Project Settings and defaults to `"Assets/3DModels"`.
