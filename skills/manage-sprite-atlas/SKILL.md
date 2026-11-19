---
name: manage-sprite-atlas
description: Manages SpriteAtlas assets through a prebuild pipeline, covering master and variant atlases, packing settings, and platform overrides. Use when asked to create or optimize sprite atlases.
required_editor_version: ">=6000.3"
---

# Unity SpriteAtlas V2

Provides *editor-safe* procedural knowledge for scripting atlases in Unity projects. **V2 enforces strict separation between editor authoring and runtime access.**

> ⚠ **Critical V2 Principle**: `SpriteAtlas` is **runtime-only**. `SpriteAtlasAsset` is **editor-only**. Never mix contexts. Always use V2.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Run C# with `unity command script eval --code '<snippet>'`. Don't use `using` directives or unqualified types in the snippet. The compiler reads using `UnityEngine` and a bare AssetDatabase, SpriteAtlas, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

For Engine pages: if the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder (`<editor-install>/Editor/Data/Documentation/en/Manual/<local file>`), and Script Reference pages from `en/ScriptReference`.

Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/atlas.md`.
Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

Read the Unity version from `ProjectSettings/ProjectVersion.txt`, and the `com.unity.addressables` major.minor for the Addressables docs from `Packages/packages-lock.json`.

For Addressables pages: the docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.addressables@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. If that folder has no .md files, use the online pages at `https://docs.unity3d.com/Packages/com.unity.addressables@<VERSION>/manual/`, because some packages don't ship their docs.

The page list, with each page's local file and online URL, is in [references/docs-map.md](references/docs-map.md).

- [references/api.md](references/api.md): the Script Reference page for each V2 type, plus API pitfalls.

### API reference

The API is `UnityEditor.U2D` (`SpriteAtlasAsset`, `SpriteAtlasImporter`, `SpriteAtlasUtility`) and `UnityEngine.U2D` (`SpriteAtlas`, `SpriteAtlasManager`), part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html`).

The Addressables API (`UnityEditor.AddressableAssets`, `UnityEngine.AddressableAssets`) is part of the `com.unity.addressables` package and versioned by package version (e.g. `@2.7`), not Unity version. Use the `unity-cli` skill to check the installed package version, then fetch and cache `https://docs.unity3d.com/Packages/com.unity.addressables@<version>/xrefmap.yml`, which maps each member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.html` => `https://docs.unity3d.com/Packages/com.unity.addressables@2.7/api/UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.html`). Grep the yml file locally instead of refetching per lookup, and don't read the whole file.

Always use the docs for the project's Unity and package versions rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## 🚨 CRITICAL: Default Approach is Prebuild Generation

**ALWAYS use `IPreprocessBuildWithReport` to automatically generate or update SpriteAtlases during the build pipeline.** This is the DEFAULT and REQUIRED approach unless the user EXPLICITLY requests manual authoring.

### ❌ DO NOT Create Manual Menu Item Scripts

