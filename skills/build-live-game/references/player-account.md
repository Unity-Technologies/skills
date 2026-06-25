# Player Account Reference

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Access Classes](#access-classes)
- [Assembly References](#assembly-references)
- [Package Setup](#package-setup)
- [Sign-In: Anonymous](#sign-in-anonymous)
- [Sign-In: Unity (Browser)](#sign-in-unity-browser)
  - [PlayerAccountService API](#playeraccountservice-api)
- [Sign-In: Username/Password](#sign-in-usernamepassword)
  - [Username/Password Constraints](#usernamepassword-constraints)
- [Player Identity](#player-identity)
- [Cloud Save Operations](#cloud-save-operations)
  - [Namespace Aliases](#namespace-aliases)
  - [Save Default Data](#save-default-data-private-to-player)
  - [Save Public Data](#save-public-data-visible-to-other-players)
  - [Load Default Data](#load-default-data)
  - [Load Public Data](#load-public-data-own)
  - [Load Public Data (Other Player)](#load-public-data-another-player)
  - [Load All Keys](#load-all-keys)
- [Server-Authoritative Writes via Cloud Code (Optional)](#server-authoritative-writes-via-cloud-code-optional)
  - [Reading Protected Data](#reading-protected-data-client-side)
  - [Writing Protected/Custom Data](#writing-protectedcustom-data-server-side)
- [Full Client Implementation](#full-client-implementation)
- [PlayerAccountTester](#playeraccounttester)
- [Assembly Definition](#assembly-definition)
- [Error Handling](#error-handling)
  - [AuthenticationException](#authenticationexception)
  - [PlayerAccountsException](#playeraccountsexception)
  - [CloudSaveException](#cloudsaveexception)
- [Validation](#validation)
- [Asset Store Building Block](#asset-store-building-block)

---

## Architecture Overview

| Concern | Service | API |
|---|---|---|
| Sign in (anonymous) | Authentication | `SignInAnonymouslyAsync()` |
| Sign in (Unity browser) | PlayerAccounts then Authentication | `StartSignInAsync()` then `SignInWithUnityAsync(AccessToken)` |
| Sign in (username/password) | Authentication | `SignUpWithUsernamePasswordAsync` / `SignInWithUsernamePasswordAsync` |
| Player identity | Authentication | `PlayerId`, `PlayerName`, `UpdatePlayerNameAsync` |
| Per-player data | Cloud Save | `CloudSaveService.Instance.Data.Player` |

```
UnityServices.InitializeAsync()
            |
            v
  AuthenticationService.Instance
            |
   +--------+-----------+
   |        |            |
   v        v            v
  Anon   Unity/PA    Password
   |     (browser)      |
   |        |            |
   |   PlayerAccountService.Instance
   |   .StartSignInAsync()
   |        |
   |   SignedIn event fires
   |        |
   |   SignInWithUnityAsync(AccessToken)
   |        |            |
   +--------+-----------+
            |
            v
   PlayerId / PlayerName
            |
            v
   CloudSaveService.Instance.Data.Player
      |          |            |
      v          v            v
   Default    Public     Protected
  (owner     (anyone    (owner read,
  read+write) read,     server-only
              owner      write)
              write)
```

---

## Access Classes

Cloud Save player data supports three access classes. The access class used when saving determines which access class must be used when loading.

| Access Class | Read | Write | Use Case |
|---|---|---|---|
| Default | Owner only | Owner only | Private settings, preferences, game progress |
| Public | Any player | Owner only | Display names, public profiles, shared stats |
| Protected | Owner only | Server only (Cloud Code) | Anti-cheat data, server-awarded state |

---

## Assembly References

Your `.asmdef` must reference all four assemblies. `Unity.Services.Authentication.PlayerAccounts` is a **separate assembly** from `Unity.Services.Authentication`, even though both ship inside the `com.unity.services.authentication` package.

| Assembly Name | Package | Purpose |
|---|---|---|
| `Unity.Services.Core` | `com.unity.services.core` | `UnityServices.InitializeAsync()` |
| `Unity.Services.Authentication` | `com.unity.services.authentication` | `AuthenticationService.Instance`, sign-in methods, player identity |
| `Unity.Services.Authentication.PlayerAccounts` | `com.unity.services.authentication` (separate assembly in same package) | `PlayerAccountService.Instance`, browser-based Unity sign-in |
| `Unity.Services.CloudSave` | `com.unity.services.cloudsave` | `CloudSaveService.Instance.Data.Player`, save/load operations |

---

## Package Setup

Ensure the following packages are listed in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.services.authentication": "3.6.1",
    "com.unity.services.cloudsave": "3.4.0",
    "com.unity.services.core": "1.16.0"
  }
}
```

**Optional** — add `com.unity.services.cloudcode` if using Protected player data or Custom (shared) data:

```json
    "com.unity.services.cloudcode": "2.10.3"
```

---

## Sign-In: Anonymous

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

async Task SignInAnonymouslyAsync()
{
    await UnityServices.InitializeAsync();

    if (!AuthenticationService.Instance.IsSignedIn)
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
}
```

`SignInAnonymouslyAsync` handles both new and returning players. If a session token is cached from a previous session, it signs in silently with the cached token. If no token exists, it creates a new anonymous account. Always check `IsSignedIn` before calling to avoid double sign-in errors.

---

## Sign-In: Unity (Browser)

Uses `PlayerAccountService` to open a system browser for Unity account sign-in. After the user signs in via the browser, the `SignedIn` event fires and provides an `AccessToken` that is passed to `AuthenticationService.Instance.SignInWithUnityAsync`.

**Important:** The `Task` returned by `StartSignInAsync` completes when the browser **opens**, NOT when sign-in finishes. You must subscribe to the `SignedIn` event to know when authentication is complete.

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using UnityEngine;

void Start()
{
    // Wire the event BEFORE calling StartSignInAsync
    PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;
}

async Task StartSignInWithUnityAsync()
{
    await UnityServices.InitializeAsync();

    if (PlayerAccountService.Instance.IsSignedIn)
    {
        // Already signed in to Player Accounts -- go straight to Authentication
        OnPlayerAccountSignedIn();
        return;
    }

    // Opens the system browser. Task completes when browser opens, NOT when sign-in finishes.
    await PlayerAccountService.Instance.StartSignInAsync();
}

async void OnPlayerAccountSignedIn()
{
    try
    {
        await AuthenticationService.Instance.SignInWithUnityAsync(
            PlayerAccountService.Instance.AccessToken);
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
    }
    catch (AuthenticationException ex)
    {
        Debug.LogError($"Unity sign-in failed: {ex.Message}");
    }
    catch (RequestFailedException ex)
    {
        Debug.LogError($"Request failed: {ex.Message}");
    }
}
```

Namespace: `Unity.Services.Authentication.PlayerAccounts`. Access via `PlayerAccountService.Instance` (`IPlayerAccountService`).

If using any authentication other than anonymous, it requires the Unity identity provider to be enabled in **Project Settings > Services > Authentication > Identity Providers**.

### PlayerAccountService API

| Member | Type | Description |
|---|---|---|
| `StartSignInAsync(bool isSigningUp = false)` | `Task` | Opens browser for sign-in (or sign-up if `true`). Task completes when browser opens. |
| `SignOut()` | `void` | Signs out of Player Accounts and revokes the access token. Synchronous. |
| `RefreshTokenAsync()` | `Task` | Refreshes the current access token using the refresh token. |
| `AccessToken` | `string` | Access token obtained during sign-in. Pass this to `SignInWithUnityAsync`. |
| `IdToken` | `string` | ID token obtained during sign-in. |
| `IdTokenClaims` | `IdToken` | Parsed claims from the ID token (email, subject, etc.). |
| `IsSignedIn` | `bool` | Whether the player is signed in to Player Accounts. |
| `AccountPortalUrl` | `string` | URL to the Unity Player Account portal. |
| `SignedIn` | `event Action` | Fires when browser sign-in completes successfully. |
| `SignedOut` | `event Action` | Fires when the player signs out. |
| `SignInFailed` | `event Action<RequestFailedException>` | Fires when sign-in fails. |

---

## Sign-In: Username/Password

Two separate methods: `SignUpWithUsernamePasswordAsync` for first-time registration, `SignInWithUsernamePasswordAsync` for subsequent sign-ins.

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

async Task SignUpWithPasswordAsync(string username, string password)
{
    await UnityServices.InitializeAsync();

    try
    {
        await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        Debug.Log($"Signed up and signed in as {AuthenticationService.Instance.PlayerId}");
    }
    catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
    {
        // Error code 10003: username already exists. Sign in instead.
        Debug.LogWarning("Username already exists, signing in instead.");
        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
    }
    catch (AuthenticationException ex)
    {
        Debug.LogError($"Sign-up failed: {ex.Message} (code: {ex.ErrorCode})");
    }
}

async Task SignInWithPasswordAsync(string username, string password)
{
    await UnityServices.InitializeAsync();

    try
    {
        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
    }
    catch (AuthenticationException ex)
    {
        Debug.LogError($"Sign-in failed: {ex.Message} (code: {ex.ErrorCode})");
    }
}
```

Requires the username/password identity provider in **Project Settings > Services > Authentication > Identity Providers**.

### Username/Password Constraints

| Field | Constraint |
|---|---|
| Username length | 3-20 characters |
| Username characters | `A-Z`, `a-z`, `0-9`, `.`, `-`, `@`, `_` |
| Username case | Case insensitive (stored lowercase) |
| Password length | 8-30 characters |
| Password requirements | At least 1 lowercase + 1 uppercase + 1 number + 1 symbol |

---

## Player Identity

After any successful sign-in, the following properties and methods are available on `AuthenticationService.Instance`:

| Member | Type | Description |
|---|---|---|
| `PlayerId` | `string` | Unique player identifier, available immediately after sign-in |
| `PlayerName` | `string` | Player display name (may be null until fetched) |
| `IsSignedIn` | `bool` | True if a token exists in memory |
| `IsAnonymous` | `bool` | True if signed in anonymously |
| `SignedIn` | `event Action` | Fires on successful sign-in |
| `SignedOut` | `event Action` | Fires on sign-out |
| `Expired` | `event Action` | Fires when access token expires; SDK auto-attempts refresh |

```csharp
// Get player name. autoGenerate: true creates a random name if none exists.
string name = await AuthenticationService.Instance.GetPlayerNameAsync(autoGenerate: true);

// Update player name.
string updatedName = await AuthenticationService.Instance.UpdatePlayerNameAsync("NewDisplayName");

// Sign out. Synchronous (void), NOT async.
AuthenticationService.Instance.SignOut();
// Optionally clear cached credentials:
// AuthenticationService.Instance.SignOut(clearCredentials: true);
```

**Warning:** `SignOut` is synchronous (`void`). There is no `SignOutAsync` method.

---

## Cloud Save Operations

### Namespace Aliases

Use namespace aliases to avoid ambiguity between root-level and `Models.Data.Player` option classes:

```csharp
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerLoadOptions    = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerLoadAllOptions = Unity.Services.CloudSave.Models.Data.Player.LoadAllOptions;
using PlayerSaveOptions    = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
```

> **Namespace collision:** `SaveOptions` and `LoadOptions` exist in both `Unity.Services.CloudSave`
> and `Unity.Services.CloudSave.Models.Data.Player`. Always use the `using` aliases to avoid
> ambiguity errors.

### Save Default Data (Private to Player)

```csharp
var data = new Dictionary<string, object>
{
    { "settings_volume", 0.8f },
    { "settings_difficulty", "hard" },
    { "last_login", DateTime.UtcNow.ToString("o") }
};

await CloudSaveService.Instance.Data.Player.SaveAsync(data);
```

### Save Public Data (Visible to Other Players)

```csharp
var publicData = new Dictionary<string, object>
{
    { "display_name", "Hero123" },
    { "avatar_id", 42 }
};

var publicSaveOptions = new PlayerSaveOptions(new PublicWriteAccessClassOptions());
await CloudSaveService.Instance.Data.Player.SaveAsync(publicData, publicSaveOptions);
```

### Load Default Data

```csharp
var keys = new HashSet<string> { "settings_volume", "settings_difficulty" };
var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

if (result.TryGetValue("settings_volume", out var volumeItem))
{
    float volume = volumeItem.Value.GetAs<float>();
}
```

For string values saved via `Dictionary<string, object>`, `GetAsString()` returns the plain string. For complex objects, `GetAsString()` returns the JSON; use `item.Value.GetAs<T>()` to deserialize.

### Load Public Data (Own)

```csharp
var publicLoadOptions = new PlayerLoadOptions(new PublicReadAccessClassOptions());
var publicResult = await CloudSaveService.Instance.Data.Player.LoadAsync(
    new HashSet<string> { "display_name", "avatar_id" }, publicLoadOptions);
```

### Load Public Data (Another Player)

```csharp
var otherPlayerOptions = new PlayerLoadOptions(new PublicReadAccessClassOptions(otherPlayerId));
var otherResult = await CloudSaveService.Instance.Data.Player.LoadAsync(
    new HashSet<string> { "display_name", "avatar_id" }, otherPlayerOptions);
```

### Load All Keys

```csharp
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerLoadAllOptions = Unity.Services.CloudSave.Models.Data.Player.LoadAllOptions;

var allItems = await CloudSaveService.Instance.Data.Player.LoadAllAsync(
    new PlayerLoadAllOptions(new DefaultReadAccessClassOptions()));

foreach (var kv in allItems)
    Debug.Log($"{kv.Key}: {kv.Value.Value.GetAsString()}");
```

**Access class matching rule:** Data saved with `PublicWriteAccessClassOptions` must be loaded with `PublicReadAccessClassOptions`. Data saved with no options (Default) must be loaded with `DefaultReadAccessClassOptions` or no options. Mismatched access classes return empty results without error.

---

## Server-Authoritative Writes via Cloud Code (Optional)

The Default and Public access classes above are client-writable — suitable for preferences, display names, and non-sensitive data. Two scenarios require routing writes through a Cloud Code module instead:

- **Non-player shared data** (guild info, level configs, global state) — no single player owns this data, so it must be written via Cloud Code to the **Custom** bucket using `IGameApiClient`.
- **Tamper-proof player data** — if the game design requires anti-cheat protection for specific player state (e.g. XP, currency, unlocked items), write to the **Protected** bucket via Cloud Code. This is a game-design decision, not a requirement.

### Reading Protected Data (Client-Side)

```csharp
var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
    new HashSet<string> { "player_level", "currency" },
    new PlayerLoadOptions(new ProtectedReadAccessClassOptions()));

if (result.TryGetValue("player_level", out var item))
{
    int level = item.Value.GetAs<int>();
}
```

> **Common mistake:** Using default `LoadOptions` instead of `ProtectedReadAccessClassOptions`
> returns no data, because the keys were written to the Protected bucket by Cloud Code.

### Writing Protected/Custom Data (Server-Side)

Writes to Protected and Custom buckets must go through a Cloud Code module. The module uses `IGameApiClient` with the service token to call Cloud Save server-side APIs.

To create the module, follow [cloud-code.md — Module Creation](cloud-code.md#module-creation). All scaffolding files are required:

- [ ] `.sln` (generate fresh GUIDs)
- [ ] `.csproj` (net9.0, CloudCode.Apis + CloudCode.Core)
- [ ] `ModuleSetup.cs` (registers `GameApiClient`)
- [ ] `Properties/PublishProfiles/FolderProfile.pubxml` — **without this file, deployment fails with "Failed to retrieve main project"**
- [ ] `Assets/CloudCode/<ModuleName>.ccmr` (points to the `.sln`)

---

## Full Client Implementation

A complete `MonoBehaviour` covering all three sign-in methods, player identity, and Cloud Save operations.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using Unity.Services.Core;
using UnityEngine;
using PlayerLoadOptions    = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerLoadAllOptions = Unity.Services.CloudSave.Models.Data.Player.LoadAllOptions;
using PlayerSaveOptions    = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

public class PlayerAccountManager : MonoBehaviour
{
    public string PlayerId => AuthenticationService.Instance.PlayerId;
    public string PlayerName => AuthenticationService.Instance.PlayerName;
    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

    public event Action SignedIn;

    async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed in: {AuthenticationService.Instance.PlayerId}");
            SignedIn?.Invoke();
        };

        // Wire PlayerAccountService event BEFORE any StartSignInAsync call
        PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;
    }

    // --- Sign-In Methods ---

    public async Task SignInAnonymouslyAsync()
    {
        if (AuthenticationService.Instance.IsSignedIn) return;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /// <summary>
    /// Starts the browser-based Unity sign-in flow.
    /// The returned Task completes when the browser opens, NOT when sign-in finishes.
    /// Subscribe to SignedIn to know when authentication is complete.
    /// </summary>
    public async Task StartSignInWithUnity()
    {
        if (PlayerAccountService.Instance.IsSignedIn)
        {
            OnPlayerAccountSignedIn();
            return;
        }

        await PlayerAccountService.Instance.StartSignInAsync();
    }

    async void OnPlayerAccountSignedIn()
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken);
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"Unity sign-in failed: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Request failed: {ex.Message}");
        }
    }

    public async Task SignUpWithPasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
        }
        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
        {
            // Error code 10003: username already exists
            Debug.LogWarning("Username already exists. Use SignInWithPasswordAsync instead.");
            throw;
        }
    }

    public async Task SignInWithPasswordAsync(string username, string password)
    {
        await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        // Optionally also sign out of Player Accounts:
        // PlayerAccountService.Instance.SignOut();
    }

    // --- Player Identity ---

    public async Task<string> GetPlayerNameAsync()
    {
        return await AuthenticationService.Instance.GetPlayerNameAsync(autoGenerate: true);
    }

    public async Task<string> UpdatePlayerNameAsync(string newName)
    {
        return await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
    }

    // --- Cloud Save: Default (Private) ---

    public async Task SaveDefaultAsync(string key, object value)
    {
        await CloudSaveService.Instance.Data.Player.SaveAsync(
            new Dictionary<string, object> { { key, value } });
    }

    public async Task<string> LoadDefaultAsync(string key)
    {
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { key });
        return result.TryGetValue(key, out var item) ? item.Value.GetAsString() : null;
    }

    // --- Cloud Save: Public ---

    public async Task SavePublicAsync(string key, object value)
    {
        var options = new PlayerSaveOptions(new PublicWriteAccessClassOptions());
        await CloudSaveService.Instance.Data.Player.SaveAsync(
            new Dictionary<string, object> { { key, value } }, options);
    }

    public async Task<string> LoadPublicAsync(string key)
    {
        var options = new PlayerLoadOptions(new PublicReadAccessClassOptions());
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { key }, options);
        return result.TryGetValue(key, out var item) ? item.Value.GetAsString() : null;
    }

    // --- Cloud Save: All Keys ---

    public async Task<List<string>> LoadAllDefaultKeysAsync()
    {
        var result = await CloudSaveService.Instance.Data.Player.LoadAllAsync(
            new PlayerLoadAllOptions(new DefaultReadAccessClassOptions()));
        return new List<string>(result.Keys);
    }
}
```

---

## PlayerAccountTester

Attach alongside `PlayerAccountManager` on the same GameObject. Signs in anonymously, logs identity, then exercises a Cloud Save round-trip and lists all Default keys.

```csharp
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Attach alongside PlayerAccountManager to run a quick in-Editor smoke test.
/// On start: signs in anonymously and logs player identity.
/// After sign-in: saves a probe value to the Default bucket, loads it back,
/// and logs all Default keys visible for this player.
/// </summary>
public class PlayerAccountTester : MonoBehaviour
{
    [SerializeField] PlayerAccountManager m_Manager;

    async void Start()
    {
        if (m_Manager == null)
            m_Manager = GetComponent<PlayerAccountManager>();

        m_Manager.SignedIn += OnSignedIn;
        await m_Manager.SignInAnonymouslyAsync();
    }

    async void OnSignedIn()
    {
        LogIdentity("Signed in");

        await Task.Delay(2000);

        // Fetch the server-side player name — may be null after anonymous sign-in.
        await m_Manager.GetPlayerNameAsync();
        LogIdentity("After name fetch");

        // Round-trip a probe value through the Default Cloud Save bucket.
        const string key = "tester_probe";
        const string expected = "ok";
        await m_Manager.SaveDefaultAsync(key, expected);
        var loaded = await m_Manager.LoadDefaultAsync(key);

        var match = loaded == expected ? "OK" : $"MISMATCH (got '{loaded}')";
        Debug.Log($"[PlayerAccountTester] Save→Load round-trip: {match}");

        // List all keys in the Default bucket for this player.
        var keys = await m_Manager.LoadAllDefaultKeysAsync();
        LogKeys(keys);
    }

    void LogIdentity(string label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[PlayerAccountTester] {label}:");
        sb.AppendLine($"  PlayerId   : {m_Manager.PlayerId ?? "<null>"}");
        sb.AppendLine($"  PlayerName : {(string.IsNullOrEmpty(m_Manager.PlayerName) ? "<none>" : m_Manager.PlayerName)}");
        sb.AppendLine($"  IsSignedIn : {m_Manager.IsSignedIn}");
        Debug.Log(sb.ToString());
    }

    void LogKeys(List<string> keys)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[PlayerAccountTester] Default bucket keys ({keys.Count}):");
        foreach (var k in keys)
            sb.AppendLine($"  {k}");
        Debug.Log(sb.ToString());
    }
}
```

---

## Assembly Definition

Your `.asmdef` must reference both `Unity.Services.Authentication` and `Unity.Services.Authentication.PlayerAccounts`. These are **separate assemblies** within the same package (`com.unity.services.authentication`). Missing the PlayerAccounts reference causes `PlayerAccountService` to be unresolvable. `Unity.Services.CloudCode` is only needed if using Protected or Custom data writes.

```json
{
    "name": "MyGame.PlayerAccount",
    "rootNamespace": "",
    "references": [
        "Unity.Services.Core",
        "Unity.Services.Authentication",
        "Unity.Services.Authentication.PlayerAccounts",
        "Unity.Services.CloudSave",
        "Unity.Services.CloudCode"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

---

## Error Handling

### AuthenticationException

Thrown by all `AuthenticationService` methods. Extends `RequestFailedException`.

```csharp
try
{
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
}
catch (AuthenticationException ex)
{
    // ex.ErrorCode contains a numeric error code from AuthenticationErrorCodes
    Debug.LogError($"Auth failed: {ex.Message} (code: {ex.ErrorCode})");
}
catch (RequestFailedException ex)
{
    // Network or other service errors
    Debug.LogError($"Request failed: {ex.Message}");
}
```

Key error codes:

| Constant | Value | Meaning |
|---|---|---|
| `AccountAlreadyLinked` | 10003 | Username already exists (on sign-up) or external ID already linked |
| `InvalidParameters` | 10002 | Bad input parameters |
| `ClientInvalidUserState` | 10000 | Operation not valid in current state |

### PlayerAccountsException

Thrown by `PlayerAccountService` methods. Also extends `RequestFailedException`.

```csharp
try
{
    await PlayerAccountService.Instance.StartSignInAsync();
}
catch (PlayerAccountsException ex)
{
    Debug.LogError($"Player Accounts error: {ex.Message} (code: {ex.ErrorCode})");
}
```

Key error codes (from `PlayerAccountsErrorCodes`):

| Constant | Value | Meaning |
|---|---|---|
| `InvalidState` | 10101 | Player is already signed in |
| `MissingClientId` | 10102 | Client ID not configured in Unity Dashboard |

### CloudSaveException

Thrown by Cloud Save operations.

```csharp
try
{
    await CloudSaveService.Instance.Data.Player.SaveAsync(data);
}
catch (CloudSaveValidationException ex)
{
    foreach (var detail in ex.Details)
        Debug.LogError($"Validation: {detail.Field} {string.Join(", ", detail.Messages)}");
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

---

## Validation

After implementing a player account feature, verify:

1. **Compile check:** The project compiles without errors. All namespaces (`Unity.Services.Core`, `Unity.Services.Authentication`, `Unity.Services.Authentication.PlayerAccounts`, `Unity.Services.CloudSave`) resolve correctly.
2. **Initialization order:** `UnityServices.InitializeAsync()` is called before any sign-in method. Service singletons (`AuthenticationService.Instance`, `PlayerAccountService.Instance`, `CloudSaveService.Instance`) are accessed only after initialization.
3. **Assembly references:** The `.asmdef` references **both** `Unity.Services.Authentication` and `Unity.Services.Authentication.PlayerAccounts`. These are separate assemblies in the same package; omitting the PlayerAccounts reference makes `PlayerAccountService` unresolvable.
4. **SignedIn event wiring:** `PlayerAccountService.Instance.SignedIn` is subscribed **before** calling `StartSignInAsync()`. The `StartSignInAsync` Task completes when the browser opens, not when sign-in finishes.
5. **Access class matching:** Data saved with `PublicWriteAccessClassOptions` is loaded with `PublicReadAccessClassOptions`. Data saved with no options (Default) is loaded with `DefaultReadAccessClassOptions` or no options. Mismatched access classes return empty results without error.
6. **SignOut is synchronous:** `AuthenticationService.Instance.SignOut()` and `PlayerAccountService.Instance.SignOut()` are both `void` methods. Do not `await` them.

---

## Asset Store Building Block

- **Player Account Building Block:** Complete reference implementation with sign-in UI (anonymous, Unity browser, username/password), Cloud Save data management, and a Cloud Code module with generated client bindings. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-player-account-341928)
