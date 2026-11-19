# Native shell — instrumenting the app's own native surfaces

## Contents
- Scope law
- Survey extension
- Registry
- The bridge
- Teardown contract
- Single-writer rule (and the engine-wrapper trap)
- iOS spine case — custom paywall
- iOS spine case — ATT completion
- Android spine case — paywall Activity or DialogFragment
- Android spine case — consent-form completion
- Verify addendum

You are extending an instrumented game across the engine boundary: surfaces the game presents
through the platform's native UI stack, which the C# plane cannot see. The parent skill's
constraints apply unchanged; this reference adds their application at the seam where bolt-on
SDKs have always cheated.

## Scope law: ownership of the code, not ownership of the pixels

Reflection and swizzling are forbidden (Constraint 9), and the native shell is exactly where
bolt-on SDKs swizzle (automatic screen reporting hooks `viewDidAppear` fleet-wide; that
interception is what this API replaces). The scope splits on who owns the code:

- **App-owned surfaces** — ViewControllers, sheets, and prompts the app's own code presents
  (a custom paywall VC, a native settings screen, a webview wrapper, a pre-consent
  explainer): yours to instrument, by adding calls to code the developer owns. In scope.
- **Vendor-owned UI** — the StoreKit payment sheet, the ATT dialog itself, ad-SDK
  ViewControllers, Game Center sheets: never hooked, never registered as screens. The app's
  own code that *presents* them or *receives their callbacks* is the authored seam — the
  present-call, the completion handler.
- **Engine-generated shell classes** — `UnityAppController` and the `Unity*ViewController*`
  family on iOS; `UnityPlayerActivity` and the `Unity*` classes on Android — are
  engine-owned, never app-owned: excluded.

A webview wrapper registers as **one** screen; the page inside it is not yours to observe.
An app-owned script-message handler receiving the page's messages is an authored seam;
JavaScript injected to watch the page is not.

## Survey extension

The game-model file's `native_surfaces` section (see the survey-procedure reference) records every
app-owned surface: class, platform, what it presents, and `file:line` evidence of the
present-call. Empty is a valid value; absent is not.

## Registry

Each app-owned surface registers as a screen — same id namespace, same denominator. One
surface, one id: if the surface already has a screen entry from the engine-side
classification, that entry gains `residence: native_shell`; never mint a second id for the
same surface. `kind` is unchanged by residence (a native paywall sheet is
`kind: overlay, residence: native_shell`).

## The bridge

The package ships one native entry point: `UnityInsightsEvents_Send(eventName, paramsJson)`
(iOS, `Runtime/Plugins/iOS/UnityInsightsEvents.h`) and
`com.unity.insights.events.InsightsEvents.send(eventName, paramsJson)` (Android). This
reference uses it for one call only, the native-only `purchase` (see "The one exception" in
the iOS paywall case). Screens, consent and surface events do not go through it: it holds
calls made before Unity registers and replays them, where this reference requires a loud
drop, and it returns nothing a surface could latch its entered state on. For those you
author a small bridge: a native half per platform exposing four entry points, and one
managed half routing them into the package API.

- Entry points (iOS C functions / Android static methods): `screenEnter(id, overlay)`,
  `screenExit(id, reason)`, `consentChange(framework, consentStatus, surface)`,
  `record(name, paramsJson)`. `screenEnter` **returns whether the bridge accepted the
  enter** (false on the uninstalled drop path) — callers latch their entered state from
  the return value, never from having made the call, or a pre-install enter is dropped
  and the surface never retries.
- The managed half routes into `Events.EnterScreen` / `Events.ExitScreen` /
  `Events.SendConsentChange` / the typed custom senders the Editor generates from
  `.ugtsevent` schemas (`SendCustomEvent` is internal): the same API every other trigger
  class uses. The native shell gets no private vocabulary. The native-only `purchase` is the
  one call that skips the authored bridge: it goes straight to the packaged entry point,
  which maps the same wire names onto the same events. That entry point can also send a
  `custom_event` by wire name with no schema type check (the registry gate still drops an
  undeclared `x_` name); prefer routing custom events through the managed typed sender so
  the schema binds too.
- `Events.ExitScreen` takes no id — it pops the topmost Screen frame. The managed half must
  compare the bridge's `id` to the topmost open screen and, on a mismatch, warn and drop
  rather than pop the wrong frame. On Android this check is a **warning only** (a queued
  enter may not be applied when the exit arrives — dropping there loses valid exits).
