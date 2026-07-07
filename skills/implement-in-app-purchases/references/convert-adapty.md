# Adapty — Conversion Assessment and Guidance

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Overview](#overview)
- [Step 1 — Check If Already in Observer Mode](#step-1--check-if-already-in-observer-mode)
- [Step 2 — Feature Support Check](#step-2--feature-support-check)
- [Step 3 — Native Google BillingClient Check](#step-3--native-google-billingclient-check)
- [Step 4 — Route to Outcome](#step-4--route-to-outcome)
- [Case 1 — Already in Observer Mode with Unity IAP 5](#case-1--already-in-observer-mode-with-unity-iap-5)
- [Case 2 — Unsupported Features Detected](#case-2--unsupported-features-detected)
- [Case 3 — No Blockers, Conversion Is Viable](#case-3--no-blockers-conversion-is-viable)

Use this reference when the project has Adapty (`AdaptySDK-Unity`) installed and the user wants to **replace or remove Adapty** and switch to Unity IAP 5 (`com.unity.purchasing`). This path does not cover adding Unity IAP 5 alongside Adapty for non-IAP purposes.

---

## Trigger Phrases

- "Replace Adapty SDK with Unity IAP"
- "Remove Adapty and use Unity IAP"
- "Migrate from Adapty to Unity IAP"
- "Use Unity IAP 5 to handle store purchases and work with Adapty"
- "Make Adapty work with Unity IAP 5"

---

## Overview

Adapty is architecturally independent from Unity IAP — it has its own native Swift (iOS) and Kotlin (Android) SDKs that call Apple StoreKit and Google BillingClient directly. There is no simple swap. Conversion requires assessing which Adapty features the project uses and whether Unity IAP 5 can replace them.

This path always produces one of three outcomes:

| Case | Condition | Outcome |
|---|---|---|
| **1** | Already in observer mode with Unity IAP 5 | Report — no action needed |
| **2** | Adapty-only features are in active use | Report limitations, present two choices |
| **3** | No blockers — only basic purchase flow used | Present two conversion choices |

Run Steps 1–3 in order. Collect all findings before routing.

---

## Step 1 — Check If Already in Observer Mode

### 1a — Check Unity IAP 5 is installed

Search `Packages/manifest.json` for `com.unity.purchasing` with a version matching `5\.`. Record whether it is present.

### 1b — Check Adapty observer mode configuration

Search `Assets/**/*.cs` for:

```
Adapty\.Activate.*observerMode|observerMode\s*=\s*true|AdaptyProfileParameters|ReportTransaction
```

Also search for `AdaptyObserverModeDelegate` or any class implementing it.

If found, observer mode is active.

**If both Unity IAP 5 is installed AND observer mode is active → this is Case 1. Skip Step 2 and go directly to [Case 1](#case-1--already-in-observer-mode-with-unity-iap-5).**

Otherwise continue to Step 2.

---

## Step 2 — Feature Support Check

Search `Assets/**/*.cs`, `Assets/**/*.prefab`, and `Assets/**/*.unity` for usage of Adapty features that have **no Unity IAP 5 equivalent**. Record every feature found.

### 2a — Remote Paywalls / Paywall Builder

```
GetPaywall|PaywallView|AdaptyUI|AdaptyPaywall|GetPaywallForDefaultAudience|ShowPaywall|PresentCodeRedemptionSheet
```

Also check `Packages/manifest.json` or imported packages for `adapty-ui` or `AdaptyUI`.

**What it means:** Adapty's Paywall Builder lets you configure paywall layouts and copy remotely without an app update. Unity IAP 5 has no remote paywall system — all products and UI must be defined in code or a local catalog. **Note: Paywall Builder is also unavailable in Adapty's own Observer Mode**, so even a partial migration loses this feature.

### 2b — A/B Testing / Experiments

```
variationId|GetPaywall.*audience|LogShowPaywall|LogStartCheckout
```

**What it means:** Adapty runs A/B tests on paywall layouts and pricing server-side. Unity IAP 5 has no A/B testing capability. In Adapty Observer Mode, A/B testing also requires significant additional coding and is not automatic.

### 2c — Cross-Platform Entitlement Sync

```
Adapty\.Identify|Adapty\.Profile|AdaptyProfile|accessLevels|isActive
```

Specifically look for `Adapty.Identify` being called with a customer user ID — this indicates the project relies on Adapty as the cross-platform entitlement source of truth (a user who buys on iOS retains access on Android). Unity IAP 5 has no cross-platform entitlement layer.

### 2d — Webhook-Driven Backend Events

Search `Assets/**/*.cs` for patterns suggesting backend subscription event handling:

```
webhook|SubscriptionCancelled|BillingIssue|AccessLevelUpdated
```

Also ask the user: *"Does your backend receive Adapty webhook events for subscription renewals, cancellations, or billing issues?"* If yes, flag this — Unity IAP 5 delivers no server-side lifecycle events.

### 2e — Third-Party Analytics Integrations

```
AdaptyAttributionNetwork|UpdateAttribution|SetFallbackPaywalls|Amplitude|Mixpanel|AppsFlyer|Adjust
```

**What it means:** Adapty forwards purchase and paywall events to third-party analytics tools automatically. Unity IAP 5 has no such integration layer — you would need to instrument each event manually.

---

## Step 3 — Native Google BillingClient Check

Search the following for custom native billing code alongside Adapty:

- `Assets/**/*.cs` for: `AndroidJavaObject|AndroidJavaClass|BillingClient|BillingManager|GoogleBilling`
- `Assets/Plugins/Android/**/*.java`, `*.kt` for: `com\.android\.billingclient`

**If found:** The project has custom native Google BillingClient code in addition to Adapty. This is unusual — flag it to the user. Native BillingClient code will conflict with Unity IAP 5 at runtime and must be removed or replaced as part of the conversion. Add it to the blockers list in Case 2 if active and not just scaffolding.

---

## Step 4 — Route to Outcome

Evaluate findings from Steps 2 and 3:

```
If native BillingClient detected (Step 3)      → Case 2 (flag as additional blocker)
If any Step 2 feature detected                 → Case 2
If no blockers from Steps 2 or 3              → Case 3
```

If multiple blockers are found, list all of them in the Case 2 report — do not stop at the first.

---

## Case 1 — Already in Observer Mode with Unity IAP 5

**Condition:** Unity IAP 5 installed and Adapty already configured in observer mode.

Report and stop:

> "This project already has Unity IAP 5 installed and Adapty is running in observer mode. Unity IAP 5 handles purchase transactions and Adapty validates and tracks them server-side via `ReportTransaction`.
>
> If you want to remove Adapty entirely, re-run this skill and specify that you want to stop using Adapty. That will route to a full removal assessment."

Do not make any changes.

---

## Case 2 — Unsupported Features Detected

**Condition:** One or more Adapty-only features are in active use.

Produce a blockers report listing every issue found, then present two choices:

> "The following Adapty features are in use that have no Unity IAP 5 equivalent:
>
> [List all detected blockers, e.g.:]
> - **Remote Paywalls / Paywall Builder** (`GetPaywall` / `AdaptyUI` detected): Unity IAP 5 has no remote paywall system. Paywall layouts and copy must be hardcoded. Note: Adapty's Paywall Builder is also unavailable in Adapty Observer Mode — switching to observer mode does not preserve this feature.
> - **A/B Testing** (`variationId` / `logShowPaywall` detected): Unity IAP 5 has no A/B testing. In Adapty Observer Mode, A/B testing requires significant additional manual instrumentation and is not automatic.
> - **Cross-platform entitlement sync** (`Adapty.Identify` detected): Unity IAP 5 has no cross-platform entitlement layer. A user who purchases on iOS will not retain access on Android without a custom server-side solution.
> - **Webhook-driven backend events**: Your backend appears to receive Adapty subscription events. Unity IAP 5 delivers no server-side lifecycle events — you would need to build your own subscription tracking infrastructure.
> - **Third-party analytics integrations**: Adapty forwards purchase events to [detected tools]. Unity IAP 5 has no integration layer — each event would need manual instrumentation.
>
> **Your options:**
>
> **(a) Stop conversion (recommended)** — Keep Adapty as the billing backend. The features in use have no Unity IAP 5 equivalent. No changes will be made.
>
> **(b) Convert to observer mode + Unity IAP 5 anyway** — Unity IAP 5 takes over purchase transactions. Adapty switches to observer mode for server-side validation and lifecycle tracking. You accept the following consequences:
> - Adapty Paywall Builder stops working — you must build replacement paywall UI in Unity.
> - [List each unsupported feature and its specific consequence.]
> - BillingClient Gradle conflict between Unity IAP 5 and Adapty must be resolved by excluding the conflicting dependency.
>
> Which would you like to do?"

If the user chooses **(a)**: stop, make no changes.

If the user chooses **(b)**: continue with [Case 3, Path A](#path-a--convert-to-observer-mode--unity-iap-5) but preface the report with a clear warning documenting every accepted consequence.

---

## Case 3 — No Blockers, Conversion Is Viable

**Condition:** No Adapty-only features detected — the project uses Adapty only for basic purchase flow.

Present two choices:

> "No blockers were found. The project uses Adapty only for basic purchase initiation and receipt handling (no remote paywalls, no cross-platform entitlements, no A/B testing). Two conversion paths are available:
>
> **(a) Observer mode + Unity IAP 5** — Unity IAP 5 handles purchase transactions. Adapty stays in observer mode, receiving `ReportTransaction` calls for server-side validation and subscription lifecycle tracking. Lower risk — Adapty's subscription event webhooks and analytics continue to work.
>
> **(b) Full removal — Unity IAP 5 only** — Adapty is removed entirely. Unity IAP 5 handles all billing. You lose Adapty's server-side receipt validation, subscription lifecycle tracking, and analytics. If the project had any Adapty paywall UI, replacement purchase UI must be built in Unity. Simpler architecture, no Adapty subscription cost.
>
> Which path would you like?"

---

### Path A — Convert to Observer Mode + Unity IAP 5

1. **Install Unity IAP 5** if not already present — follow Step 1 of [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md).

2. **Resolve the BillingClient Gradle conflict.** Both SDKs bundle `com.android.billingclient`. Unity IAP 5 declares `com.android.billingclient:billing:9.0.0` via `Plugins/UnityPurchasing/Android/IAPResolver/IAPAndroidDependencies.cs` — do **not** add a project-wide `configurations.all { exclude group: 'com.android.billingclient' }` in `mainTemplate.gradle`, as that will strip Unity IAP's own BillingClient along with Adapty's and leave the Android build with no BillingClient at all.

   Instead, remove only Adapty's copy:
   - Locate Adapty's EDM4U dependency file (typically `Assets/Adapty/Editor/AdaptyDependencies.xml` or similar) and delete the `<androidPackage spec="com.android.billingclient:billing:..."/>` entry, **or**
   - After Force Resolve, delete the Adapty-contributed `billing-*.aar` from `Assets/Plugins/Android/` and keep the one contributed by Unity IAP.

   Then run **Assets > External Dependency Manager > Android Resolver > Delete Resolved Libraries**, followed by **Force Resolve**.

3. **Enable Adapty observer mode.** In the `Adapty.Activate` call, set `observerMode: true`:

   ```csharp
   var config = new AdaptyConfig("YOUR_PUBLIC_SDK_KEY")
   {
       ObserverMode = true
   };
   Adapty.Activate(config);
   ```

4. **Implement Unity IAP 5 purchase flow.** Follow [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md) using the product catalog extracted from existing Adapty `GetPaywall` calls or hardcoded product IDs found in the codebase.

5. **Call `Adapty.ReportTransaction()` after every confirmed purchase.** In the Unity IAP `OnPurchasePending` handler, after granting content and saving, report the transaction to Adapty:

   ```csharp
   Adapty.ReportTransaction(transactionId, null, (error) =>
   {
       if (error != null) Debug.LogWarning($"Adapty ReportTransaction failed: {error}");
   });
   ```

   On Android, also call `Adapty.RestorePurchases` at app startup to sync any purchases Adapty may have missed.

6. **Produce a verification report** covering:
   - Files changed
   - Gradle conflict resolution confirmed
   - Adapty observer mode configuration location
   - `ReportTransaction` call location in purchase flow
   - Manual steps: verify Adapty dashboard shows purchases from the updated build in sandbox; confirm Paywall Builder is no longer used (unavailable in observer mode)

---

### Path B — Full Removal of Adapty

1. **Extract the product catalog** from existing Adapty usage — search for hardcoded product ID strings and `GetPaywall` / `GetPaywallForDefaultAudience` calls.

2. **Identify any Adapty paywall UI** — search for `AdaptyUI`, `PaywallView`, or `ShowPaywall`. These views must be replaced with custom Unity UI wired to `IAPManager.Buy(productId)`. Inform the user that building replacement shop UI is required before removal.

3. **Implement Unity IAP 5** using the extracted catalog — follow [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md) in full.

4. **Remove Adapty SDK references from code.** Search `Assets/**/*.cs` for `using Adapty;` and all `Adapty.*` calls. Replace purchase calls with the Unity IAP 5 equivalents from the new `IAPManager`. Remove paywall presentation calls — these must be replaced by the new shop UI.

5. **Remove the Adapty package.** Delete the imported Adapty `.unitypackage` files from `Assets/` (typically under `Assets/Adapty/`). Remove any EDM4U dependency files Adapty registered.

6. **Run Assets > External Dependency Manager > Android Resolver > Force Resolve** after package removal to clean up Adapty's native dependencies.

7. **Produce a verification report** covering:
   - Files changed and Adapty references removed
   - Product catalog — confirm all product IDs are preserved in Unity IAP 5
   - Paywall UI — confirm replacement UI is in place or listed as a pending TODO
   - Manual steps: verify no orphaned Adapty Gradle entries remain in the Android build; confirm sandbox purchases succeed end-to-end with Unity IAP 5
