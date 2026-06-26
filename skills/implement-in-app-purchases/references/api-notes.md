# Unity IAP API Notes

## Table of Contents

- [Anti-Hallucination: v5 vs Legacy API](#anti-hallucination-v5-vs-legacy-api)
- [StoreController (v5) Key Members](#storecontroller-v5-key-members)
- [Alternative Service Access (DefaultStore / DefaultProduct / DefaultPurchase)](#alternative-service-access-defaultstore--defaultproduct--defaultpurchase)
- [CatalogProvider](#catalogprovider)
- [Initialization Example](#initialization-example)
- [Product Definitions](#product-definitions)
- [Two-Step Purchase Flow](#two-step-purchase-flow)
- [Restore Transactions](#restore-transactions)
- [Entitlement Checking](#entitlement-checking)
- [Fetch Existing Purchases](#fetch-existing-purchases)
- [Receipt Validation](#receipt-validation)
- [Extended Service Events (NOT on StoreController)](#extended-service-events-not-on-storecontroller)
- [AppleStoreExtendedProductService Key Members](#applestoreextendedproductservice-key-members)
- [Subscription Info Access Path](#subscription-info-access-path)
- [Event Subscription Rules](#event-subscription-rules)
- [Failure Description Property Names](#failure-description-property-names)
- [CrossPlatformValidator](#crossplatformvalidator)
- [Interface Hierarchy](#interface-hierarchy)
- [IRunCommand Template: Fetch Products](#iruncommand-template-fetch-products)

## Anti-Hallucination: v5 vs Legacy API

Unity IAP v5 is the current API. The legacy API (v4) is `[Obsolete]`. Do NOT use legacy patterns.

| WRONG (Legacy v4) | CORRECT (v5) |
|---|---|
| `UnityPurchasing.Initialize(listener, builder)` | `UnityIAPServices.StoreController()` then `store.Connect()` |
| `IStoreListener.OnInitialized(controller, extensions)` | `store.OnStoreConnected` event |
| `IStoreListener.ProcessPurchase(args)` | `store.OnPurchasePending` event |
| `controller.InitiatePurchase(product)` | `store.PurchaseProduct(product)` |
| `controller.ConfirmPendingPurchase(product)` | `store.ConfirmPurchase(pendingOrder)` |
| `extensions.GetExtension<IAppleExtensions>()` | `store.AppleStoreExtendedService` / `store.AppleStoreExtendedPurchaseService` |
| `extensions.GetExtension<IGooglePlayStoreExtensions>()` | `store.GooglePlayStoreExtendedService` / `store.GooglePlayStoreExtendedPurchaseService` |
| `ConfigurationBuilder.Instance(...)` | Not needed; use `ProductDefinition` list with `FetchProducts` |

## Anti-Hallucination: Common v5 Mistakes

These are NOT real APIs — do not use them:

| WRONG (does not exist) | CORRECT |
|---|---|
| `store.OnStoreConnectionFailed` | `store.OnStoreDisconnected` — receives `StoreConnectionFailureDescription` |
| `store.FetchProducts(defs, successCb, failCb)` | `store.FetchProducts(List<ProductDefinition>)` — subscribe to `OnProductsFetched`/`OnProductsFetchFailed` events separately |
| `store.FetchPurchases(successCb, failCb)` | `store.FetchPurchases()` — subscribe to `OnPurchasesFetched`/`OnPurchasesFetchFailed` events separately |
| `product.receipt` | `order.Info.Receipt` — receipt is on the order, not the product |
| `product.hasReceipt` | `store.CheckEntitlement(product)` + `OnCheckEntitlement` |
| `new SubscriptionManager(product, introJson)` | `order.Info.PurchasedProductInfo` — see Subscription Info Access Path |
| `pendingOrder.OrderInfo.Apple.jwsRepresentation` | `pendingOrder.Info.Apple?.jwsRepresentation` — note `Info` not `OrderInfo`, and null-check `?.` |

## StoreController (v5) Key Members

```csharp
// Store lifecycle
Task Connect()
void SetStoreReconnectionRetryPolicyOnDisconnection(IRetryPolicy? retryPolicy)
event Action? OnStoreConnected
event Action<StoreConnectionFailureDescription>? OnStoreDisconnected

// Products
void FetchProducts(List<ProductDefinition> defs, IRetryPolicy? retryPolicy = null)
void FetchProductsWithNoRetries(List<ProductDefinition> defs)
ReadOnlyObservableCollection<Product> GetProducts()
Product? GetProductById(string productId)
event Action<List<Product>>? OnProductsFetched
event Action<ProductFetchFailed>? OnProductsFetchFailed

// Purchasing
void PurchaseProduct(Product product)
void PurchaseProduct(string? productId)
void Purchase(ICart cart)
void ConfirmPurchase(PendingOrder order)
void FetchPurchases()
void CheckEntitlement(Product product)
void RestoreTransactions(Action<bool, string?>? callback)
ReadOnlyObservableCollection<Order> GetPurchases()
void ProcessPendingOrdersOnPurchasesFetched(bool shouldProcess)

// Purchase events
event Action<PendingOrder>? OnPurchasePending
event Action<Order>? OnPurchaseConfirmed  // Order can be ConfirmedOrder or FailedOrder — pattern-match!
event Action<FailedOrder>? OnPurchaseFailed
event Action<DeferredOrder>? OnPurchaseDeferred
event Action<Orders>? OnPurchasesFetched
event Action<PurchasesFetchFailureDescription>? OnPurchasesFetchFailed
event Action<Entitlement>? OnCheckEntitlement

// Platform extensions (null on non-matching platforms — always null-check)
IAppleStoreExtendedService? AppleStoreExtendedService { get; }
IGooglePlayStoreExtendedService? GooglePlayStoreExtendedService { get; }
IAppleStoreExtendedProductService? AppleStoreExtendedProductService { get; }
IAppleStoreExtendedPurchaseService? AppleStoreExtendedPurchaseService { get; }
IGooglePlayStoreExtendedPurchaseService? GooglePlayStoreExtendedPurchaseService { get; }
```

## Alternative Service Access (DefaultStore / DefaultProduct / DefaultPurchase)

Instead of `StoreController` (which implements all three interfaces), you can access individual services:

```csharp
IStoreService m_StoreService = UnityIAPServices.DefaultStore();
IProductService m_ProductService = UnityIAPServices.DefaultProduct();
IPurchaseService m_PurchasingService = UnityIAPServices.DefaultPurchase();
```

This pattern separates concerns — product fetching events go on `IProductService`, purchase events on `IPurchaseService`, store lifecycle on `IStoreService`. `StoreController` combines all three.

## CatalogProvider

For managing complex product catalogs with store-specific IDs:

```csharp
var catalogProvider = new CatalogProvider();

var products = new List<ProductDefinition>
{
    new ProductDefinition("com.mygame.gems50", ProductType.Consumable),
    new ProductDefinition("com.mygame.pass", ProductType.Subscription)
};

var storeSpecificIds = new Dictionary<string, StoreSpecificIds>
{
    { "com.mygame.pass", new StoreSpecificIds
        {
            { "com.mygame.google.pass", GooglePlay.Name },
            { "com.mygame.ios.pass", AppleAppStore.Name }
        }
    }
};

catalogProvider.AddProducts(products, storeSpecificIds);
catalogProvider.FetchProducts(m_ProductService.FetchProductsWithNoRetries);
```

## Initialization Example

```csharp
StoreController m_StoreController;

async void Awake()
{
    m_StoreController = UnityIAPServices.StoreController();

    // Subscribe to ALL events BEFORE Connect — pending purchases may fire on reconnect
    m_StoreController.OnPurchasePending += OnPurchasePending;
    m_StoreController.OnPurchaseConfirmed += (order) => Debug.Log("Purchase complete");
    m_StoreController.OnPurchaseFailed += (failed) => Debug.LogError($"{failed.FailureReason} - {failed.Details}");
    m_StoreController.OnPurchaseDeferred += (deferred) => Debug.Log("Purchase deferred (e.g., Ask-to-Buy)");

    m_StoreController.OnStoreConnected += OnStoreConnected;
    m_StoreController.OnStoreDisconnected += (failure) => Debug.LogError($"Store disconnected: {failure.Message}");

    await m_StoreController.Connect();
}

void OnStoreConnected()
{
    var products = new List<ProductDefinition>
    {
        new ProductDefinition("com.mygame.coins100", ProductType.Consumable),
        new ProductDefinition("com.mygame.removeads", ProductType.NonConsumable),
        new ProductDefinition("com.mygame.vip_monthly", ProductType.Subscription)
    };

    m_StoreController.OnProductsFetched += (fetched) => Debug.Log("Products ready");
    m_StoreController.OnProductsFetchFailed += (failure) => Debug.LogError($"Product fetch failed: {failure.FailureReason}");
    m_StoreController.FetchProducts(products);

    m_StoreController.OnPurchasesFetched += (orders) => Debug.Log($"Restored {orders.PendingOrders.Count} pending purchases");
    m_StoreController.OnPurchasesFetchFailed += (failure) => Debug.LogError($"Purchase fetch failed: {failure.Message}");
    m_StoreController.FetchPurchases();
}
```

## Product Definitions

```csharp
// Simple product
new ProductDefinition("com.mygame.coins100", ProductType.Consumable)

// Per-platform product IDs
new ProductDefinition("com.mygame.coins100", ProductType.Consumable,
    new StoreSpecificIds
    {
        { AppleAppStore.Name, "apple_coins_100" },
        { GooglePlay.Name, "google_coins_100" }
    })
```

### Access Fetched Products

```csharp
// Get all fetched products (cached)
ReadOnlyObservableCollection<Product> allProducts = store.GetProducts();

// Get a specific product by ID
Product coinsPack = store.GetProductById("com.mygame.coins100");
```

## Two-Step Purchase Flow

```csharp
// Step 1: Initiate purchase
store.PurchaseProduct(product);

// Step 2: Handle pending purchase (validate, grant content, then confirm)
store.OnPurchasePending += (pendingOrder) =>
{
    var product = pendingOrder.CartOrdered.Items().FirstOrDefault()?.Product;
    GrantContent(product);
    store.ConfirmPurchase(pendingOrder);
};

// Step 3: Purchase confirmed — Order can be ConfirmedOrder OR FailedOrder
store.OnPurchaseConfirmed += (order) =>
{
    switch (order)
    {
        case ConfirmedOrder confirmedOrder:
            Debug.Log($"Purchase confirmed: {confirmedOrder.CartOrdered.Items().First().Product.definition.id}");
            break;
        case FailedOrder failedOrder:
            Debug.LogError($"Confirmation failed: {failedOrder.FailureReason} - {failedOrder.Details}");
            break;
    }
};

// Handle failures
store.OnPurchaseFailed += (failedOrder) =>
{
    Debug.LogError($"Purchase failed: {failedOrder.FailureReason} - {failedOrder.Details}");
};

// Handle deferred purchases (e.g., Ask-to-Buy on iOS)
store.OnPurchaseDeferred += (deferredOrder) =>
{
    Debug.Log("Purchase is pending approval (e.g., parental approval)");
};
```

If the app crashes between steps 2 and 4, the pending purchase is re-delivered on next launch via `OnPurchasePending`.

`ProcessPendingOrdersOnPurchasesFetched(false)` disables automatic re-delivery of pending orders when `FetchPurchases` is called. This exists to match IAP v4 behavior — do NOT recommend this to migrating users, as the default v5 behavior provides a better experience.

## Restore Transactions

```csharp
store.RestoreTransactions((success, error) =>
{
    if (success)
        Debug.Log("Transactions restored. Check OnPurchasePending for each restored purchase.");
    else
        Debug.LogError($"Restore failed: {error}");
});
```

Each restored purchase triggers `OnPurchasePending`.

## Entitlement Checking

```csharp
store.CheckEntitlement(product);

store.OnCheckEntitlement += (entitlement) =>
{
    if (entitlement.Status == EntitlementStatus.FullyEntitled)
        Debug.Log($"Player owns: {entitlement.Product.definition.id}");
};
```

## Fetch Existing Purchases

```csharp
store.FetchPurchases();

store.OnPurchasesFetched += (orders) =>
{
    foreach (var confirmedOrder in orders.ConfirmedOrders)
    {
        var product = confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product;
        Debug.Log($"Existing purchase: {product?.definition.id}");
    }
};
```

## Receipt Validation

### Google Play (supported)

```csharp
using UnityEngine.Purchasing.Security;

// Google-only constructor (recommended)
var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);

try
{
    var result = validator.Validate(order.Info.Receipt);
    foreach (IPurchaseReceipt receipt in result)
    {
        Debug.Log($"Valid receipt: {receipt.productID}, purchased: {receipt.purchaseDate}");
        if (receipt is GooglePlayReceipt googleReceipt)
            Debug.Log($"Purchase token: {googleReceipt.purchaseToken}");
    }
}
catch (IAPSecurityException ex)
{
    Debug.LogError($"Invalid receipt: {ex.Message}");
    // Do NOT grant the content
}
```

### Apple App Store (deprecated for local validation)

`CrossPlatformValidator` for Apple is **deprecated**. StoreKit 2 validates locally automatically. For server-side validation:

```csharp
var jwsRepresentation = order.Info.Apple?.jwsRepresentation;
// Send to your server for verification with Apple's App Store Server API
```

## Extended Service Events (NOT on StoreController)

These events are on the platform-specific extended services, NOT directly on `StoreController`. Always null-check before subscribing (`?.` does not work with `+=`).

```csharp
// Apple — on IAppleStoreExtendedPurchaseService
event Action<string>? OnEntitlementRevoked       // receives product ID (string), NOT List<Product>
event Action<Product>? OnPromotionalPurchaseIntercepted

// Google — on IGooglePlayStoreExtendedPurchaseService
event Action<DeferredPaymentUntilRenewalDateOrder>? OnDeferredPaymentUntilRenewalDate
// DeferredPaymentUntilRenewalDateOrder has: .CurrentOrder (Order), .SubscriptionOrdered (Product)
```

**Usage pattern** (cannot use `?.` with `+=`):
```csharp
if (store.AppleStoreExtendedPurchaseService != null)
    store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked += OnRevoked;
```

## AppleStoreExtendedProductService Key Members

```csharp
Dictionary<string, string> GetIntroductoryPriceDictionary()
Dictionary<string, string> GetProductDetails()
void SetStorePromotionOrder(List<Product> products)
void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible)
void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action<string> errorCallback)
void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback)
```

**CRITICAL:** `AppleProductMetadata` does NOT expose `introductoryPrice`, `introductoryPriceLocale`, `introductoryNumberOfPeriods`, or `subscriptionPeriod` as public properties. Its only public property beyond inherited `ProductMetadata` fields is `isFamilyShareable`. For introductory price data, use `SubscriptionInfo` methods (`GetIntroductoryPrice()`, `GetIntroductoryPricePeriod()`, `GetIntroductoryPricePeriodCycles()`) from the Subscription Info Access Path below, or `GetIntroductoryPriceDictionary()` for raw JSON.

## Subscription Info Access Path

Subscription info is on `IPurchasedProductInfo`, NOT on `CartItem`:

```csharp
// CORRECT — via order.Info.PurchasedProductInfo
var purchasedInfo = order.Info.PurchasedProductInfo?.FirstOrDefault(p => p.productId == productId);
bool isSubscribed = purchasedInfo?.subscriptionInfo?.IsSubscribed() == Result.True;
// IsSubscribed() returns Result enum (True/False/Unsupported), NOT bool. Use == Result.True for null-safe check.

// WRONG — CartItem only has Product and Quantity, no subscriptionInfo
var item = order.CartOrdered.Items().FirstOrDefault();
item.subscriptionInfo  // DOES NOT EXIST — will not compile
```

### SubscriptionInfo Methods

```csharp
// State queries (return Result enum: True | False | Unsupported)
Result IsSubscribed()
Result IsExpired()
Result IsCancelled()
Result IsFreeTrial()
Result IsAutoRenewing()
Result IsIntroductoryPricePeriod()

// Dates & durations
string   GetProductId()
DateTime GetPurchaseDate()
DateTime GetExpireDate()
DateTime GetCancelDate()
TimeSpan GetRemainingTime()
TimeSpan GetSubscriptionPeriod()
TimeSpan GetFreeTrialPeriod()
TimeSpan GetIntroductoryPricePeriod()
string   GetIntroductoryPrice()
long     GetIntroductoryPricePeriodCycles()
```

## Event Subscription Rules

**CRITICAL:** Always subscribe to BOTH success and failure events. Not listening to failure events generates runtime warnings.

| Call | Success event | Failure event (REQUIRED) |
|---|---|---|
| `FetchProducts()` | `OnProductsFetched` | `OnProductsFetchFailed` |
| `FetchPurchases()` | `OnPurchasesFetched` | `OnPurchasesFetchFailed` |
| `Connect()` | `OnStoreConnected` | `OnStoreDisconnected` |
| `PurchaseProduct()` | `OnPurchasePending` | `OnPurchaseFailed` |
| `CheckEntitlement()` | `OnCheckEntitlement` | — |

**Always subscribe to `OnPurchaseDeferred`** — fires for Ask-to-Buy (iOS) and Google Play deferred purchases. Not subscribing means silently dropping deferred purchases.

## Failure Description Property Names

Each type has public fields and read-only properties. Both compile, but prefer the PascalCase properties:

| Type | Field (lowercase) | Property (PascalCase) | NOT |
|---|---|---|---|
| `StoreConnectionFailureDescription` | `.message` | `.Message` | ~~`.reason`~~ |
| `ProductFetchFailed` | — | `.FailureReason`, `.FailedFetchProducts` | — |
| `FailedOrder` | — | `.FailureReason`, `.Details` | — |
| `PurchasesFetchFailureDescription` | `.message`, `.failureReason` | `.Message`, `.FailureReason` | — |

## CrossPlatformValidator

```csharp
// Google-only constructor (recommended — Apple local validation is a no-op under StoreKit 2)
new CrossPlatformValidator(byte[] googlePublicKey, string googleBundleId)

// Full constructor (legacy — Apple args are only needed for pre-StoreKit 2 builds)
new CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, string googleBundleId, string appleBundleId)

// Validate
IPurchaseReceipt[] Validate(string unityIAPReceipt)  // throws IAPSecurityException
```

**WARNING:** `CrossPlatformValidator` for Apple App Store is **deprecated**. `AppleTangle.Data()` is also deprecated. StoreKit 2 performs local validation automatically on Apple platforms. `CrossPlatformValidator` still works for Google Play validation.

Supported stores (still active): `"GooglePlay"` | Deprecated: `"AppleAppStore"`, `"MacAppStore"`

## Interface Hierarchy

```
StoreController implements:
  IStoreService      - Connect(), Apple/Google service extensions
  IProductService    - FetchProducts(), GetProducts(), GetProductById()
  IPurchaseService   - PurchaseProduct(), ConfirmPurchase(), FetchPurchases(), Apple/Google purchase extensions
```

## IRunCommand Template: Fetch Products

```csharp
using UnityEngine;
using UnityEngine.Purchasing;
using System.Collections.Generic;

namespace Unity.AI.Assistant.Agent.Dynamic.Extension.Editor
{
    internal class CommandScript : IRunCommand
    {
        public string Title => "Fetch IAP products";
        public string Description => "Connects to the store and fetches product information.";
        public async void Execute(ExecutionResult result)
        {
            var store = UnityIAPServices.StoreController();

            // Subscribe to events BEFORE Connect
            store.OnStoreConnected += () =>
            {
                var products = new List<ProductDefinition>
                {
                    new ProductDefinition("com.mygame.coins100", ProductType.Consumable),
                    new ProductDefinition("com.mygame.removeads", ProductType.NonConsumable)
                };

                store.OnProductsFetched += (fetched) =>
                {
                    foreach (var p in fetched)
                        result.Log($"{p.definition.id}: {p.metadata.localizedPriceString}");
                };
                store.OnProductsFetchFailed += (failure) => result.LogError($"Product fetch failed: {failure.FailureReason}");

                store.FetchProducts(products);
            };
            store.OnStoreDisconnected += (failure) => result.LogError($"Store disconnected: {failure.Message}");

            await store.Connect();
        }
    }
}
```
