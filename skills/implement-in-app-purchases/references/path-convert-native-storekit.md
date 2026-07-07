# Convert Native iOS StoreKit to Unity IAP 5

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Important Constraints](#important-constraints)
- [Step 1 — Pre-Conversion Scan](#step-1--pre-conversion-scan)
- [Step 2 — StoreKit Version Detection](#step-2--storekit-version-detection)
- [Step 3 — Product Classification](#step-3--product-classification)
- [Step 4 — Migration Report (produce before any edits)](#step-4--migration-report-produce-before-any-edits)
- [Step 5 — Blocker Detection](#step-5--blocker-detection)
- [Step 6 — Target Architecture](#step-6--target-architecture)
- [Step 7 — Migration Phases](#step-7--migration-phases)
- [Step 8 — Behavior Change Rules](#step-8--behavior-change-rules)
- [Step 9 — Native → Unity IAP Concept Mapping](#step-9--native--unity-iap-concept-mapping)
- [Step 10 — Rollback Plan](#step-10--rollback-plan)
- [Step 11 — Test Checklist](#step-11--test-checklist)
- [Step 12 — Questions to Ask (only when required)](#step-12--questions-to-ask-only-when-required)

Use this reference when the project uses a custom native iOS StoreKit plugin (Objective-C or Swift, bridged into Unity via `[DllImport("__Internal")]` and `UnitySendMessage`) and needs to be migrated to `com.unity.purchasing`. Always install the latest stable version.

## Trigger Phrases

- "Convert native StoreKit to Unity IAP"
- "Migrate SKPaymentQueue integration to Unity IAP"
- "Replace custom iOS billing plugin with Unity IAP"
- "Remove native StoreKit and use Unity IAP"
- "Migrate DllImport StoreKit bridge to Unity IAP"
- "Replace SKProduct / SKPayment with Unity IAP"

## Important Constraints

- **Do not delete existing native iOS plugin files unless explicitly requested.** Always preserve with `#if !USE_UNITY_IAP_V5` guards on the C# side. ObjC/Swift files cannot use C# defines — guard their call sites in C# instead.
- **Create the new Unity IAP adapter first**, then mark the old native adapter as deprecated.
- **Preserve public game-facing APIs** via a compatibility facade so gameplay code does not need large rewrites.
- **Do not change product IDs.**
- **Do not change backend validation endpoints** unless the receipt format change makes it unavoidable — document any backend change required. The receipt format change from SK1 bundle to per-transaction JWS is a breaking backend change; flag it prominently.
- **Do not grant purchases on the client** if the existing project uses server-side validation.
- **Do not assume all products are consumables.** Detect and classify each product.
- **Do not assume subscriptions are simple.** Detect promotional offers, offer codes, and introductory price usage. If found, surface as migration blockers and let the user choose a path.
- **If the project contains RevenueCat, Adapty, or Essential Kit**, stop and notify the user — this skill does not support converting those packages.
- **If StoreKit-specific features are not cleanly expressible in Unity IAP**, report them as migration blockers rather than silently removing them.
- **A mixed Unity IAP + native StoreKit architecture is not recommended.** Both systems register as `SKPaymentQueue` observers — only one reliably receives transaction callbacks. Do not offer a mixed approach; if the user asks, explain the risk and redirect to Option A or B (see Subscription Blocker Options).

---

## Step 1 — Pre-Conversion Scan

Scan before making any changes. Do not edit files until the migration report is accepted.

### 1a — Native iOS Plugin Files

Search `Assets/Plugins/iOS/` for:
- `*.m`, `*.mm` files — Objective-C plugins
- `*.h` files — headers with StoreKit imports or `extern "C"` bridge declarations
- `*.swift` files — Swift plugins (rare but increasingly common)

List all files found. For each, note whether it imports `StoreKit` or `StoreKit2`.

### 1b — C# Bridge Patterns

Search `Assets/**/*.cs` for:

```
DllImport.*__Internal
UnitySendMessage
SKProduct|SKPayment|SKPaymentTransaction|SKPaymentQueue|SKReceiptRefresh
StoreKit|AppStore|AppleIAP|iOSBilling|iOSIAP|iOSPurchase
Application\.platform.*IPhonePlayer|RuntimePlatform\.IPhonePlayer
```

Also search for callback method names referenced in `UnitySendMessage` calls in the native files — these will be C# methods receiving callbacks.

### 1c — StoreKit 1 Patterns (in .m / .mm / .h files)

```
SKProductsRequest|SKProductsRequestDelegate
SKPaymentQueue|SKPaymentTransactionObserver
SKPaymentTransaction|SKPayment|SKMutablePayment
SKProduct|SKProductSubscriptionPeriod|SKProductDiscount
paymentQueue:updatedTransactions:|finishTransaction:
SKReceiptRefreshRequest
SKPaymentDiscount|paymentDiscount
SKStorefront|paymentQueueDidChangeStorefront
canMakePayments
```

### 1d — StoreKit 2 Patterns (in .swift files)

```
import StoreKit
Product\.products\(for:\)|product\.purchase\(\)
Transaction\.currentEntitlements|Transaction\.updates|Transaction\.finish
verificationResult|JWSTransaction
winBackOffer|eligibleWinBackOffers
```

### 1e — Scene / Prefab / Asset Scan

Search `.unity`, `.prefab`, `.asset` files for references to billing-related MonoBehaviour names found in 1b. List all scene references so they can be rewired after migration.

---

## Step 2 — StoreKit Version Detection

Classify the native plugin as **StoreKit 1**, **StoreKit 2**, or **Mixed** based on Step 1 findings.

| Generation | Signals | Notes |
|---|---|---|
| **StoreKit 1** | `SKPaymentQueue`, `SKPaymentTransaction`, `SKProductsRequest` | Most Unity projects pre-2023 |
| **StoreKit 2** | `Product.products(for:)`, `Transaction.currentEntitlements`, `Transaction.updates` | Swift 5.5+, iOS 15+ |
| **Mixed** | Both sets of signals present | Treat as StoreKit 1 for blocker purposes; flag SK2 usage for review |

Document the detected generation in the migration report. StoreKit 2 has additional blockers (see Step 5).

---

## Step 3 — Product Classification

Classify every detected product into one of: **Consumable**, **NonConsumable**, **Subscription**, **Unknown**.

Infer from (in priority order):

1. `SKProduct.subscriptionPeriod` non-nil → **Subscription**
2. `finishTransaction:` called after grant without a "restore" path → **Consumable**
3. `restoreCompletedTransactions` handling or checking `originalTransaction` → **NonConsumable** or **Subscription**
4. Product ID naming conventions (`coins`, `gems`, `pack` → Consumable; `remove_ads`, `unlock`, `premium` → NonConsumable; `monthly`, `annual`, `sub` → Subscription)
5. Entitlement / backend grant code patterns
6. Comments or config files

If type cannot be confidently inferred, mark as **Unknown** and add to the blocker question list.

---

## Step 4 — Migration Report (produce before any edits)

Generate all sections before touching any file. Do not skip any.

1. **Current native billing architecture** — describe the bridge pattern (C# `[DllImport("__Internal")]` → ObjC/Swift plugin → StoreKit)
2. **StoreKit generation** — SK1, SK2, or Mixed
3. **Files using native StoreKit** — full list with role of each file (plugin, bridge, callback receiver, UI)
4. **Product catalog discovered** — all product IDs found
5. **Product type mapping** — inferred type + confidence + source of inference for each product
6. **Purchase flow mapping** — native → Unity IAP step-by-step
7. **Restore purchase flow mapping** — `restoreCompletedTransactions` / `Transaction.currentEntitlements` → `store.FetchPurchases()` + `store.RestoreTransactions(callback)`
8. **Backend validation mapping** — what data the old flow sent vs what Unity IAP provides (see Behavior Change Rules — receipt format is a breaking change)
9. **Transaction finish behavior mapping** — `finishTransaction:` → `store.ConfirmPurchase(pendingOrder)` (only after grant)
10. **Subscription behavior mapping** — detected subscription products, restore path, renewal handling, introductory price usage
11. **StoreKit-specific features detected** — list each (see Blocker Detection)
12. **Migration blockers** — features that cannot be cleanly mapped
13. **Proposed new Unity IAP architecture** — new files and their roles
14. **Code files to create** — with paths
15. **Code files to modify** — with nature of each change
16. **Manual App Store Connect checks** — product ID match, subscription group configuration
17. **Test plan** — from the checklist in this document
18. **Rollback plan** — from the rollback plan section in this document

---

## Step 5 — Blocker Detection

Report these as migration blockers if detected. Do not silently remove them.

| Feature | Detection Signal | Blocker Level |
|---|---|---|
| Promotional offer signing (SK1 `SKPaymentDiscount`) | `SKPaymentDiscount`, `paymentDiscount`, `offerIdentifier`, `keyIdentifier`, `nonce`, `signature` | **Hard** — requires server-side signed offers; Unity IAP has no equivalent |
| SK2 promotional offer signing | `promotionalOffer`, `PromotionalOffer`, `eligiblePromotionOfferSignature` | **Hard** — not exposed in Unity IAP |
| Win-back offers (SK2) | `winBackOffer`, `eligibleWinBackOffers` | **Hard** — not supported in Unity IAP 5.4 |
| `SKReceiptRefreshRequest` (manual receipt refresh) | `SKReceiptRefreshRequest` | **Soft** — `store.FetchPurchases()` covers the re-delivery use case; document loss of explicit refresh |
| `SKStorefront` / storefront change observer | `SKStorefront`, `paymentQueueDidChangeStorefront` | **Soft** — not exposed in Unity IAP; document loss |
| Multi-quantity purchases | `quantity > 1` in `SKMutablePayment` | **Hard** — Unity IAP does not support quantity > 1 per transaction |
| Custom `SKPaymentQueue` observers remaining alongside Unity IAP | Other `addTransactionObserver` calls not being removed | **Hard** — both observers compete; only one reliably receives callbacks |
| `SKOverlay` / `SKStoreProductViewController` | `SKOverlay`, `SKStoreProductViewController` | **Soft** — not IAP-related; these are store UI overlays. Document as out of scope for this migration |
| Direct App Store receipt bundle validation (`appStoreReceiptURL`) | `appStoreReceiptURL`, `receiptData`, `/verifyReceipt` in backend URLs | **Hard blocker for backend** — Unity IAP uses per-transaction JWS, not the receipt bundle. Backend must migrate from `/verifyReceipt` to JWS validation |

### Subscription Blocker Options (present to user when hard subscription blockers found)

**Option A — Remove the blocking feature and simplify App Store Connect setup for Unity IAP compatibility**
Remove promotional offer signing or simplify subscription configuration so Unity IAP can handle all products without native StoreKit. Recommended — single system, no coexistence risk.

**Option B — Stay on native StoreKit for all products**
Do not migrate at this time. Keep the native StoreKit plugin as-is until the App Store Connect configuration can be simplified or Unity IAP exposes the required features. Stop the conversion and document the blocker and this decision in `IapMigrationNotes.md`.

---

## Step 6 — Target Architecture

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
| `LegacyStoreKitAdapterDeprecated.cs` | Compatibility facade (only if game code calls old StoreKit bridge API directly) |
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

---

## Step 7 — Migration Phases

### Phase 1 — Package Check

- Verify `com.unity.purchasing` exists in `Packages/manifest.json`.
- If absent or outdated, install the latest stable version via **Window > Package Manager > Unity Registry > In App Purchasing**. Do not auto-edit `manifest.json` unless the user explicitly allows it.
- Confirm installation before proceeding.

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
5. **Deferred purchases (Ask-to-Buy)** — `OnPurchaseDeferred`: show pending UI, do not grant. When approved, `OnPurchasePending` fires.
6. **Promotional purchase interception** — if the existing plugin intercepts App Store promotional purchases, wire up:
   ```csharp
   if (store.AppleStoreExtendedPurchaseService != null)
       store.AppleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted += OnPromotionalPurchase;
   ```
   Call `store.AppleStoreExtendedPurchaseService?.ContinuePromotionalPurchases()` when ready to proceed.
7. **Failed purchases** — `OnPurchaseFailed`: map `FailureReason` to user-facing error.
8. **Restore** — `store.FetchPurchases()` re-delivers unconfirmed purchases via `OnPurchasePending`. Add `store.RestoreTransactions(callback)` for the explicit "Restore Purchases" button (required on iOS).
9. **Validation handoff** — send `order.Info.Apple?.jwsRepresentation` to backend in place of the SK1 receipt bundle. Document the backend endpoint change required (see Behavior Change Rules).

### Phase 4 — Compatibility Facade

If game code calls the old native bridge directly (e.g., `NativeStoreKit.Purchase(id)`):

- Create `LegacyStoreKitAdapterDeprecated.cs` that implements the same method signatures.
- Delegate all calls to `UnityIapStorePurchaseService`.
- Mark all methods `[Obsolete("Use IStorePurchaseService — remove after migration validation")]`.

### Phase 5 — Remove Native Plugin from Active Path

- Stop calling `[DllImport("__Internal")]` bridge methods for migrated products.
- Wrap all native bridge calls in C# with `#if !USE_UNITY_IAP_V5` — do not delete them.
- Do not delete ObjC/Swift plugin files — leave them in `Assets/Plugins/iOS/`. The `#if` guards on the C# call sites are sufficient to prevent the old bridge from being invoked.
- If the existing plugin registers its own `SKPaymentQueue` observer (common), add a note in `IapMigrationNotes.md` that the plugin's observer registration must be removed or disabled before shipping — both observers competing for the same queue is a hard conflict.

### Phase 6 — Test Scaffolding

- Add debug logs at each IAP event.
- Add editor stubs for sandbox testing.
- Add the QA checklist from the test plan below to `IapMigrationNotes.md`.

---

## Step 8 — Behavior Change Rules

### Receipt Format (backend-breaking change)

The receipt format is fundamentally different between native StoreKit and Unity IAP. Flag this as a **required backend change** if server-side validation is in use.

| Native approach | Unity IAP equivalent |
|---|---|
| SK1: `[NSData dataWithContentsOfURL:[[NSBundle mainBundle] appStoreReceiptURL]]` base64-encoded, validated against Apple's `/verifyReceipt` endpoint | `order.Info.Apple?.jwsRepresentation` — a per-transaction JWS string, validated against Apple's App Store Server API |
| SK2: Per-transaction JWS string from `verificationResult` | Same — `order.Info.Apple?.jwsRepresentation` |

Unity IAP uses StoreKit 2 under the hood (on iOS 15+). The resulting receipt is a JWS per transaction, not the SK1 app receipt bundle. If the backend currently calls `/verifyReceipt` with a base64 receipt, it must migrate to Apple's App Store Server API (`GET /inApps/v1/transactions/{transactionId}`) or use the JWS directly.

### Transaction Finishing (Acknowledgement / Consumption)

| Native StoreKit | Unity IAP |
|---|---|
| `SKPaymentQueue.default().finishTransaction(transaction)` — called after grant for all types | `store.ConfirmPurchase(pendingOrder)` — called after grant for all types |

**Never call `ConfirmPurchase` before the entitlement is safely granted and saved.**

### Restore Transactions

| Native StoreKit | Unity IAP |
|---|---|
| `SKPaymentQueue.default().restoreCompletedTransactions()` | `store.RestoreTransactions(callback)` — for the explicit "Restore Purchases" button |
| Checking for existing transactions at startup | `store.FetchPurchases()` — re-delivers any unconfirmed purchases via `OnPurchasePending` |

`store.FetchPurchases()` should be called at startup. `RestoreTransactions` is only needed for the user-triggered restore button (required on iOS for Apple App Store compliance).

### App Account Token (replacing `applicationUsername`)

If the native plugin set `SKMutablePayment.applicationUsername` to associate purchases with a backend user:

```csharp
// Set after Connect() — accepts a Guid, not a string hash
store.AppleStoreExtendedService?.SetAppAccountToken(Guid.Parse(userAccountGuid));
```

Must be called **after** `await store.Connect()`. The token is attached to the transaction and surfaced in the JWS payload.

### Ask-to-Buy (Deferred Purchases)

| Native StoreKit | Unity IAP |
|---|---|
| `SKPaymentTransactionStatePurchasing` + parent approval pending → `paymentQueue:removedTransactions:` | `store.OnPurchaseDeferred` — show pending UI, do not grant |
| Purchase approved → `paymentQueue:updatedTransactions:` with `SKPaymentTransactionStatePurchased` | `store.OnPurchasePending` — grant content, then confirm |

If the existing plugin simulates Ask-to-Buy via `store.AppleStoreExtendedPurchaseService?.simulateAskToBuy`, that property is still available in Unity IAP 5.

### Code Redemption Sheet

If the native plugin calls `SKPaymentQueue.default().presentCodeRedemptionSheet()`:

```csharp
store.AppleStoreExtendedPurchaseService?.PresentCodeRedemptionSheet();
```

Available after `Connect()`. Null-check required — only non-null on iOS.

### Promotional Purchases

If the native plugin intercepts App Store promotional purchases (products promoted on the App Store product page):

```csharp
// Subscribe after Connect() — if() required (?.+= does not work with events)
if (store.AppleStoreExtendedPurchaseService != null)
    store.AppleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted += OnPromotionalPurchase;

void OnPromotionalPurchase(Product product)
{
    // Show confirmation UI, then:
    store.AppleStoreExtendedPurchaseService?.ContinuePromotionalPurchases();
}
```

---

## Step 9 — Native → Unity IAP Concept Mapping

### StoreKit 1

| Native StoreKit 1 | Unity IAP 5.x |
|---|---|
| `SKProduct` | `Product` (via `store.GetProductById`) |
| `SKPaymentTransaction` | `PendingOrder` (on `OnPurchasePending`) |
| `SKPaymentTransaction.transactionIdentifier` | `pendingOrder.Info.TransactionID` |
| `SKPaymentTransaction.payment.productIdentifier` | `pendingOrder.CartOrdered.Items().First().Product.definition.id` |
| App receipt (`appStoreReceiptURL` base64) | `order.Info.Apple?.jwsRepresentation` (per-transaction JWS — see receipt format note) |
| `SKPaymentQueue.default().finishTransaction:` | `store.ConfirmPurchase(pendingOrder)` |
| `SKPaymentQueue.default().restoreCompletedTransactions()` | `store.RestoreTransactions(callback)` |
| `SKPaymentTransactionStatePurchased` | `store.OnPurchasePending` |
| `SKPaymentTransactionStateFailed` | `store.OnPurchaseFailed` |
| `SKPaymentTransactionStateDeferred` | `store.OnPurchaseDeferred` |
| `SKPaymentTransactionStateRestored` | `store.OnPurchasesFetched` (via `FetchPurchases`) or `store.OnPurchasePending` (via `RestoreTransactions`) |
| `SKProductsRequest` + `productsRequest:didReceiveResponse:` | `store.FetchProducts(definitions)` → `store.OnProductsFetched` |
| `SKPaymentQueue.canMakePayments()` | `store.AppleStoreExtendedService?.canMakePayments` (after `Connect()`) |
| `SKMutablePayment.applicationUsername` | `store.AppleStoreExtendedService?.SetAppAccountToken(Guid)` (after `Connect()`) |
| `SKPaymentDiscount` (promotional offer) | **Not supported** — report as hard blocker |
| `SKReceiptRefreshRequest` | No direct equivalent — `store.FetchPurchases()` covers re-delivery |
| `SKStorefront` | Not exposed in Unity IAP |
| `product.subscriptionPeriod` | `store.AppleStoreExtendedProductService?.GetProductDetails()` (returns raw JSON) |
| `product.introductoryPrice` | `store.AppleStoreExtendedProductService?.GetIntroductoryPriceDictionary()` |

### StoreKit 2

| Native StoreKit 2 | Unity IAP 5.x |
|---|---|
| `Product.products(for:)` | `store.FetchProducts(definitions)` → `store.OnProductsFetched` |
| `product.purchase()` | `store.PurchaseProduct(product)` |
| `Transaction.currentEntitlements` (async stream) | `store.FetchPurchases()` → `store.OnPurchasesFetched` |
| `Transaction.updates` (async stream) | `store.OnPurchasePending` (event-driven) |
| `transaction.finish()` | `store.ConfirmPurchase(pendingOrder)` |
| JWS transaction string from `verificationResult` | `order.Info.Apple?.jwsRepresentation` |
| `Transaction.currentEntitlements` for entitlement check | `store.CheckEntitlement(product)` → `store.OnCheckEntitlement` |
| `winBackOffer` / `eligibleWinBackOffers` | **Not supported** — report as hard blocker |

---

## Step 10 — Rollback Plan

- Wrap all new Unity IAP C# code in `#if USE_UNITY_IAP_V5`.
- Wrap all replaced native bridge calls (`[DllImport("__Internal")]` invocations) in `#if !USE_UNITY_IAP_V5`.
- ObjC/Swift plugin files cannot use C# defines — leave them in place untouched. The `#if` guards on the C# call sites prevent the bridge from being invoked.
- To revert: remove `USE_UNITY_IAP_V5` from **Edit > Project Settings > Player > Scripting Define Symbols**. The native StoreKit bridge is restored without any code changes.
- Never activate both purchase systems simultaneously — both register as `SKPaymentQueue` observers and will compete.
- Document the rollback steps in `IapMigrationNotes.md`.

---

## Step 11 — Test Checklist

Include this in `IapMigrationNotes.md` after migration:

- [ ] Fresh install — products load and display prices
- [ ] Upgrade from old app version to migrated version — no duplicate entitlements
- [ ] Consumable purchase — content granted
- [ ] Repeat consumable purchase — repeatable, no block
- [ ] Non-consumable purchase — content granted once
- [ ] Non-consumable restore via "Restore Purchases" button — content restored on reinstall
- [ ] Subscription purchase — subscription active
- [ ] Subscription restore — status refreshed on launch
- [ ] Ask-to-Buy (deferred) — pending UI shown, no premature grant; purchase completes when parent approves
- [ ] Promotional purchase intercept (if applicable) — `OnPromotionalPurchaseIntercepted` fires, `ContinuePromotionalPurchases()` proceeds correctly
- [ ] Code redemption sheet (if applicable) — `PresentCodeRedemptionSheet()` opens
- [ ] Interrupted purchase flow — re-delivered on next launch via `OnPurchasePending`
- [ ] App killed during purchase — pending order re-delivered on next launch
- [ ] Backend validation success — entitlement granted (JWS payload accepted by backend)
- [ ] Backend validation failure — entitlement NOT granted, purchase not confirmed
- [ ] Network failure during validation — purchase left pending, re-delivered on next launch
- [ ] Sandbox tester account — no real charges, sandbox transactions process correctly
- [ ] TestFlight build — products load and sandbox purchases succeed
- [ ] App Store Connect product ID match — all IDs match exactly
- [ ] App Store Connect subscription group — base plan configuration compatible with Unity IAP
- [ ] No duplicate entitlement grant
- [ ] No unfinished transactions remaining (all purchases confirmed)
- [ ] No double grant of consumables

---

## Step 12 — Questions to Ask (only when required)

Ask only when the information cannot be inferred:

1. Which product IDs are consumable, non-consumable, or subscription?
2. Does the backend currently validate using the SK1 receipt bundle (base64 + `/verifyReceipt`)? If so, a backend migration to JWS validation is required.
3. Does the plugin intercept App Store promotional purchases (`paymentQueue:shouldAddStorePayment:forProduct:`)?
4. Are any subscriptions using promotional offer signing (`SKPaymentDiscount`)? If so, this is a hard blocker.
5. Should the skill preserve the old public C# billing API as a compatibility facade?
6. Should native ObjC/Swift plugin files be kept, disabled, or removed after migration validation?

If enough information can be inferred, proceed with a best-effort plan and mark uncertain items as **TODO**.
