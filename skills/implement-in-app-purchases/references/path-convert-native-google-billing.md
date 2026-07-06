# Convert Native Google Billing to Unity IAP 5

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Important Constraints](#important-constraints)
- [Step 1 — Pre-Conversion Scan](#step-1--pre-conversion-scan)
- [Step 2 — Product Classification](#step-2--product-classification)
- [Step 3 — Migration Report (produce before any edits)](#step-3--migration-report-produce-before-any-edits)
- [Step 4 — Blocker Detection](#step-4--blocker-detection)
- [Step 5 — Target Architecture](#step-5--target-architecture)
- [Step 6 — Migration Phases](#step-6--migration-phases)
- [Step 7 — Behavior Change Rules](#step-7--behavior-change-rules)
- [Step 8 — Native → Unity IAP Concept Mapping](#step-8--native--unity-iap-concept-mapping)
- [Step 9 — Rollback Plan](#step-9--rollback-plan)
- [Step 10 — Test Checklist](#step-10--test-checklist)
- [Step 11 — Questions to Ask (only when required)](#step-11--questions-to-ask-only-when-required)

Use this reference when the project uses C# → Java/Kotlin AndroidJavaObject calls to Google Play BillingClient and needs to be migrated to `com.unity.purchasing`. Always install the latest stable version.

## Trigger Phrases

- "Convert native Google Billing to the latest Unity IAP"
- "Migrate BillingClient integration to Unity IAP"
- "Replace AndroidJavaObject Google billing bridge with Unity IAP"
- "Upgrade custom Google Play Billing to the latest stable version of com.unity.purchasing"
- "Remove native BillingClient and use Unity IAP"

## Important Constraints

- **Do not delete existing native billing files unless explicitly requested.** Always preserve with `#if !USE_UNITY_IAP_V5` guards.
- **Create the new Unity IAP adapter first**, then mark the old native adapter as deprecated.
- **Preserve public game-facing APIs** via a compatibility facade so gameplay code does not need large rewrites.
- **Do not change product IDs.**
- **Do not change backend validation endpoints** unless the receipt format change makes it unavoidable — document any backend change required.
- **Do not grant purchases on the client** if the existing project uses server-side validation.
- **Do not assume all products are consumables.** Detect and classify each product.
- **Do not assume subscriptions are simple.** Detect base plans, offer tokens, pricing phases, and `SubscriptionOfferDetails`. If found, surface as a migration blocker and let the user choose a path (see Subscription Blocker Options below).
- **If the project contains RevenueCat, Adapty, or Essential Kit**, stop and notify the user — this skill does not support converting those packages.
- **If Google-specific features are not cleanly expressible in Unity IAP**, report them as migration blockers rather than silently removing them.

## Step 1 — Pre-Conversion Scan

Scan before making any changes. Do not edit files until the migration report is accepted.

### 1a — Unity C# Bridge Patterns

Search `Assets/**/*.cs`:

```
AndroidJavaObject|AndroidJavaClass|UnityPlayer\.currentActivity
UnitySendMessage
GoogleBilling|BillingManager|BillingClient|BillingBridge|AndroidBillingBridge|PlayBilling
purchaseToken|originalJson|signature|productId|orderId
\backnowledge\b|\bconsume\b
restore.?purchases|query.?products|query.?purchases
subscription.?status|receipt.?valid|entitlement.?grant
```

### 1b — Android Java/Kotlin Patterns

Search `Assets/Plugins/Android/**/*.java`, `*.kt`:

```
com\.android\.billingclient\.api\.BillingClient
PurchasesUpdatedListener|BillingClientStateListener
ProductDetails|ProductDetails\.SubscriptionOfferDetails
QueryProductDetailsParams|BillingFlowParams
AcknowledgePurchaseParams|ConsumeParams|QueryPurchasesParams
launchBillingFlow|acknowledgePurchase|consumeAsync
queryProductDetailsAsync|queryPurchasesAsync|enablePendingPurchases
offerToken|basePlanId|offerId|pricingPhases
```

### 1c — Gradle / Manifest Patterns

Search `mainTemplate.gradle`, `launcherTemplate.gradle`, `baseProjectTemplate.gradle`, `AndroidManifest.xml`:

```
com\.android\.billingclient:billing
com\.android\.billingclient:billing-ktx
```

Also list all `.jar`/`.aar` files under `Assets/Plugins/Android/` — these may contain pre-compiled billing code.

### 1d — Scene / Prefab / Asset Scan

Search `.unity`, `.prefab`, `.asset` files for references to billing-related MonoBehaviour names found in 1a.

## Step 2 — Product Classification

Classify every detected product into one of: **Consumable**, **NonConsumable**, **Subscription**, **Unknown**.

Infer from (in priority order):
1. `BillingClient.ProductType.INAPP` (consumable or non-consumable) / `BillingClient.ProductType.SUBS`
2. `consumeAsync` calls → Consumable; `acknowledgePurchase` without consume → NonConsumable
3. Product ID naming conventions (e.g., `coins`, `gems`, `pack` → Consumable; `remove_ads`, `unlock` → NonConsumable; `monthly`, `annual`, `sub` → Subscription)
4. Entitlement / backend grant code
5. Comments or config files

If type cannot be confidently inferred, mark as **Unknown** and add to the blocker question list.

## Step 3 — Migration Report (produce before any edits)

Generate all 18 sections. Do not skip any.

1. **Current native billing architecture** — describe the bridge pattern (C# wrapper → JNI → BillingClient)
2. **Files using native Google Billing** — full list with role of each file
3. **Product catalog discovered** — all product IDs found
4. **Product type mapping** — inferred type + confidence + source of inference for each product
5. **Purchase flow mapping** — native → Unity IAP step-by-step
6. **Restore purchase flow mapping** — `queryPurchasesAsync` → `FetchPurchases` + `RestoreTransactions`
7. **Backend validation mapping** — what data the old flow sent (`purchaseToken`, `originalJson`, `signature`, `packageName`) vs what Unity IAP provides (`order.Info.Receipt`, `order.Info.Apple?.jwsRepresentation`)
8. **Consumable consume behavior mapping** — native `consumeAsync` → Unity IAP `ConfirmPurchase` (only after grant)
9. **Non-consumable acknowledgement behavior mapping** — native `acknowledgePurchase` → Unity IAP `ConfirmPurchase` (only after grant)
10. **Subscription behavior mapping** — detected subscription products, restore path, renewal handling
11. **Google-specific features detected** — list each (see Blocker Detection below)
12. **Unity IAP 5.2 migration blockers** — features that cannot be cleanly mapped
13. **Proposed new Unity IAP architecture** — new files and their roles
14. **Code files to create** — with paths
15. **Code files to modify** — with nature of each change
16. **Manual Play Console checks** — product IDs, base plan compatibility, billing permissions
17. **Test plan** — from the checklist in this document
18. **Rollback plan** — from the rollback plan section in this document

## Step 4 — Blocker Detection

Report these as migration blockers if detected. Do not silently remove them.

| Feature | Detection Signal | Blocker Level |
|---|---|---|
| Multiple subscription base plans | `basePlanId` usage, multiple `SubscriptionOfferDetails` per product | **Hard** — ask user to choose option A/B/C (see below) |
| Explicit offer token selection | `offerToken` set on `BillingFlowParams` | **Hard** |
| Introductory offer selection by `offerId` | `offerId` in offer params | **Hard** |
| Offer tags | `offerTags` access on `SubscriptionOfferDetails` | **Soft** — document loss |
| Pricing phase inspection | `pricingPhases` / `PricingPhase` fields read | **Soft** — document loss |
| Multi-quantity purchases | `quantity` > 1 in `BillingFlowParams` | **Hard** |
| Alternative billing / external offers | `setExternalOfferToken` or alternative billing config | **Hard** |
| Personalized price disclosure | `setIsPersonalizedPrice(true)` | **Hard** |
| Obfuscated account / profile IDs | `setObfuscatedAccountId`, `setObfuscatedProfileId` | **Soft** — Unity IAP exposes `SetObfuscatedAccountId/ProfileId` post-`Connect()` on `GooglePlayStoreExtendedService` |
| Custom `BillingFlowParams` fields | Any `BillingFlowParams.Builder` method not listed above | **Soft** — document loss |
| Direct `ProductDetails` metadata | Reading `ProductDetails` fields not available on `Product.metadata` | **Soft** — document loss |

### Subscription Blocker Options (present to user when hard subscription blockers found)

**A mixed Unity IAP + native BillingClient architecture is not recommended and must not be offered as an option.** Both systems compete for the same `PurchasesUpdatedListener` slot on the device — only one receives purchase callbacks, the other silently drops purchases. This risks lost purchases, double grants, and unacknowledged tokens. If the user asks about a mixed approach, explain this risk and redirect to Option A or B.

**Option A — Simplify Play Console setup for Unity IAP compatibility**
Remove extra base plans/offers in Play Console so one product has one base plan. Unity IAP then handles all products including subscriptions. This is the recommended path — single system, no coexistence risk.

**Option B — Stay on native Google Billing for all products**
Do not migrate at this time. Keep the native BillingClient implementation as-is until the Play Console subscription configuration can be simplified or Unity IAP exposes the required subscription features. Stop the conversion and document the blocker and this decision in `IapMigrationNotes.md`.

## Step 5 — Target Architecture

Create under `Assets/Scripts/IAP/`:

| File | Purpose |
|---|---|
| `IStorePurchaseService.cs` | Game-facing interface (stable public API) |
| `UnityIapStorePurchaseService.cs` | Unity IAP 5.x implementation |
| `ProductCatalogDefinition.cs` | ScriptableObject or plain class holding all `ProductDefinitionEntry` |
| `ProductDefinitionEntry.cs` | Per-product data: ID, type, store-specific IDs |
| `PurchaseEntitlementMapper.cs` | Maps Unity IAP order data → game grant calls |
| `PurchaseValidationRequest.cs` | DTO sent to backend |
| `PurchaseValidationResult.cs` | DTO received from backend |
| `PurchaseRestoreResult.cs` | Result of restore operation |
| `LegacyGoogleBillingAdapterDeprecated.cs` | Compatibility facade (only if game code calls old billing API) |
| `IapMigrationNotes.md` | Human-readable notes on what changed, blockers found, manual steps |

### Game-Facing Interface (preserve or adapt from existing API surface)

```csharp
public interface IStorePurchaseService
{
    Task InitializeAsync();
    Task FetchProductsAsync();
    IReadOnlyList<StoreProductInfo> GetProducts();
    Task PurchaseAsync(string productId);
    Task RestorePurchasesAsync();
    bool IsInitialized { get; }
}
```

## Step 6 — Migration Phases

### Phase 1 — Package Check

- Verify `com.unity.purchasing` exists in `Packages/manifest.json`.
- If `com.unity.purchasing` is absent or outdated, install the latest stable version via Package Manager.
- If missing or outdated, provide Package Manager instructions. Do not auto-edit `manifest.json` unless the user explicitly allows it.

### Phase 2 — Product Catalog Extraction

- Extract all product IDs and inferred types from the scan.
- Generate `ProductCatalogDefinition.cs` / `ProductDefinitionEntry.cs`.
- Each entry: `productId`, `ProductType`, optional `storeSpecificIds`, `notes`.

### Phase 3 — New Unity IAP Service

Create `UnityIapStorePurchaseService`:

1. **Initialization** — `UnityIAPServices.StoreController()`, subscribe all events **before** `Connect()`.
2. **Product fetching** — build `List<ProductDefinition>` from catalog, call `store.FetchProducts()`, handle `OnProductsFetched` / `OnProductsFetchFailed`.
3. **Purchase initiation** — `store.PurchaseProduct(product)`.
4. **Purchase callback** — `OnPurchasePending`: validate receipt → grant entitlement → `ConfirmPurchase(pendingOrder)`. **Never confirm before grant.**
5. **Deferred purchases** — `OnPurchaseDeferred`: show pending UI, do not grant.
6. **Failed purchases** — `OnPurchaseFailed`: map `FailureReason` to user-facing error.
7. **Restore / fetch** — `store.FetchPurchases()` (re-delivers unconfirmed via `OnPurchasePending`) + `store.RestoreTransactions(callback)` for explicit iOS restore button.
8. **Validation handoff** — send `order.Info.Receipt` (and `order.Info.Apple?.jwsRepresentation` on Apple) to backend in place of legacy `purchaseToken` + `originalJson` + `signature`. Document any backend change required.

### Phase 4 — Compatibility Facade

If game code calls the old billing API directly (e.g., `GoogleBillingManager.Purchase(id)`):

- Create `LegacyGoogleBillingAdapterDeprecated.cs` that implements the same method signatures.
- Delegate all calls to `UnityIapStorePurchaseService`.
- Mark all methods `[Obsolete("Use IStorePurchaseService — remove after migration validation")]`.

### Phase 5 — Remove Native Dependency from Active Path

- Stop calling `AndroidJavaObject` bridge for migrated products.
- Wrap native calls in `#if !USE_UNITY_IAP_V5` — do not delete them.
- Leave Gradle billing dependencies unless user explicitly requests removal.

### Phase 6 — Test Scaffolding

- Add debug logs at each IAP event.
- Add editor stubs for sandbox testing.
- Add the QA checklist from the test plan below to `IapMigrationNotes.md`.

## Step 7 — Behavior Change Rules

### Acknowledgement and Consumption

Native billing requires explicit `acknowledgePurchase` or `consumeAsync`. Unity IAP abstracts both through `ConfirmPurchase(pendingOrder)`. Map:

| Native | Unity IAP |
|---|---|
| `consumeAsync` (consumable) | `ConfirmPurchase(pendingOrder)` after grant |
| `acknowledgePurchase` (non-consumable / subscription) | `ConfirmPurchase(pendingOrder)` after grant |

**Never call `ConfirmPurchase` before the entitlement is safely granted.**

### Backend Validation

| Native field sent | Unity IAP equivalent |
|---|---|
| `purchaseToken` | `order.Info.Receipt` (contains the Google receipt JSON) |
| `originalJson` | Embedded in `order.Info.Receipt` |
| `signature` | Embedded in `order.Info.Receipt` |
| `packageName` | `Application.identifier` |
| `productId` | `pendingOrder.CartOrdered.Items().First().Product.definition.id` |

If the backend parses `purchaseToken` or `originalJson` fields directly, document the receipt format change as a required backend update.

### Obfuscated IDs

If native code sets `setObfuscatedAccountId` / `setObfuscatedProfileId` in `BillingFlowParams`, set the equivalent **after** `await store.Connect()`:

```csharp
store.GooglePlayStoreExtendedService?.SetObfuscatedAccountId("hashed_id");
store.GooglePlayStoreExtendedService?.SetObfuscatedProfileId("hashed_profile_id");
```

Retrieve per-order:

```csharp
string accountId = store.GooglePlayStoreExtendedPurchaseService?.GetObfuscatedAccountId(order);
```

### Pending Purchases

If native code handles `BillingClient.enablePendingPurchases()` or checks purchase state for `PENDING`:

- Unity IAP surfaces deferred purchases via `store.OnPurchaseDeferred`.
- On `OnPurchaseDeferred`: show "pending approval" UI. Do not grant.
- When approved, the store re-delivers via `store.OnPurchasePending`.

### Subscription Upgrade / Downgrade

If native code calls `launchBillingFlow` with a `subscriptionUpdateParams` (prorating an existing subscription):

```csharp
store.GooglePlayStoreExtendedPurchaseService?.UpgradeDowngradeSubscription(
    currentOrder,       // Order object of the current subscription
    newProduct,         // Product to upgrade/downgrade to
    GooglePlayReplacementMode.ChargeFullPrice  // choose appropriate mode
);
```

See [platform-notes.md](platform-notes.md) for all `GooglePlayReplacementMode` values.

## Step 8 — Native → Unity IAP Concept Mapping

| Native Google Billing | Unity IAP 5.x |
|---|---|
| `ProductDetails` | `Product` (via `store.GetProductById`) |
| `Purchase` | `PendingOrder` (on `OnPurchasePending`) |
| `purchaseToken` | `order.Info.Receipt` (contains the token) |
| `originalJson` | embedded in `order.Info.Receipt` |
| `signature` | embedded in `order.Info.Receipt` |
| `acknowledgePurchase` | `store.ConfirmPurchase(pendingOrder)` |
| `consumeAsync` | `store.ConfirmPurchase(pendingOrder)` |
| `queryPurchasesAsync` | `store.FetchPurchases()` → `OnPurchasesFetched` |
| `BillingResult` / `BillingResponseCode` | `FailedOrder.FailureReason` / `StoreConnectionFailureDescription` |
| `ProductType.INAPP` | `ProductType.Consumable` or `ProductType.NonConsumable` (distinguish by consume behavior) |
| `ProductType.SUBS` | `ProductType.Subscription` |
| `offerToken` / `basePlanId` / `pricingPhases` | **Not directly exposed** — report as blocker |

## Step 9 — Rollback Plan

- Wrap all new IAP code in `#if USE_UNITY_IAP_V5`.
- Wrap all replaced native bridge calls in `#if !USE_UNITY_IAP_V5`.
- To revert: remove `USE_UNITY_IAP_V5` from **Edit > Project Settings > Player > Scripting Define Symbols**. The native billing path is restored without any code changes.
- Never activate both purchase systems for the same product simultaneously.
- Document the rollback steps in `IapMigrationNotes.md`.

## Step 10 — Test Checklist

Include this in `IapMigrationNotes.md` after migration:

- [ ] Fresh install — products load and display prices
- [ ] Upgrade from old app version to migrated version — no duplicate entitlements
- [ ] Consumable purchase — content granted
- [ ] Repeat consumable purchase — repeatable, no block
- [ ] Non-consumable purchase — content granted once
- [ ] Non-consumable restore — content restored on reinstall
- [ ] Subscription purchase — subscription active
- [ ] Subscription restore — status refreshed on launch
- [ ] Pending purchase (if applicable) — deferred UI shown, no premature grant
- [ ] Interrupted purchase flow — re-delivered on next launch via `OnPurchasePending`
- [ ] Backend validation success — entitlement granted
- [ ] Backend validation failure — entitlement NOT granted, purchase not confirmed
- [ ] Network failure during validation — purchase left pending, re-delivered on next launch
- [ ] App killed during purchase — pending order re-delivered on next launch
- [ ] App resumed after Google purchase UI — `OnPurchasePending` fires correctly
- [ ] Sandbox tester account — no real charges
- [ ] Internal testing track build
- [ ] Play Console product ID match — all IDs match exactly
- [ ] Play Console base plan compatibility — no advanced offer config that Unity IAP cannot reach
- [ ] No duplicate entitlement grant
- [ ] No unacknowledged purchases remaining
- [ ] No double consumption of consumables

## Step 11 — Questions to Ask (only when required)

Ask only when the information cannot be inferred:

1. Which product IDs are consumable, non-consumable, or subscription?
2. Does the backend currently validate Google purchase tokens directly (raw `purchaseToken` field)?
3. Are any subscriptions using multiple base plans or offer tokens?
4. Should the skill preserve the old public C# billing API as a compatibility facade?
5. Should native billing files be kept, disabled, or removed?

If enough information can be inferred, proceed with a best-effort plan and mark uncertain items as **TODO**.
