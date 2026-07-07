# Convert Essential Kit Billing to Unity IAP 5

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Important Constraints](#important-constraints)
- [Step 1 — Verify Essential Kit Billing Is Active](#step-1--verify-essential-kit-billing-is-active)
- [Step 2 — Extract Product Definitions](#step-2--extract-product-definitions)
- [Step 3 — Migration Report (produce before any edits)](#step-3--migration-report-produce-before-any-edits)
- [Step 4 — Add Unity IAP 5](#step-4--add-unity-iap-5)
- [Step 5 — Disable Essential Kit Billing](#step-5--disable-essential-kit-billing)
- [Step 6 — Remove Conflicting Gradle Dependency](#step-6--remove-conflicting-gradle-dependency)
- [Step 7 — Essential Kit → Unity IAP Concept Mapping](#step-7--essential-kit--unity-iap-concept-mapping)
- [Step 8 — Verification Report](#step-8--verification-report)

Use this reference when the project uses Essential Kit (VoxelBusters) Billing Services and needs to be converted to `com.unity.purchasing` (Unity IAP 5). Always install the latest stable version of Unity IAP.

---

## Trigger Phrases

- "Convert Essential Kit billing function to Unity IAP"
- "Replace VoxelBusters Essential Kit IAP with Unity IAP"
- "Migrate from Essential Kit in-app purchases to Unity IAP"
- "Remove Essential Kit billing and use Unity IAP"

---

## Important Constraints

- **Do not delete any Essential Kit source files.** Disabling the Billing service toggle in settings is sufficient to prevent Essential Kit from initializing native billing at runtime. No C# source changes are needed.
- **Leave all other Essential Kit services completely untouched.** Only the Billing service and its Gradle dependency are affected.
- **Do not change product IDs.** The same IDs used in Essential Kit must be used in Unity IAP to preserve store history and existing purchases.
- **Do not change backend validation endpoints** unless the receipt format change makes it unavoidable — document any required backend change.
- **This path covers store billing (Apple App Store / Google Play) only.** Do not consider IAP D2C Capabilities / third-party payment provider migration here.

---

## Step 1 — Verify Essential Kit Billing Is Active

### 1a — Confirm Essential Kit is installed

Search `Packages/manifest.json` for `com.voxelbusters.essentialkit` and check that `Assets/Plugins/VoxelBusters/` exists.

### 1b — Check Billing service is enabled

Read `Resources/EssentialKitSettings.asset` (Unity YAML serialized file). Search for the Billing Services enabled flag:

```
billingServicesEnabled|isBillingServicesEnabled|BillingServices.*enabled: 1
```

Also inspect the file for any `BillingServicesSettings` section.

If the Billing service flag is **not set to enabled (1)**, stop and report:

> "Essential Kit Billing Services is not enabled in Project Settings (Window > Voxel Busters > Essential Kit > Open Settings → Services). There is nothing to convert."

Do not proceed.

### 1c — Check Unity IAP package status

Search `Packages/manifest.json` for `com.unity.purchasing`. If absent or outdated, install the latest stable version via Package Manager before continuing.

---

## Step 2 — Extract Product Definitions

Read `Resources/EssentialKitSettings.asset` and locate the products array under `BillingServicesSettings`. Each product entry contains:

| Essential Kit Field | Description |
|---|---|
| `Id` | Internal product identifier used in code (e.g., `coins_100`) |
| `PlatformId` | Common store product ID across platforms |
| `PlatformIdOverrides` | Per-platform IDs — separate entries for iOS (`AppleAppStore`) and Android (`GooglePlay`) |
| `ProductType` | `Consumable`, `NonConsumable`, or `Subscription` |
| `Title` | Display name |
| `Description` | User-facing description |

If **no products are defined** in the settings asset, stop and report:

> "No billing products found in Essential Kit Billing Settings. There is nothing to convert."

Do not proceed.

### Product type mapping

| Essential Kit `BillingProductType` | Unity IAP `ProductType` |
|---|---|
| `Consumable` | `ProductType.Consumable` |
| `NonConsumable` | `ProductType.NonConsumable` |
| `Subscription` | `ProductType.Subscription` |

### Store-specific IDs

If `PlatformIdOverrides` has separate Apple and Google IDs, use `StoreSpecificIds` in the `ProductDefinition`:

```csharp
new ProductDefinition("coins_100", ProductType.Consumable,
    new StoreSpecificIds
    {
        { AppleAppStore.Name, "apple_coins_100" },
        { GooglePlay.Name, "google_coins_100" }
    })
```

If only a single `PlatformId` is used, pass it directly as the product ID.

---

## Step 3 — Migration Report (produce before any edits)

Generate all sections before touching any file.

1. **Essential Kit billing architecture** — event-based (`OnTransactionStateChange`), auto-finish vs manual-finish mode (`AutoFinishTransactions` setting)
2. **Files using Essential Kit Billing** — C# files referencing `BillingServices`, scene/prefab shop UI
3. **Product catalog extracted** — all products with ID, type, and store-specific IDs
4. **Auto Finish Transactions setting** — if disabled, the project uses server-side verification; document the receipt field mapping change (see Step 7)
5. **Backend validation mapping** — how receipt data sent to backend changes
6. **Restore flow mapping** — `RestorePurchases()` → `store.RestoreTransactions()` + `store.FetchPurchases()`
7. **UI wiring** — shop buttons and callbacks that will need rewiring to Unity IAP events
8. **Proposed Unity IAP architecture** — new files, product catalog definition
9. **Files to create / modify**
10. **Manual steps required** — listed in Step 8

---

## Step 4 — Add Unity IAP 5

Use the product catalog extracted in Step 2 as input and follow **`path-add-iap-to-new-project.md`** for the full Unity IAP 5 implementation. Specifically:

- Match the project's existing patterns for the IAPManager (Step 4 of that file).
- Apply the save-before-confirm contract (Step 5 of that file).
- Apply product type behavior rules (Step 6 of that file) — note that consumable restore rules are the same: do not restore old consumable orders.
- Use the existing save system found in the project (Step 7 of that file).
- Rewire existing shop UI buttons to `IAPManager.Buy(productId)` (Step 8 of that file).

Do not duplicate content from `path-add-iap-to-new-project.md` — reference it.

---

## Step 5 — Disable Essential Kit Billing

After Unity IAP is in place, disable the Essential Kit Billing service to prevent it from initializing its own native billing layer (which would conflict with Unity IAP at runtime).

**Edit `Resources/EssentialKitSettings.asset`** — set the Billing Services enabled flag to `0`:

```yaml
# Before
billingServicesEnabled: 1

# After
billingServicesEnabled: 0
```

Do not modify any other field in this file. Do not delete the file. Do not touch any other section of the settings asset.

This single change prevents Essential Kit from calling `BillingClient.startConnection()` (Android) or initializing StoreKit (iOS) at startup. All Essential Kit C# billing source files remain in place and will compile normally — the service simply will not be initialized.

---

## Step 6 — Remove Conflicting Gradle Dependency

Search `Assets/Plugins/VoxelBusters/EssentialKit/Essentials/Editor/CrossPlatformEssentialKitDependencies.xml` for `com.android.billingclient`. If the file does not exist or contains no `com.android.billingclient` entries, **skip this step entirely**.

If found, Essential Kit's billing Gradle dependency conflicts with Unity IAP's own BillingClient. Remove only the billing dependency line(s):

```xml
<!-- Remove these lines -->
<androidPackage spec="com.android.billingclient:billing:VERSION" />
<androidPackage spec="com.android.billingclient:billing-ktx:VERSION" />
```

Leave all other `<androidPackage>` entries in the file untouched.

After editing, run **Assets > External Dependency Manager > Android Resolver > Resolve** (or Force Resolve) to regenerate the Gradle dependency list.

---

## Step 7 — Essential Kit → Unity IAP Concept Mapping

### API mapping

| Essential Kit | Unity IAP 5 |
|---|---|
| `BillingServices.InitializeStore()` | `store.Connect()` → `store.FetchProducts(definitions)` |
| `BillingServices.OnInitializeStoreComplete` | `store.OnStoreConnected` + `store.OnProductsFetched` |
| `BillingServices.BuyProduct(product, options)` | `store.PurchaseProduct(product)` |
| `BillingServices.OnTransactionStateChange` (Purchased) | `store.OnPurchasePending` |
| `BillingServices.OnTransactionStateChange` (Failed) | `store.OnPurchaseFailed` |
| `BillingServices.OnTransactionStateChange` (Deferred) | `store.OnPurchaseDeferred` |
| `BillingServices.FinishTransactions(transactions)` | `store.ConfirmPurchase(pendingOrder)` |
| `BillingServices.RestorePurchases(forceRefresh)` | `store.RestoreTransactions(callback)` (user-triggered) + `store.FetchPurchases()` (startup) |
| `BillingServices.OnRestorePurchasesComplete` | `store.OnPurchasesFetched` + `store.RestoreTransactions` callback |
| `BillingServices.IsProductPurchased(id)` | `store.CheckEntitlement(product)` + `store.OnCheckEntitlement` |
| `BillingServices.CanMakePayments()` | `store.AppleStoreExtendedService?.canMakePayments` (Apple only) |
| `BillingServices.GetProductWithId(id)` | `store.GetProductById(id)` |
| `BillingServices.GetTransactions()` | `store.GetPurchases()` |
| `IBillingProduct` | `Product` |
| `IBillingTransaction` | `PendingOrder` (on `OnPurchasePending`) |
| `product.Price.LocalizedText` | `product.metadata.localizedPriceString` |
| `product.LocalizedTitle` | `product.metadata.localizedTitle` |
| `product.LocalizedDescription` | `product.metadata.localizedDescription` |

### Receipt / validation mapping

| Essential Kit | Unity IAP 5 |
|---|---|
| `transaction.Receipt` (iOS JWS token) | `pendingOrder.Info.Apple?.jwsRepresentation` |
| `transaction.RawData` JSON (`transaction` + `signature` fields, Android) | `pendingOrder.Info.Receipt` (contains the full Google receipt JSON) |

If the project sends `transaction.RawData` to a backend, document the receipt format change — the backend must now parse `order.Info.Receipt` instead.

### Auto Finish Transactions

Essential Kit's `AutoFinishTransactions` setting maps to Unity IAP's two-step flow:

| EK Setting | Unity IAP equivalent |
|---|---|
| `AutoFinishTransactions = true` | Call `ConfirmPurchase(pendingOrder)` immediately after granting content |
| `AutoFinishTransactions = false` (server verification) | Hold the `PendingOrder` reference, send receipt to server, call `ConfirmPurchase` only after server confirms — never confirm before grant |

---

## Step 8 — Verification Report

After applying changes, produce a report with these sections:

1. **Files changed** — list with nature of each change
2. **Product catalog** — all product IDs, types, and store-specific IDs
3. **Essential Kit Billing service** — confirmed disabled in `EssentialKitSettings.asset`
4. **Gradle dependency** — confirmed `com.android.billingclient:billing` removed from `CrossPlatformEssentialKitDependencies.xml`
5. **Receipt / backend mapping** — any backend changes required
6. **Manual steps still required:**
   - Run **Assets > External Dependency Manager > Android Resolver > Force Resolve** after editing the dependencies XML
   - Verify no duplicate `BillingClient` classes in the Android build (`./gradlew dependencies` or check Unity build log)
   - Confirm Essential Kit Billing service is visually disabled in **Window > Voxel Busters > Essential Kit > Open Settings → Services**
   - Verify all existing App Store Connect / Google Play Console product IDs match the Unity IAP catalog exactly
   - Test with sandbox/license test accounts
