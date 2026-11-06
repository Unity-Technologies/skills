---
name: implement-in-app-purchases
description: Implements, configures, and debugs Unity In-App Purchases, including subscriptions, receipt validation, and direct-to-customer payments via Stripe or Coda. Use when asked about real-money purchases or microtransactions, or migrating from native store billing, RevenueCat, Adapty, Essential Kit, or UniPay.
---

# Unity In-App Purchasing

Namespace: `UnityEngine.Purchasing` | Security: `UnityEngine.Purchasing.Security`
Package: `com.unity.purchasing`

**Unity IAP has its own initialization path** via `UnityIAPServices.StoreController()` → `store.Connect()`. It does not require `UnityServices.InitializeAsync()`, but they can coexist if your project uses other UGS services. If Analytics is present and `InitializeAsync()` is called, IAP will automatically send transaction events.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Before You Start

**Always read [references/pre-check.md](references/pre-check.md) first.** It scans the project for third-party IAP packages, native Google Billing, and existing Unity IAP versions, then routes to the correct path. Do not read any other reference file or make any changes until routing is resolved.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need.

In docs.unity3d.com URLs, replace `<VERSION>` with the major.minor of `com.unity.purchasing` in `Packages/packages-lock.json` before fetching. The IAP manual pages on docs.unity.com aren't versioned, so use those URLs as written.

