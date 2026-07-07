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

**Not valid when:** the project has IAP v4, native Google Billing, or a third-party IAP package — resolve those first via `pre-check.md` before returning to this path.

## Trigger Phrases

- "Add IAP D2C Capabilities"
- "Integrate Stripe / Coda payments"
- "Add third-party payment provider"
- "Set up web-based IAP checkout"
- "Add Unity payment provider"

## Prerequisites (verify before any changes)

| Requirement | Detail |
|---|---|
| Unity Editor | **2022.3 or later** — IAP 5.4 does not support older Editor versions |
| `com.unity.purchasing` | **v5.4+** |
| `com.unity.services.authentication` | **v3.7.1+** — IAP D2C Capabilities will not initialize without this version or later |
| `com.unity.services.core` | **v1.18.0+** — required by IAP 5.4 |
| Unity Gaming Services initialized | `UnityServices.InitializeAsync()` and sign-in must complete **before** IAP D2C Capabilities initialization |
| Unity Deployment package | Required to deploy product catalogs to the Remote Catalog service (see Step 3) — install via **Window > Package Manager > Unity Registry > Deployment** |
| Unity Cloud project linked | Project must be connected to a Unity Cloud organization |
| Payment provider account | Developer must have a Stripe or Coda account connected in the Unity Cloud IAP dashboard |
| Unity Cloud IAP dashboard setup | Products must be created and deployed to the Remote Catalog in Unity Cloud before the client can fetch them |

If any prerequisite is missing, stop and list the missing items for the user before writing any code.

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

Locate `Assets/**/*.ucat` files. Each file is a JSON product definition (see format below). Use them as-is.

### If existing IAP 5 `ProductDefinition` objects are found

These are local store products — IAP D2C Capabilities uses a Remote Catalog instead. Inform the user that existing local product definitions need to be re-created as `.ucat` files deployed to Unity Cloud, and that product IDs (`uSKU`) should match the existing IDs to preserve store history.

### If no product definitions exist

**Stop and ask:**

> "Please provide the first IAP product ID and type — for example `com.mygame.coins100` as Consumable — and tell me which inventory field, currency, or item should be credited after purchase."

- Default to Consumable if type is not provided, but state the assumption explicitly.
- **If any Subscription products are provided or detected**, exclude them from the implementation and warn the user:

  > "The following products were skipped because subscriptions are not supported by IAP D2C Capabilities in v5.4+: [list]. Only Consumable and NonConsumable products will be implemented."

  Continue with the remaining Consumable and NonConsumable products. If no supported products remain after exclusion, stop and inform the user.

### Product definition format (`.ucat`)

IAP D2C Capabilities products are defined as JSON files with the `.ucat` extension under `Assets/` — not defined in code. Each file defines one product. The local `.ucat` shape is fed by `Editor/Authoring/Core/Model/CatalogItem.cs`; on upload the SDK converts it to the wire DTO (`CatalogItemDto.cs`) — see "Local vs uploaded shape" below.

**Identity — filename drives it:**

The `.ucat` filename (without extension) is the source of truth for two identifiers on load (`Editor/Authoring/IO/CatalogItemLoader.cs`):

