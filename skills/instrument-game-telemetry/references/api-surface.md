# API surface — events, contexts, outcomes, data rules

## Contents
- Source of truth
- Contexts
- Outcomes
- Point-event families
- Custom events
- Envelope calls
- Data rules

## Source of truth

After the package is installed, the generated source is the inventory: read
`Runtime/Generated/Events.Send.cs` (every point event and its typed params),
`Runtime/Events.Spans.cs` (every context), `EventsBuilder.cs` (the `WithX` builders), and
`Collectors.cs` (collector factories and their current signatures) inside the package
folder. This file is the map of what to look for and the inventory **before** install.
It is a snapshot and the package moves: whenever this file disagrees with the package
source, the source wins without ceremony. Treat every signature below as verify-in-source
before writing it.

Generation candidate: derive this inventory from the package codegen when the pipeline
grows a docs target; until then, re-verify the snapshot stamp whenever the package version
bumps.

Snapshot: read from `com.unity.insights.events` package version `0.1.0-exp.5`. The builder surface was checked against `Runtime/EventsBuilder.cs`, the identity
behavior against `Runtime/ContextProviders/` and `Runtime/EngineIdentifiers.cs`, and the
collector factories against `Runtime/Collectors.cs`. An installed package older than that
may still carry `WithInstall`, `WithSession`, `WithProject`, `WithRegistry` or the GA
collector's `syncConsent`; an installed package's source wins either way. Source means code,
not comments: the `EventsCore.Configure()` doc example gates `GameTelemetry` behind a
consent gate, which the consent rule forbids.

## Contexts

Paired `Events.EnterX(...)` / `Events.ExitX(...)`. Every Exit carries an `Outcome`; duration
is measured by the runtime.

| Context | Wraps | Typical Enter site |
|---|---|---|
| Session | the whole foreground life | after `Apply()`, opened by the helper from the first context of any type it routes or the first point event, else by the helper's `AfterSceneLoad` fallback; one opener, never before the first scene load (Envelope calls) |
| Screen | one screen or overlay (`overlay: true` for dialogs/popups over a screen) | where the screen is shown — `EnterScreen(string screen, NavDirection navDirection = Unspecified, bool overlay = false)`; `NavDirection` is `DrillIn | Back | Replace` |
| Play | a play period that spans attempts (roguelike run, match lobby-to-result) | where play begins |
| Attempt | one attempt at a goal (a level try, a run) | where the player gains control — `EnterAttempt(string node, ...)`; `node` is the registry node id |
| Phase | a sub-segment of an attempt (wave, stage, tier segment) | the segment boundary — `EnterPhase(string phase)`; `phase` is the manifest's `phase_` id |
| AdBreak | the showing of an ad | around the ad SDK show call |
| Flow | a multi-step funnel (FTUE, checkout) | the first step; mark progress with `CompleteFlowStep(string step)` |
| ConsentFlow | the consent surface | when the surface appears |
| Cutscene | a non-interactive sequence | when interactivity is taken away |

## Outcomes

`Completed` (won / finished), `Failed` (lost / died), `Abandoned` (player backed out mid-span:
restart, return to menu), `Quit` (app quit with the span open), `Skipped`, `Cancelled`.
Pick the outcome at the call site where the reason is known — never inside a shared `Close()`
that always reports `Completed`. Beware the API's own default: most `ExitX` overloads default
the outcome to `Completed` (`ExitPlay`, `ExitPhase`, `ExitAttempt`, and `ExitAdBreak`
require one) — the default is the trap;
pass the outcome explicitly at every terminal path.

## Point-event families

Wire only the families the game's profile has. This list is a sample of the families, not
the complete catalog — the generated source carries events this file does not name (for
example a selection-changed event for loadout/equip moments); plan from the source when it
is available.

