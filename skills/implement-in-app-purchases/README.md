# Unity In-App Purchases Skill

This skill helps you implement, configure, debug, and migrate Unity In-App Purchases (IAP) using `com.unity.purchasing` v5. It covers standard Apple App Store / Google Play billing, IAP D2C Capabilities (Direct-to-Customer — Stripe/Coda via Unity Cloud), and conversion from a wide range of third-party and native billing implementations.

---

## What This Skill Covers

| Path | When to use |
|---|---|
| **Add IAP to a new project** | No existing IAP — start from scratch with Unity IAP 5 |
| **Migrate v4 → v5** | Project uses `IStoreListener`, `UnityPurchasing.Initialize`, or `ConfigurationBuilder` |
| **Convert native Google BillingClient** | Project calls Android BillingClient via `AndroidJavaObject` / JNI bridge |
| **Convert native iOS StoreKit** | Project uses a custom ObjC/Swift StoreKit plugin bridged via `DllImport("__Internal")` |
| **Convert Essential Kit billing** | Project uses VoxelBusters Essential Kit for in-app purchases (without losing other features) |
| **Assess/Convert UniPay (FLOBUK)** | Project uses UniPay — determines whether migration is needed or possible |
| **Assess/Convert RevenueCat** | Project uses RevenueCat — evaluate/implement observer mode to work with Unity IAP 5 |
| **Assess/Convert Adapty** | Project uses Adapty — evaluate/implement observer mode to work with Unity IAP 5 |
| **Implement IAP D2C Capabilities** | Add third-party payment provider (Stripe or Coda) via Unity Cloud |

The skill always scans the project first and routes to the correct path automatically. You can also specify a path manually if you know exactly which one you need.

---

## Example Prompts

```
Add in-app purchases code to my game. I have a 100-coin pack and a Remove Ads unlock.
```
```
Migrate my existing IAP code in the project from Unity IAP v4 to v5.
```
```
My project uses AndroidJavaObject to call Google Play BillingClient. Convert it to Unity IAP.
```
```
I have a custom ObjC StoreKit plugin with SKPaymentQueue. Replace it with Unity IAP.
```
```
Disable Essential Kit billing and replace with Unity IAP 5. Keep other Essential Kit features working as before.
```
```
Can my RevenueCat project implement app store purchases through Unity's IAP v5?
```
```
My project uses Adapty. What features will we lose if we use Unity IAP instead? Give me a detailed report and do not make any changes now.
```
```
Add Stripe payment support via Unity IAP D2C Capabilities.
```
```
Add a web checkout option for my players using Unity's IAP D2C Capabilities with Coda.
```

---

## Path Details, Limitations, and Best Practices

### Add IAP to a New Project

**What it does:** Installs Unity IAP 5, creates an `IAPManager`, wires up the two-step purchase flow (pending → confirm), integrates with your existing save system, and connects to your shop UI.

**Limitations:**
- The skill cannot create App Store Connect or Google Play Console product entries — you must do that manually.
- If you use local Google Play receipt validation, you must run the Receipt Validation Obfuscator manually in the Unity Editor (**Services > In-App Purchasing > Receipt Validation Obfuscator**).
- Apple local receipt validation is a no-op under StoreKit 2. Use server-side JWS validation for Apple.

**Best practices:**
- Define all products in one authoritative catalog (a `ScriptableObject` or constants class) — not scattered across button click handlers.
- Always call `ConfirmPurchase` **after** saving the granted reward. An unconfirmed purchase re-delivers safely; a confirmed-but-unsaved one loses the reward permanently.
- Subscribe to all events **before** calling `Connect()` — pending purchases from a previous session may fire immediately on reconnect.
- Add a "Restore Purchases" button for any project with NonConsumable or Subscription products — required on iOS.

---

### Migrate v4 → v5

**What it does:** Replaces the listener-based `IStoreListener` / `UnityPurchasing.Initialize` / `ConfigurationBuilder` pattern with the event-driven `StoreController` pattern.

**Limitations:**
- `product.receipt` and `product.hasReceipt` are removed. The skill migrates ownership checks to `store.CheckEntitlement(product)` — verify your entitlement logic after migration.
- Apple local validation via `CrossPlatformValidator` still compiles but is a no-op under StoreKit 2. If your project validates Apple receipts locally, this validation silently stops working after migration.
- Developer payload (the third `Purchase()` argument) is removed. If your backend uses it, a backend change is required.
- `SubscriptionManager` is replaced — subscription info is now on `order.Info.PurchasedProductInfo`, not on `CartItem`.

