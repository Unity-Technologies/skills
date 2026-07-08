# RevenueCat — Conversion Assessment and Guidance

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Overview](#overview)
- [Step 1 — Check If Already in Observer Mode](#step-1--check-if-already-in-observer-mode)
- [Step 2 — Feature Support Check](#step-2--feature-support-check)
- [Step 3 — Platform Support Check](#step-3--platform-support-check)
- [Step 4 — Route to Outcome](#step-4--route-to-outcome)
- [Case 1 — Already in Observer Mode with Unity IAP 5](#case-1--already-in-observer-mode-with-unity-iap-5)
- [Case 2 — Blocker Detected (Amazon or Unsupported Features)](#case-2--blocker-detected-amazon-or-unsupported-features)
- [Case 3 — No Blockers, Conversion Is Viable](#case-3--no-blockers-conversion-is-viable)

Use this reference when the project has RevenueCat (`com.revenuecat.purchases-unity`) installed and the user wants to **replace or remove RevenueCat** and switch to Unity IAP 5 (`com.unity.purchasing`). This path does not cover adding Unity IAP 5 alongside RevenueCat for non-IAP purposes (e.g., IAP D2C Capabilities).

---

## Trigger Phrases

- "Replace RevenueCat with Unity IAP"
- "Remove RevenueCat and use Unity IAP"
- "Migrate from RevenueCat to Unity IAP"
- "Convert RevenueCat to Unity IAP 5"
- "Stop using RevenueCat"

---

## Overview

RevenueCat is architecturally independent from Unity IAP — it calls Apple StoreKit and Google BillingClient directly via its own native SDKs. Because of this, there is no simple "swap" — conversion requires assessing what RevenueCat features the project uses, what platforms it targets, and how much of RevenueCat's value Unity IAP 5 can actually replace.

This path always produces one of three outcomes:

| Case | Condition | Outcome |
|---|---|---|
| **1** | Already in observer mode with Unity IAP 5 | Report — no action needed |
| **2** | Blocker found: Amazon store targeted, or RevenueCat-only features in use | Report blockers, present two choices |
| **3** | No blockers — no Amazon, no RevenueCat-only features | Present two conversion choices |

Run Steps 1–3 in order. Collect all findings before routing.

---

## Step 1 — Check If Already in Observer Mode

### 1a — Check Unity IAP 5 is installed

Search `Packages/manifest.json` for `com.unity.purchasing` with a version matching `5\.`. Record whether it is present.

### 1b — Check RevenueCat observer mode configuration

Search `Assets/**/*.cs` for:

```
PurchasesAreCompletedBy\.MyApp|PurchasesAreCompletedBy\.YourApp|observerMode\s*=\s*true|SetPurchasesAreCompletedBy
```

If found, observer mode is active.

**If both Unity IAP 5 is installed AND observer mode is active → this is Case 1. Skip Steps 2–3 and go directly to [Case 1](#case-1--already-in-observer-mode-with-unity-iap-5).**

Otherwise continue to Step 2.

---

## Step 2 — Feature Support Check

Search `Assets/**/*.cs`, `Assets/**/*.prefab`, and `Assets/**/*.unity` for usage of RevenueCat features that have **no Unity IAP 5 equivalent**. Record every feature found.

### 2a — Remote Paywalls / Offerings

```
GetOfferings|Offerings|Offering\b|Package\b.*revenuecat|RevenueCatUI|PaywallView|PresentPaywall
```

Also check `Packages/manifest.json` for `com.revenuecat.purchases-ui-unity`.

**What it means:** RevenueCat's Offerings system lets you configure products and paywall layouts remotely without an app update. Unity IAP 5 has no remote paywall or Offerings system — all products must be defined in code or a local catalog.

### 2b — A/B Testing / Experiments

```
GetCurrentOffering|Experiments|currentOffering
```

**What it means:** RevenueCat Experiments allows server-side A/B testing of product prices and paywall layouts. Unity IAP 5 has no A/B testing capability.

### 2c — Cross-Platform Entitlement Sync

```
LogIn|LogOut|Purchases\.SharedPurchases|appUserId|CustomerInfo
```

Specifically look for `LogIn` being called with a user ID — this indicates the project relies on RevenueCat as the cross-platform entitlement source of truth (a user who buys on iOS retains access on Android). Unity IAP 5 has no cross-platform entitlement layer — each platform's receipt is independent.

### 2d — Webhook-Driven Backend Events

Search `Assets/**/*.cs` for patterns suggesting server-side subscription event handling:

```
webhook|SubscriptionStatusChange|EntitlementRevoked|BillingIssue
```

Also ask the user: *"Does your backend receive RevenueCat webhook events for subscription renewals, cancellations, or billing issues?"* If yes, flag this — Unity IAP 5 delivers no server-side lifecycle events.

### 2e — Offline Entitlement Caching

```
offlineCustomerInfo|OfflineEntitlements|entitlementVerification
```

**What it means:** RevenueCat's offline entitlement feature processes and caches entitlements during server outages. Unity IAP 5 has no equivalent — if the store is unreachable, entitlement state is unavailable.

---

## Step 3 — Platform Support Check

### 3a — Native Google BillingClient detection

Search the following for custom native billing code alongside RevenueCat:

- `Assets/**/*.cs` for: `AndroidJavaObject|AndroidJavaClass|BillingClient|BillingManager|GoogleBilling`
- `Assets/Plugins/Android/**/*.java`, `*.kt` for: `com\.android\.billingclient`

**If found:** The project has custom native Google BillingClient code in addition to RevenueCat. This is unusual — flag it to the user. Native BillingClient code will conflict with Unity IAP 5 at runtime and must be removed or replaced as part of the conversion. Add it to the blockers list in Case 2 if native BillingClient code is active and not just scaffolding.

### 3b — Amazon Appstore detection

Search the following for Amazon signals:

- `Packages/manifest.json` and `Packages/packages-lock.json` for: `amazon`
- `Assets/**/*.cs` for: `useAmazon|SetUseAmazon|AmazonStore|SyncAmazonPurchase`
- `ProjectSettings/ProjectSettings.asset` for Amazon as an enabled build target

**If found:** Amazon is a hard blocker. Unity IAP 5 has removed Amazon Appstore support entirely. RevenueCat observer mode is also broken for Amazon — `syncPurchases()` does not work on Amazon builds and requires `syncAmazonPurchase()` with full purchase details.

---

## Step 4 — Route to Outcome

Evaluate findings from Steps 2 and 3:

```
If Amazon detected (Step 3b)                    → Case 2
If native BillingClient detected (Step 3a)      → Case 2 (flag as additional blocker)
If any Step 2 feature detected                  → Case 2
If no blockers from Steps 2 or 3               → Case 3
```

If multiple blockers are found, list all of them in the Case 2 report — do not stop at the first.

---

## Case 1 — Already in Observer Mode with Unity IAP 5

**Condition:** Unity IAP 5 installed and RevenueCat already configured in observer mode.

Report and stop:

> "This project already has Unity IAP 5 installed and RevenueCat is running in observer mode (`PurchasesAreCompletedBy.MyApp`). The two SDKs are already co-operating — Unity IAP 5 handles purchase transactions and RevenueCat validates and tracks them server-side.
>
> If you want to remove RevenueCat entirely, re-run this skill and specify that you want to stop using RevenueCat. That will route to a full removal assessment."

Do not make any changes.

---

## Case 2 — Blocker Detected (Amazon or Unsupported Features)

**Condition:** Amazon store is targeted, or one or more RevenueCat-only features are in active use.

Produce a blockers report listing every issue found, then present two choices:

> "The following blockers were found that prevent a clean conversion to Unity IAP 5:
>
> [List all detected blockers, e.g.:]
> - **Amazon Appstore**: Unity IAP 5 does not support Amazon. RevenueCat observer mode does not work on Amazon builds either — `syncPurchases()` is broken for Amazon and requires manual `syncAmazonPurchase()` calls. Dropping Amazon support is the only viable path if Unity IAP 5 is the sole billing backend.
> - **Cross-platform entitlement sync** (`LogIn` detected): Unity IAP 5 has no cross-platform entitlement layer. A user who purchases on iOS will not retain access on Android without a custom server-side solution.
> - **Remote Paywalls / Offerings**: Unity IAP 5 has no remote paywall system. All products and paywall layouts must be defined in code or a local catalog.
> - **Subscription webhooks**: Your backend appears to receive RevenueCat webhook events. Unity IAP 5 delivers no server-side lifecycle events — you would need to build your own subscription tracking infrastructure.
>
> **Your options:**
>
> **(a) Stop conversion (recommended)** — Keep RevenueCat as the billing backend. The features and platforms in use have no Unity IAP 5 equivalent. No changes will be made.
>
> **(b) Convert to observer mode + Unity IAP 5 anyway** — Unity IAP 5 takes over purchase transactions for Google Play and Apple App Store. RevenueCat switches to observer mode and continues server-side validation and lifecycle tracking. You accept the following consequences:
> - Amazon Appstore billing will stop working.
> - [List each unsupported feature and its consequence.]
> - BillingClient Gradle conflict between Unity IAP 5 and RevenueCat must be resolved by excluding the conflicting dependency.
>
> Which would you like to do?"

If the user chooses **(a)**: stop, make no changes.

If the user chooses **(b)**: continue with [Case 3, Path A](#path-a--convert-to-observer-mode--unity-iap-5) but preface the report with a clear warning documenting every accepted consequence.

---

## Case 3 — No Blockers, Conversion Is Viable

**Condition:** No Amazon support, no RevenueCat-only features detected.

Present two choices:

> "No blockers were found. The project uses RevenueCat only for basic purchase flow (no remote paywalls, no cross-platform entitlements, no Amazon). Two conversion paths are available:
>
> **(a) Observer mode + Unity IAP 5** — Unity IAP 5 handles purchase transactions. RevenueCat stays in observer mode for server-side validation and subscription lifecycle tracking. Lower risk — RevenueCat's `CustomerInfo` and webhook delivery continue to work.
>
> **(b) Full removal — Unity IAP 5 only** — RevenueCat is removed entirely. Unity IAP 5 handles all billing. You lose RevenueCat's server-side receipt validation, subscription lifecycle tracking, and analytics. Simpler architecture, no RevenueCat subscription cost.
>
> Which path would you like?"

---

### Path A — Convert to Observer Mode + Unity IAP 5

1. **Install Unity IAP 5** if not already present — follow Step 1 of [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md).

2. **Resolve the BillingClient Gradle conflict.** Both SDKs bundle `com.android.billingclient`. Unity IAP 5 declares `com.android.billingclient:billing:9.0.0` via `Plugins/UnityPurchasing/Android/IAPResolver/IAPAndroidDependencies.cs` — do **not** add a project-wide `configurations.all { exclude group: 'com.android.billingclient' }` in `mainTemplate.gradle`, as that will strip Unity IAP's own BillingClient along with RevenueCat's and leave the Android build with no BillingClient at all.

   Instead, remove only RevenueCat's copy:
   - Locate RevenueCat's EDM4U dependency file (typically `Assets/RevenueCat/Editor/RevenueCatDependencies.xml` or similar) and delete the `<androidPackage spec="com.android.billingclient:billing:..."/>` entry, **or**
   - After Force Resolve, delete the RevenueCat-contributed `billing-*.aar` from `Assets/Plugins/Android/` and keep the one contributed by Unity IAP.

   Then run **Assets > External Dependency Manager > Android Resolver > Delete Resolved Libraries**, followed by **Force Resolve**.

3. **Switch RevenueCat to observer mode.** In the `Purchases` configuration call, set:

   ```csharp
   var config = PurchasesConfiguration.Builder.Init("your_api_key")
       .SetPurchasesAreCompletedBy(PurchasesAreCompletedBy.MyApp)
       .Build();
   Purchases.Configure(config);
   ```

4. **Implement Unity IAP 5 purchase flow.** Follow [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md) using the product catalog extracted from the existing RevenueCat `Purchases.GetOfferings()` or hardcoded product IDs found in the codebase.

5. **Call `syncPurchases()` after every confirmed purchase.** In the Unity IAP `OnPurchasePending` handler, after granting content and saving, call:

   ```csharp
   Purchases.SharedPurchases.SyncPurchases();
   ```

   This registers the purchase token with RevenueCat's backend so it can validate the receipt and update `CustomerInfo`.

6. **Produce a verification report** covering:
   - Files changed
   - Gradle conflict resolution confirmed
   - RevenueCat observer mode configuration location
   - `SyncPurchases()` call location in purchase flow
   - Manual steps: verify RevenueCat dashboard shows purchases from the updated build in sandbox

---

### Path B — Full Removal of RevenueCat

1. **Extract the product catalog** from existing RevenueCat usage — search for hardcoded product ID strings and `GetOfferings()` calls.

2. **Implement Unity IAP 5** using the extracted catalog — follow [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md) in full.

3. **Remove RevenueCat SDK references from code.** Search `Assets/**/*.cs` for `using RevenueCat;` and all `Purchases.*` calls. Replace with the Unity IAP 5 equivalents from the new `IAPManager`.

4. **Remove RevenueCat packages** from `Packages/manifest.json`:
   - `com.revenuecat.purchases-unity`
   - `com.revenuecat.purchases-ui-unity` (if present)

   Remove the OpenUPM scoped registry entry for `com.revenuecat` from `manifest.json` if no other RevenueCat packages remain.

5. **Run Assets > External Dependency Manager > Android Resolver > Force Resolve** after package removal to clean up RevenueCat's native dependencies.

6. **Produce a verification report** covering:
   - Files changed and RevenueCat references removed
   - Product catalog — confirm all product IDs are preserved in Unity IAP 5
   - Manual steps: remove the RevenueCat project from the RevenueCat dashboard if no longer needed; verify no orphaned RevenueCat Gradle entries remain in the Android build
