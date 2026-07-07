# Editing `IAPProductCatalog.json`

## Table of Contents

- [Quick reference — minimal safe edit](#quick-reference--minimal-safe-edit)
- [File location](#file-location)
- [Schema](#schema)
- [Serialization rules](#serialization-rules)
  - [JsonUtility round-trip](#jsonutility-round-trip)
  - [The `Price.data` decimal array](#the-pricedata-decimal-array)
  - [Enum integers](#enum-integers)
  - [Canonical store keys](#canonical-store-keys)
  - [Non-ASCII characters in descriptions](#non-ascii-characters-in-descriptions)
- [Validation — run before saving](#validation--run-before-saving)
- [Post-edit refresh](#post-edit-refresh)
- [When to escalate to the Editor window](#when-to-escalate-to-the-editor-window)
- [Catalog as part of Codeless IAP](#catalog-as-part-of-codeless-iap)
  - [Codeless vs scripted — which to use](#codeless-vs-scripted--which-to-use)
  - [Auto-init race condition](#auto-init-race-condition)
  - [Pushing the catalog to the storefronts](#pushing-the-catalog-to-the-storefronts)
  - [Detection checklist](#detection-checklist)

`Assets/Resources/IAPProductCatalog.json` is a Unity-managed JSON asset deserialized by `JsonUtility.FromJson<ProductCatalog>` (`Runtime/Purchasing/Extension/ProductCatalog.cs`). It can be edited safely outside the Editor as long as the serialization rules below are respected.

## Quick reference — minimal safe edit

1. **Read** the file as raw text. It is canonical JSON, single-line.
2. **Parse** it into a dict/object.
3. **Modify** fields using the schema and serialization rules below.
4. **Write back** as JSON. Field name spelling and casing must match exactly; preserving order is not required.
5. **Refresh** so the Editor / runtime sees the change (see [Post-edit refresh](#post-edit-refresh)).

Adding a new $1.99 consumable `gems_50` in one operation:

```jsonc
{
  "id": "gems_50",
  "type": 0,
  "storeIDs": [],
  "defaultDescription": { "googleLocale": 21, "title": "50 Gems", "description": "A small pouch of gems." },
  "screenshotPath": "",
  "applePriceTier": 0,
  "googlePrice": { "data": [199, 0, 0, 131072], "num": 1.99 },
  "pricingTemplateID": "",
  "descriptions": [],
  "payouts": []
}
```

Append to the `products` array, save, then trigger a refresh.

## File location

- **Current:** `Assets/Resources/IAPProductCatalog.json` (constant `ProductCatalog.kCatalogPath`)
- **Legacy:** `Assets/Plugins/UnityPurchasing/Resources/IAPProductCatalog.json` (constant `ProductCatalog.kPrevCatalogPath`) — auto-migrated on Editor load via `ProductCatalogEditor.MigrateProductCatalog()`. If both exist, the current path wins.

`Resources.Load("IAPProductCatalog")` deserializes it at runtime via `ProductCatalogImpl.LoadDefaultCatalog()`. The asset must remain under a `Resources/` directory for runtime load to succeed.

## Schema

```jsonc
{
  "appleSKU": "",                                  // app-level Apple SKU (Apple XML exporter)
  "appleTeamID": "",                               // Apple team ID (Apple XML exporter)
  "enableCodelessAutoInitialization": true,        // auto-init Unity IAP at runtime
  "enableUnityGamingServicesAutoInitialization": false,
  "products": [
    {
      "id": "gold_100",                            // canonical Unity SKU — required, non-empty, unique
      "type": 0,                                   // ProductType int (see Enum integers)
      "storeIDs": [                                // per-store override SKUs
        { "store": "AppleAppStore", "id": "com.example.gold100" }
      ],
      "defaultDescription": {                      // required for export
        "googleLocale": 21,                        // TranslationLocale int (en_US = 21)
        "title": "",
        "description": ""
      },
      "screenshotPath": "",                        // Apple screenshot path (optional)
      "applePriceTier": 0,                         // Apple price tier (Apple XML exporter)
      "googlePrice": { "data": [99,0,0,131072], "num": 0.99 },  // see Price.data
      "pricingTemplateID": "",                     // Google Play pricing template
      "descriptions": [                            // additional locale variants
        { "googleLocale": 30, "title": "...", "description": "..." }
      ],
      "payouts": [                                 // optional grant metadata
        { "t": "Currency", "st": "Gold", "q": 100, "d": "" }
      ]
    }
  ]
}
```

Per-product required fields for the runtime to accept the product: `id` (non-empty, trimmed). Everything else is optional at runtime; exporters and the Editor window enforce stricter rules (see [Validation](#validation--run-before-saving)).

## Serialization rules

### JsonUtility round-trip

- Field names are **case-sensitive** and must match the `[SerializeField]` field names in `ProductCatalog.cs` exactly. The visible field names you see in the JSON (e.g. `title`, `description` inside `LocalizedProductDescription`) are the **backing field** names, not the public C# property names (`Title`, `Description`).
- Unknown fields are silently dropped on the next save.
- Missing fields deserialize to default values (`0`, `""`, empty list, etc.) — safe to omit defaults, but the Editor window writes them out explicitly.
- The file is normally a single line — pretty-printing is fine, but JsonUtility re-emits as one line on the next Editor save.
- Do not change the top-level key set. `appleSKU`, `appleTeamID`, `enableCodelessAutoInitialization`, `enableUnityGamingServicesAutoInitialization`, and `products` are all `[SerializeField]`-bound — removing keys is fine (they default), but renaming silently loses data.

### The `Price.data` decimal array

`Price` is the trickiest field. It serializes a `decimal` via two mirror fields and **`data` is authoritative on read**:

```csharp
public void OnBeforeSerialize() {
    data = decimal.GetBits(value);        // int[4]: [low, mid, high, flags]
    num  = decimal.ToDouble(value);
}
public void OnAfterDeserialize() {
    if (data != null && data.Length == 4)
        value = new decimal(data);        // num is ignored on read
}
```

`data` is the `decimal.GetBits` representation: `[low32 mantissa, mid32 mantissa, high32 mantissa, flags]`.

The `flags` int encodes scale (decimal places) and sign:
- Bits 16–23: scale (0–28)
- Bit 31: sign (0 = positive, 1 = negative; negative prices are never valid here)
- All other bits: unused, must be 0

For scale 2 (cents-style prices): `flags = 0x00020000 = 131072`
For scale 0 (yen / whole-unit prices): `flags = 0`

| Price (display) | Mantissa | Scale | `data` |
|---|---|---|---|
| 0.99 | 99 | 2 | `[99, 0, 0, 131072]` |
| 1.99 | 199 | 2 | `[199, 0, 0, 131072]` |
| 2.99 | 299 | 2 | `[299, 0, 0, 131072]` |
| 4.99 | 499 | 2 | `[499, 0, 0, 131072]` |
| 9.99 | 999 | 2 | `[999, 0, 0, 131072]` |
| 19.99 | 1999 | 2 | `[1999, 0, 0, 131072]` |
| 99.99 | 9999 | 2 | `[9999, 0, 0, 131072]` |
| 1000 (¥, ₩) | 1000 | 0 | `[1000, 0, 0, 0]` |

Formula for arbitrary positive prices with up to 2³¹−1 unscaled units:

```
unscaled = round(price * 10^scale)           // e.g. 2.99 with scale=2 → 299
data     = [unscaled, 0, 0, scale << 16]
```

Keep `num` in sync with the display price even though it's not read — the Editor reads it diagnostically and the file diffs more readably.

For prices larger than ~$21M (unscaled > 2³¹−1), the high two ints carry the overflow — at that point invoke Unity in batchmode and let `Price.OnBeforeSerialize` do the conversion, rather than constructing the bits by hand.

### Enum integers

`ProductType` (`type` field):

| Int | Enum | Use for |
|---|---|---|
| 0 | `Consumable` | Coins, gems, lives, ammo — granted then consumed |
| 1 | `NonConsumable` | One-time unlocks, remove-ads, character packs |
| 2 | `Subscription` | Recurring entitlements |

`TranslationLocale` (`googleLocale` field): the int is the zero-based index into the enum declared in `Runtime/Purchasing/Extension/ProductCatalog.cs`. Common values:

| Int | Locale |
|---|---|
| 13 | `zh_CN` |
| 14 | `zh_TW` |
| 17 | `da_DK` |
| 18 | `nl_NL` |
| 21 | `en_US` (default for new descriptions) |
| 22 | `en_GB` |
| 30 | `fr_FR` |
| 33 | `de_DE` |
| 41 | `it_IT` |
| 42 | `ja_JP` |
| 46 | `ko_KR` |
| 63 | `pl_PL` |
| 64 | `pt_BR` |
| 69 | `ru_RU` |
| 75 | `es_ES` |

For anything outside this list, count from the top of the `TranslationLocale` enum in `ProductCatalog.cs` — the order is the index.

`ProductCatalogPayoutType` (`payouts[].t` field): **serialized as a string**, not an int. Valid values: `"Other"`, `"Currency"`, `"Item"`, `"Resource"`.

### Canonical store keys

`storeIDs[].store` must be one of the keys validated by `ProductCatalogEditor.kStoreKeys`:

- `AppleAppStore`
- `GooglePlay`
- `MacAppStore`

Using `"Apple"`, `"apple"`, `"google"`, etc. won't crash but the override will not be picked up by the runtime store routing. An empty `storeIDs` array means the product's `id` is used as the platform SKU on every store.

### Non-ASCII characters in descriptions

`LocalizedProductDescription.Title` / `Description` setters encode any character with code point > 127 as `\uXXXX` (`EncodeNonLatinCharacters` in `ProductCatalog.cs`). The getter regex-decodes both forms. So both representations work on read:

```jsonc
{ "title": "Caf\\u00e9 Pack" }   // Editor-written form
{ "title": "Café Pack" }          // raw UTF-8 — also accepted on read
```

**Prefer the `\uXXXX` form** when writing programmatically, to match what the Editor will round-trip the file into on the next save. Mixed forms in one file are fine but the next Editor save normalizes everything to escaped form.

Subtype `payouts[].st` has a 64-char max (`ProductCatalogPayout.MaxSubtypeLength`). Subtype `payouts[].d` (data) has a 1024-char max (`MaxDataLength`).

## Validation — run before saving

These mirror the Editor window's validation. Verify each before writing:

**Per product:**
- `id` is non-empty after trim. (`ProductCatalog.allValidProducts` filters on this.)
- `id` is unique across all products in the array (the Editor flags duplicates).
- `type` ∈ `{0, 1, 2}`.
- Every `defaultDescription.googleLocale` and `descriptions[].googleLocale` is a valid int index into `TranslationLocale`.
- `storeIDs[].store` ∈ canonical store keys.
- If `googlePrice.data` is present, it is a 4-element int array; sign bit (bit 31 of `data[3]`) is 0.

**Per-exporter (only if export is intended):**

- **Apple XML** (`AppleXMLProductCatalogExporter.Validate`):
  - Catalog-level: `appleSKU` non-empty, `appleTeamID` non-empty, no duplicate product IDs, no duplicate Apple store IDs, no duplicate runtime IDs.
  - Per item: `id` non-empty, `defaultDescription.Title` non-empty, `defaultDescription.Description` non-empty, **`screenshotPath` non-empty (required, not optional)**.
  - `applePriceTier` is written to the XML (`<wholesale_price_tier>`) but not validated — any int is accepted.
- **Google CSV** (`GooglePlayProductCatalogExporter.Validate`):
  - Catalog-level: no duplicate product IDs, no duplicate Google store IDs, no duplicate runtime IDs.
  - Per item: `id` non-empty AND must start with a lowercase letter or digit AND contain only `a-z`, `0-9`, `_`, `.` (same rule applies to `storeIDs[].id` for the `GooglePlay` override).
  - Description rules (apply to `defaultDescription` and every entry in `descriptions`):
    - `Title` non-empty; ≤ 55 chars (error if longer); warning if > 25 chars.
    - `Description` non-empty; ≤ 80 chars (error if longer).
  - Price: either `googlePrice.value` ≠ 0 (i.e. non-zero `data` mantissa) **or** `pricingTemplateID` non-empty.

**Top-level:**
- `enableCodelessAutoInitialization` is a bool, not 0/1.
- If the catalog ends up empty (no products with non-empty `id`), `CodelessIAPStoreListener.InitializeCodelessPurchasingOnLoad` short-circuits — flipping the auto-init flag in that state has no effect until a product is added.

## Post-edit refresh

The Unity Editor caches `IAPProductCatalog` as a `TextAsset` keyed by Resources path. After writing the JSON externally:

- **Editor open, not in Play Mode:** Unity detects external changes on next focus and re-imports the asset automatically (`AssetDatabase`'s file watcher). If you want to force it from the Editor side, run **Assets → Refresh** (Ctrl/Cmd+R). The next `Resources.Load("IAPProductCatalog")` call will see the new contents.
- **Editor open, in Play Mode:** the runtime `TextAsset` was loaded at play-start and is cached. Changes won't be picked up until play mode is restarted (or you re-call `Resources.Load` after `AssetDatabase.ImportAsset` to bypass the cache).
- **Editor closed:** edits are picked up next time the Editor opens.
- **Player build:** the catalog is baked into the build at build-time. Edits after build do nothing — rebuild required.

For batchmode-driven workflows that need a guaranteed refresh:

```
"<unity>/Unity.exe" -batchmode -quit -projectPath "<path>" -logFile -
```

Plain batchmode without `-executeMethod` is enough to trigger asset import on startup. The next Editor session will see the change.

## When to escalate to the Editor window

Hand the user back to the IAP Catalog window (Services → In-App Purchasing → IAP Catalog…) when:

- The decimal price is larger than ~$21M (would need the high mantissa ints — let `Price.OnBeforeSerialize` do it).
- You need to set up many `payouts` with custom subtypes — the Editor has inline validation on lengths.
- The user wants to use **App Store Export** (Apple XML / Google CSV) — that flow is a Unity dialog with its own validation results panel.
- You hit a JsonUtility round-trip error you can't diagnose from the schema rules above.

For everything else — adding/removing products, changing prices in normal ranges, toggling auto-init flags, updating descriptions, adding store ID overrides — direct file edits are safe and faster than driving the Editor UI.

---

## Catalog as part of Codeless IAP

The catalog file plays two distinct roles in the package. Knowing which one applies in the current project changes how edits are made and what side-effects to expect.

### Codeless vs scripted — which to use

| Use case | Path |
|---|---|
| Ship a buy button with zero C# | **Codeless** — catalog + `CodelessIAPButton` |
| Custom UI states, server validation, granular control of the purchase flow | **Scripted** — `StoreController` + an `IAPManager` (see [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md)) |
| Catalog-managed product list + scripted purchase flow | **Mixed** — catalog drives `CodelessIAPStoreListener` for product registration, scripted code subscribes to its events |
| Export products to Apple Application Loader / Play Console | Codeless catalog — the only built-in export path |

Default recommendation when starting a new project from scratch: scripted IAP. Codeless is convenient for prototypes but couples the project to a global singleton (`CodelessIAPStoreListener.Instance`) and hides the init/purchase flow most production games eventually need to customize.

### Auto-init race condition

If the project has **both** a non-empty `IAPProductCatalog.json` with `enableCodelessAutoInitialization: true` **and** a scripted `StoreController` initialized in user code, two init paths race for the same native store. Symptoms:

- Duplicate `OnPurchasePending` callbacks (one per init's listener set).
- "Store already connected" warnings on `Connect()`.
- Unpredictable which init's product list wins.

Mitigations (pick one):

1. **Keep scripted, disable codeless:** set `"enableCodelessAutoInitialization": false` in the catalog JSON. The catalog stays available for `ProductCatalog.LoadDefaultCatalog()` calls.
2. **Keep codeless, remove scripted init:** delete the user's `StoreController` setup; rely on `CodelessIAPStoreListener.Instance`.
3. **Empty the catalog:** clear `products` to `[]`. `CodelessIAPStoreListener.InitializeCodelessPurchasingOnLoad` short-circuits on empty catalogs even with the flag on.

Surface this before adding scripted IAP to a project that already has a non-empty catalog.

### Pushing the catalog to the storefronts

The IAP Catalog window's **App Store Export** button opens `ProductCatalogExportWindow`, which generates bulk-import files. Nothing in the package calls App Store Connect or Play Console APIs directly — devs upload the generated files manually.

| Exporter | Output | Upload destination |
|---|---|---|
| `AppleXMLProductCatalogExporter` | Application Loader XML | App Store Connect → Transporter / Application Loader |
| `GooglePlayProductCatalogExporter` | CSV | Play Console → In-app products → Import |

Editing the JSON directly populates the same fields the exporters read; the validation surface in the Editor window (`ExporterValidationResults`) is the user-visible signal that fields are missing.

### Detection checklist

When triaging an IAP issue or planning changes, check in order:

1. Does `Assets/Resources/IAPProductCatalog.json` exist?
2. Is `enableCodelessAutoInitialization` true? (`grep enableCodelessAutoInitialization Assets/Resources/IAPProductCatalog.json`)
3. Are there `CodelessIAPButton` or legacy `IAPButton` components in scenes/prefabs?
4. Does user code construct a `StoreController` directly (or call `UnityIAPServices.StoreController(...)`)?
5. Does user code reference `CodelessIAPStoreListener.Instance`?

Routing the result:

| Catalog | autoInit | Scripted `StoreController` | Action |
|---|---|---|---|
| present (non-empty) | true | absent | Pure codeless — edit the catalog freely. |
| present (non-empty) | true | present | **Race condition** — surface to the user, apply one of the three mitigations. |
| present | false | present | Catalog is dormant unless `ProductCatalog.LoadDefaultCatalog()` is called from user code. Edits safe. |
| present (empty) | true or false | present | Codeless auto-init short-circuits on empty catalog. Treat as scripted-only. |
| absent | n/a | present | Standard scripted IAP. See [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md). |
| absent | n/a | absent | No IAP — see [pre-check.md](pre-check.md) routing. |
