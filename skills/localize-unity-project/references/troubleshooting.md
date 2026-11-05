# Localization error messages

Exact error strings this skill's steps can produce, with the root cause and the fix.

| Error | Root cause | Resolution |
|---|---|---|
| `CS0118: 'UI' is a namespace but is used like a type` | An unqualified UI type name such as `Image` or `Mask` resolves to the `UnityEngine.UI` namespace. | Fully qualify the type, for example `UnityEngine.UI.Image`. |
| `OperationException: Failed to load sub-asset` | An Asset Table entry points at an asset inside a `Resources/` folder, which Addressables can't load as a sub-asset. | Copy the asset out of `Resources/`, mark the copy Addressable, and point the table entry at the copy's GUID. |
| Text starting `No translation found for` shown in the game UI | The key has no entry, or an empty value, in the selected locale's table. | Run the table completeness check in `table-completeness-check.md` Fill the gaps for the keys and locales the user asked for, and report the rest without changing them. |
| `NullReferenceException` on the first TMP call, for example `TMP_FontAsset.CreateFontAsset` | TMP Essential Resources were never imported, so `TMP_Settings.instance` is `null`. | Import them non-interactively as in `cjk-fonts.md`. |
| `UnassignedReferenceException` on a font asset, or glyphs show as squares (tofu) | The font asset's material or atlas texture wasn't saved as a sub-asset, or the font lacks the locale's glyphs. | Follow the font repair steps in `api-notes.md` and the font setup in `cjk-fonts.md`. |
| `OperationCanceledException` from `LocalizeAll` | The user clicked Cancel in the confirmation dialog. | Report it as a cancellation. Don't retry or bypass the dialog. |
