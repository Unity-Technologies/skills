# CloudBehaviours Reference

Server-authoritative game logic authored as a C# class, with the client bindings — RPCs, synchronized
state, push events — generated for you. Part of `com.unity.services.cloudcode`, requiring Unity 6000.5+.

Read this when the goal needs cloud-owned state that clients read but cannot write: shared match state,
per-player secret state, server-owned timers. For request/response server logic with nothing to
synchronize, plain Cloud Code modules are generally available and the better default.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Anti-Hallucination Reference](#anti-hallucination-reference)
- [The authoring model](#the-authoring-model)
- [What `public` means on the cloud class](#what-public-means-on-the-cloud-class)
- [Scopes](#scopes)
- [Deployed names](#deployed-names)
- [Generated code is not editable](#generated-code-is-not-editable)
- [Client bindings](#client-bindings)
- [Per-viewer state with `[SyncView]`](#per-viewer-state-with-syncview)
- [Calling another behaviour](#calling-another-behaviour)
- [Timers](#timers)
- [State, serialization, and model types](#state-serialization-and-model-types)
- [Endpoint access control](#endpoint-access-control)
- [Local development, debugging, and deployment](#local-development-debugging-and-deployment)
- [Generator diagnostics](#generator-diagnostics)
- [Silent failure modes](#silent-failure-modes)

## Prerequisites

| Requirement | Detail |
| :-- | :-- |
| `com.unity.services.cloudcode` **`3.1.0-exp.1`**+ | Confirm in the project with **Create > Services > Cloud Behaviour Script** in the create menu |
| Editor is 6000.5 or newer | every CloudBehaviours assembly is gated on `UNITY_6000_5_OR_NEWER` |
| Project is linked to a Unity Cloud project | required to deploy, and to authenticate at runtime |

`Scope.MultiplayerSession` additionally needs `com.unity.services.multiplayer` installed. Player-scoped
behaviours do not. One module can hold CloudBehaviours and native Cloud Code classes side by side.

If the create-menu entry is missing, say so and do not write CloudBehaviour code: the installed version
does not carry CloudBehaviours and no amount of correct code will compile against it. Fall back to a plain
stateful module unless the user asks for the experimental package.

## Anti-Hallucination Reference

| Correct | Incorrect (do NOT use) |
|---|---|
| `[StateScope(Scope.Player)]` / `[StateScope(Scope.MultiplayerSession)]` on the cloud class | any other class attribute to mark a behaviour -- `[StateScope]` is the only one |
| cloud class inherits `CloudBehaviour` | a plain class with only `[StateScope]` -- that is a native module, not a behaviour |
| `public int Round;`, `public int Round { get; set; }` or `public int Round { get; private set; }` -- all synchronize | a get-only or computed property -- ignored with warning `CCSG113`; expose a computed value with `[SyncView]` |
| `public` members reach clients; `private` ones never leave the cloud | assuming access control hides a `public` field, or that a `private` one is synchronized |
| `[CloudCodeService] ITimerService timers` as a method parameter | an unannotated `ITimerService` parameter -- build error `CCSG136` |
| `await self.EndTurn(arg).ScheduleAsync(delay)` on the injected `I{Behaviour}` | assembling an endpoint-name string by hand to schedule work |
| `[CloudCodeService] ITimerService` + `GetTimerAsync(id)` to cancel | expecting a cancel on `ICall` -- it has none |
| `await other.Endpoint(arg).ForScope(scopeId)` to invoke another scope instance now | `ScheduleAsync` for a cross-scope call -- it only ever targets the executing instance |
| `[CloudCodeAccess(Access.Service)]` on a timer target | leaving it callable by clients because "only the timer calls it" |
| `Access.Service` endpoints called only from behaviours, via `I{Behaviour}` | calling one from the client -- the generated client has no method for it |
| `Context.ServiceToken` inside a timer callback | `Context.PlayerId` -- it is `null`; a timer runs for no player |
| `[SyncView("Hand")] internal List<Card> ProjectHand(string playerId)` | a `private` projection -- build error `CCSG142` |
| client partial: `OnEnable` -> `EnableClient()`, `OnDisable` -> `DisableClient()` | hand-writing `EnableClient`, `Hydrate`, an RPC, or a synchronized property -- they are generated |
| client calls the generated method: `await behaviour.PlayTurn(move)` | `CallModuleEndpointAsync("Behaviour", "PlayTurn", args)` from a behaviour's client |
| `Synchronized`, `ScopeChanged`, `Fatal`, `Error`, `MessageReceived` on the generated bindings | any other event name for connection or state callbacks |
| read synchronized fields in a `ScopeChanged` handler -- the client hydrated before raising it | calling `Hydrate()` on connect or reconnect -- it already ran, and yours is a second RPC |
| `List<T>`, `T[]`, `Dictionary<,>` across the boundary | a custom `IEnumerable<T>` type -- compile-time error |
| model types carrying only fields and properties | methods or constructors on a model -- not regenerated client-side; a computed or get-only property is error `CCSG131` |
| behaviour class names unique within a module | two behaviours sharing a simple name across namespaces -- generator error |

## Creating the module

Always create a new module with **Create > Services > Cloud Behaviour Script**, because the creator writes
the module's `.ccmu` asset along with both assemblies, and the Deployment window lists only modules that
have a `.ccmu`. A module built from hand-written asmdefs alone compiles and generates bindings, but never
appears in the Deployment window, and nothing reports why. An agent without computer-use tools asks the
user to run this menu step; with them, it runs the step itself.

The first behaviour in a folder opens the Cloud Behaviour Module Creator, which writes
`<ModuleName>.ccmu` at the module root and scaffolds two assemblies:

| Folder | Assembly references | Platform |
| :-- | :-- | :-- |
| `Cloud/` | `Unity.Services.CloudCode.Core`, `Unity.Services.CloudCode.Apis`, `Unity.Services.CloudBehaviours.Editor`, `Unity.Services.CloudCode.SourceGenerator`, `Newtonsoft.Json.dll` | Editor only, `noEngineReferences` |
| `Client/` | `Unity.Services.CloudBehaviours.Runtime`, `Unity.Services.Core`, `Unity.Services.CloudCode`, `Unity.Services.CloudCode.SourceGenerator`, **the cloud assembly**, and for `Scope.MultiplayerSession` also `Unity.Services.CloudBehaviours.Multiplayer.Runtime` + `Unity.Services.Multiplayer` | all |

Two entries in that `Client/` row are easy to get wrong, and both fail as `CCSG127` plus a wall of
`CS0103` on the client partial:

- **The client assembly must reference the cloud assembly**, even though the cloud assembly is
  Editor-only. This is correct and does not break player builds: the generator emits stubs of the cloud
  types into the client assembly, so nothing Editor-only is carried into a player.
- **A session-scoped behaviour needs the two multiplayer assemblies on the *client* assembly**, because
  the generated client addresses its state through a `MultiplayerScopeProvider`. Installing
  `com.unity.services.multiplayer` is not sufficient on its own — the references have to be there too.

Use the table to check what the creator generated. The creator also wires the cloud reference, which a
hand-written client asmdef usually misses. The split matters: the cloud assembly has no engine
references, so `UnityEngine` types are unavailable server-side, and the generator needs both assemblies to
reference it.

If the user already has hand-written assemblies and the module is missing from the Deployment window, the
module is missing its `.ccmu`. Add one instead of rebuilding the module, because a new `.ccmu` leaves every
assembly name, GUID and script reference as it is:

1. Stop until the user confirms a rollback point, such as a commit in their version control or a backup
   copy of the project, so a failed repair has a clean way back.
2. Check the cloud asmdef against the `Cloud/` row of the table above and add any missing references. The
   create menu refuses to add behaviours to a module whose cloud assembly lacks them.
3. Write `<ModuleName>.ccmu` in the folder that holds both assembly folders, in the format and with the
   GUID rules in the `.ccmu` File Format section of `references/cloud-code-modules.md`.
   Take each GUID from the `guid:` line of the matching `.asmdef.meta` in this project, never from
   anywhere else, because a GUID resolves only in the project that minted it.
4. Verify: the `.ccmu` imports with zero console errors, and the module is listed in
   **Services > Deployment** under the cloud asmdef's name. If either check fails, restore the rollback
   point and read the console error before trying again.

## The authoring model

What you write, and what the generator writes for you:

1. **The creation flow scaffolds two assemblies** — a `Cloud/` assembly (Editor-only, no engine
   references) and a `Client/` assembly, each referencing `Unity.Services.CloudCode.SourceGenerator`.
2. **You implement the behaviour on the cloud side**: a class inheriting `CloudBehaviour` with a
   `[StateScope]`, public methods for the operations, public fields for the state clients should see, and
   private members for everything that stays server-side.
3. **The source generators run on every compile** and emit two halves from that one class:
   - the **Cloud Code module** itself — a `[CloudCodeFunction("Behaviour__Method")]` per public method,
     the `Behaviour_Hydrate` endpoint, and the synchronization that pushes state to clients in scope.
     This is what gets deployed and what the service runs.
   - the **client bindings** — the other half of your `partial` client class, carrying the RPC methods
     (except `Access.Service` endpoints, which clients cannot call),
     the read-only synchronized properties and the `UnityEvent`s your game code calls and subscribes to.
4. **Your game client uses the bindings** like any other component: enable it, call the generated
   methods, react to `Synchronized` and to your own push events.

Neither generated half appears under `Assets/`; both are compiler output, regenerated on each compile.

```csharp
// Cloud/MatchState.cs — cloud assembly, runs in Cloud Code
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudBehaviours;
using Unity.Services.CloudCode.Core;

[StateScope(Scope.MultiplayerSession)]
public class MatchState : CloudBehaviour
{
    public int Round;                                  // synchronized to every client in scope
    private List<string> _deck;                        // stays in the cloud

    public event Action<string> RoundEnded;            // push channel

    public string PlayTurn(string move)                // RPC
    {
        Round++;
        Logger.LogDebug("Round {Round}", Round);       // Logger comes from the base class
        RoundEnded?.Invoke(Context.PlayerId);          // Context is IExecutionContext
        return "ok";
    }
}
```

```csharp
// Client/MatchStateClient.cs — client assembly
using Unity.Services.CloudBehaviours;
using UnityEngine;

[CloudBehaviour(typeof(MatchState))]
public partial class MatchStateClient : MonoBehaviour
{
    protected virtual void OnEnable() => EnableClient();
    protected virtual void OnDisable() => DisableClient();

    async void OnMoveButton()
    {
        var result = await PlayTurn("rock");            // generated RPC
    }
}
```

Write only `OnEnable`/`OnDisable` and your own logic. `EnableClient`, `DisableClient`, `Hydrate`, the
RPC methods, the synchronized properties, and the `UnityEvent`s are all generated — implementing any of
them by hand collides with the generated partial.

## What `public` means on the cloud class

Accessibility is the opt-in mechanism, which is why secrets simply stay `private`:

| Cloud class member | Becomes | On the client |
| :-- | :-- | :-- |
| `public` method | RPC | `async Task` / `async Task<T>` method |
| `public` field (non-static, non-const, non-readonly), or `public` property with a public getter and a setter of any accessibility | synchronized variable | `public T Name { get; private set; }` |
| `public event Action`, `Action<…>`, `EventHandler`, `EventHandler<T>` | push channel | `UnityEvent` / `UnityEvent<…>` |
| method with `[SyncView("name")]` | per-viewer projection | read-only property named `name` |
| parameter with `[CloudCodeService]` | injected service | omitted from every caller-facing signature — the client RPC and `I{Behaviour}` alike |

Non-public members are invisible to clients and to the generator.

Two traps live in that table:

- **A service parameter needs `[CloudCodeService]`.** `Task StartTurn(ITimerService timers)` fails the build with `CCSG136`, because a Cloud Code service or generated `I{Behaviour}` can't arrive in a request payload. Annotate every injected service.
- **Read-only members don't synchronize.** A get-only or computed property, or a `readonly` field, is ignored with warning `CCSG113`; a `public static` or `const` member is skipped with warning `CCSG158`. Declare synchronized state as a public field or a property with a setter (`{ get; private set; }` is fine), expose a computed value through a `[SyncView]` method, and mutate a collection's contents rather than reassigning a read-only one. The client still sees it as a read-only property.

## Scopes

`[StateScope(...)]` binds the instance's state and decides who may call it.

| Attribute | State | Default access | Use for |
| :-- | :-- | :-- | :-- |
| `[StateScope(Scope.Player)]` | per player, persistent | `Access.Player` | inventory, progression, inboxes |
| `[StateScope(Scope.MultiplayerSession)]` | per session, shared by all members | `Access.SessionMember` | turn state, shared match state |

`[StateScope]` is **mandatory**. A behaviour without one is `CCSG126`, an error that aborts generation
for the whole module — there is no stateless or non-scoped CloudBehaviour. (Plain Cloud Code functions
do default to `Access.Global` when unscoped; behaviours do not work that way.) For stateless
request/response work, use a plain Cloud Code module instead.

New scripts are scaffolded as `Scope.Player`. Persistent private state → `Player`; shared volatile
state → `MultiplayerSession`. Override per endpoint with `[CloudCodeAccess(...)]` — see
[Endpoint access control](#endpoint-access-control).

## Deployed names

The generator emits one Cloud Code function per endpoint, and these strings appear in logs, in the
Deployment window, and anywhere a function name is passed as a string (timers, for instance):

| Thing | Deployed name |
| :-- | :-- |
| RPC `Method` on behaviour `B` | `B__Method` |
| Hydration | `B_Hydrate` (single underscore) |
| State sync push | `B__Synchronize` |
| Push event `Ev` | `B__Ev` |

Two behaviours in one module may declare the same *method* name — bindings resolve by full type identity.
Their **class** names must differ, though, even across namespaces: the generated `{module}Extensions`
aggregator derives `Add{Name}Client` and short-name type references from the simple name, so duplicates
are reported as an error.

Names must also be unique *within* a behaviour, across kinds. An endpoint, a synchronized member, an
event, and a `[SyncView]` projection all share one wire key namespace, and the comparison is
case-insensitive, so `Score` and `score` collide. Overloads collide for the same reason — one wire key,
two methods. A same-kind duplicate keeps the first declaration with warning `CCSG117`, which shows up as a member
that mysteriously does not exist on the client. Members of different kinds sharing a name have no
survivor, so all of them are dropped and the build fails with error `CCSG118`. Event payloads are
limited to four parameters.

## Generated code is not editable

The generator runs on every compilation and emits sources the compiler holds in memory — they never
appear under `Assets/` and are not checked in. To regenerate, recompile. When generated code looks
wrong, fix the cloud class or the bindings class; there is nothing to edit in the output.

### Reading the generated sources

Not editable is not the same as not inspectable, and reading them is often the fastest way to close the
loop on "the client says that member does not exist". Roslyn can dump every generator's output to disk,
but the setup has three traps, so follow it exactly.

**1. Put the `csc.rsp` next to the `.asmdef`, never at the `Assets/` root.** A root `csc.rsp` is applied
to every assembly in the project, packages included. Roslyn names output files by generator, not by the
assembly being compiled, so dozens of parallel compiles write the same paths and fail with `CS0016`. Add
one file per assembly you want to read, each naming its own output folder:

```
# Assets/MyGame/Client/csc.rsp   (beside MyGame.Client.asmdef)
/generatedfilesout:Generated/MyGame.Client
```

**2. Create that folder before you compile.** The path is relative to the project root, not `Assets/`.
Neither Unity nor Roslyn creates it; Roslyn only creates the per-generator subfolders beneath it. Without
it, every assembly using the rsp fails with:

```
error CS0016: Could not write to output file '<project>\Generated\...\AssemblyMonoScriptTypes.generated.cs'
  -- 'Could not find a part of the path ...'
```

The message names a generated file, so it reads like a generator bug. It is a missing folder. If the rsp
was global, `Unity.Pipeline.CodeGen` is among the casualties and `unity status` reports no Pipeline
instance while the Editor is open in front of you; delete the rsp to recover.

**3. Keep the folder outside `Assets/`.** Unity would import the dumped `.cs` files and compile them again.

Recompile, and the output lands under the folder you named:

```
Generated/
  MyGame.Client/
    CloudCodeSourceGenerator/
      SourceGenerator.Generators.BehaviourBindingsGenerator/    <-- client partial + its inspector
        MyGame.Client.MatchStateClient.g.cs
        MyGame.Client.MatchStateClient_Inspector.g.cs
        MyGame.Client.MatchState_Sync.g.cs
  MyGame.Cloud/
    CloudCodeSourceGenerator/
      SourceGenerator.Generators.BehaviourManifestGenerator/    <-- the deployed cloud module
        MyGame.Cloud.IMatchState.g.cs
        MyGame.Cloud.MatchState_System.g.cs
        MyGame.Cloud.MatchState_Sync.g.cs
        MyGame.Cloud.__MatchStateCcClient.g.cs
        MyGameExtensions.g.cs
```

The file name is the hint name — `{Namespace}.{Type}.g.cs` — so a member you expected on the client is
either in `…MatchStateClient.g.cs` or it was never generated, which is the whole diagnosis. Read the
bindings file to see what the client actually got, and the manifest file to see the endpoint names and
the `_Sync` payload the cloud will push.

This is build output: it is rewritten on every compile, editing it changes nothing, and it does not
belong in source control. It is also a trap for everyone else: if the `csc.rsp` is committed and
`Generated/` is ignored, the folder does not exist on a fresh clone and the project fails to compile for
anyone but its author. Either keep the `csc.rsp` files uncommitted, or commit a `.gitkeep` in each output
folder and ignore only the contents:

```
Generated/**/*.cs
```

## Client bindings

### What the generator emits

For `[CloudBehaviour(typeof(TCloud))] public partial class MyClient : MonoBehaviour`, the generated half
of the partial contains:

```csharp
[field: SerializeField] internal string TargetSessionType { get; private set; }  // MultiplayerSession scope — or:
[field: SerializeField] internal string TargetPlayerId { get; private set; }     // Player scope

public CloudCodeScope Scope { get; }       // the scope provider's current scope
public bool IsClientEnabled { get; }

public UnityEvent ScopeChanged;
public UnityEvent<CloudBehaviourException> Fatal;
public UnityEvent<CloudBehaviourError> Error;
public UnityEvent<IMessageReceivedEvent> MessageReceived;
public UnityEvent Synchronized;

public void EnableClient();
// MultiplayerSession scope also: EnableClient(string sessionType), EnableClient(ISession),
//                                EnableClient(SessionObserver), EnableClient(MultiplayerSession)
// Player scope also:             EnableClient(string playerId)
public void DisableClient();
public async Task Hydrate();

// one read-only property per synchronized member:   public int Round { get; private set; }
// one UnityEvent per push channel:                  public UnityEvent<string> RoundEnded;
// one async method per client-callable RPC:         public async Task<string> PlayTurn(string move);
```

The binding creates a `PlayerScopeProvider` or `MultiplayerScopeProvider` according to the cloud
class's `[StateScope]`, so the common case needs no wiring. The provider isn't exposed; choose what to
bind to with the `EnableClient` overload instead.

### Lifecycle

```csharp
protected virtual void OnEnable() => EnableClient();
protected virtual void OnDisable() => DisableClient();
```

`EnableClient()` subscribes to the provider and starts resolving the scope; `DisableClient()` tears that
down. Calling any `EnableClient` overload twice without an intervening `DisableClient()` logs a warning and
does nothing. To rebind to a different target, call `DisableClient()` first.

RPCs need a resolved scope. Rather than firing calls immediately after enabling, drive them from user
input or from `ScopeChanged`, which raises once the provider has a scope and the client has tried to
hydrate against it. Gate state-dependent work on `Synchronized`, or on the absence of
`HydrationFailedOnConnect` (see [Hydration](#hydration)), because `ScopeChanged` also follows a failed hydrate. `Scope` itself goes non-null earlier — the moment the provider resolves — so polling
`Scope != null` hands you a scope whose synchronized fields are not populated yet.

### Choosing what to bind to

`TargetSessionType` and `TargetPlayerId` are `internal` properties with a serialized backing field, so
they are editable in the Inspector (**Target Session Type**, **Target Player ID**) but not settable from
other code. Leaving them empty is the useful default:

| Scope | Target | Empty means | Set means |
| :-- | :-- | :-- | :-- |
| `MultiplayerSession` | `TargetSessionType` | adopt the single active session; if more than one is active the binding reports `CloudBehaviourExceptionReason.AmbiguousSessions` | watch only for sessions of that type — must match `SessionOptions.Type` used at create/join/matchmaking |
| `Player` / none | `TargetPlayerId` | follow the signed-in player | bind to that specific player id |

To choose the target from code, call the matching overload instead: `EnableClient(sessionType)`,
`EnableClient(ISession)`, `EnableClient(SessionObserver)` or `EnableClient(MultiplayerSession)` for
session scope, and `EnableClient(playerId)` for player scope. An overload replaces any target set in the
Inspector.

### Reacting to state and events

Synchronized members arrive as read-only properties; `Synchronized` raises after each applied update
(an RPC return, an incoming push, or the hydrate that runs when the scope resolves). Refreshing UI
from `Synchronized` decouples the view from individual RPCs:

```csharp
protected virtual void OnEnable()
{
    EnableClient();
    Synchronized.AddListener(RefreshUi);
    RoundEnded.AddListener(OnRoundEnded);
}

protected virtual void OnDisable()
{
    Synchronized.RemoveListener(RefreshUi);
    RoundEnded.RemoveListener(OnRoundEnded);
    DisableClient();
}

void OnRoundEnded(string playerId) => Debug.Log($"round ended by {playerId}");
```

Remove runtime listeners in `OnDisable`: `UnityEvent` keeps them across disable and enable, so re-enabling would add duplicates.

Because these are `UnityEvent`s, listeners can also be wired in the Inspector.

### Hydration

A client hydrates itself when its scope resolves: it pulls current state through the `B_Hydrate`
endpoint, applies it, and only then raises `ScopeChanged`, with `Synchronized` immediately after. A
`ScopeChanged` handler can therefore read synchronized fields directly — they are already current,
provided the hydrate succeeded (see below) — and a client that connects and does nothing else still holds server state.

If that first hydrate fails, `Error` is raised with `HydrationFailedOnConnect` *before*
`ScopeChanged`; `ScopeChanged` still follows, no `Synchronized` does, and the fields keep their
previous or default values. An `Error` handler therefore runs on a client that has not yet announced
its scope — `Scope` is already set, so calling `Hydrate()` from that handler is legal.

Every RPC return also carries the caller's fresh view of synchronized state, so a client that calls
an RPC does not need to hydrate either.

That leaves `Hydrate()` for two cases: catching up inside a live scope after a period of not
listening, and recovering from `HydrationFailedOnConnect`. A reconnect is not one of them — it
re-resolves the scope, which hydrates.

Connecting costs one hydrate RPC per scope resolution, per client. A pooled client toggled
repeatedly, or many behaviours on one player, pays it each time.

### Failures

Two events, split by whether the client survives.

`Fatal` carries a `CloudBehaviourException` whose `Reason` is one of `Unknown`, `SubscriptionError`,
`Kicked`, or `AmbiguousSessions`. The client is **already disabled** by the time handlers run —
`IsClientEnabled` is false and nothing further arrives until `EnableClient()` is called again. Do not
call it from the handler: a failure that reproduces immediately would recurse. `AmbiguousSessions` in
particular means the project has multiple active sessions and the binding cannot guess — set
**Target Session Type** in the Inspector, or call `EnableClient(sessionType)`.

`Error` carries a `CloudBehaviourError` and reports a failure the client **carries on from**: it keeps
its `Scope` and stays enabled. Read `Reason` to decide whether to retry. Today the only value besides
`Unknown` is `HydrationFailedOnConnect` — the client could not fetch server state when its scope
resolved, so synchronized fields still hold their previous or default values. Call `Hydrate()` to try
again.

```csharp
Fatal.AddListener(e => Debug.LogError($"client stopped: {e.Reason}"));
Error.AddListener(e =>
{
    if (e.Reason == CloudBehaviourErrorReason.HydrationFailedOnConnect)
        _ = Hydrate();
});
```

Both also log to the console, so an unhandled failure is not silent.

## Per-viewer state with `[SyncView]`

A plain synchronized field sends the same value to everyone in scope. When each player must see a
*different* value — their own hand, their own fog of war, their own private balance — express it as a
projection instead.

`[SyncView("wireName")]` marks a method as the synced member. The method **is** the state on the wire:
the generator emits one member named `wireName`, typed as the method's return type, and fills it per
recipient by calling the method with that player's id.

```csharp
[StateScope(Scope.MultiplayerSession)]
public class CardGame : CloudBehaviour
{
    [CloudCodeSerializeProperty]
    private Dictionary<string, List<Card>> _hands = new();   // every hand: persisted, cloud-only

    [SyncView("Hand")]
    public List<Card> ProjectHand(string playerId) =>
        _hands.TryGetValue(playerId, out var hand) ? hand : new List<Card>();
}
```

The client sees `public List<Card> Hand { get; private set; }` holding only that player's cards. The
unfiltered dictionary never leaves the cloud, which is the point: filtering client-side would ship every
hand to every player.

### Requirements

- Signature is `TWire Method(string playerId)` — one `string` parameter, non-`void` return.
- The method must be `public` or `internal`. A `private` projection is build error `CCSG142`, so
  `internal` is the tidy choice: visible to the generator, invisible outside the assembly. A `[SyncView]` method never becomes an RPC even when public, because
  projections are partitioned out before endpoints are collected.
- It must be synchronous and non-generic: `async`, `Task`-returning, generic or `static` methods are not
  captured.
- The `name` argument is required and non-empty; it is the member name clients see, so it should read
  like a field (`"Hand"`, not `"ProjectHand"`).
- The `playerId` passed in is always the id of the player the payload is being built for — the viewer
  about to receive this value, not whoever triggered the call. On an RPC return that is the caller, on a
  push it is each recipient in turn, and on `Hydrate()` it is the player asking. Build the value for
  *that* player and nothing else.
- When the invocation has no calling player — a timer callback, or a cross-behaviour call — the
  **return value** carries an empty envelope rather than calling projections with an empty id. This is
  the RPC return only. Pushes are unaffected: they are built per recipient, so each one still has a real
  player id and projections resolve normally. See
  [What a timer callback still pushes](#what-a-timer-callback-still-pushes).
- Keep the method side-effect free. It runs once per recipient on every synchronization, so mutating
  state there produces surprising repeated writes.
- The projection runs for the calling player's own view as well, so a caller sees their projected value
  in the RPC return, not the raw state.

### Sending to specific players

Per-viewer control applies to synchronized state. A declared event is pushed to everyone in scope, and a
check inside the method decides only whether the event fires, not who receives it. When only some players
should hear about something, push to each of them with
`Push.SendPlayerMessageAsync(Context, message, messageType, playerId)` instead of invoking an event.

## Calling another behaviour

The generator emits an `I{Behaviour}` interface for every behaviour in the module, with one member per
endpoint. A member does not invoke anything — it returns an unexecuted **`ICall`** (or `ICall<T>` when
the endpoint returns a value) describing the invocation. Inject the interface with `[CloudCodeService]`,
name the endpoint with its real arguments, then choose how to run it:

| Execution mode | Runs | Against | Returns |
|---|---|---|---|
| `ScheduleAsync(delay)` | later, after `delay` | the scope instance that is **executing** | `Task<string>` — the timer's id |
| `ForScope(scopeId)` | immediately | another instance of the **target's** scope | `Task`, or `Task<T>` carrying the endpoint's return value |

The two are alternatives. A described call handed to neither fails the build with `CCSG133`.

Use `ScheduleAsync` for timers ([Timers](#timers)). Use `ForScope` for everything else: one behaviour
invoking another right now, including across different scopes.

> **Do not call `ScheduleAsync` on another behaviour's `I{Behaviour}`**: it fails the build with `CCSG132`.
> `ScheduleAsync` is only for deferring **your own** behaviour's endpoints.
>
> To defer work against another behaviour, schedule one of your own endpoints and do the cross-scope
> call from inside it:
>
> ```csharp
> // in MatchRoom
> public async Task EndMatchSoon([CloudCodeService] IMatchRoom self) =>
>     await self.FlushScores().ScheduleAsync(TimeSpan.FromSeconds(30));   // own endpoint: fine
>
> [CloudCodeAccess(Access.Service)]
> public async Task FlushScores([CloudCodeService] ILeaderboard leaderboard)
> {
>     foreach (var entry in _scores)
>         await leaderboard.SubmitScore(entry.Value).ForScope(entry.Key);  // cross-scope: ForScope
> }
> ```

### Cross-scope invocation with `ForScope`

`ForScope(scopeId)` targets a specific instance of the callee's scope. The `scopeId` is a player id or a
multiplayer session id, matching the target behaviour's own `[StateScope]` — not the caller's.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudBehaviours;
using Unity.Services.CloudCode.Core;

[StateScope(Scope.Player)]
public class Leaderboard : CloudBehaviour
{
    public int BestScore;

    [CloudCodeAccess(Access.Service)]            // no client method is generated; a behaviour can call it
    public int SubmitScore(int score)
    {
        if (score > BestScore) BestScore = score;
        return BestScore;
    }
}

[StateScope(Scope.MultiplayerSession)]
public class MatchRoom : CloudBehaviour
{
    [CloudCodeSerializeProperty] private Dictionary<string, int> _scores = new();

    [CloudCodeAccess(Access.SessionHost)]
    public async Task EndMatch([CloudCodeService] ILeaderboard leaderboard)
    {
        foreach (var entry in _scores)
        {
            // entry.Key is a player id: the scope id of that player's Leaderboard instance
            var best = await leaderboard.SubmitScore(entry.Value).ForScope(entry.Key);
            Logger.LogInformation($"{entry.Key} best is now {best}");
        }
    }
}
```

`MatchRoom` is session-scoped and `Leaderboard` is player-scoped, so each call names the player whose
instance it should land on. Awaiting an `ICall<T>.ForScope(...)` gives that endpoint's return value
directly — the callee's synchronized members are not part of what the caller receives.

### Requirements

- **Both behaviours must live in the same module** (the same cloud assembly). `I{Behaviour}` and the
  `{module}Extensions` registrations are generated per module. If the target is in another module, this
  route does not exist — call it as a plain Cloud Code module instead.
- **`[CloudCodeService]` on the injected parameter is mandatory.** Without it the build fails with
  `CCSG136`.
- **Injected parameters are not part of the interface.** The generator strips every
  `[CloudCodeService]` parameter from `I{Behaviour}`, exactly as it does from the client bindings, so
  name only the endpoint's real arguments. Do not pass `null` as a placeholder for an injected slot —
  there is no slot. An endpoint declared
  `Task FlushScores(int round, [CloudCodeService] ILeaderboard leaderboard)` is called as
  `self.FlushScores(round)`.
- **Lock the target down.** A callable endpoint is callable by clients too. Anything only a behaviour
  should invoke wants `[CloudCodeAccess(Access.Service)]` — a cross-behaviour call arrives as a service
  call, so the restriction blocks clients while letting the caller through, and the generated client has
  no method for it. See
  [Endpoint access control](#endpoint-access-control).
- **Expect no calling player.** A service invocation has no player behind it: `Context.PlayerId` is
  `null` and there is no player access token. Pass what the endpoint needs as arguments, and use
  `Context.ServiceToken` for outbound service calls.
- **Class names must be unique within the module**, even across namespaces.

## Timers

Timers are server-authoritative: the cloud schedules a call back into one of the behaviour's own
endpoints, so a player who closes the game cannot dodge a turn clock.

Scheduling uses the same generated `I{Behaviour}` interface described in
[Calling another behaviour](#calling-another-behaviour): inject it, name the endpoint with its real
arguments, and run the resulting `ICall` with `ScheduleAsync(delay)`. That defers the invocation on the
scope instance that is executing.

```csharp
using System;
using System.Threading.Tasks;
using Unity.Services.CloudBehaviours;
using Unity.Services.CloudCode.Core;

[StateScope(Scope.MultiplayerSession)]
public class TurnClock : CloudBehaviour
{
    public string CurrentTimerId;                        // persisted, so it survives to the next call
    public int TurnNumber;

    [CloudCodeAccess(Access.SessionHost)]
    public async Task StartTurn([CloudCodeService] ITurnClock self)
    {
        CurrentTimerId = await self.EndTurn(TurnNumber).ScheduleAsync(TimeSpan.FromSeconds(30));
    }

    [CloudCodeAccess(Access.Service)]
    public void EndTurn(int turnNumber)                  // the scheduled endpoint
    {
        if (turnNumber != TurnNumber) return;            // a stale timer fires against an old turn
        TurnNumber++;
    }

    [CloudCodeAccess(Access.SessionHost)]
    public async Task CancelTurn([CloudCodeService] ITimerService timers)
    {
        var timer = await timers.GetTimerAsync(CurrentTimerId);
        timer.Cancel();
    }
}
```

Arguments are ordinary typed parameters on the endpoint — no dictionary of loosely-typed values, and no
function-name string to assemble. Under the hood `ScheduleAsync` registers a timer against the deployed
endpoint for you.

### Cancelling

`ScheduleAsync` returns the timer's id. `ICall` has no cancel of its own, so keep injecting
`[CloudCodeService] ITimerService` for that: `GetTimerAsync(id)` returns the timer, which exposes
`RemainingTime()` and `Cancel()`. Persist the id if you will ever need it — a later invocation gets a
fresh instance.

### Lock the target down, and don't expect a player

A scheduled endpoint is still an endpoint, so a client can call it directly. Restrict it with
`[CloudCodeAccess(Access.Service)]`: a timed invocation arrives as a service call, so the timer still
fires while clients are blocked. The generated client has no method for it, so a stray client call is a
compile error.

That same identity difference shapes what the endpoint can assume when a timer runs it:

- `Context.PlayerId` is `null` — the invocation is not on behalf of anyone, so branching on the "calling
  player" does not work. Pass what you need as arguments, or read it from state.
- there is no player access token; use `Context.ServiceToken` for outbound service calls.
- session data is refreshed before it runs, so `Context.Session.PlayerIds` reflects who is in the session
  at that moment — handy for skipping players who left.

### What a timer callback still pushes

A timer runs for no player, but clients still see what it changes. A timer-triggered invocation pushes
to the same recipients as any other call: every session member for `Scope.MultiplayerSession`, and the
owner for `Scope.Player`.

- **Synchronized state is pushed** to each recipient, with each payload built against that recipient's
  id, so `[SyncView]` projections resolve per viewer as they do on a client call. The service fills
  `Session.PlayerIds` for a session-scoped behaviour whatever the endpoint's access level, so a
  service-token invocation still reaches every member.
- **Declared events are pushed** to the same recipients, carrying a full synchronization envelope.
- **Only the RPC return value is empty**, and only when the behaviour has projections. Nothing reads the
  return of a timer invocation anyway.

A value that fails to arrive after a timer fired is not explained by the timer.

### Limits and patterns

- The delay must be **greater than zero**.
- Each scope instance (one player, or one session) may hold at most **10 active timers**; registering
  an eleventh throws. Cancelling frees the slot, so cancel the ones you no longer need rather than
  letting them pile up.
- The delays of all active timers on a scope instance may add up to at most **24 hours** in
  production, so no single timer can be longer than that.
- There is no recurring timer. For a repeating clock, have the scheduled endpoint schedule the next one,
  keeping exactly one alive at a time.
- **Only schedule your own behaviour's endpoints.** Calling `ScheduleAsync` on another behaviour's
  `I{Behaviour}` fails the build with `CCSG132`. See [Calling another behaviour](#calling-another-behaviour) for the
  pattern that works.

## State, serialization, and model types

### Two separate rules: sync and persistence

Whether a member reaches clients and whether it survives to the next invocation are decided
independently:

- **Reaches clients** — determined purely by `public` accessibility.
- **Persists between invocations** — public members persist by default; two attributes override it, and
  both apply to fields and properties:

| Attribute | Effect |
| :-- | :-- |
| `[CloudCodeSerializeProperty]` | persists a non-public member — durable state the client never sees |
| `[CloudCodeIgnorePropertyAttribute]` (`[CloudCodeIgnoreProperty]`) | keeps a public member out of persistence while it still synchronizes |

```csharp
[CloudCodeSerializeProperty]
private string _correctAnswer;      // survives invocations, never sent to clients

[CloudCodeIgnoreProperty]
public int FramesSinceStart;        // synced for display, not worth persisting
```

Only serialized members survive: the runtime may rehydrate the instance between calls, so anything held
in a plain private field is gone by the next invocation.

Custom serialization with `IStateSerializer` isn't supported on a CloudBehaviour yet, and implementing it
is error `CCSG157`. Don't suggest it for state migration; use the attributes above and keep saved members
backward compatible.

A persisted data type can't hide an inherited member unless both are properties of the same type. The
server can't save two members with one name, so that pair is error `CCSG156`. Rename one, or add
`[CloudCodeIgnoreProperty]` to the one that doesn't need saving.

`CCSG156` can't see non-public members of a type compiled in another assembly, such as a shared asmdef.
There, an `internal` or `private` member that hides a public one isn't reported, and every save of the
behaviour fails at runtime. Don't hide inherited members in shared data types.

Synchronized state is a public field or a public property with a public getter and a setter of any
accessibility, the same accessor shapes that persist. A `readonly` field or a get-only or computed property is
ignored with warning `CCSG113`; use a `[SyncView]` method for a computed value. A `public static` or
`const` member is skipped with warning `CCSG158`. Data **types** crossing the
boundary are stricter: a member that can be read but not assigned there is error `CCSG131`.
`[CloudCodeSerializeProperty]` and `[CloudCodeIgnoreProperty]` still apply to both fields and properties
for persistence purposes.

### Manual synchronization

`AutoSynchronize` is `true` by default. After each endpoint returns, Cloud Code pushes the updated
synchronized state to the other clients in the scope, once per call however many fields changed, so a
single endpoint never needs to turn it off.

Turn it off to batch changes from several calls into one push. `AutoSynchronize` is saved with the
behaviour's state, so setting it to `false` keeps it off for every later call in that scope, from any
player, until an endpoint sets it back to `true`. End the batch by setting it to `true` in the last call;
that call's normal push then carries everything:

```csharp
public void BeginPlacement()
{
    AutoSynchronize = false;      // later calls change state without pushing
}

public void PlaceShip(int x, int y)
{
    Ships.Add(new Ship { X = x, Y = y });
}

public void LockPlacement()
{
    Locked = true;
    AutoSynchronize = true;       // this call's push carries the whole batch
}
```

Rules:

- Write the endpoint that sets `AutoSynchronize` back to `true` in the same change that sets it to
  `false`. Left off, state still saves and each caller still gets it in the RPC return, but no other
  client receives a synchronization push, so the game looks frozen to everyone else.
- Events still push during a batch, and each one carries the full synchronized state, so raising an
  event mid-batch sends the batch's changes early.
- Re-enable it on paths that abandon the batch too, such as a player leaving mid-placement or a round
  timing out. The endpoint that starts the next round, or a timer callback, can do it.
- To push partway through a call, for example before a slow step, call `SynchronizeAsync()` and leave
  `AutoSynchronize` on. The end-of-call push still follows.
- Don't initialise the field to `false`. That makes the behaviour fully manual, and every endpoint that
  should reach other clients has to call `SynchronizeAsync()` itself.

The calling client always receives current state in the RPC return, whatever `AutoSynchronize` is set to.

Risks:

- The flag belongs to the scope instance, not to the call or the player. One player's endpoint turning it
  off stops the end-of-call push of every endpoint of that behaviour, for every player in the scope, and it
  stays off across restarts of the scope because it is saved with the rest of the state. Only events and
  explicit `SynchronizeAsync()` calls still push.
- Nothing warns when it is left off. The symptom is other clients no longer receiving updates from
  endpoints unrelated to the batch, while each caller still sees current state in its own RPC return. When
  pushes stop, look for an endpoint that set `AutoSynchronize = false` and a path that never set it back.
- Read-only endpoints cannot opt out of synchronization. An endpoint that changes nothing still pushes the
  full synchronized state to every push recipient other than the caller: every other member of a
  `Scope.MultiplayerSession`, or the owner of a `Scope.Player` scope when someone else calls it. An owner
  calling its own player scope sends nothing. Don't turn `AutoSynchronize` off to avoid the push: it is the
  same permanent switch. Accept it, and keep read-only session endpoints off hot paths, for example by
  rate-limiting how often the client calls them.

Who receives the push depends on scope. For `Scope.MultiplayerSession` the synchronization message goes
to `Context.Session.PlayerIds`, so session members see each other's changes without calling anything.
The caller is left out because the RPC return already gave it the state.

A player-scoped behaviour's only client is its owner, so every push goes to the owner. When the owner
calls, the RPC return carries the state, so no synchronization push is sent, and events still reach the
owner. When a timer, a service call or another player's `Global` call changes the state, the owner gets
the synchronization push and the events. A caller who isn't the owner gets only the RPC return. Don't
design a player-scoped behaviour as though other clients will be pushed to.

For a raw push outside synchronization, `SendMessageAsync(messageType, payload)` sends to every session
member for `Scope.MultiplayerSession`, and to the owner for `Scope.Player`. Declared events are usually
clearer than raw messages.

### Model types

Custom classes, structs, and enums referenced from public fields, properties, endpoint parameters/returns,
or event payloads are regenerated in the client bindings assembly. The regenerated version carries the
public fields and properties (as `{ get; set; }`) and enum members — nothing else.

```csharp
// Good — data only
public class AttackResult
{
    public string TargetPlayerId;
    public int X;
    public bool WasHit;
}

// Compiles, but the method is missing on the client
public class AttackResult
{
    public bool WasHit;
    public bool IsSunk() => /* ... */;             // methods aren't regenerated
}

// Fails the build with CCSG131
public class AttackResult
{
    public bool WasHit;
    public string Summary => $"{WasHit}";          // readable but not assignable
}
```

A method is only dropped from the client stub. A member that can be read but not assigned, such as a
computed or get-only property, can't round-trip, so the build fails with `CCSG131`.

Nested types referenced by a model are generated recursively and stay nested inside their containing
type. Enum bindings preserve the underlying type (`byte`, `long`, …), so values outside `int` range are
safe. Types that share a simple name across different namespaces are generated separately.

A model type cannot inherit from a `[CloudCodeService]` type — service objects hold server-only state
that cannot be serialized.

### Unsupported types

A custom collection type — anything implementing `IEnumerable<T>` that is not `List<T>`, an array, or
`Dictionary<,>` — is a **compile-time error** when used as an endpoint parameter, an endpoint return, or a
data-type field, because it cannot round-trip through JSON. Use `List<T>` or `T[]`.

Everything crossing the boundary must be JSON-serializable: primitives, `string`, enums, plain
classes/structs, and `List<T>`/arrays/dictionaries of those.

## Endpoint access control

Every CloudBehaviour endpoint has an access level. Left alone it is derived from the class scope, which
is the right answer most of the time:

| Class scope | Resolved access |
| :-- | :-- |
| `[StateScope(Scope.Player)]` | `Access.Player` |
| `[StateScope(Scope.MultiplayerSession)]` | `Access.SessionMember` |

Resolution happens when the module loads, so the attribute is only needed where an endpoint should differ
from its class default.

```csharp
using Unity.Services.CloudBehaviours;
using Unity.Services.CloudCode.Core;

[StateScope(Scope.MultiplayerSession)]
public class Match : CloudBehaviour
{
    public void SubmitMove(string move) { }                  // Access.SessionMember (default)

    [CloudCodeAccess(Access.SessionHost)]
    public void StartMatch() { }                             // only the session host

    [CloudCodeAccess(Access.Service)]
    public void GrantReward(string playerId, int amount) { }  // only service-token callers
}
```

An `Access.Service` endpoint gets no method on the generated client binding, so calling it from game
code is a compile error rather than a runtime `403`. Behaviours still reach it through `I{Behaviour}`
(`ScheduleAsync`, `ForScope`).

### The `Access` enum

Which values are legal depends on the class's `[StateScope]`, so the enum groups by scope:

| Valid on | Value | Who may call |
| :-- | :-- | :-- |
| **any scope** | `Unspecified` | resolve from the class scope at load time (the default) |
| | `Global` | any authenticated caller |
| | `Service` | service-token callers only — other services, trusted backends, timer invocations; never a game client |
| **`Scope.Player` only** | `Player` | the player who owns the scope |
| **`Scope.MultiplayerSession` only** | `SessionMember` | any member of the session |
| | `SessionHost` | the host of the session |

An `Access` value the module cannot honour throws when the module loads rather than failing quietly, so
pass enum members and avoid casting integers into `Access`.

### Choosing a level

Reach for `Access.Service` on anything that mints value — granting currency, awarding items, adjusting
another player's state. `SessionHost` suits match flow control (start, force-advance, kick) where one
client is authoritative. Beyond that, the scope defaults are usually correct, and adding attributes that
merely restate them is noise.

Access control is not a substitute for keeping state private: an endpoint restricted to `Service` still
exposes any `public` field to every client in scope.

## Local development, debugging, and deployment

### The local Cloud Code server

Deploying to the local server is the fast loop: it runs the cloud assembly in a local process, supports
breakpoints, and lets Play mode talk to real cloud logic without a round trip to UGS.

- Configuration lives on the `CloudCodeLocalServerSettings` asset — the listening **port** (default
  **14750**) and an optional **secrets** `TextAsset`. The settings UI can create and assign a placeholder
  secrets JSON file and open it in an external editor; invalid JSON surfaces an error dialog rather than
  failing at runtime.
- A module whose source changed is **redeployed automatically** onto the running server — no stop/start
  cycle. Redeploy happens after a recompile, outside Play mode only. **Nothing deploys to the local server
  during Play mode**, not even an explicit `services_deployment_item_deploy`, which is refused. A
  `404 function-not-found` for an endpoint added while playing means the module isn't deployed yet: exit
  Play mode, wait for `Local Cloud Code server reloaded` in the Console, then re-enter. Same known limitation
  as modules: see
  the Deploying via the Deployment Window section of `references/cloud-code-modules.md`.
- The server won't start without an active services environment: see
  the Running the Local Cloud Code Server section of `references/cloud-code-modules.md`.
- On startup the server checks for port conflicts and a usable .NET runtime, and a server that dies during
  startup reports why.

### The binding Inspector

The generator also emits a custom Inspector for each bindings component, which is the fastest way to see
what a binding is actually doing at runtime. In Play mode it shows:

- a live connection-status indicator and the resolved player or session identifier — so a binding that
  never resolved a scope is visible without adding a single log line;
- the current value of every synchronized member and `[SyncView]` projection, read-only;
- `UnityEvent` slots for `ScopeChanged`, `Fatal`, `Error`, `MessageReceived`, `Synchronized`, and each
  push event, so listeners can be wired without code.

Check it before instrumenting anything. A missing member points at generation (see
[Generator diagnostics](#generator-diagnostics)); a resolved scope with stale values points at the push
path; no scope at all points at the lifecycle.

### Logging

Two independent switches:

| Where | How |
| :-- | :-- |
| Server-side, inside a behaviour | `Logger` on the base class (`Microsoft.Extensions.Logging.ILogger`) |
| Cloud Code's own Editor/authoring logging, including local server operation | **Verbose Logging** in **Project Settings > Services > Cloud Code**, backed by the `ENABLE_UNITY_CLOUD_CODE_AUTHORING_VERBOSE_LOGGING` define |
| Client-side binding traffic — scope resolution, pushes, RPC calls | add the `ENABLE_CLOUDBEHAVIOUR_LOGS` scripting define; the generated bindings' log calls are `[Conditional]` on it, so they compile out without it |

Local server infrastructure logging defaults to non-verbose, so turn Verbose Logging on before chasing a
deployment or connection problem.

### Deployment

CloudBehaviour modules appear in the **Deployment** window as Cloud Code modules and deploy from there to
the linked UGS project. A module asset is a `.ccmu` file; its inspector offers deployment actions and a
summary of the last successful deployment. The window finds modules only through their `.ccmu`, so a module
that is missing from it has no `.ccmu`. Repair it by adding one (see
[Creating the module](#creating-the-module)).

Names come from the **cloud assembly (asmdef) name**, not the `.ccmu` asset name. While the local server
runs, an explicit deploy goes to it instead of the environment, so stop it before deploying remotely.

A deployed behaviour runs in the same hosting model as any other Cloud Code module, so **cold start
applies**: after roughly 15 minutes without traffic the worker shuts down and the next call pays a
latency spike while it starts again. Budget for it in the UX — do not put a tight timeout on the first
call after an idle period, and do not treat that first slow response as a failure.

### When the generated bindings do not compile

Work through it in this order, since the later steps are rarer:

1. Recompile. Generated sources are produced per compilation; a stale error can survive an interrupted
   one.
2. Check both assemblies reference `Unity.Services.CloudCode.SourceGenerator`, and that the cloud
   assembly references `Unity.Services.CloudBehaviours.Editor`. A missing reference is the most common
   cause, and the generator's diagnostic names it.
3. Check the console for generator diagnostics — collisions between behaviour members, read-only members
   skipped for synchronization, unsupported collection types, and duplicate names are all reported there
   rather than as mystery type errors.
4. Confirm no generated member has been implemented by hand in the partial (`EnableClient`,
   `DisableClient`, `Hydrate`, an RPC, a synchronized property).
5. Confirm the editor is 6000.5+ — both scaffolded assemblies are gated behind `UNITY_6000_5_OR_NEWER`,
   so on an older editor the types simply do not exist and every reference to them fails.

## Generator diagnostics

The generator reports problems as compiler diagnostics with ids of the form `CCSG1xx` (CloudBehaviours;
`CCSG2xx` is native modules, `CCSG3xx` bindings). They surface in the Console and in the IDE like any
other compiler message, and severities can be adjusted through `.editorconfig`.

**Read them before assuming the generator is broken.** Most CloudBehaviour problems are reported, name
the offending member, and say what was kept and what was dropped.

**An error that aborts generation takes every generated member with it.** The client partial then has no
generated half, so a single `CCSG1xx` error surfaces as a pile of `CS0103 - does not exist in the current
context` on `EnableClient`, `DisableClient`, `Synchronized`, the RPCs and the synchronized properties.
Those are symptoms. Fix the `CCSG1xx` and they all go away together — editing the client partial to chase
them is wasted work.

Common ones, by what they tell you:

| Id | Situation | Severity | Fix |
| :-- | :-- | :-- | :-- |
| `CCSG126` | Behaviour class has no `[StateScope]` | error | Add `[StateScope]` to the class |
| `CCSG101` | Two behaviours in a module share a simple class name | error | Rename one behaviour so simple names are unique in the module |
| `CCSG120` / `CCSG122` | Name reserved by a generated type or framework type | error | Rename the member or class away from the reserved name |
| `CCSG115` | `Access` value incompatible with the class scope | error | Use an `Access` value valid for the class scope |
| `CCSG116` | `[CloudCodeAccess]` on something that is not an endpoint | error | Remove `[CloudCodeAccess]` or put it on an endpoint |
| `CCSG109` | `ref` / `out` parameter | error | Remove the `ref` / `out` modifier |
| `CCSG112` / `CCSG125` | Generic behaviour class, or a nested one | error | Make the behaviour class non-generic and top-level |
| `CCSG110` / `CCSG111` | Unsupported awaitable return, or `async void` | error | Return `Task`, `Task<T>` or a plain value, and never `async void` |
| `CCSG128` / `CCSG129` | Custom collection type in a signature or data-type field | error | Use `List<T>`, arrays or dictionaries instead of a custom collection |
| `CCSG131` | Data-type member that is readable but not settable, including a `readonly` field | error | Give the member a setter, or remove it from the data type |
| `CCSG132` | `ScheduleAsync` on another behaviour's `I{Behaviour}` — a timer only runs on the behaviour that scheduled it | error | Call `ScheduleAsync` only on the behaviour that owns the timer |
| `CCSG133` | A described `ICall` is never given `ScheduleAsync` or `ForScope` | error | Call `ScheduleAsync` or `ForScope` on the described `ICall` |
| `CCSG136` | Cloud Code service or generated `I{Behaviour}` parameter without `[CloudCodeService]` | error | Mark the parameter `[CloudCodeService]` |
| `CCSG142` | `[SyncView]` method that isn't `public` or `internal` | error | Make the `[SyncView]` method `public` or `internal` |
| `CCSG127` | `Scope.MultiplayerSession` client assembly missing `Unity.Services.CloudBehaviours.Multiplayer.Runtime` / `Unity.Services.Multiplayer` references — the message names both. Fires even with the package installed | error | Add the two assembly references the message names to the client assembly |
| `CCSG118` | Members of different kinds sharing a name — all of them dropped | error | Rename the members so no two kinds share a name |
| `CCSG117` | Duplicate member name within a behaviour — first declaration kept | **warning** | Rename the duplicate member |
| `CCSG119` | Member name reserved by the generated bindings class | **warning** | Rename the member away from the bindings class name |
| `CCSG113` | Read-only synchronized member ignored: a `readonly` field, or a get-only or computed property | **warning** | Give the member a setter if it must synchronize |
| `CCSG158` | `public static` or `const` member on a behaviour — not synchronized | **warning** | Move the value to a synchronized instance member if clients need it |
| `CCSG114` | Unsupported event handler type, including more than four payload parameters | **warning** | Use a supported handler type with at most four payload parameters |

The warnings matter as much as the errors: each one means a member you wrote is **not** on the wire, and
the build still succeeds. If a client member is missing, search the Console for `CCSG1` before changing
any code.

## Silent failure modes

These compile and then do nothing, which is what makes them expensive:

- **Read-only and static members don't synchronize.** Public fields and public properties with a
  setter of any accessibility sync. A `readonly` field or a get-only or computed property raises warning
  `CCSG113`; a `public static` or `const` member raises warning `CCSG158`. Either way the client property never appears.
- **Names collide across kinds, case-insensitively.** Endpoints, synchronized fields, events and
  projections share one wire-key namespace, and overloads collide for the same reason. A same-kind
  duplicate keeps the first declaration with **warning** `CCSG117`, so the build stays green while the
  dropped member is missing on the client. A cross-kind clash drops all of them and fails with error `CCSG118`.
- **Push events are never replayed**, so a late joiner sees nothing; lasting state belongs in a
  synchronized field.
- **Player-scoped behaviours push to no other clients.** State reaches that one client through RPC
  returns, the hydrate on connect, and `Hydrate()`.
- **A module without a `.ccmu` never reaches the Deployment window.** Hand-written asmdefs compile and
  generate bindings, but nothing lists the module for deployment and nothing warns. Create new modules
  through the menu, and repair an existing one by adding its `.ccmu` (see
  [Creating the module](#creating-the-module)).
- **Model types are data only.** Methods and constructors are not regenerated on the client — the stub
  carries the public fields and properties, nothing else. A computed or get-only property is error
  `CCSG131`.

When a member is missing on the client, read the console for generator diagnostics before changing code.
