# Example report

A worked example of the skill's final report.

The user asks: "Why did build 57 of android-release fail?"

The categories are `missing_using_or_reference` at line 7104 and `script_compiler_error` at line 8402, the second with `stageMatch: false`. The section above both is `+ Building Unity player (android)`. Line 7104 is a `[Script Updater]` package error that doesn't recur. The last compile pass, above line 8401 (`Scripts have compiler errors.`), holds only the project's errors:

```text
8389:[error] ... Assets/Scripts/Rendering/OutlineFeature.cs(22,17): error CS0619: 'RenderTargetHandle' is obsolete: 'Deprecated in favor of RTHandle. #from(2022.1) #breakingFrom(2023.1)'
```

Build 56 succeeded on Unity `2022_3_62f1`, and build 57, at the same revision, is the first on `6000_3_26f1`. Both rows give Your project as the side, and neither lets the report name it. The report:

```text
Build #57 of android-release failed in "Building Unity player (android)" because Assets/Scripts/Rendering/OutlineFeature.cs doesn't compile against Unity 6000.3. Confidence: high.

Evidence: Missing Reference Error, Script Compiler Error. Line 8389: OutlineFeature.cs uses RenderTargetHandle, which no longer compiles from Unity 2023.1. Build 56 succeeded on 2022.3 at the same revision, so the upgrade started the failure. The steps read returned not found. Docs read: Troubleshoot common issues.

Fix:
1. Move OutlineFeature.cs to the current render pass API (RTHandle), or build with Unity 2022.3 until it's ported.
2. Confirm locally in batch mode before you rebuild (Troubleshoot common issues, "Run locally in batch mode").

Next step: after the fix, rebuild with `unity pipeline cloud-build builds start --build-target android-release --cloud-org my-studio --cloud-project my-game`. Your agent can apply the fix if you ask.

Side effects: I only read this build's record, log and history. I saved the log to /tmp/build-57.log and the build list to /tmp/builds-android-release.json. I changed nothing.
```
