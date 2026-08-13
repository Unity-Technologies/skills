---
name: localization
description: "Sets up and configures Unity Localization, including locales, String/Asset Tables, CJK font support, and Addressables workflows. Use when the user wants to add languages to a project, translate UI text, support Asian (CJK) languages with TMP fonts, or mentions i18n, l10n, multilingual support, or making a game support multiple languages."
---

This guide covers setting up and configuring Unity Localization, including locales, String and Asset Tables, Addressables integration, and CJK font support via Asset Tables.

## 0. Package Installation Check
Before doing anything else, verify that the Localization packages is installed. Many APIs in this skill will fail silently or throw confusing errors if the package isn't present.
1. **Check:** Use `UnityEditor.PackageManager.Client.List(true)` to check for `com.unity.localization`.
2. **Install:** If missing, use `Client.Add("com.unity.localization")`.
3. **Wait:** Do not proceed until `Client.List` confirms installation.

## 1. Localization Settings & Locales
If `LocalizationEditorSettings.ActiveLocalizationSettings` is null, you must find or create it:
1. **Find:** Use `AssetDatabase.FindAssets("t:LocalizationSettings")`. If found, load the first one and assign it to `LocalizationEditorSettings.ActiveLocalizationSettings`.
2. **Create:** If not found, create a new instance and save it to `Assets/Localization/LocalizationSettings.asset`. Use `ScriptableObject.CreateInstance<LocalizationSettings>()` followed by `AssetDatabase.CreateAsset()`.
3. **Activate:** Set `LocalizationEditorSettings.ActiveLocalizationSettings = settings`.
4. **Locales:** Ensure locales (en, fr, de, etc.) exist. Create them if missing and add them to settings using `LocalizationEditorSettings.AddLocale(locale)`.

## 2. Modifying Localization Tables
Programmatic changes to String or Asset tables require notification to the Editor.
Always create the required asset tables, unless there is already an existing one in the project.

### **Safe Population Pattern**
When populating tables from a dataset, match by `Locale.Identifier.Code` explicitly. The order of `GetLocales()` is not guaranteed to match your input data array — assuming it does will cause silent data mismatches that are very hard to debug.
For **Asset Tables**, use the GUID of the asset: `table.GetEntry(sharedId) ?? table.AddEntry(sharedId, guid);`.

### **Refresh & Notification**
After any modification (adding keys, updating values), notify the Editor so it can refresh its internal state. Skipping this will leave the Editor showing stale data until the next reimport.
1. Call `EditorUtility.SetDirty(collection)`, `EditorUtility.SetDirty(collection.SharedData)`, on each modified `Table`.
2. **Unity 6+ Notification:** `LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(sender, collection);`
3. Always call `AssetDatabase.SaveAssets()` at the end.

## 3. UI Localization and Layout
### **Namespacing & Conflicts**
- **Always qualify names:** Use `UnityEngine.UI.Image`, `UnityEngine.UI.VerticalLayoutGroup`, `UnityEngine.UI.ScrollRect`, `UnityEngine.UI.Mask`, `UnityEngine.UI.CanvasScaler`, `UnityEngine.UI.GraphicRaycaster`, `UnityEngine.UI.ContentSizeFitter`, `UnityEngine.UI.LayoutRebuilder`, etc. 
- `UnityEngine.UI` is both a namespace and a class container, so unqualified names produce `CS0118` (namespace used like a type). Full qualification avoids this entirely.
- **Single Instance:** Always check `GameObject.Find("YourCanvasName")` and destroy the old one before creating a new one.
- **No Debug Dropdown:** NEVER create a manual UI dropdown or debug menu to change the locale. The Localization package has a built-in way to do this properly (e.g., via the "Localization Scene Controls" window for previews).