- The managed half maps the bridge's framework strings to the enum:
  `"att"` → `ConsentFramework.Att`, `"cmp"` → `ConsentFramework.Cmp` (`.Gdpr` also exists).
- Install the managed half in the bootstrap **after** `Events.Configure()...Apply()`. Before
  install, the native side must drop operations with a loud log rather than buffer them —
  the init window staying closed is correct behavior, not a bug.
- Threading: route native callbacks to Unity's scripting thread with at-call timestamps. On
  iOS the UI thread usually is the scripting thread; on Android it is not — queue and drain
  on the main thread, and add no extra queues beyond that one.

## Teardown contract (required, not hygiene)

When the system terminates the app, the platform's window teardown can dismiss a presented
native VC **after** the scripting VM has shut down; its `viewDidDisappear` then calls back
into a dead VM and crashes. Ship both guards and keep both when adapting:

1. **Managed:** the bridge's `Install()` wires `Application.quitting += Uninstall`, which
   nulls the native callback registration while the VM is alive — late lifecycle calls then
   hit the loud-drop path.
2. **Native (iOS):** the registration function lazily installs (once, on first non-null
   registration) a `UIApplicationWillTerminateNotification` observer that nulls the callback
   slot with a loud log — the guard for teardown orders where the managed hook never runs.

On Android, give the Java bridge the symmetric `registerCallback(null)` from
`Application.quitting`, so a lifecycle callback racing process teardown drops loudly instead
of invoking a proxy into a dying VM.

## Single-writer rule (and the engine-wrapper trap)

A surface's Enter/Exit calls live on exactly one side of the boundary. Before wiring, run a
**writer census** for the surface's id: the engine's screen-report seam, legacy analytics
calls, any auto-tracker. Many "native" surfaces are presented BY an engine screen object —
a C# wrapper that already flows through the game's screen instrumentation, so the id has an
engine writer before you add anything. If a wrapper exists, suppress the engine writer for
that id on the path where the native sheet presents, and keep it for any engine-drawn
fallback path — one writer per path. On a retrofit, that suppression modifies existing
instrumentation: it is a gated-tier edit, planned and approved like any other change to
what exists. Wiring the native side without the census double-writes
every presentation.

Ordering caution: engine-side navigation must not happen under an open native sheet —
dismiss first, then navigate — because the runtime exits the topmost screen frame and a
replace under an open overlay mispairs the exits.

## iOS spine case — custom paywall

Enter/Exit at the VC's own lifecycle edges, guarded so *covered* never reads as *exited* (a
vendor sheet presented on top must not close the screen):

```objc
// In the app's own paywall ViewController (.m: plain extern; .mm: extern "C")
extern void UGTSScreenEnter(const char *screenId, bool overlay);
extern void UGTSScreenExit(const char *screenId, const char *outcomeReason);

- (void)viewDidAppear:(BOOL)animated {
    [super viewDidAppear:animated];
    if (self.isBeingPresented || self.isMovingToParentViewController)
        UGTSScreenEnter("scr_paywall", true);   // overlay: the engine screen stays open underneath
}
- (void)viewDidDisappear:(BOOL)animated {
    [super viewDidDisappear:animated];
    if (self.isBeingDismissed || self.isMovingFromParentViewController)
        UGTSScreenExit("scr_paywall", NULL);    // or a registry-declared reason
}
```

`overlay: true` is the default for presented sheets and modals — native storefront sheets
included; `false` only when the native surface replaces navigation outright.

