---
name: localization
description: "Sets up and configures Unity Localization, including locales, String/Asset Tables, CJK font support, and Addressables workflows. Use when the user wants to add languages to a project, translate UI text, support Asian (CJK) languages with TMP fonts, or mentions i18n, l10n, multilingual support, or making a game support multiple languages."
---

This guide covers setting up and configuring Unity Localization, including locales, String and Asset Tables, Addressables integration, and CJK font support via Asset Tables.

## 0. Package Installation Check
Before doing anything else, verify that the Localization package is installed. Many APIs in this skill
will fail silently or throw confusing errors if the package isn't present.

1. **Check by reading the project, not by asking the Package Manager.** Look for
   `com.unity.localization` in **`Packages/packages-lock.json`**. That file records what Unity
   actually resolved, it is plain JSON, and reading it needs no Editor and no async call.
   (`Packages/manifest.json` only records what was *requested*, so check the lock file.)
2. **Install if missing:** `UnityEditor.PackageManager.Client.Add("com.unity.localization")`.
3. **Wait properly.** `Client.Add` and `Client.List` are **asynchronous**: they return a request that
   is still `InProgress` when the call returns, so reading the result in the same statement tells you
   nothing. Do not busy-wait on `IsCompleted` either; that blocks the main thread you are running on.
   Instead, return after firing the install, then **poll `packages-lock.json` in a later call** until
   the id appears. Installation also triggers a domain reload, so expect the first poll or two to
   fail; a fresh install typically resolves in a few seconds.
4. **Confirm the types are actually loaded** before using them, since the lock file can be written
   before the assemblies are ready:
   ```csharp
   var t = System.Type.GetType(
       "UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
   return t != null ? "ready" : "not loaded yet";
   ```
   Only proceed once that returns `ready`.

## 1. Localization Settings & Locales
If `LocalizationEditorSettings.ActiveLocalizationSettings` is null, you must find or create it:
1. **Find:** Use `AssetDatabase.FindAssets("t:LocalizationSettings", new[] { "Assets" })`. If found, load the first one and assign it to `LocalizationEditorSettings.ActiveLocalizationSettings`.
   - **Always pass the search folders.** An unscoped `FindAssets` searches the whole project including
     read-only packages, so it can return an asset from a package and you end up pointing the project
     at something you cannot edit. This applies to every `FindAssets` call in this skill.
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
- **Locale switching: use the package, and keep preview and runtime separate.** These are two
  different mechanisms, and conflating them is why locale switching often ends up hand-rolled.
  - **To preview a locale while authoring**, use the **Localization Scene Controls** window
    (`Window > Asset Management > Localization Scene Controls`). This is Editor-only. It is not a
    runtime feature, so it is not the answer when the game itself needs a language setting.
  - **To switch locale at runtime**, assign `LocalizationSettings.SelectedLocale`. That is the
    supported entry point, and everything bound through `LocalizeStringEvent` updates from it.
  - **To pin which locale the game starts in**, configure a startup locale selector on the
    Localization Settings asset. `SpecificLocaleSelector` is the one that forces a chosen locale;
    the default chain otherwise picks up the system language.
  - **NEVER hand-roll locale state.** A real in-game language menu is fine and expected, as long as
    it sets `SelectedLocale` and lets the package propagate the change. What is forbidden is a debug
    dropdown or menu that tracks its own "current language" variable, swaps strings itself, or
    reaches around the package, because nothing else in the project will follow it.

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

### Prerequisite: TMP Essential Resources must be imported

**Check this before touching any TMP API.** In a project that has never imported them,
`TMP_Settings.instance` is `null` and TMP calls fail with a bare
`NullReferenceException` that names nothing useful. `TMP_FontAsset.CreateFontAsset` is one of them, so
font creation dies on the first line with an error that looks like a bug in your code.

```csharp
// The check.
var ready = TMPro.TMP_Settings.instance != null;
```

If it is not ready, import them **non-interactively**:

