# Placement rules — where calls go, and where they never go

## Contents
- Structure
- Bootstrap
- Trigger classes
- Contexts
- Screens
- Outcomes
- Teardown
- Consent
- Accounts
- Purchases
- Ads
- Store receipt
- Adding beside existing code
- Economy
- Known runtime limitation

## Structure

Create one static helper class (for example `Telemetry`) that owns all lifecycle state —
session, current screen, overlay depth, open Play/Attempt — and route every context
Enter/Exit through it. Point events (`Events.SendX`) may be called directly at the call site.
Never spread context-stack bookkeeping across gameplay classes.

If the game's code or the helper lives in an assembly defined by an `.asmdef`, add
`Unity.Insights.Events` to that asmdef's `references`, or the compile gate will fail, and
`Unity.Insights.Events.Generated` as well once any `.ugtsevent` schema exists (the generated
facade then declares `Events`; the generator warns about asmdefs that lack it). Code in the
default `Assembly-CSharp` needs nothing extra.

## Bootstrap

Bootstrap with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` —
never depend on a scene object existing. Run the full `Events.Configure()...Apply()` chain
with **every** collector in it, and make sure Unity Services Core gets initialized: every
collector holds its deliveries until Core is initialized, and the package never initializes
it itself (the api-surface reference, Envelope calls). When the game already calls
`UnityServices.InitializeAsync(...)`, leave that call and its options as they are and add
none: Core keeps the options of the first call and every later call awaits that same
initialization, so a bare call at `BeforeSceneLoad` would initialize Core with the default
environment before the game's `SetEnvironmentName(...)` runs. Only when the game has no
initializer, kick off a bare `UnityServices.InitializeAsync()` beside the chain, never
awaited before `Apply()`. The session enter never runs at `BeforeSceneLoad`: the
engine writes its session id at the first scene load, and a session enter sent before that
stamps a stale id. Have the helper open the session from the first context of any type it
routes (ConsentFlow, Cutscene and Play included), or the first point event it routes, while
no session frame is open (the id is written before any `Awake`), with an `EnterSession()`
from an `AfterSceneLoad` method as the fallback for a game that shows nothing at boot, run
only if the helper reports no session frame open: one opener, or the second enter splits
the session and fake-quits every open frame (see the api-surface reference, Envelope calls). All
collectors go in this first pass: a collector added later misses everything already sent
(including the open session's enter) and `Apply()` warns. The one exception is the GA
collector held back under "One Firebase dependency check in flight" below. Later passes change context
only: `WithUser(id)` / `WithUser(null)` on sign-in and sign-out (consent is recorded with `EndUserConsent.SetConsentState(...)`, not
through the chain). The session id is the engine's: there is no builder call for it, and
there is no pass for a new session (see the api-surface reference, Envelope calls).

**One Firebase dependency check in flight.** With the GA collector in the chain, `Apply()`
constructs it, and the collector starts `FirebaseApp.CheckAndFixDependenciesAsync()` at
once (`Runtime/Collectors/FirebaseGa4Backend.cs`). The Firebase SDK throws
`InvalidOperationException` synchronously for a second check while one is in flight. The
package covers only its own side: one check per process, shared by every collector a later
`Apply()` builds, retried every 500 ms for up to 30 s while the game's own check is running,
after which the collector reports itself unavailable and GA4 stays silent for the session.
The other order has no package-side help: a game that starts its own check after the chain
(its own Firebase init, Remote Config, Consent Mode, a Firebase feature the game uses
directly) gets the exception itself, and Firebase never configures on that device. The rule
is one check at a time, in a deterministic order: the chain's check at `BeforeSceneLoad`
goes first, the game's own `CheckAndFixDependenciesAsync()` starts after `Apply()` has
returned (from an `AfterSceneLoad` method or later, never from a second `BeforeSceneLoad`
method, whose order against the chain is undefined) and is wrapped in a bounded retry on
`InvalidOperationException` with a short delay between attempts, since the game cannot
observe when the collector's check has settled. Say in the report what the order is. On
Workflow B the game's call is existing code, so the change is gated: present it as a gated
item, and until it is approved and applied leave the GA collector out of the chain. The
collector starts its check at `Apply()`, so the game's unchanged check (from `Awake` or
anywhere after the chain) would overlap it and throw. The native collectors still go in.
When the developer does not approve, deliver a work order that adds the GA collector to the
bootstrap chain and the bounded retry to the game's check as one change, and say in the
report that GA4 receives nothing until it is applied. A game with no Firebase code of its own needs
nothing here.

On a project with no linked cloud project nothing is sent at all, the Log collector
included: the runtime fetches the project's collection policy from its remote settings once
per launch, holds every event until it answers, and with no cloud project id (or a service
that never answers) the policy is Off and everything is dropped before any collector. Say
so in the report; verification needs a linked project reachable from the Editor.

If the game has a consent flow, the gate it drives belongs only on developer-owned
destinations: a vendor collector such as GA4 (`.Gated(gate)`), opened or closed from the
consent outcome. Never gate the ingest collector: the telemetry stream is diagnostics the
developer enabled at the project level, events always send with the consent intents stamped
on each record, and consent governs the downstream use of the identifiers collected with an
event, never whether it sends. The gate replays the buffered session start, so the session
enter stays unconditional (never consent-gated), wherever the helper places it. A remote
kill switch for the package is the one case where the ingest collector sits behind a gate,
and that gate is never the consent gate (Consent below, and the kill-switch row in the
Decisions table).

## Trigger classes

- **T1 — automatic.** The session context is the runtime's job once you call
  `EnterSession`/`ExitSession`. Verify it emits; never duplicate lifecycle moments with
  custom events (`app_open`-style names are derived downstream, never sent).
- **T2 — declared.** Screens, from the survey's classification as declared in the registry
  manifest.
- **T3 — owned surfaces.** Purchases from the IAP integration callbacks; the ad funnel from
  the mediation SDK callbacks; economy from the wallet chokepoint.
- **T4 — placed calls.** Everything else: contexts on state transitions, point events at
  their moments, ideally from one bus subscriber.

## Contexts

Contexts go on state transitions. Never in UI button handlers, never in scene `Start()`,
never in ad callbacks — the three classic mistakes. `EnterAttempt` fires where the player
gains control; `ExitAttempt` fires at every terminal path (win, fail, quit, teardown).

## Screens

`EnterScreen` / `ExitScreen` where the screen is actually shown and hidden. Ids are the
registry manifest's `scr_` ids, never raw scene names, and never auto-tracked from
`SceneManager.sceneLoaded`. Use `navDirection` if the game distinguishes drill-in from back.
Dialogs and popups that appear over a screen (pause, game over, level up) are always overlay
screens (`overlay: true`) — instrument each from the survey or deliberately exclude it; never
mix flat and overlay for the same kind of UI.

## Outcomes

Pick the `Outcome` where the reason is known, never inside a shared close path that always
reports `Completed`. Map consistently: won → `Completed`, lost/died → `Failed`, backed out
mid-run → `Abandoned`, app quit → `Quit`.

**Attempt exits carry `outcomeReason`, and its vocabulary is declared, not improvised:**

- A **Failed** exit always carries the cause (`fall`, `hazard`, `timeout`, ...): the fail
  curve is computed from it, and a failed exit without a reason is a verify finding. Declare
  the values in the registry's `enums.outcome_reasons`; a reason outside the declared enum
  is also a finding.
- On a **run-based** topology, `Completed` IS the natural terminal (death, final wave,
  timer), so the completion carries the cause the same way; objective-declared completions
  omit it.
- A **Skipped** exit's reason is the skip rail, spec-fixed: `rewarded_ad | currency | free`.
- The **`sys_*` namespace is the runtime's**, never yours to write: the runtime's own
  structural exits stamp reasons such as `sys_quit`, `sys_parent_exited`, and
  `sys_reconciled` (the frame-checkpoint reconciliation). Game code never emits a `sys_`
  reason, and the registry never declares one.
- Teardown paths use plain outcomes with the reason where one is known: app quit →
  `Quit`, pause-to-title backing out of a run → `Abandoned`.

## Teardown

On every target but WebGL the package handles the quit itself: its `Application.quitting`
handler, registered at `Apply()`, exits every open frame deepest first (the session
`Completed` / `sys_quit`, the frames inside it `Quit` / `sys_parent_exited`), and a kill is
reconciled on the next launch from the frame checkpoint (Known runtime limitation, below).
Do not add a handler of your own there: a second one exits an empty stack and warns. The
checkpoint is off on WebGL and the package registers no quit handler there, so keep a manual
handler under `#if UNITY_WEBGL && !UNITY_EDITOR` (the Editor with WebGL as the active target
still runs the package's handler): in the quit path (`Application.quitting`), exit every
still-open context innermost-first, ending with `ExitSession`, the quit's attempt exit using
`Outcome.Quit`; the handler never fabricates a reason it does not know, and any
runtime-injected reason rides the `sys_*` namespace per the Outcomes rule above. Current
engines build no Insights module for WebGL, so nothing is delivered there; the handler is
forward compatibility, and the report says so.