**Best practices:**
- Migrate one system at a time: get initialization working first, then purchase flow, then restore.
- Run the migration in a branch. The skill uses `#if` guards where needed, but test thoroughly before removing the old code.
- After migration, recheck all `OnPurchaseConfirmed` handlers — the event now receives `Order` (base type) and you must pattern-match `ConfirmedOrder` vs `FailedOrder`.

---

### Convert Native Google BillingClient

**What it does:** Converts C# → JNI → BillingClient bridge code to Unity IAP 5, preserves the public billing API via a compatibility facade, and wraps old native code in `#if !USE_UNITY_IAP_V5` for easy rollback.

**Limitations:**
- **Multiple subscription base plans / offer tokens** (`basePlanId`, `offerToken`, `offerId`) are a hard blocker — Unity IAP does not expose offer-level selection. You must either simplify your Play Console subscription setup to one base plan per product, or stay on native billing.
- **Personalized price disclosure** (`setIsPersonalizedPrice`) and **alternative billing / external offers** are hard blockers.
- **Multi-quantity purchases** (quantity > 1 per transaction) are not supported in Unity IAP.
- A mixed native BillingClient + Unity IAP architecture is not supported — both compete for the same `PurchasesUpdatedListener` slot. The skill will not generate a mixed setup.
- The receipt format changes: the backend receives `order.Info.Receipt` (a JSON object containing `purchaseToken`, `orderId`, and `signature`) instead of raw fields. If your backend parses these fields individually, a backend update is required.

**Best practices:**
- Run the blocker scan and read the migration report before making any changes. Hard blockers require a decision before code is written.
- Keep the Gradle billing dependency in place until migration is validated — Unity IAP brings its own BillingClient, but removing the old dependency prematurely can break the build in unexpected ways.
- Use the `#if USE_UNITY_IAP_V5` / `#if !USE_UNITY_IAP_V5` guards throughout. The rollback path (remove the define) must work cleanly before you ship.
- Test on a physical Android device with a real sandbox account — the BillingClient behavior in the Unity Editor and on emulators is not representative of production.

---

### Convert Native iOS StoreKit

**What it does:** Converts a custom Objective-C or Swift StoreKit plugin (bridged via `DllImport("__Internal")` and `UnitySendMessage`) to Unity IAP 5, preserving the C# game-facing API via a compatibility facade.

**Limitations:**
- **Receipt format is a breaking backend change.** Unity IAP uses per-transaction JWS (StoreKit 2 style) rather than the SK1 base64 app receipt bundle. If your backend validates the receipt bundle against Apple's `/verifyReceipt` endpoint, it must migrate to Apple's App Store Server API or JWS validation before you can ship.
- **Promotional offer signing** (`SKPaymentDiscount` / SK2 signed offers) is a hard blocker — Unity IAP has no equivalent for server-signed promotional offers.
- **Win-back offers** (StoreKit 2) are not supported in Unity IAP 5.4.
- **`SKStorefront`** (region detection via storefront change observer) is not exposed in Unity IAP.
- **`SKReceiptRefreshRequest`** has no direct equivalent — `store.FetchPurchases()` covers the purchase re-delivery use case but not manual receipt refresh.
- ObjC/Swift plugin files cannot use C# `#if` defines. The skill guards only the C# call sites — the native files remain in `Assets/Plugins/iOS/` and compile unconditionally.
- A mixed native StoreKit + Unity IAP architecture is not supported — both register as `SKPaymentQueue` observers and only one reliably receives callbacks.

**Best practices:**
- Audit the backend receipt validation endpoint before starting. If it calls `/verifyReceipt`, plan the backend migration in parallel with the client migration.
- If the plugin intercepts App Store promotional purchases (`shouldAddStorePayment`), wire up `OnPromotionalPurchaseIntercepted` and `ContinuePromotionalPurchases()` before removing the native interceptor.
- Test Ask-to-Buy explicitly — the deferred → pending two-stage flow behaves differently than a native `PURCHASING` → `PURCHASED` transition.
- Validate the migration on TestFlight, not just in the Unity Editor or Simulator. StoreKit behavior differs between Editor sandbox, Simulator, and TestFlight builds.

---

### Convert Essential Kit Billing

**What it does:** Disables the Essential Kit Billing service flag in `EssentialKitSettings.asset`, removes the conflicting `com.android.billingclient` Gradle dependency, and implements Unity IAP 5 using the product catalog extracted from the Essential Kit settings.

