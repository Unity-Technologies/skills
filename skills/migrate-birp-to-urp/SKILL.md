---
name: migrate-birp-to-urp
description: Plans, executes, and troubleshoots migration from the Built-in Render Pipeline to URP. Use when asked to convert a project, scene, material, or shader to URP, or to fix visuals broken by the change.
---
Classify the request, inspect the current project state, choose the correct migration path, and validate the Built-in to URP migration outcome carefully.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for, because it clutters their project and console. Read state through `script eval` instead, as described in [Final step](#final-step).
- Avoid long explanations.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `<editor-install>/Editor/Data/Documentation/en/Manual` folder.

Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/`.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The product version, eg `7000.0`

Unity documentation: read [references/docs-map.md](references/docs-map.md) for the page list, with each page's local file name and online `.md` URL.

### API reference

The APIs are `UnityEngine.Rendering` and `UnityEditor` (Graphics and Quality settings, materials, lighting), part of the Unity Engine, and `UnityEngine.Rendering.Universal`, part of the `com.unity.render-pipelines.universal` package. The Engine API is versioned by Unity version and the URP API by package version, so check both before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version and the installed URP package version.
2. For Engine APIs, if the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in `<editor-install>/Editor/Data/Documentation/en/ScriptReference`, and skip to step 6.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html`).
6. For URP APIs, fetch and cache `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@<version>/xrefmap.yml` (replace `<version>` with the installed package version, e.g. `17.3`) and grep it locally. Each entry maps a fully qualified name to an `href` (e.g. `href: api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html` => `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/api/UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.html`).

Always use the docs for the project's Unity and URP versions rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## Mandatory Execution Reference

For any actual migration or repair pass, read [references/implementation-patterns.md](references/implementation-patterns.md) before the first `script eval` call that edits project settings, materials, post-processing, lighting, probes, or scenes. This is mandatory even for short prompts such as "migrate this project to URP" when rollback safety is already confirmed.

If that resource cannot be loaded, use the embedded rules in this SKILL.md and lower your confidence: do not claim PPv2 conversion, lighting/probe refresh, or migration completion from intent or tool logs alone.

## Critical Default

For simple requests like "migrate this Built-in project to URP" or "move this project to URP", start or resume the safe migration workflow rather than jumping straight to conversion.

A generic upgrade prompt must not require the user to mention every migration risk. Inspect and handle common Built-in dependencies such as PPv2, baked lighting/lightmaps, reflection probes, particle/fog/smoke materials, Quality levels, camera post-processing, and representative scene validation.

Default result is phase-based:

1. Detect whether the project is Built-in, partially migrated, already URP, or HDRP.
2. Classify the request as standard 3D URP, URP 2D, selected-material conversion, planning-only, or troubleshooting.
3. Inspect migration risks before recommending changes: opaque materials, particle/VFX materials, custom shaders, post-processing, camera effects, baked lighting/lightmaps/reflection probes, Quality levels, and representative scenes.
4. Stop before any pipeline-mutating work until the user confirms a rollback point such as a branch, backup, archive, or disposable copy. Pipeline-mutating work includes installing URP, assigning a URP asset in Graphics/Quality settings, running converters, and editing materials or scenes.
5. Choose the next migration phase, execute that phase, save/re-query its outputs, and report its phase status.
6. Treat custom shaders, `GrabPass`, Surface Shaders, `OnRenderImage`, replacement shaders, and package-owned shaders as scoped follow-up work, not automatic converter work.
7. Validate representative scenes, Console output, camera rendering, lighting/baked GI state, materials, post-processing persistence, and quality-level assignments before declaring full migration success.

Default phases:

- Phase 0: Inspect and plan. Detect pipeline state, representative scenes, rollback safety, PPv2, material/shader risks, Quality levels, lighting/lightmap/probe state, and custom render code.
- Phase 1: URP setup and supported material conversion. Install/reuse URP, create/assign URP assets in Graphics/Quality, convert supported opaque and particle/effect materials, and verify no supported materials remain on Built-in shaders.
- Phase 2: Post-processing and camera migration. Create persistent URP Volume profiles, wire scene Volumes/camera post-processing, disable legacy PPv2 for URP validation, and classify unsupported PPv2 effects such as SSR.
- Phase 3: Lighting, lightmaps, and reflection probes. Resolve stale Built-in lighting state, configure URP lighting/probe settings, rebake/refresh when feasible, or clearly mark lighting/probes partial.
- Phase 4: Final validation and report. Save/reload/re-query project state, capture representative scenes, check Console, and report complete/incomplete/manual items.

One turn may complete multiple phases if the project is small and Unity remains stable, but do not force all phases into one response. Prefer an honest phase boundary over an over-claimed "complete" result.

