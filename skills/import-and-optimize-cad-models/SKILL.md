---
name: import-and-optimize-cad-models
description: >-
  Imports and optimizes CAD models and point clouds with Asset Transformer Toolkit (Pixyz). Make
  sure to use this skill when the user wants to import CAD or point cloud files, or build RuleSets,
  Actions, or LODs for them.
required_packages:
    com.unity.industry.toolkit: ">=4.0.0"
---
## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's scripts. Never build logging or detection they didn't ask for, because it clutters their project and console.
- Avoid long explanations.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

The docs pages are local markdown copies of the Asset Transformer Toolkit package documentation, stored at `<project-folder>/Library/PackageCache/com.unity.industry.toolkit@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time.

- `Documentation~/import-models.md`: importing models with an Importer.
- `Documentation~/use-the-rule-engine.md`: RuleSets, Rules, and Actions.
- `Documentation~/lod-generator.md`: generating LODs.
- `Documentation~/prepare-optimize-model.md`: preparing and optimizing imported models.

If a page you need isn't in that folder, because some package versions don't ship all their docs, read the same page online under `https://docs.unity3d.com/Packages/com.unity.industry.toolkit@<VERSION>/manual/`. Use the `.html` file names, for example `manual/use-the-rule-engine.html`, because this package's online docs have no `.md` variant and a `.md` URL returns 404. Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The product version, eg `7000.0`

For this skill, use the `com.unity.industry.toolkit` version from `Packages/packages-lock.json`, eg `4.0`.

### Before you start: check the package version
This skill needs Asset Transformer Toolkit 4.0.0 or later (`com.unity.industry.toolkit`). Earlier versions don't have the `ATTAssistantUtilities` API every workflow below relies on.

1. Read the resolved version of `com.unity.industry.toolkit` from `Packages/packages-lock.json` (fall back to `Packages/manifest.json`).
2. **Not installed:** tell the user this workflow needs Asset Transformer Toolkit 4.0 or later, which is an entitled Unity package they may need access to through their Unity plan, and stop. Don't add it to the manifest yourself, because the install fails without that entitlement.
3. **Installed below 4.0.0:** tell the user which version you found and that 4.0 or later is required, and ask before upgrading. Don't fall back to older APIs, raw C# against internal types, or reflection, because this skill's workflows only work through the 4.0 `ATTAssistantUtilities` API.
4. **4.0.0 or later:** continue.

### API reference
The API is in the `Unity.Pixyz.Plugin4Unity` and `UnityEditor.PixyzPlugin4Unity` namespaces, part of the `com.unity.industry.toolkit` package. It is versioned by package version, not Unity version (e.g. `@4.0`), so check the project's installed package version before relying on a specific class or method.

The package's online docs have no scripting API reference for the RuleSet, Rule, RuleBlock, or `ATTAssistantUtilities` APIs, so the reference files below are the only documentation of the members this skill uses. Use them instead of searching online or relying on training knowledge, because APIs often change between major package versions.

1. Use the `unity-cli` skill to check the installed package version.
2. Fetch `https://docs.unity3d.com/Packages/com.unity.industry.toolkit@<VERSION>/xrefmap.yml` (replace `<VERSION>` with the installed package version, e.g. `4.0`) and grep it for the class you need. If it has an entry, read the page its `href` points to, because that page matches the installed version.
3. If it has no entry, use the reference file below that covers the class.
4. If neither covers the member you need, tell the user rather than guessing a member name or using reflection, because a wrong name fails to compile and reflection is not available.

### Technical Notes
- Pixyz Plugin is the former name of Asset Transformer Toolkit. Call it Asset Transformer Toolkit when you address the user, unless the user calls it Pixyz.
- Most classes and code are in the `Unity.Pixyz.Plugin4Unity.Editor` assembly.
- Asset Transformer Toolkit is NOT the same product as Asset Transformer Studio (formerly Pixyz Studio). NEVER rely on information about Asset Transformer Studio, because its features and APIs differ.

### Importers
- Asset Transformer Toolkit imports 3D files from outside the project.
- When you modify an Importer, NEVER start a reimport unless the user asked for one, because an import can take a very long time.
- Modify Importer fields through `SerializedObject` and `SerializedProperty`. NEVER use reflection to access or modify Importer fields, because reflection is not available.
- To create an Importer for a file, or to reimport a model with an existing ImporterScriptableObject, read [references/create-importer](references/create-importer.md).

### RuleSets and Actions
Read [references/rulesets-and-actions](references/rulesets-and-actions.md)
Read [references/ruleset-api](references/ruleset-api.md) when the RuleSet API is needed.
Read [references/rule-api](references/rule-api.md) when the Rule API is needed.
Read [references/ruleblock-api](references/ruleblock-api.md) when the RuleBlock API is needed, including `ActionBase.Id` for constructing `RuleBlock` instances.

### Levels of Detail
Read [references/lods](references/lods.md)

## Final step

This skill changes Importer, RuleSet, and LOD settings in the Editor, not runtime scripts, so a clean console and a read-back of each change are enough. Run these checks in order:

1. Run `unity command console status --format json` and check `groundTruth.consoleErrors` is zero and `compilationFailed` is false.
2. Read back each asset you changed with `script eval`, because a change can run without errors and still not stick:
   - **Importer:** reload the asset with `AssetDatabase.LoadAssetAtPath`, then check each property you set through `SerializedObject.FindProperty` holds the new value.
   - **RuleSet:** reload the asset, check `RulesCount`, the Action IDs in each Rule (`GetRule(i).GetBlock(j)`), and that `IsValid()` returns `true`. Check it against the [validation checklist](references/rulesets-and-actions.md#validation-checklist). Then read back every Action parameter you changed and compare it with the requested value. Skip parameters you set to a scene GameObject, because Unity doesn't serialize references to scene objects and they read back as null. Report those as temporary instead (see [Path C](references/rulesets-and-actions.md#path-c-success)).
   - **LOD settings:** reload the Importer or GameObject, check the LOD rule count and types match what you set, and read back every LOD parameter you changed and compare it with the requested value.
3. Stop here unless the user asked for more proof or you suspect a specific fault. In that case, inspect the asset or scene through `script eval`. Don't enter Play mode, because Asset Transformer Toolkit runs only in Edit mode and can't run Importers or RuleSets in Play mode. If an import or RuleSet run is still in progress, don't verify its results yet. Report it under **Background tasks** instead.

If any check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed, fix it, and run the checks again.

## Final report

Return these sections in order:

1. **Changes**: each Importer, RuleSet, or LOD setting you created or changed, with old and new values.
2. **Side effects**: anything you changed that the user didn't name, such as the save folder `EnsureImporterSaveFolder` created, or "None".
3. **Verification**: the results of the final step checks.
4. **Background tasks**: any import or RuleSet run still in progress, and that the user must wait for it to finish.
5. **Follow-up**: anything the user must do, such as refreshing the Inspector to see changes or setting a parameter by hand.