```csharp
// Do NOT use EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources").
// It returns true and then opens a dialog that waits for a human, so nothing gets imported and the
// run appears to hang. Import the package directly instead.
string package = null;
var cache = System.IO.Path.GetFullPath(System.IO.Path.Combine(
    UnityEngine.Application.dataPath, "..", "Library", "PackageCache"));
foreach (var dir in System.IO.Directory.GetDirectories(cache))
{
    // TMP ships inside com.unity.ugui in Unity 6, and the folder name carries a version hash,
    // so search for the file rather than hardcoding a path.
    var candidate = System.IO.Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
    if (System.IO.File.Exists(candidate)) { package = candidate; break; }
}
UnityEditor.AssetDatabase.ImportPackage(package, false);   // false = non-interactive
```

Then poll `TMP_Settings.instance != null` in a **later** call, the same way as the package check in
Step 0, and only continue once it is non-null. Verified on Unity 6000.5.8f1: the non-interactive
import completes in a few seconds and the assets land in `Assets/TextMesh Pro`.

1. **Use locale-specific fonts:** Western fonts like Arial or Liberation Sans don't contain CJK glyphs, which results in "tofu" (square blocks). Always use a font designed for the target language:
   - For **Simplified Chinese (zh-Hans)**: Use `msyh.ttc` (Microsoft YaHei) or equivalent.
   - For **Japanese (ja)**: Use `msgothic.ttc` (MS Gothic) or equivalent.
   - For **Korean (ko)**: Use `malgun.ttf` (Malgun Gothic) or equivalent.
   - If system font copying fails, stop and report it. Do not substitute with a Western font.
2. **Robust Font Creation:** Create dynamic `TMP_FontAsset` from imported fonts.
3. **Multi-Atlas & Dynamic:** CJK character sets are too large for static atlases; a single atlas will run out of space immediately.
   - `fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;`
   - `fontAsset.isMultiAtlasTexturesEnabled = true;`