### **Localized String Events (Robust Binding)**
- **Check Component Type:** Identify if the target is `TextMeshPro` or legacy `UnityEngine.UI.Text`.
- **Bind Correctly:** add the public `UnityEngine.Localization.Components.LocalizeStringEvent`
  component and wire it yourself — set `StringReference` to the table entry, then add an
  `OnUpdateString` listener that assigns the value to the text component (`TMP_Text.text` for
  TextMeshPro, `UnityEngine.UI.Text.text` for legacy Text).

  Do **not** reflect into `UnityEditor.Localization.Plugins.TMPro.LocalizeComponent_TMPro` or its
  UGUI counterpart. Those are `internal` (measured on Localization 1.5.12), so reaching them means
  routing around access control to reach an API Unity makes no stability commitment about — it can
  change or disappear in any package release. `LocalizeStringEvent` is public and does the same job
  with the wiring made explicit.
- **Layout Rebuild:** After setting localized text or populating a list, call `UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentTransform)` to ensure dimensions update.

## 4. Asian Language Font Support (CJK)
Avoid TMP Fallback Fonts for CJK locales. Use **Asset Table Font Swapping** for each specific locale instead — fallbacks are unreliable and hard to debug when glyphs are missing.

1. **Use locale-specific fonts:** Western fonts like Arial or Liberation Sans don't contain CJK glyphs, which results in "tofu" (square blocks). Always use a font designed for the target language:
   - For **Simplified Chinese (zh-Hans)**: Use `msyh.ttc` (Microsoft YaHei) or equivalent.
   - For **Japanese (ja)**: Use `msgothic.ttc` (MS Gothic) or equivalent.
   - For **Korean (ko)**: Use `malgun.ttf` (Malgun Gothic) or equivalent.
   - If system font copying fails, stop and report it. Do not substitute with a Western font.
2. **Robust Font Creation:** Create dynamic `TMP_FontAsset` from imported fonts.
3. **Multi-Atlas & Dynamic:** CJK character sets are too large for static atlases; a single atlas will run out of space immediately.
   - `fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;`
   - `fontAsset.isMultiAtlasTexturesEnabled = true;`
4. **Sub-Assets:** Save atlas and material as sub-assets, or they'll be lost on reimport: `AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);`. 
    - Explicitly link the material's texture: `fontAsset.material.mainTexture = fontAsset.atlasTexture;` and set both as dirty before saving.
5. **Addressables:** Every asset referenced in an Asset Table must be marked as Addressable. 
    - Do not reference assets inside a `Resources/` folder in an Asset Table. This causes `OperationException: Failed to load sub-asset` errors. If an asset is in `Resources/`, copy it to `Assets/Fonts/` or similar before making it Addressable.
    - If a font asset is deleted and recreated, the new GUID must be manually updated in the Asset Table and re-added to Addressables.
6. **Specialized Types:** For TextMesh Pro font swapping, prefer `LocalizedTmpFont` over `LocalizedAsset<TMP_FontAsset>` to avoid implicit conversion errors.
7. **Build Requirement:** After updating Asset Tables or Addressable groups, trigger a build: `AddressableAssetSettings.BuildPlayerContent();`.

### **Verification Step**
Before concluding any CJK localization task:
1. **The Tofu Check:** Switch the editor locale to `zh-Hans`, `ja`, and `ko`. Inspect the UI. If any characters appear as squares (tofu), the font setup has FAILED.
2. **Asset Table Check:** Verify that the `AssetTable` for the CJK locale points to the correct CJK `TMP_FontAsset`, NOT a default Western font.
3. **Multi-Atlas Check:** Confirm `isMultiAtlasTexturesEnabled` is `true` on the CJK font assets.

## 5. Automatic Layout (UGUI)
- **Parent:** `VerticalLayoutGroup` with `Child Control Height: True`, `Child Force Expand Height: False`.
- **Labels:** Each label must have a `ContentSizeFitter` set to `Vertical Fit: Preferred Size`.
- **TMP:** Set `Enable Word Wrapping: True` and `Overflow: Overflow`.

