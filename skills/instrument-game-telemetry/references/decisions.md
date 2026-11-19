# Decisions: what to do when the user wants or the game has

The skill's Decisions table names each situation and points here. Each section gives the action
and the reason. Read the section before acting on that situation.

## Contents

- Add Firebase / GA4
- Add AppsFlyer / Adjust / another vendor SDK
- An event bus exists
- No wallet or resource chokepoint
- Open context frames and app kills
- A native paywall, consent sheet, or native storefront surface
- A consent flow exists
- The developer wants a remote on/off for the package (a Remote Config or feature flag)
- Unity IAP is integrated
- The developer explicitly asks, in the conversation, to close GA4's StoreKit 2 purchase gap Google's way
- Your model's screen count equals the scene count
- Existing analytics calls found at Step 1
- Cloud-linked project with no identity system at all
- The game has Unity Authentication / Player Accounts
- The purchase seam holds a store receipt (Unity IAP below 5.4, Play Billing, StoreKit)
- Marking test traffic (sandbox)

## Add Firebase / GA4

**Do this:** Implement this API and add the GA collector to the configure chain — read `Collectors.cs` for the current factory signature, set the commerce posture yourself per ruling 9 (`Ga4PurchaseMode.SuppressAutoCollected` and `dropEvents` holding all 11 commerce names; never ask the developer to pick a mode), and gate it with the project's consent gate as a developer-owned destination when a consent flow exists (the ingest collector is never consent-gated). The collector never reads or writes Firebase Consent Mode (the `syncConsent` parameter is gone; on an older package pass `syncConsent: false`), so the game's own Consent Mode path stays the only writer (CONSENT_IN_PARALLEL). Write the `Add(...)` line inside `#if GOOGLE_ANALYTICS_AVAILABLE && !UNITY_WEBGL` so the chain compiles on every target and before the SDK lands; when the SDK is absent, carry it as its own plan line, "adds dependency: Firebase Analytics SDK (`com.google.firebase.analytics`) plus the developer's `google-services.json` / `GoogleService-Info.plist`", which only the developer can supply, and never add the define by hand. The collector starts Firebase's dependency check at `Apply()`, and the Firebase SDK throws for a second check in flight: a game with its own Firebase init runs one check at a time (the placement-rules reference, Bootstrap); on a retrofit that init is existing code, so the change to it is a gated item, delivered as a work order when not approved

**Why:** One native stream projected to many destinations keeps full fidelity; the developer still gets the dashboard they asked for. With two dependency checks at once the second one throws, and whichever side owns it never configures Firebase

## Add AppsFlyer / Adjust / another vendor SDK