## Consent

The model first, because wiring it wrong inverts it: the ingest stream **always sends**.
The developer chose diagnostics at the project level and chose to extend it with telemetry;
end-user consent is between the developer and their player, and what it changes is the
**stamp** on each record (the analytics and ads intent statuses) and the downstream use of
identifiers, never whether an event is sent. A `CollectorGate` is the tool for the
developer's own destinations (their GA4 property, a vendor collector), where their consent
posture does govern delivery.

**A kill switch is not consent.** A developer who wants a remote on/off for the package (a
Remote Config or feature flag that stops the game sending at all) may put the ingest
collector behind its own `CollectorGate`: the package names "a remote flag" as a use of the
gate beside consent, and this is the developer deciding whether to send, not the player's
consent deciding it. Three rules keep it from inverting the model above. The gate resolves
on every boot path: the flag fetch is paired with a timeout fallback that opens or closes
the gate when the flag source never answers (no network, services init failed), because an
unresolved gate holds everything and drops past its capacity, the silent drop described in
the api-surface reference. `Close()` is final for the session (`Open()` is a no-op after it), so a
fallback that closes loses that boot's data even when the flag arrives late as "on", and
with a durable ingest outbox a closed gate also replays nothing from earlier launches; the
usual choice is to open on timeout, since the switch is an emergency stop, and a developer
who would rather lose a boot than send while unsure picks close knowingly. The developer
picks the fallback and the report records it. The held buffer's capacity is named:
`.Gated(gate, bufferCapacity)` defaults to 256 deliveries (events plus context enters and
exits, oldest kept), so say how long a boot that covers for this game and raise it when the
flag can take longer. And the gate's value never derives from consent, locally or on the
server: the flag is the developer's operational switch, the same for every player whatever
they consented to, and the consent outcome never opens or closes it; consent stays a stamp
on the ingest stream. A collector sits behind one gate (`.Gated` is a setting, not a
layer), so a vendor collector with a consent gate keeps it and is not covered by the kill
switch; say so in the report. Only on a game with no consent flow may the vendor collector
share the kill-switch gate. Unity's own remote collection policy already stops the stream
server-side (collection mode Off drops everything before any collector), so a gate here is
for the developer's own flag, never a substitute for that. Nothing here changes the rule
for consent.