- **`CatalogListingId` = `"catalog/" + <filename-stem>`**. Always. This is the id passed to `PurchaseProduct(catalogListingId)` and matched against the remote catalog. It is **never** stored in the `.ucat` JSON body (`[IgnoreDataMember, JsonIgnore]` — see `CatalogItem.cs:41-42`); the `catalog/` prefix is added by the SDK, not the developer.
- **`uSKU` = the JSON `uSKU` field if present, otherwise `<filename-stem>`**. Omit `uSKU` from the JSON when you want it to match the filename (the common case). Include it only when the store SKU must differ from the filename (e.g. legacy IDs you can't rename). On save, if `uSKU` equals the filename stem, the writer strips it out again to keep the file lean.

So a file named `coins_100.ucat` with no `uSKU` field gives you `uSKU = "coins_100"` and `CatalogListingId = "catalog/coins_100"` for free.

```json
{
  "type": "Consumable",
  "productDetails": [
    {
      "language": "en_US",
      "title": "100 Coins",
      "subtitle": "Best value",
      "description": "A pack of 100 coins.",
      "badge": { "text": "Popular", "imageUrl": "https://example.com/badges/popular.png" }
    }
  ],
  "pricing": [
    { "currencyCode": "USD", "amount": 1.99 },
    { "currencyCode": "EUR", "amount": 1.79, "webshopPrice": 1.49 }
  ],
  "imageUrl": "https://example.com/img/coins100.png",
  "storeIdOverrides": [
    { "store": "apple",  "value": "com.mygame.coins100.ios" },
    { "store": "google", "value": "com.mygame.coins100.android" }
  ],
  "isWebshopAvailable": true,
  "categories": ["currency", "starter"],
  "hdImages": [
    { "url": "https://example.com/hd/coins100.png", "altText": "100 coins bundle" }
  ],
  "promotion": {
    "type": "Sale",
    "startsAt": "2026-12-01T00:00:00Z",
    "endsAt": "2026-12-15T23:59:59Z"
  }
}
```

(Add `"uSKU": "..."` explicitly only when overriding the filename-derived value.)

**Top-level fields:**

| Field | Type | Required | Notes |
|---|---|---|---|
| `uSKU` | string |   | Product ID — equivalent to `ProductDefinition.id` in standard IAP. **Optional in the JSON body** — defaults to the filename stem. Include it only to override. Pattern `^[a-zA-Z0-9._-]+$`, max 141 chars. |
| `type` | enum | ✓ | `"Consumable"`, `"NonConsumable"`, or `"Subscription"`. |
| `productDetails` | array | ✓ | ≥1 entry. See sub-schema below. |
| `pricing` | array | ✓ | ≥1 entry. See sub-schema below. |
| `imageUrl` | string | | HTTPS URL, max 2048 chars. Recommended for shop UI. |
| `storeIdOverrides` | array | | Per-store SKU overrides. See sub-schema below. |
| `isWebshopAvailable` | bool | | **Local toggle only** — never uploaded. Opts the item into the webshop schema variant. When `true`, the SDK adds the `UnityRemoteCatalogWebshop` `$schema` URL and emits `categories` / `hdImages` / `promotion` on upload. When `false`, those webshop fields are stripped on upload and any server-side webshop data is erased. |
| `categories` | string[] | | Webshop taxonomy. Only meaningful when `isWebshopAvailable=true`. |
| `hdImages` | array | | Webshop hero images. Only meaningful when `isWebshopAvailable=true`. See sub-schema below. |
| `promotion` | object | | Webshop promotion overlay. Only meaningful when `isWebshopAvailable=true`. See sub-schema below. |

**`productDetails[]`:** `language` (required, enum e.g. `en_US`, `fr_FR`), `title` (required, 1–50 chars), `subtitle` (optional, 1–50 chars), `description` (optional, 1–250 chars), `badge` (optional, `{ text (required), imageUrl (optional HTTPS) }`).

**`pricing[]`:** `currencyCode` (required, ISO 4217 e.g. `"USD"`), `amount` (required, decimal in major units — `1.99`, not micros), `webshopPrice` (optional, decimal in major units — set only when a webshop-only price differs from `amount`; values below `0.001` are treated as unset).

**`storeIdOverrides[]`:** `store` (required, enum: `"apple"`, `"google"`, `"xbox"`, `"applemacos"`), `value` (required, the store-specific SKU).

**`hdImages[]`:** `url` (required, HTTPS), `altText` (optional).

**`promotion`:** `type` (required, enum: `"Sale"`, `"Bonus"`, `"Limited"`), `startsAt` (optional, ISO 8601 UTC), `endsAt` (optional, ISO 8601 UTC).

**Local vs uploaded shape.** The `.ucat` on disk is what devs edit; the payload sent to the Live Content admin API is converted by `LiveContentConfigClient.ConvertToDto`:

- `amount` and `webshopPrice` go from decimal major units on disk (`1.99`) to micros integer on the wire (`1990000`). Rounding: `Math.Round(amount * 1_000_000, MidpointRounding.AwayFromZero)`.
- `$schema` is populated by the SDK on upload — never in the on-disk `.ucat`:
  - Always: `https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalog/versions/1.1.0`
  - Added when `isWebshopAvailable=true`: `https://services.api.unity.com/schema-registry/v1/schemas/UnityRemoteCatalogWebshop/versions/1.1.0`
- `isWebshopAvailable` is never uploaded — the server infers webshop-ness from the `$schema` array.
- `categories`, `hdImages`, `promotion` are only emitted on upload when `isWebshopAvailable=true`. Toggling it off and re-uploading erases the server-side webshop data.

Remind the user that `.ucat` files must be **deployed to the Remote Catalog** before the client can fetch them — see Step 3.

### Product definition format (`.catalog.csv`)

An alternative to per-item `.ucat` JSON is a single `.catalog.csv` file at any location under `Assets/`. The parser is `Editor/Authoring/Core/IO/CatalogCsvParser.cs`; each row is aggregated by `CatalogListingId` into the same `CatalogItem` model as the `.ucat` path, so uploaded output is identical.

**Columns (23):**

| Column | Maps to | Aggregation |
|---|---|---|
| `CatalogListingId` | catalog listing id — row-identity key. If provided, it **must include the `catalog/` prefix** (e.g. `catalog/coins_100`). If blank, the parser derives it as `"catalog/" + Sku`. | per-item |
| `Sku` | `uSKU` — required. Also seeds `CatalogListingId` when that column is blank. | per-item |
| `ProductType` | `type` (Consumable / NonConsumable / Subscription) | per-item |
| `Language` | `productDetails[].language` | per-row |
| `Title` | `productDetails[].title` | per-row |
| `Subtitle` | `productDetails[].subtitle` | per-row |
| `Description` | `productDetails[].description` | per-row |
| `BadgeText` | `productDetails[].badge.text` | per-row |
| `BadgeImageUrl` | `productDetails[].badge.imageUrl` | per-row |
| `CurrencyCode` | `pricing[].currencyCode` | per-row |
| `Amount` | `pricing[].amount` (decimal major units, e.g. `1.99`) | per-row |
| `WebshopPrice` | `pricing[].webshopPrice` (decimal major units) | per-row |
| `ImageUrl` | `imageUrl` | per-item |
| `GoogleOverride` | `storeIdOverrides[store="google"].value` | per-item |
| `AppleOverride` | `storeIdOverrides[store="apple"].value` | per-item |
| `XboxStoreOverride` | `storeIdOverrides[store="xbox"].value` | per-item |
| `MacAppStoreOverride` | `storeIdOverrides[store="applemacos"].value` | per-item |
| `IsWebshopAvailable` | `isWebshopAvailable` (local toggle — see .ucat notes) | per-item |
| `Category` | `categories[]` (one category per row) | per-row, aggregated |
| `HdImageUrl` | `hdImages[].url` (one image per row) | per-row, aggregated |
| `HdImageAltText` | `hdImages[].altText` | per-row, aggregated |
| `PromotionType` | `promotion.type` (Sale / Bonus / Limited) | per-item, first row wins, conflicts on later rows flagged |
| `PromotionStartsAt` | `promotion.startsAt` (ISO 8601 UTC) | per-item, first row wins |
| `PromotionEndsAt` | `promotion.endsAt` (ISO 8601 UTC) | per-item, first row wins |

**Aggregation rules:**
- **per-item** columns (Sku, ProductType, imageUrl, overrides, IsWebshopAvailable, Promotion*): set once on the first row of a given `CatalogListingId`. Subsequent rows are conflict-checked and flagged if they disagree.
- **per-row** columns without aggregation (Language/Title/Subtitle/Description/Badge*/CurrencyCode/Amount/WebshopPrice): one `productDetails` or `pricing` entry per row.
- **per-row, aggregated** (Category, HdImageUrl, HdImageAltText): each row contributes one entry to the corresponding array on the item.

**Minimal example (one product, EN + FR localizations, one price):**

```csv
CatalogListingId,Sku,ProductType,Language,Title,Description,CurrencyCode,Amount,ImageUrl
,com.mygame.coins100,Consumable,en_US,100 Coins,A pack of 100 coins.,USD,1.99,https://example.com/img/coins100.png
,com.mygame.coins100,Consumable,fr_FR,100 Pièces,Un lot de 100 pièces.,EUR,1.79,
```

The `CatalogListingId` column is deliberately blank — the parser derives the value as `"catalog/" + Sku` (yielding `catalog/com.mygame.coins100` here). If you fill it in explicitly, the value **must** include the `catalog/` prefix (e.g. `catalog/coins_100`); a bare value like `coins_100` fails validation because catalog listing IDs must start with `catalog/`.

The same amount/schema conversion applies on upload as for `.ucat`.

## Step 3 — Deploy Catalog to Remote Catalog

`.ucat` and `.catalog.csv` files under `Assets/` are local definitions only. Until they are deployed to the Remote Catalog for the active Unity Cloud environment, `FetchRemoteCatalog()` returns no products and no purchase can be initiated. Deploy from the Editor **before** writing or running the client code in later steps.

### Deploying from the Editor

1. Open **Services > Deployment** in the Unity Editor. If the menu is missing, install the Unity Deployment package via **Window > Package Manager > Unity Registry > Deployment**.
2. The Deployment window lists the `.ucat` and `.catalog.csv` files discovered under `Assets/` alongside any other deployable assets.
3. Select the catalog files and click **Deploy**.
4. On success, the window marks each file as deployed. The products are now fetchable by the client via `FetchRemoteCatalog()` (see Step 5).

The deployment targets the environment configured in **Edit > Project Settings > Services > Environments** (Development / Staging / Production). To move a catalog between environments, switch the active environment and re-deploy. For environment configuration, project linking, and multi-environment workflows, see the [build-live-games](https://github.com/Unity-Technologies/skills) skill (also shipped with Unity AI Assistant).

## Step 4 — IAPManager with PaymentProvider

### Key difference from standard IAP 5

IAP D2C Capabilities uses a `PaymentProvider.Name` parameter when obtaining the `StoreController`:

```csharp
// Standard IAP 5
_storeController = UnityIAPServices.StoreController();

// IAP D2C Capabilities — pass the payment provider name
_storeController = UnityIAPServices.StoreController(PaymentProvider.Name);
```

`PaymentProvider.Name` is a constant that identifies the third-party provider integration. It does **not** return the display name of the provider (Stripe/Coda).

### Initialization sequence

IAP D2C Capabilities requires Unity Gaming Services and player sign-in to complete **before** `StoreController` is obtained. Follow this order:

1. `await UnityServices.InitializeAsync()`
2. Sign the player in via Unity Authentication
3. Only after sign-in succeeds: obtain `StoreController(PaymentProvider.Name)` and call `Connect()`

Do not call `StoreController(PaymentProvider.Name)` before the player is authenticated.

**Anonymous sign-in warning:** Do not use anonymous sign-in as the authentication method for D2C purchases. Anonymous sign-in does not persist purchases after the session token is lost — if the player reinstalls the app or clears app data, their purchase history becomes unrecoverable. Use a persistent identity method (e.g., Unity Authentication with a linked account, or your own identity provider).

### Coexistence with existing Apple / Google StoreController

If the project already has a `StoreController` for Apple App Store or Google Play (standard IAP 5):

- **Always create a new, separate `StoreController`** scoped to `PaymentProvider.Name`. Never modify or replace the existing one.
- The two controllers are independent — they manage different products on different billing backends and do not interfere with each other.
- After the new IAP D2C Capabilities `StoreController` is successfully created and connected, **prompt the user:**

  > "An existing Apple/Google StoreController was found. Would you like to:
  > - **Keep both** — run Apple/Google billing and IAP D2C Capabilities side by side (existing products stay on Apple/Google, new products use IAP D2C Capabilities)
  > - **Remove the Apple/Google StoreController** — migrate fully to IAP D2C Capabilities (note: existing Apple/Google products and purchase history will no longer be managed by the app)"

- If the user chooses **keep both**: leave the existing `StoreController` and its initialization code untouched. Document the dual-controller setup in code comments.
- If the user chooses **remove**: wrap the existing `StoreController` code in `#if !USE_IAP_D2C_ONLY` guards — do not delete it. Mark it `[Obsolete]` and document the removal decision. Instruct the user to also retire the corresponding products in App Store Connect / Play Console as needed.

### Checking eligible payment providers

Before initiating purchase, use `GetEligiblePaymentProviders()` to confirm that at least one payment provider is available for the current user and region. Call this after `Connect()` and `FetchProducts()` succeed:

```csharp
var eligible = await store.PaymentProviderStoreExtendedService?.GetEligiblePaymentProviders();
if (eligible == null || eligible.Providers.Count == 0)
{
    // No payment providers available — hide purchase UI or show an error
    return;
}

if (!eligible.PaymentOptionPopupEnabled)
{
    // Only one provider available — proceed directly without showing the picker UI
}
```

`EligiblePaymentProviders.Providers` is a priority-ordered list of provider names. If `PaymentOptionPopupEnabled` is false, there is only one provider and the built-in picker UI should be suppressed.

### Built-in payment options picker UI

Unity IAP 5.4 ships a ready-made Purchase Options UI that presents the player with all eligible payment options (native App Store/Google Play, D2C payment provider, webshop). **Use `ShowPurchaseOption` as the primary purchase entry point** — it is the recommended integration path for D2C payments.

Obtain `IPaymentOptionProvider` once after connect and store it:

**UGUI** (for projects using Unity UI):
```csharp
var m_PaymentOptionProvider = store.PaymentProviderStoreExtendedService?.GetPaymentOptionProviderUGUI();
```

**UI Toolkit** (for projects using UI Toolkit):
```csharp
var m_PaymentOptionProvider = store.PaymentProviderStoreExtendedService?.GetPaymentOptionProviderUITK(hostDocument);
```

Then initiate purchase with the `catalogListingId`:
```csharp
public async void Buy(string catalogListingId)
{
    var eligibility = await store.PaymentProviderStoreExtendedService?.GetEligiblePaymentProviders();
    if (eligibility?.Providers.Count > 0)
    {
        // Show the Purchase Options UI — lets the player pick native, D2C, or webshop
        await m_PaymentOptionProvider.ShowPurchaseOption(catalogListingId);
    }
    else if (m_NativeStore != null)
    {
        // No PSP available — fall back to a NATIVE StoreController (App Store / Google Play).
        // `store` above is scoped to PaymentProvider.Name, so calling PurchaseProduct on it
        // would still route through the PSP flow (not native billing). The fallback must use
        // a separate native-scoped StoreController that the app kept alive alongside `store` —
        // see "Coexistence with existing Apple / Google StoreController" above.
        m_NativeStore.PurchaseProduct(catalogListingId);
    }
    else
    {
        // No PSP available AND no native controller — the sale can't proceed.
        // Surface the error to the caller so it can show an appropriate message.
        Debug.LogWarning($"No payment path available for {catalogListingId}.");
    }
}
```

`GetEligiblePaymentProviders()` returns an `EligiblePaymentProviders` bundle. An empty `Providers` list means no routing rules match the player or no payment providers are available for their region.

**Do not call `PurchaseProduct` on the PSP-scoped `StoreController` as a "native fallback"** — it routes through the payment provider store regardless. Native billing requires a separate `StoreController()` (default constructor = platform default). Projects that already have an Apple/Google `StoreController` should reuse it here; greenfield D2C-only projects have no native fallback path and should surface a "payment unavailable" error instead.

`EligiblePaymentProviders` also exposes `PaymentOptionPopupEnabled` (server-driven killswitch) — check it before showing the picker if you want to respect the server's suppression.

### Interpreting the picker result

`ShowPurchaseOption(catalogListingId)` returns `Task<string?>`. The returned string is the identifier of whatever the player picked:

- `IPaymentOptionProvider.NativeAlias` (`"native"`) — the platform default (App Store / Google Play) at click time.
- `IPaymentOptionProvider.WebshopAlias` (`"webshop"`) — the "Continue with webshop" button (only appears when the listing has a webshop; see `PaymentProviderProductMetadata.hasWebshop` below).
- A specific PSP identifier (`"stripe"`, `"codapay"`, …) — the player picked a payment provider directly.
- `null` — the player dismissed the picker (X button / backdrop tap).

The cross-product overload `ShowPurchaseOption(IReadOnlyList<PurchaseOption>)` returns `Task<PurchaseOption?>` — each `PurchaseOption` is `(StoreName, CatalogListingId, Badge?)` and the returned value is the chosen one (or null on cancel). Failures surface as `PurchaseChoiceFailedException` on the awaited task (carries the `PurchaseOption Choice` and the inner exception).

### Provider memory

The SDK remembers the last payment provider used by a player on a given device and pre-selects it the next time the Purchase Options picker is shown. This is intentional UX — returning players skip the picker and go straight to their previous provider.

Implications to be aware of:

- **Not a bug:** If the picker appears to skip showing options, the player has a remembered provider. This is expected behavior.
- **Testing:** After switching providers in the Unity Dashboard, test devices may still route to the previously remembered provider. Clear app data on the device to reset provider memory during testing.
- **No override API:** There is no programmatic way to clear or override provider memory at runtime. If your UX requires always showing the full picker, present a "Change payment method" option that re-invokes `ShowPurchaseOption` — the picker will still show the remembered provider pre-selected, but the player can change it.

### IAPManager responsibilities

Same as standard IAP 5 (see `path-add-iap-to-new-project.md` Step 4), with these additions:
- Hold the `PaymentProvider.Name`-scoped `StoreController` instance.
- Hold the `IPaymentOptionProvider` instance (UGUI or UI Toolkit, matching project's UI system).
- Expose `Buy(string catalogListingId)` — calls `ShowPurchaseOption(catalogListingId)` if eligible, falls back to `PurchaseProduct(catalogListingId)` if not.
- Expose `RestorePurchases()` only if NonConsumable products exist.
- Ensure `UnityServices.InitializeAsync()` and player sign-in both complete before `StoreController(PaymentProvider.Name)` is obtained and `Connect()` is called.
- Call `GetEligiblePaymentProviders()` after connect and use the result to gate purchase UI visibility.
- **Only automatic entitlement delivery is supported.** Do not implement server-authoritative grant logic in this path unless the user explicitly requests it.

## Step 5 — Remote Catalog

IAP D2C Capabilities products come from Unity Cloud, not a local `List<ProductDefinition>`. Use `RemoteCatalogProvider` to fetch them:

```csharp
// 1. Create the provider
var catalogProvider = new RemoteCatalogProvider();

// 2. Fetch catalog — no parameters needed, fetches all configured providers
var result = await catalogProvider.FetchRemoteCatalog();

if (!result.Success)
    throw result.Exception;

// 3. Pass fetched definitions to the StoreController
var productDefinitions = catalogProvider.GetProducts();
_storeController.FetchProducts(productDefinitions);
```

- `FetchRemoteCatalog` fetches the catalog deployed to the Unity Cloud environment configured in **Edit > Project Settings > Services > Environments** (Development / Staging / Production).
- Call `FetchRemoteCatalog` after `Connect()` succeeds.
- `catalogProvider.GetProducts()` returns `List<ProductDefinition>` — pass directly to `store.FetchProducts()`.
- If `result.Success` is false, do not proceed — surface the error to the user.

### CatalogListing and multiple offers per product

IAP D2C Capabilities supports products with multiple purchasable offers, each identified by a `catalogListingId`. A `CatalogListing` groups a `ProductDefinition` with its metadata and availability flag:

```csharp
// Look up the Product that owns a given catalogListingId (set in Unity Cloud dashboard).
// GetProductByCatalogListingId returns the Product; grab the specific CatalogListing off it.
var product = _storeController.GetProductByCatalogListingId("coins_100_offer_usd");
if (product != null
    && product.catalogListings.TryGetValue("coins_100_offer_usd", out var listing)
    && listing.availableToPurchase)
{
    // listing.definition  — ProductDefinition (id, type, catalogListingId)
    // listing.metadata    — ProductMetadata (localizedTitle, localizedPriceString, etc.)
    _storeController.PaymentProvidersExtendedPurchaseService?
        .PurchaseProduct("coins_100_offer_usd", PaymentProvider.Name);
}
```

Use `catalogListingId` when a single product (uSKU) has multiple regional or promotional offers. If products have only one offer each, `GetProductById(uSKU)` is sufficient and `CatalogListing` lookup is not needed.

### Iterating catalog listings on a fetched product

After `OnProductsFetched` fires, each `Product` object exposes a `catalogListings` dictionary. Iterate its values to enumerate all available offers for that product:

```csharp
void OnProductsFetched(List<Product> products)
{
    foreach (var product in products)
    {
        foreach (var catalogListing in product.catalogListings.Values)
        {
            Debug.Log($"ID: {catalogListing.definition.id} - Price: {catalogListing.metadata.localizedPriceString}");
        }
    }
}
```

`product.catalogListings` is a `Dictionary<string, CatalogListing>` keyed by `catalogListingId`. Use `.Values` to iterate all listings, or index directly by ID when the listing ID is known.

### Webshop-aware product metadata

Products fetched through the PaymentProvider store carry an extended metadata type, `PaymentProviderProductMetadata`, that surfaces webshop-specific fields alongside the standard `ProductMetadata`. Retrieve it via the extension method on `ProductMetadata`:

```csharp
var pspMeta = product.metadata.GetPaymentProviderProductMetadata();
if (pspMeta?.hasWebshop == true)
{
    // The listing has a webshop configured; you can render a "Continue with webshop" button
    // and/or a "save X% in the webshop" badge.
    var webshopPrice = pspMeta.localizedWebshopPrice;      // decimal? — null when no webshop price
    var webshopPriceString = pspMeta.webshopPriceString;   // pre-formatted for the current locale
}
```

Returns `null` when the product wasn't fetched through the PaymentProvider store. The built-in picker already reads `hasWebshop` to decide whether to append the webshop button on the single-listing form — this API is for custom UIs that need the same signal.

### Direct webshop launch (no picker)

Skip the picker entirely and open the webshop for a specific listing (or the generic Unity webshop when `catalogListingId` is null):

```csharp
await store.PaymentProvidersExtendedPurchaseService?
    .RedirectToWebshop("coins_100_offer_usd");
```

The SDK fetches the webshop URL, runs the registered compliance callback (`SetComplianceCheck`), and opens the URL on approval. Network failures propagate as exceptions on the returned `Task`; compliance rejection routes through the standard `OnPurchaseFailed` path.

## Step 6 — Deep Link Setup

IAP D2C Capabilities launches the device's mobile browser to handle payment. After payment, the browser must redirect back to the game via a deep link.

### Choosing a deep link scheme

**Use app-scheme deep links** (e.g., `mygame://iapresult/okay`) rather than URL-scheme deep links (e.g., `https://mygame.com/iapresult`).

URL-scheme deep links require the domain to be verified by Google (Digital Asset Links). App-scheme deep links avoid this requirement and are simpler to configure.

**Prompt the user:**

> "What redirect URL did you configure in the Unity Cloud IAP payment provider dashboard? For example: `mygame://iapresult/okay`. If you haven't set one yet, set it in the dashboard first before continuing."

### Registering the deep link scheme

Unity IAP 5.4 provides a programmatic API to register the deep link scheme directly, without editing `AndroidManifest.xml` manually:

```csharp
// Pass the SCHEME ONLY (the part before "://"), not the full redirect URL.
store.PaymentProviderStoreExtendedService?.SetDeepLinkScheme("mygame");
```

Call `SetDeepLinkScheme` before `Connect()`. The SDK matches incoming deep links against `<scheme>:` — passing the full URL (e.g. `"mygame://iapresult/okay"`) produces the prefix `"mygame://iapresult/okay:"` which will never match. Register only the scheme portion of whatever redirect URL is configured in the Unity Cloud IAP payment provider dashboard.

### AndroidManifest.xml (required in addition to `SetDeepLinkScheme`)

Android also requires the scheme to be declared in `AndroidManifest.xml` so the OS routes the browser redirect back to the app. `SetDeepLinkScheme` alone is not sufficient on Android — both are needed.

If the project does not have a custom `AndroidManifest.xml`, instruct the user to enable it: **Edit > Project Settings > Player > Publishing Settings > Custom Main Manifest**.

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

### Proxy HTML page (recommended for production)

On some Android devices and iOS configurations, the OS silently drops a direct app-scheme redirect originating from a payment provider domain. To avoid missed redirects, host a lightweight proxy HTML page on a domain you control and use its URL as the Success Redirect URL in the Unity Cloud payment provider dashboard:

```html
<!DOCTYPE html>
<html>
<head><title>Redirecting…</title></head>
<body>
<script>
  // Replace with your app-scheme deep link
  window.location = "mygame://iapresult/okay";
</script>
<p>Redirecting back to the app…</p>
</body>
</html>
```

How to set this up:

1. Host the HTML file at a stable HTTPS URL you control (e.g., `https://mygame.com/iap-redirect.html`).
2. In the Unity Cloud IAP dashboard, set **Success Redirect URL** to the HTTPS proxy URL — **not** the app-scheme URL directly.
3. The proxy page's JavaScript immediately redirects the browser to the app-scheme URL (`mygame://iapresult/okay`), which the OS routes back into the app.
4. The `AndroidManifest.xml` intent filter and `SetDeepLinkScheme` registration remain the same — they handle the final app-scheme leg of the redirect.

**When to skip this:** If you are testing in the Unity Editor or on a controlled device where direct app-scheme redirects work reliably, the proxy is optional. For production builds targeting a broad range of Android and iOS devices, the proxy is recommended.

### Unity Editor

In the Unity Editor, the purchase callback is received directly without a browser redirect — no additional setup is required for Editor testing.

### Google Play external link validation (dev/QA only)

If testing on Android and running into external link validation issues, the following scripting define can be added for dev/QA builds only:

```
IAP_SKIP_EXTERNAL_LINK_VALIDATION
```

**Location:** Edit > Project Settings > Player > Scripting Define Symbols

**WARNING:** Do not include this define in production builds — it violates Google Play policies.

## Step 7 — Purchase Handling

Purchase handling follows the same save-before-confirm contract as standard IAP 5. See **Step 5 — Purchase Handling Contract** in `path-add-iap-to-new-project.md` for the full rules.

IAP D2C Capabilities-specific notes:

- **Checkout presentation mode:** By default, the payment flow opens in the device's external browser. Unity IAP 5.4 also supports an in-app WebView via `CheckoutPresentationMode`. Set the mode on `IPaymentProvidersExtendedService` before purchase:

  ```csharp
  // External browser (default)
  store.PaymentProviderStoreExtendedService?.SetCheckoutPresentationMode(CheckoutPresentationMode.ExternalBrowser);

  // In-app WebView
  store.PaymentProviderStoreExtendedService?.SetCheckoutPresentationMode(CheckoutPresentationMode.WebView);
  ```

  When using `ExternalBrowser`, the game is suspended during payment and resumes via the deep link redirect. When using `WebView`, the game remains active and the WebView is dismissed on completion. `OnPurchasePending` fires in both cases after the payment is processed.

- `OnPurchaseDeferred` fires if the payment is not immediately completed. Do not grant — show pending UI.
- Always confirm (`ConfirmPurchase`) only after entitlement is granted and saved. For consumables, Unity IAP D2C Capabilities prevents re-purchase until the previous order is confirmed.

### Apple and Google external purchase compliance

Apple and Google allow external web payments only in select regions and under their own program rules. Unity does not perform compliance on your behalf — the developer is responsible for eligibility and disclosure requirements.

Three compliance tools are available:

**1. Gate purchases with a compliance callback**

Register a callback via `SetComplianceCheck` that runs before any purchase is initiated. Return `false` to block the purchase for ineligible players:

```csharp
store.PaymentProvidersExtendedPurchaseService?.SetComplianceCheck(async (context) =>
{
    bool isEligible = await CheckRegionalEligibility(context);
    return isEligible;
});
```

The callback runs for `PurchaseProduct` and `Purchase` calls only — it does **not** gate `GenerateURL`.

**2. Disclose the checkout URL before opening it**

Some Apple and Google program rules require showing or disclosing the checkout URL to the player before redirecting. Use `GenerateURL` to obtain the URL without opening it, then call `PurchaseProduct` separately to open the checkout:

```csharp
// Get the URL without opening checkout
string url = await store.PaymentProvidersExtendedPurchaseService.GenerateURL(catalogListingId);

// Disclose or display the URL to the player, then proceed
store.PaymentProvidersExtendedPurchaseService.PurchaseProduct(catalogListingId, PaymentProvider.Name);
```

If `GenerateURL` was already called for the same listing, `PurchaseProduct` reuses the same order — no duplicate charge.

**3. Attach Apple/Google transaction tokens**

Some programs require reporting transaction tokens back to Apple or Google. Attach tokens when calling `GenerateURL`:

```csharp
var tokens = new List<PaymentProviderToken>
{
    new PaymentProviderToken { store = "apple", token = appleToken, type = "acquisition" },
    new PaymentProviderToken { store = "google", token = googleToken }
};
await store.PaymentProvidersExtendedPurchaseService.GenerateURL(catalogListingId, tokens);
```

Tokens are stored with the order and surfaced in the `order.paid` webhook payload under `externalTransactionTokens`. You can supply up to two tokens per order (e.g., one for EU, one for Japan).

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

These cannot be automated and must be completed by the developer:

1. **Product catalog deployment** — deploy `.ucat` / `.catalog.csv` files to the Remote Catalog from the Editor (see Step 3). Alternatively, products can be authored directly in the Unity Cloud IAP dashboard.
2. **Payment provider account** — connect Stripe or Coda account in Unity Cloud IAP dashboard (**IAP > Payment Providers > Connect**). Request enablement from Unity Client Partner with your organization ID if not yet enabled.
3. **Redirect URLs** — set the Success Redirect URL (and optionally Cancel Redirect URL) in the payment provider dashboard (e.g., `mygame://iapresult/okay`). The Cancel Redirect URL adds a back button to the checkout page.
4. **Entitlement delivery method** — in the Unity Dashboard under **IAP > Payment Providers > Entitlement Delivery Method**, select how purchases are fulfilled server-side:
   - **Your own server webhooks** — enter your webhook URL; Unity IAP sends `order.paid` events for server-side grant and fulfillment.
   - **Cloud Code module** — select a deployed Cloud Code module and endpoint to handle fulfillment without managing your own backend.
   - If client-side only (SDK fulfillment via `ConfirmPurchase`), no configuration is needed here — but note that server-side fulfillment is recommended for production.
5. **Routing rules** — in the Unity Dashboard under **IAP > Payment Providers > Provider routing**, add at least one routing rule to activate a payment provider for your players. Without a routing rule no payment provider is offered, even if a provider account is connected. Configure platform, country, and rollout percentage per rule.
6. **Environment selection** — confirm the correct environment (Development / Staging / Production) is set in **Edit > Project Settings > Services > Environments**.
7. **Android deep link verification** — if using URL-scheme deep links, complete Google Digital Asset Links domain verification. (Avoided if using app-scheme deep links.)
8. **Platform compliance review** — external web payments and third-party payment providers are permitted in select regions only. Developer is responsible for determining eligibility and meeting Apple and Google program requirements before shipping.
9. **Prohibited business / content review** — review Stripe's Prohibited/Restricted Businesses list and Coda's Prohibited Content Policy for compliance.
