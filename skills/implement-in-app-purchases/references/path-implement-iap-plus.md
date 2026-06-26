# Implement Unity IAP Plus (Third-Party Payment Provider)

## Table of Contents

- [Trigger Phrases](#trigger-phrases)
- [Prerequisites (verify before any changes)](#prerequisites-verify-before-any-changes)
- [Step 1 — Project Scan](#step-1--project-scan)
- [Step 2 — Product Discovery](#step-2--product-discovery)
- [Step 3 — IAPManager with PaymentProvider](#step-3--iapmanager-with-paymentprovider)
- [Step 4 — Remote Catalog](#step-4--remote-catalog)
- [Step 5 — Deep Link Setup](#step-5--deep-link-setup)
- [Step 6 — Purchase Handling](#step-6--purchase-handling)
- [Step 7 — Product Type Behavior](#step-7--product-type-behavior)
- [Step 8 — Cloud Save Integration](#step-8--cloud-save-integration)
- [Step 9 — Verification Report](#step-9--verification-report)
- [Step 10 — Manual Steps (always include in report)](#step-10--manual-steps-always-include-in-report)

Use this reference when the user explicitly asks to add IAP Plus (third-party payment provider such as Stripe or Coda) to a project. This path is valid only when the project has no IAP, or already has `com.unity.purchasing` v5.x installed.

**Not valid when:** the project has IAP v4, native Google Billing, or a third-party IAP package — resolve those first via `pre-check.md` before returning to this path.

## Trigger Phrases

- "Add IAP Plus"
- "Integrate Stripe / Coda payments"
- "Add third-party payment provider"
- "Set up web-based IAP checkout"
- "Add Unity payment provider"

## Prerequisites (verify before any changes)

| Requirement | Detail |
|---|---|
| `com.unity.purchasing` | **v5.4+** |
| Unity Authentication package | Required — IAP Plus depends on a signed-in player |
| Unity Gaming Services initialized | `UnityServices.InitializeAsync()` and sign-in must complete **before** IAP Plus initialization |
| Unity Cloud project linked | Project must be connected to a Unity Cloud organization |
| Payment provider account | Developer must have a Stripe or Coda account connected in the Unity Cloud IAP dashboard |
| Unity Cloud IAP dashboard setup | Products must be created and deployed to the Remote Catalog in Unity Cloud before the client can fetch them |

If any prerequisite is missing, stop and list the missing items for the user before writing any code.

## Step 1 — Project Scan

Search the project for existing IAP and economy signals before writing any code.

### 1a — Existing IAP detection

Search `Packages/manifest.json` for `com.unity.purchasing`:
- If **v5.4+** found → proceed, IAP Plus will be added alongside.
- If **v5.0–5.3** found → inform the user that v5.4+ is required, instruct them to upgrade `com.unity.purchasing` to v5.4+ via Package Manager, and continue once the upgrade is confirmed.
- If **absent** → proceed, IAP Plus will be added fresh.

### 1b — Unity Authentication detection

Search `Packages/manifest.json` for `com.unity.services.authentication`. If absent, inform the user it must be installed — IAP Plus will not initialize without a signed-in player.

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

Locate `Assets/**/*.ucat` files. Each file is a JSON product definition (see format below). Use them as-is.

### If existing IAP 5 `ProductDefinition` objects are found

These are local store products — IAP Plus uses a Remote Catalog instead. Inform the user that existing local product definitions need to be re-created as `.ucat` files deployed to Unity Cloud, and that product IDs (`uSKU`) should match the existing IDs to preserve store history.

### If no product definitions exist

**Stop and ask:**

> "Please provide the first IAP product ID and type — for example `com.mygame.coins100` as Consumable — and tell me which inventory field, currency, or item should be credited after purchase."

- Default to Consumable if type is not provided, but state the assumption explicitly.
- **Subscriptions are not supported** by IAP Plus in v5.4+. If the user requests a subscription product, inform them and stop.

### Product definition format (`.ucat`)

IAP Plus products are defined as JSON files with the `.ucat` extension, uploaded to Unity Cloud — not defined in code. Each file defines one product:

```json
{
  "uSKU": "com.mygame.coins100",
  "type": "Consumable",
  "productDetails": [
    {
      "title": "100 Coins",
      "description": "A pack of 100 coins.",
      "language": "en-US"
    }
  ],
  "pricing": [
    {
      "currencyCode": "USD",
      "amount": 1.99
    }
  ],
  "imageUrl": "https://example.com/img/coins100.png"
}
```

| Field | Notes |
|---|---|
| `uSKU` | Product ID — equivalent to `ProductDefinition.id` in standard IAP. Do not change existing IDs. |
| `type` | `"Consumable"` or `"Non-consumable"`. Subscriptions not supported. |
| `productDetails` | Array of localised title/description. Include at minimum `en-US`. |
| `pricing` | Array of currency/amount pairs. At minimum include the base currency. |
| `imageUrl` | URL to product image. Optional but recommended for shop UI. |

Remind the user that `.ucat` files must be **deployed to the Remote Catalog** via Unity Cloud dashboard or the Deployment package before the client can fetch them.

## Step 3 — IAPManager with PaymentProvider

### Key difference from standard IAP 5

IAP Plus uses a `PaymentProvider.Name` parameter when obtaining the `StoreController`:

```csharp
// Standard IAP 5
_storeController = UnityIAPServices.StoreController();

// IAP Plus — pass the payment provider name
_storeController = UnityIAPServices.StoreController(PaymentProvider.Name);
```

`PaymentProvider.Name` is a constant that identifies the third-party provider integration. It does **not** return the display name of the provider (Stripe/Coda).

### Initialization sequence

IAP Plus requires Unity Gaming Services and player sign-in to complete **before** `StoreController` is obtained. Follow this order:

1. `await UnityServices.InitializeAsync()`
2. Sign the player in via Unity Authentication
3. Only after sign-in succeeds: obtain `StoreController(PaymentProvider.Name)` and call `Connect()`

Do not call `StoreController(PaymentProvider.Name)` before the player is authenticated.

### Coexistence with existing Apple / Google StoreController

If the project already has a `StoreController` for Apple App Store or Google Play (standard IAP 5):

- **Always create a new, separate `StoreController`** scoped to `PaymentProvider.Name`. Never modify or replace the existing one.
- The two controllers are independent — they manage different products on different billing backends and do not interfere with each other.
- After the new IAP Plus `StoreController` is successfully created and connected, **prompt the user:**

  > "An existing Apple/Google StoreController was found. Would you like to:
  > - **Keep both** — run Apple/Google billing and IAP Plus side by side (existing products stay on Apple/Google, new products use IAP Plus)
  > - **Remove the Apple/Google StoreController** — migrate fully to IAP Plus (note: existing Apple/Google products and purchase history will no longer be managed by the app)"

- If the user chooses **keep both**: leave the existing `StoreController` and its initialization code untouched. Document the dual-controller setup in code comments.
- If the user chooses **remove**: wrap the existing `StoreController` code in `#if !USE_IAP_PLUS_ONLY` guards — do not delete it. Mark it `[Obsolete]` and document the removal decision. Instruct the user to also retire the corresponding products in App Store Connect / Play Console as needed.

### IAPManager responsibilities

Same as standard IAP 5 (see `path-add-iap-to-new-project.md` Step 3), with these additions:
- Hold the `PaymentProvider.Name`-scoped `StoreController` instance.
- Expose `Buy(string productId)` — delegates to `_storeController.PurchaseProduct(productId)`.
- Expose `RestorePurchases()` only if NonConsumable products exist.
- Ensure the player is authenticated before `InitializeAsync()` is called.
- **Only automatic entitlement delivery is supported.** Do not implement server-authoritative grant logic in this path unless the user explicitly requests it.

## Step 4 — Remote Catalog

IAP Plus products come from Unity Cloud, not a local `List<ProductDefinition>`. Use `RemoteCatalogProvider` to fetch them:

```csharp
// 1. Create the provider
var catalogProvider = new RemoteCatalogProvider();

// 2. Fetch catalog for the payment provider
var result = await catalogProvider.FetchRemoteCatalog(new List<string>
{
    PaymentProvider.Name
});

if (!result.Success)
    throw result.Exception;

// 3. Pass fetched definitions to the StoreController
var productDefinitions = catalogProvider.GetProducts();
_storeController.FetchProducts(productDefinitions);
```

- `FetchRemoteCatalog` fetches the catalog deployed to the Unity Cloud environment configured in **Edit > Project Settings > Services > Environments** (Development / Staging / Production).
- Call `FetchRemoteCatalog` after `Connect()` succeeds.
- `catalogProvider.GetProducts()` returns `IEnumerable<ProductDefinition>` — pass directly to `store.FetchProducts()`.
- If `result.Success` is false, do not proceed — surface the error to the user.

## Step 5 — Deep Link Setup

IAP Plus launches the device's mobile browser to handle payment. After payment, the browser must redirect back to the game via a deep link.

### Choosing a deep link scheme

**Use app-scheme deep links** (e.g., `mygame://iapresult/okay`) rather than URL-scheme deep links (e.g., `https://mygame.com/iapresult`).

URL-scheme deep links require the domain to be verified by Google (Digital Asset Links). App-scheme deep links avoid this requirement and are simpler to configure.

**Prompt the user:**

> "What redirect URL did you configure in the Unity Cloud IAP payment provider dashboard? For example: `mygame://iapresult/okay`. If you haven't set one yet, set it in the dashboard first before continuing."

### AndroidManifest.xml

The deep link scheme must be declared in `AndroidManifest.xml` so Android routes the browser redirect back to the app. If the project does not have a custom `AndroidManifest.xml`, instruct the user to enable it: **Edit > Project Settings > Player > Publishing Settings > Custom Main Manifest**.

Add an intent filter for the deep link scheme inside the `<activity>` element:

```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="mygame" android:host="iapresult" />
</intent-filter>
```

Replace `mygame` and `iapresult` with the scheme and host from the user's configured redirect URL.

### Unity Editor

In the Unity Editor, the purchase callback is received directly without a browser redirect — no additional setup is required for Editor testing.

### Google Play external link validation (dev/QA only)

If testing on Android and running into external link validation issues, the following scripting define can be added for dev/QA builds only:

```
IAP_SKIP_EXTERNAL_LINK_VALIDATION
```

**Location:** Edit > Project Settings > Player > Scripting Define Symbols

**WARNING:** Do not include this define in production builds — it violates Google Play policies.

## Step 6 — Purchase Handling

Purchase handling follows the same save-before-confirm contract as standard IAP 5. See **Step 4 — Purchase Handling Contract** in `path-add-iap-to-new-project.md` for the full rules.

IAP Plus-specific notes:
- The payment flow happens in the device's browser. The game is suspended during payment and resumes via the deep link redirect. `OnPurchasePending` fires after the deep link returns control to the game.
- `OnPurchaseDeferred` fires if the payment is not immediately completed. Do not grant — show pending UI.
- Always confirm (`ConfirmPurchase`) only after entitlement is granted and saved. For consumables, Unity IAP Plus prevents re-purchase until the previous order is confirmed.

## Step 7 — Product Type Behavior

Same rules as standard IAP 5 (see `path-add-iap-to-new-project.md` Step 5), with one restriction:

**Subscriptions are not supported by IAP Plus in v5.4+.** If subscription products are detected or requested, inform the user and exclude them from the IAP Plus implementation. Document them as a TODO for when subscription support is added.

## Step 8 — Cloud Save Integration

Same rules as standard IAP 5 — see `path-add-iap-to-new-project.md` Step 6. Save must complete before `ConfirmPurchase` is called.

## Step 9 — Verification Report

After applying changes, produce a report with these sections:

1. **Files changed** — list with nature of each change
2. **Product IDs, types, and `.ucat` files** — catalog summary
3. **Reward mapping** — product ID → field/method credited
4. **Deep link scheme configured** — scheme, host, AndroidManifest entry
5. **Save behavior** — which save method is called, when
6. **Restore behavior** — which products are restorable, how
7. **Pending / deferred handling** — confirmation of save-before-confirm and deferred UI
8. **Manual steps still required** — listed below

## Step 10 — Manual Steps (always include in report)

These cannot be automated and must be completed by the developer:

1. **Unity Cloud IAP dashboard** — create products matching the `.ucat` definitions and deploy to the Remote Catalog.
2. **Payment provider account** — connect Stripe or Coda account in Unity Cloud IAP dashboard. Request enablement from Unity Client Partner with your organization ID if not yet enabled.
3. **Redirect URL** — set the deep link redirect URL in the payment provider dashboard (e.g., `mygame://iapresult/okay`).
4. **Environment selection** — confirm the correct environment (Development / Staging / Production) is set in **Edit > Project Settings > Services > Environments**.
5. **Android deep link verification** — if using URL-scheme deep links, complete Google Digital Asset Links domain verification. (Avoided if using app-scheme deep links.)
6. **Platform compliance review** — external web payments and third-party payment providers are permitted in select regions only. Developer is responsible for adhering to Apple App Store and Google Play regional requirements.
7. **Prohibited business / content review** — review Stripe's Prohibited/Restricted Businesses list and Coda's Prohibited Content Policy for compliance.