- **Account**: `SendLogin`, `SendSignUp`, `SendAccountLinked`, `SendAccountDeleted` — paired
  with the matching `WithUser` envelope change when the developer asked for account identity
  (see Envelope calls), sent alone otherwise.
- **Economy**: `SendResourceEarn`, `SendResourceSpend` — one event per transaction at the
  moment the balance changes, for every resource (coins, XP, gems), never an end-of-run
  aggregate.
- **Progression / quests**: `SendQuestStart`, `SendQuestComplete`, and the attempt/phase
  contexts above carry most progression meaning.
- **IAP**: `SendPurchaseInitiate`, `SendPurchase`, `SendPurchaseFailed`,
  `SendPurchaseCancelled`, `SendPurchaseFulfilled` — from the IAP integration's callbacks
  only. With the IAP+ SDK installed (`com.unity.purchasing` 5.4 or later) that SDK sends the
  purchase funnel itself and every collector added through the chain drops
  `purchase_initiate`, `payment_options_shown`, `purchase`, `purchase_fulfilled` and
  `purchase_failed` (one warning on the first drop; the sandbox capture file, which is not
  a chain collector, still records them); do not wire those five there. `SendPurchaseCancelled`,
  `shop_view`, `product_view`, `payment_method_selected` and the subscription events are
  unchanged. None of this family projects to GA4 unless ruling 9 unlocks it (the rulings reference).
- **Ads**: `SendAdRequested`, `SendAdLoaded`, `SendAdLoadFailed`, `SendAdImpression`,
  `SendAdRewardGranted` — from the ad SDK callbacks, with the show wrapped in
  `EnterAdBreak`/`ExitAdBreak`.
- **Consent**: `SendConsentChange(framework, surface, consentStatus)` — `framework` is the
  `ConsentFramework?` enum (`Att`, `Cmp`, `Gdpr`, `Coppa`, `InApp` for the game's own settings
  toggle); `consentStatus` is `InsightsConsentState?` (`Granted`, `Denied`, `Unset`,
  `NotRequired`). A record of the surface's outcome: it never changes consent, the envelope,
  or what is sent. Consent on every event comes from `EndUserConsent`, set by the app first.
- **Player state**: `SendSettingsChanged(setting, value)` — the only way player state is
  tracked; there is no user-property API.
- **Pickups**: `SendPowerupUsed` at the point of use.
- **Registry**: `SendRegistryDelta` for runtime-created content (see the registry reference).

## Custom events

A custom event is declared as a schema file and sent through a typed method, never by
string. Create one `.ugtsevent` file per event under `Assets/` (Create > Services > Insights
> Custom Event Schema): a JSON Schema whose `title` (or, absent one, the file name) is the
wire name (`x_secret_found`) and whose `properties` are the scalar params (`string`,
`integer`, `number`, `boolean`, with `enum` and `required` where they apply). The Editor
generates the project's `Events` facade from these files,
`Assets/InsightsEvents/Generated/Events.g.cs` in its own `Unity.Insights.Events.Generated`
assembly (writing it defines `UGTS_GENERATED_EVENTS`, which compiles the package's shell
out, so a project has exactly one `Events`), with one typed sender per schema
(`Events.SendSecretFound(...)`: the generator drops the `x_` prefix, PascalCases the rest and prefixes `Send`; read the
generated file for the exact spelling): required params positional, optional ones nullable,
closed string sets as C# enums. Call that method. The stringly `SendCustomEvent` is internal,
and only the generated assembly may call it; game code cannot. Game code in its own assembly
definition references `Unity.Insights.Events.Generated` explicitly (the generator warns about
definitions that reference the runtime without it); code in the predefined assemblies gets
it automatically. Regeneration runs on import, or from Services > Insights > Regenerate
Custom Events. Commit `Assets/InsightsEvents/Generated/` (the facade, its asmdef, and
their `.meta` files): a checkout without it does not compile wherever the typed senders
are called, a headless build included. The automatic regeneration (on load and on schema
import) is scheduled on `EditorApplication.delayCall` and the generator class is not
public, so a batch build that must regenerate first (a schema changed and the facade was
not recommitted) runs the menu item from a project editor method, in its own
`-batchmode -executeMethod` invocation before the one that builds:

```csharp
if (!EditorApplication.ExecuteMenuItem("Services/Insights/Regenerate Custom Events"))
    EditorApplication.Exit(1);
```

A false return only catches a wrong menu path; the generator skips a colliding schema
with a warning and still returns, so the wrapper also fails on that warning in the log.
The name must start with `x_`, and the event is declared by reference in the
manifest's `custom_events` section before it is wired: the event name, the `.ugtsevent`
schema's `version`, a `trigger_note`, and an `owner_note` saying why no standard event fits;
params and their types live in the schema, declared once. Before declaring one, check the
generated source for a standard event that fits: standard beats
custom every time (standard events get derivations and projections; `x_` events get
neither), and a custom twin of a standard event is a mapping bug, not an extension.
Custom events get no standard derivations or projections — the standard path stays the lazy
path — but an undeclared or unprefixed name is never acceptable. If a meaningful moment fits
no standard event, declare the `x_` event and ship it now.

## Envelope calls

`Events.Configure()` → `WithX(...)` → `Add(Collectors.X())` → `Apply()`. The builder on
master carries `WithUser`, `WithSandbox`, `Add` and `Apply`, and nothing else: there is no
builder call for the project, the install id, the session id, consent or the registry; all of
them are stamped by the runtime's context providers or read from the packed manifest.

- Project id and environment id come from Unity Services Core once it initializes
  (`UnityServices.InitializeAsync()`), with `Application.cloudProjectId` as the fallback for
  the project id alone. `WithProject` is gone. The package never initializes Core itself,
  and Core is a hard precondition: until it is initialized every collector, the Log
  collector included, holds its deliveries (256, then dropped with a "Deferred buffer for
  ServicesCoreReadySignal is full" warning) and replays them with the values once it is, so
  a game that never calls `UnityServices.InitializeAsync()` delivers nothing anywhere. When
  the game has no initializer of its own, kick it off at bootstrap, never awaited before
  `Apply()`. When it has one, keep it and add none: Core keeps the options of the first
  `InitializeAsync` call and every later call awaits that same initialization, so a bare
  call ahead of the game's own would replace its environment (`SetEnvironmentName`) and any
  other options it sets. Delivery also follows the project's
  remote collection policy, fetched once per launch from the project's remote settings after
  the first scene load: events are held until it answers, and a policy that is Off (or a
  project with no cloud project id, or a service that never answers) drops everything before
  any collector, the Log collector included.
- Install identity is not configurable (`WithInstall` is gone). The runtime stamps
  `unity_identities.unity_installation_id` on every record from its context providers:
  Unity Services Core's installation id when that package is installed and initialized (the
  id shared with the Ads SDK and the Insights pipeline), else an id resolved in the order
  Core resolves it (its persisted `UnityInstallationId` PlayerPrefs value, then the engine's
  installation id, then the legacy Analytics id, then a fresh id written into that key), so
  the value is the same whichever initializes first. Never pass a hardware id anywhere.
- Consent has no builder method: the package stamps the ads and analytics intents from the
  engine's `UnityEngine.UnityConsent.EndUserConsent` on every event, read before each one, so
  the app records consent in one place. Record the outcome with
  `EndUserConsent.SetConsentState(...)`; pair with `SendConsentChange` and, for
  developer-owned destinations, the `CollectorGate`. The intents are stamped, never a
  send/do-not-send switch.
- `WithUser(id | null)` — the developer-provided user id; only on the developer's explicit
  choice (Constraint 7). This is distinct from the module's player id: where the package
  carries an Authentication context provider and `com.unity.services.authentication` is
  installed and signed in, the player id is mirrored into the envelope automatically — you
  never write it. The provider samples per envelope, so an async sign-in that completes
  after `Apply()` is harmless: envelopes carry the player id from the moment sign-in
  lands.
