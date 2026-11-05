# Unity IAP Platform-Specific Notes

This file lists only the platform behavior agents commonly get wrong. For the extension APIs, setup steps and code samples, read the docs pages below before writing the code, and again if a step fails. Look up individual extension types in the package's `xrefmap.yml`. In API URLs, replace `<VERSION>` with the major.minor of `com.unity.purchasing` in `Packages/packages-lock.json`.

- [Apple App Store](https://docs.unity.com/en-us/iap/apple-store.md): promotional purchases, Ask-to-Buy, offer code redemption, App Account Token and storefront.
- [Use Google Play billing extensions](https://docs.unity.com/en-us/iap/use-google-play-billing-extensions.md): obfuscated IDs, subscription upgrade and downgrade, and deferred subscription changes.
- [Receipt validation](https://docs.unity.com/en-us/iap/receipt-validation.md): Google local validation and Apple server-side validation.
- [SubscriptionInfo class reference](https://docs.unity.com/en-us/iap/subscriptioninfo-class-reference.md): per-store behavior of each method.

## Table of Contents

- [Apple App Store](#apple-app-store)
- [Google Play Store](#google-play-store)
- [SubscriptionInfo platform differences](#subscriptioninfo-platform-differences)
- [Cross-platform rules](#cross-platform-rules)

## Apple App Store

- If you subscribe to `AppleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted`, you MUST eventually call `ContinuePromotionalPurchases()`, because otherwise the promotional purchase hangs indefinitely.
- Ask-to-Buy purchases arrive through `store.OnPurchaseDeferred`. Show a "pending approval" state and don't grant content, because the purchase arrives later through `OnPurchasePending` if a parent approves it, or fails if they decline.
- To test Ask-to-Buy in the sandbox, set `AppleStoreExtendedPurchaseService.simulateAskToBuy = true`. Remove it before release.
- Handle `OnEntitlementRevoked` (for example after a refund) by removing the content. It receives the product ID as a `string`.
- Check `AppleStoreExtendedService.canMakePayments` before showing a store, because parental controls can block payments.
- Apple non-renewable subscriptions can't be restored through `RestoreTransactions`. Track them server-side.

## Google Play Store

- `UpgradeDowngradeSubscription` takes the player's current subscription `Order`, the new `Product`, and a `GooglePlayReplacementMode`. Read the [GooglePlayReplacementMode API](https://docs.unity3d.com/Packages/com.unity.purchasing@<VERSION>/api/UnityEngine.Purchasing.GooglePlayReplacementMode.html) page to pick the mode that matches the user's billing intent, and confirm the choice with the user, because each mode charges the player differently.
- Pass hashed values to `SetObfuscatedAccountId` and `SetObfuscatedProfileId`, never raw user IDs, because Google rejects personally identifiable values.
- `OnDeferredPaymentUntilRenewalDate` is on `GooglePlayStoreExtendedPurchaseService`, not on `StoreController`.

## SubscriptionInfo platform differences

| Method | Apple | Google |
|---|---|---|
| `GetPurchaseDate()` | Returns the **most recent renewal** date | Returns the **original purchase** date |
| `GetCancelDate()` | Returns the cancellation date | Returns `DateTime.MinValue` (not supported) |

Check `IsSubscribed()` first. A `Result.Unsupported` result means the platform doesn't provide that information.

## Cross-platform rules

- Platform extension services are `null` on other platforms and in the Editor:

  | Extension | iOS | Android | Editor |
  |---|---|---|---|
  | `AppleStoreExtendedService` | Available | `null` | `null` |
  | `AppleStoreExtendedPurchaseService` | Available | `null` | `null` |
  | `GooglePlayStoreExtendedService` | `null` | Available | `null` |
  | `GooglePlayStoreExtendedPurchaseService` | `null` | Available | `null` |

  Null-check each one with `if` before use, because `?.` doesn't work with `+=`.
- Add a "Restore Purchases" button on iOS that calls `store.RestoreTransactions()`, because Apple's review guidelines require it.
- Check subscription status at every app launch, not only at purchase time, because subscriptions can expire or be cancelled outside the app.
- Test with Apple sandbox accounts and Google license testing accounts, so test purchases aren't charged.
