# Pre-Check: Project Scan and Path Routing

## Table of Contents

- [Ambiguous Intent](#ambiguous-intent)
- [IAP D2C Capabilities (Direct-to-Customer, Third-Party Payment Provider)](#iap-d2c-capabilities-direct-to-customer-third-party-payment-provider)
- [Scan Order (standard IAP paths)](#scan-order-standard-iap-paths)
- [Routing Summary](#routing-summary)

  Scan steps: [1 — Third-Party IAP Package Check](#1--third-party-iap-package-check-highest-priority) · [2 — Native Google Billing](#2--native-google-billing-check) · [2b — Native iOS StoreKit](#2b--native-ios-storekit-check) · [3 — Unity IAP v4](#3--unity-iap-v4-check) · [4 — Unity IAP v5 Already Installed](#4--unity-iap-v5-already-installed) · [5 — No IAP Detected](#5--no-iap-detected)
  Third-party sub-checks: [1a Essential Kit](#1a--essential-kit-conversion-supported) · [1b UniPay](#1b--unipay-assessment-path) · [1c RevenueCat](#1c--revenuecat-assessment-path) · [1d Adapty](#1d--adapty-assessment-path)

Run this scan before doing anything else. Do not read any other reference file or make any changes until routing is resolved.

## Ambiguous Intent

If the user's prompt does not clearly indicate which billing backend they want — for example:

- "Add a StoreController for me"
- "Add a purchase manager"
- "Set up IAP"
- "Help me add in-app purchases"
- "Upgrade to latest IAP"

**Stop and ask before scanning or routing:**

> "Would you like to implement purchasing using:
> - **Apple App Store / Google Play** (standard platform billing), or
> - **IAP D2C Capabilities** (third-party payment provider such as Stripe or Coda via Unity Cloud)?"

Do not assume a default. Route only after the user confirms their intent.

---

## IAP D2C Capabilities (Direct-to-Customer, Third-Party Payment Provider)

If the user explicitly asks for IAP D2C Capabilities, Stripe/Coda payment integration, or a third-party payment provider, **do not follow the standard scan order below**. Instead:

- If the project has IAP v4, native Google Billing, or a third-party IAP package → resolve those blockers first using the standard scan order, then return here.
- If the project has no IAP, or has `com.unity.purchasing` **v5.4+** → route to [path-implement-iap-d2c.md](path-implement-iap-d2c.md).
- If the project has `com.unity.purchasing` **v5.0–5.3** → inform the user that IAP D2C Capabilities require v5.4+, instruct them to upgrade via Package Manager, and continue once the upgrade is confirmed.

---

## Scan Order (standard IAP paths)

Run scans in this exact order. The first match wins — stop scanning and route immediately.

---

### 1 — Third-Party IAP Package Check (highest priority)

#### 1a — Essential Kit (conversion supported)

Search `Packages/manifest.json` and `Packages/packages-lock.json` for `com.voxelbusters.essentialkit`.

**If found → only one path is valid:**

> Route to [convert-essentialkit.md](convert-essentialkit.md)

Inform the user that Essential Kit was detected and that the conversion path will be used. Do not offer any other path.

---

#### 1b — UniPay (assessment path)

Search `Packages/manifest.json`, `Packages/packages-lock.json`, and `Assets/Plugins/` for UniPay signals:

```
com\.flobuk\.unipay|UniPay|Assets/Plugins/UniPay|Assets/Plugins/SIS
```

**If found → only one path is valid:**

> Route to [convert-unipay.md](convert-unipay.md)

Inform the user that UniPay was detected and that the assessment path will be used. Do not offer any other path.

---

#### 1c — RevenueCat (assessment path)

Search `Packages/manifest.json` and `Packages/packages-lock.json` for `com.revenuecat.purchases-unity`.

**If found → only one path is valid:**

> Route to [convert-revenuecat.md](convert-revenuecat.md)

Inform the user that RevenueCat was detected and that the assessment path will be used. Do not offer any other path.

---

#### 1d — Adapty (assessment path)

Search `Packages/manifest.json`, `Packages/packages-lock.json`, and `Assets/` for Adapty signals:

```
com\.adapty|Assets/Adapty|AdaptySDK
```

**If found → only one path is valid:**

> Route to [convert-adapty.md](convert-adapty.md)

Inform the user that Adapty was detected and that the assessment path will be used. Do not offer any other path.

---

### 2 — Native Google Billing Check

Search the following locations:

- `Assets/**/*.cs` for: `AndroidJavaObject|AndroidJavaClass|BillingClient|BillingManager|GoogleBilling|BillingBridge`
- `Assets/Plugins/Android/**/*.java`, `*.kt` for: `com\.android\.billingclient`
- `mainTemplate.gradle`, `launcherTemplate.gradle`, `baseProjectTemplate.gradle` for: `com\.android\.billingclient:billing`

**If found → only one path is valid:**

> Route to [path-convert-native-google-billing.md](path-convert-native-google-billing.md)

Inform the user that native Google Billing was detected and that the conversion path will be used. If `com.unity.purchasing` is absent or not the latest stable version, instruct the user to install or upgrade to the latest stable version via Package Manager before proceeding.

Do not offer any other path.

---

### 2b — Native iOS StoreKit Check

Search the following for native StoreKit signals:

- `Assets/Plugins/iOS/**/*.m`, `*.mm`, `*.h`, `*.swift` for: `SKProductsRequest|SKPaymentQueue|SKPaymentTransaction|SKPaymentTransactionObserver|Product\.products|Transaction\.currentEntitlements`
- `Assets/**/*.cs` for: `DllImport.*__Internal` combined with IAP-related method names, or `UnitySendMessage` calls associated with StoreKit

**If found → only one path is valid:**

> Route to [path-convert-native-storekit.md](path-convert-native-storekit.md)

Inform the user that a native iOS StoreKit plugin was detected and that the conversion path will be used. If `com.unity.purchasing` is absent or not the latest stable version, instruct the user to install or upgrade via Package Manager before proceeding.

Do not offer any other path.

---

### 3 — Unity IAP v4 Check

Search `Packages/manifest.json` for `com.unity.purchasing` with a version matching `4\.`.

Also search `Assets/**/*.cs` for legacy v4 API patterns:

```
IStoreListener|UnityPurchasing\.Initialize|ConfigurationBuilder
```

**If found → only one path is valid:**

> Route to [migration-v4-to-v5.md](migration-v4-to-v5.md)

Inform the user that Unity IAP v4 was detected and that the v4→v5 migration path will be used. Do not offer any other path.

---

### 4 — Unity IAP v5 Already Installed

If none of the above matched, search `Packages/manifest.json` for `com.unity.purchasing` with a version matching `5\.`:

**If found → inform the user:**

> "This project already has Unity IAP v5 installed. You can extend the existing setup — for example, add new products, add platform-specific features, or integrate IAP D2C Capabilities. What would you like to do?"

Then route based on the user's answer. Do not automatically route to the new-project path — the project already has IAP configured.

---

### 5 — No IAP Detected

If none of the above matched and `com.unity.purchasing` is absent from `manifest.json`:

**→ only one path is valid:**

> Route to [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md)

---

## Routing Summary

| What is detected | Valid path |
|---|---|
| Essential Kit (`com.voxelbusters.essentialkit`) | Convert Essential Kit billing → IAP 5 — see convert-essentialkit.md |
| UniPay (FLOBUK) | Assessment — see convert-unipay.md |
| RevenueCat | Assessment — see convert-revenuecat.md |
| Adapty | Assessment — see convert-adapty.md |
| Native Google BillingClient | Convert native Google Billing → IAP 5 — see path-convert-native-google-billing.md |
| Native iOS StoreKit plugin (`SKPaymentQueue` / `DllImport("__Internal")`) | Convert native StoreKit → IAP 5 — see path-convert-native-storekit.md |
| `com.unity.purchasing` v4 or v4 API patterns | Migrate IAP v4 → v5 |
| `com.unity.purchasing` v5 already installed | Ask user what to extend — do not auto-route |
| No IAP detected | Add Unity IAP 5 to new project |
| User explicitly requests IAP D2C Capabilities / payment provider | See IAP D2C Capabilities section above |

---

## Also check: Codeless catalog presence

Regardless of which route above wins, check whether `Assets/Resources/IAPProductCatalog.json` exists. This is the **Codeless IAP** catalog and is independent of the routing decision — but if it is present and `enableCodelessAutoInitialization` is true, adding a scripted `StoreController` creates a race condition with `CodelessIAPStoreListener`.

Surface the catalog's presence and auto-init flag to the user before generating scripted IAP code. See [codeless-catalog.md](codeless-catalog.md) for the race condition details and the three mitigation options.
