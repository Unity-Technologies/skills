# Add Unity IAP 5 to a Project With No IAP

## Table of Contents

- [Step 1 — Package Installation](#step-1--package-installation)
- [Step 2 — Project Scan](#step-2--project-scan)
- [Step 3 — Product Discovery](#step-3--product-discovery)
- [Step 4 — IAPManager Architecture](#step-4--iapmanager-architecture)
- [Step 5 — Purchase Handling Contract](#step-5--purchase-handling-contract)
- [Step 6 — Product Type Behavior Rules](#step-6--product-type-behavior-rules)
- [Step 7 — Cloud Save Integration](#step-7--cloud-save-integration)
- [Step 8 — UI Integration](#step-8--ui-integration)
- [Step 9 — Verification Report](#step-9--verification-report)

Use this reference when the project has no existing in-app purchase implementation and needs Unity IAP 5 added from scratch.

## Step 1 — Package Installation

Check `Packages/manifest.json` for `com.unity.purchasing`:

- **If absent:** Install via **Window > Package Manager > Unity Registry > In App Purchasing**. Do not edit `manifest.json` directly unless the user explicitly allows it. Confirm installation before proceeding.
- **If present but below v5.0:** Instruct the user to upgrade via Package Manager to the latest stable v5 release. Do not proceed until the upgrade is confirmed.
- **If v5.0+ is already present:** Note the exact version and proceed.

Always use the latest stable v5 release unless the user specifies a version. Never downgrade an existing package.

## Step 2 — Project Scan

### 2a — Code scan

Search `Assets/**/*.cs` for any existing IAP signals:

```
ProductType|ProductDefinition|StoreController|IAPButton|CodelessIAP
ProcessPurchase|PendingOrder|DeferredOrder|ConfirmPurchase|FetchPurchases
IStoreListener|UnityPurchasing\.Initialize|ConfigurationBuilder
```

### 2b — Inventory and economy scan

Search `Assets/**/*.cs` for terms that indicate what needs to be credited after purchase:

```
\bcoins\b|\bgems\b|\blives\b|\binventory\b|\bcurrency\b
PlayerData|SaveAsync|CloudSave|SaveDataAsync|SaveGame
Economy
```

### 2c — Shop UI scan

Search `Assets/**/*.unity`, `*.prefab`, `*.asset` for shop-related GameObjects and scripts:
```
Shop|Store|Purchase|Buy|IAP|Product|Monetiz
```

Collect: scene names, prefab paths, button component names, and any serialized product ID strings.

## Step 3 — Product Discovery

### If product definitions are already found (from Step 2a/2b/2c)

Use them. Confirm types with the user if ambiguous.

### If no product definitions exist

**Stop and ask:**

> "Please provide the first IAP product ID and type — for example `com.mygame.coins100` as Consumable — and tell me which inventory field, currency, or item should be credited after purchase."

- If the user provides only a product ID with no type, **default to Consumable** but state the assumption explicitly before proceeding.
- Collect all products before writing any code. For each product record: `productId`, `ProductType`, reward target (field name / method name / amount).

## Step 4 — IAPManager Architecture

### Match the project's existing patterns

Before generating code:
- Check whether the project uses MonoBehaviour singletons, ScriptableObject services, or dependency injection.
- Check the namespace convention used in `Assets/Scripts/`.
- Prefer the pattern already in use rather than introducing a new one.

### IAPManager responsibilities

Create a single `IAPManager` (MonoBehaviour singleton or service, matching project pattern) that:

- Holds the `StoreController` instance.
- Exposes `Buy(string productId)`.
- Exposes `RestorePurchases()` — **only** if the product list contains `NonConsumable` or `Subscription` types.
- Fires UI-friendly events or callbacks for: `OnInitialized`, `OnProductsLoaded`, `OnPurchaseSuccess`, `OnPurchaseFailed`, `OnPurchaseDeferred`.
- Initializes once in `Awake()` — see the **Initialization Flow** section in SKILL.md for the exact event subscription and `Connect()` ordering rules.
- Registers all products from a single authoritative product list (not scattered across UI handlers).

### Product catalog

Define products in one place — a `ScriptableObject`, a plain `List<ProductDefinition>`, or a constants class — not inside button click handlers. Wire button click handlers to `IAPManager.Buy(productId)` using the catalog, not hardcoded strings.

## Step 5 — Purchase Handling Contract

For API mechanics (two-step flow, event names, `ConfirmPurchase` signature) see the **Two-Step Purchase Flow** and **Required Event Subscriptions** sections in SKILL.md. This section covers only the grant-and-save contract that is specific to this path.

### PendingOrder — the save-before-confirm rule

```
OnPurchasePending fires
  → grant reward to inventory / currency / entitlement
  → save player data (see Cloud Save Integration below)
  → ONLY IF save succeeds: call ConfirmPurchase(pendingOrder)
  → IF save fails: do NOT confirm — store will re-deliver on next launch
```

Never call `ConfirmPurchase` before the save completes. An unconfirmed purchase is safe — it re-delivers. A confirmed purchase that was never saved is a lost reward.

### Duplicate grant prevention

`OnPurchasePending` may fire more than once for the same purchase (app restart before confirmation). Track processed order IDs (e.g., in Cloud Save or a local ledger) and skip grant if the order ID was already processed.

### DeferredOrder

Do not grant anything. Fire `OnPurchaseDeferred` event to update UI ("Purchase pending approval"). Wait for `OnPurchasePending` when the purchase is approved.

### FailedOrder

Do not grant anything. Fire `OnPurchaseFailed` with the reason. See **Failure Description Property Names** in SKILL.md for the correct property names per type.

## Step 6 — Product Type Behavior Rules

### Consumable

- Credit inventory or currency after purchase.
- Persist the credited state in save data before confirming.
- **Do not restore** old consumable orders — confirmed consumables are not returned by `FetchPurchases` and must not be re-granted.
- Track consumable grants yourself (order ID ledger in Cloud Save or local save).

### NonConsumable

- Unlock a durable entitlement (feature flag, item ownership).
- Include `RestorePurchases()` — required on iOS, good practice on Android.
- Re-apply the entitlement on app startup: call `store.FetchPurchases()` and re-check ownership in `OnPurchasesFetched`, or use `store.CheckEntitlement(product)`. See **Entitlement Checking** and **Fetch Existing Purchases** in SKILL.md.

### Subscription

- Restore or check subscription state on app startup, not only at purchase time. Subscriptions can expire or be cancelled externally.
- Use `store.CheckEntitlement(product)` or inspect `OnPurchasesFetched` results to update active/expired/unknown state.
- See **Subscription Info** in SKILL.md for the correct access path (`order.Info.PurchasedProductInfo`, `IsSubscribed() == Result.True`).

## Step 7 — Cloud Save Integration

### Detect the existing save system first

Search for any of these patterns before writing save code:

```
\.SaveAsync\(\)|CloudSaveService\.Instance
SaveGame\(|SaveDataAsync\(|PlayerPrefs\.SetInt|JsonUtility\.ToJson|JsonConvert\.Serialize
```

### Rules

- **Use the existing save abstraction** — do not create a new save system.
- Prefer methods already in use: `CloudSaveService.Instance.Data.Player.SaveAsync()`, `SaveGame()`, `SaveDataAsync()`, or equivalent.
- If no save system exists, use `PlayerPrefs` as a minimal fallback and document it as a TODO for the developer to upgrade.
- Save **must complete before `ConfirmPurchase`** is called (see Step 5).

## Step 8 — UI Integration

### Wiring

- Wire existing shop buttons to `IAPManager.Buy(productId)` — do not embed product IDs in button click handlers directly.
- Subscribe to `IAPManager` events in the shop UI script to drive state changes.

### States to handle in UI

| State | Trigger | UI action |
|---|---|---|
| Initializing | Before `OnInitialized` | Disable buy buttons or show spinner |
| Products loaded | `OnProductsLoaded` | Display localized price from `product.metadata.localizedPriceString` |
| Product unavailable | Product missing from `OnProductsFetched` result | Hide or grey out the button |
| Purchase pending | `PurchaseProduct()` called | Disable button, show loading state |
| Purchase deferred | `OnPurchaseDeferred` | Show "pending approval" message |
| Purchase success | `OnPurchaseSuccess` | Show confirmation, update inventory display |
| Purchase failed | `OnPurchaseFailed` | Show error message, re-enable button |

### Restore button

Add a "Restore Purchases" button **only** if the product list contains `NonConsumable` or `Subscription` products. Required on iOS. Wire to `IAPManager.RestorePurchases()`.

## Step 9 — Verification Report

After applying changes, produce a report with these sections:

1. **Files changed** — list with nature of each change
2. **Product IDs and types** — final catalog
3. **Reward mapping** — product ID → field/method credited
4. **Save behavior** — which save method is called, when
5. **Restore behavior** — which products are restorable, how
6. **Pending / deferred handling** — confirmation of save-before-confirm and deferred UI
7. **Duplicate grant prevention** — how order IDs are tracked
8. **Manual steps still required** — Unity Editor steps (Receipt Validation Obfuscator if using local Google Play validation), App Store Connect / Play Console product setup, sandbox testing accounts