- When several sanctioned identity sources exist, declare by stability, not by presence: a
  linked account id first, a platform account id (Game Center / Play Games) otherwise, and
  **never an anonymous id** — an anonymous player id is per-install plumbing that adds
  nothing over the install id. Resolve the winner, push on change (link and unlink
  transitions re-resolve), and keep the whole resolution under the one consent gate.
- Vendor User-ID features (GA4's User-ID, and equivalents): the declared identity reaches a
  vendor's user-id feature only at the **declaration surface** — the same authored site that
  sets the developer-provided id also calls the vendor's own user-id API, per vendor,
  explicitly, under the same consent gate, and clearing one clears the others. Never through
  the projection: adapters never see the developer-provided id by design, so a collector or
  adapter is the wrong place to look for this. One declared identity, fanned out by explicit
  per-vendor enrollment.
- There is no session builder call: the session id is the engine Insights module's, read
  afresh for every event (a random GUID only before the engine starts its session), so the
  two streams join session for session. Do not invent a session id or look for a
  `WithSession`. Closing and reopening the session frame mid-process is `ExitSession(...)`
  then `EnterSession()`; the id stays the engine's. The package's
  `EngineSessionContextProvider` reads the id before every envelope build, from the
  `unity_connect.session_id` PlayerPrefs value the engine writes at every session start (the
  first scene load, and a resume after more than 30 minutes in the background), so events
  follow a session the engine has rolled without the game doing anything. The engine writes
  that key at the first scene load and does not clear it at process start, and the package
  takes any non-empty value, so an event sent before the first scene loads (from a
  `RuntimeInitializeOnLoadMethod` at `BeforeSceneLoad`) carries whatever the key holds: the
  previous run's session id, or a fallback GUID on the install's first launch; either way
  not the id its exit will carry. That window is exactly where the skill's bootstrap runs:
  `Configure()...Apply()` at `BeforeSceneLoad` is fine, but an `EnterSession()` issued there
  stamps a stale id on the session enter record while its exit and every later event carry
  the current one, a mis-join rather than a gap. So the helper opens the session itself
  from the first context of any type it routes (Screen, Attempt, Flow, ConsentFlow,
  Cutscene, Play included) or the first point event it routes (a `SendLogin` from the
  bootstrap sign-in, a `SendConsentChange` before the first screen) while no session frame
  is open yet: the engine id is written before any `Awake`, and the first scene's `Awake` /
  `OnEnable` run before `AfterSceneLoad`, so a screen entered from a state machine's initial
  transition before the session enter could never be exited (the dispatcher drops any exit
  that is not the innermost frame). One opener only: the helper is the single owner of the
  "session frame open" state, and the fallback for a game that shows nothing at boot is an
  `EnterSession()` from a `RuntimeInitializeLoadType.AfterSceneLoad` method that runs only
  if the helper reports no session frame open (a second `EnterSession` while one is open
  does not stack: the dispatcher exits every frame above the session `Quit` /
  `sys_parent_exited`, exits the session `Quit` / `sys_implicit_reenter` with a warning, and
  opens a new one, so the session splits and every open screen and attempt gets a fake
  quit). `EnterSession` /
  `ExitSession` open and close the session context frame (the enter/exit pair with its
  outcome and measured duration); they do not mint or change the session id, and a
  mid-process "fresh session" is not something the game forces through the builder. Never
  write the session id yourself.
- There is no registry builder call: `schema_version`, `audience` (`project.audience`),
  `registry_hash`, and on custom events the payload's `schema_version` (the pinned `.ugtsevent`
  version) all come from the packed manifest automatically (see the registry reference). Do not look for
  a `WithRegistry`. `Events.Audience` returns the manifest's audience, readable before `Apply()`,
  for code that must limit what it sends (a children's game).
- `Add(Collectors.Log())` — the local stream, used for verification.
  `Add(Collectors.GameTelemetry())` — real ingestion.
  `Add(Collectors.GoogleAnalytics(...))` — the Firebase/GA4 projection, only when the
  developer asked for that dashboard; read `Collectors.cs` for the current factory
  signature (the purchase mode and the commerce `dropEvents` are set per the rulings reference
  ruling 9, never chosen by the developer) and gate it behind the consent gate when a
  consent flow exists: it is a developer-owned destination, and the ingest collector is never
  consent-gated. The factory compiles only with the Firebase Analytics package present
  (`GOOGLE_ANALYTICS_AVAILABLE`, set by the asmdef version define for
  `com.google.firebase.analytics`, or by the package's Editor define sync when the SDK came
  in as a `.unitypackage`; never on WebGL), so write the `Add(...)` line inside
  `#if GOOGLE_ANALYTICS_AVAILABLE && !UNITY_WEBGL` and the chain compiles on every target
  and before the SDK lands. When the SDK is absent, carry it as its own plan line, "adds
  dependency: Firebase Analytics SDK (`com.google.firebase.analytics`) plus the developer's
  `google-services.json` / `GoogleService-Info.plist`", which only the developer can supply;
  never add the define by hand. The first argument is a `Ga4PurchaseMode`, and it throws on
  `Unspecified`. The factory no longer takes `syncConsent`: the collector never reads or
  writes Firebase Consent Mode (on an older package that still has the parameter, pass
  `false`). Read `Collectors.cs` for the rest of the signature (`dropEvents`,
  `tutorialFlowId`, `displayNameResolver`, `legacyNames`).
  `.Gated(gate)` puts a collector behind a `CollectorGate`.
- `WithSandbox(bool)`: the test-traffic marker; see Data rules.
- The frame checkpoint needs no call and has no opt-out: on every target but WebGL the
  package keeps the open context frames in a file under `persistentDataPath` from the first
  `Apply()` made with the Engine Diagnostics gate open; after an app kill, the next launch's
  first such `Apply()` exits each frame left open with `Outcome.Abandoned` /
  `sys_reconciled` (sent once the collection policy has answered), and a clean quit exits the session `Completed` /
  `sys_quit` and the frames inside it `Quit` / `sys_parent_exited` through the package's own
  `Application.quitting` handler, registered at `Apply()`. Reconciliation replays only when
  the saved snapshot's sandbox flag matches the current build's (the Editor and a desktop
  build share `persistentDataPath`). Do not add a second teardown handler there (each extra
  exit lands on an empty stack and logs a warning); on WebGL, where the checkpoint is off,
  the teardown handler in the placement-rules reference is the only guard, and current engines deliver
  nothing there anyway (no Insights module for WebGL).
- `Add(Collectors.Log())` logs only in the Editor and in development builds (the factory
  passes `Debug.isDebugBuild` itself), so it goes in the chain unconditionally.
  `Add(Collectors.GameTelemetry())` sends to production by default
  (`IngestClient.DefaultEndpoint`, `engine-data.unity3d.com`); pass an endpoint only when
  the developer names one (a staging gateway), and the report then says which endpoint the
  build sends to, because a staging endpoint left in a release build sends production play
  to staging.

All collectors go in the first chain at bootstrap; a collector added in a later pass misses
everything sent before it and `Apply()` warns.

The bootstrap chain's shape, for a project with no legacy writer, no kill switch, and GA4
commerce off (ruling 9's default):

```csharp
_ = UnityServices.InitializeAsync();                    // only when the game has no Core initializer of its own (first call's options win); never await it before Apply()
var chain = Events.Configure()
    .WithSandbox(isTestBuild || Application.isEditor)   // build define or receipt environment; must stay true in the Editor (Data rules)
    .Add(Collectors.Log())                              // logs in the Editor and development builds only
    .Add(Collectors.GameTelemetry());                   // never consent-gated; default endpoint is production
#if GOOGLE_ANALYTICS_AVAILABLE && !UNITY_WEBGL
// Ruling 9: SuppressAutoCollected (never Project) and all 11 commerce names dropped unless the
// developer's explicit ask unlocks them.
chain.Add(Collectors.GoogleAnalytics(Ga4PurchaseMode.SuppressAutoCollected, dropEvents: ga4CommerceNames)
    .Gated(consentGate));                               // .Gated(consentGate) only when a consent flow exists; otherwise ungated
#endif
chain.Apply();
// No EnterSession here: the helper opens the session from the first context or point event
// it routes, with the AfterSceneLoad fallback (placement-rules Bootstrap).
```

A gate nobody opens is a silent drop: it holds deliveries until `Open()` and drops past its
capacity, so on a game with no consent surface the collector goes in ungated.

## Data rules

- Params are stable identifiers: asset names, enum names, explicit ids. Never localized or
  player-facing strings; verify what `.name` resolves to before using it.
- No personal data in any param: no names, emails, account ids, or player-typed free text.
  One scoped exception: the opaque store receipt on `SendPurchase` (the placement-rules reference,
  "Store receipt").
- A stored artifact gets a join key. When a moment produces a record kept somewhere else (a
  feedback report, a save, an upload, a crash dump, a replay), the event carries that
  record's id as a param (`feedback_id`, `save_id`), so the row joins to the artifact; the
  content never rides, its size may (`text_chars`). The personal-data rule keeps the text
  out; it does not drop the key, and an event declared without it cannot be joined to
  anything afterwards. The key is clean when it is opaque and minted for the record (a push
  id, a GUID); a key built from an account id, a device id, an email, or a URL that carries
  one is that identifier and stays out (Constraint 7), so mint an opaque id for the record
  instead, and keep it short: the GA4 projection truncates string params at 100 characters.
  On a moment a standard event covers, the key rides on a companion `x_` event declared for
  the artifact (one record, one row), never on a custom twin of the standard event. On
  maintenance, an event that carries such a key keeps it whenever the event is touched
  (Step 1, the maintenance row).
- `sandbox`: check `EventsBuilder` for `WithSandbox(bool)`. Where present, set it on test
  builds (build define or receipt environment) and keep it true in the Editor: a build
  define is false in play mode, and `WithSandbox(false)` there unmarks Editor traffic and
  switches the sandbox capture file off, so write `isTestBuild || Application.isEditor`
  (the chain example above). Where present but uncalled, the runtime's default is
  `Debug.isDebugBuild` (`EventDispatcher.cs` and the `WithSandbox` doc comment agree).
  Where absent (0.1.0-exp.1), the runtime sets it from `Application.isEditor`: only Editor
  traffic is marked, device test builds are production-shaped: disclose that and have the
  developer separate test devices server-side.
- Sandbox capture: every event whose envelope is sandbox is also written, as it is sent, to
  `persistentDataPath/Unity.Insights.Events/captures/<utc start>.<session>.jsonl`, one
  sealed wire record per line (AES under a key built into the package, then Base64, so
  other code on the device cannot tail it), with no collector to add. A run starts a new
  file on its first sandbox event and on each engine session change; captures not written
  for 7 days are deleted; gates, batching and retries do not affect it; it is written only
  in the Editor or a development build (`Debug.isDebugBuild`, so a release-configured test
  build with `WithSandbox(true)` marks its traffic but writes no capture), never on WebGL,
  and not with the Engine Diagnostics gate shut. It is not readable by eye: the Log
  collector's console lines are the human-readable stream for Verify rung 2, and the
  capture is what `insights.events telemetry_verify` reads.
- Screen ids are the registry manifest's `scr_`-prefixed ids (`scr_main_menu`,
  `scr_paywall`), never scene names.
