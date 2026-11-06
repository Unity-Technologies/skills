---
name: localize-unity-project
description: Sets up Unity Localization, including locales, String and Asset Tables, and CJK fonts. Use when the user wants to add languages or translate UI text.
---

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for.

## Terms

These names differ between the Editor UI, the package API and this skill:

- **Table collection**: the asset (`StringTableCollection` or `AssetTableCollection`) holding one **table** per locale plus the shared key list, `SharedData`.
- **`LocalizedTmpFont`**: the package type that references a `TMP_FontAsset` entry in an Asset Table.
- **`LocalizedFontAsset`**: this skill's component template ([scripts/LocalizedFontAsset.cs](scripts/LocalizedFontAsset.cs)), not a package type. It applies a `LocalizedTmpFont` to the `TMP_Text` on its GameObject.

## 1. Check the Localization package is installed
Before doing anything else, verify that the Localization package is installed. Many APIs in this skill
will fail silently or throw confusing errors if the package isn't present.

1. **Check by reading the project, not by asking the Package Manager.** Look for
   `com.unity.localization` in **`Packages/packages-lock.json`**, because it records what Unity
   actually resolved and reading it needs no Editor or async call. `Packages/manifest.json` only
   records what was *requested*.
2. **Install if missing:** `UnityEditor.PackageManager.Client.Add("com.unity.localization")`.
3. **Wait properly.** `Client.Add` is **asynchronous**, and busy-waiting on `IsCompleted` blocks the
   main thread you are running on. Return after firing the install, then **poll `packages-lock.json`
   in a later call** until the id appears. The install triggers a domain reload, so expect the first
   poll or two to fail.
4. **Confirm the types are actually loaded** before using them, since the lock file can be written
   before the assemblies are ready:
   ```csharp
   var t = System.Type.GetType(
       "UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
   return t != null ? "ready" : "not loaded yet";
   ```
   Only proceed once that returns `ready`.

## 2. Read the documentation

### Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.localization@*/`. When reading docs, read only the .md files in that subfolder and don't browse other Library or PackageCache folders, because that takes too much time. This limit doesn't apply to package lookups that other steps run, such as the TMP import in [references/cjk-fonts.md](references/cjk-fonts.md). If that folder has no `.md` files, use the online pages at `https://docs.unity3d.com/Packages/com.unity.localization@<VERSION>/manual/` instead, because some package versions don't ship their docs.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The product version, eg `7000.0`

Read the Unity version from `ProjectSettings/ProjectVersion.txt`, and each package's major.minor from `Packages/packages-lock.json` (for example `1.5` for `com.unity.localization`). TextMesh Pro (TMP) API pages are in the `com.unity.ugui` docs. If `packages-lock.json` lists `com.unity.textmeshpro` instead, use `https://docs.unity3d.com/Packages/com.unity.textmeshpro@<VERSION>/api/` with that package's version.

Read [references/docs-map.md](references/docs-map.md) for the manual, API and Script Reference pages to read.

### API reference

The API is `UnityEngine.Localization` and `UnityEditor.Localization`, part of the `com.unity.localization` package. It is versioned by package version, not Unity version (e.g. `@1.4`, `@1.5`), so check the project's installed package version before relying on a specific class or method.