**The IAP boundary — never cross it.** The paywall's purchase buttons start the purchase
journey, and that journey stays on the C# IAP-callback seam the parent skill wired
(`SendPurchaseInitiate` through `SendPurchaseFulfilled`). This reference adds the **screen**,
never the funnel: a native `record("purchase_initiate", ...)` beside the C# callback wiring
double-counts every purchase. Surface events are the one exception in kind, not in seam:
storefront browsing facts for a native storefront ride `record(...)` from the same native
lifecycle as the screen — the transaction events never cross the boundary (the one
exception, below, moves `purchase` alone, and only when no C# seam exists). Name those
browsing facts by the standard rules, not ad hoc: the catalog's `paywall_shown` covers the
storefront surfacing; anything beyond it is a custom event, so it takes the mandatory `x_`
prefix AND a registry `custom_events` declaration before the bridge may send it
(`record("x_product_view", ...)` with `x_product_view` declared — an undeclared or
unprefixed name fails validation and never projects).

**The one exception: no C# purchase seam.** When the purchase flow is native-only (the game's
own StoreKit 2 or Play Billing code completes the transaction, and no Unity IAP and no C#
callback ever sees it), the game's own native transaction code sends the `purchase` event
itself, through the package's native entry point: `UnityInsightsEvents_Send("purchase",
paramsJson)` on iOS (`UnityInsightsEvents.h`), `com.unity.insights.events.InsightsEvents.send(
"purchase", paramsJson)` on Android. Params are keyed by wire name, and the receipt keys follow
the placement-rules reference, Store receipt: `apple_jws_representation` (sent even when unverified),
`apple_app_receipt` only when there is no JWS, `google_play_original_json` and
`google_play_signature`. The rules that make it safe:

- Exactly one `purchase` per transaction, sent where the game's code holds the completed
  transaction. Never alongside a C# `SendPurchase` for the same transaction: if any C# seam
  sees it, that seam sends, and the native code only hands it the receipt.
- The exception moves `purchase` only. In a native-only flow, `purchase_initiate`,
  `purchase_failed`, `purchase_cancelled` and `purchase_fulfilled` are not sent from native:
  they have no C# seam to stay on, so that flow sends `purchase` and nothing else of the
  funnel. Wherever a C# seam does see them, they stay there.
- Only the game's own transaction code: no new `Transaction.updates` observer, restore,
  renewal or `queryPurchasesAsync` path, and nothing hooked into a third-party SDK.
- The receipt rules are unchanged: optional, never waited on, never logged.

Swift in an iOS plugin builds into the UnityFramework target, and a framework target cannot
use a bridging header; the package's header is not in UnityFramework's umbrella header
either, so neither the header nor an Objective-C shim is visible to Swift there. The package
exposes the symbol as a plain `extern "C"` function in `UnityInsightsEvents.mm`, so declare
it in Swift with `@_silgen_name`, matching the C signature exactly
(`void UnityInsightsEvents_Send(const char* eventName, const char* paramsJson)`). Declare
nothing else this way. From Objective-C or Objective-C++ purchase code, include
`UnityInsightsEvents.h` instead.

```swift
// In the game's own StoreKit 2 purchase code (UnityFramework: no bridging header)
@_silgen_name("UnityInsightsEvents_Send")
func UnityInsightsEvents_Send(_ eventName: UnsafePointer<CChar>?, _ paramsJson: UnsafePointer<CChar>?)

if case .success(let verification) = try await product.purchase() {
    let transaction = verification.unsafePayloadValue          // verified or not
    let params: [String: Any] = [
        "product": "prod_gems_100",                            // the registry id, not the store id
        "transaction_id": String(transaction.id),
        "price_micro": NSDecimalNumber(decimal: product.price * 1_000_000).int64Value,
        "currency": product.priceFormatStyle.currencyCode,     // ISO 4217, e.g. "USD"
        "apple_jws_representation": verification.jwsRepresentation,
    ]
    let json = String(data: try JSONSerialization.data(withJSONObject: params), encoding: .utf8)!
    UnityInsightsEvents_Send("purchase", json)
    // the game's own verification and finish() logic continues unchanged
}
```

## iOS spine case — ATT completion

The ATT dialog is vendor-owned; the completion handler is app-owned — the handler is the seam:

```objc
extern void UGTSConsentChange(const char *framework, const char *consentStatus, const char *surface);

[ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
    // consent_status is the ConsentState value set: granted / denied / unset / not_required.
    const char *s = (status == ATTrackingManagerAuthorizationStatusAuthorized) ? "granted"
                  : (status == ATTrackingManagerAuthorizationStatusDenied
                     || status == ATTrackingManagerAuthorizationStatusRestricted) ? "denied"
                  : "unset";                                     // not determined
    UGTSConsentChange("att", s, "att_prompt");
}];
```

The managed half maps the string onto `InsightsConsentState` and calls
`Events.SendConsentChange(ConsentFramework.Att, surface, consentStatus)`. This records the event only. Whether an ATT outcome also moves `EndUserConsent` and the
`CollectorGate` is the app's declared consent policy, wired in C# where that policy lives.
An app-owned pre-prompt explainer screen, if one exists, is a native-shell screen in its own
right.

## Android spine case — paywall Activity or DialogFragment

Enter once per real presentation, exit only on real dismissal — never on rotation, never on
plain backgrounding. Back-press is a dismissal like any other (`isFinishing()` is true).

```java
// Activity form. The entered flag rides savedInstanceState so a rotated
// (recreated) instance knows the enter already happened.
private boolean ugtsEntered;

