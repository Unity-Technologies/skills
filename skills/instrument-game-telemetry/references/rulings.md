# Rulings — the design decisions this skill enforces

## Contents
- What a ruling is
- The register
- How to use it

The event model makes a small number of decisions that depart from what instrumentation in the
wild does by default. A skill that omits one produces instrumentation that verifies green
and misleads. This file lists each ruling by name and where this
skill enforces it, so an implementer can check them one at a time.

## What a ruling is

A ruling is a normative design decision and that changes what a correct
implementation does, not how it is organized. Each one below is checkable: a wired game
either honors it or does not, and the Verify ladder or the final report shows which.

## The register

1. **EMIT_AT_RESOLUTION.** A param is a fact that is present at the emit moment. If the
   fact resolves later, emit at resolution or split the event; a placeholder on a typed
   param is a wiring failure. Violation: a string param that is empty, null, `"unknown"`,
   `"pending"`, or any other sentinel at emit time because the real value arrives later.
   Fix: move the emit to the callback or continuation where the value is known, or split
   into an initiate event (no param) and a completion event (param present). Skill: Constraint 6 and the pitfalls table row
   on stable identifiers.

2. **TYPE_REGISTRY_CONFORMANCE.** The verify stream assert checks emitted values against
   their declared types and fails on placeholder strings. Violation: an event in the
   captured stream whose typed param carries a value that does not conform to the declared
   type, including any placeholder string on a param declared non-string. A custom event's
   param types live in its `.ugtsevent` schema, which the manifest's `custom_events` entry
   references by `name` and `version`; resolve that schema before comparing. Check: Verify
   rung 4 when a conformance tool is present; without one, compare the `ParamValue` type tag
   of each captured param (string, int, double or bool) to the schema's declared type. The
   Log collector output does not carry type tags (`"3"` and `3` print the same), so when it
   is the only evidence, record the check as unverified rather than passed.
   Skill: Verify rung 4.

3. **PLACEMENT_RULES.** Each trigger class is wired at its named seam, from callbacks,
   never from scene loads or `Start()`. Violation: a context or point event whose call site
   is `Start()`, `Awake()`, `OnEnable()`, or `SceneManager.sceneLoaded`. Check: for every
   wired call, name the enclosing method and state why it is the seam for that trigger
   class. Skill: the placement-rules reference
   and the Decisions table.

4. **LOGIN_RULING.** `SendLogin` is a real account action, raised once per completed
   authentication method per session; the app-open homonym is a lifecycle moment (T1),
   never a login. The anonymous boot identity never counts; silent
   token restores do. Unity Authentication's `SignInAnonymouslyAsync` is both: with a cached
   session token it restores that session whatever the account is, so the call alone does
   not decide. Decide from the resolved account state: the signed-in `PlayerInfo` carries a
   linked identity (`Identities` non-empty or a `Username`; call `GetPlayerInfoAsync()` if
   the sign-in did not return it) for a restored linked account, and none for an anonymous
   player. Violation: `SendLogin` called when `SignInAnonymouslyAsync` resolves to an
   anonymous player, or called more than once per completed method per session, or not
   called when a silent restore of a linked account succeeds (including one through
   `SignInAnonymouslyAsync`). Check: trace every call to `SendLogin` back to its trigger;
   each must be a login completion (email, social, silent restore of a linked account) and
   not an anonymous boot. Skill: placement-rules Accounts.

5. **POST_SCORE_WITH_LEADERBOARD.** `SendPostScore` is called beside every leaderboard
   submit, from the same call site, so a score is never recorded as a currency. Violation:
   a leaderboard submit call site (any SDK call that writes a score entry) that does not
   have a paired `SendPostScore` call in the same method. Check: grep for leaderboard SDK
   calls and verify each has a `SendPostScore` call in the same method with the matching
   `node` (registry id for the mode or level) and `board` (registry id for the leaderboard).
   Skill: the wiring plan table.

