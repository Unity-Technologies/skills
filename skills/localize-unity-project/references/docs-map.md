# Localization docs map

## Pages

Localization manual, local at `<project-folder>/Library/PackageCache/com.unity.localization@*/Documentation~/` (online fallback: `https://docs.unity3d.com/Packages/com.unity.localization@<VERSION>/manual/`, same names with `.html`):
- `QuickStartGuideWithVariants.md`: end-to-end setup.
- `LocaleSelector.md`: startup locale selectors, including the Specific Locale Selector.
- `StringTables.md` and `AssetTables.md`: table collections and entries.
- `Addressables.md`: how tables and assets use Addressables groups.
- `EditModeSupport.md`: the Localization Scene Controls window and Edit mode events.
- `Smart/SmartStrings.md`: smart strings, plurals and arguments.

Localization API, under `https://docs.unity3d.com/Packages/com.unity.localization@<VERSION>/api/`:
- `UnityEditor.Localization.LocalizationEditorSettings.html`: settings, locales and collections.
- `UnityEngine.Localization.Settings.LocalizationSettings.html`: `SelectedLocale` and `AvailableLocales`.
- `UnityEngine.Localization.Components.LocalizeStringEvent.html`: binding a string to a label.
- `UnityEngine.Localization.LocalizedTmpFont.html`: the localized TMP font reference.

TMP API, under `https://docs.unity3d.com/Packages/com.unity.ugui@<VERSION>/api/`:
- `TMPro.TMP_FontAsset.html`: `atlasPopulationMode`, `isMultiAtlasTexturesEnabled` and `HasCharacters`.
- `TMPro.TMP_Text.html`: `text` and `isTextOverflowing`.

UGUI manual, under `https://docs.unity3d.com/Packages/com.unity.ugui@<VERSION>/manual/`:
- `UIAutoLayout.html`: how layout groups and layout elements size labels.
- `script-VerticalLayoutGroup.html` and `script-ContentSizeFitter.html`: the components that let labels grow with longer translations.

Addressables API, under `https://docs.unity3d.com/Packages/com.unity.addressables@<VERSION>/api/`:
- `UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.html`: `CreateOrMoveEntry` and `BuildPlayerContent`.

Script Reference, under `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/`:
- `unityeditor/packagemanager/client/add.md`: adding a package from script.
- `unityeditor/assetdatabase/addobjecttoasset.md` and `unityeditor/assetdatabase/loadallassetsatpath.md`: saving and reading back sub-assets.
- `unityeditor/assetdatabase/importpackage.md`: importing a `.unitypackage` without a dialog.
- `unityeditor/events/unityeventtools/addpersistentlistener.md` and `unityengine/events/unityeventbase/setpersistentlistenerstate.md`: persistent listeners.

## Engine API lookup

Engine APIs (`UnityEditor.AssetDatabase`, `UnityEditor.PackageManager`, `UnityEngine.Events`) are versioned by Unity version, so check the project's Unity version first. Then:

1. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
2. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`) and grep it for the `uid` (about 6 MB, so don't read the whole file).
3. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEditor.AssetDatabase.ImportPackage(System.String,System.Boolean)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/assetdatabase/importpackage.md`. If the page doesn't exist, use the `href` in the `xrefmap.yml` entry under `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/`.