Wrap the consent surface in `EnterConsentFlow` / `ExitConsentFlow`. On the outcome, in this
order: `EndUserConsent.SetConsentState(...)` (`UnityEngine.UnityConsent`, the
package reads it for every event), then `SendConsentChange(...)`, then
`gate.Open()` or `gate.Close()` on the developer-owned destinations' gate.

The gate, opened or closed for each gated developer-owned destination, is where a vendor
collector's consent handling ends: the GA collector neither reads nor writes Firebase
Consent Mode (the `syncConsent` parameter that once did is gone; on an older package that
still has it, pass `false`, since its default pushed Unity's intents into Consent Mode
deny-by-default, which this rule forbids). Wire `FirebaseAnalytics.SetConsent` from the
consent outcome only when the developer asks for Consent Mode, as a parallel track beside
the Unity calls above and never through this API; otherwise state in the report that
Consent Mode is not configured and Firebase runs at its plist/manifest default.

Children/VPC remains the developer's duty, not machinery: where the audience includes
children, state the obligation in the report; nothing in
this API collects ads identifiers, and no call you place discharges a legal duty.

## Accounts

Wire `SendLogin`, `SendSignUp`, `SendAccountLinked`, `SendAccountDeleted` from the account
system's callbacks. Apply `WithUser(...)` alongside the ones that change who the player is
only when the developer asked for account identity (Constraint 7); otherwise
leave it unwired and list it in the report.

## Purchases

Wire `SendPurchaseInitiate`, `SendPurchase`, `SendPurchaseFailed`, `SendPurchaseCancelled`,
`SendPurchaseFulfilled` from the IAP integration callbacks. Never build a second purchase
path, and never emit purchase events from UI code — the funnel lives on the callback seam.
With the IAP+ SDK installed (`com.unity.purchasing` 5.4 or later) the funnel is that SDK's:
the package drops `purchase_initiate`, `payment_options_shown`, `purchase`,
`purchase_fulfilled` and `purchase_failed` in every chain collector (the sandbox capture
file still records them), so wire only `SendPurchaseCancelled`, `shop_view`,
`product_view`, `payment_method_selected` and the subscription events, and say in the plan
which SDK owns the funnel. None of this family projects to GA4 unless ruling 9 unlocks it
(the rulings reference).

## Ads

Wire `SendAdRequested`, `SendAdLoaded`, `SendAdLoadFailed`, `SendAdImpression`,
`SendAdRewardGranted` from the ad SDK callbacks, and wrap the showing of an ad in
`EnterAdBreak` / `ExitAdBreak`.

## Store receipt