@Override protected void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);
    // Honor the flag only within the same process: after a process kill the restored,
    // visible surface MUST re-enter. The pid token tells rotation (same process) apart
    // from restore (new process).
    ugtsEntered = savedInstanceState != null
        && savedInstanceState.getBoolean("ugts_entered")
        && savedInstanceState.getLong("ugts_pid") == android.os.Process.myPid();
}
@Override protected void onSaveInstanceState(Bundle out) {
    super.onSaveInstanceState(out);
    out.putBoolean("ugts_entered", ugtsEntered);
    out.putLong("ugts_pid", android.os.Process.myPid());
}
@Override protected void onStart() {
    super.onStart();
    // Latch from the ACCEPTED return, never from having called: a screen that starts
    // before the managed bridge is installed gets a dropped enter — the false return
    // leaves the flag unset, so the next onStart (or resume) retries instead of the
    // screen silently never entering for its whole lifetime.
    if (!ugtsEntered) { ugtsEntered = UGTSNativeBridge.screenEnter("scr_paywall", true); }
}
@Override protected void onStop() {
    super.onStop();
    // Real dismissal only: rotation (isChangingConfigurations) and backgrounding are
    // neither — covered is not exited, the same rule as the iOS isBeingDismissed guard.
    // ugtsEntered guards the exit too: a screen whose enter was never accepted must not
    // call screenExit — Android's warning-only mismatch rule could otherwise pop the
    // underlying engine screen's frame instead of this one's.
    if (ugtsEntered && isFinishing() && !isChangingConfigurations()) {
        UGTSNativeBridge.screenExit("scr_paywall", null);
        ugtsEntered = false;
    }
}
```

DialogFragment analog: same flag-with-pid-token; enter in `onStart`; exit in `onStop` guarded
by `(isRemoving() || requireActivity().isFinishing()) && !requireActivity().isChangingConfigurations()`.

One more path needs its own exit: an Activity `finish()`ed while already stopped runs
`onDestroy` without another `onStop`, and a frame left open there is not harmless — the
strict LIFO rule makes the dangling screen frame mismatch the NEXT exit (parent or session),
dropping a valid close. So `onDestroy` is the fallback exit, guarded by the same flag so a
normal dismissal (which already exited in `onStop` and cleared the flag) can never exit twice:

```java
@Override protected void onDestroy() {
    super.onDestroy();
    if (ugtsEntered && isFinishing()) {
        UGTSNativeBridge.screenExit("scr_paywall", null);
        ugtsEntered = false;
    }
}
```

(DialogFragment: the same guarded fallback in `onDestroyView`.)

## Android spine case — consent-form completion

The CMP's form UI is vendor-owned; the completion listener the app registers is the seam:

```java
consentForm.show(activity, formError -> {
    String consentStatus;                                         // ConsentState values only
    switch (consentInformation.getConsentStatus()) {
        // OBTAINED says the player answered, not what: report granted/denied only when the
        // app reads the actual choice (e.g. the TCF purpose consents); otherwise omit it.
        case ConsentInformation.ConsentStatus.OBTAINED:     consentStatus = null;           break;
        case ConsentInformation.ConsentStatus.REQUIRED:     consentStatus = "unset";        break;
        case ConsentInformation.ConsentStatus.NOT_REQUIRED: consentStatus = "not_required"; break;
        default:                                            consentStatus = null;           break;
    }
    UGTSNativeBridge.consentChange("cmp", consentStatus, "ump_form");
});
```

The consent event records the surface; it never changes consent or what is sent. Moving
`EndUserConsent` (and the `CollectorGate`) is the app's consent policy, done in C# before the
event.

## Verify addendum

For every wired native surface, drive it open and closed in the scripted traversal and
assert one paired enter/exit per presentation — and, where a vendor adapter is active,
exactly one screen record per presentation in the vendor debug view: no auto-tracked VC
rows, no doubles from a surviving engine-side writer.