**Limitations:**
- Essential Kit C# source files are **not deleted**. The Billing service is disabled via the settings flag — all EK code remains and compiles. Do not expect a clean removal.
- Only the Billing service is affected. All other Essential Kit services (Notification, GameServices, etc.) are left completely untouched.
- **Product IDs must not change.** The same IDs used in Essential Kit must be carried over to Unity IAP to preserve store history.
- If your project uses **server-side receipt validation**, the receipt format changes from `transaction.RawData` (Android) and `transaction.Receipt` (iOS JWS) to `order.Info.Receipt` and `order.Info.Apple?.jwsRepresentation`. Document the backend change required.
- Subscriptions are fully supported, but the restore path changes — EK's `OnRestorePurchasesComplete` maps to both `store.OnPurchasesFetched` and the `RestoreTransactions` callback in Unity IAP.

**Best practices:**
- Run `Assets > External Dependency Manager > Android Resolver > Force Resolve` after removing the Gradle billing dependency — do not skip this step.
- Verify the Essential Kit Billing service is visually disabled in **Window > Voxel Busters > Essential Kit > Open Settings → Services** after the settings file edit.
- Confirm all product IDs in App Store Connect and Play Console match the Unity IAP catalog exactly before testing.

---

### Assess UniPay (FLOBUK)

**What it does:** This path does **not** perform a conversion. It assesses whether migration is needed and routes to one of three outcomes: unsupported platform (stop), upgrade required (stop), or no action needed (UniPay already wraps Unity IAP 5).

**Limitations:**
- UniPay's Steam, Meta Quest, PayPal, and Facebook Instant Games integrations have **no Unity IAP equivalent**. If your project targets any of these platforms, there is no migration path — keep UniPay.
- If `com.unity.purchasing` is below v5.4, IAP D2C Capabilities (Stripe/Coda) are not available — an upgrade to the latest stable v5.4+ is needed before adding D2C support.
- The skill does not perform a "remove UniPay" conversion — that is a manual refactor scoped to what features you are replacing.

**Best practices:**
- If the assessment concludes "no action needed," there is no code to write. Work within UniPay's API for new products or purchase logic changes.
- If you want to remove UniPay entirely and use Unity IAP directly, ask the skill explicitly and describe which UniPay features you are replacing — it will assess feasibility and scope.

---

### Assess/Convert RevenueCat

**What it does:** Evaluates whether the project can switch to Unity IAP 5 for handling purchases. Produces one of three outcomes: already in observer mode (no action), blockers detected (report + two choices), or no blockers (two conversion paths: observer mode or full removal).

**Limitations:**
- **Amazon Appstore** is a hard blocker — Unity IAP 5 has removed Amazon support. RevenueCat observer mode also does not work reliably on Amazon builds. If your project targets Amazon, conversion is not viable.
- **RevenueCat Offerings / remote paywalls** have no Unity IAP equivalent — all products must be defined in code or a local catalog.
- **RevenueCat A/B testing (Experiments)** has no Unity IAP equivalent.
- **Cross-platform entitlement sync** (a user who buys on iOS retains access on Android) has no Unity IAP equivalent without a custom backend.
- **RevenueCat webhook events** (subscription renewals, cancellations, billing issues) are not delivered by Unity IAP — you would need to build your own subscription event infrastructure.
- In **observer mode**, `SyncPurchases()` must be called after every Unity IAP confirmed purchase. Missing this call means RevenueCat does not validate the receipt and `CustomerInfo` is not updated.

**Best practices:**
- Run the full feature check before deciding. RevenueCat's value often comes from features that are not obvious in the codebase (e.g., webhooks configured server-side).
- If the project has a marketing team managing paywall copy or pricing remotely via RevenueCat Offerings, a full removal will require significant UI work to replace that capability.
- Observer mode is the lower-risk path — it preserves RevenueCat's server-side validation and subscription lifecycle tracking while Unity IAP handles the native purchase flow.

---

### Assess/Convert Adapty

**What it does:** Evaluates whether the project can switch to Unity IAP 5 for app store purchase handling. Produces one of three outcomes: already in observer mode (no action), blockers detected (report + two choices), or no blockers (observer mode or full removal).

**Limitations:**
- **Adapty Paywall Builder** has no Unity IAP equivalent — and it is also **unavailable in Adapty's own Observer Mode**. Switching to observer mode loses Paywall Builder regardless of whether Unity IAP is involved.
- **Adapty A/B testing** has no Unity IAP equivalent. In observer mode, A/B testing is possible but requires significant manual instrumentation.
- **Cross-platform entitlement sync** has no Unity IAP equivalent without a custom backend.
- In **observer mode**, `Adapty.ReportTransaction()` must be called after every Unity IAP confirmed purchase. Missing this call means Adapty does not validate the receipt server-side.