- [Unity In-App Purchasing](https://docs.unity.com/en-us/iap.md): overview of the IAP manual and its sections.
- [Initialize Unity IAP](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing.md): install and initialize, with `StoreController` and D2C (`PaymentProvider.Name`, `RemoteCatalogProvider`) samples.
- [Create your catalog programmatically](https://docs.unity.com/en-us/iap/code-catalog.md): define products with `CatalogProvider` and store-specific IDs.
- [Purchases](https://docs.unity.com/en-us/iap/purchases.md): process pending purchases, confirm them, handle failed confirmations, and determine purchase status.
- [Restore purchases](https://docs.unity.com/en-us/iap/restore-purchases.md): restore behavior on Google Play and Apple platforms.
- [Receipt validation](https://docs.unity.com/en-us/iap/receipt-validation.md): local and server-side validation, and the receipt structure.
- [SubscriptionInfo class reference](https://docs.unity.com/en-us/iap/subscriptioninfo-class-reference.md): every `SubscriptionInfo` method and its per-store behavior.
- [Apple App Store](https://docs.unity.com/en-us/iap/apple-store.md): configure IAP for iOS, macOS and tvOS.
- [Use Google Play billing extensions](https://docs.unity.com/en-us/iap/use-google-play-billing-extensions.md): obfuscated IDs and deferred subscription changes.
- [Set up Codeless IAP](https://docs.unity.com/en-us/iap/codeless-iap.md): the IAP Catalog window, IAP buttons and automatic initialization.
- [Upgrade from IAP v4 to v5](https://docs.unity.com/en-us/iap/upgrade-to-iap-v5.md): the official v4 to v5 API mapping.
- [Integrate D2C payment providers workflow](https://docs.unity.com/en-us/iap/payment-providers/workflow.md): the D2C setup workflow for Stripe and Coda.
- [Initiate a purchase](https://docs.unity.com/en-us/iap/payment-providers/initiate-purchase.md): D2C eligibility checks and the Purchase Options UI.
- [Deep link success URL example](https://docs.unity.com/en-us/iap/payment-providers/success-deep-link.md): redirect players back to the app after D2C checkout.
- [Test mode for D2C payments](https://docs.unity.com/en-us/iap/payment-providers/test-mode.md): simulate D2C purchases before a provider account is connected.
- [Comply with Apple and Google external purchase requirements](https://docs.unity.com/en-us/iap/payment-providers/external-purchase-compliance.md): compliance checks, checkout URLs and transaction tokens.
- [Add a routing rule](https://docs.unity.com/en-us/iap/payment-providers/routing.md): choose which payment providers players see.
- [Create your Remote Catalog](https://docs.unity.com/en-us/iap/payment-providers/configure-remote-catalog.md): `.ucat` and `.catalog.csv` catalog items and deployment.

### API reference

The API is `UnityEngine.Purchasing`, part of the `com.unity.purchasing` package. It is versioned by package version, not Unity version, so check the project's installed package version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the installed package version.
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/xrefmap.yml`, replacing `<VERSION>` with the installed package's major.minor. This file maps every public API member's fully qualified name to its docs page `href`. Append the `href` to the same base URL, so `href: api/UnityEngine.Purchasing.StoreController.html` becomes `https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.StoreController.html`.
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions (e.g. the v4 `UnityPurchasing` class is obsolete in v5).

Most-used pages:

- [StoreController API](https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.StoreController.html): every `StoreController` method, event and property, with signatures.
- [EntitlementStatus API](https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.EntitlementStatus.html): the entitlement status values.

## Detailed References

- **Project scan and path routing (read first):** See [references/pre-check.md](references/pre-check.md)
- **Common v5 API mistakes and where to read each API topic:** See [references/api-notes.md](references/api-notes.md)
- **Platform extension gotchas (Apple, Google):** See [references/platform-notes.md](references/platform-notes.md)
- **Editing `IAPProductCatalog.json` (schema, decimal serialization, refresh):** See [references/codeless-catalog.md](references/codeless-catalog.md)
- **v4 → v5 migration:** See [references/migration-v4-to-v5.md](references/migration-v4-to-v5.md)
- **Convert native Google BillingClient to Unity IAP 5:** See [references/path-convert-native-google-billing.md](references/path-convert-native-google-billing.md)
- **Convert native iOS StoreKit plugin to Unity IAP 5:** See [references/path-convert-native-storekit.md](references/path-convert-native-storekit.md)
- **Convert Essential Kit billing to Unity IAP 5:** See [references/convert-essentialkit.md](references/convert-essentialkit.md)
- **UniPay (FLOBUK) — assessment and guidance:** See [references/convert-unipay.md](references/convert-unipay.md)
- **RevenueCat — conversion assessment and guidance:** See [references/convert-revenuecat.md](references/convert-revenuecat.md)
- **Adapty — conversion assessment and guidance:** See [references/convert-adapty.md](references/convert-adapty.md)
- **Add Unity IAP 5 to a project with no existing IAP:** See [references/path-add-iap-to-new-project.md](references/path-add-iap-to-new-project.md)
- **Implement IAP D2C Capabilities (third-party payment provider — Stripe/Coda, requires v5.4+):** See [references/path-implement-iap-d2c.md](references/path-implement-iap-d2c.md)

Read reference files on demand — only when you need the API gotchas, platform extension details, or migration mappings.

## Core rules

Read the linked page for the flow and sample code before writing each part, and reread it if the code fails. The rules below are the mistakes agents make that the pages don't stop them making.

### Initialization

Read [Initialize Unity IAP](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing.md) and [Create your catalog programmatically](https://docs.unity.com/en-us/iap/code-catalog.md).

- Subscribe to every event **before** `await store.Connect()`, because pending purchases from a previous session can fire as soon as the store connects. Initialize in `Awake()` so IAP is ready before other `Start()` methods.
- Subscribe to the failure event of every call (`OnStoreDisconnected`, `OnProductsFetchFailed`, `OnPurchasesFetchFailed`, `OnPurchaseFailed`) and to `OnPurchaseDeferred`. Missing failure events cause runtime warnings, and a missing `OnPurchaseDeferred` silently drops Ask-to-Buy and Google deferred purchases.
- `GetProducts()` and `GetPurchases()` return caches. Call `FetchProducts()` / `FetchPurchases()` first; `FetchPurchases()` overwrites the whole cached list.

### Purchases

Read [Purchases](https://docs.unity.com/en-us/iap/purchases.md).

- `store.PurchaseProduct(product)` leads to `OnPurchasePending` with a `PendingOrder`. Grant the content, save it, then call `store.ConfirmPurchase(pendingOrder)`, because unconfirmed purchases are re-delivered on the next launch.
- `OnPurchasePending` can fire more than once for the same purchase (for example, after a restart before confirmation). Check whether the content was already granted before granting it.
- Confirmed consumables are NOT returned by `FetchPurchases()`. Track consumable grants yourself, for example in Cloud Save or Economy.
- On `OnPurchaseDeferred`, show a pending state and don't grant. The purchase arrives later through `OnPurchasePending` if approved.

### Restore

Read [Restore purchases](https://docs.unity.com/en-us/iap/restore-purchases.md).

- `store.RestoreTransactions(callback)` re-delivers non-consumables and renewable subscriptions, each through `OnPurchasePending`. Add a "Restore Purchases" button on iOS, because Apple's review guidelines require one.
- Apple non-renewable subscriptions can't be restored through `RestoreTransactions`, so track them server-side.

### Receipt validation

Read [Receipt validation](https://docs.unity.com/en-us/iap/receipt-validation.md).

- Google Play: validate locally with `CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier)`. Generate the tangle with **Services > In-App Purchasing > Receipt Validation Obfuscator**.
- Apple StoreKit 2: local validation is a no-op. Send `order.Info.Apple?.jwsRepresentation` to a server for validation.

### Ownership and subscriptions

Read the [EntitlementStatus API](https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.EntitlementStatus.html) and the [SubscriptionInfo class reference](https://docs.unity.com/en-us/iap/subscriptioninfo-class-reference.md).

- To check whether the player owns a product, call `store.CheckEntitlement(product)`, handle `store.OnCheckEntitlement`, and compare with `EntitlementStatus.FullyEntitled`. This replaces v4's `product.hasReceipt`.
- Subscription info is on `order.Info.PurchasedProductInfo`, NOT on `CartItem`. `IsSubscribed()` returns `Result`, not `bool`, so compare with `== Result.True`.

### Compile errors

- `ProductFetchFailed` has `.FailureReason` and `.FailedFetchProducts`, not `.Message`, and `StoreConnectionFailureDescription` has `.Message`, not `.reason`. A wrong name causes CS1061. The full table and the other common v5 mistakes are in [api-notes.md](references/api-notes.md).

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `eval` rather than adding logging. Take no screenshots, because they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

After writing code that uses this package, also read the code back and check it, because IAP code can compile cleanly and still lose or double-grant purchases:

1. Confirm all API calls match the v5 signatures on the [API reference](#api-reference) pages for the installed version — do NOT use v4 legacy patterns (`IStoreListener`, `UnityPurchasing.Initialize`, `ConfigurationBuilder`).
2. Check the "Anti-Hallucination: Common v5 Mistakes" table in [api-notes.md](references/api-notes.md) — do NOT use `OnStoreConnectionFailed` (use `OnStoreDisconnected`), do NOT pass callbacks to `FetchProducts`/`FetchPurchases` (use events), do NOT use `product.receipt` (use `order.Info.Receipt`).
3. Check that all required events are subscribed **before** calling `Connect()` (see [Initialization](#initialization)).
4. Verify the two-step purchase flow: `OnPurchasePending` → grant content → `ConfirmPurchase(pendingOrder)`.
5. Confirm both success and failure events are subscribed for every async operation (`OnProductsFetched`/`OnProductsFetchFailed`, `OnStoreConnected`/`OnStoreDisconnected`, etc.).
6. If handling subscriptions, verify `IsSubscribed()` is compared with `== Result.True`, not cast to `bool`.
7. If updated files coexist with legacy versions in the same project, use a unique namespace (e.g., add `.Updated` suffix) to avoid CS0101/CS0111 compilation errors.
8. If the project subscribes to `OnAuthAccountChanged` (v5.4+): verify the handler re-fetches products and purchases from scratch — Unity IAP clears both caches before raising this event. Do not read `GetProducts()` or `GetPurchases()` inside the handler.

## Final report

Report the following to the user, in this order:

1. What changed: each file you changed, with the old and new API calls or values where they apply (for example, `IStoreListener.ProcessPurchase` → `OnPurchasePending`).
2. How you verified it: the outcome of each Final step check, including the console error count and anything you read through `eval` in Play mode.
3. Incidental side effects: anything you touched beyond the requested change, such as packages you installed or upgraded, project or Services settings you changed, generated files (for example tangle classes or `IAPProductCatalog.json`), and temporary files you created or removed.
4. Follow-up the user must do, for example product setup in App Store Connect or the Google Play Console, and sandbox testing on a device. Include the manual steps that the path reference file lists.