`SendPurchase` takes four optional receipt params, after `placement` and before
`extraParams`: `appleAppReceipt`, `appleJwsRepresentation`, `googlePlayOriginalJson` and
`googlePlaySignature` (on the wire `apple_app_receipt`, `apple_jws_representation`,
`google_play_original_json`, `google_play_signature`). They let the purchase be verified
later. Fill them only from the game's own purchase seam: code the game wrote that holds the
transaction, such as its Unity IAP callback, its own `Transaction.updates` loop or its own
`PurchasesUpdatedListener`. Never add an observer, a restore or renewal path, or a hook into a
third-party SDK to get one.

| The seam holds | Read | Pass as |
|---|---|---|
| A Play Billing `Purchase` | `purchase.getOriginalJson()`, `purchase.getSignature()` | `googlePlayOriginalJson`, `googlePlaySignature` |
| A StoreKit 2 `VerificationResult<Transaction>` | `result.jwsRepresentation`, verified or not | `appleJwsRepresentation` |
| A StoreKit 1 transaction, and no JWS | the app receipt: the file at `Bundle.main.appStoreReceiptURL`, base64 | `appleAppReceipt` |
| Unity IAP 5.0 to 5.3: the order (`PendingOrder`, `ConfirmedOrder`) | Apple: `order.Info.Apple?.jwsRepresentation`, else `order.Info.Apple?.AppReceipt`. Google: `order.Info.Receipt` is Unity's JSON wrapper (`Store`, `TransactionID`, `Payload`), and its `Payload` is a JSON string whose `json` and `signature` are the two Play values | `appleJwsRepresentation`, else `appleAppReceipt`; `googlePlayOriginalJson`, `googlePlaySignature` |
| Unity IAP 4.x: `ProcessPurchase(PurchaseEventArgs args)` | `args.purchasedProduct.receipt`, the same wrapper. Apple: `Payload` is the base64 app receipt. Google: `Payload` holds `json` and `signature` as above | `appleAppReceipt`; `googlePlayOriginalJson`, `googlePlaySignature` |
| Unity IAP 5.4 or later | nothing: the IAP+ SDK raises the purchase funnel itself, and the package drops a placed `SendPurchase` | none |
| `unity_order_id` | reserved: not placed | none |

The Unity IAP rows were checked against the com.unity.purchasing source at 4.14.2, 5.0.0,
5.2.1 and 5.3.0. Unity IAP documents `order.Info.Receipt` as empty for a consumable once the
order is confirmed: read it where `SendPurchase` already is, and send without it when it is
empty.

- The receipt is always optional. `SendPurchase` fires for every transaction, with or without
  one, and never waits on receipt retrieval (no refresh, no retry, no await). The JWS is the
  same purchase as the app receipt, so pass `appleAppReceipt` only when there is no JWS.
- Pass receipts by named argument (`appleJwsRepresentation: jws`), never by position.
- When the game's native code completes the transaction and reports it to C# over the game's
  own bridge, read the receipt in that native code, add it to the existing bridge message, and
  pass it on the existing C# `SendPurchase`. Place no `purchase` on the native side: the C#
  seam sends, once per transaction.
- Receipts go on `SendPurchase` only: never in `extraParams`, on another event, in a custom
  event, or to a vendor SDK. A request to send them to Firebase or GA4 alone is refused.
- Never log a receipt, and never parse or inspect one beyond lifting the strings above.
- Constraint 3 (no personal data) has one scoped exception here: a store receipt is passed
  through opaque, as the store issued it, even though it may carry store-side ids
  (`obfuscatedAccountId`, `appAccountToken`) or, for StoreKit 1, the purchase history. The
  exception covers these four params only.

## Adding beside existing code

A raise-beside-legacy edit inherits its host's syntax: check the body form before
appending. An expression-bodied lambda or member (`=> expr`) cannot take a second
statement — convert it to a block body in the same edit, or the compile gate fails on
your addition.

## Economy

If the game has one wallet class where all currency changes pass, instrument it once with
`SendResourceEarn` / `SendResourceSpend`. If it does not, recommend adding one. One event per
transaction, at the moment the balance changes, for every resource in the inventory — never a
single aggregate at the end of a run.

## Known runtime limitation

A kill cannot run any quit handler, so the exits for whatever was open never go out in that
launch. The package keeps every open frame in a checkpoint file under `persistentDataPath`
(every target but WebGL, with no setup and no opt-out, from the first `Apply()` made with
the Engine Diagnostics gate open), and the next launch's first such `Apply()` exits each
frame left open as `Abandoned` / `sys_reconciled`, carrying the session it opened in, so
the gap closes one launch late; the replay happens only when the saved snapshot's sandbox
flag matches the current build's, and goes out once the collection policy has answered. On
WebGL nothing is checkpointed, the manual teardown handler covers clean quits only, and
current engines deliver nothing there anyway. Build no workaround of your own; state in
the final report how kills show up in the data so the developer reads the gaps correctly.
