# Pre-Check: Project Scan and Path Routing

## Table of Contents

- [Ambiguous Intent](#ambiguous-intent)
- [IAP Plus (Third-Party Payment Provider)](#iap-plus-third-party-payment-provider)
- [Scan Order (standard IAP paths)](#scan-order-standard-iap-paths)
- [Routing Summary](#routing-summary)

Run this scan before doing anything else. Do not read any other reference file or make any changes until routing is resolved.

## Ambiguous Intent

If the user's prompt does not clearly indicate which billing backend they want — for example:

- "Add a StoreController for me"
- "Add a purchase manager"
- "Set up IAP"
- "Help me add in-app purchases"

**Stop and ask before scanning or routing:**

> "Would you like to implement purchasing using:
> - **Apple App Store / Google Play** (standard platform billing), or
> - **IAP Plus** (third-party payment provider such as Stripe or Coda via Unity Cloud)?"

Do not assume a default. Route only after the user confirms their intent.

---

## IAP Plus (Third-Party Payment Provider)

If the user explicitly asks for IAP Plus, Stripe/Coda payment integration, or a third-party payment provider, **do not follow the standard scan order below**. Instead:

- If the project has IAP v4, native Google Billing, or a third-party IAP package → resolve those blockers first using the standard scan order, then return here.
- If the project has no IAP, or has `com.unity.purchasing` **v5.4+** → route to [references/path-implement-iap-plus.md](path-implement-iap-plus.md).
- If the project has `com.unity.purchasing` **v5.0–5.3** → inform the user that IAP Plus requires v5.4+, instruct them to upgrade via Package Manager, and continue once the upgrade is confirmed.

---

## Scan Order (standard IAP paths)

Run scans in this exact order. The first match wins — stop scanning and route immediately.

---

### 1 — Third-Party IAP Package Check (highest priority)

Search `Packages/manifest.json` and `Packages/packages-lock.json` for:

```
com\.revenuecat|com\.adapty|com\.voxelbusters\.essentialkit|com\.unipay
```

**If found → Stop.**

Inform the user:

> "This project uses [package name], which is a third-party IAP package. This skill only supports Unity IAP 5 (`com.unity.purchasing`). Migrating from third-party IAP packages is not supported. No changes will be made."

Do not proceed with any other path.

---

### 2 — Native Google Billing Check

Search the following locations:

- `Assets/**/*.cs` for: `AndroidJavaObject|AndroidJavaClass|BillingClient|BillingManager|GoogleBilling|BillingBridge`
- `Assets/Plugins/Android/**/*.java`, `*.kt` for: `com\.android\.billingclient`
- `mainTemplate.gradle`, `launcherTemplate.gradle`, `baseProjectTemplate.gradle` for: `com\.android\.billingclient:billing`

**If found → only one path is valid:**

> Route to [references/path-convert-native-google-billing.md](path-convert-native-google-billing.md)

Inform the user that native Google Billing was detected and that the conversion path will be used. Do not offer any other path.

---

### 3 — Unity IAP v4 Check

Search `Packages/manifest.json` for `com.unity.purchasing` with a version matching `4\.`.

Also search `Assets/**/*.cs` for legacy v4 API patterns:

```
IStoreListener|UnityPurchasing\.Initialize|ConfigurationBuilder
```

**If found → only one path is valid:**

> Route to [references/migration-v4-to-v5.md](migration-v4-to-v5.md)

Inform the user that Unity IAP v4 was detected and that the v4→v5 migration path will be used. Do not offer any other path.

---

### 4 — No IAP Detected

If none of the above matched:

Search `Packages/manifest.json` for `com.unity.purchasing`. If absent or if no IAP signals were found in any scan:

**→ only one path is valid:**

> Route to [references/path-add-iap-to-new-project.md](path-add-iap-to-new-project.md)

---

## Routing Summary

| What is detected | Valid path |
|---|---|
| RevenueCat / Adapty / Essential Kit / UniPay | **Stop — unsupported** |
| Native Google BillingClient | Convert native Google Billing → IAP 5 |
| `com.unity.purchasing` v4 or v4 API patterns | Migrate IAP v4 → v5 |
| No IAP detected | Add Unity IAP 5 to new project |
| User explicitly requests IAP Plus / payment provider | See IAP Plus section above |

---

## Also check: Codeless catalog presence

Regardless of which route above wins, check whether `Assets/Resources/IAPProductCatalog.json` exists. This is the **Codeless IAP** catalog and is independent of the routing decision — but if it is present and `enableCodelessAutoInitialization` is true, adding a scripted `StoreController` creates a race condition with `CodelessIAPStoreListener`.

Surface the catalog's presence and auto-init flag to the user before generating scripted IAP code. See [codeless-catalog.md](codeless-catalog.md) for the race condition details and the three mitigation options.
