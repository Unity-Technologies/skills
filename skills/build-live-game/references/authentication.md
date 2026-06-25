# Authentication Reference

## Table of Contents

- [Anti-Hallucination Reference](#anti-hallucination-reference)
- [Sign-In Methods](#sign-in-methods)
- [Key Properties](#key-properties)
- [Events](#events)
- [Player Info / Name](#player-info--name)
- [Link / Unlink Methods](#link--unlink-methods)
- [Profile Switching](#profile-switching)
- [Username/Password Constraints](#usernamepassword-constraints)
- [Options / Types](#options--types)
- [Identity Providers Quick Reference](#identity-providers-quick-reference)
- [Code Templates](#code-templates)
- [Error Handling](#error-handling)
- [Session Warnings](#session-warnings)

Accessed via `AuthenticationService.Instance` (`IAuthenticationService`, namespace `Unity.Services.Authentication`). Assembly: `Unity.Services.Authentication`.

Call `UnityServices.InitializeAsync()` from `com.unity.services.core` before using this service.

---

## Anti-Hallucination Reference

| Correct | Incorrect (do NOT use) |
|---|---|
| `SignOut(bool clearCredentials = false)` -- synchronous void | `SignOutAsync`, `SignOut()` returning Task |
| `SignInWithSteamAsync(sessionTicket, identity)` -- identity param is required | `SignInWithSteamAsync(sessionTicket)` -- deprecated |
| `SignInWithOculusAsync(nonce, userId)` -- both params required | `SignInWithOculusAsync(nonce)` -- missing userId |
| `LinkWithSteamAsync(sessionTicket, identity)` | `LinkWithSteamAsync(sessionTicket)` -- deprecated |
| `LinkWithOculusAsync(nonce, userId)` | `LinkWithOculusAsync(nonce)` -- missing userId |
| `SwitchProfile(string)` -- synchronous void | `SwitchProfileAsync` |
| `ClearSessionToken()` -- synchronous void | `ClearSessionTokenAsync` |
| No `UnlinkUsernamePasswordAsync` | Username/password cannot be unlinked once linked |
| No `IsAnonymous` property | Use `PlayerInfo.Identities` to check linked providers |

---

## Sign-In Methods

```csharp
// Anonymous sign-in. Creates a new player if no cached credentials exist.
Task SignInAnonymouslyAsync(SignInOptions options = default)

// Google ID token sign-in.
Task SignInWithGoogleAsync(string idToken, SignInOptions options = default)

// Google Play Games auth code sign-in.
Task SignInWithGooglePlayGamesAsync(string authCode, SignInOptions options = default)

// Apple ID token sign-in.
Task SignInWithAppleAsync(string idToken, SignInOptions options = default)

// Apple Game Center signature sign-in.
Task SignInWithAppleGameCenterAsync(string signature, string teamPlayerId, string publicKeyURL, string salt, ulong timestamp, SignInOptions options = default)

// Facebook access token sign-in.
Task SignInWithFacebookAsync(string accessToken, SignInOptions options = default)

// Steam session ticket sign-in. The identity param is REQUIRED (deprecated overload without it).
[Obsolete] Task SignInWithSteamAsync(string sessionTicket, SignInOptions options = default)
Task SignInWithSteamAsync(string sessionTicket, string identity, SignInOptions options = default)
// For sub-apps (PlayTest, Demo) -- include appId:
Task SignInWithSteamAsync(string sessionTicket, string identity, string appId, SignInOptions options = default)

// Oculus sign-in. Both nonce AND userId are required.
Task SignInWithOculusAsync(string nonce, string userId, SignInOptions options = default)

// OpenID Connect sign-in with a custom provider ID.
Task SignInWithOpenIdConnectAsync(string idProviderName, string idToken, SignInOptions options = default)

// Unity token sign-in.
Task SignInWithUnityAsync(string token, SignInOptions options = default)

// Username/password sign-up (first-time registration).
Task SignUpWithUsernamePasswordAsync(string username, string password)

// Username/password sign-in (no SignInOptions).
Task SignInWithUsernamePasswordAsync(string username, string password)

// Device code flow -- generate code, then poll for sign-in.
Task<SignInCodeInfo> GenerateSignInCodeAsync(string identifier = default)
Task SignInWithCodeAsync(bool usePolling = false, CancellationToken cancellationToken = default)
Task<SignInCodeInfo> GetSignInCodeInfoAsync(string code)
Task ConfirmCodeAsync(string code, string idProvider = default, string externalToken = default)

// Sign out. Synchronous (void), NOT async.
void SignOut(bool clearCredentials = false)

// Permanently delete the current player's account.
Task DeleteAccountAsync()
```

---

## Key Properties

| Property | Type | Description |
|---|---|---|
| `IsSignedIn` | `bool` | Token exists in memory (player has signed in during this session) |
| `IsAuthorized` | `bool` | Access token is present AND not expired (player can make API calls) |
| `IsExpired` | `bool` | Access token has expired but still exists (SDK will try auto-refresh) |
| `PlayerId` | `string` | Unique player identifier |
| `PlayerName` | `string` | Player display name |
| `AccessToken` | `string` | JWT access token for service calls |
| `Profile` | `string` | Current profile name |
| `SessionTokenExists` | `bool` | True if a session token is cached |

**Key distinction:** `IsSignedIn` means a token exists (even if expired). `IsAuthorized` means the token is currently valid. `IsExpired` means the token expired but still exists -- the SDK will attempt automatic refresh. If refresh fails, `SignInFailed` fires.

---

## Events

| Event | Signature | When fired |
|---|---|---|
| `SignedIn` | `Action` | Sign-in succeeds |
| `SignedOut` | `Action` | Player signs out |
| `Expired` | `Action` | Access token expires; SDK auto-attempts refresh. Re-auth only needed if refresh fails (`SignInFailed` fires) |
| `SignInFailed` | `Action<RequestFailedException>` | Sign-in or token refresh fails |
| `SignInCodeReceived` | `Action<SignInCodeInfo>` | Device code flow: sign-in code generated |
| `SignInCodeExpired` | `Action` | Device code flow: sign-in code expired |
| `PlayerNameChanged` | `Action<string>` | Player name updated (provides new name) |
| `PlayerIdChanged` | `Action<string>` | Player ID changed (provides new ID) |
| `PlayerInfoChanged` | `Action<PlayerInfo>` | Player info updated |

---

## Player Info / Name

```csharp
Task<PlayerInfo> GetPlayerInfoAsync()
Task<string> GetPlayerNameAsync(bool autoGenerate = true)
Task<string> UpdatePlayerNameAsync(string name)
```

### `PlayerInfo` Properties

| Property | Type | Description |
|---|---|---|
| `Id` | `string` | UGS player ID |
| `Username` | `string?` | Username (if set) |
| `Identities` | `List<Identity>` | Linked external providers |
| `CreatedAt` | `DateTime?` | Account creation time |
| `LastPasswordUpdate` | `DateTime?` | Last password change |

Helper methods: `GetGoogleId()`, `GetAppleId()`, `GetFacebookId()`, `GetSteamId()`, `GetOculusId()`, `GetGooglePlayGamesId()`, `GetAppleGameCenterId()`, `GetUnityId()`, `GetOpenIdConnectId(providerName)`, `GetCustomId()`, `GetOpenIdConnectIdProviders()`.

---

## Link / Unlink Methods

All Link methods accept an optional `LinkOptions` parameter (for `ForceLink`).

```csharp
Task LinkWithGoogleAsync(string idToken, LinkOptions options = default)
Task LinkWithGooglePlayGamesAsync(string authCode, LinkOptions options = default)
Task LinkWithAppleAsync(string idToken, LinkOptions options = default)
Task LinkWithAppleGameCenterAsync(string signature, string teamPlayerId, string publicKeyURL, string salt, ulong timestamp, LinkOptions options = default)
Task LinkWithFacebookAsync(string accessToken, LinkOptions options = default)
[Obsolete] Task LinkWithSteamAsync(string sessionTicket, LinkOptions options = default)
Task LinkWithSteamAsync(string sessionTicket, string identity, LinkOptions options = default)
Task LinkWithSteamAsync(string sessionTicket, string identity, string appId, LinkOptions options = default)
Task LinkWithOculusAsync(string nonce, string userId, LinkOptions options = default)
Task LinkWithOpenIdConnectAsync(string idProviderName, string idToken, LinkOptions options = default)
Task LinkWithUnityAsync(string token, LinkOptions options = default)

Task UnlinkGoogleAsync()
Task UnlinkGooglePlayGamesAsync()
Task UnlinkAppleAsync()
Task UnlinkAppleGameCenterAsync()
Task UnlinkFacebookAsync()
Task UnlinkSteamAsync()
Task UnlinkOculusAsync()
Task UnlinkOpenIdConnectAsync(string idProviderName)
Task UnlinkUnityAsync()
```

**Note:** There is no `UnlinkUsernamePasswordAsync`. Username/password credentials cannot be unlinked once linked.

---

## Profile Switching

```csharp
// Switch active profile. Must be called BEFORE sign-in. Synchronous.
void SwitchProfile(string profile)

// Clear cached session token for the active profile.
void ClearSessionToken()

// Process externally-obtained tokens.
void ProcessAuthenticationTokens(string accessToken, string sessionToken = default)
```

Use `SwitchProfile(string profile)` before sign-in to store separate cached credentials per-profile (useful for multiple accounts on one device). Must be called BEFORE signing in -- call `SignOut()` first if already signed in.

---

## Username/Password Constraints

| Field | Constraint |
|---|---|
| Username length | 3-20 characters |
| Username characters | `A-Z`, `a-z`, `0-9`, `.`, `-`, `@`, `_` |
| Username case | Case insensitive (stored lowercase) |
| Password length | 8-30 characters |
| Password requirements | At least 1 lowercase + 1 uppercase + 1 number + 1 symbol |

Username/password credentials **cannot be unlinked** once linked. `UpdatePasswordAsync()` signs out all other devices for this player.

Additional username/password management methods:

```csharp
// Add username/password to an existing signed-in account.
Task AddUsernamePasswordAsync(string username, string password)

// Update the password for the currently signed-in account.
// WARNING: Signs out ALL other devices for this player.
Task UpdatePasswordAsync(string currentPassword, string newPassword)
```

---

## Options / Types

### `SignInOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `CreateAccount` | `bool` | `true` | If false, only sign in if an existing player maps to the credentials |

### `LinkOptions`

| Property | Type | Default | Description |
|---|---|---|---|
| `ForceLink` | `bool` | `false` | If true, removes existing link on another player and reassigns to current |

### `SignInCodeInfo`

| Property | Type | Description |
|---|---|---|
| `SignInCode` | `string` | The code to display to the user |
| `Identifier` | `string` | Identifier for the code |
| `Expiration` | `string` | When the code expires |

---

## Identity Providers Quick Reference

| Provider | Dashboard credentials | SDK input | SDK method |
|---|---|---|---|
| Anonymous | None | None | `SignInAnonymouslyAsync()` |
| Google | Client ID, Client Secret | ID token | `SignInWithGoogleAsync(idToken)` |
| Google Play Games | Client ID, Client Secret | Auth code | `SignInWithGooglePlayGamesAsync(authCode)` |
| Apple | Client ID, Client Secret | ID token | `SignInWithAppleAsync(idToken)` |
| Apple Game Center | Client ID, Client Secret | Signature bundle | `SignInWithAppleGameCenterAsync(...)` |
| Facebook | Client ID, Client Secret | Access token | `SignInWithFacebookAsync(accessToken)` |
| Steam | Publisher Web API Key | Session ticket + identity | `SignInWithSteamAsync(sessionTicket, identity)` |
| Oculus | App ID, App Secret | Nonce + user ID | `SignInWithOculusAsync(nonce, userId)` |
| OpenID Connect | Client ID, Client Secret, Issuer URL | ID token | `SignInWithOpenIdConnectAsync(providerName, idToken)` |
| Unity Player Accounts | Enable in dashboard | Access token | `SignInWithUnityAsync(token)` |
| Username / Password | Enable in dashboard | Username + password | `SignInWithUsernamePasswordAsync(u, p)` |

All social identity providers are configured in the **Unity Dashboard**: Your project > Products > Authentication > Identity Providers > Add provider.

---

## Code Templates

### Initialize and Sign In Anonymously

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;

async Task InitAndSignIn()
{
    await UnityServices.InitializeAsync();

    // SignInAnonymouslyAsync auto-detects returning vs new players:
    // - If a session token exists, it signs in silently with the cached token
    // - If no token exists, it creates a new anonymous account
    if (!AuthenticationService.Instance.IsSignedIn)
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    Debug.Log($"Signed in as {AuthenticationService.Instance.PlayerId}");
}
```

### Sign In with Steam (with Identity Param)

```csharp
// sessionTicket from SteamUser.GetAuthSessionTicket
// identity string for security (required -- old overload without it is deprecated)
await AuthenticationService.Instance.SignInWithSteamAsync(sessionTicket, identity);

// For sub-apps (PlayTest, Demo) -- include appId:
await AuthenticationService.Instance.SignInWithSteamAsync(sessionTicket, identity, appId);
```

### Username/Password Sign Up then Sign In

```csharp
// First-time registration
await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync("myUsername", "MyPassword123!");

// Subsequent sign-ins
await AuthenticationService.Instance.SignInWithUsernamePasswordAsync("myUsername", "MyPassword123!");
```

### Link a Provider

```csharp
// Player is already signed in anonymously; link their Google account
try
{
    await AuthenticationService.Instance.LinkWithGoogleAsync(googleIdToken);
    Debug.Log("Google account linked successfully.");
}
catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
{
    Debug.LogWarning("This Google account is already linked to another player.");
}

// Force-link: removes existing link on another player and reassigns to current
await AuthenticationService.Instance.LinkWithGoogleAsync(
    idToken,
    new LinkOptions { ForceLink = true });
```

### Switch Profile

```csharp
// Must sign out first, then switch, then sign in
AuthenticationService.Instance.SignOut();
AuthenticationService.Instance.SwitchProfile("ProfileB");
await AuthenticationService.Instance.SignInAnonymouslyAsync();
```

### Token Expiry Handling

```csharp
AuthenticationService.Instance.Expired += () =>
{
    // Token expired - the SDK will attempt to refresh automatically.
    // If refresh fails, you'll get SignInFailed and need to re-authenticate.
    Debug.Log("Token expired, SDK is attempting refresh...");
};

AuthenticationService.Instance.SignInFailed += (ex) =>
{
    // Refresh failed, prompt user to sign in again
    Debug.LogError($"Sign-in failed: {ex.Message}");
};
```

### Error Handling

```csharp
try
{
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
}
catch (AuthenticationException ex)
{
    // ex.ErrorCode contains the specific error code
    Debug.LogError($"Auth failed: {ex.Message} (code: {ex.ErrorCode})");
}
catch (RequestFailedException ex)
{
    // Network or other service errors
    Debug.LogError($"Request failed: {ex.Message}");
}
```

### Account Management (GetPlayerInfo, UpdatePlayerName, DeleteAccount)

```csharp
// Get full player info
var info = await AuthenticationService.Instance.GetPlayerInfoAsync();

// Get or update player name
var name = await AuthenticationService.Instance.GetPlayerNameAsync();
var updated = await AuthenticationService.Instance.UpdatePlayerNameAsync("NewName");

// Add username/password to existing account
await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);

// Update password (signs out ALL other devices for this player)
await AuthenticationService.Instance.UpdatePasswordAsync(currentPassword, newPassword);

// Delete account permanently
await AuthenticationService.Instance.DeleteAccountAsync();
```

---

## Error Handling

Throws `AuthenticationException` (extends `RequestFailedException`) on failure. Use `ex.ErrorCode` and compare against `AuthenticationErrorCodes`:

| Constant | Value | Meaning |
|---|---|---|
| `ClientInvalidUserState` | 10000 | Operation not valid in current state |
| `ClientNoActiveSession` | 10001 | No active session |
| `InvalidParameters` | 10002 | Bad input parameters |
| `AccountAlreadyLinked` | 10003 | External ID already linked to another player |
| `AccountLinkLimitExceeded` | 10004 | Max linked accounts for this provider type |
| `ClientUnlinkExternalIdNotFound` | 10005 | External ID not linked |
| `ClientInvalidProfile` | 10006 | Invalid profile name |
| `InvalidSessionToken` | 10007 | Session token invalid or expired |
| `InvalidProvider` | 10008 | Unknown identity provider |
| `BannedUser` | 10009 | Player has been banned |
| `EnvironmentMismatch` | 10010 | Session token from a different environment |

**Error response format:** Authentication errors follow RFC 7807 (Problem Details). Branch your error handling on `status` and `title` fields, never on `detail` -- the detail message may change without notice.

---

## Session Warnings

**Console platforms (Xbox, PlayStation, Switch):** Token refresh is NOT automatic. You must manually re-sign in when the token expires.

**Anonymous-only accounts:** Calling `ClearSessionToken()` or `SignOut(clearCredentials: true)` on an account that ONLY has anonymous sign-in will **permanently lose** that account. Always link at least one external provider before clearing credentials.

**DeleteAccountAsync compliance:** iOS App Store (Guideline 5.1.1) requires apps that support account creation to offer account deletion. `DeleteAccountAsync()` only deletes **Authentication data**. It does NOT automatically clean up data in other UGS services (Cloud Save, Economy, Leaderboards, etc.). You must manually delete data from each service before calling `DeleteAccountAsync()`.

---

## Asset Store Building Blocks

- **Player Account Building Block:** Ready-made sign-in UI (anonymous, Unity browser, username/password), Cloud Save integration for player data, and a Cloud Code module for server-authoritative writes. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-player-account-341928)
