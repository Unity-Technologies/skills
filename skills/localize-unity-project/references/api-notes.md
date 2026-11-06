# Localization API pitfalls

These are the failures the API pages don't warn about. For the API itself, read the `LocalizationEditorSettings`, Addressables and TMP pages listed in `docs-map.md`, and the `Addressables.md` manual page for how Asset Tables load their assets.

## Asset Table entries need a save
`EditorUtility.SetDirty(table)` only marks the table. Without `AssetDatabase.SaveAssets()` an entry added with `table.GetEntry(sharedEntryId) ?? table.AddEntry(sharedEntryId, guid)` reads back correctly for the rest of the session and is gone when the Editor closes. Follow the save sequence in the parent skill: dirty the collection and its `SharedData` too, then save once at the end.

## Avoid the Resources folder
Don't reference assets inside a `Resources/` folder in Asset Tables, because Addressables can't load sub-assets from `Resources/` and fails with `OperationException: Failed to load sub-asset`. This catches the TMP default font, which lives under `Assets/TextMesh Pro/Resources/`. Copy the asset out with `AssetDatabase.CopyAsset` (for example to `Assets/Fonts/`), and use the copy's path for Addressables and the Asset Table.

## TMP_FontAsset repair
If fonts appear as "tofu" or throw `UnassignedReferenceException`:
- Check that the Material and Atlas Texture are nested under the Font Asset in the Project window.
- Re-assign `fontAsset.material.mainTexture = fontAsset.atlasTexture;` and re-save.
- If the font was deleted and recreated, update the GUID in both the Asset Table and the Addressables Group.
