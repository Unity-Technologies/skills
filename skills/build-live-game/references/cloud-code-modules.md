# Cloud Code Scripts and Modules Reference

Covers **Cloud Code modules** (`.ccmu`, authored natively in the Editor), **legacy modules** (`.ccmr`
references to a .NET solution built outside Unity) and JavaScript **scripts**. For CloudBehaviours, see the CloudBehaviours reference.

> **On Unity 6000.5+, create new C# modules as `.ccmu`.** They are authored inside the Unity project, compile with it, and
> generate their client bindings at compile time — no separate solution, no `dotnet publish`. `.ccmr` is
> legacy: on 6000.5+ maintain the ones a project already has and do not create new ones; on an older
> Editor, new modules use `.ccmr`. On 6000.5+, if you are about to write a
> `.sln`, a `.csproj` or a `FolderProfile.pubxml`, you are on the wrong path — see
> [Creating a Cloud Code Module](#creating-a-cloud-code-module-ccmu) instead.

> **The signatures, enums and diagnostics below are a convenience snapshot and can lag the package.**
> Read the [Cloud Code documentation](https://docs.unity.com/en-us/cloud-code/modules/overview.md) for
> the project's installed `com.unity.services.cloudcode` version before acting, and again whenever a
> step fails or a symbol does not resolve.

## Table of Contents

- [Anti-Hallucination Reference](#anti-hallucination-reference)
- [ICloudCodeService Methods](#icloudcodeservice-methods)
- [Module Runtime Constraints](#module-runtime-constraints)
- [Subscriptions](#subscriptions)
- [Triggers](#triggers)
- [Creating a Cloud Code Module (.ccmu)](#creating-a-cloud-code-module-ccmu)
  - [Injected Services](#injected-services-cloudcodeservice)
  - [Registering Services](#registering-services-cloudcloudsetupcs)
- [Module State and Persistence](#module-state-and-persistence)
  - [Evolving a persisted class](#evolving-a-persisted-class)
- [Generated Client Bindings](#generated-client-bindings)
- [Legacy Modules (.ccmr) - Maintenance Only](#legacy-modules-ccmr---maintenance-only)
- [Code Templates](#code-templates)
- [Deploying via the Deployment Window](#deploying-via-the-deployment-window)
- [Running the Local Cloud Code Server](#running-the-local-cloud-code-server)
- [Error Handling](#error-handling)
- [Error Messages](#error-messages)

---

## Anti-Hallucination Reference

| Correct | Incorrect (do NOT use) |
|---|---|
| For a new C# module, author a `.ccmu` in the Editor | Scaffolding a `.sln`/`.csproj`/`.pubxml` for a new module -- that is legacy `.ccmr` |
| Create a `.ccmu` module with **Create > Services > Cloud Code Module Script** | Hand-writing a new module's `.asmdef` files -- a module without a `.ccmu` never appears in the Deployment window |
| A `.ccmu` module's name is its **cloud asmdef** `name` | The `.ccmu` filename -- it is only a fallback |
| `.ccmu` client bindings are generated at **compile time** from `[CloudModuleClient]` | Clicking **Generate Bindings** -- that is the `.ccmr` flow |
| `[CloudCodeService]` on an injected parameter of a `.ccmu` endpoint | A bare one -- the server still injects it, but it leaks into the generated client binding |
| `config.AddGameApiClient()` in `ICloudCodeSetup` | `config.Dependencies.AddSingleton(GameApiClient.Create())` -- `Create()` is `[Obsolete]` |
| The generated client for a `.ccmu` module; `CallModuleEndpointAsync("Module", "Function", args)` for a legacy `.ccmr` module | `CallModuleEndpointAsync` on a `.ccmu` module, or `CallEndpointAsync` for modules |
| `CallEndpointAsync("scriptName", args)` | `CallEndpointAsync` for modules -- scripts only |
| `SubscribeToPlayerMessagesAsync()`, or the `(callbacks)` overload | `SubscribeToGeneralMessagesAsync` -- no such method |
| Pass a `SubscriptionEventCallbacks` to the subscribe call, or attach to `subscription.Callbacks` afterwards | Callbacks as a constructor argument -- there is no such constructor |
| Non-generic `CallEndpointAsync` returns `Task<string>` | Returns `Task` (void) -- it returns a string |
| Non-generic `CallModuleEndpointAsync` returns `Task<string>` | Returns `Task` (void) -- it returns a string |
| `EventConnectionState` enum | `SubscriptionEventConnectionState` |
| `Error` callback is `Action<string>` | `Action<EventConnectionState>` |

---

## ICloudCodeService Methods

```csharp
// Call a Cloud Code Script endpoint. Returns the script's return value as string.
Task<string> CallEndpointAsync(string function, Dictionary<string, object> args = default)

// Call a Cloud Code Script endpoint with typed deserialization.
Task<TResult> CallEndpointAsync<TResult>(string function, Dictionary<string, object> args = default)

// Call a Cloud Code Module endpoint. Returns the result as string.
// module = module name, function = method name.
// Legacy .ccmr modules only: module = the .ccmr filename without extension.
// A .ccmu module is called through its generated client, not with this method.
Task<string> CallModuleEndpointAsync(string module, string function,
    Dictionary<string, object> args = default, CloudCodeScope scope = default)

// Call a Cloud Code Module endpoint with typed deserialization.
Task<TResult> CallModuleEndpointAsync<TResult>(string module, string function,
    Dictionary<string, object> args = default, CloudCodeScope scope = default)

// Subscribe to real-time messages sent to the current player. Requires Wire.
// Either wire callbacks up afterwards on the returned .Callbacks, or pass them in.
Task<ISubscriptionEvents> SubscribeToPlayerMessagesAsync()
Task<ISubscriptionEvents> SubscribeToPlayerMessagesAsync(SubscriptionEventCallbacks callbacks)

// Subscribe to real-time project-wide messages. Requires Wire. Same two forms.
Task<ISubscriptionEvents> SubscribeToProjectMessagesAsync()
Task<ISubscriptionEvents> SubscribeToProjectMessagesAsync(SubscriptionEventCallbacks callbacks)
```

**CRITICAL:** The first argument to `CallModuleEndpointAsync` is the **module name**, the second is the **function name**. Do NOT confuse the parameter order.

---

## Module Runtime Constraints

- **Cold start:** An idle module's worker is eventually shut down, and the next call after that pays a latency spike while it spins back up. The idle window is a service-side setting rather than a documented guarantee, so design the UX to tolerate a slow first call rather than coding to a specific number.
- **No UnityEngine:** Modules run on standard .NET, not in the player. The `UnityEngine` namespace is NOT
  available. A `.ccmu` cloud assembly is Editor-only and compiled by Unity, so its dependencies must be
  available to the Editor; a `.ccmr` solution can pull NuGet packages directly.
- **Push messages:** Server-to-client push messages can only be sent from **modules**, not from JavaScript scripts.
- **.NET version:** modules target .NET 8 on Unity before 6.7 and on the local Cloud Code server; on
  Unity 6.7+ they can target .NET 10. Legacy `.ccmr` solutions support up to .NET 10.

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

There are two forms. **Prefer building the callbacks first and passing them in**, so every handler is
attached before the subscription goes live:

```csharp
var callbacks = new SubscriptionEventCallbacks();
callbacks.MessageReceived += OnMessageReceived;        // Action<IMessageReceivedEvent>
callbacks.MessageBytesReceived += OnBytesReceived;     // Action<byte[]>
callbacks.ConnectionStateChanged += OnStateChanged;    // Action<EventConnectionState>
callbacks.Kicked += OnKicked;                          // Action
callbacks.Error += OnError;                            // Action<string>

var subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
```

The parameterless overload is a convenience wrapper that calls the same method with a fresh, empty
`SubscriptionEventCallbacks`. You then attach handlers to the returned object's `Callbacks` property —
which means a message arriving between the subscription going live and your `+=` running has no handler to
reach:

```csharp
var subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync();
subscription.Callbacks.MessageReceived += OnMessageReceived;      // anything before this line is missed
```

Both overloads exist for `SubscribeToProjectMessagesAsync` too.

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

Triggers let Cloud Code endpoints run automatically in response to events from other UGS services, without any client call. Configured in the Unity Dashboard, the Deployment Window (`.tr` files, via `com.unity.services.tooling`), or `unity ugs tr` (see `references/ugs-cli.md`) -- not in client code.

Which services emit trigger events, and under what event names, is service-side configuration rather than
anything in this SDK — it changes independently of the package. Treat the following as illustration and
confirm the current set in the Dashboard's trigger editor, which lists what is actually available to your
project:

| Emitter | Example trigger events |
|---|---|
| Authentication | Player signed in, account deleted |
| Scheduler | Cron-based or one-shot scheduled events |
| Leaderboards | Score submitted, leaderboard reset |
| Cloud Save | Data saved, data deleted |

---

## Creating a Cloud Code Module (`.ccmu`)

A Cloud Code module is authored **inside the Unity project**. Its cloud code lives in an Editor-only
assembly that Unity compiles with the rest of the project, and the client bindings are generated at
compile time. There is no solution to build, nothing to publish, and no NuGet restore.

### Requirements

- Unity **6000.5** or newer.
- `com.unity.services.cloudcode` **3.0.0** or newer.
- **Api Compatibility Level set to `.NET Standard 2.1`** in Player Settings. Any other level makes the
  module compile fine and then fail to deploy with "Unsupported API Compatibility Level".

### Create it

Right-click a folder under `Assets/` -> **Create > Services > Cloud Code Module Script** and fill in the
creation window. An agent without computer-use tools asks the user to run this menu step; with them, it
runs the step itself. The rest of this section describes what the Editor generates for you, so you can
recognise and check the result. It is not a list of files to write by hand.

Always use the menu for a new module, because it writes the module's `.ccmu` asset along with both
assemblies, and the Deployment window lists only modules that have a `.ccmu`. A module built from
hand-written asmdefs alone compiles and generates bindings, but never appears in the Deployment window,
and nothing reports why.

If an existing module is missing from the Deployment window, check for its `.ccmu`. When there is none,
repair the module by adding one rather than rebuilding it, because that keeps every assembly name, GUID
and script reference:

1. Stop until the user confirms a rollback point, such as a commit in their version control or a backup
   copy of the project.
2. Write `<ModuleName>.ccmu` in the folder that holds both assembly folders, following
   [`.ccmu` File Format](#ccmu-file-format).
3. Verify: the `.ccmu` imports with zero console errors, and the module is listed in
   **Services > Deployment** under the cloud asmdef's name. If either check fails, restore the rollback
   point and read the console error before trying again.

### Directory Structure

The module is a folder named after the module. The `.ccmu` sits at its root, one level **above** both
assemblies:

```
Assets/<AnyFolder>/
  MyModule/
    MyModule.ccmu                <-- the module asset
    Cloud/
      MyModule.asmdef            <-- Editor-only cloud assembly; its name IS the module name
      MyCloudCodeScript.cs       <-- [CloudCodeFunction] entry points
      CloudSetup.cs              <-- you add this; only if an endpoint injects IGameApiClient
    Client/
      MyModuleClient.asmdef      <-- ships in the player; must reference the cloud assembly
      MyCloudCodeScriptClient.cs <-- [CloudModuleClient] partial; bindings generated into it
```

The module folder can live anywhere under `Assets/`. **No `~` suffix** -- unlike `.ccmr`, the cloud
assembly is meant to be compiled by Unity, so hiding it from the importer would break the module.

This is the layout the Editor creates. Keep it, and in particular **leave the `.ccmu` in the folder it was
created in**. The Editor treats a folder holding a `.ccmu` as one that already contains a module, so moving
the file can allow a second module to be created on top of this one. If a project already keeps its `.ccmu`
somewhere else, leave that alone rather than "fixing" it.

### `.ccmu` File Format

Plain JSON, two GUIDs, nothing else. It carries no code, no name and no path -- it exists only to pair the
two assemblies. The create menu writes it for a new module; write it by hand only to repair an existing
module that lacks one:

```json
{
  "cloudAssemblyDefinitionGuid": "<guid from Cloud/MyModule.asmdef.meta>",
  "clientAssemblyDefinitionGuid": "<guid from Client/MyModuleClient.asmdef.meta>"
}
```

**Never invent these values.** They are Unity asset GUIDs: Unity mints one per asset on import and stores
it in that asset's `.meta` file. Copy each one out of the `guid:` line of the matching `.asmdef.meta` --
the cloud assembly's for `cloudAssemblyDefinitionGuid`, the client assembly's for
`clientAssemblyDefinitionGuid`:

```yaml
# Cloud/MyModule.asmdef.meta
fileFormatVersion: 2
guid: <this value>
```

A GUID identifies an asset in one project only, so a value taken from anywhere else -- another project,
another module, a sample, an example in documentation -- resolves to nothing. Both keys are required; a
`.ccmu` naming a GUID Unity cannot resolve fails to import and logs an error.

Creating the module through the Editor writes the file with the right values already in it.

### Deployed Module Name

**The module name is the cloud assembly's `name` field, not the `.ccmu` filename.** That is the name the
generated bindings pass to `CallModuleEndpointAsync` and the name the module deploys under. The filename
is used only as a fallback, when the asmdef cannot be read.

The creation window keeps the three in sync: module name `MyModule` produces `MyModule.ccmu`,
`MyModule.asmdef` (cloud) and `MyModuleClient.asmdef` (client). **Leave those names as generated.** Renaming
a module means renaming the cloud assembly, and that changes the deployed module name and every call site
along with it.

### Cloud Assembly (`Cloud/MyModule.asmdef`)

Editor-only, no engine references, not auto-referenced. It must reference Cloud Code Core and Apis; the
source generator reference is what emits the client bindings.

```json
{
    "name": "MyModule",
    "rootNamespace": "",
    "references": [
        "Unity.Services.CloudCode.Apis",
        "Unity.Services.CloudCode.Core",
        "Unity.Services.CloudCode.SourceGenerator"
    ],
    "includePlatforms": [ "Editor" ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_6000_5_OR_NEWER" ],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`includePlatforms: ["Editor"]`, `noEngineReferences: true` and `autoReferenced: false` all matter: they keep
server-only code out of player builds, out of every other assembly's global scope, and away from
`UnityEngine` types that do not exist on the Cloud Code host.

**Nothing validates them at import or deploy time, though.** Get one wrong and the module still imports,
still compiles, and still deploys — it is simply wrong in ways you find out later. The checks that do exist:

| When | What is checked | How it surfaces |
| :-- | :-- | :-- |
| `.ccmu` import | only that both asmdef GUIDs resolve | console error, asset fails to import |
| **Create > Services > Cloud Code Module Script** into an existing module's `Cloud/` folder | Editor-only, no engine references, not auto-referenced, references Core + Apis | modal dialog: *"This Cloud Code module's server Assembly Definition (.asmdef) is missing or misconfigured..."*, and the script is not created |
| Deploy | nothing about the asmdef flags; the only structural check is that two modules do not share a cloud assembly | per-item error status on each offending module in the Deployment window, logged as *"Assembly Failure -- Multiple CCMs reference the same assembly definition"* |

So the asmdef flags are effectively enforced only when you add a script through the menu. A hand-edited
module is on the honour system — worth checking by eye, because nothing else will. (Deploy
does make other checks unrelated to the asmdef — Api Compatibility Level and whether the project compiles —
covered under [Deploying via the Deployment Window](#deploying-via-the-deployment-window).)

### Client Assembly (`Client/MyModuleClient.asmdef`)

This is the assembly that ships in your game. It **must reference the cloud assembly**, which looks wrong at
first glance: the cloud assembly is Editor-only, so how can a runtime assembly that gets compiled into a
player depend on it?

- **In the Editor**, the reference is what lets the source generator see the cloud class and read its
  `[CloudCodeFunction]` methods, and what makes `[CloudModuleClient(typeof(MyCloudCodeScript))]` resolve.
  Without it there is nothing to generate bindings from.
- **In a player build**, the Editor-only cloud assembly is not linked — so the generator emits an **empty
  stub type** for each bound cloud class, purely so the `typeof(...)` in the attribute still resolves. The
  stub carries no members.

The upshot: your server code never ships to players. What ships is the generated bindings plus an empty
shell of the cloud class name. You do not write or manage the stubs; they exist only in non-Editor
compilation.

The asmdef itself is an ordinary runtime one — no `includePlatforms`, engine references allowed:

```json
{
    "name": "MyModuleClient",
    "rootNamespace": "",
    "references": [
        "MyModule",
        "Unity.Services.CloudCode",
        "Unity.Services.CloudCode.SourceGenerator"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### Cloud Script (`Cloud/MyCloudCodeScript.cs`)

A plain class — no `.csproj`, no publish profile.

```csharp
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace MyModule.Cloud
{
    // The scope determines how state is persisted. See Module State and Persistence below.
    [StateScope(Scope.Player)]
    public class MyCloudCodeScript
    {
        [CloudCodeFunction(nameof(SayHello))]
        public string SayHello(string name) => $"Hello, {name}!";

        [CloudCodeFunction(nameof(GetScoreAsync))]
        public async Task<int> GetScoreAsync() => await Task.FromResult(1);
    }
}
```

The generated namespaces derive from the **cloud** assembly name: `<CloudAsmName>.Cloud` for cloud scripts
and `<CloudAsmName>.Client` for client scripts.

### Injected Services (`[CloudCodeService]`)

A module gets its services in one of two ways — constructor injection and marked method parameters. Both
are supported and both generate correct client bindings; the one exception is `IExecutionContext`, which
must always be a method parameter (see below).

**Constructor injection.** The class takes its services in a constructor; the runtime resolves them from the
DI container. The class must have **exactly one public constructor**, or instantiation fails with
`Invalid constructor: type X must have exactly one public constructor`. Constructor parameters are not
endpoint parameters, so they never reach the generated client:

```csharp
public class Rewards
{
    readonly IGameApiClient _client;
    public Rewards(IGameApiClient client) => _client = client;

    [CloudCodeFunction(nameof(Grant))]
    public Task<int> Grant(string rewardId) { ... }   // client sees: Grant(string rewardId)
}
```

**Method-parameter injection.** The service is a parameter on the endpoint itself. Here — and *only* here —
it must carry `[CloudCodeService]`:

```csharp
[CloudCodeFunction(nameof(Grant))]
public Task<int> Grant([CloudCodeService] IGameApiClient client, string rewardId) { ... }
```

The attribute tells the generator "this is injected, not caller data". It has no bearing on constructor
parameters.

**Mark every injected parameter.** An unmarked parameter is treated as caller data: it lands in the
generated client as an argument the caller has to supply, and is sent in the request payload. That is
almost never what you want for a service — `IExecutionContext` in particular is a server-side interface
the client cannot construct, so the generated client is unusable. No diagnostic fires; an unexplained
error inside a generated `.g.cs` is the signal.

**`IExecutionContext` must be a `[CloudCodeService]` method parameter** — a constructor asking for it
fails. Other services are fine in the constructor, and that is the tidier form when every endpoint uses
them. Keep per-invocation values out of the constructor: the instance is cached per scope, so whatever it
captures outlives a single call.

The runtime provides these without any registration:

| Service | Gives you |
| :-- | :-- |
| `IExecutionContext` | player ID, project ID, environment, service token, session |
| `ILogger<T>` | logging |
| `ITimerService` | register and cancel server-owned timers that call back into the module |
| `IPushClient` | send push messages to a player or the whole project |

Anything else — `IGameApiClient` above all — must be registered in `CloudSetup.cs` (below) before it can be
injected. Private helper methods are ordinary C# and need no attribute; only `[CloudCodeFunction]`
endpoints do.

### Registering Services (`Cloud/CloudSetup.cs`)

A class implementing `ICloudCodeSetup` registers what `[CloudCodeService]` can inject. Required once per
module if any endpoint injects `IGameApiClient`; omit the file entirely if none do.

```csharp
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace MyModule.Cloud
{
    public class CloudSetup : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.AddGameApiClient();
        }
    }
}
```

Use the `config.AddGameApiClient()` extension. The older
`config.Dependencies.AddSingleton(GameApiClient.Create())` still appears in legacy `.ccmr` modules, but
`GameApiClient.Create()` is `[Obsolete]` — its own message directs you to `AddGameApiClient()`.

### Client Script (`Client/MyCloudCodeScriptClient.cs`)

An empty `partial` class carrying `[CloudModuleClient(typeof(CloudClass))]`. The source generator fills in
the other half at compile time. Do not add members here -- they will collide with the generated partial.

```csharp
using Unity.Services.CloudCode;

namespace MyModule.Client
{
    [CloudModuleClient(typeof(global::MyModule.Cloud.MyCloudCodeScript))]
    public partial class MyCloudCodeScriptClient
    {
    }
}
```

### Adding Another Script to an Existing Module

Right-click the module's **`Cloud/` folder** -> **Create > Services > Cloud Code Module Script**. The same
menu item detects the existing module and creates the cloud/client pair inside it instead of starting a
new module. Creating a script in the `Client/` folder is rejected -- always start from `Cloud/`.

---

## Module State and Persistence

A module class annotated `[StateScope]` keeps state between calls. The runtime holds one instance per
scope — per player, or per multiplayer session — and every change to a persisted member is saved for you.
Saving is guaranteed, but not necessarily complete by the time a call returns, so do not treat "the call
finished" as "the state is on disk".

```csharp
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

[StateScope(Scope.MultiplayerSession)]
public class TurnTimer
{
    public bool GameOver;                       // public members persist
    public int TurnCount;
    public List<string> History = new();

    [CloudCodeSerializeProperty]
    private string _secretAnswer;               // private, but persisted

    [CloudCodeIgnoreProperty]
    public int CallsThisSession;                // public, but not persisted

    readonly ILogger<TurnTimer> _logger;        // not persisted; injected by the constructor below

    // Services are injected through the constructor or a [CloudCodeService] parameter -- never into a
    // bare field. A field nothing assigns stays null.
    public TurnTimer(ILogger<TurnTimer> logger) => _logger = logger;

    [CloudCodeFunction("PlayTurn")]
    public void PlayTurn(string move) { TurnCount++; }
}
```

| Scope | Instance per | Typical use |
| :-- | :-- | :-- |
| `Scope.Player` | player | inventory, progression, per-player counters |
| `Scope.MultiplayerSession` | session | turn order, shared board, match state |
| *(no `[StateScope]`)* | none — stateless | validation, rewards, calls to other services |

### Targeting an instance from the client

A `.ccmu` client targets an instance through its constructor: the generated client takes the player ID or the session ID that its cloud class's `[StateScope]` needs (see [Generated Client Bindings](#generated-client-bindings)), and passes the matching `CloudCodeScope` for you.

```csharp
// Scope to a multiplayer session: the generated client takes the session ID
var client = new TurnTimerClient(sessionId);
await client.PlayTurn(move);
```

`CloudCodeScope` and `ScopeType` (namespace `Unity.Services.CloudCode`) are what the generated client sends on your behalf.

#### `ScopeType` Enum

| Scope Type | Value | Description |
|---|---|---|
| `MultiplayerSession` | 1 | Scoped to a multiplayer session ID |
| `Player` | 2 | Scoped to the calling player |

**There is no default scope.** The `scope` argument defaults to `null`, which is sent as "no scope", not as
`Player`. A function on a `[StateScope]` class called without a scope is rejected server-side with
*"Function's ... class in module ... requires a scope and must be called with a scope"*. Conversely a
non-scoped function called *with* a scope is rejected too, and a mismatched scope
(`Player` against a `[StateScope(Scope.MultiplayerSession)]` class) fails the same way. The generated client
gets this right on its own — it passes the scope its cloud class declares — which is one more reason to always call a `.ccmu` module through it.

---


### What persists

- **public members persist** — fields and properties alike
- `[CloudCodeSerializeProperty]` persists a non-public member
- `[CloudCodeIgnoreProperty]` excludes a public one
- anything else — plain private fields — is not persisted

**Accessibility decides this, not what the member holds.** The serializer asks only: is it ignored, is it
explicitly marked, is it public? There is no exemption for service types. So a service or logger assigned to
a **public** field *is* persisted, with two consequences worth avoiding:

- Its object graph is walked and written into your scope state — which for `IExecutionContext` means the
  service token, and for `IGameApiClient` means client configuration.
- Persisted members are written back when state is loaded, so the value the constructor injected is overwritten by
  whatever came out of storage — and an interface cannot be reconstructed from the stored JSON.

Keep injected services and loggers in **private** fields, which are excluded by default:

```csharp
readonly IGameApiClient _client;                  // private: not persisted
readonly ILogger<MyModule> _logger;
```

If one genuinely has to be public, exclude it explicitly:

```csharp
[CloudCodeIgnoreProperty]
public IGameApiClient Client;                     // public, but excluded
```

`[CloudCodeIgnoreProperty]` is checked before `[CloudCodeSerializeProperty]`, so ignore always wins.

"Not persisted" is not the same as "reset every call". The runtime keeps one instance per scope and can
reuse it across invocations. Do not build on that: a non-persisted field may hold its value across
invocations but can be discarded at any point. Keep anything that must survive in a persisted member.

### Custom serialization

Implementing `IStateSerializer` on the module class replaces the default serialization entirely:

```csharp
public byte[] OnSerialize();
public void OnDeserialize(byte[] input);
```

The runtime checks the module instance for the interface and uses it when present, so this works because
the class you author is the class that gets deployed. Reach for it when the default serialization is
unworkable — large binary blobs, custom compression, or a schema you want to version by hand.

### Evolving a persisted class

Persisted state outlives the code that wrote it. A deployed module reads records written by every
earlier version of itself, so the shape of a `[StateScope]` class is a storage contract, not just a
class declaration. Nothing warns you: the module compiles, deploys and runs, and the damage shows up
later as a value that is gone, a list that keeps growing, or a scope that no longer loads.

| Change to a persisted class | What happens to already-persisted records |
| :-- | :-- |
| **Add** a member | Keeps its field initializer (or the value the constructor set). Loading only overwrites members that are present in the stored record. |
| **Rename** a member | Treated as a remove plus an add: the stored value is stranded and the new member keeps its initializer. |
| **Remove** a member | Value is dropped on the next write. |
| **Retype** a member | A compatible stored value converts (a number into a `string` field). An incompatible one (`"abc"` into an `int`, an object into a number) throws on load, and every call for that scope fails with `CloudCodeExecutionScopeStateException` until the state is fixed. |
| **Rename the class** | Instance state is keyed on the class name, so a rename orphans that scope's entire state — every field, not one. |
| **Change the `Scope`** | Different instance key. The old state is unreachable. |

**Give a new member the initializer you want; it applies to old records.** A new `bool AllowSpectators = true;`
reads `true` for every player or session that existed before the change, because the instance is
constructed first and the stored record only overwrites the members it contains.

**Initialize persisted collections empty.** Loading adds stored items to the collection the
initializer already created, so `public List<string> Tags = new() { "starter" };` gains another
`"starter"` on every load and save. A `HashSet` or `Dictionary` does not duplicate, but its
initializer entries come back after the player's state removed them. Declare persisted collections
as `= new();` and add starting items from code the first time the state is set up.

**Design the state so migration stays cheap:**

- Keep the persisted surface small and explicit. Private fields plus `[CloudCodeIgnoreProperty]` on
  anything derived or cached means fewer members that have to be migrated later.
- Put game state in one payload class, versioned as a whole, rather than a wide set of loose fields
  on the module class.
- Never reuse a member name for a new meaning, and never renumber or reorder an enum that is
  persisted — the stored value is what comes back.
- Do not hide the real schema inside a hand-serialized string. A single opaque `string` field keeps
  every one of these problems and moves them into your own serializer.

**Migrate with a version envelope and a chain of forms.** Implement `IStateSerializer`, stamp a
schema version alongside the payload, and let each version own the single hop to the next. A module
never writes a migration between two arbitrary versions: it deserializes whichever form was
persisted and calls `Migrate` until it reaches the latest.

```csharp
using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Core;

public interface ISchema
{
    ISchema Migrate();   // next form, or null when this is the latest
}

public class SchemaV1 : ISchema
{
    public string Name = "";
    public ISchema Migrate() => new SchemaV2 { CharacterName = Name };
}

public class SchemaV2 : ISchema
{
    public string CharacterName = "";
    public ISchema Migrate() => null;
}

[StateScope(Scope.Player)]
public class PlayerProfile : IStateSerializer
{
    const int SchemaVersion = 2;

    public SchemaV2 Data = new();

    public byte[] OnSerialize()
    {
        var envelope = new JObject
        {
            ["SchemaVersion"] = SchemaVersion,
            ["Data"] = JObject.FromObject(Data),
        };
        return Encoding.UTF8.GetBytes(envelope.ToString(Formatting.None));
    }

    public void OnDeserialize(byte[] input)
    {
        // Nothing stored for this scope -- keep the instance as constructed.
        if (input == null || input.Length == 0)
            return;

        var envelope = JObject.Parse(Encoding.UTF8.GetString(input));
        var version = envelope["SchemaVersion"]?.Value<int>() ?? 1;
        // A record written before this serializer existed is the bare v1 member set, with no envelope.
        var payload = envelope["SchemaVersion"] == null ? envelope : (JObject)envelope["Data"];

        ISchema schema = version switch
        {
            1 => payload.ToObject<SchemaV1>(),
            2 => payload.ToObject<SchemaV2>(),
            // State written by a newer version cannot be migrated backwards, and running on a
            // default payload would silently discard the player's data.
            _ => throw new Exception($"cannot read schema version {version}, this module writes {SchemaVersion}"),
        };

        while (schema?.Migrate() is { } next) schema = next;

        Data = schema as SchemaV2 ?? throw new Exception($"chain from version {version} did not end at SchemaV2");
    }
}
```

`JObject` needs `Newtonsoft.Json.dll` in the **cloud** asmdef's `precompiledReferences`. The generated
asmdef ships that array empty with `overrideReferences: true`, so the reference has to be added by hand —
without it the `Newtonsoft.Json` namespace does not resolve in the module assembly at all:

```json
    "overrideReferences": true,
    "precompiledReferences": [ "Newtonsoft.Json.dll" ],
```

Adding a `SchemaV3` later means writing `SchemaV2.Migrate`, bumping `SchemaVersion`, adding a `3` case
to the switch and changing both the `Data` field type and the final type check to `SchemaV3`. The
earlier hops do not change. The default serializer stores the persisted members as a JSON object
keyed by member name, so a record written before the envelope existed is that bare v1 payload. Keep the old
`SchemaV*` classes — deleting one breaks every record still stored in that form. A hop is also where
a value is repaired rather than defaulted: a list that did not exist in v1, a cosmetic an older
record claims the player owns.

**Verify against what is actually stored, not against the class.** Before changing a deployed
`[StateScope]` class, look at what real records hold. There is no API or CLI that reads a module's
server-side state, so add a temporary read-only `[CloudCodeFunction]` that returns only the members
you need to check, deploy it to a non-production environment, call it once for a known player or
session, and remove it afterwards. A deployed function is callable by clients, so never return
secret state such as `_secretAnswer`. This shows the values after deserialization, not the stored
format.

## Generated Client Bindings

`.ccmu` and `.ccmr` both give you a strongly-typed client instead of raw `CallModuleEndpointAsync`, but
they generate it in completely different ways. Do not mix the two flows.

### `.ccmu` -- generated at compile time

You do not write either half of this. The creation flow generates the cloud script and its client partial,
and the client partial already carries `[CloudModuleClient(typeof(CloudClass))]`. From there the source
generator emits `<ClientClass>.g.cs` on every compile. Nothing to click, nothing to deploy first, no
`GeneratedModuleBindings/` folder.

The constructor depends on the cloud class's `[StateScope]`:

| Cloud class | Constructor |
| :-- | :-- |
| `[StateScope(Scope.Player)]` | `new MyScriptClient(playerId)` |
| `[StateScope(Scope.MultiplayerSession)]` | `new MyScriptClient(sessionId)` |
| *(no `[StateScope]`)* | `new MyScriptClient()` |

Each also has an overload taking an `ICloudCodeService`, for tests or a non-default instance.

```csharp
var client = new MyCloudCodeScriptClient(AuthenticationService.Instance.PlayerId);
var score = await client.GetScoreAsync();
```

The generated methods return `Task`/`Task<T>` and pass the **cloud assembly name** as the module name, so
a rename of that assembly changes the deployed endpoint the client calls.
An `Access.Service` function gets no client method, since only Cloud Code itself (timers, for example)
can call it; calling it from the client is a compile error, not a runtime `403`.

**Types in endpoint signatures.** Declare a class or enum that an endpoint takes or returns once, in the cloud
assembly, and use that type from client code. The generator emits a data-only stub of it, with the same name and
namespace, into the client assembly, in the Editor and in player builds. The game assembly sees the stub through the client assembly and never references the Editor-only cloud assembly. A class is reduced to its public instance fields and properties that have both a public getter and a public setter, and each one is emitted on the stub as a `{ get; set; }` property. Get-only, computed, `readonly` and private-setter members, non-public members, indexers, statics, consts, methods, constructors and initializers are not copied. The generator warns only about public members it leaves out because they are read-only (CCSG216) or computed (CCSG229); non-public, static, const and indexer members are skipped silently. Declare a model type as plain data with public settable members, and do not declare a second copy on the client. A type from any other assembly is referenced directly and is not stubbed.

### `.ccmr` -- generated on demand from the deployed module

The generator reads the *deployed* module's metadata and writes one file per class under
`Assets/CloudCode/GeneratedModuleBindings/`. Deploy first, then generate. Do not edit those files -- they
are overwritten on regeneration.

- Select the `.ccmr` in the **Project window**, then click **Generate Bindings** in the Inspector, or
- **Services > CloudCode > Generate All Modules Bindings** to regenerate every `.ccmr` at once.

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

**Known limitations (`.ccmr`):** no distinction between required/optional parameters; tuples are not
supported.

---

## Legacy Modules (`.ccmr`) - Maintenance Only

**On Unity 6000.5+, do not create new `.ccmr` modules**; see
[Creating a Cloud Code Module](#creating-a-cloud-code-module-ccmu) instead. This section covers reading,
fixing and redeploying the `.ccmr` modules a project already has, and creating a new one on an older Editor,
which cannot author `.ccmu`.

A legacy module is a .NET class library (.NET 8, up to .NET 10) living outside `Assets/`, built and published by the Deployment
Window, and referenced from Unity by a `.ccmr` file.

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

### `.csproj` (net8.0, CloudCode.Apis 0.0.27, CloudCode.Core 0.0.7)

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <Configurations>Debug;Release</Configurations>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Com.Unity.Services.CloudCode.Apis" Version="0.0.27" />
        <PackageReference Include="Com.Unity.Services.CloudCode.Core" Version="0.0.7" />
        <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="7.0.1" />
    </ItemGroup>

    <PropertyGroup>
        <PublishReadyToRunComposite>true</PublishReadyToRunComposite>
    </PropertyGroup>

</Project>
```

### `FolderProfile.pubxml` (linux-x64, SelfContained=false)

Cloud Code runs on Linux. `SelfContained=false` keeps the output small -- the runtime is provided by the
Cloud Code host.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishDir>bin\Release\net8.0\publish\</PublishDir>
    <PublishProtocol>FileSystem</PublishProtocol>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
    <PublishSingleFile>False</PublishSingleFile>
  </PropertyGroup>
</Project>
```

### `ModuleSetup.cs` (registers GameApiClient)

Required once per module if any endpoint needs `IGameApiClient`. This is what the Editor's `.ccmr`
template generates:

```csharp
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.AddGameApiClient();
    }
}
```

An older module may instead contain `config.Dependencies.AddSingleton(GameApiClient.Create())`. That still
works, but `GameApiClient.Create()` is `[Obsolete]` and its own message points at `AddGameApiClient()`, so
prefer the form above when you touch the file.

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
        <TargetFramework>net8.0</TargetFramework>
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

### Migrating a `.ccmr` to `.ccmu`

Create a `.ccmu` module and move each `[CloudCodeFunction]` class into its `Cloud/` folder. `[StateScope]`
and the function bodies carry over unchanged, and so does constructor injection — with one exception. What
changes:

| `.ccmr` | becomes |
| :-- | :-- |
| `.sln`, `.csproj`, `.pubxml`, test project | deleted -- Unity compiles the cloud assembly |
| constructor-injected `IGameApiClient` / `ILogger<T>` | works as-is; the class needs exactly one public constructor |
| constructor-injected `IExecutionContext` | **must move** to a `[CloudCodeService]` method parameter -- it is a scoped service and cannot be constructor-injected here |
| bare `IExecutionContext` **method parameter** | same parameter, now marked `[CloudCodeService]` |
| `ModuleSetup.cs` with `GameApiClient.Create()` | `CloudSetup.cs` with `config.AddGameApiClient()` |

NuGet `PackageReference`s do not survive either: a `.ccmu` compiles under Unity, so a dependency has to be
available to the Editor. Keep the module name stable (name the cloud asmdef after the old `.ccmr`) so the deployed module keeps its name, and replace every
`CallModuleEndpointAsync` call site with the new module's generated client.

---

## Code Templates

JavaScript scripts: maintain, deploy and call existing ones; write new server logic as a C# module.

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

### Call a Cloud Code Module (`.ccmu`, generated client)

```csharp
// Cloud class PlayerProgress is [StateScope(Scope.Player)], so the client takes a player ID.
var progress = new PlayerProgressClient(AuthenticationService.Instance.PlayerId);
var result = await progress.AwardXp(50);

Debug.Log($"Total XP: {result.TotalXp}");
```

### Call a Module Endpoint (Typed Result, raw)

For legacy `.ccmr` modules only. A `.ccmu` module is always called through its generated client, as above.

```csharp
// Module class name: "ProgressionModule", method: "AwardXp"
var result = await CloudCodeService.Instance.CallModuleEndpointAsync<ProgressResult>(
    "ProgressionModule", "AwardXp",
    new Dictionary<string, object> { { "amount", 50 } });
Debug.Log($"Total XP: {result.TotalXp}");
```

### Subscribe to Player Messages (Full Example)

```csharp
ISubscriptionEvents subscription;

async Task SubscribeToMessages()
{
    // Attach every handler before subscribing, so nothing can arrive unhandled.
    var callbacks = new SubscriptionEventCallbacks();
    callbacks.MessageReceived += OnMessageReceived;
    callbacks.ConnectionStateChanged += state =>
        Debug.Log($"Subscription state: {state}");
    callbacks.Error += error =>
        Debug.LogError($"Subscription error: {error}");
    callbacks.Kicked += () =>
        Debug.LogWarning("Kicked from subscription");

    subscription = await CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
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
2. All `.js`, `.ccmu` and `.ccmr` files under `Assets/` are listed. **Select only what you mean to deploy**
   -- deploying everything can overwrite modules that are not ready to go out.
3. A `.ccmu` deploys the compiled cloud assembly, under the **cloud asmdef's name**. Its client bindings
   already exist -- they were generated when Unity compiled the module, so there is nothing to regenerate.
4. A `.ccmr` builds and publishes its solution. After it deploys, generate or regenerate its bindings
   (see [Generated Client Bindings](#generated-client-bindings)).

**The local Cloud Code server never deploys during play mode.** Entering play mode does not deploy modules to
it, and nothing is deployed to it while the Editor is in play mode. Edit and deploy in edit mode, then
enter play mode. Changes made during play mode do not reach the local server until you exit play mode and
deploy again.

A `.ccmu` deploy fails with **"Unsupported API Compatibility Level"** unless the project's Player Settings
have Api Compatibility Level on `.NET Standard 2.1`. It also fails if the project has compile errors --
the module is deployed from its compiled assembly, so it has to build first.

**Deploying a `.ccmu` needs a running Editor.** Nothing packages a `.ccmu` outside the Editor, and
`unity ugs deploy` skips `.ccmu` (see `references/ugs-cli.md`), so a CI pipeline that deploys `.ccmu` modules has
to run the Editor. Only legacy `.ccmr` modules deploy without one, through their `.sln`; do not create a
`.ccmr` to get around this.

**Local development needs no deploy.** While the local Cloud Code server runs (Cloud Code button on the
Editor toolbar > **Start Local Server**; it needs an active environment), each successful compile redeploys
changed `.ccmu` modules onto it, outside Play mode. **Nothing deploys to the local server during Play mode**,
not even an explicit deploy, which is refused: exit Play mode before recompiling and deploying. A
`404 function-not-found` for an endpoint added while playing means exactly this; exiting Play mode redeploys
it and logs `Local Cloud Code server reloaded`.

**Known limitation.** An unfocused Editor doesn't notice file edits or run the local auto-deploy until it
regains focus. After editing module code without focusing the Editor, run `unity command recompile`, then
deploy with the `Deploy.cs` script from
`references/deployment.md`: while the local
server runs, the deploy targets it, not the environment. Stop the server before deploying remotely.

To deploy without the window, run the deploy script in
the Trigger Deployment Programmatically section of `references/deployment.md`.

---

## Running the Local Cloud Code Server

The local server runs the project's modules in a process on this machine, and the SDK routes
`CallModuleEndpointAsync` to it instead of the cloud. Start and stop it with the Cloud Code button on the
Editor toolbar (**Start Local Server** / **Stop Local Server**), outside Play mode; it needs Unity 6000.3+.
Redeploy on compile, and what to do when the Editor is unfocused, are covered in
[Deploying via the Deployment Window](#deploying-via-the-deployment-window).

The server listens on port **14750** by default. To run several Editors, each with its own local server,
give each project a distinct port in **Edit > Project Settings > Services > Cloud Code > Local Debugging >
Port** while its server is stopped; a port already in use fails startup with *"Server Port <port> is not
available."*
Play mode reads the port and whether a server is running from Editor preferences shared by every Editor on
the machine, so calls go to the most recently started server, and stopping any Editor's server sends the
others' calls to the cloud.

### It will not start until an active environment is set

Startup validates the environment first, and a project with no active environment fails with:

> Cannot start the local Cloud Code server. The environment has not been set. Please use the Environment
> Selector, found under Edit → Project Settings → Services → Environments, to set it.

A fresh clone or a CI checkout has no active environment, so this is usually the first thing that fails.
Set one in **Edit > Project Settings > Services > Environments** (ask the user, or drive the Editor UI).

---

## Error Handling

```csharp
try
{
    var result = await client.MyFunction(arg);
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

Read the `CloudCodeExceptionReason` API reference for the installed package before writing a handler that
branches on `ex.Reason`, because the set of reasons changes between package versions:

`https://docs.unity3d.com/Packages/com.unity.services.cloudcode@<VERSION>/api/Unity.Services.CloudCode.CloudCodeExceptionReason.html`

Take `<VERSION>` from the installed `com.unity.services.cloudcode` version in
`Packages/packages-lock.json` (for `3.0.0`, use `3.0`). That file lists the package even when it arrives as
a dependency of another package, which `Packages/manifest.json` does not. If a reason the code needs is missing from that page, the installed version does not
have it.

---

## Error Messages

| Error string | Cause | Fix |
| :-- | :-- | :-- |
| `Unsupported API Compatibility Level` | The project's Api Compatibility Level is not `.NET Standard 2.1` | Set it in Player Settings, then redeploy the `.ccmu` |
| `Invalid constructor: type X must have exactly one public constructor` | The module class has no public constructor or several | Keep exactly one public constructor |
| `Function's ... class in module ... requires a scope and must be called with a scope` | A `[StateScope]` function was called without a scope, or with the wrong one | Pass a `CloudCodeScope` that matches the class's `[StateScope]`, or use the generated client |
| `This Cloud Code module's server Assembly Definition (.asmdef) is missing or misconfigured...` | The cloud asmdef is not Editor-only, has engine references, is auto-referenced, or lacks the Core and Apis references | Fix the asmdef, then create the script again |
| `Assembly Failure -- Multiple CCMs reference the same assembly definition` | Two modules share one cloud assembly | Give each module its own cloud assembly |
| `Server Port <port> is not available.` | Another process, often another Editor's local server, holds the port | Set a distinct port under Local Debugging > Port while the server is stopped |
| `Cannot start the local Cloud Code server. The environment has not been set...` | The project has no active environment | Set one in **Edit > Project Settings > Services > Environments** |
| `CloudCodeExecutionScopeStateException` | Persisted state no longer deserializes after an incompatible member change | Restore a compatible member type, or migrate with `IStateSerializer` |