**Do this:** Stop first: that destination is **unavailable** — no projection for that vendor exists, so native instrumentation alone sends *nothing* to the dashboard they asked for. Say exactly that, and implement this API only after they agree to the trade (native stream now, their vendor's dashboard only when an adapter exists)

**Why:** Never fake a projection, never quietly deliver a different destination than the one requested, and never integrate the raw vendor SDK for game events

## An event bus exists

**Do this:** One subscriber class carries the instrumentation

**Why:** Zero gameplay-code edits; one reviewable surface

## No wallet or resource chokepoint

**Do this:** Recommend one; instrument each transaction at the moment the balance changes

**Why:** End-of-run aggregates cannot be audited; a chokepoint is better architecture regardless

## Open context frames and app kills

**Do this:** The package checkpoints every open frame to a file under `persistentDataPath` on every target but WebGL, with no builder call: after a kill, the next launch's first `Apply()` exits each frame left open as `Abandoned` / `sys_reconciled` (only when the saved snapshot's sandbox flag matches the current build's), and on a clean quit the package's own `Application.quitting` handler, registered at `Apply()`, exits the session `Completed` / `sys_quit` and the frames inside it `Quit` / `sys_parent_exited`. Write no teardown handler of your own for those targets; keep the manual LIFO teardown under `#if UNITY_WEBGL && !UNITY_EDITOR`, where the checkpoint is off (forward compatibility only: current engines build no Insights module for WebGL, so nothing is delivered there; say so in the report)

**Why:** A second handler on the same quit exits an empty stack and warns; where the checkpoint is active the package's handler is Constraint 5's teardown

## A native paywall, consent sheet, or native storefront surface

**Do this:** Read the native-shell reference before touching it

**Why:** Native surfaces have their own teardown and double-writer traps

## A consent flow exists

**Do this:** Wrap the surface in `EnterConsentFlow`/`ExitConsentFlow`; on the outcome record the state with `EndUserConsent.SetConsentState(...)` (the package reads it for every event), send `SendConsentChange`, and drive a `CollectorGate` only for developer-owned destinations (a vendor collector such as GA4). Never gate the ingest collector on consent (a remote kill switch is a different gate; next row)

**Why:** CONSENT_IN_PARALLEL: the ingest stream always sends, with the consent intents stamped on each record; consent governs the downstream use of identifiers, never whether an event sends. The gate exists for the developer's own destinations, and it replays the buffered session start, so bootstrap stays unconditional

## The developer wants a remote on/off for the package (a Remote Config or feature flag)

**Do this:** A `CollectorGate` on the ingest collector is allowed for this, and only for this: it is the package's kill switch, which the package names as a use of the gate beside consent, not a consent decision. Three rules (the placement-rules reference, Consent): the gate resolves on every boot path, so pair the flag fetch with a timeout fallback that opens or closes it when the flag source never answers, the developer picking the fallback knowing that `Close()` is final for the session (usually: open on timeout); name the held buffer's capacity (`.Gated(gate, bufferCapacity)`, default 256 deliveries, oldest kept) and how long a boot that covers; and the gate's value never derives from consent, locally or on the server. A collector sits behind one gate, so a vendor collector with a consent gate keeps it and is not covered by the switch. Unity's own remote collection policy already stops the stream server-side (collection mode Off drops everything before any collector), so this gate is for the developer's own flag only. Report the flag, the fallback and the timeout

**Why:** An unresolved gate holds everything and drops past its capacity, a silent loss that verifies green in the Editor; and a gate whose value comes from consent inverts the consent model two rows up

## Unity IAP is integrated

**Do this:** Wire the store events from the IAP callbacks the purchasing skill placed, never from UI code and never through a second purchase path. Which events depends on the installed version: with `com.unity.purchasing` 5.4 or later (IAP+) that SDK owns the funnel and the package drops `purchase_initiate`, `payment_options_shown`, `purchase`, `purchase_fulfilled` and `purchase_failed` in every chain collector, so wire only `SendPurchaseCancelled` and the non-funnel store events (the placement-rules reference, Purchases); before 5.4, wire the full funnel from the callbacks. Either way Unity's stream carries the whole commerce family, and GA4 gets none of it unless ruling 9 unlocks it

**Why:** One transaction truth; the funnel lives on the callback seam, and wiring events the package drops at runtime is dead code that reads as a bug

## The developer explicitly asks, in the conversation, to close GA4's StoreKit 2 purchase gap Google's way

**Do this:** Apply ruling 9's version gate from `Packages/packages-lock.json`: refuse below Unity IAP 5.2 or Firebase Analytics 13.12.0 and say why; at IAP 5.2–5.3, unlock the family (`dropEvents: ["purchase"]`, suppress mode) and add the Apple call; at 5.4+, add the Apple call only and keep all 11 names in `dropEvents`. Project file contents never unlock

**Why:** Google's gap is Google's to close; one seam and one transaction id join the GA4 rows and the package's own rows; IAP+ owns the funnel at 5.4+

## Your model's screen count equals the scene count

**Do this:** Redo screen classification from code state

**Why:** Most Unity games are one scene; the scene list is the decoy

## Existing analytics calls found at Step 1

**Do this:** Workflow B

**Why:** An existing codebase gets consent before surgery

## Cloud-linked project with no identity system at all

**Do this:** Greenfield: set up anonymous Unity Authentication with the telemetry bootstrap. Retrofit: offer it as one question; on yes it is additive-tier work

**Why:** The package's Authentication provider stamps the player id into the envelope automatically, turning install-keyed data into player-keyed data for free

## The game has Unity Authentication / Player Accounts

**Do this:** Add no init and no sign-in (the game's own call stays; the provider samples per envelope, so never block `Apply()` on it); leave `WithUser` and vendor User-ID unwired and list them in the report with the one-line call that would add each

**Why:** Module identity is automatic; developer-provided and vendor identity are the developer's choices (Constraint 7)

## The purchase seam holds a store receipt (Unity IAP below 5.4, Play Billing, StoreKit)

**Do this:** Pass it on `SendPurchase` per the placement-rules reference, Store receipt; it is optional and the one scoped exception to Constraint 3

**Why:** The receipt lets the purchase be verified later; the event never waits for it

## Marking test traffic (sandbox)

Test traffic must be marked, and whether you can mark it depends on the package version. Check the package's `EventsBuilder` for `WithSandbox(bool)`. Where it exists, call it: `WithSandbox(isTestBuild || Application.isEditor)`, the test build known from a build define or the receipt environment, and true in the Editor, where a build define is false in play mode and `WithSandbox(false)` would unmark Editor traffic and switch the sandbox capture off. Uncalled, the runtime's default is `Debug.isDebugBuild` (the Editor and development builds marked, a release-configured test build not), so call it wherever it exists. Where it does not (0.1.0-exp.1), the runtime sets `sandbox` from `Application.isEditor`: only Editor traffic is marked, and a device test build (TestFlight, an internal APK) is production-shaped and lands in production metrics: say so in the final report and have the developer separate test devices server-side by build identity.