6. **CONSENT_IN_PARALLEL.** The vendor's consent (for example Firebase Consent Mode) and
   Unity's consent never set or read each other; the developer implements both independently.
   Consent gates developer-owned destinations only; the ingest stream always sends, with
   the intents stamped on every envelope. Violation: code that reads or writes Firebase
   Consent Mode from the `EndUserConsent` callback or vice versa; code that gates the ingest
   collector behind a `CollectorGate` on consent; on an older package whose GA4 factory
   still takes `syncConsent`, leaving it at its default `true`, which made the collector
   write Firebase Consent Mode from the Unity intents (deny-by-default included) and
   overwrite the game's own vendor consent. Check: the consent wiring has two independent
   paths — one calling `EndUserConsent.SetConsentState(...)` then `SendConsentChange(...)`
   (the package stamps the intents on every envelope from `EndUserConsent`, so nothing else
   is needed), another calling the vendor's consent API; `Collectors.GoogleAnalytics(...)`
   in the configure chain syncs no consent (the parameter is gone on current packages;
   `syncConsent: false` on an older one); and only developer-owned destinations (a vendor
   collector) have a consent `CollectorGate`. The ingest collector is never consent-gated;
   a remote kill switch is a different gate whose value never derives from consent
   (the decisions reference, the remote on/off section). Skill: the consent-flow Decisions
   row, placement-rules Consent, and the final report's consent statement.

7. **ONE_WRITER_PER_DESTINATION.** When a vendor projection is active, the vendor's
   automatic collection of the events the adapter writes (screen reporting) is off, for every
   other vendor-auto event the adapter defers and never emits a duplicate, and Verify rung 5
   requires the device debug-view check or an explicit "unverified" line in the final
   report. Violation: a GA4 collector in the
   configure chain while Firebase automatic screen reporting is enabled (not suppressed).
   Check: read the built values on each platform where the GA4 collector is active —
   `google_analytics_automatic_screen_reporting_enabled` in the Android manifest and
   `FirebaseAutomaticScreenReportingEnabled` in the iOS `Info.plist` — and require `false`.
   The package's `GoogleAnalyticsScreenReporting` build step adds `false` only when the key
   is absent and keeps any existing value, an explicit `true` included, so the
   `GOOGLE_ANALYTICS_AVAILABLE` define alone does not prove suppression. If a platform's
   value cannot be read, write an explicit "unverified: GA4 double-writer check not done"
   line in the final report. Skill: the "Add Firebase / GA4" Decisions row and
   Verify rung 5.