Ask configuration questions only when the project state or user intent leaves a real decision unresolved. If the user says "do not modify files", stay in audit/planning mode and do not perform conversion or asset edits.

Read [references/migration-gates.md](references/migration-gates.md) before any generic migration pass and before reporting status: it holds the generic upgrade contract and the success gate.

## Execution path: running C# in the Editor

Every C# step in this skill runs inside a live Editor through the Unity CLI. **The `unity-cli`
skill owns getting you there** — installing the CLI, confirming a connected Editor, adding the
project's `com.unity.pipeline` package, telling a genuinely absent Editor apart from one stuck in
Safe Mode, and discovering the Editor's command catalog. Follow it first; don't re-derive any of
it here.

Two things it can't know for you:

- **You need `script eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog. Its presence depends on the Pipeline package version, not on the CLI, so a healthy
  install can still lack it — if it's missing, say so and stop.
- **On an Editor older than 6000.3, expect the Pipeline package not to work at all**, and read the
  symptom correctly rather than retrying. `com.unity.pipeline` uses `IPreprocessBuildWithContext`
  and `BuildCallbackContext`, which Unity introduced in **6000.3**; the package's own manifest
  declares `"unity": "6000.0"`, so it installs happily and then fails to compile. Measured:
  present in 6000.3 / 6000.4 / 6000.5, absent in 6000.0 / 6000.1 / 6000.2 and in the 2022 and 2023
  lines. The symptom is misleading — the server never starts and `unity status` shows no row and no
  error:

  | Error | Cause | Fix |
  |---|---|---|
  | `CS0246` on `IPreprocessBuildWithContext` / `BuildCallbackContext` in the Editor log | Editor older than 6000.3 can't compile the Pipeline package. | Tell the user the Editor is too old for the Pipeline package; don't debug the CLI. |

  **This matters here more than in most skills:** someone migrating a project off the Built-in
  pipeline is, by definition, often on an older Editor.
- **A render-pipeline migration is not safely authorable blind.** Assigning a URP asset, converting
  materials, and rebaking lighting all need a live Editor. An unreachable Editor is a stop, not a
  cue to hand-edit `ProjectSettings/GraphicsSettings.asset`.

Run C# with `unity command script eval --code '<snippet>'`. Its default timeout depends on the
Pipeline version, so read it from the command's schema (`unity command --detail full --format json`).
It matters here: installing URP triggers a package refresh and domain reload that will outlast it. Treat that as a phase boundary rather than raising the timeout.

Read [references/eval-execution.md](references/eval-execution.md) before your first `script eval` call: it covers how to pass C# to `script eval`, the two execution modes, and the render-pipeline detection snippets.

## 0. Pre-Flight: Pipeline Detection and Reference Loading

Before doing anything else, you **must determine the active render pipeline and migration mode**:

1. **Detect Pipeline:** Run the render-pipeline detection snippet from [references/eval-execution.md](references/eval-execution.md).
   - If `currentRenderPipeline` or `defaultRenderPipelineAsset` references a `UniversalRenderPipelineAsset` -> **URP**.
   - If no render pipeline asset is assigned -> **Built-in**.
   - If it references an `HDRenderPipelineAsset` -> **HDRP**. Explain that this skill only covers Built-in to URP migration. Basic comparison advice is fine, but do not drive an HDRP migration with this skill.
2. **Load Base Reference:** Read [references/migration-workflow.md](references/migration-workflow.md).
3. **Load Shader References When Relevant:** If the request mentions materials, shaders, magenta materials, rendering errors, image effects, or custom rendering, also read:
   - [references/custom-shader-triage.md](references/custom-shader-triage.md)
   - [references/complex-shader-situations.md](references/complex-shader-situations.md) when inspection or project-file search finds advanced shader/effect patterns
4. **Load Quality Reference When Relevant:** If the request mentions shadows, lighting, baked lighting, lightmaps, reflection probes, quality settings, visual mismatch, or performance after migration, also read [references/quality-settings-map.md](references/quality-settings-map.md).
5. **Load Implementation Patterns When Executing:** If the request permits actual migration changes, post-processing conversion, baked-lighting/probe repair, or resume from a partial migration, also read [references/implementation-patterns.md](references/implementation-patterns.md).
6. **Classify the Request** as one of:
   - Full-project Built-in to URP migration
   - Built-in 2D to URP 2D migration
   - Selected material-only conversion
   - Planning / explanation only
   - Troubleshooting after a previous migration
7. **If the project is already on URP,** switch to troubleshooting mode instead of re-running setup blindly.
8. **Proceed** only after the pipeline and migration path are clear.

## 1. Assess Current Migration State

Read [references/assess-migration-state.md](references/assess-migration-state.md) before making any changes: it covers inspecting pipeline state, inventorying migration surfaces, scanning for rendering risk markers, and reporting findings.

## 2. Gather Requirements

Read [references/gather-requirements.md](references/gather-requirements.md) to interpret the request and find out scope, target renderer, validation targets, and execution permission.

## 3. Safety Gate

Before any pipeline-mutating step:

1. **Confirm rollback safety.**
   - NEVER install URP, assign a URP asset, run the Render Pipeline Converter, edit materials, or edit migration scene state before the user confirms a backup, branch, archive, disposable copy, or rollback point, because assigning a URP asset immediately turns Built-in materials magenta and the converter has no undo.
   - If rollback safety is missing, do not partially switch the project to URP, because a half-switched project renders neither pipeline correctly. Stop in planning mode and ask for the rollback confirmation first.
2. **Separate engine-upgrade risk from pipeline-upgrade risk.**
   - If the project is also moving to a new Unity version, recommend doing the engine upgrade first and the pipeline migration second. Do not treat both as a single blind operation, because mixing the two hides which change broke what.
3. **If rollback safety is confirmed,** treat it as covering the whole migration pass. Do not ask again before each converter; proceed through setup, material conversion, PPv2 migration, lighting/probe work, save/reload verification, and final reporting unless a new risky decision appears.
4. **If the user already authorized a full disposable migration,** do not ask for permission to continue after routine setup or recoverable errors. Continue with the next incomplete migration phase. Ask only when a new destructive cleanup decision appears, a package/domain reload stops execution, or repeated repair attempts hit the validation iteration limit.
5. **If the project is already partially migrated,** identify whether rollback safety was previously confirmed. If yes, resume from the first incomplete item. If no, report the partial state and ask before making additional changes.

## 4-8. Migration Path, URP Setup, Conversion, and Triage

Read [references/setup-and-conversion.md](references/setup-and-conversion.md) before choosing the migration path, setting up URP, converting materials, or triaging shaders, post-processing, and camera effects.

## 9-11. Parity, Validation, and Troubleshooting

Read [references/parity-validation-troubleshooting.md](references/parity-validation-troubleshooting.md) after setup, conversion, or troubleshooting, and when the user reports visual mismatch, lighting, performance, or other post-migration problems.

## 12. Core Guardrails

- Detect the active render pipeline first; for HDRP, explain the mismatch and offer only basic comparison guidance, because this skill covers Built-in to URP only.
- Do not mutate the project before rollback safety is confirmed; instead stop in planning mode before URP install, asset assignment, converters, material edits, or scene edits, because converter changes are one-way.
- Do not treat tool logs or created assets as proof; instead save/reload/re-query Graphics, Quality, materials, Volumes, lighting, probes, Console, and representative scenes before status claims, because many of these changes look applied in logs but don't persist.
- Do not collapse partial phases into project-level completion; instead report `Phase complete`, `Partial migration`, and `Manual follow-up` based on the success gate, because an over-claimed "complete" hides work the user still has to do.
- Do not bulk-rewrite fragile rendering code; instead use scoped mappings for custom shaders, `GrabPass`, `OnRenderImage`, replacement shaders, package-owned render code, and unsupported PPv2 effects, because these have no automatic converter path and blind rewrites break them silently.

## 13. Final step

Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.
2. For a migration or repair pass, don't stop at console counts: pass the success gate in [references/migration-gates.md](references/migration-gates.md), including the representative-scene capture in [references/capturing-the-editor.md](references/capturing-the-editor.md). For planning-only or audit requests, stop after step 1.
3. If the change affects runtime behaviour and you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## 14. Reporting Back

Summarize:

- which migration path was used
- whether the project is still Built-in, partially migrated, or fully on URP
- which converters ran
- what was fixed automatically
- what still needs manual work
- which scenes, materials, or cameras were validated
- what remains risky, especially around complex shaders, package effects, and visual parity
- whether post-processing was fully migrated, partially prepared, or only documented for manual follow-up
- whether Quality levels are explicitly assigned or intentionally relying on Graphics settings fallback
- whether particle/VFX materials were converted or still need URP particle-shader follow-up
- whether baked lighting/lightmaps/reflection probes were preserved as reference, verified visually, rebaked/refreshed, or left as manual follow-up
- whether exposure, tonemapping, ambient fill, and camera/Volume post-processing wiring were visually balanced or still remain partial

Use completion language conservatively. If the report contains any `Partial`, `Manual follow-up`, `not validated`, `may still need`, or unsupported-feature item, state that specific completed phases passed and call the overall migration partial/manual. Do not pair a project-level "complete" claim with manual follow-up bullets.
