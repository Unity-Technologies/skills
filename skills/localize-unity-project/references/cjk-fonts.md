# CJK Font Support

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
Step 1 of the skill, and only continue once it is non-null. Verified on Unity 6000.5.8f1: the non-interactive
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