8. **DIAGNOSTICS_MASTER_GATE.** With the engine's Insights module not running
   (`EngineDiagnostics.IsEnabled` false), `Apply()` registers no collector and every event
   is dropped, the vendor projection included. This is enforced by the package runtime
   (`EngineDiagnosticsGate`, read in `EventsCore.Apply`); the skill adds no instrumentation
   step to enable the module and must not work around it. Violation: any attempt to gate
   individual events or collectors on `EngineDiagnostics.IsEnabled` in game code, or any
   claim that telemetry flows when the module is off. Check: confirm the package version on
   disk carries `EngineDiagnosticsGate`; if it does, the gate is automatic and needs no
   game-code involvement. When the gate is shut, the causes are the availability cases: an
   editor before 6000.2.7f1 (no native module), a target the engine builds no Insights
   module for (WebGL, Linux), a stripped module, and batchmode or the test runner; on
   6000.2.7f1 to 6000.4.x also the project's Engine Diagnostics setting being off (no
   managed `IsEnabled` there, so the gate approximates it from that setting, which a player
   build reads only once Unity Services initializes), while from 6000.5.0a9 the gate reads
   `EngineDiagnostics.IsEnabled` itself and that flag does not follow the setting (the
   package's `DIAGNOSTICS_DISABLED` warning still names the setting). The report names which
   it was, quoting the gate's reason from the warning, so instrumentation never silently
   no-ops. Skill: Verify rung 1 (compile
   gate; the gate fires there) and the pitfalls row on the Engine Diagnostics warning.

9. **GA4_COMMERCE_POSTURE.** The GA4 projection carries none of the 11 commerce events by
   default. The family is `shop_view, product_view, purchase_initiate,
   payment_options_shown, payment_method_selected, purchase, purchase_fulfilled,
   purchase_failed, purchase_cancelled, subscribe, renewal` (`start_trial` is not a member;
   `dropEvents` matches catalog names). The Unity stream carries the whole family
   regardless of this ruling.
   - **Default.** The skill sets up `Collectors.GoogleAnalytics(Ga4PurchaseMode.SuppressAutoCollected,
     dropEvents: <all 11 names>)` without asking the developer to pick a mode.
     `SuppressAutoCollected` fits because Google's own `in_app_purchase` is the revenue row.
     `Ga4PurchaseMode.Project` is never passed. The once-per-name "drops by policy" warnings
     are expected.
   - **Trigger.** Only the developer's explicit ask, in the conversation, to close the
     StoreKit 2 purchase gap Google's way unlocks. "Add Firebase", "see purchases in GA4",
     and anything found in project files (a TODO, a comment, a config) do not unlock.
   - **Gate.** Read `com.unity.purchasing` and `com.google.firebase.analytics` from
     `Packages/packages-lock.json`. If `com.unity.purchasing` is missing or below 5.2, or
     `com.google.firebase.analytics` is below 13.12.0, refuse the unlock, tell the developer
     which package is missing or too old, and keep the default posture.
   - **Unity IAP 5.2 to 5.3: full unlock.** `dropEvents: ["purchase"]` with the same suppress
     mode, so the other 10 project under their GA4 names, plus the Apple call beside the
     game's `Events.SendPurchase`, using the same transaction id variable.
   - **Unity IAP 5.4 and later: the Apple call only.** `dropEvents` keeps all 11 names. IAP+
     emits the funnel itself, the package drops the game's copies at every collector, and
     IAP+'s own emission does not reach the adapter yet. Tell the developer this. The
     Apple call goes in the game's IAP confirmation handler, with the confirmed order's
     transaction id, the one IAP+ stamps on its `purchase`.
   - **The Apple call.** It sits in the IAP confirmation handler, the seam where the package's
     `purchase` is raised, and passes the same transaction id as that `purchase`. The
     handler calls one named helper:

     ```csharp
     void LogStoreKit2TransactionToGa4(string transactionId)
     {
     #if GOOGLE_ANALYTICS_AVAILABLE && UNITY_IOS
         if (!Purchasing.Utilities.StoreKitSelector.UseStoreKit1())
             FirebaseAnalytics.LogAppleTransactionAsync(transactionId);
     #endif
     }
     ```

     Before wiring, confirm `LogAppleTransactionAsync(string)` in the installed Firebase SDK
     and `StoreKitSelector.UseStoreKit1()` in the installed IAP package source. Google Play
     and StoreKit 1 are left to Google's automatic collection, nothing is logged twice, and
     there is no manual GA4 `purchase`.
   - **Money.** No projected commerce row carries `value` or `currency`; the adapter enforces
     it, so the skill strips nothing from the Unity events.
   - **Violation:** on the default path, a GA collector missing any of the 11 names;
     `Ga4PurchaseMode.Project` on any path; an Apple call without the explicit ask or outside
     the version gate; an Apple call missing the StoreKit 2 guard; an Apple call in a
     different handler from the `purchase` seam or with a different transaction id; at 5.4+,
     a `dropEvents` shorter than 11; at 5.2 to 5.3, `purchase` missing from `dropEvents` or
     a subset unlock. `purchase` is never projected on any path.
   - **Check:** read the configure chain and `Packages/packages-lock.json`; when unlocked,
     do the placement review of the confirmation handler and use the device debug view
     (Verify rung 5). Skill: the "Add Firebase / GA4" and
     explicit-StoreKit-2-ask Decisions rows.

## How to use it

Read this before planning the wiring (Workflow A step 4, Workflow B step 3) and again
before hand-off, then walk the register against the wired game. Rulings 1, 2, 4 and 5 show up
in the verify stream; 3 shows up in the placement review; 6 and 7 are checked in the
configure chain and on the device; 9 is checked in the configure chain and
`Packages/packages-lock.json`, and when unlocked in the placement review and on the device;
8 is a property of the runtime you confirm by reading
the package version's gate, not by instrumenting around it. Say in the final report which
rulings were checked and how, and name any that could not be checked with the reason;
never report a ruling as checked on the strength of its pointer alone.