**Best practices:**
- If your marketing team uses Adapty's Paywall Builder to update paywall layouts without app releases, note that this capability is lost in observer mode. Make sure all stakeholders understand this before proceeding.
- Full removal requires replacing any `AdaptyUI` / `PaywallView` shop UI with custom Unity UI before Adapty can be removed. Budget time for this work.
- Observer mode is the lower-risk path and preserves Adapty's webhook delivery and analytics integrations.

---

### Implement IAP D2C Capabilities

**What it does:** Adds Direct-to-Customer (D2C) third-party payment provider support (Stripe or Coda) via Unity Cloud, including remote catalog setup, deep link configuration, the built-in payment options picker UI, Apple/Google external purchase compliance tools, and entitlement delivery guidance.

**Limitations:**
- Requires **Unity IAP v5.4+**, **Unity Editor 2022.3+**, `com.unity.services.authentication` **v3.7.1+**, and `com.unity.services.core` **v1.18.0+**. All must be satisfied before any code is written.
- **Subscriptions are not supported** by IAP D2C Capabilities in v5.4. Only Consumable and NonConsumable products can be used.
- Requires a **Stripe or Coda account** connected in the Unity Cloud IAP dashboard, and Unity must enable D2C for your organization — contact your Unity Client Partner if it is not yet enabled.
- **Routing rules** must be configured in the Unity Dashboard before any player is offered a D2C payment option. Without a routing rule, no provider is offered even if a provider account is connected.
- **External web payments via Stripe/Coda are permitted in select regions only.** Apple and Google have their own program eligibility requirements. The developer is responsible for determining eligibility and meeting disclosure requirements — the skill does not perform compliance on your behalf.
- **Anonymous sign-in** must not be used as the authentication method. If a player's session token is lost (reinstall, app data clear), purchase history tied to an anonymous identity becomes unrecoverable.
- The **receipt format is different from standard IAP 5** — D2C purchases go through Unity Cloud, not the device's native store, so `order.Info.Apple` and `order.Info.Receipt` behave differently for D2C orders.

**Best practices:**
- Set up **routing rules** in the Unity Dashboard before testing. Without them, `GetEligiblePaymentProviders()` returns an empty list and the purchase UI never appears — this is a common "nothing happens" issue during initial integration.
- Use a **proxy HTML page** for the Success Redirect URL rather than a direct app-scheme URL. On some Android and iOS devices, direct app-scheme redirects from the payment provider domain are silently dropped. Host the proxy on a stable HTTPS domain you control.
- Use **`ShowPurchaseOption(catalogListingId)`** as the primary purchase entry point — it shows the built-in picker UI and handles provider selection automatically. Only fall back to `PurchaseProduct` directly when `GetEligiblePaymentProviders()` returns an empty `Providers` list.
- Configure a **Cancel Redirect URL** in the payment provider dashboard — without it, the checkout page has no "Back" button and players who change their mind are stuck in the browser.
- The SDK remembers the **last used payment provider per player per device** (provider memory). This is expected behavior, not a bug. Clear app data to reset it during testing.
- Deploy the **Deployment package** (`com.unity.services.deployment`) early — it is required to push `.ucat` product definitions to the Remote Catalog. Without it, the catalog cannot be deployed and `FetchRemoteCatalog()` returns no products.

---

## General Best Practices

- **Always let the skill scan first.** The pre-check (`pre-check.md`) detects third-party packages, native billing code, and existing Unity IAP versions before routing. Skipping it leads to incompatible changes.
- **Read the migration report before approving any code changes.** Every conversion path produces a report covering what will change, what blockers were found, and what manual steps remain. Review it before proceeding.
- **Never confirm a purchase before saving the reward.** An unconfirmed purchase re-delivers safely. A confirmed-but-unsaved one is gone permanently.
- **Subscribe to all failure events.** `OnProductsFetchFailed`, `OnPurchasesFetchFailed`, `OnStoreDisconnected` — not subscribing generates runtime warnings and leaves failures silently unhandled.
- **Subscribe to `OnPurchaseDeferred`.** Ask-to-Buy (iOS) and Google Play deferred purchases fire this event. Not subscribing silently drops them.
- **Use `#if USE_UNITY_IAP_V5` guards** for all migration work. The rollback path (remove the define from Player Settings) must work cleanly before shipping.
- **Test with real sandbox accounts on real devices.** Editor sandbox and emulators do not reproduce all edge cases — particularly pending purchases, deferred flows, and restore behavior.
