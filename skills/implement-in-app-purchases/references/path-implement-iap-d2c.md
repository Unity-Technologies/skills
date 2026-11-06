# Implement Unity IAP D2C Capabilities (Third-Party Payment Provider)

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Prerequisites (verify before any changes)](#prerequisites-verify-before-any-changes)
- [Step 1 — Project Scan](#step-1--project-scan)
- [Step 2 — Product Discovery](#step-2--product-discovery)
- [Step 3 — Deploy Catalog to Remote Catalog](#step-3--deploy-catalog-to-remote-catalog)
- [Step 4 — IAPManager with PaymentProvider](#step-4--iapmanager-with-paymentprovider)
- [Step 5 — Remote Catalog](#step-5--remote-catalog)
- [Step 6 — Deep Link Setup](#step-6--deep-link-setup)
- [Step 7 — Purchase Handling](#step-7--purchase-handling)
- [Step 8 — Product Type Behavior](#step-8--product-type-behavior)
- [Step 9 — Cloud Save Integration](#step-9--cloud-save-integration)
- [Step 10 — Verification Report](#step-10--verification-report)
- [Step 11 — Manual Steps (always include in report)](#step-11--manual-steps-always-include-in-report)

Use this reference when the user explicitly asks to add IAP D2C Capabilities (third-party payment provider such as Stripe or Coda) to a project. This path is valid only when the project has no IAP, or already has `com.unity.purchasing` v5.x installed.

**Not valid when:** the project has IAP v4, native Google Billing, or a third-party IAP package — resolve those first via the pre-check routing before returning to this path.

## Trigger Phrases

- "Add IAP D2C Capabilities"
- "Integrate Stripe / Coda payments"
- "Add third-party payment provider"
- "Set up web-based IAP checkout"
- "Add Unity payment provider"

## Prerequisites (verify before any changes)

1. Read the Prerequisites section of [Integrate D2C payment providers workflow](https://docs.unity.com/en-us/iap/payment-providers/workflow.md). It lists the minimum Editor version, the required package versions (`com.unity.purchasing`, Authentication, Core, Deployment) and the Unity Cloud setup.
2. Check each one against the project: the Editor version in `ProjectSettings/ProjectVersion.txt`, and the package versions in `Packages/packages-lock.json`.
3. If anything is missing, stop and list the missing items for the user before writing any code, because D2C fails at runtime, not at compile time, when a prerequisite is missing.

## Step 1 — Project Scan

Search the project for existing IAP and economy signals before writing any code.

### 1a — Existing IAP detection

Search `Packages/manifest.json` for `com.unity.purchasing`:
- If **v5.4+** found → proceed, IAP D2C Capabilities will be added alongside.
- If **v5.0–5.3** found → inform the user that v5.4+ is required, instruct them to upgrade `com.unity.purchasing` to v5.4+ via Package Manager, and continue once the upgrade is confirmed.
- If **absent** → proceed, IAP D2C Capabilities will be added fresh.

### 1b — Unity Authentication detection

Search `Packages/manifest.json` for `com.unity.services.authentication`. If absent, inform the user it must be installed — IAP D2C Capabilities will not initialize without a signed-in player.

### 1c — Inventory and economy scan

Search `Assets/**/*.cs` for:
```
\bcoins\b|\bgems\b|\blives\b|\binventory\b|\bcurrency\b
PlayerData|SaveAsync|CloudSave|SaveDataAsync|SaveGame
```

### 1d — Shop UI scan

Search `Assets/**/*.unity`, `*.prefab` for shop-related GameObjects and scripts:
```
Shop|Store|Purchase|Buy|IAP|Product
```

## Step 2 — Product Discovery

### If existing `.ucat` files are found

Locate `Assets/**/*.ucat` and `Assets/**/*.catalog.csv` files. Use them as-is (see [Catalog file formats](#catalog-file-formats)).

### If existing IAP 5 `ProductDefinition` objects are found

These are local store products — IAP D2C Capabilities uses a Remote Catalog instead. Inform the user that existing local product definitions need to be re-created as `.ucat` files deployed to Unity Cloud, and that product IDs (`uSKU`) should match the existing IDs to preserve store history.

### If no product definitions exist

**Stop and ask:**

> "Please provide the first IAP product ID and type — for example `com.mygame.coins100` as Consumable — and tell me which inventory field, currency, or item should be credited after purchase."

- Default to Consumable if type is not provided, but state the assumption explicitly.
- **If any Subscription products are provided or detected**, exclude them from the implementation and warn the user:

  > "The following products were skipped because subscriptions are not supported by IAP D2C Capabilities in v5.4+: [list]. Only Consumable and NonConsumable products will be implemented."

  Continue with the remaining Consumable and NonConsumable products. If no supported products remain after exclusion, stop and inform the user.

### Catalog file formats

D2C products are defined as `.ucat` files (one product per file) or a single `.catalog.csv` file under `Assets/`, not in code. Before creating or editing either format, read [Create your Remote Catalog](https://docs.unity.com/en-us/iap/payment-providers/configure-remote-catalog.md) for the fields, the CSV columns and the validation rules, and reread it if the Deployment window reports a validation error. Prefer one `.ucat` per product for a handful of products, and `.catalog.csv` when the user already keeps products in a spreadsheet.

Gotchas the agent must respect:

- **The `.ucat` filename drives identity.** `CatalogListingId` is always `"catalog/" + <filename-stem>` and is never written in the JSON. `uSKU` defaults to the filename stem, so omit `uSKU` from the JSON unless the store SKU must differ from the filename. Name files after the product ID so `coins_100.ucat` gives `uSKU = "coins_100"` and `CatalogListingId = "catalog/coins_100"`.
- **In `.catalog.csv`, a filled-in `CatalogListingId` must include the `catalog/` prefix.** Leave the column blank to derive it from `Sku`.
- **Prices are decimal major units** (`1.99`), not micros. The SDK converts them on upload.
- **`isWebshopAvailable` is a local toggle.** Turning it off and redeploying erases the server-side webshop data (`categories`, `hdImages`, `promotion`). Confirm with the user before turning it off on a deployed item.
- **Skip `Subscription` items**, as described above.

## Step 3 — Deploy Catalog to Remote Catalog

`.ucat` and `.catalog.csv` files are local definitions only. Until they are deployed to the Remote Catalog for the active Unity Cloud environment, `FetchRemoteCatalog()` returns no products and no purchase can start.

1. Follow the upload section of [Create your Remote Catalog](https://docs.unity.com/en-us/iap/payment-providers/configure-remote-catalog.md) to deploy the catalog files from the Deployment window. If the Deployment window is missing, the Deployment package isn't installed (see Prerequisites).
2. Check the environment in **Edit > Project Settings > Services > Environments**, because deployment targets the active environment and each environment has its own catalog.
3. If the user can't deploy yet (for example, no Unity Cloud access), list deployment as a manual step in the report and continue with the client code.

For environment configuration, project linking and multi-environment workflows, use the build-live-game skill.

## Step 4 — IAPManager with PaymentProvider

Read [Initialize Unity IAP](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing.md) and [Initiate a purchase](https://docs.unity.com/en-us/iap/payment-providers/initiate-purchase.md) before writing the IAPManager, and reread them if a D2C call fails to compile or returns nothing.

### Initialization order

1. `await UnityServices.InitializeAsync()`.
2. Sign the player in with Unity Authentication. Don't use anonymous sign-in for D2C purchases, because an anonymous player who reinstalls or clears app data loses their purchase history. Use a persistent identity (a linked Unity Authentication account or the user's own identity provider), and ask the user which one if the project has none.
3. Only after sign-in succeeds, obtain `UnityIAPServices.StoreController(PaymentProvider.Name)` and call `Connect()`.

`PaymentProvider.Name` identifies the payment provider integration. It is not the provider's display name (Stripe or Coda).

### Payment provider services on the StoreController

The payment-provider APIs are on two properties of the `PaymentProvider.Name`-scoped `StoreController`. The names are asymmetric, and agents often swap them:

| Property | Interface | Holds |
| --- | --- | --- |
| `store.PaymentProviderStoreExtendedService` (singular) | `IPaymentProvidersExtendedService` | eligibility, the Purchase Options UI, presentation modes, `SetDeepLinkScheme` |
| `store.PaymentProvidersExtendedPurchaseService` (plural) | `IPaymentProvidersExtendedPurchaseService` | `PurchaseProduct`, `RedirectToWebshop`, `GenerateURL`, `SetComplianceCheck`, `SetPaymentProviderOverride` |

Don't use `PurchaseService.PaymentProviders`, because it is a different entry point. Both properties are `null` on a `StoreController` scoped to Apple or Google, so guard with `?.` if the same code can see either kind of controller.

### Coexistence with existing Apple / Google StoreController

If the project already has a `StoreController` for Apple App Store or Google Play:

1. Create a new, separate `StoreController` scoped to `PaymentProvider.Name`. Never modify or replace the existing one, because the two manage different products on different billing backends.
2. After the new controller connects, ask the user:

   > "An existing Apple/Google StoreController was found. Would you like to:
   > - **Keep both** — run Apple/Google billing and IAP D2C Capabilities side by side (existing products stay on Apple/Google, new products use IAP D2C Capabilities)
   > - **Remove the Apple/Google StoreController** — migrate fully to IAP D2C Capabilities (note: existing Apple/Google products and purchase history will no longer be managed by the app)"

3. If the user chooses **keep both**, leave the existing controller and its initialization untouched, and document the dual-controller setup in code comments.
4. If the user chooses **remove**, wrap the existing controller code in `#if !USE_IAP_D2C_ONLY` guards rather than deleting it, mark it `[Obsolete]`, and tell the user to retire the matching products in App Store Connect or the Play Console.

### Purchase entry point

Use the built-in Purchase Options UI (`ShowPurchaseOption(catalogListingId)`) as the purchase entry point, as [Initiate a purchase](https://docs.unity.com/en-us/iap/payment-providers/initiate-purchase.md) describes. Get the `IPaymentOptionProvider` once after `Connect()`, with `GetPaymentOptionProviderUGUI()` or `GetPaymentOptionProviderUITK(hostDocument)` to match the project's UI system.

Gotchas:

- Call `GetEligiblePaymentProviders()` after `Connect()` and gate the purchase UI on it. An empty `Providers` list means no routing rule matches the player or region. If `PaymentOptionPopupEnabled` is false, skip the picker, because the server has suppressed it.
- **Don't call `PurchaseProduct` on the D2C-scoped controller as a "native fallback".** It still routes through the payment provider. A native fallback needs a separate default `StoreController()`. Reuse the project's existing Apple/Google controller if there is one; in a D2C-only project, show a "payment unavailable" error instead.
- `ShowPurchaseOption(catalogListingId)` returns the player's choice: `IPaymentOptionProvider.NativeAlias` (`"native"`), `IPaymentOptionProvider.WebshopAlias` (`"webshop"`), a provider ID such as `"stripe"`, or `null` if the player dismissed the picker. Treat `null` as a cancel, not an error.
- **The SDK remembers each player's last provider on the device** and preselects it. If the picker seems to skip options, that's expected. During testing, clear app data to reset it, because there is no API to clear it.

### IAPManager responsibilities

Same as standard IAP 5 (see `path-add-iap-to-new-project.md` Step 4), with these additions:
- Hold the `PaymentProvider.Name`-scoped `StoreController` instance.
- Hold the `IPaymentOptionProvider` instance (UGUI or UI Toolkit, matching project's UI system).
- Expose `Buy(string catalogListingId)` — calls `ShowPurchaseOption(catalogListingId)` if eligible, and otherwise falls back to the native controller or reports that payment is unavailable.
- Expose `RestorePurchases()` only if NonConsumable products exist.
- Ensure `UnityServices.InitializeAsync()` and player sign-in both complete before `StoreController(PaymentProvider.Name)` is obtained and `Connect()` is called.
- Call `GetEligiblePaymentProviders()` after connect and use the result to gate purchase UI visibility.
- **Only automatic entitlement delivery is supported.** Do not implement server-authoritative grant logic in this path unless the user explicitly requests it.

## Step 5 — Remote Catalog

D2C products come from the Remote Catalog, not a local `List<ProductDefinition>`. Fetch them with `RemoteCatalogProvider`, as [Initialize Unity IAP](https://docs.unity.com/en-us/iap/set-up-in-app-purchasing.md) shows.

- Call `FetchRemoteCatalog()` after `Connect()` succeeds, then pass `catalogProvider.GetProducts()` to `store.FetchProducts()`.
- If the fetch result isn't successful, stop and surface the error, because every later step depends on the catalog.
- `FetchRemoteCatalog()` reads the catalog of the active environment. An empty result usually means the catalog wasn't deployed to that environment (Step 3).
- A product (uSKU) can have several catalog listings, for example regional or promotional offers. `product.catalogListings` is keyed by `catalogListingId`, and `GetProductByCatalogListingId(id)` returns the owning `Product`. Check `listing.availableToPurchase` before offering a listing. If every product has one listing, `GetProductById(uSKU)` is enough.
- For a custom shop UI, `product.metadata.GetPaymentProviderProductMetadata()` returns webshop fields (`hasWebshop`, `localizedWebshopPrice`, `webshopPriceString`). It returns `null` for products not fetched through the payment provider store.
- `PaymentProvidersExtendedPurchaseService.RedirectToWebshop(catalogListingId)` opens the webshop directly, without the picker. It runs the compliance callback first. Network failures throw on the returned `Task`, and a compliance rejection arrives through `OnPurchaseFailed`.

## Step 6 — Deep Link Setup

D2C checkout runs in a browser, which must redirect back to the game when payment completes. Read [Deep link success URL example](https://docs.unity.com/en-us/iap/payment-providers/success-deep-link.md) and the Unity Manual's [deep linking](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/cross-platform-features/deep-linking.md) page and the platform page it links to before configuring it, and reread them if the redirect doesn't return to the app. In this Manual URL, replace `<VERSION>` with the major.minor of the Editor in `ProjectSettings/ProjectVersion.txt`, not the package version.

1. Ask the user:

   > "What redirect URL did you configure in the Unity Cloud IAP payment provider dashboard? For example: `mygame://iapresult/okay`. If you haven't set one yet, set it in the dashboard first before continuing."

2. Prefer an app-scheme deep link (`mygame://...`) over an `https://` link, because `https://` links on Android need Google Digital Asset Links domain verification.
3. Call `store.PaymentProviderStoreExtendedService?.SetDeepLinkScheme("mygame")` **before** `Connect()`. Pass the scheme only, not the full URL, because the SDK matches incoming links against `<scheme>:` and a full URL never matches.
4. On Android, also declare the scheme and host in a custom `AndroidManifest.xml` intent filter, as the Manual's Android deep linking page describes, because `SetDeepLinkScheme` alone doesn't make the OS route the redirect to the app.
5. For production builds, recommend the proxy page from the success deep link example as the Success Redirect URL, because some devices drop a direct app-scheme redirect from a payment provider domain. The app-side setup stays the same.

In the Editor, the purchase callback arrives without a browser redirect, so no deep link setup is needed to test there.

If Android external link validation blocks dev or QA testing, the `IAP_SKIP_EXTERNAL_LINK_VALIDATION` scripting define bypasses it. Add it only to dev or QA builds and tell the user, because shipping it violates Google Play policies.

## Step 7 — Purchase Handling

Purchase handling follows the same save-before-confirm contract as standard IAP 5. See **Step 5 — Purchase Handling Contract** in `path-add-iap-to-new-project.md` for the full rules.

IAP D2C Capabilities-specific notes:

- **Checkout presentation mode:** leave the default, the external browser, unless the user asks for an in-app WebView. In that case call `store.PaymentProviderStoreExtendedService?.SetCheckoutPresentationMode(CheckoutPresentationMode.WebView)` before the purchase. With the external browser the game is suspended and resumes through the deep link. With WebView the game stays active. `OnPurchasePending` fires either way.
- **Webshop presentation mode is independent.** `RedirectToWebshop` uses `SetWebshopPresentationMode`, not the checkout mode, so setting checkout to WebView doesn't move the webshop into the WebView. If the user wants both in-app, set both.
- `OnPurchaseDeferred` fires if the payment is not immediately completed. Do not grant — show pending UI.
- Always confirm (`ConfirmPurchase`) only after entitlement is granted and saved. For consumables, Unity IAP D2C Capabilities prevents re-purchase until the previous order is confirmed.

### Apple and Google external purchase compliance

Apple and Google allow external web payments only in some regions and under their own program rules, and the developer, not Unity, is responsible for eligibility and disclosure. Read [Report transactions to Apple and Google](https://docs.unity.com/en-us/iap/payment-providers/external-purchase-compliance.md) before adding any compliance code, and ask the user which programs and regions they ship in, because the required flow depends on it.

Gotchas:

- `SetComplianceCheck` gates `PurchaseProduct` and `Purchase` only. It does **not** gate `GenerateURL`.
- Calling `GenerateURL` and then `PurchaseProduct` for the same listing reuses the same order, so disclosing the URL first doesn't double-charge.
- An order accepts at most two Apple/Google transaction tokens.

## Step 8 — Product Type Behavior

Same rules as standard IAP 5 (see `path-add-iap-to-new-project.md` Step 6), with one restriction:

**Subscriptions are not supported by IAP D2C Capabilities in v5.4+.** Skip any subscription products, warn the user which ones were excluded, and continue with Consumable and NonConsumable products only. Document excluded subscriptions as a TODO in the verification report for when subscription support is added.

## Step 9 — Cloud Save Integration

Same rules as standard IAP 5 — see `path-add-iap-to-new-project.md` Step 7. Save must complete before `ConfirmPurchase` is called.

## Step 10 — Verification Report

After applying changes, produce a report with these sections:

1. **Files changed** — list with nature of each change
2. **Product IDs, types, and `.ucat` files** — catalog summary
3. **Reward mapping** — product ID → field/method credited
4. **Deep link scheme configured** — scheme, host, AndroidManifest entry
5. **Save behavior** — which save method is called, when
6. **Restore behavior** — which products are restorable, how
7. **Pending / deferred handling** — confirmation of save-before-confirm and deferred UI
8. **Manual steps still required** — listed below

## Step 11 — Manual Steps (always include in report)

These happen in the Unity Dashboard or with third parties, so the agent can't do them. List each one in the report with a link to the section of [Integrate D2C payment providers workflow](https://docs.unity.com/en-us/iap/payment-providers/workflow.md) that explains it:

1. **Deploy the catalog** to the Remote Catalog (Step 3), if you couldn't.
2. **Connect a payment provider** (Stripe or Coda). Until then, the environment runs in [test mode](https://docs.unity.com/en-us/iap/payment-providers/test-mode.md).
3. **Set the redirect URLs** to match the deep link scheme you registered in Step 6.
4. **Choose purchase fulfillment**: SDK only, a webhook, or a Cloud Code module. Recommend server-side fulfillment for production.
5. **Add at least one routing rule** ([Add a routing rule](https://docs.unity.com/en-us/iap/payment-providers/routing.md)), because without one no player is offered a payment provider, even with a connected account.
6. **Confirm the active environment** in **Edit > Project Settings > Services > Environments** matches where the catalog was deployed.
7. **Review platform compliance** ([Report transactions to Apple and Google](https://docs.unity.com/en-us/iap/payment-providers/external-purchase-compliance.md)) and the provider's prohibited-business policy before shipping.