### Notes when translating an existing project
- **Minimal Code Changes**: Never modify code unrelated to localization. Use a static helper class (e.g., `L10n`) to wrap `LocalizationSettings.StringDatabase.GetLocalizedString` for easy injection into existing scripts.
- **Robust Mapping Strategy**: When mapping existing UI text to keys, sort keys by string length (descending) and match longest strings first. This prevents short strings (like "NO") from matching parts of longer sentences. Use case-insensitive matching where appropriate.
- **Component Event Listeners**: When setting up `LocalizeStringEvent` via script, avoid `UnityEventTools.AddPersistentListener` as it often fails to set the dynamic mode (Mode 0) correctly. 
Instead, use the **SerializedObject Pattern** described in Section 3 to explicitly set `m_MethodName` to `set_text` and `m_Mode` to `0`. Persistent listeners **MUST** point to a method on a `UnityEngine.Object`; lambdas will fail.
- **Initialization & Refresh**: 
    - `LocalizationEditorSettings.CreateStringTableCollection` expects a **directory path** (e.g., `Assets/Localization`), not a full asset path.
    - Always call `lEvent.RefreshString()` after assigning a `LocalizedString` reference programmatically to update the UI immediately.
    - Ensure keys are added to **all** tables in a collection (en, de, ja, etc.) to avoid "No translation found" errors.
- **Namespaces & Linq**: Always include `using System.Linq;` when searching collections and `using UnityEngine.Localization;` when working with locales or tables.
- **Verification**: After modifying tables or addressables, run `AddressableAssetSettings.BuildPlayerContent()` and switch the Editor locale to verify changes. Check `LocalizationSettings.Instance` status after activation.
- **Smart Strings**: Set up smart strings where needed. Inspect the context of each string by taking the entire UI it is on, and any scripts that affect it, into account. Set the context on the string table to ensure translations make sense.

## 6. Recommended Translation Strategy
To efficiently translate an existing project, follow this multi-step workflow:

1. **Extraction & Component Setup:**
   - **Find all occurrences:** Scan all scenes and prefabs for strings in code and UI components (Legacy `UnityEngine.UI.Text`, `TextMeshPro`, buttons, etc.).
   - **Shared Table:** Create a central String Table (e.g., `UIStrings`) with the base language and a "Context" column for each key to guide translators.
   - **Attach Components:** For every UI element found, attach a `LocalizeStringEvent` (for text) and a `LocalizedFont` helper (for font swapping).
   - **Validation:** Ensure these components are set up with persistent listeners (`EditorAndRuntime`) so they update in the Editor immediately when the locale changes.

2. **Context-Aware Translation:**
   - **Translate:** Once the table is populated, provide translations for each locale.
   - **Context is King:** Always refer to the "Context" column or inspect the UI layout to ensure the translation fits the intended meaning and space.
   - **Grammar & Tone:** Ensure the tone matches the game's style. For example, use imperative verbs for buttons (e.g., German: "Lauf!" instead of "Laufen") and correct pluralization for labels (e.g., "Punkte" instead of "Punkt").

3. **Quality Assurance (QA):**
   - **Scene Controls:** Use `Window > Asset Management > Localization Scene Controls` or script: `LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("de");`.
   - **Visual Inspection:** Methodically inspect every prefab and scene in the base language and all target languages.
   - **Layout Fit:** Check for text overflows or "tofu" (missing glyphs). Adjust font sizes or use `ContentSizeFitter` if strings are too long.


## API Reference
For detailed API usage, common namespace conflicts, Addressables patterns, and font repair steps, see [references/api-notes.md](references/api-notes.md).

## 7. Accelerated Localization Workflow
To localize an entire project efficiently, use a batch processing script that handles all scenes in one pass.

**Ask before acting:** Before running any batch operation, confirm with the user:
> "This will open every scene in the project, attach `LocalizeStringEvent` components, and save all modified scenes. This cannot be undone automatically. Shall I proceed?"

Only proceed once the user has confirmed. The batch processor template is in [resources/L10nBatchProcessor.cs](resources/L10nBatchProcessor.cs).

### **Technical Tips for Speed**
- **Table References:** Use `TableReference` names (strings) instead of GUIDs — they are easier to read and maintain.
- **Batch Refresh:** Use `LocalizationSettings.Instance.ForceRefresh()` after modifications to force the UI to update in the editor.
- **Font Swap Automation:** Create the `GameAssets` table once and use a script to re-assign `LocalizeFontEvent` to all labels in one pass.
- **LocalizedFontAsset component:** The template is in [resources/LocalizedFontAsset.cs](resources/LocalizedFontAsset.cs).
