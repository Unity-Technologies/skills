# Cloud Code Reference

## Table of Contents

- [Anti-Hallucination Reference](#anti-hallucination-reference)
- [Two Execution Models](#two-execution-models)
- [ICloudCodeService Methods](#icloudcodeservice-methods)
- [Module Runtime Constraints](#module-runtime-constraints)
- [Subscriptions](#subscriptions)
- [Triggers](#triggers)
- [Module Scope](#module-scope)
- [Module Creation](#module-creation)
- [Generated Type-Safe Bindings](#generated-type-safe-bindings)
- [Code Templates](#code-templates)
- [Deploying via the Deployment Window](#deploying-via-the-deployment-window)
- [Error Handling](#error-handling)

Accessed via `CloudCodeService.Instance` (`ICloudCodeService`, namespace `Unity.Services.CloudCode`). Assembly: `Unity.Services.CloudCode`.

Call `UnityServices.InitializeAsync()` from `com.unity.services.core` and sign in via `com.unity.services.authentication` before use.

---

## Anti-Hallucination Reference

| Correct | Incorrect (do NOT use) |
|---|---|
| `CallModuleEndpointAsync("Module", "Function", args)` | `CallEndpointAsync` for modules |
| `CallEndpointAsync("scriptName", args)` | `CallEndpointAsync` for modules -- scripts only |
| `SubscribeToPlayerMessagesAsync()` -- no parameters | `SubscribeToPlayerMessagesAsync(callbacks)` |
| `SubscribeToProjectMessagesAsync()` -- no parameters | `SubscribeToGeneralMessagesAsync`, `SubscribeToProjectMessagesAsync(callbacks)` |
| `subscription.Callbacks.MessageReceived += handler` | Passing callbacks as constructor/method arg |
| Non-generic `CallEndpointAsync` returns `Task<string>` | Returns `Task` (void) -- it returns a string |
| Non-generic `CallModuleEndpointAsync` returns `Task<string>` | Returns `Task` (void) -- it returns a string |
| `EventConnectionState` enum | `SubscriptionEventConnectionState` |
| `Error` callback is `Action<string>` | `Action<EventConnectionState>` |

---

## Two Execution Models

| Model | Language | Client Method | Deployed as |
|---|---|---|---|
| **Scripts** | JavaScript | `CallEndpointAsync` | `.js` files via Deployment Window |
| **Modules** | C# (.NET 9) | `CallModuleEndpointAsync` | `.ccmr` module references via Deployment Window |

**Recommendation:** Prefer **C# modules** over JavaScript scripts for production. Modules offer type safety, generated client bindings, and access to NuGet packages. Use scripts only for lightweight prototyping.

---

## ICloudCodeService Methods

```csharp
// Call a Cloud Code Script endpoint. Returns the script's return value as string.
Task<string> CallEndpointAsync(string function, Dictionary<string, object> args = default)

// Call a Cloud Code Script endpoint with typed deserialization.
Task<TResult> CallEndpointAsync<TResult>(string function, Dictionary<string, object> args = default)

// Call a Cloud Code Module endpoint. Returns the result as string.
// module = module name (filename of .ccmr without extension), function = method name.
Task<string> CallModuleEndpointAsync(string module, string function,
    Dictionary<string, object> args = default, CloudCodeModuleScope scope = default)

// Call a Cloud Code Module endpoint with typed deserialization.
Task<TResult> CallModuleEndpointAsync<TResult>(string module, string function,
    Dictionary<string, object> args = default, CloudCodeModuleScope scope = default)

// Subscribe to real-time messages sent to the current player. Requires Wire. NO PARAMETERS.
Task<ISubscriptionEvents> SubscribeToPlayerMessagesAsync()

// Subscribe to real-time project-wide messages. Requires Wire. NO PARAMETERS.
Task<ISubscriptionEvents> SubscribeToProjectMessagesAsync()
```

**CRITICAL:** The first argument to `CallModuleEndpointAsync` is the **module name**, the second is the **function name**. Do NOT confuse the parameter order.

---

## Module Runtime Constraints

- **Cold start:** After ~15 minutes of inactivity, the module's worker shuts down. The next call experiences a latency spike while the worker spins up. Design your UX to tolerate this.
- **No UnityEngine:** Modules run on standard .NET. The `UnityEngine` namespace is NOT available. Use only .NET APIs and NuGet packages.
- **Push messages:** Server-to-client push messages can only be sent from **modules**, not from JavaScript scripts.

---

## Subscriptions

Cloud Code Subscriptions require `com.unity.services.wire`. Subscribe to real-time server-push messages.

### `ISubscriptionEvents`

```csharp
public interface ISubscriptionEvents
{
    SubscriptionEventCallbacks Callbacks { get; }  // Wire up handlers here
    Task SubscribeAsync();      // Re-subscribe after unsubscribing
    Task UnsubscribeAsync();    // Stop receiving messages
}
```

### `SubscriptionEventCallbacks`

Wire up callbacks on the `Callbacks` property of the returned `ISubscriptionEvents`:

```csharp
var subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync();
subscription.Callbacks.MessageReceived += OnMessageReceived;       // Action<IMessageReceivedEvent>
subscription.Callbacks.MessageBytesReceived += OnBytesReceived;    // Action<byte[]>
subscription.Callbacks.ConnectionStateChanged += OnStateChanged;   // Action<EventConnectionState>
subscription.Callbacks.Kicked += OnKicked;                         // Action
subscription.Callbacks.Error += OnError;                           // Action<string>
```

### `IMessageReceivedEvent` Properties

| Property | Type | Description |
|---|---|---|
| `MessageType` | `string` | Message type identifier set by the server |
| `Message` | `string` | Raw message payload (typically JSON) |
| `Id` | `string` | Unique message ID |
| `CorrelationId` | `string` | Correlation ID for tracing |
| `Source` | `string` | Source of the message |
| `Type` | `string` | CloudEvents type |
| `SpecVersion` | `string` | CloudEvents spec version |
| `Time` | `DateTime` | Timestamp |
| `ProjectId` | `string` | UGS project ID |
| `EnvironmentId` | `string` | UGS environment ID |

### `EventConnectionState`

Namespace: `Unity.Services.CloudCode.Subscriptions`.

| State | Value | Meaning |
|---|---|---|
| `Unknown` | 0 | Initial state |
| `Unsubscribed` | 1 | Not connected |
| `Subscribing` | 2 | Connection in progress |
| `Subscribed` | 3 | Connected and receiving messages |
| `Unsynced` | 4 | Temporarily disconnected, will reconnect |
| `Error` | 5 | Connection error |

---

## Triggers

Triggers let Cloud Code endpoints run automatically in response to events from other UGS services, without any client call. Configured in the Unity Dashboard or via the UGS CLI, not in client code.

| Emitter | Example trigger events |
|---|---|
| Authentication | Player signed in, account deleted |
| Scheduler | Cron-based or one-shot scheduled events |
| Leaderboards | Score submitted, leaderboard reset |
| Cloud Save | Data saved, data deleted |

---

## Module Scope

`CallModuleEndpointAsync` accepts an optional `CloudCodeModuleScope` to scope execution. Namespace: `Unity.Services.CloudCode.Models`.

```csharp
// Scope to a multiplayer session
var scope = new CloudCodeModuleScope(ScopeType.MultiplayerSession, sessionId);
var result = await CloudCodeService.Instance.CallModuleEndpointAsync<MyResult>(
    "MyModule", "MyFunction", args, scope);
```

### `ScopeType` Enum

| Scope Type | Value | Description |
|---|---|---|
| `MultiplayerSession` | 1 | Scoped to a multiplayer session ID |
| `Player` | 2 | Scoped to the calling player (default) |

---

## Module Creation

A Cloud Code module is a .NET 9 class library built and published by the Deployment Window.

**A deployable module requires ALL of the following files — skipping any one breaks the build or deploy step:** the `<Module>.sln` solution, the `<Module>/<Module>.csproj` main project (with the NuGet references shown below), the module class (`<Module>.cs`) with at least one `[CloudCodeFunction]` endpoint, `ModuleSetup.cs` implementing `ICloudCodeSetup`, `<Module>/Properties/PublishProfiles/FolderProfile.pubxml` for the linux-x64 publish, and the `Assets/.../<Module>.ccmr` Unity asset pointing at the `.sln`. A `.ccmr` referencing a missing `.sln`, or a module folder lacking `.csproj`/`FolderProfile.pubxml`, is incomplete and the Deployment Window will fail to publish it.

### Directory Structure

Place the module at the **project root** (alongside `Assets/`). Unity does not scan the project root, so no `~` suffix is needed.

> **Tilde rule:** Only add `~` to a folder name (e.g. `MyModule~/`) when the module lives **inside `Assets/`** -- the suffix tells Unity to skip that folder during compilation. At the project root, omit it.

```
<ProjectRoot>/
  Assets/
    CloudCode/
      MyModule.ccmr                         <-- Unity asset pointing to the .sln
  MyModuleCCM/
    MyModule.sln
    MyModule/
      MyModule.csproj
      MyModule.cs                           <-- [CloudCodeFunction] entry points
      ModuleSetup.cs                        <-- registers IGameApiClient via DI
      Properties/
        PublishProfiles/
          FolderProfile.pubxml              <-- publish to linux-x64 (required)
    MyModule.Tests/
      MyModule.Tests.csproj                 <-- IsPublishable=false, never deployed
      MyModuleTests.cs
```

### `.gitignore` (keep module files tracked)

Unity's stock `.gitignore` excludes `*.sln` and `*.csproj` project-wide. Without an override the module's `.sln`/`.csproj`/`.pubxml` will be silently untracked, which breaks teammates, CI, and any deploy that pulls from git. Add a `.gitignore` **inside the module directory** that re-includes them:

```
# <Module>CCM/.gitignore
!*.sln
!*.csproj
!Properties/PublishProfiles/*.pubxml
```

Verify with `git status` after creating the files -- they must appear as untracked/modified, not be missing from the listing.

### `.sln` File Template

Generate fresh GUIDs for `{MAIN-GUID}`, `{TEST-GUID}`, and `{SLN-GUID}` using `[System.Guid]::NewGuid()` in PowerShell or any GUID generator. The test project intentionally has no `Release Build.0` entry to exclude it from Release builds.

```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 16
VisualStudioVersion = 16.0.30114.105
MinimumVisualStudioVersion = 10.0.40219.1
Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "MyModule", "MyModule\MyModule.csproj", "{MAIN-GUID}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyModule.Tests", "MyModule.Tests\MyModule.Tests.csproj", "{TEST-GUID}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{MAIN-GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{MAIN-GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{MAIN-GUID}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{MAIN-GUID}.Release|Any CPU.Build.0 = Release|Any CPU
		{TEST-GUID}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{TEST-GUID}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{TEST-GUID}.Release|Any CPU.ActiveCfg = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {SLN-GUID}
	EndGlobalSection
EndGlobal
```

### `.csproj` (net9.0, CloudCode.Apis 0.0.21, CloudCode.Core 0.0.1)

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <Nullable>enable</Nullable>
        <Configurations>Debug;Release</Configurations>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Com.Unity.Services.CloudCode.Apis" Version="0.0.21" />
        <PackageReference Include="Com.Unity.Services.CloudCode.Core" Version="0.0.1" />
        <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="7.0.1" />
    </ItemGroup>

    <PropertyGroup>
        <PublishReadyToRunComposite>true</PublishReadyToRunComposite>
    </PropertyGroup>

</Project>
```

### `FolderProfile.pubxml` (linux-x64, SelfContained=false)

Cloud Code runs on Linux. `SelfContained=false` keeps the output small -- the runtime is provided by the Cloud Code host.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>bin\Release\net9.0\publish\</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <TargetFramework>net9.0</TargetFramework>
    <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
    <PublishSingleFile>False</PublishSingleFile>
  </PropertyGroup>
</Project>
```

### `ModuleSetup.cs` (registers GameApiClient)

Required once per module. Registers `GameApiClient` so it can be constructor-injected.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}
```

### `MyModule.cs` ([CloudCodeFunction], IExecutionContext, IGameApiClient)

```csharp
using System;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

public class MyModule
{
    readonly IGameApiClient m_GameApiClient;
    readonly ILogger<MyModule> m_Logger;

    public MyModule(IGameApiClient gameApiClient, ILogger<MyModule> logger)
    {
        m_GameApiClient = gameApiClient;
        m_Logger = logger;
    }

    // No context needed -- pure computation.
    [CloudCodeFunction("SayHello")]
    public string SayHello(string name) => $"Hello, {name}!";

    // Add IExecutionContext as a method parameter only when you need player/project info.
    [CloudCodeFunction("GetServerTime")]
    public string GetServerTime(IExecutionContext context)
        => DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
}
```

**Key concepts:**

| File / Concept | Purpose |
|---|---|
| `[CloudCodeFunction]` | Marks a public method as a callable endpoint |
| `IExecutionContext` | Optional method parameter -- player ID, project ID, service token |
| `IGameApiClient` | Constructor-injected via DI -- typed clients for UGS APIs |
| `ModuleSetup.cs` | Registers `GameApiClient` via `ICloudCodeSetup` -- required once per module |
| `FolderProfile.pubxml` | Publish profile targeting `linux-x64` -- Cloud Code runs on Linux |
| `IsPublishable=false` | Prevents test project from being included in `dotnet publish` |

**Dependency injection:** `IGameApiClient` and `ILogger<T>` are constructor-injected. `IExecutionContext` is a method parameter (only add it when the function needs player/project info).

### Test Project `.csproj` (IsPublishable=false)

`IsPublishable=false` is enforced in two ways: this property, and the omission of `Release Build.0` in the `.sln`. Both are intentional.

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <IsPublishable>false</IsPublishable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="coverlet.collector" Version="6.0.2" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
        <PackageReference Include="NUnit" Version="4.2.2" />
        <PackageReference Include="NUnit.Analyzers" Version="4.4.0" />
        <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="NUnit.Framework" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\MyModule\MyModule.csproj" />
    </ItemGroup>

</Project>
```

### `.ccmr` File Format

```json
{
  "modulePath": "../../MyModuleCCM/MyModule.sln"
}
```

Path is relative to the `.ccmr` file. `../../` goes up from `Assets/CloudCode/` to the project root, then into `MyModuleCCM/`. The filename without `.ccmr` (`MyModule`) is the module name used in `CallModuleEndpointAsync`.

---

## Generated Type-Safe Bindings

After deploying the module, generate a strongly-typed wrapper instead of using raw `CallModuleEndpointAsync`. The generator reads the deployed module's metadata and emits one C# file per class under `Assets/CloudCode/GeneratedModuleBindings/`. Do not edit these files -- they are overwritten on regeneration.

### How to Generate

- Select the `.ccmr` in the **Project window** then click **Generate Bindings** in the Inspector, or
- **Services > Cloud Code > Generate Cloud Code Bindings** to regenerate all at once.

### Example

```csharp
// Assets/CloudCode/GeneratedModuleBindings/MyModuleBindings.cs
// This file was generated by Cloud Code Bindings Generator. Modifications will be lost upon regeneration.
public class MyModuleBindings
{
    readonly ICloudCodeService k_Service;
    public MyModuleBindings(ICloudCodeService service) { k_Service = service; }

    public async Task<string> SayHello(string name)
        => await k_Service.CallModuleEndpointAsync<string>("MyModule", "SayHello",
            new Dictionary<string, object> { { "name", name } });
}
```

```csharp
var bindings = new MyModuleBindings(CloudCodeService.Instance);
var greeting = await bindings.SayHello("World");
```

**Known limitations:** no distinction between required/optional parameters; tuples are not supported.

---

## Code Templates

### Call a Script Endpoint

```csharp
using Unity.Services.CloudCode;
using System.Collections.Generic;

var args = new Dictionary<string, object>
{
    { "level", 5 },
    { "difficulty", "hard" }
};

int reward = await CloudCodeService.Instance.CallEndpointAsync<int>("CalculateReward", args);
Debug.Log($"Reward: {reward}");
```

### Call a Module Endpoint (Typed Result)

```csharp
// Module class name: "EconomyModule", method: "GrantDailyReward"
var result = await CloudCodeService.Instance.CallModuleEndpointAsync<GrantResult>(
    "EconomyModule", "GrantDailyReward",
    new Dictionary<string, object> { { "playerId", playerId } });

Debug.Log($"Granted: {result.CurrencyAmount} currency");
```

### Subscribe to Player Messages (Full Example)

```csharp
ISubscriptionEvents subscription;

async Task SubscribeToMessages()
{
    // Subscribe first -- no parameters
    subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync();

    // Then wire up callbacks via the Callbacks property
    subscription.Callbacks.MessageReceived += OnMessageReceived;
    subscription.Callbacks.ConnectionStateChanged += state =>
        Debug.Log($"Subscription state: {state}");
    subscription.Callbacks.Error += error =>
        Debug.LogError($"Subscription error: {error}");
    subscription.Callbacks.Kicked += () =>
        Debug.LogWarning("Kicked from subscription");
}

void OnMessageReceived(IMessageReceivedEvent evt)
{
    Debug.Log($"[{evt.MessageType}] {evt.Message}");
    // var data = JsonUtility.FromJson<MyPayload>(evt.Message);
}

async Task Unsubscribe()
{
    await subscription.UnsubscribeAsync();
    subscription = null;
}
```

---

## Deploying via the Deployment Window

1. Open **Services > Deployment** in the Unity Editor.
2. All `.js` and `.ccmr` files under `Assets/` are listed -- click **Deploy All**.
3. After deploying a module, generate or regenerate type-safe bindings (see [Generated Type-Safe Bindings](#generated-type-safe-bindings)).

---

## Error Handling

```csharp
try
{
    var result = await CloudCodeService.Instance.CallModuleEndpointAsync<MyResult>(
        "MyModule", "MyFunction", args);
}
catch (CloudCodeRateLimitedException ex)
{
    Debug.LogError($"Rate limited. Retry after {ex.RetryAfter}s");
}
catch (CloudCodeException ex)
{
    Debug.LogError($"Cloud Code error: {ex.Message} (reason: {ex.Reason})");
}
```

### `CloudCodeExceptionReason` Enum

| Reason | Value | Meaning |
|---|---|---|
| `Unknown` | 0 | Unknown error |
| `NoInternetConnection` | 1 | No network |
| `ProjectIdMissing` | 2 | Project ID not set |
| `PlayerIdMissing` | 3 | Player not signed in |
| `AccessTokenMissing` | 4 | No access token |
| `InvalidArgument` | 5 | Bad input |
| `Unauthorized` | 6 | Not authorized |
| `NotFound` | 7 | Script/module not found |
| `TooManyRequests` | 8 | Rate limited |
| `ServiceUnavailable` | 9 | Service down |
| `ScriptError` | 10 | Script/module execution failure |
| `SubscriptionError` | 11 | Subscription error |

---

## Asset Store Building Blocks

Several Building Blocks from the Unity Asset Store include Cloud Code modules as working reference implementations:

- **Achievements Building Block** — Cloud Code module with achievement progress tracking and server-authoritative unlocks. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-achievements-341918)
- **Player Account Building Block** — Cloud Code module with global score tracking via server-authoritative writes. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-player-account-341928)
- **Leaderboards Building Block** — Cloud Code modules with number game scoring and leaderboard admin functions. [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-leaderboards-341926)

Each block includes `.ccmr` module bindings, generated type-safe client bindings, and the full C# module source.