4. **Sub-Assets:** add the atlas textures **and the material**. Adding only the texture is the usual
   mistake, and the material then never reaches the file at all: measured on Unity 6000.5.8f1, a font
   asset saved without the second call contains **zero** `Material` objects on disk, and one with it.
   A material that exists only in memory is not part of the asset, so anything that loads the asset
   fresh gets whatever TMP reconstructs rather than the material you configured, and any setting you
   applied to it is silently gone.
    ```csharp
    // Every atlas texture, not just the first. Step 3 enabled multi-atlas, so there may be several.
    foreach (var atlas in fontAsset.atlasTextures)
    {
        UnityEditor.AssetDatabase.AddObjectToAsset(atlas, fontAsset);
    }
    // The material too. Without this line it is not written into the asset.
    UnityEditor.AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
    ```
    - Explicitly link the material's texture: `fontAsset.material.mainTexture = fontAsset.atlasTexture;`
      and set the font asset, its material, and its textures dirty before saving. (`atlasTexture` is
      the first entry of `atlasTextures`, which is what the primary material draws from, so this is
      consistent with adding every texture above.)
    - **Verify against the file, not against the object you are holding.** After
      `AssetDatabase.SaveAssets()`, call `AssetDatabase.LoadAllAssetsAtPath(path)` and confirm a
      `Material` is among the returned objects. Do not settle for `fontAsset.material != null`: that
      stays true whether or not the material was saved, because TMP will hand back an in-memory one,
      so it cannot tell a saved material from an unsaved one.
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
- **Component Event Listeners**: wire `LocalizeStringEvent.OnUpdateString` with
`UnityEventTools.AddPersistentListener`, passing a delegate built over the text component's public
`text` setter. The setter has no C# method-group name, so build the delegate by name:
`(UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, "set_text")`.
That is reflection over a **public** member, which is fine. See
[resources/L10nBatchProcessor.cs](resources/L10nBatchProcessor.cs) for the working version, including
clearing any existing persistent listeners first so repeated runs don't stack duplicates.
  - Do **not** write the persistent-call fields directly through `SerializedObject` (`m_MethodName`,
    `m_Mode`, `m_PersistentCalls`). Those are private serialized names with no compatibility
    guarantee, and it isn't necessary: `AddPersistentListener` with the delegate above produces the
    same serialized call (target = the text component, method = `set_text`, mode = `EventDefined`).
  - **You must then set the call state, or the label will not update in the Editor.**
    `AddPersistentListener` leaves the call at `UnityEventCallState.RuntimeOnly`, so the binding is
    correct but dormant outside Play mode: switching locale in the Editor changes nothing, and it
    stays that way through a save and reload. Fix it with the public
    `UnityEventBase.SetPersistentListenerState`:
    ```csharp
    UnityEventTools.AddPersistentListener(lse.OnUpdateString, setText);
    var index = lse.OnUpdateString.GetPersistentEventCount() - 1;
    lse.OnUpdateString.SetPersistentListenerState(
        index, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
    ```
    Verified on Unity 6000.5.8f1: without the second call the listener does not fire in Edit mode
    even after a prefab save and reload; with it the call state becomes `EditorAndRuntime` and the
    text updates immediately.
  - Persistent listeners **MUST** point to a method on a `UnityEngine.Object`; lambdas will fail.
  - **Then confirm the binding is live, don't assume it.** Wiring that looks right in the Inspector
    but does nothing is the characteristic failure of this step. All of the read-back you need is
    public API on the event, so none of this requires touching serialized fields:

    | Check | Call | Expect |
    |---|---|---|
    | Something was wired | `GetPersistentEventCount()` | `> 0` |
    | It points at the text component | `GetPersistentTarget(i)`, `GetPersistentMethodName(i)` | the component, `set_text` |
    | It will fire while authoring | `GetPersistentListenerState(i)` | `EditorAndRuntime` |
    | It actually updates the label | `lEvent.RefreshString()` | the text value changes |

    Do all four. A count above zero only proves something was wired, and a `RuntimeOnly` call fails
    the last check while being perfectly correct for a build, so reading the state is what tells a
    dormant binding apart from a broken one. A component that was added and configured but never
    fires is worse than an unlocalized label, because it reads as done.
- **Initialization & Refresh**: 
    - `LocalizationEditorSettings.CreateStringTableCollection` expects a **directory path** (e.g., `Assets/Localization`), not a full asset path.
    - Always call `lEvent.RefreshString()` after assigning a `LocalizedString` reference programmatically to update the UI immediately.
    - Keys must exist with a **non-empty value in every table** of a collection (en, de, ja, …). A key
      that exists with an empty value is the common gap, and it is not silent: the package prints its
      own "No translation found for …" text **into the game UI**, so the shipped screen shows a
      developer message. Do not eyeball this. Run the completeness check below.
- **Namespaces & Linq**: Always include `using System.Linq;` when searching collections and `using UnityEngine.Localization;` when working with locales or tables.
- **Verification**: After modifying tables or addressables, run `AddressableAssetSettings.BuildPlayerContent()` and switch the Editor locale to verify changes. Check `LocalizationSettings.Instance` status after activation.

### Table completeness check (run this before declaring the work done)

Enumerating the tables answers "did every key get a value in every locale" mechanically, so a missing
entry is found before anyone plays the game. Run it and report the output.

```csharp
var gaps = new System.Collections.Generic.List<string>();
var checkedCount = 0;

foreach (var col in UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections())
{
    foreach (var key in col.SharedData.Entries)
    {
        foreach (var table in col.StringTables)
        {
            checkedCount++;
            var entry = table.GetEntry(key.Id);
            // A missing entry and a present-but-empty entry both show as untranslated in game.
            if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            {
                gaps.Add($"{col.TableCollectionName} / {table.LocaleIdentifier.Code} / {key.Key}");
            }
        }
    }
}

// Zero entries is not a pass. It means no collections or no locales were found, so the check
// examined nothing: report that distinctly instead of letting it read as success.
if (checkedCount == 0)
{
    return "INCONCLUSIVE: no table entries found. Either no String Table Collection exists yet, "
         + "or the collection has no locale tables. Fix that before trusting this check.";
}

return gaps.Count == 0
    ? $"COMPLETE: {checkedCount} entries checked, no gaps"
    : $"GAPS ({gaps.Count} of {checkedCount} checked):\n  " + string.Join("\n  ", gaps);
```