**NEVER create scripts with `[MenuItem]` attributes for atlas generation unless explicitly requested.** The prebuild approach eliminates the need for manual clicks. **Only use manual authoring for**: Hand-optimized layouts, specific sprite arrangements, or editor preview requirements. See [Advanced: Manual Authoring](#advanced-manual-authoring-not-default---use-only-when-explicitly-requested).

## 🚨 CRITICAL: Sprite Source Location Restriction

**ONLY add sprites from the project's Assets folder. NEVER add sprites from Unity built-in assets, packages, or external locations.** Unity built-in assets cannot be packed into SpriteAtlas, and package assets may cause import/dependency issues.

## Critical V2 Architecture

| Context | Component | Purpose | Allowed Usage |
|---------|-----------|---------|---------------|
| **Editor Authoring** | `SpriteAtlasAsset` | Add/remove sprites/folders; store metadata | ✅ Editor scripts only |
| **Editor Settings** | `SpriteAtlasImporter` | Configure texture, packing, platform settings | ✅ Editor scripts only |
| **Editor Packing** | `SpriteAtlasUtility.PackAtlases()` | *Optional* editor preview packing (not for build) | ⚠ Only for preview; atlases auto-pack at build |
| **Runtime** | `SpriteAtlas` | Query packed sprites (read-only) | ✅ Runtime scripts only |
| **Runtime Loading** | `SpriteAtlasManager` | Dynamic loading callbacks | ✅ Runtime scripts only |

### Forbidden Cross-Context Usage (Common Error Sources)

| ❌ Invalid Pattern | ✅ Correct Pattern |
|--------------------|-------------------|
| `new SpriteAtlas()` in editor code | Use `SpriteAtlasAsset` + `SpriteAtlasImporter` |
| `AssetDatabase.LoadAssetAtPath<SpriteAtlas>(...)` in editor | Use `SpriteAtlasAsset.Load(...)` |
| `SpriteAtlasAsset.GetPackables()` in editor | Use `SpriteAtlas.GetPackables()` |
| Modifying `SpriteAtlas` in editor scripts | Modify `SpriteAtlasAsset` → reimport → use `SpriteAtlasImporter` |
| Create variants from original packable objects (sprites/folders) | Creating variants from a Master runtime `SpriteAtlas` |

**NEVER script against `SpriteAtlas` in editor code** — it is *only* for runtime use in V2 except for GetPackables.

## Procedure

Run these steps in order. **ALWAYS perform steps 1 to 4 BEFORE generating any code.**

### 1. Read the shipped resources before writing code (REQUIRED)

This skill ships working C# under `scripts/`, and the atlas code that goes wrong is almost always
code written without reading it first. Open the files for the path you are taking, then write.

| Taking this path | Read first |
|---|---|
| Any atlas work at all | [scripts/authoringvsruntime.cs](scripts/authoringvsruntime.cs), [references/common-errors.md](references/common-errors.md) |
| Prebuild generation (the default) | [scripts/spriteatlasprebuildgenerator.cs](scripts/spriteatlasprebuildgenerator.cs), [scripts/enablespritepacking.cs](scripts/enablespritepacking.cs), [scripts/savespriteatlasasset.cs](scripts/savespriteatlasasset.cs) |
| Option B, Addressables late-binding | [scripts/buildaddressablespostprocess.cs](scripts/buildaddressablespostprocess.cs), [scripts/spriteatlaslatebinding.cs](scripts/spriteatlaslatebinding.cs), [scripts/handlelatebinding.cs](scripts/handlelatebinding.cs) |
| Anything that might use an older API | [scripts/deprecatedmethods.cs](scripts/deprecatedmethods.cs), [scripts/dontscriptspriteatlasineditor.cs](scripts/dontscriptspriteatlasineditor.cs), [scripts/dontpackinruntimebuilds.cs](scripts/dontpackinruntimebuilds.cs) |

`scripts/` also covers custom packers, variants, platform settings, and runtime access. Browse it
when your task isn't in the table, because writing the API from memory is the most common cause of
an atlas that imports cleanly and then does nothing at runtime.

### 2. Check for Existing Scripts (REQUIRED)

**BEFORE generating any code, scan the project for existing SpriteAtlas scripts generated by this skill.**

Search for files containing the identifier: `// [UNITY-SKILL:SPRITEATLAS]`

If you find none, go to step 3, because there is nothing to update or overwrite.

**If existing scripts are found, ALWAYS ask the user with this format:**

> "I found existing SpriteAtlas scripts in your project:
>
> **Prebuild Generator:**
> - `Assets/Editor/SpriteAtlas/SpriteAtlasPrebuildGenerator.cs`
>
> **Addressables Builder:**
> - `Assets/Editor/SpriteAtlas/BuildAddressablesPostprocess.cs`
>
> **Runtime Loader:**
> - `Assets/Scripts/SpriteAtlas/SpriteAtlasLateBinding.cs`
>
> What would you like to do?"

**Then present options:**

- **Option A: Update existing scripts** (Recommended if requirements changed). Regenerates them at the same paths. ⚠ May overwrite custom modifications.
- **Option B: Create new scripts with different names**. Generates alongside the existing scripts, which stay unchanged. You'll need to specify new names or paths.
- **Option C: Abort (keep existing unchanged)**. Makes no changes to the project.

**User Choice Handling:**

- **If A chosen**: Regenerate at existing paths, and keep `// [UNITY-SKILL:SPRITEATLAS]` on the first line so the next run finds the scripts again
- **If B chosen**: Ask for new script names (e.g., "SpriteAtlasPrebuild_Custom.cs"), then generate
- **If C chosen**: Stop immediately, inform user no changes were made

### 3. Delivery Mechanism (REQUIRED)

Ask: "How do you want to deliver the sprite atlases?"

**Option A: Built-in Data (Immediate Loading)**
- Atlases are included in the build and loaded immediately
- Set `includeInBuild = true`
- Increases initial build size, and can't update without a new build

**Option B: Late-Binding via Addressables (On-Demand Loading)**
- Atlases are NOT included in build, loaded on-demand via Addressables
- Set `includeInBuild = false`
- Create Addressables entries for each atlas
- Add addressable setup as prebuild step
- **🚨 REQUIRED**: Build Addressables content as build step
- **🚨 REQUIRED**: Create late-binding runtime loader script
- Requires the Addressables package, and loads asynchronously

| Use Case | Recommended |
|----------|-------------|
| Core UI sprites that are always visible | **Option A: Built-in** |
| Tutorial or onboarding sprites | **Option A: Built-in** |
| Level-specific sprites (100+ levels) | **Option B: Addressables** |
| DLC or seasonal content | **Option B: Addressables** |
| Localized UI sprites (multiple languages) | **Option B: Addressables** |
| Character skins or cosmetics | **Option B: Addressables** |

### 4. SpritePacker Mode (REQUIRED)

**Enable Sprite Packer mode before creating atlases, then read the setting back and confirm it took.**
Use the code in [scripts/enablespritepacking.cs](scripts/enablespritepacking.cs); it sets
`EditorSettings.spritePackerMode` and configures the importer's packing settings.

This is the step that decides whether the atlas you produce is real. `Disabled` is the zero value of
`SpritePackerMode`, so any project where nobody has set it carries packing **Disabled**, and an atlas
created while it is Disabled still imports, still shows up as an asset, and still looks finished, but
can never pack. Unity says so in the Inspector: *"Sprite Atlas packing is disabled"*. Nothing else in
the workflow fails, so an atlas shipped this way reads as a success. Do not assume a project is
already configured: read the value.

So do not treat "I set it" as done. After setting it, read `EditorSettings.spritePackerMode` back,
confirm it is **not** `Disabled`, and report the value you actually read. If you cannot read it back,
say so rather than assuming the write landed.

**DO NOT edit meta files DIRECTLY.**

### 5. Create Prebuild Script

Create this script in an Editor folder. Customize based on user's delivery choice. For Option A, generate 1 script: the prebuild generator (`includeInBuild = true`).

#### Option B: Late-Binding via Addressables (On-Demand Loading)

> 🚨 **CRITICAL ENFORCEMENT**: When user chooses Addressables, you MUST generate ALL THREE scripts below. Never generate just one or two - all three are required for Addressables delivery to work.

**Required Scripts (ALL THREE MANDATORY):**

1. **Prebuild script** (IPreprocessBuildWithReport) - Generates atlases, creates Addressables entries
2. **Build Addressables script** (IPostprocessBuildWithReport) - Builds Addressables content bundles
3. **Late-binding runtime loader** (MonoBehaviour) - Handles on-demand loading at runtime

See [references/addressables-delivery.md](references/addressables-delivery.md) for complete implementation.

#### Complete Workflow

The decision flow from step 2 through script generation is in [references/workflow.md](references/workflow.md).

### 6. Customize Categorization Rules

Edit the `OnPreprocessBuild` method to match your project's sprite organization. Choose one or combine multiple strategies:

| Strategy | When to Use | Implementation |
|----------|-------------|----------------|
| **Folder-based** | Sprites organized by folder structure | `GenerateAtlasByFolder("Assets/Art/UI", "Assets/Atlases/UI.spriteatlasv2")` |
| **Naming convention** | Sprites follow naming patterns | `GenerateAtlasByNaming("Assets/Art", "icon_", "Assets/Atlases/Icons.spriteatlasv2")` |
| **Asset labels** | Sprites tagged with labels |
| **Scene-based** | Sprites used in specific scenes | Query scene references |

### 7. Build Your Project

Atlases are automatically generated/updated during build. **No manual menu clicks required.** This is why prebuild is the default approach.

For Option B, the build also creates the Addressables entries and builds the content bundles, using the three scripts from step 5. At runtime, the late-binding loader loads each atlas when its sprites are first accessed.

### Common Categorization Patterns

Read [references/categorization-patterns.md](references/categorization-patterns.md) for common ways to group sprites into atlases.

## Advanced: Manual Authoring (NOT Default - Use Only When Explicitly Requested)

Use manual authoring only when the user EXPLICITLY requests manual control or editor preview; otherwise use the [Procedure](#procedure). When it applies, and the patterns for master atlas creation, variant creation, and runtime loading, are in [references/manual-authoring.md](references/manual-authoring.md).

## Detailed Reference

- **Common Errors**: [references/common-errors.md](references/common-errors.md) - Error messages with their cause and fix
- **API Reference**: [references/api.md](references/api.md) - Script Reference page for each V2 type, API pitfalls, and the `using` directives editor and runtime scripts need. Read it before you generate code.
- **Custom Packing**: [references/custom-packing.md](references/custom-packing.md) - `ScriptablePacker` implementation
- **Best Practices**: [references/best-practices.md](references/best-practices.md) - Optimization and common pitfalls

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.
2. Check each atlas you created or changed actually packs, because an atlas can import cleanly and still hold no sprites. In one `script eval`, load the atlas with `UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(path)`, pack it with `UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[] { atlas }, UnityEditor.EditorUserBuildSettings.activeBuildTarget, false)`, and return `atlas.spriteCount` and `atlas.CanBindTo(sprite)` for one sprite you added. These calls only read the atlas and write the packing cache, so they don't break the editor/runtime split. The check passes when `spriteCount` is greater than zero and `CanBindTo` returns `true`.
3. Stop there unless the user asked you to prove it works or you suspect a specific fault.
4. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed. If `spriteCount` is zero, read `EditorSettings.spritePackerMode` again first, because a `Disabled` packer mode is the most common cause.

## Final report

Give the user a concise but not dense checklist that includes:

- The scripts you created or updated, with their paths, and the delivery option they implement.
- The `EditorSettings.spritePackerMode` value you read back.
- The checks you ran and their results, including the `spriteCount` you read.
- Any incidental side effects, for example `EditorSettings` values you changed or atlases you packed to verify them.
- Any follow-up the user needs to do, such as installing Addressables or building the project.
