# Running C# for Web optimization

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in, so `../scripts/` is the skill's `scripts/` folder. Project paths in code spans are relative to the user's project.

## Contents

- [Passing C# to `script eval`](#passing-c-to-script-eval)
- [Reading the settings this skill audits](#reading-the-settings-this-skill-audits)
- [Applying the settings](#applying-the-settings)
- [Verify from disk, not from the objects you just wrote](#verify-from-disk-not-from-the-objects-you-just-wrote)

## Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**, so write the code without `using` directives and
with fully qualified types. If the code doesn't compile, look up the error here:

| Error | Cause | Fix |
| --- | --- | --- |
| `CS0210` | A `using` directive, such as `using UnityEditor;`, which the compiler reads as a resource-disposal statement | Remove the directive and qualify the types |
| `CS0246` | An unqualified type, such as `PlayerSettings` | Use the full name, such as `UnityEditor.PlayerSettings` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Use `UnityEngine.Object` |

## Reading the settings this skill audits

One call returns the whole Pre-Flight picture. Verified against Unity 6000.5.7f1:

```csharp
var target = UnityEditor.Build.NamedBuildTarget.WebGL;
var w = new System.Collections.Generic.List<string>();
w.Add($"activeBuildTarget={UnityEditor.EditorUserBuildSettings.activeBuildTarget}");
w.Add($"compressionFormat={UnityEditor.PlayerSettings.WebGL.compressionFormat}");
w.Add($"decompressionFallback={UnityEditor.PlayerSettings.WebGL.decompressionFallback}");
w.Add($"stripEngineCode={UnityEditor.PlayerSettings.stripEngineCode}");
w.Add($"managedStrippingLevel={UnityEditor.PlayerSettings.GetManagedStrippingLevel(target)}");
w.Add($"il2cppCodeGeneration={UnityEditor.PlayerSettings.GetIl2CppCodeGeneration(target)}");
w.Add($"apiCompatibilityLevel={UnityEditor.PlayerSettings.GetApiCompatibilityLevel(target)}");
w.Add($"exceptionSupport={UnityEditor.PlayerSettings.WebGL.exceptionSupport}");
w.Add($"debugSymbolMode={UnityEditor.PlayerSettings.WebGL.debugSymbolMode}");
w.Add($"dataCaching={UnityEditor.PlayerSettings.WebGL.dataCaching}");
w.Add($"wasm2023={UnityEditor.PlayerSettings.WebGL.wasm2023}");
w.Add($"initialMemorySize={UnityEditor.PlayerSettings.WebGL.initialMemorySize}");
w.Add($"maximumMemorySize={UnityEditor.PlayerSettings.WebGL.maximumMemorySize}");
w.Add($"memoryGrowthMode={UnityEditor.PlayerSettings.WebGL.memoryGrowthMode}");
w.Add($"targetFrameRate={UnityEngine.Application.targetFrameRate}");
w.Add($"vSyncCount={UnityEngine.QualitySettings.vSyncCount}");
return string.Join("\n", w);
```

**Three API names to get right**, because the obvious spellings do not exist and fail to compile:

| Setting | Correct form | Does NOT exist |
|---|---|---|
| Managed stripping level | `PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL)` | `PlayerSettings.managedStrippingLevel` |
| Wasm code optimization | `UnityEditor.WebGL.UserBuildSettings.codeOptimization` | `PlayerSettings.WebGL.codeOptimization`, `PlayerSettings.WebGL.optimizationLevel` |
| IL2CPP code generation | `PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL)` | a bare property |

`UserBuildSettings` lives in the WebGL build-support module, so it only resolves when that module is
installed. Read it in a separate call from the rest, and treat a resolution failure as "the Web
module isn't installed" rather than as a bad snippet.

**`codeOptimization` is the one setting here that does not live in the project file.** It persists to
`Library/EditorUserBuildSettings.asset`, and `Library/` is gitignored by every standard Unity
`.gitignore`, so this value is per-machine and does not travel: teammates and CI do not inherit it.
Two consequences. Read it back through the API and do not look for it in
`ProjectSettings/ProjectSettings.asset` — it is absent there even on a successful apply, so its
absence is not a failure. And if the release build runs in CI, apply it there as a build step rather
than assuming the repository carries it.

## Applying the settings

Most of the writes in this skill are a single batch, and
[scripts/WebOptimizer.cs](../scripts/WebOptimizer.cs) already is that batch. It declares a class
with a `[MenuItem]`, so it is a **project file, not `script eval` input** — a class declaration cannot be
flattened into a statement block. Save it under `Assets/Editor/`, let Unity compile, then invoke it
in one line:

```csharp
UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Apply Web Release Settings");
```

Keep its `using` directives; they are correct in a file. For one-off changes — a single quality
level, a frame-rate flip — an inline `script eval` statement is fine.

## Verify from disk, not from the objects you just wrote

**Applying a setting and reading it back in the same session proves nothing.** Player Settings are
in-memory objects until something saves them, so every read-back returns the value you just
assigned whether or not it ever reached
`ProjectSettings/ProjectSettings.asset`. A run that skips the save reports success, and when the
Editor session ends the whole change is gone. This was observed, not theorised: a run applied
everything, read back Brotli / High / None, said it was done, and the file on disk never changed.

So after any write:

1. **Save.** `UnityEditor.AssetDatabase.SaveAssets()`. `WebOptimizer.cs` now does this itself; an
   inline `script eval` write has to do it explicitly.
2. **Read the values back and report them.** Not "applied successfully" but the actual values, so the
   user can see what is stored. `WebOptimizer.cs` logs all nine.
3. **For anything per-build-target, name the target you read.** Several of these settings exist once
   per target, so a value can be correct for one target and unset for the one being built.

The same trap exists on the file-editing route in reverse: a hand-edited
`ProjectSettings.asset` reads back fine while the running Editor and the build still use the old
value. Verifying after a save and a reimport is what catches both.

**Do not try to reproduce this in batch mode.** A batch Editor invoked with `-quit` saves settings on
exit, so an unsaved write persists anyway and the run looks correct. Both a saving and a non-saving
version of the script pass under `-quit`. The bug only appears in a live Editor session, which is
where it was found. Concluding from a green batch run that the save is unnecessary is the wrong
conclusion from a test that cannot see the defect.
