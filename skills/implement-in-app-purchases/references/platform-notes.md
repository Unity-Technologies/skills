# Unity IAP Platform-Specific Notes

## Table of Contents

- [Apple App Store Extensions](#apple-app-store-extensions)
- [Google Play Store Extensions](#google-play-store-extensions)
- [Apple JWS Representation (Server-Side Validation)](#apple-jws-representation-server-side-validation)
- [SubscriptionInfo Platform Nuances](#subscriptioninfo-platform-nuances)
- [Cross-Platform Best Practices](#cross-platform-best-practices)
- [Platform Extension Availability](#platform-extension-availability)

## Apple App Store Extensions

Access via `store.AppleStoreExtendedPurchaseService` (for purchase features) and `store.AppleStoreExtendedService` (for store features).

### Promotional Purchases

Apple can present purchases from the App Store product page. Intercept and handle them:

```csharp
if (store.AppleStoreExtendedPurchaseService != null)
{
    store.AppleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted += (product) =>
    {
        // Player tapped "Buy" on App Store product page
        // You can delay the purchase (e.g., show a loading screen first)
        Debug.Log($"Promotional purchase intercepted: {product.definition.id}");

        // When ready, continue the purchase
        store.AppleStoreExtendedPurchaseService.ContinuePromotionalPurchases();
    };
}
```

**CRITICAL:** If you subscribe to `OnPromotionalPurchaseIntercepted`, you MUST eventually call `ContinuePromotionalPurchases()` or the purchase will hang indefinitely.

### Ask-to-Buy (Parental Approval)

On devices with Family Sharing and a child account, purchases may require parental approval:

```csharp
store.OnPurchaseDeferred += (deferredOrder) =>
{
    // Purchase is waiting for parental approval
    // Show UI: "Purchase pending approval"
    // Do NOT grant content yet
    Debug.Log("Purchase deferred - awaiting approval");
};
```

The purchase will complete later (triggering `OnPurchasePending`) when approved, or fail when rejected.

For testing in sandbox:

```csharp
// Simulate Ask-to-Buy in the editor/sandbox
store.AppleStoreExtendedPurchaseService.simulateAskToBuy = true;
```

### Offer Code Redemption

Present the Apple offer code redemption sheet:

```csharp
store.AppleStoreExtendedPurchaseService.PresentCodeRedemptionSheet();
```

### Entitlement Revocation

Listen for revoked entitlements (e.g., refund granted):

```csharp
store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked += (productId) =>
{
    Debug.Log($"Entitlement revoked for: {productId}");
    // Remove the content/feature from the player
};
```

### Store Capabilities

```csharp
// Check if the device can make payments (parental controls may block)
bool canPay = store.AppleStoreExtendedService.canMakePayments;

// Set App Account Token for server-to-server verification
store.AppleStoreExtendedService.SetAppAccountToken(Guid.NewGuid());

// Fetch storefront info (StoreKit 2 only)
store.AppleStoreExtendedService.FetchStorefront(
    (storefront) => Debug.Log($"Storefront: {storefront.CountryCode}"),
    (error) => Debug.LogError($"Storefront error: {error}")
);
```

## Google Play Store Extensions

Access via `store.GooglePlayStoreExtendedPurchaseService` (for purchase features) and `store.GooglePlayStoreExtendedService` (for store features).

### Subscription Upgrade/Downgrade

Change a player's subscription tier:

```csharp
// Get the current subscription order and the new product
Order currentOrder = /* the player's current subscription order */;
Product newProduct = store.GetProductById("com.mygame.vip_annual");

store.GooglePlayStoreExtendedPurchaseService.UpgradeDowngradeSubscription(
    currentOrder,
    newProduct,
    GooglePlayReplacementMode.ChargeFullPrice
);
```

### GooglePlayReplacementMode Values

| Mode | Behavior |
|---|---|
| `UnknownReplacementMode` | Default |
| `WithTimeProration` | Remaining time is adjusted |
| `ChargeProratedPrice` | Prorated charge for the upgrade |
| `WithoutProration` | New plan starts at next billing |
| `ChargeFullPrice` | Full price charged immediately |
| `Deferred` | Change takes effect at next billing |

### Obfuscated IDs

Set obfuscated account/profile IDs for fraud detection:

```csharp
store.GooglePlayStoreExtendedService.SetObfuscatedAccountId("hashed_account_id");
store.GooglePlayStoreExtendedService.SetObfuscatedProfileId("hashed_profile_id");
```

Retrieve them from an order:

```csharp
string accountId = store.GooglePlayStoreExtendedPurchaseService.GetObfuscatedAccountId(order);
string profileId = store.GooglePlayStoreExtendedPurchaseService.GetObfuscatedProfileId(order);
```

### Deferred Payment Events

```csharp
store.GooglePlayStoreExtendedPurchaseService.OnDeferredPaymentUntilRenewalDate += (deferredOrder) =>
{
    Debug.Log($"Payment deferred until renewal: {deferredOrder.SubscriptionOrdered.definition.id}");
};
```

## Apple JWS Representation (Server-Side Validation)

For server-side Apple receipt validation (replacing the deprecated `CrossPlatformValidator`):

```csharp
store.OnPurchasePending += (pendingOrder) =>
{
    // Access the JWS-signed transaction
    string jws = pendingOrder.Info.Apple?.jwsRepresentation;
    // Send to your server for validation with Apple's App Store Server API (v2)
    ValidateOnServer(jws);
};
```

The `jwsRepresentation` contains a signed JSON Web Signature that can be verified using Apple's public keys.

## SubscriptionInfo Platform Nuances

| Method | Apple | Google |
|---|---|---|
| `GetPurchaseDate()` | Returns the **renewal date** (most recent renewal) | Returns the **original purchase date** |
| `GetCancelDate()` | Returns actual cancellation date | Returns `DateTime.MinValue` (not supported) |

Always check `IsSubscribed()` first — if the result is `Result.Unsupported`, the platform doesn't provide that information.

## Cross-Platform Best Practices

| Concern | Recommendation |
|---|---|
| Restore button | Required on iOS (Apple guideline). Add a "Restore Purchases" button that calls `store.RestoreTransactions()`. |
| Receipt validation | **Google Play:** Use `CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier)` for local validation. **Apple (StoreKit 2):** Local validation via `CrossPlatformValidator` is a **no-op** — use `order.Info.Apple?.jwsRepresentation` for server-side validation instead. |
| Pending purchases | Always handle `OnPurchasePending` and call `ConfirmPurchase`. Unconfirmed purchases persist across launches. |
| Product IDs | Use reverse-domain naming: `com.company.game.product` |
| Testing | Use sandbox accounts (Apple) and license testing accounts (Google) for testing without real charges. |
| Subscriptions | Always check `IsSubscribed()` at app launch, not just purchase time. Subscriptions can expire or be cancelled externally. |

## Platform Extension Availability

| Extension | iOS | Android | Editor |
|---|---|---|---|
| `AppleStoreExtendedService` | Available | `null` | `null` |
| `AppleStoreExtendedPurchaseService` | Available | `null` | `null` |
| `GooglePlayStoreExtendedService` | `null` | Available | `null` |
| `GooglePlayStoreExtendedPurchaseService` | `null` | Available | `null` |

**CRITICAL:** Always null-check platform extensions before use:

```csharp
if (store.AppleStoreExtendedPurchaseService != null)
{
    store.AppleStoreExtendedPurchaseService.OnPromotionalPurchaseIntercepted += HandlePromo;
}
```
