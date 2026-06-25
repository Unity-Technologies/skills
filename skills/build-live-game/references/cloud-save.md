# Cloud Save Reference

## Table of Contents

- [Anti-Hallucination Reference](#anti-hallucination-reference)
- [Subsystems](#subsystems)
- [IPlayerDataService Methods](#iplayerdataservice-methods)
- [ICustomDataService Methods](#icustomdataservice-methods)
- [IPlayerFilesService Methods](#iplayerfilesservice-methods)
- [Key Model Types](#key-model-types)
- [Access Classes](#access-classes)
- [Code Templates](#code-templates)
- [Error Handling](#error-handling)

Accessed via `CloudSaveService.Instance` (`ICloudSaveService`, namespace `Unity.Services.CloudSave`). Assembly: `Unity.Services.CloudSave`.

Call `UnityServices.InitializeAsync()` from `com.unity.services.core` and sign in via `com.unity.services.authentication` before use.

---

## Anti-Hallucination Reference

| Correct | Incorrect (do NOT use) |
|---|---|
| `SaveAsync` returns `Task<Dictionary<string, string>>` (write lock tokens) | `SaveAsync` returns `Task` (void) |
| `Item.Value` is `IDeserializable` -- call `.GetAs<T>()` | `Item.Value` is `object` |
| `SaveAsync(IDictionary<string, object>)` -- uses `IDictionary` | `SaveAsync(Dictionary<string, object>)` -- wrong interface type |
| `SaveAsync(IDictionary<string, SaveItem>)` -- bundles value + write lock | `SaveAsync(data, Dictionary<string, WriteLockOptions>)` -- no such overload |
| Custom data is **read-only** from client (no save/delete methods) | Custom data can be written from client |
| `CloudSaveConflictException` has `Details` list | Only `CloudSaveException` exists for conflicts |
| `CloudSaveExceptionReason.Conflict` (value 11) | `WriteLockConflict` reason |
| `Item.Modified` / `Item.Created` are `DateTime?` | `Item.Modified` is `ModifiedMetadata` |
| `FileItem.Modified` / `FileItem.Created` are `DateTime?` | `FileItem.Modified` is `ModifiedMetadata` |
| `DeleteAsync(key, Models.Data.Player.DeleteOptions)` -- current API | `DeleteAsync(key, CloudSave.DeleteOptions)` -- deprecated overload |
| `ICustomDataService` methods require `customDataID` first param | Custom data methods with no ID param |

---

## Subsystems

| Subsystem | Access | Description |
|---|---|---|
| `Data.Player` | `CloudSaveService.Instance.Data.Player` | Key-value data for the signed-in player |
| `Data.Custom` | `CloudSaveService.Instance.Data.Custom` | Game-wide or server-written data; **read-only from client** |
| `Files.Player` | `CloudSaveService.Instance.Files.Player` | Binary file storage per player |

---

## IPlayerDataService Methods

`CloudSaveService.Instance.Data.Player`

**Options namespace:** All option classes (`SaveOptions`, `LoadOptions`, `DeleteOptions`, `DeleteAllOptions`, `ListAllKeysOptions`, `LoadAllOptions`, `QueryOptions`) are in `Unity.Services.CloudSave.Models.Data.Player`. Do **not** confuse with the deprecated root-level `CloudSave.SaveOptions` / `CloudSave.DeleteOptions`.

```csharp
// List all keys belonging to the current player (with metadata).
Task<List<ItemKey>> ListAllKeysAsync()
Task<List<ItemKey>> ListAllKeysAsync(ListAllKeysOptions options)

// Load specific keys. Returns dictionary of key -> Item.
Task<Dictionary<string, Item>> LoadAsync(ISet<string> keys)
Task<Dictionary<string, Item>> LoadAsync(ISet<string> keys, LoadOptions options)

// Load all keys for the current player.
Task<Dictionary<string, Item>> LoadAllAsync()
Task<Dictionary<string, Item>> LoadAllAsync(LoadAllOptions options)

// Save key-value pairs. Returns Dictionary<string, string> mapping each key to its new write-lock token.
Task<Dictionary<string, string>> SaveAsync(IDictionary<string, object> data)
Task<Dictionary<string, string>> SaveAsync(IDictionary<string, object> data, SaveOptions options)

// Save with per-key write locks bundled via SaveItem.
Task<Dictionary<string, string>> SaveAsync(IDictionary<string, SaveItem> data)
Task<Dictionary<string, string>> SaveAsync(IDictionary<string, SaveItem> data, SaveOptions options)

// Delete a specific key. Use Models.Data.Player.DeleteOptions (not the deprecated root DeleteOptions).
Task DeleteAsync(string key, Models.Data.Player.DeleteOptions options)

// Delete ALL keys for the current player.
Task DeleteAllAsync()
Task DeleteAllAsync(DeleteAllOptions options)

// Query player data with field filters.
Task<List<EntityData>> QueryAsync(Query query, QueryOptions options)
```

---

## ICustomDataService Methods

`CloudSaveService.Instance.Data.Custom`

Read-only from the client. Write via Cloud Code modules or `IAdminClient.CloudSaveData` from the `com.unity.services.apis` package. All methods require a `customDataID` parameter -- the namespace configured in the Unity Dashboard.

> **To write Custom Data from the editor or a deploy command**, use `IAdminClient.CloudSaveData.SetCustomItem` /
> `SetCustomItemBatch`. See [apis.md](apis.md) for setup and code templates. The client SDK has no write
> path for Custom Data.

```csharp
Task<List<ItemKey>> ListAllKeysAsync(string customDataID)
Task<Dictionary<string, Item>> LoadAllAsync(string customDataID)
Task<Dictionary<string, Item>> LoadAsync(string customDataID, ISet<string> keys)

// Query across custom data. Uses Models.Data.Custom.QueryOptions.
Task<List<EntityData>> QueryAsync(Query query, Models.Data.Custom.QueryOptions options = default)
```

---

## IPlayerFilesService Methods

`CloudSaveService.Instance.Files.Player`

Note: File methods use root-level `CloudSave.SaveOptions` and `CloudSave.DeleteOptions` (not the `Models.Data.Player` versions).

```csharp
// List all files for the current player.
Task<List<FileItem>> ListAllAsync()

// Save a file (byte array or stream).
Task SaveAsync(string key, byte[] bytes, SaveOptions options = default)
Task SaveAsync(string key, Stream stream, SaveOptions options = default)

// Load a file as a byte array.
Task<byte[]> LoadBytesAsync(string key)

// Load a file as a stream.
Task<Stream> LoadStreamAsync(string key)

// Delete a file.
Task DeleteAsync(string key, DeleteOptions options = default)

// Get metadata for a specific file.
Task<FileItem> GetMetadataAsync(string key)
```

---

## Key Model Types

### `Item` (`Unity.Services.CloudSave.Models`)

| Property | Type | Description |
|---|---|---|
| `Key` | `string` | The data key |
| `Value` | `IDeserializable` | Deserialized value -- call `.GetAs<T>()` or `.GetAsString()` |
| `WriteLock` | `string` | Current write-lock token (use for optimistic concurrency) |
| `Created` | `DateTime?` | Creation timestamp |
| `Modified` | `DateTime?` | Last-modified timestamp |

### `ItemKey` (`Unity.Services.CloudSave.Models`)

| Property | Type | Description |
|---|---|---|
| `Key` | `string` | The data key |
| `WriteLock` | `string` | Current write-lock token |
| `Modified` | `DateTime?` | Last-modified timestamp |

### `FileItem` (`Unity.Services.CloudSave.Models`)

| Property | Type | Description |
|---|---|---|
| `Key` | `string` | The file key |
| `Size` | `long` | File size in bytes |
| `WriteLock` | `string` | Current write-lock token |
| `ContentType` | `string` | MIME type of the file |
| `Created` | `DateTime?` | Creation timestamp |
| `Modified` | `DateTime?` | Last-modified timestamp |

### `SaveItem` (`Unity.Services.CloudSave.Models`)

Bundles a value and write lock for atomic save-with-lock operations.

```csharp
new SaveItem(value: myObject, writeLock: previousItem.WriteLock)
```

### `Query` (`Unity.Services.CloudSave.Models`)

| Property | Type | Description |
|---|---|---|
| `Fields` | `List<FieldFilter>` | Filter conditions (required) |
| `ReturnKeys` | `HashSet<string>` | Project only these keys in results |
| `Offset` | `int` | Skip N results (pagination) |
| `Limit` | `int` | Max results to return |
| `SampleSize` | `int?` | Random sample size (optional) |

### `FieldFilter` (`Unity.Services.CloudSave.Models`)

```csharp
new FieldFilter(key: "level", value: 10, op: FieldFilter.OpOptions.GE, asc: true)
```

| `OpOptions` | Meaning |
|---|---|
| `EQ` | Equal |
| `NE` | Not equal |
| `LT` | Less than |
| `LE` | Less than or equal |
| `GT` | Greater than |
| `GE` | Greater than or equal |

### `EntityData` (`Unity.Services.CloudSave.Models`)

Query results return `List<EntityData>`, where each entry is one player's matching data.

| Property | Type | Description |
|---|---|---|
| `Id` | `string` | Player ID |
| `Data` | `List<Item>` | Matching items for this player |

---

## Access Classes

Namespace: `Unity.Services.CloudSave.Models.Data.Player`.

### `AccessClass` Enum

| Value | Meaning |
|---|---|
| `Default` (0) | Owner read/write -- private to the player |
| `Private` (1) | Alias for Default |
| `Protected` (2) | Owner read, server-only write |
| `Public` (3) | Any player can read, owner writes |

### Read Access Class Options (for Load/ListAllKeys)

- `DefaultReadAccessClassOptions()` -- read own Default-class data
- `PublicReadAccessClassOptions()` -- read own Public-class data
- `PublicReadAccessClassOptions(string playerId)` -- read **another player's** Public-class data
- `ProtectedReadAccessClassOptions()` -- read own Protected-class data

### Write Access Class Options (for Save/Delete)

- `DefaultWriteAccessClassOptions()` -- write to Default-class keys
- `PublicWriteAccessClassOptions()` -- write to Public-class keys

---

## Code Templates

### Save Player Data (Capture Write Lock Tokens)

```csharp
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using System.Collections.Generic;

var data = new Dictionary<string, object>
{
    { "level", 10 },
    { "gold", 500 },
    { "inventory", new string[] { "sword", "shield" } }
};

// SaveAsync returns write-lock tokens for each saved key
Dictionary<string, string> writeLocks = await CloudSaveService.Instance.Data.Player.SaveAsync(data);
// writeLocks["level"] == "new-lock-token-for-level", etc.
```

### Load Specific Keys

```csharp
var keys = new HashSet<string> { "level", "gold" };
var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

if (result.TryGetValue("level", out var levelItem))
{
    int level = levelItem.Value.GetAs<int>();
    Debug.Log($"Level: {level}, Modified: {levelItem.Modified}");
}
```

### Save with Write Lock Using SaveItem

```csharp
using Unity.Services.CloudSave.Models;

// First load to get the current write lock
var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "gold" });
var goldItem = items["gold"];

// Bundle value + write lock in SaveItem -- fails if another client updated in the meantime
var saveData = new Dictionary<string, SaveItem>
{
    { "gold", new SaveItem(value: 600, writeLock: goldItem.WriteLock) }
};

Dictionary<string, string> newLocks = await CloudSaveService.Instance.Data.Player.SaveAsync(saveData);
// newLocks["gold"] is the updated write-lock token
```

### Handle Write-Lock Conflicts

```csharp
using Unity.Services.CloudSave;

try
{
    await CloudSaveService.Instance.Data.Player.SaveAsync(saveData);
}
catch (CloudSaveConflictException ex)
{
    foreach (var detail in ex.Details)
    {
        Debug.LogError($"Conflict on '{detail.Key}': " +
            $"you sent lock '{detail.AttemptedWriteLock}', " +
            $"server has '{detail.ExistingWriteLock}'");
    }
    // Reload, merge, and retry
}
```

### Delete a Key

```csharp
using Unity.Services.CloudSave.Models.Data.Player;

// Delete with write-lock check
var options = new DeleteOptions { WriteLock = knownWriteLock };
await CloudSaveService.Instance.Data.Player.DeleteAsync("gold", options);

// Delete ALL player data
await CloudSaveService.Instance.Data.Player.DeleteAllAsync();
```

### Save and Load with Access Classes (Public, Default, Protected)

> **Namespace note:** `SaveOptions`, `LoadOptions`, and `DeleteOptions` exist in both
> `Unity.Services.CloudSave` (root) and `Unity.Services.CloudSave.Models.Data.Player`.
> The access-class overloads live in `Models.Data.Player`. If you import both namespaces,
> use fully qualified names or a `using` alias to avoid ambiguity.

```csharp
using Unity.Services.CloudSave.Models.Data.Player;

// Save data as Public (other players can read it)
var publicData = new Dictionary<string, object> { { "displayName", "Hero123" }, { "rank", 42 } };
var publicOptions = new SaveOptions(new PublicWriteAccessClassOptions());
await CloudSaveService.Instance.Data.Player.SaveAsync(publicData, publicOptions);

// Read another player's Public-class data
var readOptions = new LoadOptions(new PublicReadAccessClassOptions(otherPlayerId));
var otherPlayerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
    new HashSet<string> { "displayName", "rank" }, readOptions);
Debug.Log($"Other player name: {otherPlayerData["displayName"].Value.GetAs<string>()}");

// Read own Protected-class data (written by server/Cloud Code)
var protectedOptions = new LoadOptions(new ProtectedReadAccessClassOptions());
var serverData = await CloudSaveService.Instance.Data.Player.LoadAllAsync(
    new LoadAllOptions(new ProtectedReadAccessClassOptions()));
```

### Read Custom Data (Game-Wide, Read-Only)

```csharp
// Custom data is read-only from client -- written via Cloud Code or admin API.
// customDataID is the namespace configured in the Unity Dashboard.
var customData = await CloudSaveService.Instance.Data.Custom.LoadAllAsync("my-game-config");

if (customData.TryGetValue("seasonConfig", out var config))
{
    var season = config.Value.GetAs<SeasonConfig>();
    Debug.Log($"Current season: {season.Name}");
}
```

### Query Player Data

```csharp
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;

// Find players with level >= 10, sorted ascending
var query = new Query(
    fields: new List<FieldFilter>
    {
        new FieldFilter(key: "level", value: 10, op: FieldFilter.OpOptions.GE, asc: true)
    },
    returnKeys: new HashSet<string> { "level", "displayName" },
    offset: 0,
    limit: 20
);

var results = await CloudSaveService.Instance.Data.Player.QueryAsync(query, new QueryOptions());

foreach (var entity in results)
{
    Debug.Log($"Player {entity.Id}:");
    foreach (var item in entity.Data)
        Debug.Log($"  {item.Key} = {item.Value.GetAsString()}");
}
```

### Save and Load a File

```csharp
// Save
byte[] screenshotBytes = await CaptureScreenshot();
await CloudSaveService.Instance.Files.Player.SaveAsync("screenshot_latest", screenshotBytes);

// Load
byte[] loaded = await CloudSaveService.Instance.Files.Player.LoadBytesAsync("screenshot_latest");

// Get metadata
var meta = await CloudSaveService.Instance.Files.Player.GetMetadataAsync("screenshot_latest");
Debug.Log($"Size: {meta.Size}, ContentType: {meta.ContentType}, Modified: {meta.Modified}");
```

### List All Keys

```csharp
var keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();
foreach (var key in keys)
{
    Debug.Log($"Key: {key.Key}, WriteLock: {key.WriteLock}, Modified: {key.Modified}");
}
```

---

## Error Handling

```csharp
try { ... }
catch (CloudSaveConflictException ex)
{
    // Write-lock conflict -- inspect per-key details
    foreach (var detail in ex.Details)
        Debug.LogError($"Key '{detail.Key}': attempted={detail.AttemptedWriteLock}, existing={detail.ExistingWriteLock}");
}
catch (CloudSaveValidationException ex)
{
    // Input validation failure -- inspect per-field details
    foreach (var detail in ex.Details)
        Debug.LogError($"Field '{detail.Field}' key '{detail.Key}': {string.Join(", ", detail.Messages)}");
}
catch (CloudSaveRateLimitedException ex)
{
    Debug.LogError($"Rate limited. Retry after {ex.RetryAfter}s");
}
catch (CloudSaveException ex)
{
    Debug.LogError($"Cloud Save error: {ex.Message} (reason: {ex.Reason})");
}
```

### `CloudSaveExceptionReason` Enum

| Reason | Value | Meaning |
|---|---|---|
| `Unknown` | 0 | Unknown error |
| `NoInternetConnection` | 1 | No network |
| `ProjectIdMissing` | 2 | Project ID not set |
| `PlayerIdMissing` | 3 | Player not signed in |
| `AccessTokenMissing` | 4 | No access token |
| `InvalidArgument` | 5 | Bad input |
| `Unauthorized` | 6 | Not authorized |
| `KeyLimitExceeded` | 7 | Too many keys stored |
| `NotFound` | 8 | Key not found |
| `TooManyRequests` | 9 | Rate limited |
| `ServiceUnavailable` | 10 | Service down |
| `Conflict` | 11 | Write-lock conflict |

---

## Asset Store Building Blocks

The following Building Blocks from the Unity Asset Store demonstrate Cloud Save patterns:

- **Achievements Building Block** — Reads/writes achievement records in Protected buckets via Cloud Code, with Access Control denying direct player writes. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-achievements-341918)
- **Player Account Building Block** — Stores player profile data in Default and Public access classes with direct client writes for non-sensitive data. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-player-account-341928)
- **Leaderboards Building Block** — Uses Cloud Save for player score data alongside Cloud Code modules. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-leaderboards-341926)