Verified on Unity 6000.5.8f1 against a table with one deliberately emptied `ja` value: it reports
`GAPS 1 of 4` naming exactly that entry, `COMPLETE (4 checked)` once the value is filled, and
`INCONCLUSIVE` in a project with no tables.

**Report the gap list rather than resolving it silently.** Some gaps are decisions, not mistakes: a
locale you were not asked to translate, or a key that is intentionally identical across languages.
Filling those with the English text hides the decision. List them and let the user say which are
intentional.
- **Smart Strings**: Set up smart strings where needed. Inspect the context of each string by taking the entire UI it is on, and any scripts that affect it, into account. Set the context on the string table to ensure translations make sense.

## 6. Recommended Translation Strategy
To efficiently translate an existing project, follow this multi-step workflow:

1. **Extraction & Component Setup:**
   - **Find all occurrences. There are two separate hiding places, and scanning one misses the other.**
     - **Authored text** sits on components in scenes and prefabs: legacy `UnityEngine.UI.Text` and
       TextMeshPro (`TMP_Text`, the base of `TextMeshProUGUI` and `TextMeshPro`). Walk **both**
       families. Measured on a real project: `FindObjectsByType<Text>` found 1 component while
       `FindObjectsByType<TMP_Text>` found 13, so a legacy-only pass reports success having done
       almost nothing.
     - **Text composed in code** never appears on a component at edit time, so no scene walk can see
       it. `scoreLabel.text = $"EXP {value}"` is invisible to every component-based scan and is the
       string that survives a "finished" localization pass. Find it in the C# instead:
       ```bash
       # Assignments and SetText calls that carry a string literal.
       grep -rnE '\.text\s*(=|\+=)\s*\$?"|SetText\(\s*\$?"' --include='*.cs' Assets/
       ```
       That pattern catches plain, interpolated, concatenated and `+=` forms plus `SetText`, and
       deliberately does not match `label.text = someVariable` or `label.text = Localize("KEY")`
       (nothing to extract at the first, already routed at the second). Its blind spot is a literal
       held in a variable or const declared elsewhere; if the count looks low for the project, grep
       that file's string literals too.
   - **Report what you did not convert.** A composed string usually needs a smart string or a format
     argument, which is a judgment call, and some are genuinely not worth localizing. Whichever you
     choose, list every site the scan found alongside whether it was converted, and why not if it
     wasn't. Reporting "localized 24 strings" while nine found sites went untouched is the failure
     mode this list exists to prevent: the work looks finished and the gap only surfaces in a
     screenshot from another locale.
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

It walks **both** `Text` and `TMP_Text`, and `LocalizeAll` **returns the labels it could not match**
(as `scene :: object :: "text"`). Print that list. It is the whole point of the return value: a run
that wires 20 labels and silently leaves 9 alone looks identical to a complete one otherwise. The
list covers authored text only, so pair it with the code scan in Section 6 and the table completeness
check in Section 5.

### **Technical Tips for Speed**
- **Table References:** Use `TableReference` names (strings) instead of GUIDs — they are easier to read and maintain.
- **Batch Refresh:** Use `LocalizationSettings.Instance.ForceRefresh()` after modifications to force the UI to update in the editor.
- **Font Swap Automation:** Create the `GameAssets` table once and use a script to re-assign `LocalizeFontEvent` to all labels in one pass.
- **LocalizedFontAsset component:** The template is in [resources/LocalizedFontAsset.cs](resources/LocalizedFontAsset.cs).
