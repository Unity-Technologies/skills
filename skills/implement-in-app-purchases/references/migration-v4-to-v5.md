# Unity IAP v4 to v5 Migration Guide

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Procedure](#procedure)
- [Migration gotchas](#migration-gotchas)

## Trigger Phrases

- "Migrate from Unity IAP v4 to v5"
- "Upgrade Unity IAP to v5"
- "Update IAP from v4"
- "Migrate IStoreListener to StoreController"
- "Replace ConfigurationBuilder with v5 IAP"
- "Update UnityPurchasing.Initialize to v5"

## Procedure

Unity IAP v5 replaces the listener-based `IStoreListener` pattern with `StoreController` events, so every IAP call site changes.

1. Read [Upgrade from IAP v4 to v5](https://docs.unity.com/en-us/iap/upgrade-to-iap-v5.md) in full before changing code. It holds the official v4-to-v5 mapping for initialization, the purchase flow, `ConfigurationBuilder`, entitlement checks, `SubscriptionManager`, receipt validation, platform extensions and Codeless IAP. Reread the matching section whenever a migrated call fails to compile.
2. List every v4 call site in the project (`IStoreListener`, `IDetailedStoreListener`, `UnityPurchasing.Initialize`, `ConfigurationBuilder`, `GetExtension<`, `SubscriptionManager`, `product.receipt`, `product.hasReceipt`).
3. Migrate each call site using the docs mapping, then check it against the [gotchas](#migration-gotchas) below, because they are the mistakes agents make most often in this migration.
4. For a type or member the upgrade page doesn't cover, look it up in the package's `xrefmap.yml` for the installed version.

## Migration gotchas

- **Subscribe before `Connect()`.** Pending purchases from a previous session can fire as soon as the store connects. A handler registered later misses them. Initialize in `Awake()` so IAP is ready before other `Start()` methods.
- **`ProcessPurchase` logic goes in two places.** Put it in `OnPurchasePending` (new purchases) and in `OnPurchasesFetched` (restored and existing purchases), because v4 delivered both through `ProcessPurchase`.
- **Returning `PurchaseProcessingResult.Pending`** becomes: keep the `PendingOrder` and don't call `ConfirmPurchase` until ready. There is no equivalent of returning `Complete`.
- **`ConfirmPurchase` takes the `PendingOrder`, not the `Product`.**
- **`OnPurchaseConfirmed` receives the `Order` base type.** Pattern-match `ConfirmedOrder` against `FailedOrder`, because confirmation can fail.
- **Ownership checks.** Replace `product.hasReceipt` and `product.receipt == null` with `store.CheckEntitlement(product)` and `OnCheckEntitlement`, because it handles refunds and expiry across platforms. Use a manual `bool` flag only if the user's design needs it.
- **Subscribe to every failure event and to `OnPurchaseDeferred`.** Missing failure events cause runtime warnings, and missing `OnPurchaseDeferred` silently drops Ask-to-Buy and Google deferred purchases.
- **Failure property names.** `StoreConnectionFailureDescription` has `.Message`, not `.reason`. `ProductFetchFailed` has `.FailureReason`, not `.Message`.
- **Platform config moves after `Connect()`.** `SetAppAccountToken`, `SetObfuscatedAccountId`, `SetObfuscatedProfileId` and `canMakePayments` used to be set on `ConfigurationBuilder` before initialization. In v5 they are on the extended services, which exist only after `await store.Connect()`.
- **`SetApplicationUsername(hash)` becomes `SetAppAccountToken(Guid)`.** It takes a `Guid` that identifies the user's account, not a hash.
- **Platform events are on the extended services, not `StoreController`.** `OnPromotionalPurchaseIntercepted` and `OnEntitlementRevoked` are on `AppleStoreExtendedPurchaseService`. `OnDeferredPaymentUntilRenewalDate` is on `GooglePlayStoreExtendedPurchaseService` and receives a `DeferredPaymentUntilRenewalDateOrder`, not a `Product`. Null-check the service with `if`, because `?.` doesn't work with `+=`.
- **`OnEntitlementRevoked` receives one product ID `string`**, not `List<Product>`.
- **`UpgradeDowngradeSubscription` takes `Order` and `Product` objects** instead of string IDs, and a `GooglePlayReplacementMode`, not the deprecated `GooglePlayProrationMode`.
- **Apple promotion and intro-price APIs moved, not removed.** `GetIntroductoryPriceDictionary`, `GetProductDetails` and the `StorePromotion` methods are on `AppleStoreExtendedProductService`. `AppleProductMetadata` has no intro-price properties, so use the `SubscriptionInfo` methods instead.
- **`SubscriptionManager` is replaced** by `order.Info.PurchasedProductInfo`. Subscription info is NOT on `CartItem`. Methods are PascalCase (`IsSubscribed()`) and still return `Result`, so compare with `== Result.True`, not `?? false`.
- **Receipt validation.** Under StoreKit 2, Apple local validation returns an empty array. Use the Google-only `CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier)` constructor, and send `order.Info.Apple?.jwsRepresentation` to a server for Apple.
- **Don't recommend `ProcessPendingOrdersOnPurchasesFetched(false)`.** It restores v4 behavior but loses the v5 re-delivery of unconfirmed purchases.
- **Codeless:** `CodelessIAPStoreListener.initializationComplete` becomes `CodelessIAPStoreListener.IsInitialized()`.