1. Read the installed package version from `Packages/packages-lock.json`, as in [Step 1](#1-check-the-localization-package-is-installed).
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.localization@<version>/xrefmap.yml` (replace `<version>` with the installed package version, e.g. `1.5`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEngine.Localization.LocalizedTmpFont.html` => `https://docs.unity3d.com/Packages/com.unity.localization@1.5/api/UnityEngine.Localization.LocalizedTmpFont.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions. The same steps work for the TMP (`com.unity.ugui` or `com.unity.textmeshpro`) and `com.unity.addressables` APIs.

For Engine APIs (`UnityEditor.AssetDatabase`, `UnityEditor.PackageManager`, `UnityEngine.Events`), follow the Engine API lookup steps in [references/docs-map.md](references/docs-map.md).

For Asset Table entry, `Resources/` folder and font repair pitfalls, see [references/api-notes.md](references/api-notes.md).

## 3. Localization Settings & Locales
If `LocalizationEditorSettings.ActiveLocalizationSettings` is null, you must find or create it:
1. **Find:** Use `AssetDatabase.FindAssets("t:LocalizationSettings", new[] { "Assets" })`. If found, load the first one and assign it to `LocalizationEditorSettings.ActiveLocalizationSettings`.
   - **Always pass the search folders.** An unscoped `FindAssets` searches the whole project including
     read-only packages, so it can return an asset from a package and you end up pointing the project
     at something you cannot edit. This applies to every `FindAssets` call in this skill.
2. **Create:** If not found, create a new instance and save it to `Assets/Localization/LocalizationSettings.asset`. Use `ScriptableObject.CreateInstance<LocalizationSettings>()` followed by `AssetDatabase.CreateAsset()`.
3. **Activate:** Set `LocalizationEditorSettings.ActiveLocalizationSettings = settings`.
4. **Locales:** Ensure locales (en, fr, de, etc.) exist. Create them if missing and add them to settings using `LocalizationEditorSettings.AddLocale(locale)`.

## 4. Modifying Localization Tables
Programmatic changes to String or Asset tables require notification to the Editor.
Always create the required asset tables, unless there is already an existing one in the project.

### **Safe Population Pattern**
When populating tables from a dataset, match by `Locale.Identifier.Code` explicitly. The order of `GetLocales()` is not guaranteed to match your input data array — assuming it does will cause silent data mismatches that are very hard to debug.
For **Asset Tables**, use the GUID of the asset: `table.GetEntry(sharedId) ?? table.AddEntry(sharedId, guid);`.

### **Refresh & Notification**
After any modification (adding keys, updating values), notify the Editor so it can refresh its internal state. Skipping this will leave the Editor showing stale data until the next reimport.
1. Call `EditorUtility.SetDirty(collection)`, `EditorUtility.SetDirty(collection.SharedData)`, on each modified `Table`.
2. **Notify:** `LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(sender, collection);`
3. Always call `AssetDatabase.SaveAssets()` at the end.

## 5. UI Localization and Layout
### **Namespacing & Conflicts**
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
1. **Check Component Type:** Identify if the target is `TextMeshPro` or legacy `UnityEngine.UI.Text`.
2. **Bind Correctly:** add the public `UnityEngine.Localization.Components.LocalizeStringEvent`
  component and wire it yourself — set `StringReference` to the table entry, then add an
  `OnUpdateString` listener that assigns the value to the text component (`TMP_Text.text` for
  TextMeshPro, `UnityEngine.UI.Text.text` for legacy Text).

  Do **not** reflect into `UnityEditor.Localization.Plugins.TMPro.LocalizeComponent_TMPro` or its
  UGUI counterpart, because they are `internal` and can change in any package release.
3. **Layout Rebuild:** After setting localized text or populating a list, call `UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(parentTransform)` to ensure dimensions update.

## 6. Asian Language Font Support (CJK)
Read [references/cjk-fonts.md](references/cjk-fonts.md) before setting up fonts for Chinese, Japanese or Korean locales, and follow it in order.

## 7. Automatic Layout (UGUI)
Translated strings are often much longer than the source text, so a label sized for one language clips or overflows in another.

1. Read the UGUI auto layout pages listed in [references/docs-map.md](references/docs-map.md) before changing a layout, because the right layout group and fitter settings depend on the existing hierarchy.
2. Change only the containers of labels that overflow in at least one locale, because a fixed layout that already fits every locale is the user's design.
3. Verify by switching to each locale and checking `TMP_Text.isTextOverflowing` on the affected labels.

### Notes when translating an existing project
- **Minimal Code Changes**: Never modify code unrelated to localization. Use a static helper class (e.g., `L10n`) to wrap `LocalizationSettings.StringDatabase.GetLocalizedString` for easy injection into existing scripts.
- **Robust Mapping Strategy**: When mapping existing UI text to keys, sort keys by string length (descending) and match longest strings first. This prevents short strings (like "NO") from matching parts of longer sentences. Use case-insensitive matching where appropriate.
- **Component Event Listeners**: wire `LocalizeStringEvent.OnUpdateString` with
`UnityEventTools.AddPersistentListener`, passing a delegate built over the text component's public
`text` setter. The setter has no C# method-group name, so build the delegate by name:
`(UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, "set_text")`.
That is reflection over a **public** member, which is fine. See
[scripts/L10nBatchProcessor.cs](scripts/L10nBatchProcessor.cs) for the working version, including
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

    Do all four, because a binding that is wired but never fires reads as done while leaving the
    label unlocalized.
- **Initialization & Refresh**: 
    - `LocalizationEditorSettings.CreateStringTableCollection` expects a **directory path** (e.g., `Assets/Localization`), not a full asset path.
    - Always call `lEvent.RefreshString()` after assigning a `LocalizedString` reference programmatically to update the UI immediately.
    - Keys must exist with a **non-empty value in every table** of a collection (en, de, ja, …). A key
      that exists with an empty value is the common gap, and it is not silent: the package prints its
      own "No translation found for …" text **into the game UI**, so the shipped screen shows a
      developer message. Do not eyeball this. Run the completeness check below.
- **Verification**: After modifying tables or addressables, run `AddressableAssetSettings.BuildPlayerContent()` and switch the Editor locale to verify changes. Check `LocalizationSettings.Instance` status after activation.

### Table completeness check (run this before declaring the work done)

Read [references/table-completeness-check.md](references/table-completeness-check.md), run the check it contains, and report the output.

- **Smart Strings**: Set up smart strings where needed. Inspect the context of each string by taking the entire UI it is on, and any scripts that affect it, into account. Set the context on the string table to ensure translations make sense.

## 8. Recommended Translation Strategy
Read [references/translation-strategy.md](references/translation-strategy.md) when translating an existing project or finding text that still needs localizing.


## 9. Accelerated Localization Workflow
To localize an entire project efficiently, use a batch processing script that handles all scenes in one pass.

**Ask before acting:** Before running any batch operation, confirm with the user:
> "This will open every scene in the project, attach `LocalizeStringEvent` components, and save all modified scenes. This cannot be undone automatically. Shall I proceed?"

Only proceed once the user has confirmed. The batch processor template is in [scripts/L10nBatchProcessor.cs](scripts/L10nBatchProcessor.cs).

`LocalizeAll` also enforces this itself, so do not remove or bypass these checks when adapting the template: it refuses to run in batch mode, prompts the user to save or discard unsaved scene changes, and shows a blocking Editor dialog that lists the scenes it will rewrite. Tell the user to watch the Editor and click the confirm button; if they cancel, it throws `OperationCanceledException` and writes nothing. Report that as a cancellation, not an error to work around.

It walks **both** `Text` and `TMP_Text`, and `LocalizeAll` **returns the labels it could not match**
(as `scene :: object :: "text"`). Print that list. It is the whole point of the return value: a run
that wires 20 labels and silently leaves 9 alone looks identical to a complete one otherwise. The
list covers authored text only, so pair it with the code scan in [references/translation-strategy.md](references/translation-strategy.md) and the table completeness
check in Step 7.

### **Technical Tips for Speed**
- **Table References:** Use `TableReference` names (strings) instead of GUIDs — they are easier to read and maintain.
- **Batch Refresh:** Use `LocalizationSettings.Instance.ForceRefresh()` after modifications to force the UI to update in the editor.
- **Font Swap Automation:** Create the `GameAssets` table once and use a script to attach the `LocalizedFontAsset` component ([template](scripts/LocalizedFontAsset.cs)) to all labels in one pass.

## Troubleshooting errors

If an error appears in the console or the game UI, look it up in [references/troubleshooting.md](references/troubleshooting.md), apply the resolution, then reread the [docs pages](#full-topic-map) for the step that produced it.

## Final step

Run these checks in order. If any check fails, reread the [docs pages](#full-topic-map) to find out what you missed, fix it, and run the checks again.

1. Run `unity command console status` (the `console_status` check) and confirm `groundTruth.consoleErrors` is 0 and `compilationFailed` is false.
2. Run the [table completeness check](references/table-completeness-check.md) and keep its output for the report.
3. Stop there unless the user asked for proof or a specific fault is suspected, such as a label that doesn't change when the locale changes.
4. If a fault is suspected and the change affects runtime behaviour, enter Play mode once to chase it. Read the world through `eval`, for example `LocalizationSettings.SelectedLocale` and the `text` of the label in question, rather than adding logging. Take no screenshots, because they take too much time. Exit Play mode when you're done.

## Final report

Report these in order:

1. **What changed**: locales, tables, keys, components and font assets you added or changed.
2. **Not converted**: every text site you didn't convert, and why, including `LocalizeAll`'s unmatched labels.
3. **Verification results**: the result of each Final step check, including the table completeness check output and any gaps.
4. **Follow-up**: what the user must do, such as confirm which gaps are intentional, review translations, or rebuild Addressables content.
