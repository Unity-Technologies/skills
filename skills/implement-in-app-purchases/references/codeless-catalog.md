# Editing `IAPProductCatalog.json`

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

## Table of Contents

- [Quick reference — minimal safe edit](#quick-reference--minimal-safe-edit)
- [File location](#file-location)
- [Top-level keys](#top-level-keys)
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

`Assets/Resources/IAPProductCatalog.json` is the file behind the IAP Catalog window, deserialized with `JsonUtility`. For what each product field means, read [Set up Codeless IAP](https://docs.unity.com/en-us/iap/codeless-iap.md) and the IAP Catalog window it describes; this file covers only what you need to edit the JSON directly without corrupting it. You can edit it outside the Editor as long as you follow the serialization rules below.

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

## Top-level keys

The file has five top-level keys: `appleSKU`, `appleTeamID`, `enableCodelessAutoInitialization`, `enableUnityGamingServicesAutoInitialization` and `products`. Each entry in `products` has the fields shown in the [quick reference](#quick-reference--minimal-safe-edit) example, plus optional `storeIDs` overrides (`{ "store": "AppleAppStore", "id": "..." }`), extra `descriptions` per locale, and `payouts` (`{ "t": "Currency", "st": "Gold", "q": 100, "d": "" }`).

The runtime only requires a non-empty `id` per product. The Editor window and the store exporters enforce stricter rules (see [Validation](#validation--run-before-saving)).

## Serialization rules

### JsonUtility round-trip

- Field names are **case-sensitive** and must match the `[SerializeField]` field names in `ProductCatalog.cs` exactly. The visible field names you see in the JSON (e.g. `title`, `description` inside `LocalizedProductDescription`) are the **backing field** names, not the public C# property names (`Title`, `Description`).
- Unknown fields are silently dropped on the next save.
- Missing fields deserialize to default values (`0`, `""`, empty list, etc.) — safe to omit defaults, but the Editor window writes them out explicitly.
- The file is normally a single line — pretty-printing is fine, but JsonUtility re-emits as one line on the next Editor save.
- Do not change the top-level key set. `appleSKU`, `appleTeamID`, `enableCodelessAutoInitialization`, `enableUnityGamingServicesAutoInitialization`, and `products` are all `[SerializeField]`-bound — removing keys is fine (they default), but renaming silently loses data.

### The `Price.data` decimal array

`googlePrice` stores a `decimal` in two mirror fields, `data` and `num`, and **`data` is authoritative on read**: `num` is ignored when the file loads, so a price changed only in `num` silently keeps the old value.

`data` is the `decimal.GetBits` array `[low, mid, high, flags]`, where `flags` holds the number of decimal places shifted left by 16. For a normal price:

```
unscaled = round(price * 10^scale)    // scale = decimal places, usually 2
data     = [unscaled, 0, 0, scale << 16]
```

So `0.99` is `[99, 0, 0, 131072]` and a whole-unit price such as `1000` (yen, won) is `[1000, 0, 0, 0]`. Keep `num` equal to the display price so the file stays readable. If `unscaled` exceeds 2³¹−1 (prices above about 21 million), don't build the bits by hand; hand the user back to the IAP Catalog window.

### Enum integers

- `type` is the `ProductType` integer: `0` Consumable, `1` NonConsumable, `2` Subscription.
- `googleLocale` is the zero-based index into the `TranslationLocale` enum. `21` is `en_US`. For any other locale, read the enum's order in the installed package's `Runtime/Purchasing/Extension/ProductCatalog.cs` under `Library/PackageCache/com.unity.purchasing@*/`, because guessing the index silently assigns the wrong language.
- `payouts[].t` is serialized as a **string** (`"Other"`, `"Currency"`, `"Item"`, `"Resource"`), not an integer.

### Canonical store keys

`storeIDs[].store` must be exactly `AppleAppStore`, `GooglePlay` or `MacAppStore`. Other spellings (`"apple"`, `"google"`) don't fail, but the runtime ignores the override. An empty `storeIDs` array uses the product `id` on every store.

### Non-ASCII characters in descriptions

Both `\uXXXX` escapes and raw UTF-8 load correctly. Write the `\uXXXX` form, because the Editor rewrites the file in that form on its next save and mixed forms make diffs noisy.

## Validation — run before saving

Check each product before writing, because the runtime silently skips invalid ones:

- `id` is non-empty after trimming, and unique in the array.
- `type` is `0`, `1` or `2`, and every `googleLocale` is a valid `TranslationLocale` index.
- `storeIDs[].store` is a canonical store key.
- `googlePrice.data` has four integers, and the sign bit of `data[3]` is 0.
- `enableCodelessAutoInitialization` is a JSON bool, not `0`/`1`.

If the user will export to the stores, two exporter rules catch people out: the Apple export requires a non-empty `screenshotPath`, and the Google export requires `id` (and any `GooglePlay` override) to use only lowercase letters, digits, `_` and `.`, starting with a letter or digit. The IAP Catalog window's App Store Export reports the rest.

## Post-edit refresh

- With the Editor open and not in Play mode, Unity reimports the file on focus, or use **Assets > Refresh**.
- In Play mode the catalog was loaded at start, so exit and re-enter Play mode to see the change.
- A Player build contains the catalog from build time, so rebuild after editing.

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

Default fix: **keep scripted, disable codeless.** Set `"enableCodelessAutoInitialization": false` in the catalog JSON. This is the smallest change, it keeps the user's products and store IDs in the catalog for `ProductCatalog.LoadDefaultCatalog()`, and scripted IAP is the recommended path for production.

Use an alternative only when the user asks for it:

- **Keep codeless, remove scripted init** when the user wants to stay on Codeless IAP buttons. Guard the `StoreController` setup out rather than deleting it.
- **Empty the catalog** (`products: []`) only if the user wants the catalog gone. Codeless auto-init skips an empty catalog even with the flag on, but the product data is lost.

Tell the user about the race and the fix before adding scripted IAP to a project that already has a non-empty catalog.

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
| present (non-empty) | true | present | **Race condition** — tell the user, then apply the default fix from [Auto-init race condition](#auto-init-race-condition). |
| present | false | present | Catalog is dormant unless `ProductCatalog.LoadDefaultCatalog()` is called from user code. Edits safe. |
| present (empty) | true or false | present | Codeless auto-init short-circuits on empty catalog. Treat as scripted-only. |
| absent | n/a | present | Standard scripted IAP. See [path-add-iap-to-new-project.md](path-add-iap-to-new-project.md). |
| absent | n/a | absent | No IAP — go back to the pre-check routing. |
