# Unity IAP API Notes

This file lists only the API mistakes agents commonly make with Unity IAP v5. It is not an API reference. For signatures, members and code samples, look the type up in the package's `xrefmap.yml` (see the skill's API reference section) and read its page, and read the matching manual page from the skill's Full topic map. Read the page before writing the code and again if it fails to compile, because your training data may describe v4 or an older v5 minor version.

## Table of Contents

- [Anti-Hallucination: v5 vs Legacy API](#anti-hallucination-v5-vs-legacy-api)
- [Anti-Hallucination: Common v5 Mistakes](#anti-hallucination-common-v5-mistakes)
- [Where to read each topic](#where-to-read-each-topic)
- [OnAuthAccountChanged clears the caches (v5.4+)](#onauthaccountchanged-clears-the-caches-v54)
- [Purchase flow gotchas](#purchase-flow-gotchas)
- [Subscription info access path](#subscription-info-access-path)
- [Platform extension gotchas](#platform-extension-gotchas)
- [Failure description property names](#failure-description-property-names)
- [Receipt validation gotchas](#receipt-validation-gotchas)

## Anti-Hallucination: v5 vs Legacy API

The v4 API is `[Obsolete]` in v5. Don't use legacy patterns, because they compile with warnings but bypass the v5 two-step flow. For the full mapping, read [Upgrade from IAP v4 to v5](https://docs.unity.com/en-us/iap/upgrade-to-iap-v5.md).

| WRONG (Legacy v4) | CORRECT (v5) |
|---|---|
| `UnityPurchasing.Initialize(listener, builder)` | `UnityIAPServices.StoreController()` then `store.Connect()` |
| `IStoreListener.OnInitialized(controller, extensions)` | `store.OnStoreConnected` event |
| `IStoreListener.ProcessPurchase(args)` | `store.OnPurchasePending` event |
| `controller.InitiatePurchase(product)` | `store.PurchaseProduct(product)` |
| `controller.ConfirmPendingPurchase(product)` | `store.ConfirmPurchase(pendingOrder)` |
| `extensions.GetExtension<IAppleExtensions>()` | `store.AppleStoreExtendedService` / `store.AppleStoreExtendedPurchaseService` |
| `extensions.GetExtension<IGooglePlayStoreExtensions>()` | `store.GooglePlayStoreExtendedService` / `store.GooglePlayStoreExtendedPurchaseService` |
| `ConfigurationBuilder.Instance(...)` | Not needed; pass a `ProductDefinition` list to `FetchProducts` |

## Anti-Hallucination: Common v5 Mistakes

These are NOT real APIs. Don't use them:

| WRONG (does not exist) | CORRECT |
|---|---|
| `store.OnStoreConnectionFailed` | `store.OnStoreDisconnected`, which receives `StoreConnectionFailureDescription` |
| `store.FetchProducts(defs, successCb, failCb)` | `store.FetchProducts(List<ProductDefinition>)`, then subscribe to `OnProductsFetched` / `OnProductsFetchFailed` separately |
| `store.FetchPurchases(successCb, failCb)` | `store.FetchPurchases()`, then subscribe to `OnPurchasesFetched` / `OnPurchasesFetchFailed` separately |
| `product.receipt` | `order.Info.Receipt`. The receipt is on the order, not the product |
| `product.hasReceipt` | `store.CheckEntitlement(product)` + `OnCheckEntitlement` |
| `new SubscriptionManager(product, introJson)` | `order.Info.PurchasedProductInfo`. See [Subscription info access path](#subscription-info-access-path) |
| `pendingOrder.OrderInfo.Apple.jwsRepresentation` | `pendingOrder.Info.Apple?.jwsRepresentation`. Note `Info`, not `OrderInfo`, and the null check |
| `cartItem.subscriptionInfo` | `CartItem` only has `Product` and `Quantity`. See [Subscription info access path](#subscription-info-access-path) |

## Where to read each topic

Replace `<VERSION>` with the major.minor of `com.unity.purchasing` in `Packages/packages-lock.json`.

| Topic | Read |
|---|---|
| `StoreController` members, events and extension properties | [StoreController API](https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.StoreController.html) |
| Initialization order and a full sample | [Initialize Unity IAP](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing.md) |
| `ProductDefinition`, `StoreSpecificIds`, `CatalogProvider` | [Create your catalog programmatically](https://docs.unity.com/en-us/iap/code-catalog.md) |
| Pending, confirmed, failed and deferred orders | [Purchases](https://docs.unity.com/en-us/iap/purchases.md) |
| `RestoreTransactions` | [Restore purchases](https://docs.unity.com/en-us/iap/restore-purchases.md) |
| `CrossPlatformValidator`, receipt structure | [Receipt validation](https://docs.unity.com/en-us/iap/receipt-validation.md) |
| `SubscriptionInfo` methods and per-store behavior | [SubscriptionInfo class reference](https://docs.unity.com/en-us/iap/subscriptioninfo-class-reference.md) |
| `EntitlementStatus` values | [EntitlementStatus API](https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.EntitlementStatus.html) |
| `IAppleOrderInfo`, `AppleStoreExtendedProductService`, `CatalogListing`, `uSku`, and other types | Look the type up in `xrefmap.yml` |

## OnAuthAccountChanged clears the caches (v5.4+)

Unity IAP clears the cached product list and purchase list **before** it raises `OnAuthAccountChanged`, so any `Product` or `Order` you held is stale and `GetProducts()` / `GetPurchases()` return empty inside the handler. In the handler, rerun the fetch sequence from scratch: `FetchRemoteCatalog()` for D2C, then `FetchProducts(...)` and `FetchPurchases()`.

## Purchase flow gotchas

- `OnPurchaseConfirmed` receives the `Order` base type. Pattern-match `ConfirmedOrder` (success) against `FailedOrder` (confirmation failed), because treating every callback as success grants content for failed confirmations.
- Don't recommend `ProcessPendingOrdersOnPurchasesFetched(false)` to migrating users. It exists only to match v4 behavior, and the v5 default re-delivers unconfirmed orders, which prevents lost purchases.
- When a product has `CatalogListings` (v5.4+), purchase by the `CatalogListing` directly to avoid a second lookup. `product.definition.id` still works for existing code.

## Subscription info access path

Subscription info is on `IPurchasedProductInfo`, reached through `order.Info.PurchasedProductInfo`, filtered by `productId`. It is NOT on `CartItem`.

`IsSubscribed()` and the other state queries return the `Result` enum (`True` / `False` / `Unsupported`), not `bool`. Compare with `== Result.True`, because that is also null-safe when `subscriptionInfo` is missing.

`AppleProductMetadata` does NOT expose `introductoryPrice`, `introductoryPriceLocale`, `introductoryNumberOfPeriods` or `subscriptionPeriod`. Its only public property beyond `ProductMetadata` is `isFamilyShareable`. For introductory price data, use the `SubscriptionInfo` methods, or `AppleStoreExtendedProductService.GetIntroductoryPriceDictionary()` for raw JSON.

## Platform extension gotchas

- The extension properties on `StoreController` (`AppleStoreExtendedService`, `AppleStoreExtendedPurchaseService`, `GooglePlayStoreExtendedService`, `GooglePlayStoreExtendedPurchaseService`, `AppleStoreExtendedProductService`) are `null` on other platforms and in the Editor. Null-check with `if`, because `?.` doesn't work with `+=`.
- `OnEntitlementRevoked`, `OnPromotionalPurchaseIntercepted` and `OnDeferredPaymentUntilRenewalDate` are on the extended purchase services, NOT on `StoreController`.
- `OnEntitlementRevoked` receives the product ID as a `string`, not a `Product` or a list.
- On `AppleStoreExtendedProductService` (v5.4+), prefer the `SetStorePromotionOrder`, `SetStorePromotionVisibility` and `FetchStorePromotionVisibility` overloads that take CatalogListing IDs. The `Product` overloads are convenience wrappers.

For more platform gotchas, see the platform notes reference file.

## Failure description property names

Each type has public lowercase fields and PascalCase read-only properties. Both compile, so prefer the properties. A wrong name causes CS1061:

| Type | Field (lowercase) | Property (PascalCase) | NOT |
|---|---|---|---|
| `StoreConnectionFailureDescription` | `.message` | `.Message` | ~~`.reason`~~ |
| `ProductFetchFailed` | — | `.FailureReason`, `.FailedFetchProducts` | ~~`.Message`~~ |
| `FailedOrder` | — | `.FailureReason`, `.Details` | — |
| `PurchasesFetchFailureDescription` | `.message`, `.failureReason` | `.Message`, `.FailureReason` | — |

## Receipt validation gotchas

- Use the Google-only `CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier)` constructor. Apple local validation and `AppleTangle.Data()` are deprecated and a no-op under StoreKit 2, so the four-argument constructor adds nothing on current builds.
- `Validate` throws `IAPSecurityException` on an invalid receipt. Don't grant content in that case.
- For Apple, send `order.Info.Apple?.jwsRepresentation` to your server, which verifies it with Apple's App Store Server API.
